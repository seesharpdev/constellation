namespace DistributedCarAuction.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DistributedCarAuction.Application.DTOs;
using DistributedCarAuction.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

/// <summary>
/// Comprehensive integration tests covering:
/// - Complete auction structure creation
/// - Auction state transitions
/// - Bid placement and ordering
/// - Partner notifications
/// - Winner verification and bid integrity
/// </summary>
public class AuctionIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    // Test constants
    private const string TestPartnerId = "ExternalAuctionPartner1";

    public AuctionIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    #region Helper Methods

    private async Task<Guid> CreateVehicleAsync(CreateVehicleRequest request)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(ApiEndpoints.Vehicles.Base, request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        JsonElement json = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);

        return json.GetProperty("Id").GetGuid();
    }

    private async Task<Guid> CreateAuctionAsync(string title, string description)
    {
        CreateAuctionRequest request = new(title, description);
        HttpResponseMessage response = await _client.PostAsJsonAsync(ApiEndpoints.Auctions.Base, request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        JsonElement json = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);

        return json.GetProperty("Id").GetGuid();
    }

    private async Task<Guid> CreateLotAsync(Guid auctionId, Guid vehicleId, decimal startingBid, decimal? reservePrice = null)
    {
        CreateLotRequest request = new(auctionId, vehicleId, startingBid, reservePrice);
        HttpResponseMessage response = await _client.PostAsJsonAsync(ApiEndpoints.Lots.Base, request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        JsonElement json = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);

        return json.GetProperty("Id").GetGuid();
    }

    private async Task<BidResult> PlaceBidAsync(Guid lotId, Guid bidderId, decimal amount)
    {
        BidRequest request = new(lotId, bidderId, amount);
        HttpResponseMessage response = await _client.PostAsJsonAsync(ApiEndpoints.Lots.Bids, request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        BidResult? result = await response.Content.ReadFromJsonAsync<BidResult>(_jsonOptions);
        result.Should().NotBeNull();

        return result!;
    }

    private async Task StartAuctionAsync(Guid auctionId)
    {
        HttpResponseMessage response = await _client.PostAsync(ApiEndpoints.Auctions.Start(auctionId), null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task CloseAuctionAsync(Guid auctionId)
    {
        HttpResponseMessage response = await _client.PostAsync(ApiEndpoints.Auctions.Close(auctionId), null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<decimal> GetHighestBidAsync(Guid lotId)
    {
        HttpResponseMessage response = await _client.GetAsync(ApiEndpoints.Lots.HighestBid(lotId));
        JsonElement json = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);

        return json.GetProperty("highestBid").GetDecimal();
    }

    private async Task<Guid?> GetWinnerAsync(Guid lotId)
    {
        HttpResponseMessage response = await _client.GetAsync(ApiEndpoints.Lots.Winner(lotId));
        JsonElement json = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        JsonElement winnerProperty = json.GetProperty("winner");

        return winnerProperty.ValueKind == JsonValueKind.Null ? null : winnerProperty.GetGuid();
    }

    private async Task<int> GetAuctionStateAsync(Guid auctionId)
    {
        HttpResponseMessage response = await _client.GetAsync(ApiEndpoints.Auctions.ById(auctionId));
        JsonElement json = await response.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        
        return json.GetProperty("State").GetInt32();
    }

    #endregion

    #region Test Data Factories

    private static CreateVehicleRequest CreateBmwI4Request() => new(
        VehicleType.Sedan,
        "BMW",
        "i4 M50",
        2023,
        "WBY73AW09PCN12345",
        28000m,
        "Brooklyn Grey",
        new Dictionary<string, object>
        {
            { "NumberOfDoors", 4 },
            { "HasSunroof", true }
        });

    private static CreateVehicleRequest CreateFordRangerRaptorRequest() => new(
        VehicleType.Truck,
        "Ford",
        "Ranger Raptor",
        2023,
        "6FPPX8EE7NPC12345",
        22000m,
        "Conquer Grey",
        new Dictionary<string, object>
        {
            { "LoadCapacityKg", 620 },
            { "BedLengthCm", 154 },
            { "HasFourWheelDrive", true }
        });

    private static CreateVehicleRequest CreateMiniCountrymanRequest() => new(
        VehicleType.SUV,
        "MINI",
        "Countryman Cooper S John Cooper Works",
        2022,
        "WMWXU9C50N2P12345",
        35000m,
        "Chili Red",
        new Dictionary<string, object>
        {
            { "SeatingCapacity", 5 },
            { "HasFourWheelDrive", true },
            { "CargoCapacityLiters", 450 }
        });

    private static CreateVehicleRequest CreateBmw330eRequest() => new(
        VehicleType.Sedan,
        "BMW",
        "330e",
        2023,
        "WBA53FJ09PCK12345",
        18000m,
        "Alpine White",
        new Dictionary<string, object>
        {
            { "NumberOfDoors", 4 },
            { "HasSunroof", true }
        });

    #endregion

    [Fact]
    public async Task CompleteAuctionWorkflow_ShouldSucceed()
    {
        // Arrange: Create Vehicles
        Guid bmwId = await CreateVehicleAsync(CreateBmwI4Request());
        Guid raptorId = await CreateVehicleAsync(CreateFordRangerRaptorRequest());
        Guid miniId = await CreateVehicleAsync(CreateMiniCountrymanRequest());

        // Create Auction
        Guid auctionId = await CreateAuctionAsync(
            "December Car Auction 2025",
            "Premium vehicles auction featuring sedans, trucks, and SUVs");
        
        (await GetAuctionStateAsync(auctionId)).Should().Be((int)AuctionState.Created);

        // Create Lots
        Guid lot1Id = await CreateLotAsync(auctionId, bmwId, 15000m, 18000m);
        Guid lot2Id = await CreateLotAsync(auctionId, raptorId, 20000m, 25000m);
        Guid lot3Id = await CreateLotAsync(auctionId, miniId, 25000m);

        // Start Auction
        await StartAuctionAsync(auctionId);
        (await GetAuctionStateAsync(auctionId)).Should().Be((int)AuctionState.Active);

        // Place Bids
        Guid bidder1 = Guid.NewGuid();
        Guid bidder2 = Guid.NewGuid();
        Guid bidder3 = Guid.NewGuid();

        // Lot 1 - BMW i4 M50 (multiple bids, testing ordering)
        BidResult bid1Result = await PlaceBidAsync(lot1Id, bidder1, 16000m);
        bid1Result.Success.Should().BeTrue();
        bid1Result.CurrentHighestBid.Should().Be(16000m);

        BidResult bid2Result = await PlaceBidAsync(lot1Id, bidder2, 17000m);
        bid2Result.Success.Should().BeTrue();
        bid2Result.CurrentHighestBid.Should().Be(17000m);

        BidResult bid3Result = await PlaceBidAsync(lot1Id, bidder3, 19000m);
        bid3Result.Success.Should().BeTrue();
        bid3Result.CurrentHighestBid.Should().Be(19000m);

        // Test low bid - HIGH AVAILABILITY: accepted but not currently highest
        BidRequest lowBid = new(lot1Id, bidder1, 18000m);
        HttpResponseMessage lowBidResponse = await _client.PostAsJsonAsync(ApiEndpoints.Lots.Bids, lowBid);
        lowBidResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        BidResult? lowBidResult = await lowBidResponse.Content.ReadFromJsonAsync<BidResult>(_jsonOptions);
        lowBidResult.Should().NotBeNull();
        lowBidResult!.Success.Should().BeTrue();  // Bid accepted (availability)
        lowBidResult.IsCurrentlyHighest.Should().BeFalse();  // But not the highest (consistency feedback)

        // Lot 2 - Ford Ranger Raptor (single bid below reserve)
        await PlaceBidAsync(lot2Id, bidder1, 23000m);

        // Lot 3 - MINI Countryman JCW (bid above starting, no reserve)
        await PlaceBidAsync(lot3Id, bidder2, 27000m);

        // Partner Bids
        Guid partnerBidderId = Guid.NewGuid();
        object partnerBid = new { PartnerId = TestPartnerId, LotId = lot2Id, BidderId = partnerBidderId, Amount = 26000m };
        HttpResponseMessage partnerBidResponse = await _client.PostAsJsonAsync(ApiEndpoints.Partners.Bids, partnerBid);
        partnerBidResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify Highest Bids
        (await GetHighestBidAsync(lot1Id)).Should().Be(19000m);
        (await GetHighestBidAsync(lot2Id)).Should().Be(26000m);

        // Close Auction
        await CloseAuctionAsync(auctionId);
        (await GetAuctionStateAsync(auctionId)).Should().Be((int)AuctionState.Ended);

        // Verify Winners
        (await GetWinnerAsync(lot1Id)).Should().Be(bidder3);
        (await GetWinnerAsync(lot2Id)).Should().Be(partnerBidderId);
        (await GetWinnerAsync(lot3Id)).Should().Be(bidder2);

        // Verify bids are rejected when the auction is closed
        BidRequest postCloseBid = new(lot1Id, bidder1, 20000m);
        HttpResponseMessage postCloseBidResponse = await _client.PostAsJsonAsync(ApiEndpoints.Lots.Bids, postCloseBid);
        postCloseBidResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task VehicleSearch_ShouldReturnFilteredResults()
    {
        // Create multiple vehicles
        await CreateVehicleAsync(CreateBmwI4Request());
        await CreateVehicleAsync(CreateBmw330eRequest());
        await CreateVehicleAsync(CreateMiniCountrymanRequest());

        // Search by make
        SearchFilter searchFilter = new(Make: "BMW");
        HttpResponseMessage searchResponse = await _client.PostAsJsonAsync(ApiEndpoints.Vehicles.Search, searchFilter);
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonElement resultsJson = await searchResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        resultsJson.GetArrayLength().Should().BeGreaterThanOrEqualTo(2);
        
        foreach (JsonElement vehicle in resultsJson.EnumerateArray())
        {
            vehicle.GetProperty("Make").GetString().Should().Contain("BMW");
        }

        // Search by type
        SearchFilter typeFilter = new(VehicleType: VehicleType.Sedan);
        HttpResponseMessage typeResponse = await _client.PostAsJsonAsync(ApiEndpoints.Vehicles.Search, typeFilter);
        typeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        JsonElement typeResultsJson = await typeResponse.Content.ReadFromJsonAsync<JsonElement>(_jsonOptions);
        
        foreach (JsonElement vehicle in typeResultsJson.EnumerateArray())
        {
            vehicle.GetProperty("VehicleType").GetInt32().Should().Be((int)VehicleType.Sedan);
        }
    }

    [Fact]
    public async Task AuctionStateValidation_ShouldEnforceRules()
    {
        // Create auction
        Guid auctionId = await CreateAuctionAsync("Test Auction", "Testing state transitions");

        // Cannot close auction before starting (wrong state transition)
        HttpResponseMessage closeBeforeStartResponse = await _client.PostAsync(ApiEndpoints.Auctions.Close(auctionId), null);
        closeBeforeStartResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Cannot start auction without lots
        HttpResponseMessage startWithoutLotsResponse = await _client.PostAsync(ApiEndpoints.Auctions.Start(auctionId), null);
        startWithoutLotsResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BidValidation_HighAvailability_ShouldAcceptAllBidsWithValidityFeedback()
    {
        // Setup: Create vehicle, auction, lot
        Guid vehicleId = await CreateVehicleAsync(CreateBmw330eRequest());
        Guid auctionId = await CreateAuctionAsync("Bid Test Auction", "Testing high-availability bidding");
        Guid lotId = await CreateLotAsync(auctionId, vehicleId, 5000m);

        // Start auction
        await StartAuctionAsync(auctionId);

        Guid bidderId1 = Guid.NewGuid();
        Guid bidderId2 = Guid.NewGuid();

        // First bid - valid and highest
        BidResult firstBid = await PlaceBidAsync(lotId, bidderId1, 6000m);
        firstBid.Success.Should().BeTrue();
        firstBid.IsCurrentlyHighest.Should().BeTrue();

        // Second bid - equal amount (accepted but not highest)
        BidRequest equalBid = new(lotId, bidderId2, 6000m);
        HttpResponseMessage equalResponse = await _client.PostAsJsonAsync(ApiEndpoints.Lots.Bids, equalBid);
        equalResponse.StatusCode.Should().Be(HttpStatusCode.OK);  // Accepted for availability
        BidResult? equalResult = await equalResponse.Content.ReadFromJsonAsync<BidResult>(_jsonOptions);
        equalResult.Should().NotBeNull();
        equalResult!.Success.Should().BeTrue();  // Bid was accepted
        equalResult.IsCurrentlyHighest.Should().BeFalse();  // But not the highest

        // Third bid - lower amount (accepted but not highest)
        BidRequest lowBid = new(lotId, Guid.NewGuid(), 5500m);
        HttpResponseMessage lowResponse = await _client.PostAsJsonAsync(ApiEndpoints.Lots.Bids, lowBid);
        lowResponse.StatusCode.Should().Be(HttpStatusCode.OK);  // Accepted for availability
        BidResult? lowResult = await lowResponse.Content.ReadFromJsonAsync<BidResult>(_jsonOptions);
        lowResult.Should().NotBeNull();
        lowResult!.Success.Should().BeTrue();  // Bid was accepted
        lowResult.IsCurrentlyHighest.Should().BeFalse();  // But not the highest

        // Verify highest bid is still the first valid one
        decimal highestBid = await GetHighestBidAsync(lotId);
        highestBid.Should().Be(6000m);

        // Fourth bid - higher amount (accepted and is highest)
        BidResult higherBid = await PlaceBidAsync(lotId, bidderId2, 7000m);
        higherBid.Success.Should().BeTrue();
        higherBid.IsCurrentlyHighest.Should().BeTrue();
        higherBid.CurrentHighestBid.Should().Be(7000m);
    }
}
