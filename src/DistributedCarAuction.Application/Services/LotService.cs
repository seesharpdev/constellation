namespace DistributedCarAuction.Application.Services;

using DistributedCarAuction.Application.DTOs;
using DistributedCarAuction.Application.Interfaces;
using DistributedCarAuction.Application.Interfaces.Repositories;
using DistributedCarAuction.Domain.Entities;

public class LotService : ILotService
{
    private readonly ILotRepository _lotRepository;
    private readonly IAuctionRepository _auctionRepository;
    private readonly IVehicleRepository _vehicleRepository;
    private readonly INotificationService _notificationService;
    private readonly IBroadcastService _broadcastService;

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

    public async Task<Lot> CreateLotAsync(CreateLotRequest request)
    {
		// Validate auction exists
		Auction? auction = await _auctionRepository.GetByIdAsync(request.AuctionId) ?? throw new InvalidOperationException($"Auction with ID {request.AuctionId} not found");

		// Validate vehicle exists
		Vehicle? vehicle = await _vehicleRepository.GetByIdAsync(request.VehicleId) ?? throw new InvalidOperationException($"Vehicle with ID {request.VehicleId} not found");
		Lot lot = new(request.AuctionId, vehicle, request.StartingBid, request.ReservePrice);
        
        // Add lot to auction
        auction.AddLot(lot);
        await _auctionRepository.UpdateAsync(auction);

		Lot createdLot = await _lotRepository.AddAsync(lot);
        
        return createdLot;
    }

    public async Task<BidResult> PlaceBidAsync(BidRequest request)
    {
        try
        {
			Lot? lot = await _lotRepository.GetByIdAsync(request.LotId);
            if (lot == null)
                return new BidResult(false, $"Lot with ID {request.LotId} not found");

			// Verify auction is active
			Auction? auction = await _auctionRepository.GetByIdAsync(lot.AuctionId);
            if (auction == null)
                return new BidResult(false, "Associated auction not found");

            if (!auction.CanAcceptBids())
                return new BidResult(false, $"Auction is not active (current state: {auction.State})");

            // Place bid (domain logic will validate amount)
            lot.PlaceBid(request.BidderId, request.Amount);
            await _lotRepository.UpdateAsync(lot);

            // Get the placed bid
            var placedBid = lot.Bids.LastOrDefault();
            
            // Notify about the bid
            await _notificationService.NotifyBidPlaced(request.LotId, request.BidderId, request.Amount);
            await _broadcastService.BroadcastBidAsync(auction.Id, request.LotId, request.Amount);

            return new BidResult(
                true,
                "Bid placed successfully",
                placedBid?.Id,
                lot.GetHighestBidAmount()
            );
        }
        catch (InvalidOperationException ex)
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