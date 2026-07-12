# Implementation Plan: Stalwart Email Server Setup for Integration Testing

## Overview

This plan outlines the setup of a Stalwart Mail Server (https://stalw.art/) instance for integration test purposes. The server will be used to test the StalwartMigration tool's ability to migrate data from hMailServer to Stalwart.

Stalwart is an all-in-one mail and collaboration server that supports JMAP, IMAP, POP3, SMTP, WebDAV, CalDAV, and CardDAV protocols. For integration testing, we need a lightweight, isolated instance that can be started/stopped on demand.

## Context

The existing project (`StalwartMigration.Integration.Tests`) currently has an empty directory, indicating that integration tests have not been fully set up yet. The project already has:
- A `StalwartClient` class that communicates with Stalwart's REST API v1
- Configuration models for Stalwart API credentials
- Unit tests for the client (constructor, authentication, etc.)
- Docker-based infrastructure knowledge (from SPEC.md)

## Status Update (2026-07-12): Phase 1 done, Phase 2+ strategy revised

Phase 1 (DockerHelper, StalwartTestFixture, test configuration) is implemented and green: one shared Stalwart container boots for the whole integration test run in ~20s, and the health check performs a real `authCode` login rather than probing with an empty body. 7/7 infrastructure sanity tests pass reliably with no leaked containers or volumes.

The rest of this plan (Phases 2-5, below) was written before that container existed and over-specifies the follow-on work — it asks for dedicated `TestDataInitializer`, `StalwartApiHelper`, `TestCredentialsManager`, `CredentialGenerator`, per-class `ParallelStalwartFixture`, and `TestLogger`/`DiagnosticCollector` classes before a single real test has exercised the server. Revised direction, in priority order:

1. **Leverage the existing shared fixture, don't multiply containers.** `StalwartTestFixture` already starts one Stalwart instance per test run via `IClassFixture`. New tests should reuse `fixture.StalwartClient` directly rather than spinning up per-class or per-test instances — that's what keeps total runtime in the tens-of-seconds range instead of minutes.
2. **Configure the server through the CRUD surface that already exists**, not a new abstraction. `StalwartClient` and `AccountManager` both already expose full `Create/Get/Update/Delete` for domains, accounts, and aliases (`StalwartClient.cs:186-360`, `AccountManager.cs:60-426`). A test that needs a domain and account calls those methods directly; a dedicated "TestDataInitializer" layer is unwarranted until real duplication shows up across more than a couple of test files.
3. **Isolate tests by naming and per-test cleanup, not by container.** Since the server is shared and long-lived, each test creates its own uniquely-named domain (e.g. `{Guid:N}.test.invalid`) and deletes what it created in a `finally` or `IAsyncLifetime.DisposeAsync`, so tests can't collide and the server doesn't accumulate state across a run. Deleting a domain cascades its accounts/aliases in Stalwart, so cleanup is typically a single call.
4. **Keep footprint proportional to what actually needs verifying.** Target CRUD round-trip smoke coverage (create → read back → update → delete, plus one realistic error case per resource type) rather than an exhaustive matrix. This is what makes "limited footprint, limited run time" achievable — a dozen or so short-lived, self-cleaning tests against the one shared container.
5. **Defer, don't build yet:** per-class isolated Stalwart instances (original Task 9), dedicated logging/diagnostics classes beyond what `ILogger` and xUnit output already give us (original Task 10), the full end-to-end migration workflow test (original Task 8, Phase 4), SSE/live-telemetry coverage, and credential rotation/`TestCredentialsManager` (a single shared container for the run doesn't need rotating credentials). Revisit any of these only if the lean approach proves insufficient — e.g., if runtime balloons or tests start fighting over server state.

The task list below is left in place for historical context but Phases 2-5 acceptance criteria should be read through the lens above; concrete near-term tasks are tracked in `stalwart-test-server-todo.md`.

## Architecture Decisions

### 1. Containerized Deployment
**Decision**: Use Docker to run Stalwart Mail Server
**Rationale**: 
- Provides isolated, reproducible test environments
- Easy to start/stop for test suites
- Matches the project's existing Docker-based approach
- Official images available from `stalwartlabs/stalwart`

### 2. Test-Specific Configuration
**Decision**: Create a dedicated test configuration with pre-configured domains and accounts
**Rationale**:
- Tests need known initial state
- Avoids dependency on external DNS or certificates
- Can be reset between test runs

### 3. Ephemeral Containers
**Decision**: Use Docker containers that can be spun up/down per test session
**Rationale**:
- Ensures clean state for each test
- Prevents test pollution
- Can be integrated with xUnit's fixture lifecycle

### 4. API-First Integration
**Decision**: Focus on REST API integration (port 8080) rather than full protocol testing
**Rationale**:
- The migration tool primarily uses Stalwart's REST API
- Simpler to test than full SMTP/IMAP protocol stacks
- Matches existing `StalwartClient` implementation

### 5. Internal Directory
**Decision**: Use Stalwart's internal directory for authentication
**Rationale**:
- Simplest setup for testing
- No external dependencies (LDAP, OIDC, SQL)
- Easy to provision test accounts programmatically

## Dependency Graph

```
Docker Infrastructure
    │
    ├── Docker Engine (prerequisite)
    │
    ├── Stalwart Docker Image (stalwartlabs/stalwart:v0.16)
    │       │
    │       ├── Configuration Files
    │       │   ├── /etc/stalwart (config volume)
    │       │   └── /var/lib/stalwart (data volume)
    │       │
    │       └── Container Lifecycle Management
    │               │
    │               ├── Test Fixtures (xUnit)
    │               │       │
    │               │       ├── DockerHelper class
    │               │       └── StalwartTestFixture
    │               │
    │               └── Integration Test Classes
    │                       │
    │                       ├── AccountManagerTests
    │                       ├── DomainTests
    │                       └── MigrationTests
    │
    └── Test Configuration
            │
            ├── test-stalwart-config.json
            ├── docker-compose.test.yml
            └── TestContainers configuration
```

## Task List

### Phase 1: Infrastructure Setup

#### Task 1: Create Docker Test Infrastructure
**Description**: Set up Docker-based infrastructure for running Stalwart in tests.

**Acceptance criteria:**
- [ ] DockerHelper class to manage container lifecycle (start/stop/cleanup)
- [ ] Method to retrieve bootstrap credentials from container logs
- [ ] Health check endpoint verification
- [ ] Automatic cleanup on test teardown

**Verification:**
- [ ] Can start Stalwart container programmatically
- [ ] Can retrieve admin credentials
- [ ] Can verify API is accessible at http://localhost:8080
- [ ] Container is properly cleaned up after tests

**Dependencies:** None

**Files likely touched:**
- `tests/StalwartMigration.Integration.Tests/Infrastructure/DockerHelper.cs`
- `tests/StalwartMigration.Integration.Tests/Infrastructure/StalwartTestFixture.cs`

**Estimated scope:** Medium (3-5 files)

---

#### Task 2: Create Test Configuration
**Description**: Create configuration files and models specifically for integration testing.

**Acceptance criteria:**
- [ ] Test-specific Stalwart configuration file
- [ ] Test admin credentials management
- [ ] Docker container configuration (ports, volumes)
- [ ] Environment variable configuration for testing

**Verification:**
- [ ] Configuration file is valid JSON
- [ ] Configuration can be loaded by test fixtures
- [ ] All required ports are properly mapped

**Dependencies:** None

**Files likely touched:**
- `tests/StalwartMigration.Integration.Tests/appsettings.test.json`
- `tests/StalwartMigration.Integration.Tests/docker-compose.test.yml`
- `tests/StalwartMigration.Integration.Tests/Configuration/TestStalwartConfig.cs`

**Estimated scope:** Small (1-2 files)

---

#### Task 3: Setup Test Fixture Base Class
**Description**: Create xUnit fixture classes for integration test setup and teardown.

**Acceptance criteria:**
- [ ] IClassFixture implementation for shared Stalwart instance
- [ ] IDisposable implementation for cleanup
- [ ] Lazy initialization of Stalwart container
- [ ] Thread-safe access to shared resources

**Verification:**
- [ ] Fixture can be used with `[ClassData]` attribute
- [ ] Multiple test classes can share the same fixture
- [ ] Cleanup works correctly even on test failures

**Dependencies:** Task 1 (DockerHelper)

**Files likely touched:**
- `tests/StalwartMigration.Integration.Tests/Fixtures/StalwartTestFixture.cs`
- `tests/StalwartMigration.Integration.Tests/Fixtures/TestDatabaseFixture.cs`

**Estimated scope:** Medium (3-5 files)

---

### Checkpoint: Infrastructure Setup
- [ ] All Docker infrastructure code compiles
- [ ] Container can be started and stopped programmatically
- [ ] Admin credentials can be retrieved
- [ ] API health check passes
- [ ] Cleanup removes all containers and volumes

---

### Phase 2: Test Data Setup

#### Task 4: Create Test Data Initialization
**Description**: Implement methods to initialize test data in Stalwart (domains, accounts, aliases).

**Acceptance criteria:**
- [ ] Method to create test domains via API
- [ ] Method to create test accounts with known passwords
- [ ] Method to create email aliases
- [ ] Method to verify created resources
- [ ] Method to clean up test data

**Verification:**
- [ ] Can create a domain and verify it exists via API
- [ ] Can create an account and authenticate with it
- [ ] Can create aliases and verify they work
- [ ] Cleanup removes all test data

**Dependencies:** Task 1, Task 3

**Files likely touched:**
- `tests/StalwartMigration.Integration.Tests/Helpers/TestDataInitializer.cs`
- `tests/StalwartMigration.Integration.Tests/Helpers/StalwartApiHelper.cs`

**Estimated scope:** Medium (3-5 files)

---

#### Task 5: Create Test Credentials Management
**Description**: Securely manage and rotate test credentials for Stalwart API access.

**Acceptance criteria:**
- [ ] Secure storage of test admin credentials
- [ ] Method to generate random passwords for test accounts
- [ ] Credential rotation between test runs
- [ ] No hardcoded credentials in source code

**Verification:**
- [ ] Credentials are not logged or exposed
- [ ] Different test runs use different credentials
- [ ] Credentials are properly disposed

**Dependencies:** Task 1, Task 4

**Files likely touched:**
- `tests/StalwartMigration.Integration.Tests/Security/TestCredentialsManager.cs`
- `tests/StalwartMigration.Integration.Tests/Security/CredentialGenerator.cs`

**Estimated scope:** Small (1-2 files)

---

### Checkpoint: Test Data Setup
- [ ] Test domains can be created and verified
- [ ] Test accounts can be created and authenticated
- [ ] Test data cleanup works correctly
- [ ] No credential leaks in logs

---

### Phase 3: API Integration Tests

#### Task 6: Test StalwartClient against Real Server
**Description**: Create integration tests that exercise the existing StalwartClient against a real Stalwart instance.

**Acceptance criteria:**
- [ ] Authentication tests with real credentials
- [ ] Domain CRUD operation tests
- [ ] Account CRUD operation tests
- [ ] Alias CRUD operation tests
- [ ] Error handling tests (invalid credentials, not found, etc.)

**Verification:**
- [ ] All StalwartClient methods work against real server
- [ ] Authentication succeeds with valid credentials
- [ ] CRUD operations return expected results
- [ ] Error cases return appropriate exceptions

**Dependencies:** Task 1, Task 3, Task 4

**Files likely touched:**
- `tests/StalwartMigration.Integration.Tests/Integration/StalwartClientIntegrationTests.cs`

**Estimated scope:** Medium (3-5 files)

---

#### Task 7: Test AccountManager Integration
**Description**: Create integration tests for the AccountManager class against real Stalwart server.

**Acceptance criteria:**
- [ ] Test domain creation through AccountManager
- [ ] Test account creation with metadata
- [ ] Test alias creation and management
- [ ] Test quota and settings configuration

**Verification:**
- [ ] AccountManager methods work against real server
- [ ] Created resources match expected state
- [ ] Error cases handled appropriately

**Dependencies:** Task 6

**Files likely touched:**
- `tests/StalwartMigration.Integration.Tests/Integration/AccountManagerIntegrationTests.cs`

**Estimated scope:** Medium (3-5 files)

---

### Checkpoint: API Integration Tests
- [ ] StalwartClient integration tests pass
- [ ] AccountManager integration tests pass
- [ ] All CRUD operations work correctly
- [ ] Error handling is correct

---

### Phase 4: Migration Workflow Tests

#### Task 8: Create End-to-End Migration Test
**Description**: Create a test that simulates a complete migration workflow against the test Stalwart server.

**Acceptance criteria:**
- [ ] Test setup phase (domain/account creation)
- [ ] Test data import phase (if API-based import is implemented)
- [ ] Test validation phase
- [ ] Test cleanup phase

**Verification:**
- [ ] Complete migration workflow runs without errors
- [ ] Data integrity is maintained
- [ ] Validation passes

**Dependencies:** Task 6, Task 7

**Files likely touched:**
- `tests/StalwartMigration.Integration.Tests/EndToEnd/MigrationWorkflowTests.cs`

**Estimated scope:** Large (5-8 files)

---

### Checkpoint: Migration Workflow Tests
- [ ] End-to-end migration test passes
- [ ] All phases complete successfully
- [ ] Data validation works correctly

---

### Phase 5: Test Infrastructure Polish

#### Task 9: Add Parallel Test Support
**Description**: Enable parallel test execution with isolated Stalwart instances.

**Acceptance criteria:**
- [ ] Each test class gets its own Stalwart instance
- [ ] Parallel test execution works without conflicts
- [ ] Resource cleanup handles parallel scenarios
- [ ] Test isolation is maintained

**Verification:**
- [ ] Tests can run in parallel
- [ ] No cross-test pollution
- [ ] All tests pass when run in parallel

**Dependencies:** Task 3

**Files likely touched:**
- `tests/StalwartMigration.Integration.Tests/Fixtures/ParallelStalwartFixture.cs`
- `tests/StalwartMigration.Integration.Tests/Attributes/IsolatedStalwartAttribute.cs`

**Estimated scope:** Medium (3-5 files)

---

#### Task 10: Add Test Logging and Diagnostics
**Description**: Implement comprehensive logging for integration tests to aid debugging.

**Acceptance criteria:**
- [ ] Test execution logging
- [ ] Stalwart server log capture
- [ ] API request/response logging (sanitized)
- [ ] Test failure diagnostics

**Verification:**
- [ ] Logs are captured and accessible
- [ ] No sensitive data in logs
- [ ] Failures include diagnostic information

**Dependencies:** None

**Files likely touched:**
- `tests/StalwartMigration.Integration.Tests/Infrastructure/TestLogger.cs`
- `tests/StalwartMigration.Integration.Tests/Infrastructure/DiagnosticCollector.cs`

**Estimated scope:** Small (1-2 files)

---

### Checkpoint: Test Infrastructure Polish
- [ ] Parallel tests work correctly
- [ ] Logging provides useful diagnostic information
- [ ] No sensitive data in logs
- [ ] All infrastructure tests pass

---

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Docker not available on CI | High | Use GitHub Actions with Docker support; provide fallback mock tests |
| Stalwart image version changes | Medium | Pin to specific version (v0.16); document version requirements |
| Port conflicts | Medium | Use random host ports; provide configuration overrides |
| Container startup time | Medium | Implement async startup with timeout; provide health checks |
| Resource consumption | Medium | Limit container resources; implement cleanup guarantees |
| Test flakiness | Medium | Implement retry logic for transient failures; comprehensive logging |
| API changes in Stalwart | Low | Use semantic versioning; monitor Stalwart releases |

## Open Questions

1. **Should we use TestContainers library?** - The [TestContainers](https://dotnet.testcontainers.org/) library provides a more robust way to manage containers in .NET tests. Should we use it instead of custom DockerHelper?

2. **What's the minimum Stalwart version to support?** - Should we target v0.16 or a specific patch version?

3. **How to handle persistent data between test runs?** - Should we use in-memory storage for tests, or persistent volumes that are cleaned up?

4. **Should we support non-Docker test environments?** - Should we provide a fallback for environments where Docker is not available?

5. **How to manage test configuration across environments?** - Should we use environment variables, appsettings.json, or a combination?

## Assumptions

1. Docker is available on all test environments (local development, CI/CD)
2. Test environment has sufficient resources to run Stalwart container
3. Stalwart API is stable across v0.16.x patch versions
4. Tests will primarily use the REST API (port 8080) rather than direct protocol access
5. Test data can be created and cleaned up via the REST API
6. The StalwartMigration tool's primary interface is through the REST API

## Configuration Reference

### Required Docker Setup
```bash
# Pull the image
docker pull stalwartlabs/stalwart:v0.16

# Create volumes
docker volume create stalwart-test-etc
docker volume create stalwart-test-data

# Run container
docker run -d --name stalwart-test \
  -p 8080:8080 \
  -v stalwart-test-etc:/etc/stalwart \
  -v stalwart-test-data:/var/lib/stalwart \
  -e STALWART_RECOVERY_ADMIN=admin:testpassword123 \
  stalwartlabs/stalwart:v0.16
```

### Retrieving Bootstrap Credentials
```bash
# Get the admin password from logs
docker logs stalwart-test 2>&1 | grep -A8 'bootstrap mode'
```

### Test Configuration File
```json
{
  "apiUrl": "http://localhost:8080",
  "username": "admin",
  "password": "testpassword123",
  "timeoutSeconds": 60,
  "maxRetries": 5,
  "testMode": true
}
```

## Success Criteria

- [ ] Stalwart container can be started programmatically for tests
- [ ] Test data can be initialized and cleaned up
- [ ] StalwartClient works correctly against real Stalwart instance
- [ ] Integration tests for AccountManager pass
- [ ] End-to-end migration workflow can be tested
- [ ] Tests are isolated and can run in parallel
- [ ] Comprehensive logging for debugging
- [ ] All tests pass in CI environment

## Next Steps

1. Review this plan with stakeholders
2. Resolve open questions
3. Implement Phase 1 tasks (Infrastructure Setup)
4. Verify Phase 1 checkpoint
5. Implement Phase 2 tasks (Test Data Setup)
6. Verify Phase 2 checkpoint
7. Continue with subsequent phases
