# Vandelay Integration Guide

This guide explains how to set up and use Vandelay with the Stalwart Migration Tool for efficient data migration.

## Overview

[Vandelay](https://github.com/stalwartlabs/vandelay) is a powerful JMAP importer-exporter tool from Stalwart Labs that handles the complex data migration (mail, contacts, calendars, etc.) between IMAP and JMAP servers. Our migration tool integrates Vandelay as a subprocess to leverage its capabilities while filling the critical gaps (accounts, domains, aliases) that Vandelay cannot handle.

**Architecture Summary:**
- **Our Tool**: Handles infrastructure migration (domains, accounts, aliases)
- **Vandelay**: Handles data migration (emails, contacts, calendars) via IMAP to JMAP
- **Integration**: Our tool runs Vandelay as an external subprocess with proper configuration

## Prerequisites

- Vandelay v1.0.5 or later (recommended)
- Rust toolchain (for building from source)
- Cargo package manager
- Platform matching: Vandelay binary must match your target platform (Windows/Linux)

## Installation

### Option 1: Install Pre-built Binary

Download the latest Vandelay release from [GitHub Releases](https://github.com/stalwartlabs/vandelay/releases):

**Linux:**
```bash
# Download the latest release
wget https://github.com/stalwartlabs/vandelay/releases/latest/download/vandelay-linux-x86_64.tar.gz

# Extract the archive
tar -xzf vandelay-linux-x86_64.tar.gz

# Move to PATH
sudo mv vandelay /usr/local/bin/

# Make executable
sudo chmod +x /usr/local/bin/vandelay

# Verify installation
vandelay --version
```

**Windows:**
```powershell
# Download the latest release
Invoke-WebRequest -Uri "https://github.com/stalwartlabs/vandelay/releases/latest/download/vandelay-windows-x86_64.zip" -OutFile "vandelay.zip"

# Extract the archive
Expand-Archive -Path "vandelay.zip" -DestinationPath "."

# Move to PATH (or add to existing PATH)
Move-Item -Path "vandelay.exe" -Destination "C:\Windows\System32\"

# Verify installation
vandelay --version
```

### Option 2: Build from Source

```bash
# Clone the Vandelay repository
git clone https://github.com/stalwartlabs/vandelay.git
cd vandelay

# Build in release mode
cargo build --release

# The binary will be at target/release/vandelay
# Move it to your PATH
cp target/release/vandelay /usr/local/bin/

# Verify
vandelay --version
```

### Option 3: Use Docker (Alternative)

If you don't want to install Vandelay directly, you can use it via Docker:

```bash
# Pull the Vandelay Docker image
docker pull stalwartlabs/vandelay:latest

# Run Vandelay in a container
docker run --rm -it \
  -v $(pwd)/data:/data \
  stalwartlabs/vandelay:latest \
  vandelay --help
```

## Configuration

### Vandelay Configuration File

The migration tool can generate a Vandelay configuration file or you can create one manually:

```toml
# vandelay-config.toml
[source]
type = "imap"
host = "hmailserver.example.com"
port = 993
ssl = true
username = "user@domain.com"
password = "password"

[target]
type = "jmap"
url = "http://localhost:8080"
username = "admin"
password = "secure-password"

[options]
workers = 4
batch_size = 100
retry_count = 3
timeout = 300
```

### Migration Tool Configuration

In your `hmailserver-config.json` and `stalwart-config.json`, the migration tool needs:

```json
{
  "vandelay": {
    "enabled": true,
    "executable": "vandelay",
    "configFile": "vandelay-config.toml",
    "timeoutSeconds": 300,
    "maxRetries": 3
  }
}
```

## Integration Features

### Subprocess Execution

The migration tool runs Vandelay as an external process with:

- **Proper environment**: Inherits environment variables and working directory
- **Error handling**: Captures exit codes, stdout, and stderr
- **Timeout management**: Configurable timeout per operation
- **Progress monitoring**: Tracks Vandelay progress in real-time

### Configuration Management

The tool provides several ways to configure Vandelay:

1. **Automatic Configuration**: Based on your hMailServer and Stalwart settings
2. **Custom Configuration File**: Use an existing Vandelay configuration
3. **CLI Overrides**: Override specific Vandelay settings via command line

### Error Handling

The tool handles Vandelay errors gracefully:

- **Exit Code Checking**: Properly interprets Vandelay exit codes
- **Output Parsing**: Parses Vandelay JSON output and error messages
- **Retry Logic**: Automatic retries for transient failures
- **Fallback Mode**: Gracefully falls back to custom migration if Vandelay fails

## Usage with Migration Tool

### Basic Migration with Vandelay

```bash
# Full migration using Vandelay for data, our tool for setup
stalwart-migrate migrate \
  --source hmailserver \
  --target stalwart \
  --source-config hmailserver-config.json \
  --target-config stalwart-config.json \
  --run-vandelay \
  --setup-first
```

### Vandelay-Specific Commands

```bash
# Show Vandelay help
stalwart-migrate vandelay --help

# Install/validate Vandelay installation
stalwart-migrate vandelay install

# Check Vandelay installation
stalwart-migrate vandelay check

# Run Vandelay import only
stalwart-migrate vandelay run-import \
  --config vandelay-config.toml

# Run Vandelay export only
stalwart-migrate vandelay run-export \
  --config vandelay-config.toml

# Run Vandelay with custom arguments
stalwart-migrate vandelay run \
  --import \
  --source-imap \
  --source-host hmailserver.example.com \
  --target-jmap \
  --target-url http://localhost:8080
```

## Vandelay Command Reference

### Import Commands

```bash
# IMAP to JMAP import (most common)
vandelay import imap --url imaps://hmailserver.example.com --auth-basic user@domain.com archive.sqlite

# With SSL disabled
vandelay import imap --url imap://hmailserver.example.com:143 --auth-basic user@domain.com archive.sqlite

# With custom connection settings
vandelay import imap \
  --url imaps://hmailserver.example.com \
  --auth-basic user@domain.com \
  --insecure \
  --timeout 300 \
  archive.sqlite
```

### Export Commands

```bash
# JMAP to SQLite export
vandelay export --url https://stalwart.example.com --auth-basic admin archive.sqlite

# With specific account
vandelay export \
  --url https://stalwart.example.com \
  --auth-basic admin \
  --account-id user@example.com \
  archive.sqlite
```

### Utility Commands

```bash
# Check version
vandelay --version

# List available commands
vandelay --help

# Check IMAP server capabilities
vandelay imap capabilities --url imaps://hmailserver.example.com

# Check JMAP server capabilities
vandelay jmap capabilities --url http://localhost:8080
```

## Vandelay Capabilities Utilized

The migration tool leverages Vandelay for:

| Feature | Description |
|---------|-------------|
| IMAP Import | Imports emails, contacts, calendars from IMAP servers |
| JMAP Export | Exports data from Stalwart in JMAP format |
| JMAP Import | Imports data to Stalwart via JMAP |
| Delta Sync | Efficient synchronization of changed data |
| Multi-threaded | Parallel processing for faster migration |
| Checkpoint/Resume | Resume interrupted migrations |

## What Vandelay Cannot Do (Our Tool Fills These Gaps)

| Gap | Vandelay Limitation | Our Solution |
|-----|---------------------|--------------|
| Domain Creation | Cannot create domains in Stalwart | `setup` command with `--create-domains` |
| Account Creation | Cannot create user accounts | `setup` command with `--create-accounts` |
| Alias Migration | Cannot migrate email aliases | `setup` command with `--migrate-aliases` |
| Metadata Migration | Cannot migrate quotas, forwarding rules | Custom account metadata handling |
| Windows Support | Limited Windows support | Full Windows compatibility |

## Troubleshooting Vandelay Integration

### Installation Issues

**Vandelay not found:**

```bash
# Check if Vandelay is in your PATH
echo $PATH
which vandelay

# If not, specify the full path in configuration
{
  "vandelay": {
    "executable": "/path/to/vandelay"
  }
}
```

**Permission denied:**

```bash
# Make Vandelay executable
chmod +x /usr/local/bin/vandelay

# Or run with sudo (not recommended)
sudo chown root:root /usr/local/bin/vandelay
```

### Connection Issues

**Cannot connect to hMailServer:**

```bash
# Test IMAP connection manually
vandelay imap capabilities --url imaps://hmailserver.example.com

# Check hMailServer is accessible
telnet hmailserver.example.com 993
telnet hmailserver.example.com 143
```

**Cannot connect to Stalwart:**

```bash
# Test JMAP connection manually
vandelay jmap capabilities --url http://localhost:8080

# Check Stalwart API
curl -I http://localhost:8080/api/v1/health
curl -u admin:password http://localhost:8080/api/v1/admin/statistics
```

### Performance Issues

**Slow migration:**

```bash
# Increase worker count
vandelay import imap --workers 8 --url imaps://hmailserver.example.com archive.sqlite

# Increase batch size
vandelay import imap --batch-size 200 --url imaps://hmailserver.example.com archive.sqlite
```

**Memory issues:**

```bash
# Reduce worker count
vandelay import imap --workers 2 --url imaps://hmailserver.example.com archive.sqlite

# Reduce batch size
vandelay import imap --batch-size 50 --url imaps://hmailserver.example.com archive.sqlite
```

### Error Handling

**Vandelay exits with error code:**

```bash
# Check Vandelay logs
vandelay import imap --url imaps://hmailserver.example.com archive.sqlite 2>&1 | tee vandelay.log

# Common exit codes
# 0: Success
# 1: General error
# 2: Connection error
# 3: Authentication error
# 4: Configuration error
```

**Authenticate errors:**

```bash
# Verify hMailServer credentials
vandelay imap login --url imaps://hmailserver.example.com --user user@domain.com

# Verify Stalwart credentials
vandelay jmap login --url http://localhost:8080 --user admin
```

## Fallback Mode

If Vandelay is unavailable or fails, the migration tool can fall back to its own data migration:

```bash
# Run migration without Vandelay
stalwart-migrate migrate \
  --source hmailserver \
  --target stalwart \
  --source-config hmailserver-config.json \
  --target-config stalwart-config.json \
  --no-vandelay
```

The fallback mode uses:
- Direct COM API access to hMailServer
- EML export for emails
- JSON export for metadata
- Custom import into Stalwart REST API

## Best Practices

1. **Always install Vandelay**: Even if you plan to use fallback mode, Vandelay provides better performance and reliability
2. **Use matching platforms**: Ensure Vandelay binary matches your OS (Windows binary for Windows, Linux binary for Linux)
3. **Test connectivity first**: Verify Vandelay can connect to both hMailServer and Stalwart before running migration
4. **Start with a small test**: Migrate one domain first to verify everything works
5. **Monitor progress**: Use the `--verbose` flag to see detailed progress
6. **Backup first**: Always backup your hMailServer data before migration
7. **Check disk space**: Vandelay may need temporary storage for intermediate files

## Version Compatibility

| Vandelay Version | Supported | Notes |
|-----------------|-----------|-------|
| v1.0.0 - v1.0.4 | No | Known issues with IMAP parsing |
| v1.0.5+ | Yes | Recommended version |
| Latest (main) | Yes | May have new features, but stable |

Check your Vandelay version:

```bash
vandelay --version
```

## Upgrading Vandelay

```bash
# If installed via cargo
cargo install --force --path .

# If using pre-built binary
# Download the latest version and replace the old binary
wget https://github.com/stalwartlabs/vandelay/releases/latest/download/vandelay-linux-x86_64.tar.gz
tar -xzf vandelay-linux-x86_64.tar.gz
sudo mv vandelay /usr/local/bin/
```

## Additional Resources

- [Vandelay GitHub Repository](https://github.com/stalwartlabs/vandelay)
- [Vandelay Documentation](https://github.com/stalwartlabs/vandelay#readme)
- [JMAP Specification](https://jmap.io/spec.html)
- [IMAP to JMAP Migration Guide](https://github.com/stalwartlabs/vandelay/blob/main/docs/migration.md)

See also:
- [Migration Process Guide](migration-process.md) - Complete migration workflow
- [Docker Setup Guide](docker-setup.md) - Stalwart Docker configuration
- [Account Migration Guide](account-migration.md) - Infrastructure migration details
