# Distributed Car Auction - Future Considerations

## Completed Work

### Concurrency & Thread Safety (All Phases Complete)

- [x] **Phase 1: Immediate Thread-Safety Fixes** - Thread-safe collections, atomic sequence generation with `Interlocked.Increment`, lock-based collection access
- [x] **Phase 2: Service-Layer Synchronization** - Per-entity `SemaphoreSlim` locks to prevent TOCTOU race conditions
- [x] **Phase 3: Optimistic Concurrency Control** - Version fields in entities, repository version checking, `ConcurrencyException` with retry logic
- [x] **Phase 4: Distributed Sequence Generation** - `ISequenceGenerator` interface, `InMemorySequenceGenerator` for single-instance, `RedisSequenceGenerator` stub for multi-instance
- [x] **Phase 5: Transactional Consistency** - Unit of Work pattern with `IUnitOfWork` and `IUnitOfWorkFactory`
- [x] **Refactor: Clean Architecture** - Services use only Unit of Work, removed direct repository dependencies

### Testing

- [x] **Add unit tests** - Domain entity tests for `Lot`, `Auction`, `Bid`, `BaseEntity` validation logic
- [x] **Add repository concurrency tests** - Version checking, optimistic concurrency
- [x] **Add Unit of Work tests** - Commit, rollback, pending changes tracking
- [x] **Add sequence generator tests** - Thread-safety, per-lot sequences

---

## Security (Critical)

- [ ] **Implement partner authentication** - Partner API (`/api/partners`) is currently `[AllowAnonymous]`. Add API key per partner or OAuth.
- [ ] **Derive BidderId from auth context** - Currently `BidderId` comes from request body, allowing impersonation. Should derive from authenticated user.
- [ ] **Fail closed on missing API key** - If `ApiKey` is not configured, system allows all requests. Should require key in production.
- [ ] **Use constant-time API key comparison** - Replace `==` with `CryptographicOperations.FixedTimeEquals()` to prevent timing attacks.
- [ ] **Move API key to environment variables** - `appsettings.json` should not contain production secrets.

## Security (Medium)

- [ ] **Add lock cleanup for completed auctions** - Static `ConcurrentDictionary<Guid, SemaphoreSlim>` grows unbounded; implement cleanup.
- [ ] **Add audit logging for bids and state changes** - Track who placed bids, when, and state transitions for compliance.
- [ ] **Improve rate limiting** - Currently IP-based; behind proxies all requests appear from same IP. Use authenticated user ID.

---

## Observability

- [ ] **Add health check endpoint** - Implement `/health` endpoint for container orchestration and load balancer health probes.
- [ ] **Add request/response logging middleware** - Use Serilog or custom middleware for debugging and audit trails.
- [ ] **Add Swagger authentication UI** - Allow testers to enter API key directly in Swagger UI using `AddSecurityDefinition`.

## Reliability

- [ ] **Implement idempotency for bid placement** - Add `X-Idempotency-Key` header support to prevent duplicate bids when partners retry failed requests.
- [ ] **Production Kafka integration** - Replace simulated Kafka with `Confluent.Kafka` package and inject `IProducer<string, string>`.
- [ ] **Implement Redis sequence generator** - Complete the `RedisSequenceGenerator` for multi-instance deployments.
- [ ] **Replace in-memory storage with database** - Implement database-backed repositories for persistence.

## Features

- [ ] **Auction scheduling** - Add `ScheduledStartTime` property to allow auctions to start automatically at a specified time.
- [ ] **Bid increments** - Enforce minimum bid increment rules (e.g., bids must be at least €100 higher than current highest).
- [ ] **User/Bidder management** - Replace raw GUIDs with proper user entities including authentication and bidder profiles.
- [ ] **Bid history API** - Expose endpoint to retrieve bid history for a lot (currently available internally via `lot.Bids`).

## Testing

- [ ] **Add rate limiting integration tests** - Verify 429 responses when limits are exceeded.
- [ ] **Add API key rejection tests** - Verify 401 responses for missing/invalid API keys.
- [ ] **Add security integration tests** - Test authentication bypass scenarios.

## Documentation

- [ ] **API documentation** - Add XML comments to all public endpoints for Swagger/OpenAPI generation.
- [ ] **Partner onboarding guide** - Document Kafka topic subscription and bid placement flow for external partners.
