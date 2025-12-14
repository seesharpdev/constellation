namespace DistributedCarAuction.Application.Services;

using DistributedCarAuction.Application.DTOs;
using DistributedCarAuction.Application.Interfaces;
using DistributedCarAuction.Application.Interfaces.Repositories;
using DistributedCarAuction.Domain.Entities;
using DistributedCarAuction.Domain.Enums;
using DistributedCarAuction.Domain.Exceptions;
using System.Collections.Concurrent;

public class AuctionService : IAuctionService
{
    private readonly IAuctionRepository _auctionRepository;
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
    /// Per-auction locks to serialize state transitions.
    /// Prevents race conditions when multiple requests try to start/end the same auction.
    /// </summary>
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> _auctionLocks = new();

    private static SemaphoreSlim GetAuctionLock(Guid auctionId) => 
        _auctionLocks.GetOrAdd(auctionId, _ => new SemaphoreSlim(1, 1));

    public AuctionService(
        IAuctionRepository auctionRepository,
        INotificationService notificationService,
        IBroadcastService broadcastService)
    {
        _auctionRepository = auctionRepository ?? throw new ArgumentNullException(nameof(auctionRepository));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _broadcastService = broadcastService ?? throw new ArgumentNullException(nameof(broadcastService));
    }

    public async Task<Auction> CreateAuctionAsync(CreateAuctionRequest request)
    {
		Auction auction = new(request.Title, request.Description);
		Auction createdAuction = await _auctionRepository.AddAsync(auction);
        
        await _broadcastService.BroadcastAuctionAsync(createdAuction);
        
        return createdAuction;
    }

    /// <summary>
    /// Starts an auction, enabling bid acceptance.
    /// Thread-safe: Uses per-auction locking to prevent concurrent state transitions.
    /// Handles concurrency conflicts with automatic retry.
    /// </summary>
    public async Task StartAuctionAsync(Guid auctionId)
    {
        Auction? auction = null;
        AuctionState newState = AuctionState.Created;

        SemaphoreSlim auctionLock = GetAuctionLock(auctionId);
        await auctionLock.WaitAsync();
        try
        {
            for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
            {
                try
                {
                    auction = await _auctionRepository.GetByIdAsync(auctionId) 
                        ?? throw new InvalidOperationException($"Auction with ID {auctionId} not found");
                    
                    auction.Start();
                    await _auctionRepository.UpdateAsync(auction);
                    newState = auction.State;
                    break; // Success
                }
                catch (ConcurrencyException) when (attempt < MaxRetryAttempts - 1)
                {
                    await Task.Delay(RetryBaseDelay * (int)Math.Pow(2, attempt));
                }
            }
        }
        finally
        {
            auctionLock.Release();
        }

        // Send notifications outside the lock to minimize lock hold time
        if (auction != null)
        {
            await _notificationService.NotifyAuctionStateChanged(auctionId, newState);
            await _broadcastService.BroadcastAuctionAsync(auction);
        }
    }

    /// <summary>
    /// Closes an auction, preventing further bids.
    /// Thread-safe: Uses per-auction locking to prevent concurrent state transitions.
    /// Handles concurrency conflicts with automatic retry.
    /// </summary>
    public async Task CloseAuctionAsync(Guid auctionId)
    {
        Auction? auction = null;
        AuctionState newState = AuctionState.Active;

        SemaphoreSlim auctionLock = GetAuctionLock(auctionId);
        await auctionLock.WaitAsync();
        try
        {
            for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
            {
                try
                {
                    auction = await _auctionRepository.GetByIdAsync(auctionId) 
                        ?? throw new InvalidOperationException($"Auction with ID {auctionId} not found");
                    
                    auction.End();
                    await _auctionRepository.UpdateAsync(auction);
                    newState = auction.State;
                    break; // Success
                }
                catch (ConcurrencyException) when (attempt < MaxRetryAttempts - 1)
                {
                    await Task.Delay(RetryBaseDelay * (int)Math.Pow(2, attempt));
                }
            }
        }
        finally
        {
            auctionLock.Release();
        }

        // Send notifications outside the lock to minimize lock hold time
        if (auction != null)
        {
            await _notificationService.NotifyAuctionStateChanged(auctionId, newState);
            await _broadcastService.BroadcastAuctionAsync(auction);
        }
    }

    public async Task<Auction?> GetByIdAsync(Guid auctionId)
    {
        return await _auctionRepository.GetByIdAsync(auctionId);
    }

    public async Task<List<Auction>> GetAllAsync()
    {
        return await _auctionRepository.GetAllAsync();
    }
}