# Configuration Reference

This document describes all configuration options for the Stalwart Migration Tool.

## Configuration Files

The tool uses JSON-based configuration files for hMailServer and Stalwart connections.

### File Locations

- `configs/hmailserver-config.json` - hMailServer connection settings
- `configs/stalwart-config.json` - Stalwart Mail Server connection settings

### Example Files

Copy the example files to create your configuration:

```bash
cp configs/hmailserver-config.example.json configs/hmailserver-config.json
cp configs/stalwart-config.example.json configs/stalwart-config.json
```

## hMailServer Configuration

### Required Fields

| Field | Type | Description | Default |
|-------|------|-------------|---------|
| `host` | string | hMailServer hostname or IP address | `localhost` |
| `port` | integer | hMailServer COM API port | `5000` |
| `username` | string | hMailServer administrator username | `Administrator` |
| `password` | string | hMailServer administrator password | - |
| `useComApi` | boolean | Use COM API (true) or direct database (false) | `true` |

### Optional Fields

| Field | Type | Description | Default |
|-------|------|-------------|---------|
| `databaseConnectionString` | string | SQL Server connection string (for database fallback) | - |
| `sslEnabled` | boolean | Enable SSL for COM API connections | `false` |
| `timeoutSeconds` | integer | Connection timeout in seconds | `30` |
| `maxConnections` | integer | Maximum concurrent connections | `10` |

### Example Configuration

```json
{
  "host": "localhost",
  "port": 5000,
  "username": "Administrator",
  "password": "your-password",
  "useComApi": true,
  "databaseConnectionString": "Server=localhost;Database=hmailserver;User Id=sa;Password=password;"
}
```

### Database Connection String Format

For SQL Server:
```
Server=your-server;Database=hmailserver;User Id=username;Password=password;
```

For MySQL:
```
Server=your-server;Database=hmailserver;User ID=username;Password=password;
```

## Stalwart Configuration

### Required Fields

| Field | Type | Description | Default |
|-------|------|-------------|---------|
| `apiUrl` | string | Stalwart REST API base URL | `http://localhost:8080` |
| `username` | string | Stalwart API username | `admin` |
| `password` | string | Stalwart API password | - |

### Optional Fields

| Field | Type | Description | Default |
|-------|------|-------------|---------|
| `timeoutSeconds` | integer | API request timeout in seconds | `30` |
| `maxRetries` | integer | Maximum retry attempts for failed requests | `3` |
| `retryDelayMilliseconds` | integer | Delay between retries in milliseconds | `1000` |
| `sslEnabled` | boolean | Enable SSL for API connections | `false` |
| `sslCertificateValidation` | boolean | Validate SSL certificates | `true` |
| `batchSize` | integer | Number of items to process in each batch | `50` |
| `concurrentDomains` | integer | Number of domains to process concurrently | `1` |

### Example Configuration

```json
{
  "apiUrl": "http://localhost:8080",
  "username": "admin",
  "password": "your-password",
  "timeoutSeconds": 30,
  "maxRetries": 3,
  "sslEnabled": false
}
```

## Command-Line Options

All commands support the following options:

### Source Configuration Options

| Option | Short | Description | Required |
|--------|-------|-------------|----------|
| `--source` | - | Source hMailServer configuration file path | No |
| `--source-config` | - | Path to hMailServer configuration file | No |

### Target Configuration Options

| Option | Short | Description | Required |
|--------|-------|-------------|----------|
| `--target` | - | Target Stalwart configuration file path | No |
| `--target-config` | - | Path to Stalwart configuration file | No |

### Domain Options

| Option | Short | Description | Required |
|--------|-------|-------------|----------|
| `--domain` | - | Specific domain(s) to process (can be repeated) | No |

### Common Flags

| Flag | Description | Required |
|------|-------------|----------|
| `--help` | `-h`, `-?` | Show command help | No |
| `--version` | - | Show version information | No |

## Command-Specific Options

### Setup Command

```
StalwartMigration setup [options]
```

| Option | Type | Description |
|--------|------|-------------|
| `--create-domains` | flag | Create domains in Stalwart |
| `--create-accounts` | flag | Create accounts in Stalwart |
| `--migrate-aliases` | flag | Migrate email aliases |

**Example:**
```bash
StalwartMigration setup \
  --source-config hmailserver-config.json \
  --target-config stalwart-config.json \
  --create-domains \
  --create-accounts \
  --migrate-aliases
```

### Migrate Command

```
StalwartMigration migrate [options]
```

| Option | Type | Description |
|--------|------|-------------|
| `--setup-first` | flag | Run setup phase before migration |
| `--run-vandelay` | flag | Run Vandelay for message migration |
| `--resume` | flag | Resume from last checkpoint |
| `--last-checkpoint` | string | Resume from specific checkpoint |
| `--skip-messages` | flag | Skip message migration |
| `--skip-validation` | flag | Skip validation phase |

**Example:**
```bash
StalwartMigration migrate \
  --source-config hmailserver-config.json \
  --target-config stalwart-config.json \
  --setup-first \
  --run-vandelay
```

### Vandelay Command

```
StalwartMigration vandelay [command] [options]
```

**Subcommands:**

- `install` - Validate and install Vandelay
- `check` - Check Vandelay installation
- `run-import` - Run Vandelay import only
- `run-export` - Run Vandelay export only

**Example:**
```bash
# Check Vandelay installation
StalwartMigration vandelay check

# Run Vandelay import
StalwartMigration vandelay run-import \
  --config vandelay-config.json \
  --domain example.com
```

### Export Command (Fallback)

```
StalwartMigration export [options]
```

| Option | Type | Description |
|--------|------|-------------|
| `--output` | string | Output directory for exported files |
| `--config` | string | Path to export configuration file |

**Example:**
```bash
StalwartMigration export \
  --source-config hmailserver-config.json \
  --output ./exported-data \
  --domain example.com
```

### Import Command (Fallback)

```
StalwartMigration import [options]
```

| Option | Type | Description |
|--------|------|-------------|
| `--input` | string | Input directory for import files |
| `--config` | string | Path to import configuration file |

**Example:**
```bash
StalwartMigration import \
  --target-config stalwart-config.json \
  --input ./exported-data
```

### Validate Command

```
StalwartMigration validate [options]
```

| Option | Type | Description |
|--------|------|-------------|
| `--validate-target` | flag | Test API connectivity to target |

**Example:**
```bash
StalwartMigration validate \
  --source-config hmailserver-config.json \
  --target-config stalwart-config.json
```

## Environment Variables

The tool does not currently support environment variables for configuration. All settings must be provided via configuration files or command-line options.

## Configuration Validation

The tool validates configuration files on startup and will report any missing required fields or invalid values.

## Security Notes

- Store configuration files securely
- Do not commit configuration files with real credentials to version control
- Use `.gitignore` to exclude configuration files:
  ```
  configs/*.json
  !configs/*.example.json
  ```

---

*Last Updated: 2026-07-02*
*Tool Version: 1.0*
