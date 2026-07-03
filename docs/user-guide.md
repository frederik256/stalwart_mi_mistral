# Stalwart Migration Tool - User Guide

## Overview

The Stalwart Migration Tool is a command-line utility that enables you to migrate from **hMailServer** to **Stalwart Mail Server**. This tool complements [Vandelay](https://github.com/stalwartlabs/vandelay) by handling infrastructure migration (accounts, domains, aliases) that Vandelay cannot perform, while leveraging Vandelay for efficient data migration (emails, contacts, calendars) via IMAP to JMAP.

## Key Features

- **Complete Infrastructure Migration**: Domains, accounts, and email aliases
- **Data Migration**: Messages, contacts, calendars via Vandelay integration
- **Fallback Support**: Custom export/import when Vandelay is unavailable
- **Resume Capability**: Checkpoint support for interrupted migrations
- **Cross-Platform**: Works on Windows and Linux
- **Configurable**: JSON-based configuration files
- **Validation**: Pre- and post-migration validation

## Prerequisites

### Supported Environments

- **Operating Systems**: Windows 10/11, Windows Server 2016+, Linux (Ubuntu, CentOS, etc.)
- **Runtime**: .NET 8.0+
- **hMailServer**: Version 5.6+
- **Stalwart Mail Server**: v1.0+
- **Vandelay**: v1.0.5+ (recommended)

### Required Dependencies

- hMailServer with COM API access (Windows) or direct database access
- Stalwart Mail Server running in Docker
- Network connectivity between source and target servers

## Installation

### Option 1: Download Pre-built Binary

1. Download the latest release from [GitHub Releases](https://github.com/frederik256/stalwart_mi_mistral/releases)
2. Extract the archive
3. Run the executable: `StalwartMigration`

### Option 2: Build from Source

```bash
# Clone the repository
git clone https://github.com/frederik256/stalwart_mi_mistral.git
cd stalwart_mi_mistral

# Build the solution
dotnet build StalwartMigration.sln -c Release

# Run the tool
dotnet run --project src/StalwartMigration/StalwartMigration.csproj -- --help
```

### Option 3: Install as Global Tool

```bash
# Build and install as global tool
dotnet pack src/StalwartMigration/StalwartMigration.csproj -c Release
cd src/StalwartMigration/bin/Release
```

## Quick Start

### 1. Create Configuration Files

Copy the example configuration files and customize them:

```bash
# Copy example files
cp configs/hmailserver-config.example.json configs/hmailserver-config.json
cp configs/stalwart-config.example.json configs/stalwart-config.json

# Edit with your credentials
nano configs/hmailserver-config.json
nano configs/stalwart-config.json
```

### 2. Test Connectivity

```bash
# Test hMailServer connection
StalwartMigration validate --source-config configs/hmailserver-config.json --validate-target

# Test Stalwart connection
StalwartMigration validate --target-config configs/stalwart-config.json --validate-target
```

### 3. Run Setup Phase (Required)

```bash
# Setup domains, accounts, and aliases in Stalwart
StalwartMigration setup \
  --source-config configs/hmailserver-config.json \
  --target-config configs/stalwart-config.json \
  --create-domains \
  --create-accounts \
  --migrate-aliases
```

### 4. Run Full Migration

```bash
# Full migration: setup + Vandelay + validation
StalwartMigration migrate \
  --source-config configs/hmailserver-config.json \
  --target-config configs/stalwart-config.json \
  --setup-first \
  --run-vandelay
```

### 5. Verify Migration

```bash
# Validate migration results
StalwartMigration validate \
  --source-config configs/hmailserver-config.json \
  --target-config configs/stalwart-config.json
```

## Command Reference

See [Configuration Reference](configuration.md) for detailed command options.

### Available Commands

| Command | Description |
|---------|-------------|
| `setup` | Create domains, accounts, and aliases in Stalwart |
| `migrate` | Run full migration workflow |
| `vandelay` | Vandelay-specific operations |
| `export` | Export data from hMailServer (fallback) |
| `import` | Import data into Stalwart (fallback) |
| `validate` | Validate migration results |

### Common Options

All commands support the following configuration options:

- `--source` or `--source-config`: Path to hMailServer configuration file
- `--target` or `--target-config`: Path to Stalwart configuration file
- `--domain`: Specific domain(s) to process
- `--help` or `-h`: Show command help

## Migration Scenarios

### Scenario 1: Complete Migration (Recommended)

```bash
# Step 1: Setup infrastructure
StalwartMigration setup \
  --source-config hmailserver-config.json \
  --target-config stalwart-config.json \
  --create-domains \
  --create-accounts \
  --migrate-aliases

# Step 2: Migrate data with Vandelay
StalwartMigration migrate \
  --source-config hmailserver-config.json \
  --target-config stalwart-config.json \
  --run-vandelay

# Step 3: Validate
StalwartMigration validate \
  --source-config hmailserver-config.json \
  --target-config stalwart-config.json
```

### Scenario 2: Per-Domain Migration

```bash
# Migrate specific domains only
StalwartMigration migrate \
  --source-config hmailserver-config.json \
  --target-config stalwart-config.json \
  --domain example.com \
  --domain example.org
```

### Scenario 3: Fallback Migration (Without Vandelay)

```bash
# Export from hMailServer
StalwartMigration export \
  --source-config hmailserver-config.json \
  --output ./exported-data \
  --domain example.com

# Import into Stalwart
StalwartMigration import \
  --target-config stalwart-config.json \
  --input ./exported-data
```

### Scenario 4: Resume Interrupted Migration

```bash
# Resume from last checkpoint
StalwartMigration migrate \
  --source-config hmailserver-config.json \
  --target-config stalwart-config.json \
  --resume
```

## Troubleshooting

See [Troubleshooting Guide](troubleshooting.md) for common issues and solutions.

## Getting Help

- Check the [GitHub Issues](https://github.com/frederik256/stalwart_mi_mistral/issues) for known problems
- Review the [documentation](https://github.com/frederik256/stalwart_mi_mistral/tree/main/docs)
- For bugs, please file an issue with detailed information about your environment and the error

## License

This tool is provided as-is. Please review the license file for terms of use.

---

*Last Updated: 2026-07-02*
*Tool Version: 1.0*
