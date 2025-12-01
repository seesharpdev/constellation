namespace DistributedCarAuction.Application.DTOs;

using System.ComponentModel.DataAnnotations;

public record RegisterPartnerRequest(
    [Required(ErrorMessage = "Partner ID is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Partner ID must be between 1 and 100 characters")]
    string PartnerId,

    [Required(ErrorMessage = "Callback URL is required")]
    [Url(ErrorMessage = "Callback URL must be a valid URL")]
    string CallbackUrl
);