# Plan: Fix Broken Stalwart API Endpoints

## Executive Summary

The **StalwartMigration** tool's API client is using **incorrect endpoint paths** that don't match the official Stalwart Management API specification (v1) at https://github.com/stalwartlabs/stalwart/blob/main/api/v1/openapi.yml. This causes all API calls to fail with 404 Not Found errors.

**Root Cause**: The client uses `/api/v1/` prefixed paths (e.g., `/api/v1/auth/login`, `/api/v1/domains`) but the actual Stalwart API uses `/api/` paths (e.g., `/api/auth`, `/api/discover/{email}`, `/api/account`). Additionally, the authentication flow is completely different.

**Impact**: **100% of API operations fail** - authentication, domain management, account management, and alias management all use wrong URLs.

---

## Comparison: OpenAPI Spec vs Current Implementation

### OpenAPI Spec (Official Stalwart API)

| Endpoint | Method | Purpose | Authentication |
|----------|--------|---------|----------------|
| `/api/auth` | POST | Authenticate & get auth code | Anonymous |
| `/api/discover/{email}` | GET | Discover OIDC provider | Anonymous |
| `/api/account` | GET | Get account info | Bearer/Basic |
| `/api/schema` | GET | Redirect to versioned schema | Bearer/Basic |
| `/api/schema/{hash}` | GET | Get schema at hash | Bearer/Basic |
| `/api/token/delivery` | GET | Issue delivery token | Bearer/Basic |
| `/api/token/tracing` | GET | Issue tracing token | Bearer/Basic |
| `/api/token/metrics` | GET | Issue metrics token | Bearer/Basic |
| `/api/live/delivery/{target}` | GET | Stream delivery (SSE) | Bearer/Basic/Token |
| `/api/live/tracing` | GET | Stream tracing (SSE) | Bearer/Basic/Token |
| `/api/live/metrics` | GET | Stream metrics (SSE) | Bearer/Basic/Token |

**Note**: The OpenAPI spec does NOT include endpoints for:
- `/api/v1/domains*` (domain CRUD)
- `/api/v1/accounts*` (account CRUD)
- `/api/v1/aliases*` (alias CRUD)
- `/api/v1/auth/login` or `/api/v1/auth/refresh`
- `/api/v1/health`

These are likely part of a **different API version** or a **JMAP API** (which the spec mentions handles most configuration).

### Current Implementation (StalwartClient.cs)

| Current Path | Should Be | Status |
|--------------|-----------|--------|
| `/api/v1/auth/login` | `/api/auth` | ❌ WRONG |
| `/api/v1/auth/refresh` | `/auth/token` (exchange code) | ❌ WRONG |
| `/api/v1/health` | Not in spec | ❌ NOT IN SPEC |
| `/api/v1/domains*` | Not in spec | ❌ NOT IN SPEC |
| `/api/v1/accounts*` | Not in spec | ❌ NOT IN SPEC |
| `/api/v1/aliases*` | Not in spec | ❌ NOT IN SPEC |

---

## Authentication Flow Mismatch

### OpenAPI Spec Flow:
1. **POST `/api/auth`** with `LoginRequest` (type, accountName, accountSecret, clientId, etc.)
2. Receive `LoginResponse` with `clientCode` (NOT an access token)
3. **POST `/auth/token`** to exchange `clientCode` for `access_token`
4. Use `access_token` in `Authorization: Bearer` header for subsequent requests

### Current Implementation Flow:
1. **POST `/api/v1/auth/login`** with `{username, password}`
2. Expects `AuthTokenResponse` with `access_token` directly
3. This endpoint **does not exist** in the spec

---

## Proposed Solution

### Option 1: Update Client to Match OpenAPI Spec (RECOMMENDED)
Align the client with the official OpenAPI specification.

**Pros**:
- ✅ Matches official Stalwart API
- ✅ Future-proof
- ✅ Correct authentication flow
- ✅ Access to documented endpoints (account info, schema, etc.)

**Cons**:
- ❌ Domain/Account/Alias CRUD endpoints not in spec (need JMAP or different approach)
- ⚠️ Requires research on how to manage domains/accounts via JMAP or undocumented APIs

### Option 2: Use JMAP API for Data Management
The OpenAPI spec states: "most of the server's configuration and data is managed via JMAP (see `POST /jmap/`)"

This suggests that:
- **Management API** (`/api/*`) handles: auth, account info, schema, telemetry
- **JMAP API** (`/jmap/`) handles: domains, accounts, aliases, messages

**Pros**:
- ✅ Follows Stalwart's architecture
- ✅ JMAP is a standard protocol

**Cons**:
- ❌ Requires implementing JMAP client
- ❌ More complex
- ❌ Vandelay already handles JMAP (per SPEC.md)

### Option 3: Hybrid Approach (RECOMMENDED)
1. **Management API** (`/api/*`) for authentication and account info
2. **Keep current domain/account/alias operations** but update paths if they exist in a different version
3. **Research**: The paths might exist in a different API version or base path

**Investigation Needed**: 
- Check if `/api/domains`, `/api/accounts`, `/api/aliases` exist (without `v1`)
- Check if there's a separate admin API
- Check Vandelay's source code for how it interacts with Stalwart

---

## Decision: Hybrid Approach with Path Correction

Based on the SPEC.md which states the tool uses "Stalwart REST API (v1)" and the OpenAPI spec being for the **Management API**, I'll assume:

1. **Management API** endpoints (`/api/*`) - for auth, account info, schema, telemetry
2. **Admin/Configuration API** - might be at a different base path for domains/accounts/aliases

However, since the OpenAPI spec explicitly says it covers "interactive login, account introspection, configuration schema retrieval and live telemetry streams" and that "most of the server's configuration and data is managed via JMAP", the domain/account/alias endpoints likely:
- Don't exist in the Management API at all, OR
- Are at a different base path

**Action Plan**: Update all paths to match the OpenAPI spec and add JMAP integration for domain/account management.

---

## Detailed Implementation Plan

### Phase 1: Fix Authentication (CRITICAL)

**Task 1.1**: Update authentication to match OpenAPI spec
- **File**: `StalwartClient.cs`
- **Changes**:
  - Change `AuthenticateAsync` to use `POST /api/auth` instead of `/api/v1/auth/login`
  - Implement proper `LoginRequest` model (type, accountName, accountSecret, clientId, etc.)
  - Handle `LoginResponse` with `clientCode`
  - Add `ExchangeCodeForTokenAsync` method to call `/auth/token`
  - Add `RefreshTokenAsync` to use `/auth/token` with refresh_token grant
- **New Models Needed**:
  - `LoginRequestAuthCode` / `LoginRequestAuthDevice` (tagged union)
  - `LoginResponse` variants
  - Token exchange models
- **Estimated Scope**: Large (1 file, ~100 lines changed)

**Task 1.2**: Add `/api/account` endpoint support
- **File**: `StalwartClient.cs`
- **Changes**:
  - Add `GetAccountAsync()` using `GET /api/account`
  - Add `Account` response model matching spec
- **Estimated Scope**: Small (1 file, ~20 lines)

### Phase 2: Fix API Paths for All Endpoints

**Task 2.1**: Update all domain endpoints
- **File**: `StalwartClient.cs`
- **Changes**:
  - If domains API exists: Change `/api/v1/domains*` to correct path
  - If not in spec: Mark as TODO for JMAP integration
- **Action**: Research if these exist at `/api/domains*` (without v1)

**Task 2.2**: Update all account endpoints  
- **File**: `StalwartClient.cs`
- **Changes**:
  - If accounts API exists: Change `/api/v1/accounts*` to correct path
  - If not in spec: Mark as TODO for JMAP integration

**Task 2.3**: Update all alias endpoints
- **File**: `StalwartClient.cs`
- **Changes**:
  - If aliases API exists: Change `/api/v1/aliases*` to correct path
  - If not in spec: Mark as TODO for JMAP integration

**Task 2.4**: Fix health check endpoint
- **File**: `StalwartClient.cs`
- **Changes**:
  - Health check not in OpenAPI spec
  - Options: Remove, use `/api/account` as health proxy, or keep as custom
- **Decision**: Keep for backward compatibility, document as non-standard

### Phase 3: Add Missing Endpoints from OpenAPI

**Task 3.1**: Add OIDC discovery endpoint
- **File**: `StalwartClient.cs`, `IStalwartClient.cs`
- **Changes**:
  - Add `DiscoverOidcProviderAsync(email)` using `GET /api/discover/{email}`
  - Add response model
- **Estimated Scope**: Small (2 files, ~30 lines)

**Task 3.2**: Add schema endpoints
- **File**: `StalwartClient.cs`, `IStalwartClient.cs`
- **Changes**:
  - Add `GetSchemaRedirectAsync()` using `GET /api/schema`
  - Add `GetSchemaAsync(hash)` using `GET /api/schema/{hash}`
  - Handle gzip compression and caching headers
- **Estimated Scope**: Medium (2 files, ~50 lines)

**Task 3.3**: Add token endpoints
- **File**: `StalwartClient.cs`, `IStalwartClient.cs`
- **Changes**:
  - Add `IssueDeliveryTokenAsync()` using `GET /api/token/delivery`
  - Add `IssueTracingTokenAsync()` using `GET /api/token/tracing`
  - Add `IssueMetricsTokenAsync()` using `GET /api/token/metrics`
- **Estimated Scope**: Small (2 files, ~40 lines)

**Task 3.4**: Add live telemetry endpoints (SSE support)
- **File**: `StalwartClient.cs`, `IStalwartClient.cs`
- **Changes**:
  - Add `LiveDeliveryAsync(target, timeout?)` using `GET /api/live/delivery/{target}`
  - Add `LiveTracingAsync()` using `GET /api/live/tracing`
  - Add `LiveMetricsAsync()` using `GET /api/live/metrics`
  - Handle Server-Sent Events (SSE) streaming
- **Estimated Scope**: Medium (2 files, ~80 lines)

### Phase 4: Update API Models

**Task 4.1**: Update `StalwartApiModels.cs` to match OpenAPI schemas
- **File**: `StalwartApiModels.cs`
- **Changes**:
  - Add `LoginRequest`, `LoginRequestAuthCode`, `LoginRequestAuthDevice`
  - Add `LoginResponse`, `LoginResponseAuthenticated`, `LoginResponseVerified`, `LoginResponseMfaRequired`, `LoginResponseFailure`
  - Add `Account` model matching `/api/account` response
  - Add `ProblemDetails` for RFC 7807 error responses
  - Update existing models to match spec where applicable
- **Estimated Scope**: Large (1 file, ~200 lines)

**Task 4.2**: Add new response models
- **File**: `StalwartApiModels.cs`
- **Changes**:
  - OIDC discovery document model
  - Schema response model
  - Token response models
  - SSE event models
- **Estimated Scope**: Medium (1 file, ~100 lines)

### Phase 5: Update Interface

**Task 5.1**: Update `IStalwartClient.cs`
- **File**: `IStalwartClient.cs`
- **Changes**:
  - Add new method signatures for all new endpoints
  - Update existing method signatures if paths change
- **Estimated Scope**: Small (1 file, ~50 lines)

### Phase 6: Update AccountManager

**Task 6.1**: Update `AccountManager.cs` to use new client methods
- **File**: `AccountManager.cs`
- **Changes**:
  - Update all method calls to use new client API
  - Handle new authentication flow
  - Update error handling for new response types
- **Estimated Scope**: Medium (1 file, ~100 lines)

### Phase 7: Fix Constructor Bug

**Task 7.1**: Fix `HttpClientHandler` configuration
- **File**: `StalwartClient.cs` (line 57-59)
- **Issue**: `MaxAutomaticRedirections = 0` throws `ArgumentOutOfRangeException`
- **Fix**: Remove this line or set to valid value (default is 50)
- **Estimated Scope**: Tiny (1 line)

### Phase 8: Update Tests

**Task 8.1**: Fix unit tests
- **File**: `StalwartClientTests.cs`
- **Changes**:
  - Unskip tests that were blocked by constructor bug
  - Add tests for new endpoints
  - Update test URLs to match new paths
- **Estimated Scope**: Medium (1 file, ~150 lines)

---

## Dependency Graph

```
Phase 1: Authentication (Blocked by nothing)
├── Task 1.1: Update authentication
└── Task 1.2: Add /api/account endpoint

Phase 2: Path Fixes (Depends on Phase 1 research)
├── Task 2.1: Domain endpoints
├── Task 2.2: Account endpoints  
├── Task 2.3: Alias endpoints
└── Task 2.4: Health check

Phase 3: New Endpoints (Depends on Phase 1)
├── Task 3.1: OIDC discovery
├── Task 3.2: Schema endpoints
├── Task 3.3: Token endpoints
└── Task 3.4: Live telemetry (SSE)

Phase 4: Models (Can start after Phase 1)
├── Task 4.1: Update to match spec
└── Task 4.2: Add new models

Phase 5: Interface (Depends on Phases 1-4)
└── Task 5.1: Update IStalwartClient

Phase 6: AccountManager (Depends on Phase 5)
└── Task 6.1: Update to use new interface

Phase 7: Bug Fix (Independent)
└── Task 7.1: Fix constructor

Phase 8: Tests (Depends on all above)
└── Task 8.1: Update tests
```

**Recommended Order**:
1. Phase 7 (fix constructor bug - unblocks tests)
2. Phase 1 (authentication)
3. Phase 4 (models) - can run in parallel with Phase 1
4. Phase 5 (interface)
5. Phase 2 (path fixes) - requires research from Phase 1
6. Phase 3 (new endpoints)
7. Phase 6 (AccountManager)
8. Phase 8 (tests)

---

## Checkpoints

### Checkpoint 1: After Phase 7 and 1
- [ ] Constructor bug fixed
- [ ] Authentication flow updated
- [ ] `dotnet build` succeeds
- [ ] Basic auth tests pass
- **Review**: Verify authentication works with real Stalwart server

### Checkpoint 2: After Phase 4 and 5
- [ ] All models updated
- [ ] Interface updated
- [ ] `dotnet build` succeeds
- **Review**: Verify model serialization/deserialization

### Checkpoint 3: After Phase 2, 3, 6
- [ ] All endpoints updated
- [ ] AccountManager updated
- [ ] `dotnet build` succeeds
- **Review**: Verify all API paths are correct

### Checkpoint 4: After Phase 8
- [ ] All tests pass
- [ ] Integration tests with mock server pass
- **Review**: Ready for production use

---

## File Change Summary

| File | Tasks | Changes |
|------|-------|---------|
| `StalwartClient.cs` | 1.1, 1.2, 2.1-2.4, 3.1-3.4, 7.1 | ~400 lines changed |
| `StalwartApiModels.cs` | 4.1, 4.2 | ~300 lines added |
| `IStalwartClient.cs` | 5.1 | ~50 lines added |
| `AccountManager.cs` | 6.1 | ~100 lines changed |
| `StalwartClientTests.cs` | 8.1 | ~150 lines changed |

**Total Estimated Changes**: ~1000 lines across 5 files

---

## Open Questions

1. **Domain/Account/Alias CRUD**: Where do these endpoints actually exist?
   - Option A: At `/api/domains*`, `/api/accounts*`, `/api/aliases*` (without v1)
   - Option B: In JMAP API at `/jmap/`
   - Option C: In a separate Admin API
   - **Action**: Test against a real Stalwart server or check Vandelay source

2. **JMAP Integration**: Should we implement JMAP client for domain/account management?
   - Per SPEC.md, Vandelay handles data migration via JMAP
   - But our tool needs to create domains/accounts first
   - **Action**: Research JMAP Admin API or use existing endpoints

3. **API Version**: Is there a v1 vs non-v1 difference?
   - OpenAPI spec is for `/api/*` (no version)
   - Current client uses `/api/v1/*`
   - **Action**: Verify which version the target Stalwart server uses

4. **SSE Support**: Do we need full SSE client implementation?
   - Live endpoints use Server-Sent Events
   - Might need `System.Net.Http.Sse` or custom implementation
   - **Action**: Add as optional feature, can be stubbed initially

---

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| API endpoints don't exist at new paths | Critical | Test against real Stalwart server before full implementation |
| JMAP integration more complex than expected | High | Prioritize getting basic auth working first, defer JMAP |
| SSE implementation has edge cases | Medium | Use well-tested library for SSE handling |
| Breaking changes to interface | Medium | Document breaking changes, update all callers |
| Tests require mock server | Medium | Use HttpClient mocking, don't require real server for unit tests |

---

## Success Criteria

- [ ] All API endpoints use paths matching OpenAPI spec
- [ ] Authentication flow matches OpenAPI spec
- [ ] `dotnet build` succeeds without errors
- [ ] All unit tests pass
- [ ] Integration tests pass with mock server
- [ ] Manual testing confirms endpoints work with real Stalwart server
- [ ] No breaking changes to public API (or documented if necessary)

---

## Next Steps

1. **Verify API paths**: Test against a real Stalwart server to confirm which endpoints exist
2. **Research JMAP**: Understand how Vandelay creates domains/accounts
3. **Implement Phase 7**: Fix constructor bug (quick win)
4. **Implement Phase 1**: Update authentication (highest priority)
5. **Review with team**: Present findings and get alignment on approach

---

*Plan created: 2026-07-10*  
*Status: Ready for review*  
*Priority: CRITICAL (all API operations currently failing)*
