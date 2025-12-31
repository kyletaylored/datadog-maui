# Building the Datadog MAUI API

## Quick Start

### Local Development

```bash
# 1. Set Git metadata as environment variables
source ./set-git-metadata.sh

# 2. Build the image with Git metadata
docker-compose build

# 3. Start all containers
docker-compose up -d

# 4. Verify API is running
curl http://localhost:5000/health
```

### Production Build

```bash
# Set environment variables for production
export DD_GIT_REPOSITORY_URL="https://github.com/yourusername/datadog-maui"
export DD_GIT_COMMIT_SHA=$(git rev-parse HEAD)
export DD_GIT_TAG=$(git describe --tags --exact-match 2>/dev/null || echo "latest")
export BUILD_DATE=$(date -u +"%Y-%m-%dT%H:%M:%SZ")

# Build with production configuration
docker-compose build

# Tag for registry (if pushing to container registry)
docker tag datadog-maui-api:${DD_GIT_TAG} your-registry/datadog-maui-api:${DD_GIT_TAG}
docker push your-registry/datadog-maui-api:${DD_GIT_TAG}
```

## What Gets Built

### Image Name and Tag

The image is automatically named and tagged based on Git metadata:

- **Image Name**: `datadog-maui-api`
- **Tag**: `${DD_GIT_TAG}` (defaults to `latest`)

Examples:
- `datadog-maui-api:latest` - Development builds
- `datadog-maui-api:v1.0.0` - Release builds with Git tag
- `datadog-maui-api:e7234e5` - Builds tagged with short commit SHA

### Embedded Metadata

Each image contains:

**Environment Variables:**
- `DD_GIT_REPOSITORY_URL` - Source repository URL
- `DD_GIT_COMMIT_SHA` - Full commit SHA
- `DD_GIT_TAG` - Git tag or short SHA

**Docker Labels (OCI Standard):**
- `org.opencontainers.image.created` - Build timestamp
- `org.opencontainers.image.source` - Repository URL
- `org.opencontainers.image.revision` - Commit SHA
- `org.opencontainers.image.version` - Git tag

**Docker Labels (Datadog):**
- `com.datadoghq.tags.service` - Service name
- `com.datadoghq.tags.version` - Version tag
- `git.repository_url` - Repository URL
- `git.commit.sha` - Commit SHA

## Build Commands

### Rebuild Everything

```bash
source ./set-git-metadata.sh
docker-compose build --no-cache
docker-compose up -d
```

### Rebuild Just the API

```bash
source ./set-git-metadata.sh
docker-compose build api
docker-compose up -d api
```

### Build Without Git Metadata

If you just want to test without Git metadata:

```bash
docker-compose build
```

Default values will be used:
- `DD_GIT_REPOSITORY_URL`: `https://github.com/yourusername/datadog-maui`
- `DD_GIT_COMMIT_SHA`: `unknown`
- `DD_GIT_TAG`: `latest`

## Verification

### Check Image Metadata

```bash
# List built images
docker images | grep datadog-maui-api

# Inspect image labels
docker image inspect datadog-maui-api:latest | jq '.[0].Config.Labels'

# Check environment variables
docker image inspect datadog-maui-api:latest | jq '.[0].Config.Env'
```

### Check Running Container

```bash
# Check Git environment variables
docker exec datadog-maui-api env | grep DD_GIT

# Expected output:
# DD_GIT_REPOSITORY_URL=https://github.com/yourusername/datadog-maui
# DD_GIT_COMMIT_SHA=e7234e5094f719fdb50eef4f5449fd0afe252d99
# DD_GIT_TAG=e7234e5
```

### Verify in Datadog

Traces in Datadog will have these tags:
- `git.repository_url`
- `git.commit.sha`
- `git.tag`

Search in APM:
```
service:datadog-maui-api git.commit.sha:e7234e5094f719fdb50eef4f5449fd0afe252d99
```

## Troubleshooting

### Git metadata not set

**Problem:** Environment variables show `unknown` or default values

**Solution:**
```bash
# Make sure you source the script (not just run it)
source ./set-git-metadata.sh

# Verify variables are set in your shell
echo $DD_GIT_COMMIT_SHA
```

### Image tag is always "latest"

**Problem:** Image is tagged as `datadog-maui-api:latest` even after setting `DD_GIT_TAG`

**Solution:**
```bash
# Check if DD_GIT_TAG is exported
echo $DD_GIT_TAG

# Rebuild with explicit tag
DD_GIT_TAG=v1.0.0 docker-compose build
```

### Git commands fail

**Problem:** `git rev-parse HEAD` or other git commands return errors

**Solution:**
```bash
# Initialize git repository if needed
git init
git add .
git commit -m "Initial commit"

# Or use defaults
docker-compose build  # Uses default values
```

## CI/CD Integration

### GitHub Actions

```yaml
- name: Set Git metadata
  run: |
    echo "DD_GIT_REPOSITORY_URL=${{ github.server_url }}/${{ github.repository }}" >> $GITHUB_ENV
    echo "DD_GIT_COMMIT_SHA=${{ github.sha }}" >> $GITHUB_ENV
    echo "DD_GIT_TAG=${{ github.ref_name }}" >> $GITHUB_ENV
    echo "BUILD_DATE=$(date -u +"%Y-%m-%dT%H:%M:%SZ")" >> $GITHUB_ENV

- name: Build image
  run: docker-compose build
```

### GitLab CI

```yaml
variables:
  DD_GIT_REPOSITORY_URL: $CI_PROJECT_URL
  DD_GIT_COMMIT_SHA: $CI_COMMIT_SHA
  DD_GIT_TAG: $CI_COMMIT_TAG

build:
  script:
    - export BUILD_DATE=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
    - docker-compose build
```

## Build Arguments Reference

| Argument | Description | Default | Example |
|----------|-------------|---------|---------|
| `DD_GIT_REPOSITORY_URL` | Git repository URL | `https://github.com/yourusername/datadog-maui` | `https://github.com/myorg/myrepo` |
| `DD_GIT_COMMIT_SHA` | Full commit SHA | `unknown` | `e7234e5094f719fdb50eef4f5449fd0afe252d99` |
| `DD_GIT_TAG` | Git tag or short SHA | `latest` | `v1.0.0` or `e7234e5` |
| `BUILD_DATE` | ISO 8601 timestamp | `2025-12-31` | `2025-12-31T21:49:34Z` |

## Related Documentation

- [GIT_METADATA_INTEGRATION.md](GIT_METADATA_INTEGRATION.md) - Detailed Git metadata guide
- [docker-compose.yml](docker-compose.yml) - Build configuration
- [Api/Dockerfile](Api/Dockerfile) - Dockerfile with Git metadata support
- [set-git-metadata.sh](set-git-metadata.sh) - Helper script

---

**Quick Commands:**

```bash
# Clean rebuild with Git metadata
source ./set-git-metadata.sh && docker-compose build --no-cache && docker-compose up -d

# Check everything is working
docker-compose ps && docker exec datadog-maui-api env | grep DD_GIT && curl http://localhost:5000/health
```
