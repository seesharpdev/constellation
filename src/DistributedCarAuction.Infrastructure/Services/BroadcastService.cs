namespace DistributedCarAuction.Infrastructure.Services;

using DistributedCarAuction.Application.Interfaces;
using DistributedCarAuction.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

/// <summary>
/// Simulates broadcasting auction events to external partner platforms.
/// Production: Replace with HTTP webhooks or message queue (Kafka/RabbitMQ) implementation.
/// </summary>
public class BroadcastService : IBroadcastService
{
    private readonly ILogger<BroadcastService> _logger;
    private readonly ConcurrentDictionary<string, string> _partners = new();

    public BroadcastService(ILogger<BroadcastService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task BroadcastAuctionAsync(Auction auction)
    {
        _logger.LogInformation(
            "Broadcasting auction to {PartnerCount} partner(s) - AuctionId: {AuctionId}, State: {State}",
            _partners.Count, auction.Id, auction.State);

        foreach (var partner in _partners)
        {
            _logger.LogDebug("Sending to partner {PartnerId}", partner.Key);
            // Production: POST to partner.Value or publish to message queue
        }

        return Task.CompletedTask;
    }

    public Task BroadcastBidAsync(Guid auctionId, Guid lotId, decimal amount)
    {
        _logger.LogInformation(
            "Broadcasting bid to {PartnerCount} partner(s) - LotId: {LotId}, Amount: {Amount:C}",
            _partners.Count, lotId, amount);

        foreach (var partner in _partners)
        {
            _logger.LogDebug("Sending to partner {PartnerId}", partner.Key);
            // Production: POST to partner.Value or publish to message queue
        }

        return Task.CompletedTask;
    }

    public Task RegisterPartnerAsync(string partnerId, string callbackUrl)
    {
        if (string.IsNullOrWhiteSpace(partnerId))
            throw new ArgumentException("Partner ID cannot be empty", nameof(partnerId));

        if (string.IsNullOrWhiteSpace(callbackUrl))
            throw new ArgumentException("Callback URL cannot be empty", nameof(callbackUrl));

        _partners.AddOrUpdate(partnerId, callbackUrl, (_, _) => callbackUrl);

        _logger.LogInformation("Partner registered - {PartnerId}: {CallbackUrl}", partnerId, callbackUrl);

        return Task.CompletedTask;
    }

    public int GetRegisteredPartnerCount() => _partners.Count;
}