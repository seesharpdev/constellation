namespace DistributedCarAuction.Application.Services;

using DistributedCarAuction.Application.DTOs;
using DistributedCarAuction.Application.Interfaces;
using DistributedCarAuction.Domain.Entities;
using DistributedCarAuction.Domain.Exceptions;
using System.Collections.Concurrent;

public class LotService : ILotService
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly INotificationService _notificationService;
    private readonly IBroadcastService _broadcastService;
    private readonly ISequenceGenerator _sequenceGenerator;

    /// <summary>
    /// Maximum number of retry attempts for concurrency conflicts.
    /// </summary>
    private const int MaxRetryAttempts = 3;

    /// <summary>
    /// Base delay between retry attempts (will be multiplied for exponential backoff).
    /// </summary>
    private static readonly TimeSpan RetryBaseDelay = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// Per-lot locks to serialize bid operations on the same lot.
    /// Allows concurrent bids on different lots while preventing race conditions on the same lot.
    /// </summary>
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _lotLocks = new();

    /// <summary>
    /// Per-auction locks to serialize operations that modify auction state.
    /// Used by CreateLotAsync to prevent concurrent lot additions from conflicting.
    /// </summary>
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _auctionLocks = new();

    private static SemaphoreSlim GetLotLock(Guid lotId) => 
        _lotLocks.GetOrAdd(lotId, _ => new SemaphoreSlim(1, 1));

    private static SemaphoreSlim GetAuctionLock(Guid auctionId) => 
        _auctionLocks.GetOrAdd(auctionId, _ => new SemaphoreSlim(1, 1));

    public LotService(
        IUnitOfWorkFactory unitOfWorkFactory,
        INotificationService notificationService,
        IBroadcastService broadcastService,
        ISequenceGenerator sequenceGenerator)
    {
        _unitOfWorkFactory = unitOfWorkFactory ?? throw new ArgumentNullException(nameof(unitOfWorkFactory));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _broadcastService = broadcastService ?? throw new ArgumentNullException(nameof(broadcastService));
        _sequenceGenerator = sequenceGenerator ?? throw new ArgumentNullException(nameof(sequenceGenerator));
    }

    /// <summary>
    /// Creates a new lot and adds it to an auction.
    /// Uses Unit of Work for transactional consistency: both auction update and lot creation
    /// are committed atomically.
    /// Thread-safe: Uses per-auction locking to prevent concurrent lot additions from conflicting.
    /// Handles concurrency conflicts with automatic retry.
    /// </summary>
    public async Task<Lot> CreateLotAsync(CreateLotRequest request)
    {
        SemaphoreSlim auctionLock = GetAuctionLock(request.AuctionId);
        await auctionLock.WaitAsync();
        try
        {
            // Retry loop for concurrency conflicts
            for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
            {
                // Create a new Unit of Work for each attempt (fresh transaction)
                await using IUnitOfWork uow = _unitOfWorkFactory.Create();
                
                try
                {
                    // Validate vehicle exists
                    Vehicle? vehicle = await uow.Vehicles.GetByIdAsync(request.VehicleId) 
                        ?? throw new InvalidOperationException($"Vehicle with ID {request.VehicleId} not found");

                    // Validate auction exists and get fresh copy under lock
                    Auction? auction = await uow.Auctions.GetByIdAsync(request.AuctionId) 
                        ?? throw new InvalidOperationException($"Auction with ID {request.AuctionId} not found");

                    Lot lot = new(request.AuctionId, vehicle, request.StartingBid, request.ReservePrice);
                    
                    // Add lot to auction
                    auction.AddLot(lot);
                    await uow.Auctions.UpdateAsync(auction);

                    // Add lot to repository
                    await uow.Lots.AddAsync(lot);
                    
                    // Commit both changes atomically
                    await uow.CommitAsync();
                    
                    return lot;
                }
                catch (ConcurrencyException) when (attempt < MaxRetryAttempts - 1)
                {
                    // UoW is disposed automatically, discarding uncommitted changes
                    // Exponential backoff before retry
                    await Task.Delay(RetryBaseDelay * (int)Math.Pow(2, attempt));
                }
            }

            // If we get here, all retries failed
            throw new InvalidOperationException("Failed to create lot after multiple attempts due to concurrent modifications");
        }
        finally
        {
            auctionLock.Release();
        }
    }

    /// <summary>
    /// Places a bid on a lot.
    /// Uses Unit of Work for transactional consistency.
    /// Thread-safe: Uses per-lot locking to serialize bids on the same lot.
    /// Concurrent bids on different lots proceed in parallel.
    /// Handles concurrency conflicts with automatic retry.
    /// </summary>
    public async Task<BidResult> PlaceBidAsync(BidRequest request)
    {
        try
        {
            // Quick validation outside lock using a lightweight UoW
            await using (IUnitOfWork validationUow = _unitOfWorkFactory.Create())
            {
                Lot? lot = await validationUow.Lots.GetByIdAsync(request.LotId);
                if (lot == null)
                    return new BidResult(false, $"Lot with ID {request.LotId} not found");

                Auction? auction = await validationUow.Auctions.GetByIdAsync(lot.AuctionId);
                if (auction == null)
                    return new BidResult(false, "Associated auction not found");

                if (!auction.CanAcceptBids())
                    return new BidResult(false, $"Auction is not active (current state: {auction.State})");
            }

            // Variables to capture result for notification after lock release
            BidResult? result = null;
            Guid auctionId = default;

            // Acquire per-lot lock for bid placement
            SemaphoreSlim lotLock = GetLotLock(request.LotId);
            await lotLock.WaitAsync();
            try
            {
                // Retry loop for concurrency conflicts
                for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
                {
                    // Create a new Unit of Work for each attempt (fresh transaction)
                    await using IUnitOfWork uow = _unitOfWorkFactory.Create();
                    
                    try
                    {
                        // Re-fetch lot under lock to get latest state
                        Lot? lot = await uow.Lots.GetByIdAsync(request.LotId);
                        if (lot == null)
                            return new BidResult(false, $"Lot with ID {request.LotId} not found");

                        // Re-check auction state under lock (could have ended)
                        Auction? auction = await uow.Auctions.GetByIdAsync(lot.AuctionId);
                        if (auction == null || !auction.CanAcceptBids())
                            return new BidResult(false, $"Auction is not active (current state: {auction?.State})");

                        auctionId = auction.Id;

                        // HIGH-AVAILABILITY: Check if bid would be valid (for user feedback)
                        bool isCurrentlyValid = lot.WouldBidBeValid(request.Amount);

                        // Generate sequence from distributed sequence generator
                        long sequence = await _sequenceGenerator.GetNextSequenceAsync(request.LotId);

                        // Accept bid regardless of validity (availability over consistency)
                        lot.PlaceBid(request.BidderId, request.Amount, sequence);
                        await uow.Lots.UpdateAsync(lot);

                        // Commit the transaction
                        await uow.CommitAsync();

                        // Get the placed bid (now accurate since we're under lock)
                        Bid? placedBid = lot.Bids.LastOrDefault();
                        
                        // Build result with validity feedback
                        string message = isCurrentlyValid 
                            ? "Bid placed successfully - currently the highest bid"
                            : "Bid accepted but not currently the highest bid";

                        result = new BidResult(
                            true,  // Bid was accepted (availability)
                            message,
                            placedBid?.Id,
                            lot.GetHighestBidAmount(),
                            isCurrentlyValid  // Feedback on current validity
                        );

                        // Success - exit retry loop
                        break;
                    }
                    catch (ConcurrencyException) when (attempt < MaxRetryAttempts - 1)
                    {
                        // UoW is disposed automatically, discarding uncommitted changes
                        // Exponential backoff before retry
                        await Task.Delay(RetryBaseDelay * (int)Math.Pow(2, attempt));
                    }
                }
            }
            finally
            {
                lotLock.Release();
            }

            // Send notifications outside the lock to minimize lock hold time
            if (result != null && result.Success)
            {
                await _notificationService.NotifyBidPlaced(request.LotId, request.BidderId, request.Amount);
                await _broadcastService.BroadcastBidAsync(auctionId, request.LotId, request.Amount);
            }

            return result ?? new BidResult(false, "Failed to place bid after multiple attempts due to concurrent modifications");
        }
        catch (ConcurrencyException ex)
        {
            return new BidResult(false, $"Concurrent modification detected: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            return new BidResult(false, ex.Message);
        }
        catch (Exception ex)
        {
            return new BidResult(false, $"Error placing bid: {ex.Message}");
        }
    }

    public async Task<decimal> GetHighestBidAsync(Guid lotId)
    {
        await using IUnitOfWork uow = _unitOfWorkFactory.Create();
        Lot? lot = await uow.Lots.GetByIdAsync(lotId) 
            ?? throw new InvalidOperationException($"Lot with ID {lotId} not found");
        return lot.GetHighestBidAmount();
    }

    public async Task<Guid?> GetWinnerAsync(Guid lotId)
    {
        await using IUnitOfWork uow = _unitOfWorkFactory.Create();
        Lot? lot = await uow.Lots.GetByIdAsync(lotId) 
            ?? throw new InvalidOperationException($"Lot with ID {lotId} not found");
        return lot.GetWinningBidderId();
    }

    public async Task<Lot?> GetByIdAsync(Guid lotId)
    {
        await using IUnitOfWork uow = _unitOfWorkFactory.Create();
        return await uow.Lots.GetByIdAsync(lotId);
    }
}
