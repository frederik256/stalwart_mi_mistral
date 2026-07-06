# Task List: Fix Remaining 6 Data Loss Issues

**Plan Document**: [plan.md](./plan.md)  
**Created**: 2026-07-04  
**Architecture**: Vandelay for messages, COM-only for infrastructure, fresh passwords  
**Status**: Ready for implementation

---

## Quick Status

| Phase | Tasks | Total | Completed | In Progress | Pending |
|-------|-------|-------|-----------|-------------|---------|
| Phase 1 (Error Handling) | Tasks 1-2 | 2 | 0 | 0 | 2 |
| Phase 2 (Encoding/Resources) | Tasks 3-4 | 2 | 0 | 0 | 2 |
| Phase 3 (Operational) | Tasks 5-6 | 2 | 0 | 0 | 2 |
| **Total** | **6** | **6** | **0** | **0** | **6** |

---

## Legend
- ✅ = Completed
- 🟡 = In Progress
- ❌ = Blocked
- ⬜ = Pending
- 🔴 = Critical
- 🟠 = High
- 🟡 = Medium
- 🟢 = Low

---

## Phase 1: Critical Error Handling

### 🟠 Task 1: Fix Quota Information Silent Failure (Issue #7)
- **Status**: ⬜ Pending
- **Priority**: HIGH
- **Estimated Effort**: Small (1 file, ~5 lines)
- **Dependency**: None
- **File**: `src/StalwartMigration/Infrastructure/HMailServer/HMailServerClient.cs:975-1001`

**Description**: `ConvertMaxSizeToBytes()` silently swallows exceptions and returns null.

**Acceptance criteria:**
- [ ] Exceptions logged with context
- [ ] Returns 0 instead of null on failure
- [ ] Null quota values don't propagate

**Verification:**
- [ ] `dotnet build StalwartMigration.sln` succeeds
- [ ] `dotnet test` passes

---

### 🟠 Task 2: Fix Confusing COM Error Message (Issue #10)
- **Status**: ⬜ Pending
- **Priority**: HIGH
- **Estimated Effort**: Small (1 file, ~10 lines)
- **Dependency**: None
- **File**: `src/StalwartMigration/Infrastructure/HMailServer/HMailServerClient.cs:52-88`

**Description**: COM initialization failure logged as warning, final exception lacks details.

**Acceptance criteria:**
- [ ] COM failure logged as Error (not Warning)
- [ ] Final exception includes COM error details
- [ ] Clear remediation message

**Verification:**
- [ ] `dotnet build StalwartMigration.sln` succeeds
- [ ] `dotnet test` passes

---

## 🟡 Checkpoint: Phase 1 Complete
- [ ] All builds succeed
- [ ] All tests pass
- [ ] Quota conversion and COM error messages verified
- [ ] **Review before proceeding to Phase 2**

---

## Phase 2: Encoding & Resource Management

### 🟡 Task 3: Fix IDN Encoding Issues (Issue #11)
- **Status**: ⬜ Pending
- **Priority**: MEDIUM
- **Estimated Effort**: Small (1 file, ~10 lines)
- **Dependency**: None
- **File**: `src/StalwartMigration/Utilities/Helpers/DomainValidator.cs:86-94`

**Description**: `Normalize()` doesn't handle Punycode conversion for international domains.

**Acceptance criteria:**
- [ ] IDN domains (münchen.de, 中国icann) handled correctly
- [ ] Uses IdnMapping for Punycode conversion
- [ ] Unit tests for international domains

**Verification:**
- [ ] `dotnet build StalwartMigration.sln` succeeds
- [ ] `dotnet test` passes

---

### 🟡 Task 4: Fix COM Object Leakage (Issue #12)
- **Status**: ⬜ Pending
- **Priority**: MEDIUM
- **Estimated Effort**: Small (1 file, ~5 lines)
- **Dependency**: None
- **File**: `src/StalwartMigration/Infrastructure/HMailServer/HMailServerClient.cs:1005-1043`

**Description**: Dispose() swallows exceptions and logs only warnings.

**Acceptance criteria:**
- [ ] COM disposal failures logged as Error
- [ ] Finalizer uses Marshal.FinalReleaseComObject
- [ ] COM objects properly released

**Verification:**
- [ ] `dotnet build StalwartMigration.sln` succeeds
- [ ] `dotnet test` passes

---

## 🟡 Checkpoint: Phase 2 Complete
- [ ] All builds succeed
- [ ] All tests pass
- [ ] IDN encoding and COM disposal verified
- [ ] **Review before proceeding to Phase 3**

---

## Phase 3: Operational Improvements

### 🟢 Task 5: Add Connection Health Monitoring (Issue #13)
- **Status**: ⬜ Pending
- **Priority**: LOW
- **Estimated Effort**: Medium (2 files, ~20 lines)
- **Dependency**: None
- **Files**: 
  - `src/StalwartMigration/Infrastructure/HMailServer/HMailServerClient.cs`
  - `src/StalwartMigration/Core/Models/MigrationOptions.cs`

**Description**: No proactive health monitoring for COM connections.

**Acceptance criteria:**
- [ ] Health check method exists
- [ ] Connection timeout configurable
- [ ] Connection state tracked

**Verification:**
- [ ] `dotnet build StalwartMigration.sln` succeeds
- [ ] `dotnet test` passes

---

### 🟢 Task 6: Add Rate Limiting/Throttling (Issue #14)
- **Status**: ⬜ Pending
- **Priority**: LOW
- **Estimated Effort**: Medium (3 files, ~25 lines)
- **Dependency**: None
- **Files**:
  - `src/StalwartMigration/Infrastructure/HMailServer/HMailServerClient.cs`
  - `src/StalwartMigration/Core/Models/MigrationOptions.cs`
  - `src/StalwartMigration/Core/Exporters/HMailServerExporter.cs`

**Description**: No rate limiting when iterating through large datasets.

**Acceptance criteria:**
- [ ] Configurable delay between iterations
- [ ] Configurable batch sizes
- [ ] Throttling prevents overload

**Verification:**
- [ ] `dotnet build StalwartMigration.sln` succeeds
- [ ] `dotnet test` passes

---

## 🟡 Checkpoint: Phase 3 Complete
- [ ] All builds succeed
- [ ] All tests pass
- [ ] Health monitoring and throttling verified
- [ ] **Ready for production use**

---

## Parallelization Guide

### Can Run in Parallel (Different Files/Methods)
- **Task 1** (HMailServerClient.cs - ConvertMaxSizeToBytes)
- **Task 2** (HMailServerClient.cs - Constructor)
- **Task 3** (DomainValidator.cs - Normalize)
- **Task 4** (HMailServerClient.cs - Dispose)

All 4 tasks modify **different methods or different files**, so they can run in parallel.

### Must Run Sequentially (Shared Files)
- **Task 5** (HMailServerClient.cs + MigrationOptions.cs)
- **Task 6** (HMailServerClient.cs + MigrationOptions.cs + Exporter)

Both modify `MigrationOptions.cs`, so run them sequentially.

**Recommended Execution Order:**
1. Parallel: Tasks 1, 2, 3, 4
2. Sequential: Task 5, then Task 6

---

## Implementation Workflow

For each task:

```bash
# 1. Read the code
read file_path="src/..." offset=... limit=...

# 2. Make changes
edit file_path="..." old_string="..." new_string="..."

# 3. Build
dotnet build StalwartMigration.sln -c Release

# 4. Test
dotnet test

# 5. If successful, commit
git add .
git commit -m "Fix: Issue #X - [Brief description]"
```

---

## Commit Message Templates

```bash
# Task 1
git commit -m "Fix: Issue #7 - Add logging to ConvertMaxSizeToBytes and return 0 on error"

# Task 2
git commit -m "Fix: Issue #10 - Log COM initialization errors and include details in exception"

# Task 3
git commit -m "Fix: Issue #11 - Add IDN encoding support using IdnMapping for international domains"

# Task 4
git commit -m "Fix: Issue #12 - Log COM disposal errors and use FinalReleaseComObject in finalizer"

# Task 5
git commit -m "Fix: Issue #13 - Add COM connection health monitoring with configurable timeout"

# Task 6
git commit -m "Fix: Issue #14 - Add rate limiting with configurable delay and batch size"
```

---

## Quick Start

Start with any of Tasks 1-4 (they're all independent). Example for Task 1:

```bash
# Read the ConvertMaxSizeToBytes method
read file_path="src/StalwartMigration/Infrastructure/HMailServer/HMailServerClient.cs" offset=975 limit=30

# Then make the fix
edit file_path="src/StalwartMigration/Infrastructure/HMailServer/HMailServerClient.cs" \
  old_string="catch\n        {\n            return null;  // Silent failure - quota info lost, no logging\n        }" \
  new_string="catch (Exception ex)\n        {\n            _logger.LogWarning(ex, \"Failed to convert size to bytes: {SizeValue}\", sizeObj);\n            return 0;  // Return default value instead of null\n        }"

# Build and test
dotnet build StalwartMigration.sln -c Release
dotnet test

# Commit if successful
git add .
git commit -m "Fix: Issue #7 - Add logging to ConvertMaxSizeToBytes and return 0 on error"
```

---

*Task list created: 2026-07-04*  
*Status: Ready for implementation*
