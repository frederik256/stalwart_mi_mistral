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

- [ ] **Phase 1**: Infrastructure Setup (0/3 tasks)
- [ ] **Phase 2**: Test Data Setup (0/2 tasks)
- [ ] **Phase 3**: API Integration Tests (0/2 tasks)
- [ ] **Phase 4**: Migration Workflow Tests (0/1 task)
- [ ] **Phase 5**: Test Infrastructure Polish (0/2 tasks)

**Total**: 0/10 tasks completed

---

## 📦 Phase 1: Infrastructure Setup

### Task 1: Create Docker Test Infrastructure
**ID**: T1  
**Priority**: High  
**Status**: Pending  
**Estimated**: 2-3 days  
**Assignee**: (unassigned)

**Description**: Set up Docker-based infrastructure for running Stalwart in tests.

**Acceptance Criteria**:
- [ ] `DockerHelper` class to manage container lifecycle (start/stop/cleanup)
- [ ] Method to retrieve bootstrap credentials from container logs
- [ ] Health check endpoint verification (`/api/health`)
- [ ] Automatic cleanup on test teardown

**Verification Checklist**:
- [ ] Can start Stalwart container programmatically
- [ ] Can retrieve admin credentials from container logs
- [ ] Can verify API is accessible at `http://localhost:8080`
- [ ] Container is properly cleaned up after tests
- [ ] Code compiles without errors

**Dependencies**: None  

**Files to Create/Modify**:
- `tests/StalwartMigration.Integration.Tests/Infrastructure/DockerHelper.cs`
- `tests/StalwartMigration.Integration.Tests/Infrastructure/StalwartTestFixture.cs`

**Notes**: Consider using TestContainers library as an alternative to custom DockerHelper

---

### Task 2: Create Test Configuration
**ID**: T2  
**Priority**: High  
**Status**: Pending  
**Estimated**: 1 day  
**Assignee**: (unassigned)

**Description**: Create configuration files and models specifically for integration testing.

**Acceptance Criteria**:
- [ ] Test-specific Stalwart configuration file (`appsettings.test.json`)
- [ ] Test admin credentials management class
- [ ] Docker container configuration (ports, volumes)
- [ ] Environment variable configuration for testing

**Verification Checklist**:
- [ ] Configuration file is valid JSON
- [ ] Configuration can be loaded by test fixtures
- [ ] All required ports are properly mapped
- [ ] Configuration supports CI environment

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
**Status**: Pending  
**Estimated**: 1-2 days  
**Assignee**: (unassigned)

**Description**: Create xUnit fixture classes for integration test setup and teardown.

**Acceptance Criteria**:
- [ ] `IClassFixture<T>` implementation for shared Stalwart instance
- [ ] `IDisposable` implementation for cleanup
- [ ] Lazy initialization of Stalwart container
- [ ] Thread-safe access to shared resources

**Verification Checklist**:
- [ ] Fixture can be used with `[ClassData]` attribute
- [ ] Multiple test classes can share the same fixture
- [ ] Cleanup works correctly even on test failures
- [ ] Fixture handles container startup timeouts

**Dependencies**: T1 (DockerHelper)  

**Files to Create/Modify**:
- `tests/StalwartMigration.Integration.Tests/Fixtures/StalwartTestFixture.cs`
- `tests/StalwartMigration.Integration.Tests/Fixtures/TestDatabaseFixture.cs`

**Notes**: Consider implementing both shared and isolated fixture patterns

---

## ✅ Checkpoint: Infrastructure Setup

**Verification**:
- [ ] All Docker infrastructure code compiles
- [ ] Container can be started and stopped programmatically
- [ ] Admin credentials can be retrieved
- [ ] API health check passes
- [ ] Cleanup removes all containers and volumes

**Blockers**: None identified

---

## 🗃️ Phase 2: Test Data Setup

### Task 4: Create Test Data Initialization
**ID**: T4  
**Priority**: Medium  
**Status**: Pending  
**Estimated**: 1-2 days  
**Assignee**: (unassigned)

**Description**: Implement methods to initialize test data in Stalwart (domains, accounts, aliases).

**Acceptance Criteria**:
- [ ] Method to create test domains via API
- [ ] Method to create test accounts with known passwords
- [ ] Method to create email aliases
- [ ] Method to verify created resources
- [ ] Method to clean up test data

**Verification Checklist**:
- [ ] Can create a domain and verify it exists via API
- [ ] Can create an account and authenticate with it
- [ ] Can create aliases and verify they work
- [ ] Cleanup removes all test data
- [ ] Methods handle API errors gracefully

**Dependencies**: T1, T3  

**Files to Create/Modify**:
- `tests/StalwartMigration.Integration.Tests/Helpers/TestDataInitializer.cs`
- `tests/StalwartMigration.Integration.Tests/Helpers/StalwartApiHelper.cs`

**Notes**: Use the existing `StalwartClient` for API operations

---

### Task 5: Create Test Credentials Management
**ID**: T5  
**Priority**: Medium  
**Status**: Pending  
**Estimated**: 1 day  
**Assignee**: (unassigned)

**Description**: Securely manage and rotate test credentials for Stalwart API access.

**Acceptance Criteria**:
- [ ] Secure storage of test admin credentials
- [ ] Method to generate random passwords for test accounts
- [ ] Credential rotation between test runs
- [ ] No hardcoded credentials in source code

**Verification Checklist**:
- [ ] Credentials are not logged or exposed
- [ ] Different test runs use different credentials
- [ ] Credentials are properly disposed
- [ ] Password generation meets complexity requirements

**Dependencies**: T1, T4  

**Files to Create/Modify**:
- `tests/StalwartMigration.Integration.Tests/Security/TestCredentialsManager.cs`
- `tests/StalwartMigration.Integration.Tests/Security/CredentialGenerator.cs`

**Notes**: Consider using `SecureString` or similar for credential handling

---

## ✅ Checkpoint: Test Data Setup

**Verification**:
- [ ] Test domains can be created and verified
- [ ] Test accounts can be created and authenticated
- [ ] Test data cleanup works correctly
- [ ] No credential leaks in logs

**Blockers**: None identified

---

## 🔌 Phase 3: API Integration Tests

### Task 6: Test StalwartClient against Real Server
**ID**: T6  
**Priority**: High  
**Status**: Pending  
**Estimated**: 2-3 days  
**Assignee**: (unassigned)

**Description**: Create integration tests that exercise the existing `StalwartClient` against a real Stalwart instance.

**Acceptance Criteria**:
- [ ] Authentication tests with real credentials
- [ ] Domain CRUD operation tests
- [ ] Account CRUD operation tests
- [ ] Alias CRUD operation tests
- [ ] Error handling tests (invalid credentials, not found, etc.)

**Verification Checklist**:
- [ ] All `StalwartClient` methods work against real server
- [ ] Authentication succeeds with valid credentials
- [ ] CRUD operations return expected results
- [ ] Error cases return appropriate exceptions
- [ ] Tests pass consistently

**Dependencies**: T1, T3, T4  

**Files to Create/Modify**:
- `tests/StalwartMigration.Integration.Tests/Integration/StalwartClientIntegrationTests.cs`

**Notes**: Focus on testing the public API surface of `StalwartClient`

---

### Task 7: Test AccountManager Integration
**ID**: T7  
**Priority**: High  
**Status**: Pending  
**Estimated**: 2 days  
**Assignee**: (unassigned)

**Description**: Create integration tests for the `AccountManager` class against real Stalwart server.

**Acceptance Criteria**:
- [ ] Test domain creation through AccountManager
- [ ] Test account creation with metadata
- [ ] Test alias creation and management
- [ ] Test quota and settings configuration

**Verification Checklist**:
- [ ] AccountManager methods work against real server
- [ ] Created resources match expected state
- [ ] Error cases handled appropriately
- [ ] All existing unit tests still pass

**Dependencies**: T6  

**Files to Create/Modify**:
- `tests/StalwartMigration.Integration.Tests/Integration/AccountManagerIntegrationTests.cs`

**Notes**: Test both success and error paths

---

## ✅ Checkpoint: API Integration Tests

**Verification**:
- [ ] StalwartClient integration tests pass
- [ ] AccountManager integration tests pass
- [ ] All CRUD operations work correctly
- [ ] Error handling is correct

**Blockers**: None identified

---

## 🚀 Phase 4: Migration Workflow Tests

### Task 8: Create End-to-End Migration Test
**ID**: T8  
**Priority**: Medium  
**Status**: Pending  
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

## 🎨 Phase 5: Test Infrastructure Polish

### Task 9: Add Parallel Test Support
**ID**: T9  
**Priority**: Low  
**Status**: Pending  
**Estimated**: 1-2 days  
**Assignee**: (unassigned)

**Description**: Enable parallel test execution with isolated Stalwart instances.

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
**Priority**: Low  
**Status**: Pending  
**Estimated**: 1 day  
**Assignee**: (unassigned)

**Description**: Implement comprehensive logging for integration tests to aid debugging.

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
| Create Docker Test Infrastructure | T1 | High | Pending | 2-3 days | - |
| Create Test Configuration | T2 | High | Pending | 1 day | - |
| Setup Test Fixture Base Class | T3 | High | Pending | 1-2 days | - |
| Create Test Data Initialization | T4 | Medium | Pending | 1-2 days | - |
| Create Test Credentials Management | T5 | Medium | Pending | 1 day | - |
| Test StalwartClient against Real Server | T6 | High | Pending | 2-3 days | - |
| Test AccountManager Integration | T7 | High | Pending | 2 days | - |
| Create End-to-End Migration Test | T8 | Medium | Pending | 3-4 days | - |
| Add Parallel Test Support | T9 | Low | Pending | 1-2 days | - |
| Add Test Logging and Diagnostics | T10 | Low | Pending | 1 day | - |

### Phase Completion

- **Phase 1: Infrastructure Setup**: 0% (0/3 tasks)
- **Phase 2: Test Data Setup**: 0% (0/2 tasks)
- **Phase 3: API Integration Tests**: 0% (0/2 tasks)
- **Phase 4: Migration Workflow Tests**: 0% (0/1 task)
- **Phase 5: Test Infrastructure Polish**: 0% (0/2 tasks)

**Overall Completion**: 0%

### Milestones

- [ ] **Milestone 1**: Infrastructure Setup Complete (Phase 1)
- [ ] **Milestone 2**: Test Data Setup Complete (Phase 2)
- [ ] **Milestone 3**: API Integration Tests Complete (Phase 3)
- [ ] **Milestone 4**: Migration Workflow Tests Complete (Phase 4)
- [ ] **Milestone 5**: Test Infrastructure Polish Complete (Phase 5)

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
- `api` (T6, T7)
- `migration` (T8)
- `configuration` (T2, T5)
- `testing` (all)

---

*Last updated: 2026-07-12*
