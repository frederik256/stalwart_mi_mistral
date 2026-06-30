# Implementation Plan: hMailServer to Stalwart Mail Server Migration Tool

## Repository
- **URL**: https://github.com/frederik256/stalwart_mi_mistral
- **Status**: Active (Foundation phase complete)
- **Branch**: `main`

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

### Phase 1.1: CI/CD Pipeline Setup
- Task 3.1-3.2: GitHub Actions workflow, verification
- **Purpose**: Ensure automated builds for Linux and Windows
- **Dependency**: Phase 1 must be complete

### Phase 2: Utility Layer
- Tasks 4-6: Logging, helpers, exceptions
- **Dependency**: Phase 1.1 must be complete

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
- [x] Tests pass with 0 errors
- [ ] Review with human before proceeding to CI/CD phase

---

### Phase 1.1: CI/CD Pipeline Setup

#### Task 3.1: Create GitHub Actions Build Pipeline
**Description**: Create a GitHub Actions workflow that builds the solution for both Linux and Windows platforms, runs tests, and publishes artifacts.

**Acceptance criteria:**
- [ ] `.github/workflows/build.yml` exists
- [ ] Workflow triggers on push to main branch
- [ ] Workflow triggers on pull requests to main branch
- [ ] Build job for Linux (ubuntu-latest) with rid: linux-x64
- [ ] Build job for Windows (windows-latest) with rid: win-x64
- [ ] Restore, build, and test steps for each platform
- [ ] Publish artifacts for both platforms
- [ ] Upload artifacts for download

**Verification:**
- [ ] Workflow file is syntactically valid
- [ ] Workflow can be triggered manually
- [ ] Build succeeds on GitHub Actions

**Dependencies:** Task 1-3

**Files likely touched:**
- `.github/workflows/build.yml`

**Estimated scope:** Small (1 file)

---

#### Task 3.2: Verify GitHub Actions Build is Green
**Description**: Ensure the GitHub Actions workflow runs successfully and produces green builds for both Linux and Windows.

**Acceptance criteria:**
- [ ] GitHub Actions workflow runs without errors
- [ ] All jobs complete successfully
- [ ] Build artifacts are produced for both platforms
- [ ] Tests pass on GitHub Actions

**Verification:**
- [ ] Check GitHub Actions tab in repository
- [ ] Verify green checkmark for latest commit
- [ ] Download and verify Linux artifact
- [ ] Download and verify Windows artifact

**Dependencies:** Task 3.1

**Files likely touched:** None (verification only)

**Estimated scope:** Small (verification effort)

---

### Checkpoint: CI/CD Pipeline Complete
- [ ] GitHub Actions workflow is configured
- [ ] Build succeeds on GitHub Actions
- [ ] Tests pass on GitHub Actions
- [ ] Artifacts are published for both Linux and Windows
- [ ] Review with human before proceeding to Utilities phase

---

### Phase 2: Utility Layer

#### Task 4: Implement Logging Infrastructure
**Description**: Create the logging configuration using Microsoft.Extensions.Logging as specified in SPEC.md. Support multi-level logging (Error, Warn, Info, Debug).

**Acceptance criteria:**
- [ ] `ILogger<T>` configured and available via DI
- [ ] Logging levels: Error, Warning, Information, Debug
- [ ] Console logger configured as default
- [ ] File logging support for migration logs
- [ ] Sensitive data (passwords, email content) is never logged

**Verification:**
- [ ] Unit test: Logger can be created and logs at all levels
- [ ] Build succeeds: `dotnet build`
- [ ] Manual check: Run simple app with logging output

**Dependencies:** Task 1, Task 2

**Files likely touched:**
- `Utilities/Logging/LoggerConfiguration.cs`
- `Utilities/Logging/LoggingExtensions.cs`
- `Utilities/Logging/SensitiveDataFilter.cs`

**Estimated scope:** Small (3 files)

---

#### Task 5: Implement Helper Classes and Extensions
**Description**: Create common helper classes and extension methods used throughout the application.

**Acceptance criteria:**
- [ ] String extensions (null/empty checks, validation)
- [ ] File system extensions (safe path operations)
- [ ] Collection extensions (batching, async enumeration)
- [ ] Email address validation helper
- [ ] Domain name validation helper
- [ ] Path sanitization to prevent directory traversal

**Verification:**
- [ ] Unit tests for all extension methods
- [ ] Build succeeds

**Dependencies:** Task 1, Task 2

**Files likely touched:**
- `Utilities/Extensions/StringExtensions.cs`
- `Utilities/Extensions/FileSystemExtensions.cs`
- `Utilities/Extensions/CollectionExtensions.cs`
- `Utilities/Helpers/EmailValidator.cs`
- `Utilities/Helpers/DomainValidator.cs`
- `Utilities/Helpers/PathSanitizer.cs`

**Estimated scope:** Medium (6 files)

---

#### Task 6: Implement Custom Exceptions
**Description**: Create custom exception classes for migration-specific errors as defined in SPEC.md.

**Acceptance criteria:**
- [ ] `MigrationException` base class with proper error context
- [ ] ConfigurationException for configuration errors
- [ ] ConnectionException for connection failures
- [ ] AuthenticationException for auth failures
- [ ] DataValidationException for data integrity issues
- [ ] All exceptions include helpful error messages with remediation suggestions

**Verification:**
- [ ] Exceptions can be thrown and caught properly
- [ ] Error messages are descriptive and actionable
- [ ] Build succeeds

**Dependencies:** Task 1, Task 2

**Files likely touched:**
- `Core/Exceptions/MigrationException.cs`
- `Core/Exceptions/ConfigurationException.cs`
- `Core/Exceptions/ConnectionException.cs`
- `Core/Exceptions/AuthenticationException.cs`
- `Core/Exceptions/DataValidationException.cs`

**Estimated scope:** Small (5 files)

---

### Checkpoint: Utility Layer Complete
- [ ] All utility classes compile
- [ ] Unit tests pass for utilities
- [ ] Logging works at all levels
- [ ] Review with human before proceeding to Infrastructure phase

---

### Phase 3: Infrastructure Layer

#### Task 7: Create Data Models
**Description**: Implement the core data models as defined in SPEC.md project structure. These are the foundational data structures used throughout the application.

**Acceptance criteria:**
- [ ] `Domain` class with Name, Id, Properties
- [ ] `Account` class with Name, Email, Id, Quota, Forwarding, etc.
- [ ] `EmailMessage` class with MessageId, Subject, From, To, Date, Body, Attachments
- [ ] `EmailAlias` class with Source, Destination
- [ ] `MigrationState` class for checkpoint/resume capability
- [ ] `MigrationProgress` class for progress reporting
- [ ] All models are immutable where appropriate
- [ ] All models have proper validation

**Verification:**
- [ ] Models can be serialized/deserialized to JSON
- [ ] Validation works correctly
- [ ] Unit tests for model validation
- [ ] Build succeeds

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

---

#### Task 8: Implement Stalwart REST API Client
**Description**: Create the Stalwart API integration layer as defined in SPEC.md. This client communicates with Stalwart's REST API v1.

**Acceptance criteria:**
- [ ] `StalwartClient` class with HTTP client configuration
- [ ] Authentication support (basic auth with credentials)
- [ ] Request timeout and retry logic (exponential backoff)
- [ ] `StalwartApiModels` for API request/response DTOs
- [ ] Domain CRUD operations (create, read, update, delete)
- [ ] Account CRUD operations
- [ ] Alias CRUD operations
- [ ] Health check endpoint
- [ ] Error handling for API responses

**Verification:**
- [ ] Unit tests with mocked HTTP client
- [ ] Build succeeds
- [ ] All API endpoints covered

**Dependencies:** Task 1, Task 2, Task 7

**Files likely touched:**
- `Infrastructure/Stalwart/StalwartClient.cs`
- `Infrastructure/Stalwart/StalwartApiModels.cs`
- `Infrastructure/Stalwart/AccountManager.cs`
- `Infrastructure/Stalwart/StalwartClientException.cs`

**Estimated scope:** Medium (4 files)

---

#### Task 9: Implement hMailServer COM API Client
**Description**: Create the hMailServer integration layer using COM API as primary method (SPEC.md line 36).

**Acceptance criteria:**
- [ ] `HMailServerClient` class with COM API integration
- [ ] Connection management to hMailServer
- [ ] Domain enumeration and retrieval
- [ ] Account enumeration and retrieval per domain
- [ ] Email message extraction
- [ ] Attachment extraction
- [ ] Alias extraction
- [ ] Account metadata extraction (quotas, forwarding, etc.)
- [ ] Fallback to direct database access if COM API fails

**Verification:**
- [ ] Build succeeds (note: runtime testing requires hMailServer)
- [ ] All required data can be extracted
- [ ] Unit tests with mocks

**Dependencies:** Task 1, Task 2, Task 7

**Files likely touched:**
- `Infrastructure/HMailServer/HMailServerClient.cs`
- `Infrastructure/HMailServer/HMailServerDatabase.cs`
- `Infrastructure/HMailServer/HMailServerException.cs`

**Estimated scope:** Medium (3 files)

---

#### Task 10: Implement Vandelay Integration
**Description**: Create the Vandelay subprocess integration as defined in SPEC.md. Vandelay is required for IMAP→JMAP data migration.

**Acceptance criteria:**
- [ ] `VandelayRunner` class for process execution
- [ ] `VandelayConfig` for configuration management
- [ ] `VandelayValidator` for installation validation
- [ ] `VandelayResultParser` for output parsing
- [ ] Support for `vandelay import imap` command
- [ ] Support for `vandelay export` command
- [ ] Support for `vandelay --version` validation
- [ ] Error handling for process exit codes
- [ ] Progress monitoring from Vandelay output

**Verification:**
- [ ] Unit tests with mock process execution
- [ ] Build succeeds
- [ ] Configuration generation works correctly

**Dependencies:** Task 1, Task 2, Task 7

**Files likely touched:**
- `Infrastructure/Vandelay/VandelayRunner.cs`
- `Infrastructure/Vandelay/VandelayConfig.cs`
- `Infrastructure/Vandelay/VandelayValidator.cs`
- `Infrastructure/Vandelay/VandelayResultParser.cs`
- `Infrastructure/Vandelay/VandelayResult.cs`

**Estimated scope:** Medium (5 files)

---

#### Task 11: Implement File System and Archive Management
**Description**: Create file system operations and ZIP archive management as defined in SPEC.md.

**Acceptance criteria:**
- [ ] `ArchiveManager` class for ZIP file operations
- [ ] Create ZIP archives (one per domain)
- [ ] Extract from ZIP archives
- [ ] Add files to existing archives
- [ ] Streaming support for large files
- [ ] Safe path operations (prevent directory traversal)
- [ ] Temporary file cleanup

**Verification:**
- [ ] Unit tests for archive operations
- [ ] Build succeeds
- [ ] Archive structure matches SPEC.md (JSON metadata, EML emails, binary attachments)

**Dependencies:** Task 1, Task 2, Task 5

**Files likely touched:**
- `Infrastructure/FileSystem/ArchiveManager.cs`
- `Infrastructure/FileSystem/ArchiveManagerException.cs`

**Estimated scope:** Small (2 files)

---

### Checkpoint: Infrastructure Layer Complete
- [ ] All infrastructure components compile
- [ ] Unit tests pass for infrastructure
- [ ] API clients are properly configured
- [ ] Vandelay integration is ready for testing
- [ ] Review with human before proceeding to Core phase

---

### Phase 4: Core Layer

#### Task 12: Implement Shared Services
**Description**: Create the shared services that support the migration workflow.

**Acceptance criteria:**
- [ ] `CompressionService` for ZIP compression/decompression
- [ ] `CheckpointService` for resumable migrations (every 30 seconds)
- [ ] `ValidationService` for data integrity validation
- [ ] All services support dependency injection
- [ ] All services are async
- [ ] Proper error handling in all services

**Verification:**
- [ ] Unit tests for all services
- [ ] Build succeeds
- [ ] Services can be injected and used

**Dependencies:** Task 1-11

**Files likely touched:**
- `Core/Services/CompressionService.cs`
- `Core/Services/CheckpointService.cs`
- `Core/Services/ValidationService.cs`
- `Core/Services/ICompressionService.cs`
- `Core/Services/ICheckpointService.cs`
- `Core/Services/IValidationService.cs`

**Estimated scope:** Medium (6 files)

---

#### Task 13: Implement Data Exporters
**Description**: Create the data export functionality for hMailServer as defined in SPEC.md. This is the fallback path when Vandelay is unavailable.

**Acceptance criteria:**
- [ ] `ExporterBase` abstract base class
- [ ] `HMailServerExporter` concrete implementation
- [ ] `ExportDomainAsync` method for per-domain export
- [ ] `ExportAllDomainsAsync` method for full export
- [ ] Export accounts to JSON
- [ ] Export emails to EML format
- [ ] Preserve binary attachments
- [ ] Package into ZIP archives (one per domain)
- [ ] Progress reporting
- [ ] Checkpoint support (via CheckpointService)

**Verification:**
- [ ] Unit tests for exporter
- [ ] Build succeeds
- [ ] Export produces correct archive structure

**Dependencies:** Task 7-12

**Files likely touched:**
- `Core/Exporters/ExporterBase.cs`
- `Core/Exporters/HMailServerExporter.cs`
- `Core/Exporters/IExporter.cs`
- `Core/Exporters/ExportResult.cs`

**Estimated scope:** Medium (4 files)

---

#### Task 14: Implement Data Importers
**Description**: Create the data import functionality for Stalwart as defined in SPEC.md. This is the fallback path when Vandelay is unavailable.

**Acceptance criteria:**
- [ ] `ImporterBase` abstract base class
- [ ] `StalwartImporter` concrete implementation
- [ ] `ImportDomainAsync` method for per-domain import
- [ ] `ImportAllDomainsAsync` method for full import
- [ ] Import accounts from JSON
- [ ] Import emails from EML format
- [ ] Handle binary attachments
- [ ] Extract from ZIP archives
- [ ] Progress reporting
- [ ] Checkpoint support
- [ ] Conflict resolution (merge strategy from SPEC.md)

**Verification:**
- [ ] Unit tests for importer
- [ ] Build succeeds
- [ ] Import handles all data types correctly

**Dependencies:** Task 7-13

**Files likely touched:**
- `Core/Importers/ImporterBase.cs`
- `Core/Importers/StalwartImporter.cs`
- `Core/Importers/IImporter.cs`
- `Core/Importers/ImportResult.cs`

**Estimated scope:** Medium (4 files)

---

#### Task 15: Implement Migration Orchestrator
**Description**: Create the main migration workflow coordinator as defined in SPEC.md. This orchestrates the entire migration process.

**Acceptance criteria:**
- [ ] `MigrationOrchestrator` class
- [ ] Setup phase: Connect to hMailServer and Stalwart, extract domains, create domains/accounts/aliases
- [ ] Data migration phase: Run Vandelay for each domain/account
- [ ] Fallback path: Use custom export/import when Vandelay unavailable
- [ ] Validation phase: Verify all data was migrated correctly
- [ ] Checkpoint creation every 30 seconds
- [ ] Resume from checkpoint capability
- [ ] Progress reporting throughout
- [ ] Error handling with detailed messages
- [ ] Configurable batch size
- [ ] Parallel processing for independent operations

**Verification:**
- [ ] Unit tests for orchestrator workflow
- [ ] Build succeeds
- [ ] Orchestrator can coordinate all phases

**Dependencies:** Task 7-14

**Files likely touched:**
- `Core/MigrationOrchestrator.cs`
- `Core/IMigrationOrchestrator.cs`
- `Core/MigrationOptions.cs`
- `Core/MigrationResult.cs`

**Estimated scope:** Medium (4 files)

---

### Checkpoint: Core Layer Complete
- [ ] All core components compile
- [ ] Unit tests pass for core functionality
- [ ] Migration workflow is complete
- [ ] Checkpoint/resume functionality works
- [ ] Review with human before proceeding to CLI phase

---

### Phase 5: CLI Layer

#### Task 16: Implement CLI Infrastructure
**Description**: Set up the CLI application infrastructure using System.CommandLine.

**Acceptance criteria:**
- [ ] Program.cs with proper entry point
- [ ] CLI configuration with System.CommandLine
- [ ] Dependency injection container setup
- [ ] Configuration file loading (hmailserver-config.json, stalwart-config.json)
- [ ] Logging configuration from CLI
- [ ] Error handling for CLI execution

**Verification:**
- [ ] `dotnet run --project StalwartMigration.Cli -- --help` works
- [ ] Build succeeds
- [ ] CLI shows help text

**Dependencies:** Task 1-15

**Files likely touched:**
- `CLI/Program.cs`
- `CLI/CLIConfiguration.cs`

**Estimated scope:** Small (2 files)

---

#### Task 17: Implement Setup Command
**Description**: Create the `setup` command for creating domains, accounts, and aliases in Stalwart (fills Vandelay's gap).

**Acceptance criteria:**
- [ ] `SetupCommand` class
- [ ] Options: --source, --target, --source-config, --target-config
- [ ] Flags: --create-domains, --create-accounts, --migrate-aliases
- [ ] Per-domain setup support: --domain
- [ ] Connects to hMailServer and extracts domain information
- [ ] Creates domains in Stalwart
- [ ] Creates accounts in Stalwart
- [ ] Migrates email aliases
- [ ] Progress reporting
- [ ] Checkpoint support

**Verification:**
- [ ] `stalwart-migrate setup --help` shows command help
- [ ] Unit tests for command
- [ ] Build succeeds

**Dependencies:** Task 16

**Files likely touched:**
- `CLI/Commands/SetupCommand.cs`

**Estimated scope:** Small (1 file)

---

#### Task 18: Implement Migrate Command
**Description**: Create the `migrate` command for full migration workflow.

**Acceptance criteria:**
- [ ] `MigrateCommand` class
- [ ] Options: --source, --target, --source-config, --target-config
- [ ] Flags: --setup-first, --run-vandelay, --resume
- [ ] Option: --last-checkpoint
- [ ] Orchestrates full migration: setup + Vandelay + validation
- [ ] Progress reporting
- [ ] Checkpoint creation and resume

**Verification:**
- [ ] `stalwart-migrate migrate --help` shows command help
- [ ] Unit tests for command
- [ ] Build succeeds

**Dependencies:** Task 16, Task 17

**Files likely touched:**
- `CLI/Commands/MigrateCommand.cs`

**Estimated scope:** Small (1 file)

---

#### Task 19: Implement Vandelay Command
**Description**: Create the `vandelay` subcommand for Vandelay-specific operations.

**Acceptance criteria:**
- [ ] `VandelayCommand` class with subcommands
- [ ] Subcommand: install - validate/install Vandelay
- [ ] Subcommand: check - check Vandelay installation
- [ ] Subcommand: run-import - run Vandelay import only
- [ ] Subcommand: run-export - run Vandelay export only
- [ ] Each subcommand has proper help text
- [ ] Error handling for Vandelay process

**Verification:**
- [ ] `stalwart-migrate vandelay --help` shows command help
- [ ] `stalwart-migrate vandelay install --help` works
- [ ] Unit tests for all subcommands
- [ ] Build succeeds

**Dependencies:** Task 16

**Files likely touched:**
- `CLI/Commands/VandelayCommand.cs`

**Estimated scope:** Small (1 file)

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
