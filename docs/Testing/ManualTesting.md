# Manual Testing and Validation

## Overview
This document outlines the manual testing procedures for the Stalwart Migration tool.

## Test Environment
- **hMailServer**: Windows Server with hMailServer installed
- **Stalwart Mail Server**: Docker container running Stalwart
- **Vandelay**: Optional - for message migration testing

## CLI Commands Testing

### Setup Command
```bash
# Test setup with configuration file
stalwart-migrate setup --config config.json

# Test setup with command line options
stalwart-migrate setup --hmail-server localhost --hmail-username admin --hmail-password password \
  --stalwart-url https://stalwart.example.com --stalwart-username admin --stalwart-password password

# Verify: Check that domains, accounts, and aliases are created in Stalwart
```

### Migrate Command
```bash
# Full migration with Vandelay
stalwart-migrate migrate --config config.json --use-vandelay true

# Full migration without Vandelay (fallback path)
stalwart-migrate migrate --config config.json --use-vandelay false

# Migration with specific domains
stalwart-migrate migrate --config config.json --domains domain1.com,domain2.com

# Verify: Check that messages are migrated to Stalwart
```

### Vandelay Command
```bash
# Test Vandelay integration
stalwart-migrate vandelay --config config.json import domain1.com

# Verify: Check Vandelay logs and Stalwart for migrated messages
```

### Export Command
```bash
# Export all domains
stalwart-migrate export --output-dir ./export

# Export specific domain
stalwart-migrate export --output-dir ./export --domains domain1.com

# Verify: Check that export directory contains ZIP archives
```

### Import Command
```bash
# Import from export directory
stalwart-migrate import --input-dir ./export

# Import specific domain
stalwart-migrate import --input-dir ./export --domains domain1.com

# Verify: Check that data is imported into Stalwart
```

### Validate Command
```bash
# Validate migration
stalwart-migrate validate --config config.json

# Verify: Check that all domains, accounts, and aliases exist in Stalwart
```

## Checkpoint/Resume Testing

### Create Checkpoint
1. Start a migration with a large dataset
2. Interrupt the migration (Ctrl+C)
3. Verify that checkpoint files are created in the checkpoint directory

### Resume from Checkpoint
```bash
# Resume from last checkpoint
stalwart-migrate migrate --config config.json --resume true

# Verify: Check that migration continues from where it left off
```

## Error Handling Testing

### Invalid Configuration
```bash
# Test with invalid configuration
stalwart-migrate setup --config invalid-config.json

# Verify: Clear error message is displayed
```

### Connection Errors
```bash
# Test with incorrect credentials
stalwart-migrate setup --hmail-password wrongpassword

# Verify: Clear authentication error is displayed
```

### Missing Dependencies
```bash
# Test without hMailServer installed
stalwart-migrate setup

# Verify: Clear error about missing hMailServer COM API
```

## Configuration Loading Testing

### JSON Configuration
```bash
# Test loading from JSON file
stalwart-migrate setup --config config.json

# Verify: All settings from config.json are applied
```

### Environment Variables
```bash
# Test with environment variables
export HM BailServer_HOST=localhost
export HM BailServer_USERNAME=admin
export HM BailServer_PASSWORD=password

stalwart-migrate setup

# Verify: Environment variables are used
```

## Performance Testing

### Large Dataset Test
```bash
# Generate test data with 100+ accounts and 1000+ emails
# Run migration and measure time

# Verify: Migration completes in reasonable time
```

## Test Results
- [ ] CLI help works for all commands
- [ ] Configuration loading works
- [ ] Export from hMailServer works
- [ ] Import to Stalwart works  
- [ ] Vandelay integration works
- [ ] Setup command works
- [ ] Migrate command works
- [ ] Validate command works
- [ ] Checkpoint/resume works
- [ ] Error handling provides clear messages

## Notes
- Manual testing requires actual hMailServer and Stalwart instances
- Some tests may be skipped if required infrastructure is not available
- Performance testing requires generation of large test datasets
