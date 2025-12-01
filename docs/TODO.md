# Distributed Car Auction - Future Considerations

## Security

- [ ] **Move API key to environment variables** - `appsettings.json` should not contain production secrets. Use `Environment.GetEnvironmentVariable("API_KEY")` or Azure Key Vault.

- [ ] **Add Swagger authentication UI** - Allow testers to enter API key directly in Swagger UI using `AddSecurityDefinition`.

## Observability

- [ ] **Add health check endpoint** - Implement `/health` endpoint for container orchestration and load balancer health probes.

- [ ] **Add request/response logging middleware** - Use Serilog or custom middleware for debugging and audit trails.

## Reliability

- [ ] **Implement idempotency for bid placement** - Add `X-Idempotency-Key` header support to prevent duplicate bids when partners retry failed requests.

- [ ] **Production Kafka integration** - Replace simulated Kafka with `Confluent.Kafka` package and inject `IProducer<string, string>`.

## Features

- [ ] **Auction scheduling** - Add `ScheduledStartTime` property to allow auctions to start automatically at a specified time.

- [ ] **Bid increments** - Enforce minimum bid increment rules (e.g., bids must be at least €100 higher than current highest).

- [ ] **User/Bidder management** - Replace raw GUIDs with proper user entities including authentication and bidder profiles.

- [ ] **Bid history API** - Expose endpoint to retrieve bid history for a lot (currently available internally via `lot.Bids`).

## Testing

- [ ] **Add unit tests** - Domain entity tests for `Lot`, `Auction`, `Bid` validation logic.

- [ ] **Add rate limiting integration tests** - Verify 429 responses when limits are exceeded.

- [ ] **Add API key rejection tests** - Verify 401 responses for missing/invalid API keys.

## Documentation

- [ ] **API documentation** - Add XML comments to all public endpoints for Swagger/OpenAPI generation.

- [ ] **Partner onboarding guide** - Document Kafka topic subscription and bid placement flow for external partners.

