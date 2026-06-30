# Spec: hMailServer to Stalwart Mail Server Migration Tool

## Objective

### Purpose
Create a standalone CLI migration tool that enables users to migrate from hMailServer (Windows) to Stalwart Mail Server running in Docker containers on Windows or Linux hosts.

### Target Users
- System administrators managing email servers
- IT professionals migrating from hMailServer to Stalwart
- Organizations with 10+ years of email history and <100 users but medium email volume

### Success Criteria
- ✅ Successfully migrate all email data from hMailServer to Stalwart
- ✅ Migrate user accounts (without passwords - users will reset passwords)
- ✅ Support domain-by-domain migration and full migration
- ✅ Support incremental/partial migration with resume capability
- ✅ Validate data integrity through basic validation
- ✅ Generate zip archives containing extracted data (JSON for metadata, EML for emails, binaries unmodified)
- ✅ Import via CLI tool into Stalwart server running in Docker

## Tech Stack

### Primary Technologies
- **Language**: C# 10
- **Runtime**: .NET Core (cross-platform)
- **Target Platforms**: Windows and Linux
- **CLI Framework**: System.CommandLine or similar
- **Data Formats**: 
  - JSON for account/domain metadata
  - EML for email messages
  - Binary attachments preserved as-is
  - ZIP archives for intermediate storage

### Dependencies
- **hMailServer Integration**: hMailServer COM API (primary method)
- **Stalwart Integration**: Stalwart REST API (v1) as documented at https://github.com/stalwartlabs/stalwart/blob/main/api/v1/openapi.yml
- **Vandelay Integration**: Stalwart Labs Vandelay tool (https://github.com/stalwartlabs/vandelay) for JMAP-based data migration
- **Compression**: System.IO.Compression or similar for ZIP handling
- **Email Parsing**: MimeKit for EML processing (fallback when not using Vandelay)
- **Logging**: Microsoft.Extensions.Logging
- **Process Execution**: System.Diagnostics for running Vandelay as subprocess

### Target Frameworks
- .NET 6.0 LTS or .NET 7.0+ for cross-platform compatibility

### External Tool Requirements
- **Vandelay**: Required for IMAP→JMAP data migration (mail, contacts, calendars, etc.)
  - Installation: `cargo install --path .` from source or pre-built binaries
  - Version: v1.0.5 or later (recommended)
  - Platform: Must match target platform (Windows/Linux)

## Commands

### Build Commands
```bash
# Build the solution
dotnet build StalwartMigration.sln -c Release

# Build for specific platform
dotnet publish -c Release -r win-x64 --self-contained true
dotnet publish -c Release -r linux-x64 --self-contained true
```

### Development Commands
```bash
# Run in development
dotnet run --project StalwartMigration.Cli -- [arguments]

# Run tests
dotnet test StalwartMigration.Tests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings
```

### Package Commands
```bash
# Create distributable packages
dotnet pack -c Release

# Create platform-specific binaries
dotnet publish -c Release -r win-x64 -o ./dist/win-x64
dotnet publish -c Release -r linux-x64 -o ./dist/linux-x64
```

### CLI Usage Examples

#### Setup Phase (Domains, Accounts, Aliases)
```bash
# Show help
stalwart-migrate --help

# Setup domains and accounts in Stalwart (fills Vandelay's gap)
stalwart-migrate setup --source hmailserver --target stalwart \
  --source-config hmailserver-config.json \
  --target-config stalwart-config.json \
  --create-domains --create-accounts --migrate-aliases

# Setup specific domain only
stalwart-migrate setup --source hmailserver --domain example.com \
  --create-domain --create-accounts --migrate-aliases
```

#### Migration Phase (Using Vandelay)
```bash
# Full migration using Vandelay for data, our tool for setup
stalwart-migrate migrate --source hmailserver --target stalwart \
  --source-config hmailserver-config.json \
  --target-config stalwart-config.json \
  --setup-first --run-vandelay

# Vandelay-specific operations
stalwart-migrate vandelay --help
stalwart-migrate vandelay install      # Install/validate Vandelay
stalwart-migrate vandelay check        # Check Vandelay installation
stalwart-migrate vandelay run-import    # Run Vandelay import only
stalwart-migrate vandelay run-export    # Run Vandelay export only
```

#### Legacy Fallback (Without Vandelay)
```bash
# Export from hMailServer (fallback when Vandelay unavailable)
stalwart-migrate export --source hmailserver --config hmailserver-config.json --output ./migration-data

# Import to Stalwart (fallback)
stalwart-migrate import --target stalwart --config stalwart-config.json --input ./migration-data

# Export specific domain
stalwart-migrate export --source hmailserver --domain example.com --output ./example-migration
```

#### Validation and Utilities
```bash
# Resume failed migration
stalwart-migrate migrate --resume --last-checkpoint migration-checkpoint.json

# Validate migration
stalwart-migrate validate --source hmailserver --target stalwart \
  --source-config hmailserver-config.json \
  --target-config stalwart-config.json

# Test Stalwart API connectivity
stalwart-migrate --validate-target --target-config stalwart-config.json
```

## Project Structure

```
stalwart-migration/
├── src/
│   └── StalwartMigration/
│       ├── CLI/                          # Command-line interface
│       │   ├── Commands/                # CLI command definitions
│       │   │   ├── SetupCommand.cs      # Setup domains/accounts/aliases
│       │   │   ├── MigrateCommand.cs     # Full migration workflow
│       │   │   ├── VandelayCommand.cs   # Vandelay-specific operations
│       │   │   ├── ExportCommand.cs     # Legacy export (fallback)
│       │   │   ├── ImportCommand.cs     # Legacy import (fallback)
│       │   │   └── ValidateCommand.cs   # Migration validation
│       │   ├── Program.cs               # Entry point
│       │   └── CLIConfiguration.cs      # CLI setup and configuration
│       │
│       ├── Core/                        # Core migration logic
│       │   ├── MigrationOrchestrator.cs # Main migration workflow
│       │   ├── Exporters/              # Data exporters
│       │   │   ├── HMailServerExporter.cs
│       │   │   └── ExporterBase.cs
│       │   ├── Importers/              # Data importers
│       │   │   ├── StalwartImporter.cs
│       │   │   └── ImporterBase.cs
│       │   ├── Models/                 # Data models
│       │   │   ├── Account.cs
│       │   │   ├── Domain.cs
│       │   │   ├── EmailMessage.cs
│       │   │   └── MigrationState.cs
│       │   ├── Services/               # Shared services
│       │   │   ├── CompressionService.cs
│       │   │   ├── CheckpointService.cs  # For resumable migrations
│       │   │   └── ValidationService.cs
│       │   └── Exceptions/             # Custom exceptions
│       │       └── MigrationException.cs
│       │
│       ├── Infrastructure/             # External integrations
│       │   ├── HMailServer/            # hMailServer access
│       │   │   ├── HMailServerClient.cs
│       │   │   └── HMailServerDatabase.cs
│       │   ├── Stalwart/               # Stalwart API integration
│       │   │   ├── StalwartClient.cs
│       │   │   ├── StalwartApiModels.cs
│       │   │   └── AccountManager.cs   # Account/alias creation
│       │   ├── Vandelay/               # Vandelay integration
│       │   │   ├── VandelayRunner.cs    # Process execution
│       │   │   ├── VandelayConfig.cs    # Configuration
│       │   │   ├── VandelayValidator.cs # Installation validation
│       │   │   └── VandelayResultParser.cs
│       │   └── FileSystem/             # File system operations
│       │       └── ArchiveManager.cs
│       │
│       └── Utilities/                  # Helper utilities
│           ├── Logging/               # Logging configuration
│           ├── Extensions/            # Extension methods
│           └── Helpers/               # Helper classes
│
├── tests/
│   ├── StalwartMigration.Tests/       # Unit tests
│   │   ├── Unit/
│   │   │   ├── CoreTests/
│   │   │   ├── InfrastructureTests/
│   │   │   └── UtilitiesTests/
│   │   └── Integration/
│   │       ├── ExportTests/
│   │       ├── ImportTests/
│   │       └── EndToEndTests/
│   │
│   └── StalwartMigration.Cli.Tests/    # CLI-specific tests
│       └── CommandTests/
│
├── docs/
│   ├── user-guide.md                  # User documentation
│   ├── configuration.md              # Configuration reference
│   ├── migration-process.md          # Migration process guide
│   ├── docker-setup.md               # Docker container setup guide
│   ├── vandelay-integration.md       # Vandelay setup and integration
│   ├── account-migration.md         # Account/domain/alias migration guide
│   └── troubleshooting.md            # Troubleshooting guide
│
├── configs/
│   ├── hmailserver-config.example.json
│   └── stalwart-config.example.json
│
├── SPEC.md                           # This specification
├── StalwartMigration.sln             # Solution file
├── Directory.Build.props              # Common build properties
└── README.md                         # Project overview
```

## Code Style

### General Principles
- Follow .NET coding conventions and style guides
- Use async/await for all I/O operations
- Prefer immutable data structures where possible
- Use dependency injection for testability
- Follow SOLID principles

### Naming Conventions
- **Classes**: PascalCase (`HMailServerExporter`, `MigrationOrchestrator`)
- **Interfaces**: IPascalCase (`IExporter`, `IImporter`)
- **Methods**: PascalCase (`ExportDomainAsync`, `ValidateMigration`)
- **Properties**: PascalCase (`DomainName`, `AccountCount`)
- **Parameters**: camelCase (`domainName`, `exportPath`)
- **Local Variables**: camelCase (`exportedAccounts`, `migrationState`)
- **Constants**: UPPER_SNAKE_CASE (`MAX_RETRIES`, `DEFAULT_TIMEOUT`)

### File Organization
- One class per file
- File names match class names
- Group related files in appropriate directories

### Code Formatting
- Use 4 spaces for indentation (no tabs)
- Opening braces on same line
- Private fields with underscore prefix (`_fieldName`)
- Line length: 120 characters maximum
- Use null-forgiving operator (`!`) sparingly with proper null checks

### Example Code Snippet

```csharp
public interface IExporter
{
    Task<ExportResult> ExportDomainAsync(
        Domain domain,
        string outputPath,
        CancellationToken cancellationToken = default);
    
    Task<ExportResult> ExportAllDomainsAsync(
        string outputPath,
        IProgress<MigrationProgress> progress,
        CancellationToken cancellationToken = default);
}

public class HMailServerExporter : IExporter
{
    private readonly IHMailServerClient _hmailServerClient;
    private readonly ICompressionService _compressionService;
    private readonly ILogger<HMailServerExporter> _logger;
    
    public HMailServerExporter(
        IHMailServerClient hmailServerClient,
        ICompressionService compressionService,
        ILogger<HMailServerExporter> logger)
    {
        _hmailServerClient = hmailServerClient ?? throw new ArgumentNullException(nameof(hmailServerClient));
        _compressionService = compressionService ?? throw new ArgumentNullException(nameof(compressionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<ExportResult> ExportDomainAsync(
        Domain domain,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        if (domain == null)
            throw new ArgumentNullException(nameof(domain));
        
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("Output path cannot be null or whitespace.", nameof(outputPath));
        
        var domainPath = Path.Combine(outputPath, domain.Name);
        Directory.CreateDirectory(domainPath);
        
        var accounts = await _hmailServerClient.GetAccountsByDomainAsync(domain.Name, cancellationToken);
        var result = new ExportResult { Domain = domain.Name };
        
        foreach (var account in accounts)
        {
            var accountData = await ExportAccountAsync(account, domainPath, cancellationToken);
            result.Accounts.Add(accountData);
        }
        
        _logger.LogInformation("Exported domain {Domain} with {Count} accounts", domain.Name, result.Accounts.Count);
        
        return result;
    }
    
    private async Task<AccountExportResult> ExportAccountAsync(
        Account account,
        string domainPath,
        CancellationToken cancellationToken)
    {
        // Implementation details...
    }
}
```

## Testing Strategy

### Test Framework
- **Unit Tests**: xUnit.net
- **Mocking**: Moq or NSubstitute
- **Assertions**: FluentAssertions
- **Integration Tests**: xUnit.net with real dependencies where needed
- **Coverage**: Coverlet for code coverage

### Test Structure
- Unit tests for individual classes and methods
- Integration tests for component interactions
- End-to-end tests for complete migration workflows

### Test Locations
- Unit tests: `/tests/StalwartMigration.Tests/Unit/`
- Integration tests: `/tests/StalwartMigration.Tests/Integration/`
- CLI tests: `/tests/StalwartMigration.Cli.Tests/`

### Coverage Requirements
- **Minimum Coverage**: 80% code coverage for core functionality
- **Critical Paths**: 100% coverage for migration orchestration and data integrity operations
- **Test Types**: 
  - Happy path testing
  - Error case testing
  - Edge case testing
  - Performance testing for large datasets

### Test Data Strategy
- Use synthetic test data for unit tests
- Use real-world-like data structures for integration tests
- Include edge cases: empty domains, accounts with no emails, very large mailboxes
- Mock external dependencies (hMailServer, Stalwart API) for unit tests

### Example Test

```csharp
[Fact]
public async Task ExportDomainAsync_WithValidDomain_ExportsAllAccounts()
{
    // Arrange
    var mockClient = new Mock<IHMailServerClient>();
    var mockCompression = new Mock<ICompressionService>();
    var logger = new Mock<ILogger<HMailServerExporter>>();
    
    var domain = new Domain { Name = "example.com" };
    var accounts = new List<Account> 
    {
        new Account { Name = "user1@example.com", Id = 1 },
        new Account { Name = "user2@example.com", Id = 2 }
    };
    
    mockClient.Setup(c => c.GetAccountsByDomainAsync(domain.Name, It.IsAny<CancellationToken>()))
             .ReturnsAsync(accounts);
    
    var exporter = new HMailServerExporter(
        mockClient.Object, 
        mockCompression.Object, 
        logger.Object);
    
    // Act
    var result = await exporter.ExportDomainAsync(domain, "./test-output");
    
    // Assert
    result.Should().NotBeNull();
    result.Domain.Should().Be(domain.Name);
    result.Accounts.Should().HaveCount(2);
    
    mockClient.Verify(c => c.GetAccountsByDomainAsync(domain.Name, It.IsAny<CancellationToken>()), Times.Once);
}
```

### Test Execution
- Run all tests on every build
- Include tests in CI/CD pipeline
- Fail build on test failures
- Generate coverage reports for each build

## Boundaries

### Always Do
- ✅ Validate all inputs and parameters
- ✅ Use async/await for I/O operations
- ✅ Implement proper error handling and logging
- ✅ Write tests for all new functionality
- ✅ Update documentation when functionality changes
- ✅ Use dependency injection for testability
- ✅ Implement proper cancellation support
- ✅ Validate data integrity during migration
- ✅ Create checkpoints for resumable migrations
- ✅ Clean up temporary files and resources

### Ask First
- 🔄 Database schema changes or direct database access
- 🔄 Adding new external dependencies
- 🔄 Changing the ZIP archive format or structure
- 🔄 Modifying the CLI interface or command structure
- 🔄 Changes to the migration workflow order
- 🔄 Security-related changes (authentication, encryption)
- 🔄 Performance optimizations that change the data processing approach
- 🔄 Cross-platform compatibility changes

### Never Do
- ❌ Commit secrets, API keys, or credentials to source control
- ❌ Hardcode configuration values (use config files)
- ❌ Modify hMailServer or Stalwart server source code
- ❌ Assume all email data is UTF-8 encoded
- ❌ Skip error handling for I/O operations
- ❌ Use synchronous I/O in performance-critical paths
- ❌ Remove or disable existing tests without approval
- ❌ Bypass validation checks
- ❌ Store sensitive data in plain text
- ❌ Make breaking changes to the CLI interface without versioning

## Configuration

### hMailServer Configuration
```json
{
  "host": "localhost",
  "port": 5000,
  "username": "administrator",
  "password": "secure-password",
  "useComApi": true,
  "databaseConnectionString": "Server=localhost;Database=hmailserver;User Id=sa;Password=password;"
}
```

### Stalwart Configuration
```json
{
  "apiUrl": "http://localhost:8080",
  "username": "admin",
  "password": "secure-password",
  "timeoutSeconds": 30,
  "maxRetries": 3
}
```

## Migration Workflow

### Architecture Overview
Our tool **complements Vandelay** by handling what it cannot: **accounts, domains, and aliases**. Vandelay handles the heavy lifting of **data migration** (mail, contacts, calendars, etc.) via IMAP→JMAP.

### Processing Configuration
- **Processing Mode**: Sequential (domains processed one at a time)
- **Batch Scope**: Per domain (all data for one domain processed together)
- **Checkpoint Frequency**: Every 30 seconds of runtime
- **Vandelay Integration**: Optional but recommended for data migration

### Phase 1: Setup (Fills Vandelay's Gaps)
1. **Connect to hMailServer** using COM API
2. **Connect to Stalwart** using REST API
3. **Extract domain information** from hMailServer
4. **For each domain**:
   - **Create domain in Stalwart** (Vandelay cannot do this)
   - **Extract all accounts** from hMailServer
   - **For each account**:
     - **Create account in Stalwart** (Vandelay cannot do this)
     - **Extract and migrate email aliases** (Vandelay cannot do this)
     - **Migrate account metadata** (quotas, forwarding, etc.)
5. **Create checkpoint** every 30 seconds during processing

### Phase 2: Data Migration (Vandelay-Powered)
1. **Validate Vandelay installation**
2. **For each domain/account**:
   - **Run Vandelay IMAP import**: `vandelay import imap --url imaps://hmailserver --auth-basic user@domain.com archive.sqlite`
   - **Run Vandelay JMAP export**: `vandelay export --url https://stalwart --auth-basic user@domain.com archive.sqlite`
3. **Monitor Vandelay progress** and handle errors
4. **Update checkpoint** every 30 seconds during processing

### Phase 3: Fallback (Without Vandelay)
1. **Connect to hMailServer** using COM API
2. **Extract domain information** - get list of all domains
3. **For each domain**:
   - Extract all accounts
   - For each account:
     - Extract account metadata (name, email, quotas, etc.) → JSON
     - Extract all email messages → EML format
     - Extract attachments → preserve as binary
4. **Package data** into ZIP archives (one per domain)
5. **Create checkpoint** every 30 seconds during processing
6. **Import to Stalwart** using our custom import logic

### Phase 4: Validation
1. **Verify all domains** were created in Stalwart
2. **Verify all accounts** were created in Stalwart
3. **Verify all aliases** were migrated correctly
4. **Verify Vandelay data migration** (if used)
5. **Perform basic data integrity checks**

### Phase 3: Validation (Optional)
1. **Compare counts** between source and target
2. **Verify data integrity** through checksums
3. **Test sample data** accessibility
4. **Generate validation report**

## Error Handling

### Error Categories
1. **Configuration Errors** - Invalid or missing configuration
2. **Connection Errors** - Cannot connect to hMailServer or Stalwart
3. **Authentication Errors** - Invalid credentials
4. **Data Errors** - Corrupted or incompatible data
5. **Permission Errors** - Insufficient permissions
6. **Resource Errors** - Out of memory, disk space, etc.

### Error Recovery
- Implement retry logic with exponential backoff for transient errors
- Provide detailed error messages with remediation suggestions
- Support resume from last checkpoint for failed migrations
- Log all errors with context for troubleshooting

## Performance Considerations

### Large Data Handling
- Process data in batches (configurable batch size)
- Use streaming for email data to avoid memory issues
- Implement progress reporting for long-running operations
- Support parallel processing for independent operations

### Memory Management
- Limit concurrent operations based on available memory
- Use file-based caching for large datasets
- Clean up temporary files promptly

### Network Considerations
- Implement request timeouts
- Support retry logic for failed network operations
- Use compression for API calls where possible

## Security Considerations

### Data Protection
- Never log sensitive information (passwords, email content)
- Use secure connections (HTTPS) for API calls
- Encrypt sensitive configuration data
- Implement proper credential handling

### Validation
- Validate all inputs from external sources
- Sanitize file paths to prevent directory traversal
- Validate email addresses and domain names
- Check file sizes and types before processing

## Vandelay Integration

### Purpose
Vandelay is a powerful JMAP importer-exporter tool from Stalwart Labs that handles the complex data migration (mail, contacts, calendars, etc.) between IMAP and JMAP servers. Our tool integrates Vandelay as a subprocess to leverage its capabilities while filling the critical gaps (accounts, domains, aliases) that Vandelay cannot handle.

### Integration Approach
- **Subprocess Execution**: Run Vandelay as an external process
- **Configuration Management**: Generate appropriate Vandelay configuration files
- **Error Handling**: Parse Vandelay output and handle errors gracefully
- **Progress Monitoring**: Track Vandelay progress and provide unified progress reporting
- **Fallback Mode**: Provide our own data migration when Vandelay is unavailable

### Vandelay Capabilities Utilized
- **IMAP Import**: Import mail data from hMailServer via IMAP
- **JMAP Export**: Export data to Stalwart via JMAP
- **Convergent Operations**: Resumable migrations
- **Multi-protocol Support**: Future-proof for other protocols
- **SQLite Archive**: Intermediate storage format

### Vandelay Gaps Filled by Our Tool
| Gap | Vandelay Limitation | Our Solution |
|-----|---------------------|--------------|
| **Account Creation** | Can only work with existing accounts | Create accounts via Stalwart API/database |
| **Domain Management** | No domain creation support | Create domains via Stalwart API/database |
| **Email Aliases** | No alias support in JMAP standard | Extract from hMailServer, create in Stalwart |
| **Account Metadata** | Only basic identity migration | Migrate quotas, forwarding, settings |
| **Orchestration** | Manual process required | Automated workflow with error handling |

### Requirements
- **Vandelay Installation**: Must be installed and accessible in PATH
- **Version Compatibility**: v1.0.5 or later recommended
- **Platform Matching**: Vandelay binary must match target platform (Windows/Linux)
- **Permissions**: Execute permissions for Vandelay binary

### Error Handling
- **Installation Validation**: Check Vandelay is installed and accessible
- **Version Checking**: Warn if Vandelay version is outdated
- **Exit Code Handling**: Properly handle Vandelay process exit codes
- **Output Parsing**: Parse Vandelay JSON output and error messages
- **Fallback**: Gracefully fall back to our own migration if Vandelay fails

## Docker and Container Management

### Scope
**API Only Approach**: The tool interacts with Stalwart exclusively through its REST API. Users are responsible for container lifecycle management.

### User Experience
Before running the migration tool, users must ensure Stalwart is running and accessible:

```bash
# User must start Stalwart container first:
docker run -d -p 8080:8080 -v stalwart-data:/var/lib/stalwart stalwartlabs/stalwart

# Then run our migration tool:
stalwart-migrate import --target stalwart --config stalwart-config.json --input ./migration-data
```

### Tool Capabilities
- ✅ Connect to Stalwart REST API at specified URL
- ✅ Validate API connectivity before starting migration
- ✅ Handle authentication and API errors gracefully  
- ✅ Provide clear error messages for API connectivity issues
- ✅ Optional `--validate-target` flag to test connectivity

### What Users Must Handle
- Starting Stalwart container before migration
- Ensuring container has proper ports mapped
- Managing container persistence/volumes
- Upgrading Stalwart versions
- Docker network configuration

## Vandelay Integration Examples

### C# Vandelay Process Runner Example

```csharp
public interface IVandelayRunner
{
    Task<VandelayResult> RunImportAsync(VandelayImportConfig config, CancellationToken cancellationToken = default);
    Task<VandelayResult> RunExportAsync(VandelayExportConfig config, CancellationToken cancellationToken = default);
    Task<bool> ValidateInstallationAsync(CancellationToken cancellationToken = default);
    Task<string> GetVersionAsync(CancellationToken cancellationToken = default);
}

public class VandelayRunner : IVandelayRunner
{
    private readonly ILogger<VandelayRunner> _logger;
    private readonly IProcessExecutor _processExecutor;
    
    public VandelayRunner(ILogger<VandelayRunner> logger, IProcessExecutor processExecutor)
    {
        _logger = logger;
        _processExecutor = processExecutor;
    }
    
    public async Task<VandelayResult> RunImportAsync(VandelayImportConfig config, CancellationToken cancellationToken = default)
    {
        var arguments = BuildImportArguments(config);
        
        _logger.LogInformation("Running Vandelay import: vandelay {Arguments}", arguments);
        
        var result = await _processExecutor.RunAsync("vandelay", arguments, cancellationToken);
        
        if (result.ExitCode != 0)
        {
            _logger.LogError("Vandelay import failed with exit code {ExitCode}: {ErrorOutput}", 
                result.ExitCode, result.StandardError);
            return VandelayResult.Failed(result.ExitCode, result.StandardError);
        }
        
        _logger.LogInformation("Vandelay import completed successfully");
        return VandelayResult.Success(result.StandardOutput);
    }
    
    public async Task<bool> ValidateInstallationAsync(CancellationToken cancellationToken = default)
    {
        var result = await _processExecutor.RunAsync("vandelay", "--version", cancellationToken);
        return result.ExitCode == 0;
    }
    
    private string BuildImportArguments(VandelayImportConfig config)
    {
        var args = new List<string> { "import", "imap" };
        
        args.AddRange(new[] {
            "--url", config.ImapUrl,
            "--auth-basic", config.Username,
            config.OutputPath
        });
        
        if (!string.IsNullOrEmpty(config.Password))
        {
            args.AddRange(new[] { "--auth-password", config.Password });
        }
        
        if (config.IncludeFolders?.Any() == true)
        {
            args.AddRange(config.IncludeFolders.Select(f => $"--include {f}"));
        }
        
        if (config.DryRun)
        {
            args.Add("--dry-run");
        }
        
        return string.Join(" ", args);
    }
}

public class VandelayResult
{
    public bool Success { get; }
    public int ExitCode { get; }
    public string Output { get; }
    public string Error { get; }
    public DateTimeOffset CompletedAt { get; }
    
    public static VandelayResult Success(string output) => 
        new VandelayResult(true, 0, output, string.Empty, DateTimeOffset.UtcNow);
    
    public static VandelayResult Failed(int exitCode, string error) => 
        new VandelayResult(false, exitCode, string.Empty, error, DateTimeOffset.UtcNow);
    
    private VandelayResult(bool success, int exitCode, string output, string error, DateTimeOffset completedAt)
    {
        Success = success;
        ExitCode = exitCode;
        Output = output;
        Error = error;
        CompletedAt = completedAt;
    }
}
```

## Resolved Design Decisions

### ✅ hMailServer Access
**Decision**: Use COM API for hMailServer access
**Rationale**: More reliable and officially supported method for hMailServer integration

### ✅ Processing Strategy  
**Decision**: Sequential processing, per domain batches
**Rationale**: Ensures data consistency and predictable resource usage

### ✅ Checkpoint Strategy
**Decision**: Create checkpoints every 30 seconds of runtime
**Rationale**: Balances resume capability with performance overhead

### ✅ Archive Structure
**Decision**: One ZIP archive per domain
**Rationale**: Provides good balance between file manageability and migration atomicity

### ✅ Conflict Resolution
**Decision**: Merge data strategy (non-destructive)
- **Domain Conflict**: Update existing domain settings, merge accounts
- **Account Conflict**: Merge emails (skip duplicates by Message-ID), update metadata if missing  
- **Email Conflict**: Skip emails with duplicate Message-ID in target, log for review

### ✅ Logging Level
**Decision**: Multi-level logging (Error, Warn, Info, Debug)
**Rationale**: Provides flexibility for different user needs and troubleshooting scenarios

### ✅ Docker Scope
**Decision**: API only approach
**Rationale**: Maintains separation of concerns, keeps tool focused on data migration, more portable across different Stalwart installations

## Open Questions

*None - all major design decisions have been resolved*

## Assumptions Made

1. hMailServer is accessible and running during migration
2. Stalwart server is running and API is accessible at specified URL
3. User has administrative access to both systems
4. Migration can be done while both systems are online (no downtime required)
5. Email passwords will not be migrated - users will reset passwords in Stalwart
6. SSL/TLS is not required for local API connections (can be configured)
7. The tool will be run on the same machine as both servers or have network access
8. hMailServer COM API is available and functional on the source system
9. Users will handle Docker container lifecycle management externally

## Versioning

- **Tool Version**: Follow semantic versioning (Major.Minor.Patch)
- **Configuration**: Configuration files may change between major versions
- **CLI Interface**: CLI commands and options maintain backward compatibility within major versions

## Success Metrics

- ✅ All domains migrated successfully
- ✅ All user accounts created in Stalwart
- ✅ All emails migrated and accessible
- ✅ All attachments preserved correctly
- ✅ Basic validation passes (counts match, sample data accessible)
- ✅ Migration can be resumed after interruption
- ✅ Tool runs on both Windows and Linux
- ✅ Clear error messages for troubleshooting