# Migration Process Guide

This document provides a detailed guide to migrating from hMailServer to Stalwart Mail Server using the Stalwart Migration Tool.

## Overview

The migration process follows a **three-phase approach**:

1. **Phase 1: Setup** - Create infrastructure in Stalwart (domains, accounts, aliases)
2. **Phase 2: Data Migration** - Migrate messages, contacts, calendars via Vandelay
3. **Phase 3: Validation** - Verify all data was migrated correctly

## Prerequisites Checklist

Before starting the migration, ensure all prerequisites are met:

- [ ] hMailServer is accessible and running
- [ ] hMailServer administrator credentials are available
- [ ] Stalwart Mail Server is running in Docker
- [ ] Stalwart API credentials are available
- [ ] Vandelay is installed (optional, but recommended)
- [ ] Network connectivity between source and target servers
- [ ] Configuration files are created and validated
- [ ] Sufficient disk space for temporary files
- [ ] Backup of hMailServer data is available

## Phase 1: Setup Infrastructure

This phase creates the domain, account, and alias structure in Stalwart that matches your hMailServer configuration.

### Step 1: Test Connectivity

```bash
# Test hMailServer connection
StalwartMigration validate \
  --source-config hmailserver-config.json \
  --validate-target

# Test Stalwart connection
StalwartMigration validate \
  --target-config stalwart-config.json \
  --validate-target
```

Both commands should return successfully with no errors.

### Step 2: Extract hMailServer Information

The tool will automatically extract the following from hMailServer:

- All domains
- All accounts per domain
- All email aliases per domain
- Account metadata (quotas, forwarding, etc.)

### Step 3: Run Setup Command

```bash
StalwartMigration setup \
  --source-config hmailserver-config.json \
  --target-config stalwart-config.json \
  --create-domains \
  --create-accounts \
  --migrate-aliases
```

**Options:**

- `--create-domains`: Create all domains from hMailServer in Stalwart
- `--create-accounts`: Create all accounts from hMailServer in Stalwart
- `--migrate-aliases`: Migrate all email aliases from hMailServer to Stalwart
- `--domain <domain>`: Process only specific domain(s)

**Output:**

The setup phase will:
- Create each domain from hMailServer in Stalwart
- Create each account with its properties (quota, forwarding, etc.)
- Create email aliases for each domain
- Generate a report of created infrastructure

### Step 4: Verify Setup

After setup completes:

1. Check the Stalwart admin console to verify domains were created
2. Verify accounts exist for each domain
3. Check that aliases are properly configured
4. Review the setup report for any errors or warnings

## Phase 2: Data Migration

This phase migrates the actual message data from hMailServer to Stalwart.

### Option A: Using Vandelay (Recommended)

Vandelay is the recommended method for data migration as it uses the efficient IMAP to JMAP protocol.

#### Step 1: Install Vandelay

If not already installed:

```bash
# Check if Vandelay is installed
StalwartMigration vandelay check

# Install if needed (manual installation required)
cargo install --path . --locked  # From Vandelay source
```

#### Step 2: Run Vandelay Migration

```bash
StalwartMigration migrate \
  --source-config hmailserver-config.json \
  --target-config stalwart-config.json \
  --run-vandelay
```

**Options:**

- `--run-vandelay`: Run Vandelay for message migration
- `--domain <domain>`: Migrate only specific domain(s)
- `--last-checkpoint`: Resume from a specific checkpoint

**What Vandelay Does:**

- Connects to hMailServer via IMAP
- Extracts all messages, contacts, and calendars
- Converts and uploads to Stalwart via JMAP
- Preserves folder structure
- Handles large mailboxes efficiently

### Option B: Fallback Custom Migration

If Vandelay is unavailable or you prefer the custom method:

#### Step 1: Export Data

```bash
StalwartMigration export \
  --source-config hmailserver-config.json \
  --output ./migration-data \
  --domain example.com
```

**Output:**

Creates a directory structure:
```
migration-data/
├── example.com/
│   ├── metadata.json          # Domain/Account metadata
│   ├── accounts/
│   │   └── user@example.com/
│   │       ├── emails/
│   │       │   ├── 001.eml    # Email messages in EML format
│   │       │   └── 002.eml
│   │       └── attachments/
│   │           ├── img1.jpg
│   │           └── doc1.pdf
│   └── aliases.json
└── report.json
```

#### Step 2: Import Data

```bash
StalwartMigration import \
  --target-config stalwart-config.json \
  --input ./migration-data
```

**What the Import Does:**

- Reads the exported data from the directory structure
- Creates accounts if they don't exist
- Imports email messages from EML files
- Restores attachments
- Applies aliases

## Phase 3: Validation

After migration, validate that all data was migrated correctly.

### Step 1: Run Validation

```bash
StalwartMigration validate \
  --source-config hmailserver-config.json \
  --target-config stalwart-config.json
```

**What Validation Checks:**

- Domain counts match between hMailServer and Stalwart
- Account counts match per domain
- Alias counts match per domain
- Sample data integrity checks
- Connectivity verification

### Step 2: Manual Verification

1. **Check Domain Count:**
   ```bash
   # hMailServer
   # Stalwart Admin Console
   ```

2. **Check Account Count per Domain:**
   ```bash
   # Compare account lists
   ```

3. **Test Sample Emails:**
   - Log into Stalwart webmail
   - Verify sample emails are accessible
   - Check attachments can be opened

4. **Test Sending/Receiving:**
   - Send a test email to a migrated account
   - Verify it can be received
   - Send from a migrated account

## Complete Migration Workflow

### Single Command (All Phases)

```bash
StalwartMigration migrate \
  --source-config hmailserver-config.json \
  --target-config stalwart-config.json \
  --setup-first \
  --run-vandelay
```

This runs:
1. Setup phase (domains, accounts, aliases)
2. Vandelay migration (messages, contacts, calendars)
3. Validation phase

### Per-Domain Migration

```bash
# Migrate one domain at a time
for domain in example.com example.org; do
  StalwartMigration migrate \
    --source-config hmailserver-config.json \
    --target-config stalwart-config.json \
    --domain $domain \
    --setup-first \
    --run-vandelay
done
```

## Checkpoint and Resume

The tool automatically creates checkpoints every 30 seconds of runtime. If a migration is interrupted:

### Resume from Last Checkpoint

```bash
StalwartMigration migrate \
  --source-config hmailserver-config.json \
  --target-config stalwart-config.json \
  --resume
```

### Resume from Specific Checkpoint

```bash
StalwartMigration migrate \
  --source-config hmailserver-config.json \
  --target-config stalwart-config.json \
  --last-checkpoint checkpoint-20260702-103020.json
```

Checkpoints are saved in the `./checkpoints/` directory.

## Performance Considerations

### Large Mailboxes

- The tool processes data in batches (default: 50 items)
- Adjust batch size based on your server resources:
  ```json
  {
    "batchSize": 100
  }
  ```

### Network Latency

- Increase timeout for slow connections:
  ```json
  {
    "timeoutSeconds": 60,
    "maxRetries": 5
  }
  ```

### Memory Usage

- The tool streams data where possible
- For very large migrations, consider:
  - Migrating domains one at a time
  - Running on a machine with sufficient RAM
  - Using the checkpoint feature for long-running migrations

## Troubleshooting

See [Troubleshooting Guide](troubleshooting.md) for common issues.

### Common Issues

| Issue | Solution |
|-------|----------|
| Connection timeout | Increase `timeoutSeconds` in config |
| Authentication failed | Verify credentials in config files |
| COM API not available | Set `useComApi: false` and use database connection |
| Vandelay not found | Install Vandelay or use fallback method |
| Insufficient permissions | Run with administrator privileges |

## Post-Migration Tasks

After successful migration:

1. **DNS Updates:**
   - Update MX records to point to Stalwart
   - Update SPF, DKIM, DMARC records
   - Consider TTL for smooth transition

2. **User Communication:**
   - Notify users of migration date
   - Provide new connection details
   - Reset passwords if needed

3. **Monitoring:**
   - Monitor Stalwart server for issues
   - Verify all services are running
   - Check logs for any errors

4. **Cleanup:**
   - Keep hMailServer running for fallback (recommended)
   - After verification, consider decommissioning hMailServer
   - Archive old data if needed

---

*Last Updated: 2026-07-02*
*Tool Version: 1.0*
