# Task List: Stalwart Email Server Setup for Integration Testing

**Project**: StalwartMigration.Integration.Tests  
**Objective**: Set up a Stalwart Mail Server instance for integration testing  
**Created**: 2026-07-12  
**Status**: Ready for implementation

---

## 📋 Overview

This task list implements the plan described in `stalwart-test-server-plan.md`. It breaks down the work into manageable, verifiable tasks across 5 phases.

---

## 🎯 Current Status

- [x] **Phase 1**: Infrastructure Setup (3/3 tasks) — done 2026-07-12
- [ ] **Phase 2 (revised)**: Real API Integration Tests, lean (0/2 tasks) — see below, replaces original Phase 2+3
- [~] **Phase 3 (original)**: Superseded by revised Phase 2 — original Task 6/7 split no longer applies
- [ ] **Phase 4**: Migration Workflow Tests — Backlog, not started
- [ ] **Phase 5**: Test Infrastructure Polish — Backlog, mostly deferred (see notes)

**Total**: 3/10 tasks completed under the original breakdown; see "Revised Plan" below for what's actually being tracked going forward.

---

## 🔄 Revised Plan (2026-07-12)

The original Phase 2/3/5 tasks (T4, T5, T9, T10) called for dedicated `TestDataInitializer`, `StalwartApiHelper`, `TestCredentialsManager`, `CredentialGenerator`, `ParallelStalwartFixture`, and `TestLogger`/`DiagnosticCollector` classes. That's more infrastructure than a first batch of real tests needs. See `stalwart-test-server-plan.md` → "Status Update (2026-07-12)" for the full rationale. Short version:

- Reuse the one shared `StalwartTestFixture` container for all new tests (already built, ~20s startup for the whole run) — don't spin up per-class instances.
- Configure the server for each test via the CRUD methods `StalwartClient`/`AccountManager` already expose (`CreateDomainAsync`, `CreateAccountAsync`, `CreateAliasAsync`, etc.) — no new abstraction layer.
- Isolate tests with unique per-test domain names (e.g. `{Guid:N}.test.invalid`) and clean up what each test creates (deleting a domain cascades its accounts/aliases) — no per-test containers.
- Original T4/T5/T6/T7 are replaced by the two tasks below. T8 (E2E migration), T9 (parallel instances), T10 (dedicated logging) remain backlog items, not actively planned, until the lean approach shows a concrete gap.

---

## 📦 Phase 1: Infrastructure Setup

### Task 1: Create Docker Test Infrastructure
**ID**: T1  
**Priority**: High  
**Status**: ✅ Done (2026-07-12)  
**Estimated**: 2-3 days  
**Assignee**: (unassigned)

**Description**: Set up Docker-based infrastructure for running Stalwart in tests.

**Acceptance Criteria**:
- [x] `DockerHelper` class to manage container lifecycle (start/stop/cleanup)
- [x] Method to retrieve bootstrap credentials from container logs
- [x] Health check endpoint verification — real `authCode` login against `/api/auth`, not `/api/health` (Stalwart v0.16 doesn't expose that path; an empty-body probe always returned 400)
- [x] Automatic cleanup on test teardown, including when container startup itself fails (xUnit skips `DisposeAsync` when `InitializeAsync` throws, so cleanup happens in a catch block)

**Verification Checklist**:
- [x] Can start Stalwart container programmatically
- [x] Can retrieve admin credentials from container logs
- [x] Can verify API is accessible at `http://localhost:{random port}`
- [x] Container is properly cleaned up after tests
- [x] Code compiles without errors

**Dependencies**: None  

**Files to Create/Modify**:
- `tests/StalwartMigration.Integration.Tests/Infrastructure/DockerHelper.cs`
- `tests/StalwartMigration.Integration.Tests/Infrastructure/StalwartTestFixture.cs`

**Notes**: Consider using TestContainers library as an alternative to custom DockerHelper

---

### Task 2: Create Test Configuration
**ID**: T2  
**Priority**: High  
**Status**: ✅ Done (2026-07-12)  
**Estimated**: 1 day  
**Assignee**: (unassigned)

**Description**: Create configuration files and models specifically for integration testing.

**Acceptance Criteria**:
- [x] Test-specific Stalwart configuration file (`appsettings.test.json`)
- [x] Test admin credentials management — handled by `DockerHelper`/`StalwartTestFixture` directly (random password per run via `STALWART_RECOVERY_ADMIN`); no separate class needed since the container isn't shared across processes
- [x] Docker container configuration (ports, volumes) — random host port, per-container volume names so parallel test classes don't share RocksDB data
- [x] Environment variable configuration for testing

**Verification Checklist**:
- [x] Configuration file is valid JSON
- [x] Configuration can be loaded by test fixtures
- [x] All required ports are properly mapped
- [ ] Configuration supports CI environment — not yet verified in an actual CI runner

**Dependencies**: None  

**Files to Create/Modify**:
- `tests/StalwartMigration.Integration.Tests/appsettings.test.json`
- `tests/StalwartMigration.Integration.Tests/docker-compose.test.yml`
- `tests/StalwartMigration.Integration.Tests/Configuration/TestStalwartConfig.cs`

**Notes**: Ensure no hardcoded credentials in configuration files

---

### Task 3: Setup Test Fixture Base Class
**ID**: T3  
**Priority**: High  
**Status**: ✅ Done (2026-07-12)  
**Estimated**: 1-2 days  
**Assignee**: (unassigned)

**Description**: Create xUnit fixture classes for integration test setup and teardown.

**Acceptance Criteria**:
- [x] `IClassFixture<T>` implementation for shared Stalwart instance
- [x] `IAsyncLifetime`/`IDisposable` implementation for cleanup
- [x] Container startup happens in `InitializeAsync`, one instance per test class run
- [x] Fixed the double-init bug where `StalwartTestFixtureTests` re-ran `_fixture.InitializeAsync()` per test on top of xUnit's own class-fixture call

**Verification Checklist**:
- [x] Multiple test classes can share the same fixture (`IClassFixture<StalwartTestFixture>`)
- [x] Cleanup works correctly even on test failures (cleanup added to the `InitializeAsync` catch path)
- [x] Fixture handles container startup timeouts (120s default, configurable)
- Note: "Lazy initialization" and "`[ClassData]`" from the original criteria don't apply to xUnit's `IClassFixture` model as actually implemented — dropped as not meaningful here.

**Dependencies**: T1 (DockerHelper)  

**Files to Create/Modify**:
- `tests/StalwartMigration.Integration.Tests/Fixtures/StalwartTestFixture.cs`
- `tests/StalwartMigration.Integration.Tests/Fixtures/TestDatabaseFixture.cs`

**Notes**: Consider implementing both shared and isolated fixture patterns

---

## ✅ Checkpoint: Infrastructure Setup

**Verification**:
- [x] All Docker infrastructure code compiles
- [x] Container can be started and stopped programmatically
- [x] Admin credentials can be retrieved
- [x] API health check passes (via real login, not a `/api/health` path that doesn't exist in v0.16)
- [x] Cleanup removes all containers and volumes

**Blockers**: None. Verified with a full `dotnet test` run of the solution: 7/7 integration tests, 276/276 unit tests, 39/39 CLI tests, no leaked Docker state.

---

## 🗃️ Phase 2 (revised): Real API Integration Tests, lean

This replaces the original Phase 2 (T4/T5) and Phase 3 (T6/T7) with two tasks scoped to the revised strategy in `stalwart-test-server-plan.md`: reuse the shared fixture, configure the server via the CRUD methods that already exist, isolate by naming + per-test cleanup rather than by container.

### Task 4 (revised): StalwartClient CRUD integration tests
**ID**: T4  
**Priority**: High  
**Status**: Pending  
**Estimated**: 0.5-1 day  
**Assignee**: (unassigned)

**Description**: Add `Integration/StalwartClientIntegrationTests.cs`, using the existing shared `StalwartTestFixture` (`IClassFixture<StalwartTestFixture>`). Each test configures only what it needs directly through `StalwartClient` — no new helper/initializer classes.

**Acceptance Criteria**:
- [ ] Domain round-trip: `CreateDomainAsync` → `GetDomainAsync`/`GetDomainByNameAsync` → `UpdateDomainAsync` → `DeleteDomainAsync`, each test using a unique domain name (e.g. `{Guid:N}.test.invalid`)
- [ ] Account round-trip: create a domain, then `CreateAccountAsync` → `GetAccountAsync`/`GetAccountByEmailAsync` → `UpdateAccountAsync` → `DeleteAccountAsync`
- [ ] Alias round-trip: `CreateAliasAsync` → `GetAliasAsync` → `UpdateAliasAsync` → `DeleteAliasAsync`
- [ ] One realistic error case per resource (e.g. `GetDomainAsync` with an unknown id → 404-mapped exception)
- [ ] Every test cleans up what it created (delete the domain it made; deleting a domain cascades its accounts/aliases) so the shared container doesn't accumulate state across the run

**Verification Checklist**:
- [ ] Tests pass consistently against the shared fixture container
- [ ] No test depends on ordering or on another test's leftover state
- [ ] Adds no more than ~15-20s beyond the existing ~20s fixture startup (no extra containers spun up)

**Dependencies**: T1, T3 (both done)

**Files to Create/Modify**:
- `tests/StalwartMigration.Integration.Tests/Integration/StalwartClientIntegrationTests.cs`

**Notes**: Supersedes original T6. No `TestDataInitializer`/`StalwartApiHelper` — call `StalwartClient` methods directly from the test body; only extract a shared helper if duplication actually shows up across 3+ tests.

---

### Task 5 (revised): AccountManager smoke test
**ID**: T5  
**Priority**: Medium  
**Status**: Pending  
**Estimated**: 0.5 day  
**Assignee**: (unassigned)

**Description**: Add a small `Integration/AccountManagerIntegrationTests.cs` covering the parts of `AccountManager`'s logic that add value over calling `StalwartClient` directly — `DomainExistsAsync`/`AccountExistsAsync`, and one of the batch helpers (`CreateAccountsAsync` or `CreateDomainsAsync`). Reuses the same shared fixture; not a parallel CRUD matrix — that's already covered by T4 since `AccountManager`'s CRUD methods are thin wrappers over `StalwartClient`.

**Acceptance Criteria**:
- [ ] `DomainExistsAsync`/`AccountExistsAsync` return correct results against real server state
- [ ] One batch-creation method works end-to-end (e.g. `CreateAccountsAsync` for 2-3 accounts) and cleans up after itself
- [ ] Test reuses the domain/account naming and cleanup convention from T4

**Verification Checklist**:
- [ ] Tests pass against the shared fixture
- [ ] No new Stalwart container spun up for this file

**Dependencies**: T4

**Files to Create/Modify**:
- `tests/StalwartMigration.Integration.Tests/Integration/AccountManagerIntegrationTests.cs`

**Notes**: Supersedes original T7. Original T5 (`TestCredentialsManager`/`CredentialGenerator`) is dropped — there's one shared container per run with a randomly generated admin password already; credential rotation has no reason to exist until tests run against a persistent, non-ephemeral server.

---

## ✅ Checkpoint: Real API Integration Tests

**Verification**:
- [ ] StalwartClient CRUD integration tests pass (T4)
- [ ] AccountManager smoke test passes (T5)
- [ ] Total integration suite runtime stays in the tens-of-seconds range
- [ ] No leaked Stalwart state between test runs (verify via `docker volume ls` / a fresh domain list after a full run)

---

## 🚀 Phase 4: Migration Workflow Tests (Backlog — deferred until T4/T5 land)

### Task 8: Create End-to-End Migration Test
**ID**: T8  
**Priority**: Medium — Backlog  
**Status**: Deferred, not planned  
**Estimated**: 3-4 days  
**Assignee**: (unassigned)

**Description**: Create a test that simulates a complete migration workflow against the test Stalwart server.

**Acceptance Criteria**:
- [ ] Test setup phase (domain/account creation)
- [ ] Test data import phase (if API-based import is implemented)
- [ ] Test validation phase
- [ ] Test cleanup phase

**Verification Checklist**:
- [ ] Complete migration workflow runs without errors
- [ ] Data integrity is maintained
- [ ] Validation passes
- [ ] Cleanup removes all test artifacts

**Dependencies**: T6, T7  

**Files to Create/Modify**:
- `tests/StalwartMigration.Integration.Tests/EndToEnd/MigrationWorkflowTests.cs`

**Notes**: This is the main integration test that validates the entire migration tool

---

## ✅ Checkpoint: Migration Workflow Tests

**Verification**:
- [ ] End-to-end migration test passes
- [ ] All phases complete successfully
- [ ] Data validation works correctly

**Blockers**: May depend on implementation of import functionality in main codebase

---

## 🎨 Phase 5: Test Infrastructure Polish (Backlog — deferred)

Both tasks below are explicitly **not** planned for near-term work. Revisit only if the lean Phase 2 approach (one shared container, T4/T5) proves insufficient — e.g. suite runtime grows past what's acceptable, or debugging integration failures without dedicated log capture becomes a recurring problem.

### Task 9: Add Parallel Test Support
**ID**: T9  
**Priority**: Low — Backlog  
**Status**: Deferred, not planned  
**Estimated**: 1-2 days  
**Assignee**: (unassigned)

**Description**: Enable parallel test execution with isolated Stalwart instances.

**Why deferred**: The whole integration suite already shares one container per run (~20s startup total). Per-class isolated instances only pay off once test count/runtime grows enough that the shared container becomes a bottleneck or a source of cross-test contention — neither is true yet with T4/T5's scope.

**Acceptance Criteria**:
- [ ] Each test class gets its own Stalwart instance
- [ ] Parallel test execution works without conflicts
- [ ] Resource cleanup handles parallel scenarios
- [ ] Test isolation is maintained

**Verification Checklist**:
- [ ] Tests can run in parallel
- [ ] No cross-test pollution
- [ ] All tests pass when run in parallel
- [ ] Performance is acceptable

**Dependencies**: T3  

**Files to Create/Modify**:
- `tests/StalwartMigration.Integration.Tests/Fixtures/ParallelStalwartFixture.cs`
- `tests/StalwartMigration.Integration.Tests/Attributes/IsolatedStalwartAttribute.cs`

**Notes**: Consider using xUnit's `[Collection]` feature for parallelization control

---

### Task 10: Add Test Logging and Diagnostics
**ID**: T10  
**Priority**: Low — Backlog  
**Status**: Deferred, not planned  
**Estimated**: 1 day  
**Assignee**: (unassigned)

**Description**: Implement comprehensive logging for integration tests to aid debugging.

**Why deferred**: `StalwartClient` already logs via `ILogger`, and xUnit surfaces exceptions/stack traces per test. Add dedicated `TestLogger`/`DiagnosticCollector` classes only once real T4/T5 failures show that's not enough to diagnose them.

**Acceptance Criteria**:
- [ ] Test execution logging
- [ ] Stalwart server log capture
- [ ] API request/response logging (sanitized)
- [ ] Test failure diagnostics

**Verification Checklist**:
- [ ] Logs are captured and accessible
- [ ] No sensitive data in logs
- [ ] Failures include diagnostic information
- [ ] Logs are useful for debugging

**Dependencies**: None  

**Files to Create/Modify**:
- `tests/StalwartMigration.Integration.Tests/Infrastructure/TestLogger.cs`
- `tests/StalwartMigration.Integration.Tests/Infrastructure/DiagnosticCollector.cs`

**Notes**: Ensure sensitive data (passwords, tokens) is never logged

---

## ✅ Checkpoint: Test Infrastructure Polish

**Verification**:
- [ ] Parallel tests work correctly
- [ ] Logging provides useful diagnostic information
- [ ] No sensitive data in logs
- [ ] All infrastructure tests pass

**Blockers**: None identified

---

## 📊 Progress Tracking

### Task Status Summary

| Task | ID | Priority | Status | Estimate | Assignee |
|------|----|----------|--------|----------|----------|
| Create Docker Test Infrastructure | T1 | High | ✅ Done | 2-3 days | - |
| Create Test Configuration | T2 | High | ✅ Done | 1 day | - |
| Setup Test Fixture Base Class | T3 | High | ✅ Done | 1-2 days | - |
| StalwartClient CRUD integration tests (revised) | T4 | High | Pending | 0.5-1 day | - |
| AccountManager smoke test (revised) | T5 | Medium | Pending | 0.5 day | - |
| ~~Test StalwartClient against Real Server~~ | T6 | High | Superseded by T4 | - | - |
| ~~Test AccountManager Integration~~ | T7 | High | Superseded by T5 | - | - |
| Create End-to-End Migration Test | T8 | Medium | Backlog | 3-4 days | - |
| Add Parallel Test Support | T9 | Low | Backlog | 1-2 days | - |
| Add Test Logging and Diagnostics | T10 | Low | Backlog | 1 day | - |

### Phase Completion

- **Phase 1: Infrastructure Setup**: 100% (3/3 tasks) ✅
- **Phase 2 (revised): Real API Integration Tests**: 0% (0/2 tasks) — next up
- **Phase 4: Migration Workflow Tests**: Backlog, not scheduled
- **Phase 5: Test Infrastructure Polish**: Backlog, not scheduled

**Overall Completion**: Phase 1 done; Phase 2 (revised) is the active next milestone.

### Milestones

- [x] **Milestone 1**: Infrastructure Setup Complete (Phase 1) — 2026-07-12
- [ ] **Milestone 2**: Real API Integration Tests Complete (Phase 2, revised — T4/T5)
- [ ] **Milestone 3 (backlog)**: Migration Workflow Test (Phase 4)
- [ ] **Milestone 4 (backlog)**: Test Infrastructure Polish (Phase 5)

---

## 🔍 Open Questions

1. **TestContainers vs Custom Implementation**: Should we use the TestContainers library or build our own Docker helper?
   - **Pros of TestContainers**: Well-tested, community-supported, feature-rich
   - **Pros of Custom**: More control, no external dependencies, tailored to our needs
   - **Decision Needed**: Before starting T1

2. **Stalwart Version**: Which version of Stalwart should we target?
   - **Recommendation**: v0.16 (current stable as of July 2026)
   - **Action**: Pin to specific version in Docker configuration

3. **Storage Backend for Tests**: Should we use in-memory or persistent storage?
   - **Recommendation**: Persistent volumes with cleanup for reliability
   - **Alternative**: In-memory storage (RocksDB) for faster tests

4. **CI Integration**: How to handle Docker in CI?
   - **Recommendation**: Use GitHub Actions with Docker support
   - **Fallback**: Mock tests for environments without Docker

5. **Configuration Management**: How to manage test configuration?
   - **Recommendation**: Environment variables + appsettings.test.json
   - **Alternative**: Use xUnit's `IConfiguration` interface

---

## 🚨 Risks and Mitigations

| Risk | Probability | Impact | Status | Mitigation |
|------|-------------|--------|--------|------------|
| Docker not available on CI | Medium | High | Open | Use GitHub Actions; provide mock fallback |
| Stalwart API changes | Low | Medium | Open | Pin to specific version; monitor releases |
| Port conflicts in tests | Medium | Medium | Open | Use random host ports; cleanup properly |
| Slow test execution | High | Medium | Open | Optimize container startup; parallelize |
| Resource exhaustion | Medium | Medium | Open | Limit container resources; cleanup |
| Test flakiness | Medium | High | Open | Implement retry logic; good logging |

---

## 📝 Notes

### Getting Started

To begin implementation:
1. Review and resolve open questions
2. Start with **Task 1: Create Docker Test Infrastructure** (T1)
3. Then implement **Task 2: Create Test Configuration** (T2)
4. Then implement **Task 3: Setup Test Fixture Base Class** (T3)
5. Verify Checkpoint: Infrastructure Setup
6. Continue with Phase 2 tasks

### Docker Commands Reference

```bash
# Start Stalwart container for testing
docker run -d --name stalwart-test \
  -p 8080:8080 \
  -v stalwart-test-etc:/etc/stalwart \
  -v stalwart-test-data:/var/lib/stalwart \
  -e STALWART_RECOVERY_ADMIN=admin:testpass123 \
  stalwartlabs/stalwart:v0.16

# Get admin credentials
docker logs stalwart-test 2>&1 | grep -A8 'bootstrap mode'

# Stop and remove container
docker stop stalwart-test
docker rm stalwart-test
docker volume rm stalwart-test-etc stalwart-test-data

# Check API health
curl -f http://localhost:8080/api/health
```

### Useful Resources

- [Stalwart Docker Documentation](https://stalw.art/docs/install/platform/docker)
- [Stalwart API Documentation](https://github.com/stalwartlabs/stalwart/blob/main/api/v1/openapi.yml)
- [TestContainers for .NET](https://dotnet.testcontainers.org/)
- [xUnit Documentation](https://xunit.net/)
- [Docker .NET SDK](https://github.com/docker/dotnet-sdk)

---

## 🏷️ Tags

- `infrastructure` (T1, T2, T3, T9, T10)
- `api` (T4, T5 — revised; supersede original T6, T7)
- `migration` (T8)
- `configuration` (T2, T5)
- `testing` (all)

---

*Last updated: 2026-07-12*
