namespace DistributedCarAuction.Application.Services;

using DistributedCarAuction.Application.DTOs;
using DistributedCarAuction.Application.Interfaces;
using DistributedCarAuction.Application.Interfaces.Repositories;
using DistributedCarAuction.Domain.Entities;
using DistributedCarAuction.Domain.Exceptions;
using System.Collections.Concurrent;

public class LotService : ILotService
{
    private readonly ILotRepository _lotRepository;
    private readonly IAuctionRepository _auctionRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly INotificationService _notificationService;
    private readonly IBroadcastService _broadcastService;

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
        ILotRepository lotRepository,
        IAuctionRepository auctionRepository,
        IVehicleRepository vehicleRepository,
        INotificationService notificationService,
        IBroadcastService broadcastService)
    {
        _lotRepository = lotRepository ?? throw new ArgumentNullException(nameof(lotRepository));
        _auctionRepository = auctionRepository ?? throw new ArgumentNullException(nameof(auctionRepository));
        _vehicleRepository = vehicleRepository ?? throw new ArgumentNullException(nameof(vehicleRepository));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _broadcastService = broadcastService ?? throw new ArgumentNullException(nameof(broadcastService));
    }

    /// <summary>
    /// Creates a new lot and adds it to an auction.
    /// Thread-safe: Uses per-auction locking to prevent concurrent lot additions from conflicting.
    /// Handles concurrency conflicts with automatic retry.
    /// </summary>
    public async Task<Lot> CreateLotAsync(CreateLotRequest request)
    {
        // Validate vehicle exists (can be done outside lock)
        Vehicle? vehicle = await _vehicleRepository.GetByIdAsync(request.VehicleId) 
            ?? throw new InvalidOperationException($"Vehicle with ID {request.VehicleId} not found");

        SemaphoreSlim auctionLock = GetAuctionLock(request.AuctionId);
        await auctionLock.WaitAsync();
        try
        {
            // Retry loop for concurrency conflicts
            for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
            {
                try
                {
                    // Validate auction exists and get fresh copy under lock
                    Auction? auction = await _auctionRepository.GetByIdAsync(request.AuctionId) 
                        ?? throw new InvalidOperationException($"Auction with ID {request.AuctionId} not found");

                    Lot lot = new(request.AuctionId, vehicle, request.StartingBid, request.ReservePrice);
                    
                    // Add lot to auction (atomic within lock)
                    auction.AddLot(lot);
                    await _auctionRepository.UpdateAsync(auction);

                    Lot createdLot = await _lotRepository.AddAsync(lot);
                    
                    return createdLot;
                }
                catch (ConcurrencyException) when (attempt < MaxRetryAttempts - 1)
                {
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
    /// Thread-safe: Uses per-lot locking to serialize bids on the same lot.
    /// Concurrent bids on different lots proceed in parallel.
    /// Handles concurrency conflicts with automatic retry.
    /// </summary>
    public async Task<BidResult> PlaceBidAsync(BidRequest request)
    {
        try
        {
            // Quick validation outside lock (lot existence)
            Lot? lot = await _lotRepository.GetByIdAsync(request.LotId);
            if (lot == null)
                return new BidResult(false, $"Lot with ID {request.LotId} not found");

            // Verify auction is active (can check outside lock - state transitions are atomic)
            Auction? auction = await _auctionRepository.GetByIdAsync(lot.AuctionId);
            if (auction == null)
                return new BidResult(false, "Associated auction not found");

            if (!auction.CanAcceptBids())
                return new BidResult(false, $"Auction is not active (current state: {auction.State})");

            // Variables to capture result for notification after lock release
            BidResult? result = null;
            Guid auctionId = auction.Id;

            // Acquire per-lot lock for bid placement
            SemaphoreSlim lotLock = GetLotLock(request.LotId);
            await lotLock.WaitAsync();
            try
            {
                // Retry loop for concurrency conflicts
                for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
                {
                    try
                    {
                        // Re-fetch lot under lock to get latest state
                        lot = await _lotRepository.GetByIdAsync(request.LotId);
                        if (lot == null)
                            return new BidResult(false, $"Lot with ID {request.LotId} not found");

                        // Re-check auction state under lock (could have ended)
                        auction = await _auctionRepository.GetByIdAsync(lot.AuctionId);
                        if (auction == null || !auction.CanAcceptBids())
                            return new BidResult(false, $"Auction is not active (current state: {auction?.State})");

                        // HIGH-AVAILABILITY: Check if bid would be valid (for user feedback)
                        bool isCurrentlyValid = lot.WouldBidBeValid(request.Amount);

                        // Accept bid regardless of validity (availability over consistency)
                        lot.PlaceBid(request.BidderId, request.Amount);
                        await _lotRepository.UpdateAsync(lot);

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
        Lot? lot = await _lotRepository.GetByIdAsync(lotId) ?? throw new InvalidOperationException($"Lot with ID {lotId} not found");
		return lot.GetHighestBidAmount();
    }

    public async Task<Guid?> GetWinnerAsync(Guid lotId)
    {
        Lot? lot = await _lotRepository.GetByIdAsync(lotId) ?? throw new InvalidOperationException($"Lot with ID {lotId} not found");
		return lot.GetWinningBidderId();
    }

    public async Task<Lot?> GetByIdAsync(Guid lotId)
    {
        return await _lotRepository.GetByIdAsync(lotId);
    }
}