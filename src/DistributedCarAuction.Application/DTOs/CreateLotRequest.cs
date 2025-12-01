namespace DistributedCarAuction.Application.DTOs;

using System.ComponentModel.DataAnnotations;

public record CreateLotRequest(
    [Required(ErrorMessage = "Auction ID is required")]
    Guid AuctionId,

    [Required(ErrorMessage = "Vehicle ID is required")]
    Guid VehicleId,

    [Required]
    [Range(0.01, 1000000000, ErrorMessage = "Starting bid must be between €0.01 and €1 billion")]
    decimal StartingBid,

    [Range(0.01, 1000000000, ErrorMessage = "Reserve price must be between €0.01 and €1 billion")]
    decimal? ReservePrice = null
);

