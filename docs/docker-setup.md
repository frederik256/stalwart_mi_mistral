# Docker Container Setup Guide

This guide explains how to set up Stalwart Mail Server in Docker containers for use with the migration tool.

## Overview

The Stalwart Migration Tool interacts with Stalwart Mail Server exclusively through its REST API. Users are responsible for container lifecycle management, including starting, stopping, and configuring the containers.

## Prerequisites

- Docker installed on your system
- Docker daemon running
- Minimum 4GB RAM allocated to Docker
- Port 8080 (or custom port) available
- Volume storage for persistent data

## Quick Start

### 1. Run Stalwart Mail Server Container

Start the Stalwart Mail Server container with persistent storage:

```bash
# Create a directory for persistent data
docker volume create stalwart-data

# Run the container with default configuration
docker run -d \
  --name stalwart-mail \
  -p 8080:8080 \
  -v stalwart-data:/var/lib/stalwart \
  stalwartlabs/stalwart:latest
```

### 2. Verify Container is Running

```bash
# Check container status
docker ps | grep stalwart

# View container logs
docker logs stalwart-mail

# Check API connectivity
curl -I http://localhost:8080/api/v1/health
```

## Advanced Configuration

### Custom Port Mapping

To use a different port than 8080:

```bash
docker run -d \
  --name stalwart-mail \
  -p 9000:8080 \
  -v stalwart-data:/var/lib/stalwart \
  -e STALWART_API_PORT=8080 \
  stalwartlabs/stalwart:latest
```

Then update your `stalwart-config.json`:

```json
{
  "apiUrl": "http://localhost:9000",
  "username": "admin",
  "password": "secure-password",
  "timeoutSeconds": 30,
  "maxRetries": 3
}
```

### Custom Configuration File

Mount a custom configuration file:

```bash
docker run -d \
  --name stalwart-mail \
  -p 8080:8080 \
  -v stalwart-data:/var/lib/stalwart \
  -v $(pwd)/stalwart-config.toml:/etc/stalwart/config.toml \
  stalwartlabs/stalwart:latest
```

### Environment Variables

Common environment variables for Stalwart configuration:

| Variable | Description | Default |
|----------|-------------|---------|
| `STALWART_API_PORT` | API server port | 8080 |
| `STALWART_DATA_DIR` | Data directory | /var/lib/stalwart |
| `STALWART_MAX_CONNECTIONS` | Maximum concurrent connections | 100 |
| `STALWART_LOG_LEVEL` | Logging verbosity | info |

Example with environment variables:

```bash
docker run -d \
  --name stalwart-mail \
  -p 8080:8080 \
  -v stalwart-data:/var/lib/stalwart \
  -e STALWART_MAX_CONNECTIONS=200 \
  -e STALWART_LOG_LEVEL=debug \
  stalwartlabs/stalwart:latest
```

## Network Configuration

### Docker Network Options

#### Option 1: Host Network (Recommended for Local Development)

```bash
docker run -d \
  --name stalwart-mail \
  --network host \
  -v stalwart-data:/var/lib/stalwart \
  stalwartlabs/stalwart:latest
```

This makes Stalwart accessible at `http://localhost:8080` on the host machine.

#### Option 2: Custom Bridge Network

```bash
# Create a custom network
docker network create migration-network

# Run Stalwart on the custom network
docker run -d \
  --name stalwart-mail \
  --network migration-network \
  -p 8080:8080 \
  -v stalwart-data:/var/lib/stalwart \
  stalwartlabs/stalwart:latest
```

### Connecting to Remote Stalwart

If Stalwart is running on a different machine:

```bash
# On the remote machine
docker run -d \
  --name stalwart-mail \
  -p 0.0.0.0:8080:8080 \
  -v stalwart-data:/var/lib/stalwart \
  stalwartlabs/stalwart:latest

# On your local machine, update the configuration
# stalwart-config.json
{
  "apiUrl": "http://remote-server-ip:8080",
  "username": "admin",
  "password": "secure-password"
}
```

## Persistence and Data Management

### Volume Management

All persistent data (accounts, domains, messages) is stored in the Docker volume.

```bash
# List volumes
docker volume ls

# Inspect volume details
docker volume inspect stalwart-data

# Backup volume data
docker run --rm \
  -v stalwart-data:/volume \
  -v $(pwd):/backup \
  alpine tar cvf /backup/stalwart-backup.tar /volume

# Restore volume data
docker run --rm \
  -v stalwart-data:/volume \
  -v $(pwd):/backup \
  alpine tar xvf /backup/stalwart-backup.tar -C /
```

### Version Upgrades

To upgrade Stalwart to a newer version:

```bash
# Stop the running container
docker stop stalwart-mail

# Remove the old container
docker rm stalwart-mail

# Pull the latest image
docker pull stalwartlabs/stalwart:latest

# Start a new container with the same volume
docker run -d \
  --name stalwart-mail \
  -p 8080:8080 \
  -v stalwart-data:/var/lib/stalwart \
  stalwartlabs/stalwart:latest
```

> **Note**: Always backup your volume data before upgrading.

## Security Considerations

### TLS/SSL Configuration

For production environments, configure TLS:

```bash
docker run -d \
  --name stalwart-mail \
  -p 8443:8443 \
  -v stalwart-data:/var/lib/stalwart \
  -v $(pwd)/certs:/etc/stalwart/certs \
  -e STALWART_TLS_CERT=/etc/stalwart/certs/cert.pem \
  -e STALWART_TLS_KEY=/etc/stalwart/certs/key.pem \
  stalwartlabs/stalwart:latest
```

Update your configuration to use HTTPS:

```json
{
  "apiUrl": "https://localhost:8443",
  "username": "admin",
  "password": "secure-password",
  "sslEnabled": true
}
```

### Authentication

Stalwart uses JWT tokens for authentication. The migration tool handles token management automatically. Ensure:

- The API credentials in `stalwart-config.json` are correct
- The user has administrator privileges
- The API URL is accessible from the machine running the migration tool

## Monitoring and Management

### Container Monitoring

```bash
# View real-time logs
docker logs -f stalwart-mail

# Check container resource usage
docker stats stalwart-mail

# View container processes
docker top stalwart-mail
```

### API Health Checks

```bash
# Health check endpoint
curl -I http://localhost:8080/api/v1/health

# Version information
curl http://localhost:8080/api/v1/version

# Server statistics
curl -u admin:secure-password http://localhost:8080/api/v1/admin/statistics
```

## Docker Compose (Optional)

For more complex setups, use Docker Compose:

```yaml
# docker-compose.yml
version: '3.8'

services:
  stalwart-mail:
    image: stalwartlabs/stalwart:latest
    container_name: stalwart-mail
    ports:
      - "8080:8080"
    volumes:
      - stalwart-data:/var/lib/stalwart
    environment:
      - STALWART_MAX_CONNECTIONS=200
      - STALWART_LOG_LEVEL=info
    restart: unless-stopped

volumes:
  stalwart-data:
```

Start with:

```bash
docker-compose up -d
```

## Troubleshooting Docker Issues

### Common Problems

**Port already in use:**

```bash
# Find and kill the process using port 8080
sudo lsof -i :8080
sudo kill <PID>
```

**Permission denied on volume:**

```bash
# Ensure proper permissions
chmod -R 755 $(pwd)/stalwart-data
```

**Container fails to start:**

```bash
# Check logs for errors
docker logs stalwart-mail

# Try with debug logging
docker run -d \
  --name stalwart-mail \
  -p 8080:8080 \
  -v stalwart-data:/var/lib/stalwart \
  -e STALWART_LOG_LEVEL=debug \
  stalwartlabs/stalwart:latest
```

**Out of memory:**

```bash
# Increase Docker memory allocation
# In Docker Desktop: Settings > Resources > Memory

# Or limit container memory
docker run -d \
  --name stalwart-mail \
  -p 8080:8080 \
  -v stalwart-data:/var/lib/stalwart \
  -m 4g \
  stalwartlabs/stalwart:latest
```

## Pre-Migration Checklist

Before running the migration, ensure:

- [ ] Stalwart container is running (`docker ps` shows stalwart-mail)
- [ ] API is accessible (`curl -I http://localhost:8080/api/v1/health` returns 200)
- [ ] Credentials in `stalwart-config.json` are correct
- [ ] Port mapping is correct (no conflicts)
- [ ] Volume has sufficient disk space
- [ ] Network connectivity between hMailServer and Stalwart

## Integration with Migration Tool

Once Stalwart is running in Docker, you can verify the connection using the migration tool:

```bash
# Test connectivity to Stalwart
stalwart-migrate validate --target stalwart --config stalwart-config.json

# Run the full migration
stalwart-migrate migrate \
  --source hmailserver \
  --target stalwart \
  --source-config hmailserver-config.json \
  --target-config stalwart-config.json \
  --run-vandelay
```

See [Migration Process Guide](migration-process.md) for complete migration instructions.
