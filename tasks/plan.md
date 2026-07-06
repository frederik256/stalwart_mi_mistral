# Implementation Plan: Fix Remaining 6 Data Loss Issues

## Overview

This plan addresses the **6 remaining data loss issues** after architectural simplifications:
- Vandelay handles all message migration (eliminates Issues #1, #2, #3, #5, #8, #9)
- COM-only for infrastructure (no database fallback) (eliminates Issue #6)
- Fresh passwords (no extraction) (eliminates Issue #4)

**Key Architecture:**
- **Messages**: Vandelay only (IMAP→JMAP)
- **Infrastructure**: COM API only (domains, accounts, aliases)
- **Passwords**: Users set fresh passwords in Stalwart

---

## Remaining Issues (6 total)

| # | Issue | Severity | Location |
|---|-------|----------|----------|
| 7 | Quota Information Silent Failure | 🟡 HIGH | HMailServerClient.cs:975-1001 |
| 10 | Confusing COM Error Message | 🟡 HIGH | HMailServerClient.cs:52-88 |
| 11 | IDN Encoding Issues | 🟡 MEDIUM | DomainValidator.cs:86-94 |
| 12 | COM Object Leakage | 🟠 LOW | HMailServerClient.cs:1005-1043 |
| 13 | No Connection Health Monitoring | 🟠 LOW | HMailServerClient.cs |
| 14 | No Rate Limiting/Throttling | 🟠 LOW | HMailServerClient.cs, Exporter |

---

## Dependency Graph

```
HMailServerClient.cs (5 issues)
├── ConvertMaxSizeToBytes() -- Issue #7
├── Constructor (COM init) -- Issue #10
├── Dispose() -- Issue #12
├── Health check methods (new) -- Issue #13
└── Iteration methods -- Issue #14

DomainValidator.cs (1 issue)
└── Normalize() -- Issue #11

MigrationOptions.cs (config for Issues #13, #14)
```

**Implementation Order**: All 6 tasks are independent - can be executed in any order or in parallel.

---

## Phase Overview

### Phase 1: Critical Error Handling (2 tasks)
**Goal**: Fix silent failures and confusing error messages
- Task 1: Fix Quota Information Silent Failure (#7)
- Task 2: Fix Confusing COM Error Message (#10)

### Phase 2: Encoding & Resource Management (2 tasks)
**Goal**: Fix encoding and COM resource handling
- Task 3: Fix IDN Encoding Issues (#11)
- Task 4: Fix COM Object Leakage (#12)

### Phase 3: Operational Improvements (2 tasks)
**Goal**: Add reliability features
- Task 5: Add Connection Health Monitoring (#13)
- Task 6: Add Rate Limiting/Throttling (#14)

---

## Detailed Task List

### Task 1: Fix Quota Information Silent Failure (Issue #7)

**Description**: `ConvertMaxSizeToBytes()` silently swallows all exceptions and returns null, causing quota data loss for domains and accounts.

**Files to modify:**
- `src/StalwartMigration/Infrastructure/HMailServer/HMailServerClient.cs` (lines 975-1001)

**Changes:**
- Add logging in catch block: `_logger.LogWarning(ex, "Failed to convert size to bytes: {SizeValue}", sizeObj)`
- Return default value `0` instead of `null` on failure
- Optionally add `QuotaParsingFailed` flag to Domain/Account models (future enhancement)

**Acceptance criteria:**
- [ ] Exceptions in ConvertMaxSizeToBytes are logged with context
- [ ] Returns 0 instead of null on failure
- [ ] Null quota values no longer propagate through system

**Verification:**
- [ ] `dotnet build StalwartMigration.sln` succeeds
- [ ] `dotnet test` passes
- [ ] Manual: Verify quota fields have numeric values instead of null

**Dependencies:** None

**Estimated scope:** Small (1 file, ~5 lines)

---

### Task 2: Fix Confusing COM Error Message (Issue #10)

**Description**: COM initialization failure is logged as a warning (not error), and the final exception lacks specific COM error details.

**Files to modify:**
- `src/StalwartMigration/Infrastructure/HMailServer/HMailServerClient.cs` (lines 52-88)

**Changes:**
- Change `LogWarning` to `LogError` for COM initialization failure
- Include COM exception in the final exception message
- Update remediation text to remove database fallback reference
- Simplify: Since no database fallback, throw immediately if COM fails

**Acceptance criteria:**
- [ ] COM failure logged as Error (not Warning)
- [ ] Final exception includes specific COM error details
- [ ] Clear remediation message for COM failures

**Verification:**
- [ ] `dotnet build StalwartMigration.sln` succeeds
- [ ] `dotnet test` passes
- [ ] Manual: Verify clear error messages when COM is unavailable

**Dependencies:** None

**Estimated scope:** Small (1 file, ~10 lines)

---

### Task 3: Fix IDN Encoding Issues (Issue #11)

**Description**: `DomainValidator.Normalize()` only performs `Trim().ToLowerInvariant()` without Punycode conversion for international domain names (IDN).

**Files to modify:**
- `src/StalwartMigration/Utilities/Helpers/DomainValidator.cs` (lines 86-94)

**Changes:**
- Add `using System.Globalization` for IdnMapping
- Use `IdnMapping` class for proper IDN handling
- Normalize to ASCII-compatible encoding (Punycode) when needed
- Add IDN validation and conversion

**Acceptance criteria:**
- [ ] IDN domains (münchen.de, 中国icann.测试) handled correctly
- [ ] Normalize() uses proper Punycode conversion
- [ ] Unit tests for various international domain names

**Verification:**
- [ ] `dotnet build StalwartMigration.sln` succeeds
- [ ] `dotnet test` passes (including new IDN tests)
- [ ] Manual: Verify international domain names are stored correctly

**Dependencies:** None

**Estimated scope:** Small (1 file, ~10 lines)

---

### Task 4: Fix COM Object Leakage (Issue #12)

**Description**: COM object disposal swallows exceptions and logs only warnings, masking COM object leaks that can cause resource exhaustion.

**Files to modify:**
- `src/StalwartMigration/Infrastructure/HMailServer/HMailServerClient.cs` (lines 1005-1043)

**Changes:**
- Change `LogWarning` to `LogError` in Dispose() catch block
- Add `Marshal.FinalReleaseComObject` in finalizer for safety
- Optionally add COM object reference tracking for diagnostics

**Acceptance criteria:**
- [ ] COM disposal failures logged as Error (not Warning)
- [ ] Finalizer uses Marshal.FinalReleaseComObject
- [ ] COM objects are properly released

**Verification:**
- [ ] `dotnet build StalwartMigration.sln` succeeds
- [ ] `dotnet test` passes
- [ ] Manual: Verify no COM object leaks in long-running operations

**Dependencies:** None

**Estimated scope:** Small (1 file, ~5 lines)

---

### Task 5: Add Connection Health Monitoring (Issue #13)

**Description**: No proactive health monitoring for COM API connections. Connections are only validated when operations are attempted.

**Files to modify:**
- `src/StalwartMigration/Infrastructure/HMailServer/HMailServerClient.cs` (add new method)
- `src/StalwartMigration/Core/Models/MigrationOptions.cs` (add configuration)

**Changes:**
- Add `TestComConnectionAsync()` method for on-demand health checks
- Add `ComConnectionTimeout` configuration option (TimeSpan, default: 5 seconds)
- Track connection state and last successful check time
- Add automatic reconnection logic for transient failures (optional)

**Acceptance criteria:**
- [ ] Health check method exists and works correctly
- [ ] Connection timeout is configurable
- [ ] Connection state is tracked

**Verification:**
- [ ] `dotnet build StalwartMigration.sln` succeeds
- [ ] `dotnet test` passes (including health check tests)
- [ ] Manual: Verify connection monitoring works correctly

**Dependencies:** None

**Estimated scope:** Medium (2 files, ~20 lines)

---

### Task 6: Add Rate Limiting/Throttling (Issue #14)

**Description**: No rate limiting when iterating through large datasets. Aggressive iteration can cause hMailServer performance degradation or COM API timeouts.

**Files to modify:**
- `src/StalwartMigration/Infrastructure/HMailServer/HMailServerClient.cs` (add configuration)
- `src/StalwartMigration/Core/Models/MigrationOptions.cs` (add batch/rate config)
- `src/StalwartMigration/Core/Exporters/HMailServerExporter.cs` (implement throttling)

**Changes:**
- Add `DelayBetweenIterationsMs` configuration option (int, default: 100ms)
- Add `BatchSize` configuration for batch processing (int, default: 100)
- Add `await Task.Delay(...)` between iterations in domain/account loops
- Consider adding memory pressure monitoring (optional)

**Acceptance criteria:**
- [ ] Configurable delay between iterations
- [ ] Configurable batch sizes
- [ ] Throttling prevents hMailServer overload

**Verification:**
- [ ] `dotnet build StalwartMigration.sln` succeeds
- [ ] `dotnet test` passes
- [ ] Manual: Verify throttling prevents COM API timeouts

**Dependencies:** None

**Estimated scope:** Medium (3 files, ~25 lines)

---

## Checkpoints

### Checkpoint 1: After Phase 1 (Tasks 1-2)
**Critical Error Handling Complete**
- [ ] All builds succeed without errors
- [ ] All existing tests pass
- [ ] Quota conversion and COM error messages verified
- [ ] **Review before proceeding to Phase 2**

### Checkpoint 2: After Phase 2 (Tasks 3-4)
**Encoding & Resource Management Complete**
- [ ] All builds succeed without errors
- [ ] All existing tests pass
- [ ] IDN encoding and COM disposal verified
- [ ] **Review before proceeding to Phase 3**

### Checkpoint 3: After Phase 3 (Tasks 5-6)
**Operational Improvements Complete**
- [ ] All builds succeed without errors
- [ ] All existing tests pass
- [ ] Health monitoring and rate limiting verified
- [ ] **Ready for production use**

---

## Parallelization Strategy

All 6 tasks are **independent** and can be executed in any order or in parallel:

| Group | Tasks | Files | Can Parallelize |
|-------|-------|-------|-----------------|
| A | 1-2 | HMailServerClient.cs | ✅ Yes (different methods) |
| B | 3 | DomainValidator.cs | ✅ Yes (different file) |
| C | 4 | HMailServerClient.cs | ✅ Yes (different method) |
| D | 5 | HMailServerClient.cs + MigrationOptions.cs | ✅ Yes |
| E | 6 | HMailServerClient.cs + MigrationOptions.cs + Exporter | ⚠️ Sequential with #5 |

**Note**: Tasks 5 and 6 both modify MigrationOptions.cs, so they should be sequenced or coordinated.

**Recommended Approach**: 
- Tasks 1-4: Can all run in parallel (different files or different methods)
- Tasks 5-6: Run sequentially after Tasks 1-4

---

## Implementation Workflow

For each task, follow this workflow:

1. **Read** the specific code section identified in the task
2. **Edit** the code with the required changes
3. **Build**: Run `dotnet build StalwartMigration.sln -c Release`
4. **Test**: Run `dotnet test`
5. **Verify**: Manual check if specified
6. **Commit**: If all pass, commit with descriptive message:
   ```bash
   git add .
   git commit -m "Fix: Issue #X - [Brief description]"
   ```
7. **Proceed** to next task

**Commit Message Format:**
```
Fix: Issue #7 - Add logging to ConvertMaxSizeToBytes and return 0 on error
Fix: Issue #10 - Log COM init errors and include details in exception
Fix: Issue #11 - Add IDN encoding support using IdnMapping for international domains
Fix: Issue #12 - Log COM disposal errors and use FinalReleaseComObject in finalizer
Fix: Issue #13 - Add COM connection health monitoring with configurable timeout
Fix: Issue #14 - Add rate limiting with configurable delay and batch size
```

---

## File Change Summary

| File | Tasks | Changes |
|------|-------|---------|
| HMailServerClient.cs | 1, 2, 4, 5, 6 | Multiple method fixes |
| DomainValidator.cs | 3 | Normalize() method fix |
| MigrationOptions.cs | 5, 6 | Add timeout/batch configs |
| HMailServerExporter.cs | 6 | Add throttling |

---

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Breaking existing functionality | High | Run full test suite after each change |
| COM changes affecting multiple features | Medium | Test all COM-related operations |
| Performance regression from throttling | Low | Make delay configurable, default to small value |

---

## Open Questions

1. **Rate limiting defaults**: What should the default delay be? (Recommend: 100ms)
2. **Batch size defaults**: What should the default batch size be? (Recommend: 100)
3. **Health check frequency**: Should we add periodic health checks or just on-demand? (Recommend: on-demand for now)

---

*Plan created: 2026-07-04*  
*Architecture: Vandelay for messages, COM-only for infrastructure, fresh passwords*  
*Status: Ready for implementation*
