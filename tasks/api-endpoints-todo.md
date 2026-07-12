# Task List: Fix Broken Stalwart API Endpoints

**Status**: Ready for Implementation  
**Priority**: CRITICAL - All API operations currently failing  
**Created**: 2026-07-10

---

## 🚨 Critical Blockers (Do First)

- [ ] **Task 0: Fix constructor bug**
  - File: `StalwartClient.cs` line 57-59
  - Issue: `MaxAutomaticRedirections = 0` throws exception
  - Fix: Remove or set to valid value
  - **Blocks**: All unit tests
  - **Effort**: 5 min
  - **Priority**: CRITICAL

- [ ] **Task 1: Update authentication to match OpenAPI spec**
  - File: `StalwartClient.cs`
  - Change `POST /api/v1/auth/login` → `POST /api/auth`
  - Implement proper LoginRequest/Response models
  - Add token exchange flow
  - **Effort**: 2 hours
  - **Priority**: CRITICAL

- [ ] **Task 2: Update all domain/account/alias paths**
  - File: `StalwartClient.cs`
  - Change `/api/v1/domains*` → verify correct path
  - Change `/api/v1/accounts*` → verify correct path
  - Change `/api/v1/aliases*` → verify correct path
  - **Action**: Research actual endpoints first
  - **Effort**: 1 hour (research) + 1 hour (implementation)
  - **Priority**: CRITICAL

---

## 📋 Implementation Tasks

### Phase 1: Core Authentication & Paths
- [ ] Update `AuthenticateAsync` to use `/api/auth`
- [ ] Add `ExchangeCodeForTokenAsync` for `/auth/token`
- [ ] Add `GetAccountAsync` for `/api/account`
- [ ] Fix all domain/account/alias endpoint paths
- [ ] Update `RefreshTokenAsync` flow

### Phase 2: Add Missing OpenAPI Endpoints
- [ ] Add `DiscoverOidcProviderAsync(email)` → `GET /api/discover/{email}`
- [ ] Add `GetSchemaRedirectAsync()` → `GET /api/schema`
- [ ] Add `GetSchemaAsync(hash)` → `GET /api/schema/{hash}`
- [ ] Add `IssueDeliveryTokenAsync()` → `GET /api/token/delivery`
- [ ] Add `IssueTracingTokenAsync()` → `GET /api/token/tracing`
- [ ] Add `IssueMetricsTokenAsync()` → `GET /api/token/metrics`
- [ ] Add live telemetry endpoints (SSE support)

### Phase 3: Update Models
- [ ] Add `LoginRequest` and variants (AuthCode, AuthDevice)
- [ ] Add `LoginResponse` and variants
- [ ] Add `Account` model for `/api/account`
- [ ] Add `ProblemDetails` for RFC 7807 errors
- [ ] Add OIDC discovery model
- [ ] Add schema response model
- [ ] Add token response models
- [ ] Add SSE event models

### Phase 4: Update Interface & Manager
- [ ] Update `IStalwartClient.cs` with new method signatures
- [ ] Update `AccountManager.cs` to use new client methods
- [ ] Update error handling for new response types

### Phase 5: Tests
- [ ] Unskip constructor tests
- [ ] Add tests for new endpoints
- [ ] Update test URLs
- [ ] Add integration tests

---

## 📝 Research Tasks (Do Before Implementation)

- [ ] **Verify actual API endpoints** on a real Stalwart server
  - Test `/api/auth` exists?
  - Test `/api/account` exists?
  - Test `/api/domains` exists?
  - Test `/api/accounts` exists?
  - Test `/api/aliases` exists?

- [ ] **Check Vandelay source code**
  - How does it authenticate with Stalwart?
  - How does it create domains/accounts?
  - Does it use JMAP or REST API?

- [ ] **Check Stalwart API documentation**
  - Are there multiple API versions?
  - Is there a separate Admin API?
  - What's the relationship between Management API and JMAP?

---

## 🎯 Quick Wins (Can Do Immediately)

1. **Fix constructor bug** (5 min)
   - Remove `MaxAutomaticRedirections = 0` from HttpClientHandler

2. **Add OpenAPI models** (1 hour)
   - Create LoginRequest/Response classes
   - Create Account model for /api/account
   - Add to StalwartApiModels.cs

3. **Update authentication** (2 hours)
   - Change endpoint from `/api/v1/auth/login` to `/api/auth`
   - Implement code exchange flow

---

## ⏳ Estimated Timeline

| Phase | Tasks | Time | Dependencies |
|-------|-------|------|--------------|
| Research | Verify endpoints | 1-2 hours | None |
| Quick Fix | Constructor bug | 5 min | None |
| Phase 1 | Auth + Paths | 3-4 hours | Research |
| Phase 2 | New Endpoints | 2-3 hours | Phase 1 |
| Phase 3 | Models | 2 hours | Can run parallel |
| Phase 4 | Interface + Manager | 1-2 hours | Phase 1, 3 |
| Phase 5 | Tests | 2 hours | All above |
| **Total** | | **10-13 hours** | |

---

## 📊 Progress Tracking

- [ ] Research completed
- [ ] Constructor bug fixed
- [ ] Authentication updated
- [ ] Paths verified and updated
- [ ] New endpoints implemented
- [ ] Models updated
- [ ] Interface updated
- [ ] AccountManager updated
- [ ] Tests passing
- [ ] Manual testing completed

---

## 🔗 Related Files

- `src/StalwartMigration/Infrastructure/Stalwart/StalwartClient.cs`
- `src/StalwartMigration/Infrastructure/Stalwart/IStalwartClient.cs`
- `src/StalwartMigration/Infrastructure/Stalwart/StalwartApiModels.cs`
- `src/StalwartMigration/Infrastructure/Stalwart/AccountManager.cs`
- `tests/StalwartMigration.Tests/Unit/InfrastructureTests/StalwartClientTests.cs`

---

*Last updated: 2026-07-10*
