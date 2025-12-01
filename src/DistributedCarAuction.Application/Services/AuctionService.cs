namespace DistributedCarAuction.Application.Services;

using DistributedCarAuction.Application.DTOs;
using DistributedCarAuction.Application.Interfaces;
using DistributedCarAuction.Application.Interfaces.Repositories;
using DistributedCarAuction.Domain.Entities;

public class AuctionService : IAuctionService
{
    private readonly IAuctionRepository _auctionRepository;
    private readonly INotificationService _notificationService;
    private readonly IBroadcastService _broadcastService;

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

    public async Task StartAuctionAsync(Guid auctionId)
    {
		Auction? auction = await _auctionRepository.GetByIdAsync(auctionId) ?? throw new InvalidOperationException($"Auction with ID {auctionId} not found");
		auction.Start();
        await _auctionRepository.UpdateAsync(auction);
        await _notificationService.NotifyAuctionStateChanged(auctionId, auction.State);
        await _broadcastService.BroadcastAuctionAsync(auction);
    }

    public async Task CloseAuctionAsync(Guid auctionId)
    {
		Auction auction = await _auctionRepository.GetByIdAsync(auctionId) ?? throw new InvalidOperationException($"Auction with ID {auctionId} not found");
		auction.End();
        await _auctionRepository.UpdateAsync(auction);
        await _notificationService.NotifyAuctionStateChanged(auctionId, auction.State);
        await _broadcastService.BroadcastAuctionAsync(auction);
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