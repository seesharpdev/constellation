namespace DistributedCarAuction.Application.DTOs;

using System.ComponentModel.DataAnnotations;

public record CreateAuctionRequest(
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 200 characters")]
    string Title,

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    string Description
);

