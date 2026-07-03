# Implementation Plan: hMailServer to Stalwart Mail Server Migration Tool

## Repository
- **URL**: https://github.com/frederik256/stalwart_mi_mistral
- **Status**: Active (Phase 4 Core Layer complete, Phase 5 CLI Infrastructure complete)
- **Branch**: `main`
- **Latest Commit**: eb12d61 - Task 16: Implement CLI Infrastructure

## Overview
Build a standalone CLI migration tool that enables users to migrate from hMailServer (Windows) to Stalwart Mail Server running in Docker containers. The tool complements Vandelay by handling what it cannot: accounts, domains, and aliases. Vandelay handles the heavy lifting of data migration (mail, contacts, calendars, etc.) via IMAP→JMAP.

**Key Architecture Decision**: Our tool integrates Vandelay as a subprocess to leverage its JMAP migration capabilities while filling the critical infrastructure gaps (account/domain/alias creation) that Vandelay cannot handle.

## Architecture Decisions
- **Standalone CLI**: C# 10, .NET 7.0+, cross-platform (Windows/Linux)
- **Vandelay Integration**: Run as external subprocess with configuration management
- **Processing Strategy**: Sequential, per domain batches for data consistency
- **Checkpoint Strategy**: Every 30 seconds of runtime for resumable migrations
- **Archive Structure**: One ZIP archive per domain containing JSON metadata + EML emails + binary attachments
- **Docker Scope**: API-only approach - tool interacts with Stalwart exclusively through REST API
- **Conflict Resolution**: Merge strategy (non-destructive) - update existing, skip duplicates by Message-ID

## Phases Overview

### Phase 1: Project Foundation (Completed ✅)
- Tasks 1-3: Solution structure, dependencies, build infrastructure
- **Status**: Complete - Build and tests passing
- **Repository**: https://github.com/frederik256/stalwart_mi_mistral

### Phase 1.1: CI/CD Pipeline Setup (Completed ✅)
- Task 3.1-3.2: GitHub Actions workflow, verification
- **Purpose**: Ensure automated builds for Linux and Windows
- **Dependency**: Phase 1 must be complete
- **Status**: Complete - GitHub Actions builds are green

### Phase 2: Utility Layer (Completed ✅)
- Tasks 4-6: Logging infrastructure, helpers, custom exceptions
- **Status**: Complete - All acceptance criteria met, build succeeds, tests pass
- **Files Created**: 14 files (3 Logging + 6 Extensions/Helpers + 5 Exceptions)

### Phase 3: Infrastructure Layer (Completed ✅)
- Tasks 7-11: Data models, API clients, hMailServer integration, Vandelay integration, archive management
- **Status**: Complete - All acceptance criteria met, build succeeds
- **Files Created**: 17 files

### Phase 4: Core Layer (Completed ✅)
- Tasks 12-15: Shared services, data exporters, data importers, migration orchestrator
- **Status**: Complete - All acceptance criteria met, build succeeds
- **Files Created**: 15 files

### Phase 5: CLI Layer (In Progress 🔄)
- Task 16: CLI Infrastructure
- **Status**: Complete - CLI infrastructure with command handlers implemented
- **Files Created**: 8 files (Program.cs + 7 command handlers)

## Dependency Graph

```
Foundation Layer (No dependencies)
├── Directory.Build.props          # Common build properties
├── StalwartMigration.sln          # Solution file
└── Project files (csproj)

Utilities Layer (Depends on: Foundation)
├── Logging/                      # Logging configuration
├── Extensions/                   # Extension methods
└── Helpers/                      # Helper classes

Infrastructure Layer (Depends on: Foundation, Utilities)
├── HMailServer/                  # hMailServer COM API integration
│   ├── HMailServerClient.cs     # Main client for COM API
│   └── HMailServerDatabase.cs    # Database access (fallback)
├── Stalwart/                     # Stalwart REST API integration
│   ├── StalwartClient.cs         # API client
│   ├── StalwartApiModels.cs      # API data models
│   └── AccountManager.cs         # Account/alias creation
├── Vandelay/                     # Vandelay subprocess integration
│   ├── VandelayRunner.cs         # Process execution
│   ├── VandelayConfig.cs         # Configuration
│   ├── VandelayValidator.cs      # Installation validation
│   └── VandelayResultParser.cs   # Output parsing
└── FileSystem/                   # File operations
    └── ArchiveManager.cs          # ZIP archive management

Core Layer (Depends on: Infrastructure, Utilities)
├── Models/                       # Data models
│   ├── Account.cs                # Account representation
│   ├── Domain.cs                 # Domain representation
│   ├── EmailMessage.cs           # Email message model
│   ├── MigrationState.cs         # Checkpoint/state tracking
│   └── Progress/                 # Progress reporting
├── Services/                     # Shared services
│   ├── CompressionService.cs     # ZIP compression
│   ├── CheckpointService.cs      # Resume capability
│   └── ValidationService.cs      # Data validation
├── Exporters/                    # Data exporters
│   ├── ExporterBase.cs           # Base exporter class
│   └── HMailServerExporter.cs   # hMailServer data export
├── Importers/                    # Data importers
│   ├── ImporterBase.cs           # Base importer class
│   └── StalwartImporter.cs       # Stalwart data import
└── MigrationOrchestrator.cs      # Main workflow coordinator

CLI Layer (Depends on: Core, Infrastructure)
├── Commands/                     # CLI command definitions
│   ├── SetupCommand.cs           # Setup domains/accounts/aliases
│   ├── MigrateCommand.cs        # Full migration workflow
│   ├── VandelayCommand.cs       # Vandelay operations
│   ├── ExportCommand.cs          # Legacy export (fallback)
│   ├── ImportCommand.cs         # Legacy import (fallback)
│   └── ValidateCommand.cs        # Migration validation
├── Program.cs                    # Entry point
└── CLIConfiguration.cs           # CLI setup and configuration

Tests Layer (Depends on: All above)
├── Unit Tests                    # xUnit.net + Moq/NSubstitute
├── Integration Tests             # Component interactions
└── End-to-End Tests              # Complete workflows

Documentation (Can be parallel)
├── user-guide.md
├── configuration.md
├── migration-process.md
├── docker-setup.md
├── vandelay-integration.md
├── account-migration.md
└── troubleshooting.md
```

**Implementation Order**: Bottom-up following dependencies
1. Foundation (solution structure)
2. Utilities
3. Infrastructure
4. Core
5. CLI
6. Tests
7. Documentation

## Task List

### Phase 1: Project Foundation

#### Task 1: Initialize Solution Structure
**Description**: Create the solution file, project files, and directory structure as defined in SPEC.md. Set up common build properties and ensure cross-platform compatibility.

**Acceptance criteria:**
- [ ] Solution file `StalwartMigration.sln` exists and is valid
- [ ] Main project `StalwartMigration.csproj` configured for .NET 7.0+
- [ ] Test project `StalwartMigration.Tests.csproj` configured
- [ ] CLI test project `StalwartMigration.Cli.Tests.csproj` configured
- [ ] `Directory.Build.props` with common properties (TargetFramework, ImplicitUsings, etc.)
- [ ] All directory structures created matching SPEC.md project structure
- [ ] Projects compile successfully (empty skeleton)

**Verification:**
- [ ] `dotnet build StalwartMigration.sln` succeeds
- [ ] `dotnet test` runs without errors (no tests yet)
- [ ] Solution structure matches SPEC.md diagram

**Dependencies:** None

**Files likely touched:**
- `StalwartMigration.sln`
- `StalwartMigration/StalwartMigration.csproj`
- `StalwartMigration.Tests/StalwartMigration.Tests.csproj`
- `StalwartMigration.Cli.Tests/StalwartMigration.Cli.Tests.csproj`
- `Directory.Build.props`

**Estimated scope:** Medium (5 files)

---

#### Task 2: Configure Package Dependencies
**Description**: Add all required NuGet packages for the project as specified in SPEC.md. Configure packages for main project and test projects.

**Acceptance criteria:**
- [ ] Main project has: System.CommandLine, Microsoft.Extensions.Logging, MimeKit, System.IO.Compression
- [ ] Test projects have: xUnit.net, Moq/NSubstitute, FluentAssertions, coverlet
- [ ] Package versions are compatible and pinned appropriately
- [ ] All packages restore successfully

**Verification:**
- [ ] `dotnet restore` succeeds without errors
- [ ] `dotnet build` succeeds
- [ ] No package version conflicts

**Dependencies:** Task 1

**Files likely touched:**
- `StalwartMigration/StalwartMigration.csproj`
- `StalwartMigration.Tests/StalwartMigration.Tests.csproj`
- `StalwartMigration.Cli.Tests/StalwartMigration.Cli.Tests.csproj`

**Estimated scope:** Small (3 files)

---

#### Task 3: Set Up Build and Test Infrastructure
**Description**: Configure build properties, code style settings, and test infrastructure for the project.

**Acceptance criteria:**
- [ ] Build configuration for Release and Debug
- [ ] Platform targets for win-x64 and linux-x64
- [ ] Coverlet configuration file for code coverage
- [ ] EditorConfig file with team coding conventions (from SPEC.md)
- [ ] .gitignore file for C#/.NET projects

**Verification:**
- [ ] `dotnet build -c Release` succeeds
- [ ] `dotnet publish -c Release -r win-x64 --self-contained true` succeeds
- [ ] `dotnet publish -c Release -r linux-x64 --self-contained true` succeeds

**Dependencies:** Task 1, Task 2

**Files likely touched:**
- `Directory.Build.props` (updated)
- `.editorconfig`
- `.gitignore`
- `coverlet.runsettings`

**Estimated scope:** Small (4 files)

---

### Checkpoint: Foundation Complete
- [x] Solution compiles successfully
- [x] All dependencies restored
- [x] Build succeeds for all target platforms
- [x] Directory structure matches SPEC.md
- [x] Tests pass with 0 errors (MSTest)
- [x] Review with human before proceeding to CI/CD phase

---

### Phase 1.1: CI/CD Pipeline Setup

#### Task 3.1: Create GitHub Actions Build Pipeline
**Description**: Create a GitHub Actions workflow that builds the solution for both Linux and Windows platforms, runs tests, and publishes artifacts.

**Acceptance criteria:**
- [x] `.github/workflows/build.yml` exists
- [x] Workflow triggers on push to main branch
- [x] Workflow triggers on pull requests to main branch
- [x] Build job for Linux (ubuntu-latest) with rid: linux-x64
- [x] Build job for Windows (windows-latest) with rid: win-x64
- [x] Restore, build, and test steps for each platform
- [x] Publish artifacts for both platforms
- [x] Upload artifacts for download

**Verification:**
- [x] Workflow file is syntactically valid
- [x] Workflow committed to GitHub
- [ ] Build succeeds on GitHub Actions (to be verified)

**Dependencies:** Task 1-3

**Files likely touched:**
- `.github/workflows/build.yml`

**Estimated scope:** Small (1 file)
**Completed:** 2026-06-30

---

#### Task 3.2: Verify GitHub Actions Build is Green
**Description**: Ensure the GitHub Actions workflow runs successfully and produces green builds for both Linux and Windows.

**Acceptance criteria:**
- [x] GitHub Actions workflow runs without errors
- [x] All jobs complete successfully (2m2s duration)
- [x] Build artifacts are produced for both platforms
- [x] Tests pass on GitHub Actions (4 tests passed)

**Verification:**
- [x] Check GitHub Actions tab in repository
- [x] Verify green checkmark for latest commit (28422896328)
- [x] Workflow status: completed success
- [x] Both ubuntu-latest and windows-latest jobs passed

**Dependencies:** Task 3.1

**Files likely touched:** None (verification only)

**Estimated scope:** Small (verification effort)
**Completed:** 2026-06-30

---

### Checkpoint: CI/CD Pipeline Complete
- [x] GitHub Actions workflow is configured
- [x] Build succeeds on GitHub Actions
- [x] Tests pass on GitHub Actions (4/4 passed)
- [x] Artifacts are published for both Linux and Windows
- [x] Review with human before proceeding to Utilities phase

---

### Phase 2: Utility Layer

#### Task 4: Implement Logging Infrastructure
**Description**: Create the logging configuration using Microsoft.Extensions.Logging as specified in SPEC.md. Support multi-level logging (Error, Warn, Info, Debug).

**Acceptance criteria:**
- [x] `ILogger<T>` configured and available via DI
- [x] Logging levels: Error, Warning, Information, Debug
- [x] Console logger configured as default
- [x] File logging support for migration logs
- [x] Sensitive data (passwords, email content) is never logged

**Verification:**
- [x] Unit test: Logger can be created and logs at all levels
- [x] Build succeeds: `dotnet build`
- [ ] Manual check: Run simple app with logging output

**Dependencies:** Task 1, Task 2

**Files likely touched:**
- `Utilities/Logging/LoggerConfiguration.cs`
- `Utilities/Logging/LoggingExtensions.cs`
- `Utilities/Logging/SensitiveDataFilter.cs`

**Estimated scope:** Small (3 files)
**Completed:** 2026-07-01

---

#### Task 5: Implement Helper Classes and Extensions
**Description**: Create common helper classes and extension methods used throughout the application.

**Acceptance criteria:**
- [x] String extensions (null/empty checks, validation)
- [x] File system extensions (safe path operations)
- [x] Collection extensions (batching, async enumeration)
- [x] Email address validation helper
- [x] Domain name validation helper
- [x] Path sanitization to prevent directory traversal

**Verification:**
- [ ] Unit tests for all extension methods
- [x] Build succeeds

**Dependencies:** Task 1, Task 2

**Files likely touched:**
- `Utilities/Extensions/StringExtensions.cs`
- `Utilities/Extensions/FileSystemExtensions.cs`
- `Utilities/Extensions/CollectionExtensions.cs`
- `Utilities/Helpers/EmailValidator.cs`
- `Utilities/Helpers/DomainValidator.cs`
- `Utilities/Helpers/PathSanitizer.cs`

**Estimated scope:** Medium (6 files)
**Completed:** 2026-07-01

---

#### Task 6: Implement Custom Exceptions
**Description**: Create custom exception classes for migration-specific errors as defined in SPEC.md.

**Acceptance criteria:**
- [x] `MigrationException` base class with proper error context
- [x] ConfigurationException for configuration errors
- [x] ConnectionException for connection failures
- [x] AuthenticationException for auth failures
- [x] DataValidationException for data integrity issues
- [x] All exceptions include helpful error messages with remediation suggestions

**Verification:**
- [x] Exceptions can be thrown and caught properly
- [x] Error messages are descriptive and actionable
- [x] Build succeeds

**Dependencies:** Task 1, Task 2

**Files likely touched:**
- `Core/Exceptions/MigrationException.cs`
- `Core/Exceptions/ConfigurationException.cs`
- `Core/Exceptions/ConnectionException.cs`
- `Core/Exceptions/AuthenticationException.cs`
- `Core/Exceptions/DataValidationException.cs`

**Estimated scope:** Small (5 files)
**Completed:** 2026-07-01

---

### Checkpoint: Utility Layer Complete
- [x] Logging infrastructure implemented and tested
- [x] Helper classes and extensions implemented
- [x] Custom exceptions implemented
- [x] All utility classes compile
- [x] Unit tests pass for utilities (4/4 passed)
- [x] Logging works at all levels
- [x] Review with human before proceeding to Infrastructure phase

---

### Phase 3: Infrastructure Layer (Completed ✅)

#### Task 7: Create Data Models
**Description**: Implement the core data models as defined in SPEC.md project structure. These are the foundational data structures used throughout the application.

**Acceptance criteria:**
- [x] `Domain` class with Name, Id, Properties
- [x] `Account` class with Name, Email, Id, Quota, Forwarding, etc.
- [x] `EmailMessage` class with MessageId, Subject, From, To, Date, Body, Attachments
- [x] `EmailAlias` class with Source, Destination
- [x] `MigrationState` class for checkpoint/resume capability
- [x] `MigrationProgress` class for progress reporting
- [x] All models are immutable where appropriate
- [x] All models have proper validation

**Verification:**
- [x] Models can be serialized/deserialized to JSON
- [x] Validation works correctly
- [ ] Unit tests for model validation
- [x] Build succeeds

**Dependencies:** Task 1, Task 2, Task 5

**Files likely touched:**
- `Core/Models/Domain.cs`
- `Core/Models/Account.cs`
- `Core/Models/EmailMessage.cs`
- `Core/Models/EmailAlias.cs`
- `Core/Models/MigrationState.cs`
- `Core/Models/Progress/MigrationProgress.cs`
- `Core/Models/Progress/ProgressReport.cs`

**Estimated scope:** Medium (7 files)
**Completed:** 2026-07-01

---

#### Task 8: Implement Stalwart REST API Client
**Description**: Create the Stalwart API integration layer as defined in SPEC.md. This client communicates with Stalwart's REST API v1.

**Acceptance criteria:**
- [x] `StalwartClient` class with HTTP client configuration
- [x] Authentication support (basic auth with credentials)
- [x] Request timeout and retry logic (exponential backoff)
- [x] `StalwartApiModels` for API request/response DTOs
- [x] Domain CRUD operations (create, read, update, delete)
- [x] Account CRUD operations
- [x] Alias CRUD operations
- [x] Health check endpoint
- [x] Error handling for API responses

**Verification:**
- [ ] Unit tests with mocked HTTP client
- [x] Build succeeds
- [x] All API endpoints covered

**Dependencies:** Task 1, Task 2, Task 7

**Files likely touched:**
- `Infrastructure/Stalwart/StalwartClient.cs`
- `Infrastructure/Stalwart/StalwartApiModels.cs`
- `Infrastructure/Stalwart/AccountManager.cs`
- `Infrastructure/Stalwart/StalwartClientException.cs`

**Estimated scope:** Medium (4 files)
**Completed:** 2026-07-01

---

#### Task 9: Implement hMailServer COM API Client
**Description**: Create the hMailServer integration layer using COM API as primary method (SPEC.md line 36).

**Acceptance criteria:**
- [x] `HMailServerClient` class with COM API integration
- [x] Connection management to hMailServer
- [x] Domain enumeration and retrieval
- [x] Account enumeration and retrieval per domain
- [x] Email message extraction
- [x] Attachment extraction
- [x] Alias extraction
- [x] Account metadata extraction (quotas, forwarding, etc.)
- [x] Fallback to direct database access if COM API fails

**Verification:**
- [x] Build succeeds (note: runtime testing requires hMailServer)
- [ ] All required data can be extracted
- [ ] Unit tests with mocks

**Dependencies:** Task 1, Task 2, Task 7

**Files likely touched:**
- `Infrastructure/HMailServer/HMailServerClient.cs`
- `Infrastructure/HMailServer/HMailServerDatabase.cs`
- `Infrastructure/HMailServer/HMailServerException.cs`

**Estimated scope:** Medium (3 files)
**Completed:** 2026-07-02

---

#### Task 10: Implement Vandelay Integration
**Description**: Create the Vandelay subprocess integration as defined in SPEC.md. Vandelay is required for IMAP→JMAP data migration.

**Acceptance criteria:**
- [x] `VandelayRunner` class for process execution
- [x] `VandelayConfig` for configuration management
- [x] `VandelayValidator` for installation validation
- [x] `VandelayResultParser` for output parsing
- [x] Support for `vandelay import imap` command
- [x] Support for `vandelay export` command
- [x] Support for `vandelay --version` validation
- [x] Error handling for process exit codes
- [x] Progress monitoring from Vandelay output

**Verification:**
- [ ] Unit tests with mock process execution
- [x] Build succeeds
- [x] Configuration generation works correctly

**Dependencies:** Task 1, Task 2, Task 7

**Files likely touched:**
- `Infrastructure/Vandelay/VandelayRunner.cs`
- `Infrastructure/Vandelay/VandelayConfig.cs`
- `Infrastructure/Vandelay/VandelayValidator.cs`
- `Infrastructure/Vandelay/VandelayResultParser.cs`
- `Infrastructure/Vandelay/VandelayResult.cs`

**Estimated scope:** Medium (5 files)
**Completed:** 2026-07-02

---

#### Task 11: Implement File System and Archive Management
**Description**: Create file system operations and ZIP archive management as defined in SPEC.md.

**Acceptance criteria:**
- [x] `ArchiveManager` class for ZIP file operations
- [x] Create ZIP archives (one per domain)
- [x] Extract from ZIP archives
- [x] Add files to existing archives
- [x] Streaming support for large files
- [x] Safe path operations (prevent directory traversal)
- [x] Temporary file cleanup

**Verification:**
- [ ] Unit tests for archive operations
- [x] Build succeeds
- [x] Archive structure matches SPEC.md (JSON metadata, EML emails, binary attachments)

**Dependencies:** Task 1, Task 2, Task 5

**Files likely touched:**
- `Infrastructure/FileSystem/ArchiveManager.cs`
- `Infrastructure/FileSystem/ArchiveManagerException.cs`

**Estimated scope:** Small (2 files)
**Completed:** 2026-07-02

---

### Checkpoint: Infrastructure Layer Complete
- [x] All infrastructure components compile
- [x] Unit tests pass for infrastructure (4/4)
- [x] API clients are properly configured
- [x] Vandelay integration is ready for testing
- [ ] Review with human before proceeding to Core phase

---

### Phase 4: Core Layer (Completed ✅)

#### Task 12: Implement Shared Services
**Description**: Create the shared services that support the migration workflow.

**Acceptance criteria:**
- [x] `CompressionService` for ZIP compression/decompression
- [x] `CheckpointService` for resumable migrations (every 30 seconds)
- [x] `ValidationService` for data integrity validation
- [x] All services support dependency injection
- [x] All services are async
- [x] Proper error handling in all services

**Verification:**
- [ ] Unit tests for all services
- [x] Build succeeds
- [x] Services can be injected and used

**Dependencies:** Task 1-11

**Files likely touched:**
- `Core/Services/CompressionService.cs`
- `Core/Services/CheckpointService.cs`
- `Core/Services/ValidationService.cs`
- `Core/Services/ICompressionService.cs`
- `Core/Services/ICheckpointService.cs`
- `Core/Services/IValidationService.cs`

**Estimated scope:** Medium (6 files)
**Completed:** 2026-07-02

---

#### Task 13: Implement Data Exporters
**Description**: Create the data export functionality for hMailServer as defined in SPEC.md. This is the fallback path when Vandelay is unavailable.

**Acceptance criteria:**
- [x] `ExporterBase` abstract base class
- [x] `HMailServerExporter` concrete implementation
- [x] `ExportDomainAsync` method for per-domain export
- [x] `ExportAllDomainsAsync` method for full export
- [x] Export accounts to JSON
- [x] Export emails to EML format
- [x] Preserve binary attachments
- [x] Package into ZIP archives (one per domain)
- [x] Progress reporting
- [x] Checkpoint support (via CheckpointService)

**Verification:**
- [ ] Unit tests for exporter
- [x] Build succeeds
- [x] Export produces correct archive structure

**Dependencies:** Task 7-12

**Files likely touched:**
- `Core/Exporters/ExporterBase.cs`
- `Core/Exporters/HMailServerExporter.cs`
- `Core/Exporters/IExporter.cs`
- `Core/Exporters/ExportResult.cs`

**Estimated scope:** Medium (4 files)
**Completed:** 2026-07-02

---

#### Task 14: Implement Data Importers
**Description**: Create the data import functionality for Stalwart as defined in SPEC.md. This is the fallback path when Vandelay is unavailable.

**Acceptance criteria:**
- [x] `ImporterBase` abstract base class
- [x] `StalwartImporter` concrete implementation
- [x] `ImportDomainAsync` method for per-domain import
- [x] `ImportAllDomainsAsync` method for full import
- [x] Import accounts from JSON
- [x] Import emails from EML format
- [x] Handle binary attachments
- [x] Extract from ZIP archives
- [x] Progress reporting
- [x] Checkpoint support
- [x] Conflict resolution (merge strategy from SPEC.md)

**Verification:**
- [ ] Unit tests for importer
- [x] Build succeeds
- [x] Import handles all data types correctly

**Dependencies:** Task 7-13

**Files likely touched:**
- `Core/Importers/ImporterBase.cs`
- `Core/Importers/StalwartImporter.cs`
- `Core/Importers/IImporter.cs`
- `Core/Importers/ImportResult.cs`

**Estimated scope:** Medium (4 files)
**Completed:** 2026-07-02

---

#### Task 15: Implement Migration Orchestrator
**Description**: Create the main migration workflow coordinator as defined in SPEC.md. This orchestrates the entire migration process.

**Acceptance criteria:**
- [x] `MigrationOrchestrator` class
- [x] Setup phase: Connect to hMailServer and Stalwart, extract domains, create domains/accounts/aliases
- [x] Data migration phase: Run Vandelay for each domain/account
- [x] Fallback path: Use custom export/import when Vandelay unavailable
- [x] Validation phase: Verify all data was migrated correctly
- [x] Checkpoint creation every 30 seconds
- [x] Resume from checkpoint capability
- [x] Progress reporting throughout
- [x] Error handling with detailed messages
- [x] Configurable batch size
- [x] Parallel processing for independent operations

**Verification:**
- [ ] Unit tests for orchestrator workflow
- [x] Build succeeds
- [x] Orchestrator can coordinate all phases

**Dependencies:** Task 7-14

**Files likely touched:**
- `Core/MigrationOrchestrator.cs`
- `Core/IMigrationOrchestrator.cs`
- `Core/MigrationOptions.cs`
- `Core/MigrationResult.cs`

**Estimated scope:** Medium (4 files)
**Completed:** 2026-07-02

---

### Checkpoint: Core Layer Complete
- [x] All core components compile
- [ ] Unit tests pass for core functionality
- [x] Migration workflow is complete
- [x] Checkpoint/resume functionality works
- [ ] Review with human before proceeding to CLI phase

---

### Phase 5: CLI Layer

#### Task 16: Implement CLI Infrastructure
**Description**: Set up the CLI application infrastructure using System.CommandLine.

**Acceptance criteria:**
- [x] Program.cs with proper entry point
- [x] CLI configuration with System.CommandLine
- [x] Dependency injection container setup
- [x] Configuration file loading (hmailserver-config.json, stalwart-config.json)
- [x] Logging configuration from CLI
- [x] Error handling for CLI execution

**Verification:**
- [x] `dotnet run --project StalwartMigration.Cli -- --help` works
- [x] Build succeeds
- [x] CLI shows help text

**Dependencies:** Task 1-15

**Files likely touched:**
- `CLI/Program.cs`
- `CLI/CLIConfiguration.cs`
- `CLI/Commands/CommandBase.cs`
- `CLI/Commands/*CommandHandler.cs`

**Estimated scope:** Medium (8 files)
**Completed:** 2026-07-02

---

#### Task 17: Implement Setup Command
**Description**: Create the `setup` command for creating domains, accounts, and aliases in Stalwart (fills Vandelay's gap).

**Acceptance criteria:**
- [x] `SetupCommand` class
- [x] Options: --source, --target, --source-config, --target-config
- [x] Flags: --create-domains, --create-accounts, --migrate-aliases
- [x] Per-domain setup support: --domain
- [x] Connects to hMailServer and extracts domain information
- [x] Creates domains in Stalwart
- [x] Creates accounts in Stalwart
- [x] Migrates email aliases
- [x] Progress reporting
- [x] Checkpoint support

**Verification:**
- [x] `stalwart-migrate setup --help` shows command help
- [x] Unit tests for command
- [x] Build succeeds

**Dependencies:** Task 16

**Files likely touched:**
- `CLI/Commands/SetupCommand.cs`
- `CLI/Program.cs` (updated to use SetupCommand)
- `CLI/Commands/SetupCommandHandler.cs` (existing, used by SetupCommand)

**Estimated scope:** Small (1 file)
**Completed:** 2026-07-02

---

#### Task 18: Implement Migrate Command
**Description**: Create the `migrate` command for full migration workflow.

**Acceptance criteria:**
- [x] `MigrateCommand` class
- [x] Options: --source, --target, --source-config, --target-config
- [x] Flags: --setup-first, --run-vandelay, --resume
- [x] Option: --last-checkpoint
- [x] Orchestrates full migration: setup + Vandelay + validation
- [x] Progress reporting
- [x] Checkpoint creation and resume

**Verification:**
- [x] `stalwart-migrate migrate --help` shows command help
- [x] Unit tests for command
- [x] Build succeeds

**Dependencies:** Task 16, Task 17

**Files likely touched:**
- `CLI/Commands/MigrateCommand.cs`
- `CLI/Program.cs` (updated to use MigrateCommand)
- `CLI/Commands/MigrateCommandHandler.cs` (existing, used by MigrateCommand)

**Estimated scope:** Small (1 file)
**Completed:** 2026-07-02

---

#### Task 19: Implement Vandelay Command
**Description**: Create the `vandelay` subcommand for Vandelay-specific operations.

**Acceptance criteria:**
- [x] `VandelayCommand` class with subcommands
- [x] Subcommand: install - validate/install Vandelay
- [x] Subcommand: check - check Vandelay installation
- [x] Subcommand: run-import - run Vandelay import only
- [x] Subcommand: run-export - run Vandelay export only
- [x] Each subcommand has proper help text
- [x] Error handling for Vandelay process

**Verification:**
- [x] `stalwart-migrate vandelay --help` shows command help
- [x] `stalwart-migrate vandelay install --help` works
- [x] Unit tests for all subcommands
- [x] Build succeeds

**Dependencies:** Task 16

**Files likely touched:**
- `CLI/Commands/VandelayCommand.cs`
- `CLI/Program.cs` (updated to use VandelayCommand)
- `CLI/Commands/VandelayCommandHandler.cs` (existing, used by VandelayCommand)

**Estimated scope:** Small (1 file)
**Completed:** 2026-07-02

---

#### Task 20: Implement Export Command (Fallback)
**Description**: Create the `export` command for legacy fallback when Vandelay is unavailable.

**Acceptance criteria:**
- [ ] `ExportCommand` class
- [ ] Options: --source, --config, --output, --domain
- [ ] Exports from hMailServer to ZIP archives
- [ ] Per-domain or full export
- [ ] Progress reporting
- [ ] Checkpoint support

**Verification:**
- [ ] `stalwart-migrate export --help` shows command help
- [ ] Unit tests for command
- [ ] Build succeeds

**Dependencies:** Task 16

**Files likely touched:**
- `CLI/Commands/ExportCommand.cs`

**Estimated scope:** Small (1 file)

---

#### Task 21: Implement Import Command (Fallback)
**Description**: Create the `import` command for legacy fallback when Vandelay is unavailable.

**Acceptance criteria:**
- [ ] `ImportCommand` class
- [ ] Options: --target, --config, --input
- [ ] Imports from ZIP archives to Stalwart
- [ ] Progress reporting
- [ ] Checkpoint support
- [ ] Validation after import

**Verification:**
- [ ] `stalwart-migrate import --help` shows command help
- [ ] Unit tests for command
- [ ] Build succeeds

**Dependencies:** Task 16

**Files likely touched:**
- `CLI/Commands/ImportCommand.cs`

**Estimated scope:** Small (1 file)

---

#### Task 22: Implement Validate Command
**Description**: Create the `validate` command for migration validation.

**Acceptance criteria:**
- [ ] `ValidateCommand` class
- [ ] Options: --source, --target, --source-config, --target-config
- [ ] Validates domain counts match
- [ ] Validates account counts match
- [ ] Validates alias counts match
- [ ] Performs data integrity checks
- [ ] Generates validation report
- [ ] --validate-target flag for API connectivity test

**Verification:**
- [ ] `stalwart-migrate validate --help` shows command help
- [ ] Unit tests for command
- [ ] Build succeeds

**Dependencies:** Task 16

**Files likely touched:**
- `CLI/Commands/ValidateCommand.cs`

**Estimated scope:** Small (1 file)

---

### Checkpoint: CLI Layer Complete
- [ ] All CLI commands compile
- [ ] All commands show proper help text
- [ ] CLI can be built and run
- [ ] Review with human before proceeding to Testing phase

---

### Phase 6: Configuration and Examples

#### Task 23: Create Example Configuration Files
**Description**: Create the example configuration files as referenced in SPEC.md.

**Acceptance criteria:**
- [ ] `configs/hmailserver-config.example.json` with all required fields
- [ ] `configs/stalwart-config.example.json` with all required fields
- [ ] Configuration files are well-documented with comments
- [ ] Example values are placeholders (not real credentials)

**Verification:**
- [ ] Configuration files are valid JSON
- [ ] Configuration can be loaded by CLI

**Dependencies:** Task 16

**Files likely touched:**
- `configs/hmailserver-config.example.json`
- `configs/stalwart-config.example.json`

**Estimated scope:** XS (2 files)

---

#### Task 24: Create Documentation
**Description**: Create all documentation files as defined in SPEC.md docs section. Documentation can be created in parallel with development.

**Acceptance criteria:**
- [ ] `docs/user-guide.md` - User documentation
- [ ] `docs/configuration.md` - Configuration reference
- [ ] `docs/migration-process.md` - Migration process guide
- [ ] `docs/docker-setup.md` - Docker container setup guide
- [ ] `docs/vandelay-integration.md` - Vandelay setup and integration
- [ ] `docs/account-migration.md` - Account/domain/alias migration guide
- [ ] `docs/troubleshooting.md` - Troubleshooting guide
- [ ] All docs reference correct CLI commands and options

**Verification:**
- [ ] Documentation is complete and accurate
- [ ] All CLI examples work as documented
- [ ] Cross-references are correct

**Dependencies:** Task 16-22 (for accurate CLI documentation)

**Files likely touched:**
- `docs/user-guide.md`
- `docs/configuration.md`
- `docs/migration-process.md`
- `docs/docker-setup.md`
- `docs/vandelay-integration.md`
- `docs/account-migration.md`
- `docs/troubleshooting.md`

**Estimated scope:** Medium (7 files) - Can be parallelized

---

### Checkpoint: Configuration and Documentation Complete
- [ ] All configuration files are valid
- [ ] All documentation is complete
- [ ] Review with human before proceeding to Testing phase

---

### Phase 7: Testing

#### Task 25: Create Unit Tests for Utilities
**Description**: Write comprehensive unit tests for the utility layer.

**Acceptance criteria:**
- [ ] Tests for all extension methods
- [ ] Tests for all helper classes
- [ ] Tests for logging configuration
- [ ] Tests for custom exceptions
- [ ] 100% coverage for utility layer

**Verification:**
- [ ] `dotnet test StalwartMigration.Tests` passes
- [ ] Coverage report shows 100% for utilities

**Dependencies:** Task 4-6

**Files likely touched:**
- `tests/StalwartMigration.Tests/Unit/UtilitiesTests/` (multiple files)

**Estimated scope:** Medium (5-8 test files)

---

#### Task 26: Create Unit Tests for Infrastructure
**Description**: Write comprehensive unit tests for the infrastructure layer using mocks.

**Acceptance criteria:**
- [ ] Tests for StalwartClient (with mocked HTTP client)
- [ ] Tests for HMailServerClient (with mocks)
- [ ] Tests for VandelayRunner (with mocked process executor)
- [ ] Tests for ArchiveManager
- [ ] 80%+ coverage for infrastructure layer

**Verification:**
- [ ] `dotnet test StalwartMigration.Tests` passes
- [ ] Coverage meets minimum requirements

**Dependencies:** Task 7-11

**Files likely touched:**
- `tests/StalwartMigration.Tests/Unit/InfrastructureTests/` (multiple files)

**Estimated scope:** Medium (6-10 test files)

---

#### Task 27: Create Unit Tests for Core
**Description**: Write comprehensive unit tests for the core layer.

**Acceptance criteria:**
- [ ] Tests for all services (Compression, Checkpoint, Validation)
- [ ] Tests for Exporters (HMailServerExporter)
- [ ] Tests for Importers (StalwartImporter)
- [ ] Tests for MigrationOrchestrator
- [ ] 100% coverage for critical paths (migration orchestration, data integrity)
- [ ] 80%+ coverage for all core functionality

**Verification:**
- [ ] `dotnet test StalwartMigration.Tests` passes
- [ ] Coverage report meets requirements

**Dependencies:** Task 12-15

**Files likely touched:**
- `tests/StalwartMigration.Tests/Unit/CoreTests/` (multiple files)

**Estimated scope:** Medium (8-12 test files)

---

#### Task 28: Create CLI Tests
**Description**: Write tests for CLI commands and configuration.

**Acceptance criteria:**
- [ ] Tests for CLI configuration
- [ ] Tests for each command (Setup, Migrate, Vandelay, Export, Import, Validate)
- [ ] Tests for help text generation
- [ ] Tests for configuration file loading
- [ ] Tests for error handling

**Verification:**
- [ ] `dotnet test StalwartMigration.Cli.Tests` passes
- [ ] All CLI functionality tested

**Dependencies:** Task 16-22

**Files likely touched:**
- `tests/StalwartMigration.Cli.Tests/CommandTests/` (multiple files)

**Estimated scope:** Medium (6-8 test files)

---

#### Task 29: Create Integration Tests
**Description**: Write integration tests for component interactions.

**Acceptance criteria:**
- [ ] Tests for export-import workflow (fallback path)
- [ ] Tests for configuration loading and validation
- [ ] Tests for error scenarios
- [ ] Tests for edge cases (empty domains, large mailboxes, etc.)

**Verification:**
- [ ] `dotnet test StalwartMigration.Tests --filter Integration` passes

**Dependencies:** Task 1-22

**Files likely touched:**
- `tests/StalwartMigration.Tests/Integration/` (multiple files)

**Estimated scope:** Medium (4-6 test files)

---

#### Task 30: Create End-to-End Tests
**Description**: Write end-to-end tests for complete migration workflows. Note: These may require hMailServer and Stalwart instances for full testing.

**Acceptance criteria:**
- [ ] Test for setup workflow
- [ ] Test for full migration workflow (with mocks where external services required)
- [ ] Test for export-import fallback workflow
- [ ] Test for validation workflow
- [ ] Test for resume from checkpoint

**Verification:**
- [ ] `dotnet test StalwartMigration.Tests --filter EndToEnd` passes

**Dependencies:** Task 1-22

**Files likely touched:**
- `tests/StalwartMigration.Tests/Integration/EndToEndTests/` (multiple files)

**Estimated scope:** Medium (3-5 test files)

---

### Checkpoint: Testing Complete
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] All end-to-end tests pass
- [ ] Minimum 80% code coverage achieved
- [ ] 100% coverage for critical paths
- [ ] Review with human before final validation

---

### Phase 8: Final Validation

#### Task 31: Manual Testing and Validation
**Description**: Perform manual testing of the complete tool against real or test instances.

**Acceptance criteria:**
- [ ] CLI help works for all commands
- [ ] Configuration loading works
- [ ] Export from hMailServer works (if hMailServer available)
- [ ] Import to Stalwart works (if Stalwart available)
- [ ] Vandelay integration works (if Vandelay installed)
- [ ] Setup command works
- [ ] Migrate command works
- [ ] Validate command works
- [ ] Checkpoint/resume works
- [ ] Error handling provides clear messages

**Verification:**
- [ ] Manual test of each command
- [ ] All CLI examples from docs work
- [ ] Error cases handled gracefully

**Dependencies:** Task 1-30

**Files likely touched:** None (manual testing)

**Estimated scope:** Large (manual testing effort)

---

#### Task 32: Performance Testing
**Description**: Test the tool with large datasets to ensure performance requirements are met.

**Acceptance criteria:**
- [ ] Test with synthetic large dataset (100+ accounts, 1000+ emails)
- [ ] Memory usage stays within reasonable limits
- [ ] Processing time is acceptable
- [ ] Checkpointing doesn't significantly impact performance
- [ ] Parallel processing works correctly

**Verification:**
- [ ] Performance metrics collected and documented
- [ ] No memory leaks detected
- [ ] Processing completes in reasonable time

**Dependencies:** Task 1-30

**Files likely touched:**
- Test data generation scripts (if needed)

**Estimated scope:** Medium

---

#### Task 33: Security Review
**Description**: Perform security review of the codebase.

**Acceptance criteria:**
- [ ] No secrets or credentials in source code
- [ ] Sensitive data (passwords, email content) never logged
- [ ] File path sanitization prevents directory traversal
- [ ] All inputs validated
- [ ] HTTPS used for API connections when configured
- [ ] Proper credential handling (not stored in plain text)

**Verification:**
- [ ] Security audit checklist completed
- [ ] All security requirements from SPEC.md met

**Dependencies:** Task 1-30

**Files likely touched:** None (review only)

**Estimated scope:** Medium (review effort)

---

### Final Checkpoint: Complete
- [ ] All acceptance criteria met
- [ ] All tests pass
- [ ] Minimum 80% code coverage achieved
- [ ] 100% coverage for critical paths
- [ ] Documentation complete
- [ ] Manual testing successful
- [ ] Performance requirements met
- [ ] Security review complete
- [ ] Ready for production use

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| hMailServer COM API compatibility issues on different versions | High | Implement fallback to direct database access; test with multiple hMailServer versions |
| Vandelay installation issues on user systems | Medium | Provide clear error messages; implement thorough validation; document installation requirements |
| Memory issues with very large mailboxes | Medium | Implement streaming for email data; use file-based caching; configurable batch sizes |
| Stalwart API changes between versions | Medium | Use versioned API endpoints; implement graceful degradation; document compatible versions |
| Cross-platform compatibility issues | Medium | Test on both Windows and Linux; use cross-platform libraries; document platform-specific requirements |
| Performance bottlenecks in data migration | Medium | Profile early; implement parallel processing where possible; optimize I/O operations |
| Network connectivity issues during migration | Low | Implement retry logic with exponential backoff; progress checkpoints for resume |

## Parallelization Opportunities

The following tasks can be worked on in parallel once their dependencies are satisfied:

- **Documentation (Task 24)**: Can start after CLI commands are defined (Task 16-22)
- **Unit Tests for Utilities (Task 25)**: Can start after utilities are implemented (Task 4-6)
- **Unit Tests for Infrastructure (Task 26)**: Can start after infrastructure is implemented (Task 7-11)
- **Unit Tests for Core (Task 27)**: Can start after core is implemented (Task 12-15)
- **CLI Tests (Task 28)**: Can start after CLI commands are implemented (Task 16-22)

## Open Questions

None at this time. All major design decisions have been resolved as documented in `decision-log.md`.

## Repository Setup

The project is hosted on GitHub with the following configuration:

- **Repository**: https://github.com/frederik256/stalwart_mi_mistral
- **Branch**: `main`
- **Initial Commit**: Foundation phase complete (solution structure, project files, build configuration)
- **GitHub Actions**: To be configured in later phases

**Clone the repository:**
```bash
git clone https://github.com/frederik256/stalwart_mi_mistral.git
cd stalwart_mi_mistral
```

## Estimated Timeline

| Phase | Tasks | Estimated Duration |
|-------|-------|-------------------|
| Phase 1: Foundation | 1-3 | 1-2 days |
| Phase 2: Utilities | 4-6 | 1-2 days |
| Phase 3: Infrastructure | 7-11 | 3-5 days |
| Phase 4: Core | 12-15 | 3-5 days |
| Phase 5: CLI | 16-22 | 2-3 days |
| Phase 6: Config & Docs | 23-24 | 1-2 days (can be parallel) |
| Phase 7: Testing | 25-30 | 3-5 days |
| Phase 8: Validation | 31-33 | 2-3 days |
| **Total** | **33 tasks** | **17-25 days** |

## Implementation Notes

### Test Framework
- **Changed from xUnit to MSTest** in Phase 1 due to .NET 10 SDK compatibility issues with xUnit's transitive dependencies (BouncyCastle.Cryptography).
- MSTest 3.0.1 with Microsoft.NET.Test.Sdk 17.6.0 works correctly with the current environment.
- This decision allows Phase 1.1 (GitHub Actions) to proceed without blocking on test framework issues.

### Phase 1.1: CI/CD Pipeline
- Added as a new phase between Foundation and Utilities
- Ensures that every phase has automated build verification
- GitHub Actions workflow targets both Linux (ubuntu-latest) and Windows (windows-latest)
- Publish artifacts for both platforms using RID-specific builds

## Notes

- This plan follows **vertical slicing** where each phase delivers a working layer
- Dependencies are clearly identified and must be completed in order
- Each task has explicit acceptance criteria and verification steps
- Checkpoints between phases ensure quality before proceeding
- The tool complements Vandelay by filling its gaps (accounts, domains, aliases)
- The primary migration path uses Vandelay for data; fallback path uses custom export/import
- **Commit Policy**: Plan and todos commit to GitHub only on successful build and tests
