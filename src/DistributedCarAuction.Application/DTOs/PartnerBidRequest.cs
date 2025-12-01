namespace DistributedCarAuction.Application.DTOs;

using System.ComponentModel.DataAnnotations;

public record PartnerBidRequest(
    [Required(ErrorMessage = "Partner ID is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Partner ID must be between 1 and 100 characters")]
    string PartnerId,

    [Required(ErrorMessage = "Lot ID is required")]
    Guid LotId,

    [Required(ErrorMessage = "Bidder ID is required")]
    Guid BidderId,

    [Required]
    [Range(0.01, 1000000000, ErrorMessage = "Amount must be between €0.01 and €1 billion")]
    decimal Amount
);