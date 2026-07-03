# Troubleshooting Guide

This guide provides solutions to common issues encountered during migration from hMailServer to Stalwart Mail Server.

## Overview

When issues occur during migration, follow this troubleshooting approach:

1. **Identify the error** - Check error messages and logs
2. **Isolate the problem** - Determine if it's source, target, or tool-related
3. **Check connectivity** - Verify network access to both servers
4. **Review configuration** - Ensure all settings are correct
5. **Test incrementally** - Try with a single domain or account
6. **Search this guide** - Look for your specific error

## General Troubleshooting

### Enable Verbose Logging

```bash
# Run with verbose output
stalwart-migrate <command> --verbose

# Save logs to file
stalwart-migrate <command> --verbose --log-file migration.log

# Set log level
stalwart-migrate <command> --log-level Debug
```

### Check System Requirements

```bash
# Check .NET version
dotnet --version

# Check disk space
df -h

# Check memory
free -h

# Check Docker (if using containerized Stalwart)
docker info
```

### Common CLI Errors

**Error: Command not found**

```bash
# Check if tool is built
dotnet build StalwartMigration.sln -c Release

# Run from project directory
cd StalwartMigration.Cli
dotnet run -- [arguments]

# Or use the built executable
./bin/Release/net8.0/linux-x64/publish/StalwartMigration [arguments]
```

**Error: Missing required argument**

```bash
# Show help for the command
stalwart-migrate <command> --help

# Check required arguments
stalwart-migrate <command> --help | grep -A 5 "REQUIRED"
```

**Error: Invalid configuration file**

```bash
# Validate configuration
stalwart-migrate validate --source-config hmailserver-config.json

# Check JSON syntax
jq empty hmailserver-config.json
```

## hMailServer Connectivity Issues

### Cannot Connect to hMailServer

**Error:** `Connection to hMailServer failed` or `COM API not available`

**Solutions:**

1. **Verify hMailServer is running:**
   ```powershell
   # On Windows, check the service
   Get-Service hMailServer
   Start-Service hMailServer
   ```

2. **Check COM API access:**
   ```powershell
   # Test COM API connectivity with PowerShell
   $hMailServer = New-Object -ComObject hMailServer.Application
   $hMailServer.Connect()
   ```

3. **Verify credentials:**
   ```xml
   <!-- hmailserver-config.json -->
   {
     "host": "localhost",
     "port": 5000,
     "username": "Administrator",  <!-- Note: Capital A -->
     "password": "your-password",
     "useComApi": true
   }
   ```

4. **Enable COM API in hMailServer:**
   - Open hMailServer Administrator
   - Go to Settings > Advanced > COM API
   - Ensure "Enable COM API" is checked
   - Note the port number (default: 5000)

5. **Firewall issues:**
   ```powershell
   # Check if port is open
   Test-NetConnection -ComputerName localhost -Port 5000
   
   # Open port in firewall
   New-NetFirewallRule -DisplayName "hMailServer COM API" -Direction Inbound -LocalPort 5000 -Protocol TCP -Action Allow
   ```

### Direct Database Access (Fallback)

If COM API is unavailable, use direct database access:

```json
{
  "host": "localhost",
  "databaseConnectionString": "Server=localhost;Database=hMailServer;User Id=sa;Password=your-password;",
  "useComApi": false
}
```

**Prerequisites:**
- SQL Server access
- hMailServer database credentials
- Appropriate permissions

## Stalwart Connectivity Issues

### Cannot Connect to Stalwart API

**Error:** `Connection to Stalwart failed` or `HTTP 401 Unauthorized`

**Solutions:**

1. **Verify Stalwart is running:**
   ```bash
   # Check Docker container
   docker ps | grep stalwart
   
   # Check container logs
   docker logs stalwart-mail
   ```

2. **Test API connectivity:**
   ```bash
   # Health check
   curl -I http://localhost:8080/api/v1/health
   
   # Try authentication
   curl -u admin:your-password http://localhost:8080/api/v1/admin/statistics
   ```

3. **Verify configuration:**
   ```json
   {
     "apiUrl": "http://localhost:8080",
     "username": "admin",
     "password": "your-password",
     "timeoutSeconds": 30,
     "maxRetries": 3
   }
   ```

4. **Check credentials:**
   - Default admin credentials: admin/admin or admin/password
   - If changed, use the correct credentials
   - User must have administrator privileges

5. **Port conflicts:**
   ```bash
   # Check if port 8080 is in use
   sudo lsof -i :8080
   sudo netstat -tulnp | grep 8080
   
   # Kill conflicting process or use different port
   sudo kill <PID>
   ```

### Docker Network Issues

**Error:** `Connection refused` or `No route to host`

**Solutions:**

1. **Check Docker network:**
   ```bash
   # List networks
   docker network ls
   
   # Inspect network
   docker network inspect bridge
   ```

2. **Use host network:**
   ```bash
   # Stop existing container
   docker stop stalwart-mail
   
   # Start with host network
   docker run -d --name stalwart-mail --network host -v stalwart-data:/var/lib/stalwart stalwartlabs/stalwart
   ```

3. **Check port mapping:**
   ```bash
   # Verify port is mapped correctly
   docker port stalwart-mail
   
   # Recreate with explicit port mapping
   docker run -d --name stalwart-mail -p 8080:8080 -v stalwart-data:/var/lib/stalwart stalwartlabs/stalwart
   ```

## Vandelay Issues

### Vandelay Not Found

**Error:** `vandelay: command not found`

**Solutions:**

1. **Verify installation:**
   ```bash
   # Check if Vandelay is in PATH
   which vandelay
   echo $PATH
   ```

2. **Reinstall Vandelay:**
   ```bash
   # Linux
   wget https://github.com/stalwartlabs/vandelay/releases/latest/download/vandelay-linux-x86_64.tar.gz
   tar -xzf vandelay-linux-x86_64.tar.gz
   sudo mv vandelay /usr/local/bin/
   sudo chmod +x /usr/local/bin/vandelay
   
   # Windows
   # Download from GitHub releases and add to PATH
   ```

3. **Specify path in configuration:**
   ```json
   {
     "vandelay": {
       "executable": "/path/to/vandelay"
     }
   }
   ```

### Vandelay Connection Issues

**Error:** `Failed to connect to IMAP server` or `JMAP connection failed`

**Solutions:**

1. **Test Vandelay connectivity:**
   ```bash
   # Test IMAP connection
   vandelay imap capabilities --url imaps://hmailserver.example.com
   
   # Test JMAP connection
   vandelay jmap capabilities --url http://localhost:8080
   ```

2. **Check SSL/TLS:**
   ```bash
   # Try with --insecure flag
   vandelay imap capabilities --url imaps://hmailserver.example.com --insecure
   ```

3. **Verify credentials:**
   ```bash
   # Test IMAP login
   vandelay imap login --url imaps://hmailserver.example.com --user user@domain.com
   
   # Test JMAP login
   vandelay jmap login --url http://localhost:8080 --user admin
   ```

### Vandelay Version Issues

**Error:** `Version not supported` or `Unknown command`

**Solutions:**

1. **Check version:**
   ```bash
   vandelay --version
   ```

2. **Upgrade Vandelay:**
   ```bash
   # If installed via cargo
   cargo install --force --path .
   
   # If using pre-built binary
   wget https://github.com/stalwartlabs/vandelay/releases/latest/download/vandelay-linux-x86_64.tar.gz
   tar -xzf vandelay-linux-x86_64.tar.gz
   sudo mv vandelay /usr/local/bin/
   ```

3. **Use compatible version:**
   ```bash
   # v1.0.5+ is recommended
   # Download specific version
   wget https://github.com/stalwartlabs/vandelay/releases/download/v1.0.5/vandelay-linux-x86_64.tar.gz
   ```

## Migration-Specific Issues

### Domain Migration Issues

**Error:** `Domain already exists`

**Solutions:**

```bash
# Skip existing domains
stalwart-migrate setup --create-domains --skip-existing-domains

# List existing domains in Stalwart
curl -u admin:password http://localhost:8080/api/v1/admin/domains

# Delete domain in Stalwart (if appropriate)
# Use Stalwart admin web interface or API
```

**Error:** `Invalid domain name`

```bash
# Check domain name format
# Valid: letters, numbers, hyphens, periods
# Invalid: underscores, special characters

# Use domain mapping to fix invalid names
{
  "mappings": [
    {
      "source": "invalid_domain.com",
      "target": "valid-domain.com"
    }
  ]
}
```

### Account Migration Issues

**Error:** `Account already exists`

**Solutions:**

```bash
# Skip existing accounts
stalwart-migrate setup --create-accounts --skip-existing

# Use account mapping to avoid conflicts
stalwart-migrate setup --create-accounts --account-mapping account-mapping.json

# List existing accounts in Stalwart
curl -u admin:password http://localhost:8080/api/v1/admin/accounts
```

**Error:** `Invalid email address`

```bash
# Skip invalid email addresses
stalwart-migrate setup --create-accounts --skip-invalid

# Validate email addresses before migration
stalwart-migrate validate --source hmailserver --validate-emails
```

**Error:** `Quota exceeded`

```bash
# Increase default quota
stalwart-migrate setup --create-accounts --default-quota 20480

# Set per-account quotas in mapping file
{
  "mappings": [
    {
      "source": "user@domain.com",
      "target": "user@domain.com",
      "quota": 51200
    }
  ]
}
```

### Alias Migration Issues

**Error:** `Alias target does not exist`

**Solutions:**

```bash
# Create accounts first, then aliases
stalwart-migrate setup --create-accounts
stalwart-migrate setup --migrate-aliases

# Or skip validation (not recommended)
stalwart-migrate setup --migrate-aliases --skip-validation

# Validate targets before migration
stalwart-migrate validate --source hmailserver --validate-aliases
```

### Data Migration Issues

**Error:** `Failed to export data from hMailServer`

**Solutions:**

```bash
# Try with COM API disabled (use database)
stalwart-migrate export --use-database

# Reduce batch size
stalwart-migrate export --batch-size 50

# Export specific domain only
stalwart-migrate export --domains domain.com
```

**Error:** `Failed to import data to Stalwart`

```bash
# Check Stalwart API
curl -I http://localhost:8080/api/v1/health

# Reduce batch size
stalwart-migrate import --batch-size 25

# Import specific domain only
stalwart-migrate import --domains domain.com
```

## Performance Issues

### Slow Migration

**Solutions:**

1. **Increase batch size:**
   ```bash
   stalwart-migrate migrate --batch-size 200
   ```

2. **Reduce checkpoint frequency:**
   ```bash
   stalwart-migrate migrate --checkpoint-interval 120
   ```

3. **Use faster storage:**
   - Use SSD for intermediate files
   - Use tmpfs for temporary data

4. **Improve network:**
   - Use wired connection instead of WiFi
   - Ensure low latency between servers

### Memory Issues

**Error:** `Out of memory`

**Solutions:**

1. **Reduce batch size:**
   ```bash
   stalwart-migrate migrate --batch-size 10
   ```

2. **Limit parallel processing:**
   ```bash
   stalwart-migrate migrate --max-parallel 2
   ```

3. **Increase system memory:**
   - Close other applications
   - Add more RAM to the machine
   - Use a machine with more memory

### Timeout Issues

**Error:** `Operation timed out`

**Solutions:**

```bash
# Increase timeout
stalwart-migrate migrate --timeout-seconds 300

# For specific operations
stalwart-migrate migrate --source-timeout 120 --target-timeout 120
```

## Checkpoint and Resume Issues

### Resume Not Working

**Error:** `No checkpoint found` or `Cannot resume`

**Solutions:**

```bash
# Check for checkpoint files
ls -la *.checkpoint.json

# Specify checkpoint file
stalwart-migrate migrate --resume --checkpoint-file migration.checkpoint.json

# Start fresh (lose progress)
stalwart-migrate migrate --fresh-start
```

### Corrupted Checkpoint

**Error:** `Invalid checkpoint file`

**Solutions:**

```bash
# Delete corrupted checkpoint
rm migration.checkpoint.json

# Start fresh
stalwart-migrate migrate --fresh-start
```

## Validation Issues

### Validation Failures

**Error:** `Validation failed: X items missing`

**Solutions:**

```bash
# Run validation with details
stalwart-migrate validate --full-report validation-report.json

# Check specific items
stalwart-migrate validate --validate-domains --domain-list domains.txt

# Compare counts
stalwart-migrate validate --compare-counts
```

### False Positives in Validation

**Error:** `Validation shows missing items that exist`

**Solutions:**

```bash
# Check manually
curl -u admin:password http://localhost:8080/api/v1/admin/accounts/user@domain.com

# Force re-sync
stalwart-migrate validate --force-resync
```

## Platform-Specific Issues

### Windows Issues

**Issue: COM API not available on Linux**

**Solution:** Use database access instead:

```json
{
  "useComApi": false,
  "databaseConnectionString": "Server=windows-server;Database=hMailServer;..."
}
```

**Issue: File permission errors**

```powershell
# Run as Administrator
# Or grant permissions to the user
icacls "C:\path\to\config" /grant Users:(R)
```

### Linux Issues

**Issue: Case sensitivity in paths**

```bash
# Use correct case
cd /home/user/StalwartMigration
# Not: cd /home/user/stalwartmigration
```

**Issue: Missing dependencies**

```bash
# Install .NET runtime
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update
sudo apt install -y dotnet-runtime-8.0
```

## Log Analysis

### Understanding Log Files

```bash
# View logs in real-time
stalwart-migrate migrate --verbose | tee migration.log

# Filter for errors
grep -i error migration.log

# Filter for warnings
grep -i warning migration.log

# Count errors
grep -c "ERROR" migration.log
```

### Common Log Patterns

| Pattern | Meaning | Severity |
|---------|---------|----------|
| `ERROR: Connection failed` | Connection issue | High |
| `WARN: Skipping item` | Item skipped | Medium |
| `INFO: Processing domain` | Normal progress | Low |
| `DEBUG: API request` | Detailed debug | Low |

### Log Levels

```bash
# Set log level
--log-level Trace    # Most verbose
--log-level Debug    # Detailed debug
--log-level Information  # Default
--log-level Warning  # Only warnings and errors
--log-level Error    # Only errors
--log-level Critical # Only critical errors
```

## Collecting Diagnostic Information

### For Bug Reports

```bash
# Collect system information
uname -a
.dotnet --info
.docker info (if using Docker)

# Collect configuration
echo "=== hmailserver-config.json ==="
cat hmailserver-config.json
echo "=== stalwart-config.json ==="
cat stalwart-config.json

# Collect logs
stalwart-migrate migrate --verbose --log-file diagnostics.log 2>&1

# Collect version information
stalwart-migrate --version
vandelay --version (if applicable)
```

### Create Diagnostic Archive

```bash
# Create a zip archive with all diagnostic information
diagnostics_dir="migration-diagnostics-$(date +%Y%m%d-%H%M%S)"
mkdir $diagnostics_dir

# Copy configuration
cp *.json $diagnostics_dir/

# Copy logs
cp *.log $diagnostics_dir/

# Save version info
dotnet --version > $diagnostics_dir/dotnet-version.txt
vandelay --version > $diagnostics_dir/vandelay-version.txt 2>&1 || true

# Save system info
uname -a > $diagnostics_dir/system-info.txt

# Zip it up
zip -r $diagnostics_dir.zip $diagnostics_dir

# Cleanup
rm -rf $diagnostics_dir
```

## Known Issues and Workarounds

### Issue: hMailServer COM API Timeout

**Workaround:** Increase timeout and reduce batch size

```bash
stalwart-migrate setup --timeout-seconds 120 --batch-size 50
```

### Issue: Stalwart API Rate Limiting

**Workaround:** Reduce parallel requests and add delays

```bash
stalwart-migrate migrate --max-parallel 2 --delay-ms 500
```

### Issue: Large Attachments Fail

**Workaround:** Split large migrations or increase timeouts

```bash
stalwart-migrate export --max-attachment-size 50 --timeout-seconds 600
```

### Issue: Special Characters in Email Addresses

**Workaround:** Use account mapping to normalize addresses

```json
{
  "mappings": [
    {
      "source": "user+tag@domain.com",
      "target": "user.tag@domain.com"
    }
  ]
}
```

## Contact and Support

If you encounter issues not covered in this guide:

1. **Check GitHub Issues:**
   - https://github.com/frederik256/stalwart_mi_mistral/issues

2. **Create a Bug Report:**
   - Include diagnostic archive
   - Describe steps to reproduce
   - Note your environment (.NET version, OS, etc.)

3. **Join Community:**
   - GitHub Discussions: https://github.com/frederik256/stalwart_mi_mistral/discussions
   - Stalwart Mail Server: https://stalwartlabs.github.io/

## Prevention Checklist

Before starting migration, verify:

- [ ] hMailServer is accessible (COM API or database)
- [ ] Stalwart is running and accessible
- [ ] Vandelay is installed (if using)
- [ ] Configuration files are valid
- [ ] Disk space is sufficient
- [ ] Network connectivity is good
- [ ] Backups exist for both servers
- [ ] Test migration with one domain completed successfully
- [ ] All required ports are open
- [ ] Credentials are correct

## See Also

- [User Guide](user-guide.md) - General usage instructions
- [Configuration Reference](configuration.md) - Configuration file details
- [Migration Process Guide](migration-process.md) - Complete migration workflow
- [Docker Setup Guide](docker-setup.md) - Stalwart Docker configuration
- [Vandelay Integration Guide](vandelay-integration.md) - Vandelay setup and usage
- [Account Migration Guide](account-migration.md) - Infrastructure migration details
