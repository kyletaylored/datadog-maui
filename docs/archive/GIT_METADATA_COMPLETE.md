# ✅ Git Metadata Integration - COMPLETE

## Summary

Successfully integrated Git repository metadata into the Docker build process for Datadog CI Visibility. The container image now includes source code correlation, enabling seamless navigation between APM traces, logs, and the exact Git commit that produced them.

## What Was Added

### 1. Docker Image Naming and Tagging ✅

**File:** [docker-compose.yml](docker-compose.yml:37)

```yaml
image: datadog-maui-api:${DD_GIT_TAG:-latest}
```

The image is now automatically tagged with:
- Git tag for releases (e.g., `v1.0.0`)
- Short commit SHA for development (e.g., `e7234e5`)
- `latest` as fallback

### 2. Build Arguments for Git Metadata ✅

**File:** [docker-compose.yml](docker-compose.yml:32-36)

```yaml
args:
  - DD_GIT_REPOSITORY_URL=${DD_GIT_REPOSITORY_URL:-https://github.com/yourusername/datadog-maui}
  - DD_GIT_COMMIT_SHA=${DD_GIT_COMMIT_SHA:-unknown}
  - DD_GIT_TAG=${DD_GIT_TAG:-latest}
  - BUILD_DATE=${BUILD_DATE:-2025-12-31}
```

Git metadata is passed to the Dockerfile during build.

### 3. Runtime Environment Variables ✅

**File:** [Api/Dockerfile](Api/Dockerfile:64-66)

```dockerfile
ENV DD_GIT_REPOSITORY_URL=${DD_GIT_REPOSITORY_URL}
ENV DD_GIT_COMMIT_SHA=${DD_GIT_COMMIT_SHA}
ENV DD_GIT_TAG=${DD_GIT_TAG}
```

The Datadog .NET tracer reads these environment variables and automatically tags all telemetry with Git metadata.

### 4. OCI and Datadog Labels ✅

**File:** [Api/Dockerfile](Api/Dockerfile:69-80)

```dockerfile
# OCI standard labels
LABEL org.opencontainers.image.created="${BUILD_DATE}" \
      org.opencontainers.image.source="${DD_GIT_REPOSITORY_URL}" \
      org.opencontainers.image.revision="${DD_GIT_COMMIT_SHA}" \
      org.opencontainers.image.version="${DD_GIT_TAG}" \
      org.opencontainers.image.title="Datadog MAUI API" \
      org.opencontainers.image.description=".NET API with Datadog APM tracing for MAUI mobile app"

# Datadog-specific labels
LABEL com.datadoghq.tags.service="datadog-maui-api" \
      com.datadoghq.tags.env="local" \
      com.datadoghq.tags.version="${DD_GIT_TAG}" \
      git.repository_url="${DD_GIT_REPOSITORY_URL}" \
      git.commit.sha="${DD_GIT_COMMIT_SHA}" \
      git.tag="${DD_GIT_TAG}"
```

Image metadata follows industry standards (OCI) and Datadog conventions.

### 5. Helper Script for Local Development ✅

**File:** [set-git-metadata.sh](set-git-metadata.sh)

```bash
#!/bin/bash
export DD_GIT_COMMIT_SHA=$(git rev-parse HEAD 2>/dev/null || echo "unknown")
export DD_GIT_TAG=$(git describe --tags --exact-match 2>/dev/null || git rev-parse --short HEAD 2>/dev/null || echo "latest")
export DD_GIT_REPOSITORY_URL=$(git config --get remote.origin.url 2>/dev/null || echo "https://github.com/yourusername/datadog-maui")
export BUILD_DATE=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
```

Automatically extracts Git metadata from the current repository.

## How to Use

### Local Development

```bash
# 1. Source the helper script to set environment variables
source ./set-git-metadata.sh

# Output:
# Git metadata exported:
#   DD_GIT_COMMIT_SHA:      e7234e5094f719fdb50eef4f5449fd0afe252d99
#   DD_GIT_TAG:             e7234e5
#   DD_GIT_REPOSITORY_URL:  https://github.com/yourusername/datadog-maui
#   BUILD_DATE:             2025-12-31T21:49:34Z

# 2. Build the image
docker-compose build

# 3. Start containers
docker-compose up -d
```

### CI/CD Pipeline

Set environment variables in your pipeline:

```yaml
# GitHub Actions
env:
  DD_GIT_REPOSITORY_URL: ${{ github.server_url }}/${{ github.repository }}
  DD_GIT_COMMIT_SHA: ${{ github.sha }}
  DD_GIT_TAG: ${{ github.ref_name }}
  BUILD_DATE: ${{ github.event.head_commit.timestamp }}

# GitLab CI
variables:
  DD_GIT_REPOSITORY_URL: $CI_PROJECT_URL
  DD_GIT_COMMIT_SHA: $CI_COMMIT_SHA
  DD_GIT_TAG: $CI_COMMIT_TAG
```

Then run `docker-compose build` as normal.

## Verification

### 1. Check Environment Variables

```bash
docker exec datadog-maui-api env | grep DD_GIT
```

**Expected output:**
```
DD_GIT_REPOSITORY_URL=https://github.com/yourusername/datadog-maui
DD_GIT_COMMIT_SHA=e7234e5094f719fdb50eef4f5449fd0afe252d99
DD_GIT_TAG=e7234e5
```

### 2. Check Docker Image Labels

```bash
docker image inspect datadog-maui-api:latest | jq '.[0].Config.Labels' | grep git
```

**Expected output:**
```json
"git.commit.sha": "e7234e5094f719fdb50eef4f5449fd0afe252d99",
"git.repository_url": "https://github.com/yourusername/datadog-maui",
"git.tag": "e7234e5",
"org.opencontainers.image.revision": "e7234e5094f719fdb50eef4f5449fd0afe252d99",
"org.opencontainers.image.source": "https://github.com/yourusername/datadog-maui"
```

### 3. Check Image Tag

```bash
docker images | grep datadog-maui-api
```

**Expected output:**
```
datadog-maui-api   e7234e5   abc123def456   2 minutes ago   220MB
```

### 4. Verify in Datadog APM

In the Datadog UI, traces will have these tags:
- `git.repository_url:https://github.com/yourusername/datadog-maui`
- `git.commit.sha:e7234e5094f719fdb50eef4f5449fd0afe252d99`
- `git.tag:e7234e5`

Search for traces by commit:
```
service:datadog-maui-api git.commit.sha:e7234e5094f719fdb50eef4f5449fd0afe252d99
```

## Benefits

### 1. Source Code Correlation ✅

Click on a trace or error in Datadog → jump directly to the exact commit in GitHub/GitLab that produced it.

**Example Flow:**
1. See error in Datadog APM
2. Error shows `git.commit.sha:e7234e5094f719fdb50eef4f5449fd0afe252d99`
3. Click link → opens GitHub at that exact commit
4. See the code that caused the error

### 2. Deployment Tracking ✅

Datadog automatically detects new versions when `DD_VERSION` (derived from `DD_GIT_TAG`) changes.

**In Datadog UI:**
- Deployment markers on graphs
- Compare metrics before/after deployment
- Correlate performance changes with code changes

### 3. Version-Based Error Tracking ✅

Errors are grouped by version, showing which commits introduced bugs:

**Datadog Error Tracking:**
- **v1.0.0**: 0 errors
- **v1.1.0**: 45 errors (← regression!)
- **v1.1.1**: 2 errors (← mostly fixed)

### 4. Container Image Provenance ✅

Know exactly what code is running in production:

```bash
# In production
docker exec prod-api env | grep DD_GIT_COMMIT_SHA
# DD_GIT_COMMIT_SHA=e7234e5094f719fdb50eef4f5449fd0afe252d99

# Check what commit is deployed
git log e7234e5094f719fdb50eef4f5449fd0afe252d99
```

### 5. CI/CD Integration ✅

Link CI pipeline runs to production traces:
- See which build produced the deployed image
- Track artifacts through entire pipeline
- Unified observability from build → deploy → production

## Complete Observability Stack

```
Git Commit (e7234e5)
  ↓
CI/CD Build
  ↓ (embeds Git metadata)
Docker Image (datadog-maui-api:e7234e5)
  ↓ (includes labels and env vars)
Running Container
  ↓ (Datadog tracer reads env vars)
APM Traces + Logs + Metrics
  ↓ (all tagged with Git metadata)
Datadog Cloud
  ↓
  ┌─────────────────────────────────────┐
  │ Unified View:                       │
  │ - Trace showing error               │
  │ - Logs from that request            │
  │ - Git commit that introduced bug    │
  │ - Source code at that commit        │
  │ - Deployment timeline               │
  │ - Related infrastructure metrics    │
  └─────────────────────────────────────┘
```

## Files Modified

### Configuration Files
- ✅ [docker-compose.yml](docker-compose.yml) - Added build args and image name/tag
- ✅ [Api/Dockerfile](Api/Dockerfile) - Added ARG, ENV, and LABEL directives for Git metadata

### Helper Scripts
- ✅ [set-git-metadata.sh](set-git-metadata.sh) - Extracts Git metadata for local builds

### Documentation
- ✅ [GIT_METADATA_INTEGRATION.md](GIT_METADATA_INTEGRATION.md) - Comprehensive integration guide
- ✅ [BUILD.md](BUILD.md) - Build instructions and quick reference
- ✅ [GIT_METADATA_COMPLETE.md](GIT_METADATA_COMPLETE.md) - This file (completion status)
- ✅ [SETUP_COMPLETE.md](SETUP_COMPLETE.md) - Updated with Git metadata reference

## Example Datadog Queries

### Find Traces by Commit

```
service:datadog-maui-api git.commit.sha:e7234e5094f719fdb50eef4f5449fd0afe252d99
```

### Compare Performance Across Versions

```
service:datadog-maui-api git.tag:v1.0.0  # Old version
service:datadog-maui-api git.tag:v1.1.0  # New version
```

### Find Errors from Specific Repository

```
service:datadog-maui-api status:error git.repository_url:*datadog-maui*
```

### Track Recent Deployments

```
service:datadog-maui-api @deployment.event:true
```

## Testing the Integration

### Test 1: Local Build

```bash
# Build with Git metadata
source ./set-git-metadata.sh
docker-compose build

# Verify environment variables
docker exec datadog-maui-api env | grep DD_GIT

# Make API call to generate trace
curl -X POST http://localhost:5000/data \
  -H "Content-Type: application/json" \
  -d '{"CorrelationId":"git-test","SessionName":"Test","Notes":"Testing Git metadata","NumericValue":999}'

# Check Datadog for trace with git.* tags
```

### Test 2: Image Labels

```bash
# Inspect labels
docker image inspect datadog-maui-api:e7234e5 | jq '.[0].Config.Labels'

# Should include:
# - org.opencontainers.image.revision
# - git.commit.sha
# - git.repository_url
# - com.datadoghq.tags.version
```

### Test 3: Tag Variations

```bash
# Test with specific tag
DD_GIT_TAG=test-build docker-compose build
docker images | grep datadog-maui-api

# Should show:
# datadog-maui-api   test-build   ...

# Test with version tag
DD_GIT_TAG=v1.0.0 docker-compose build
docker images | grep datadog-maui-api

# Should show:
# datadog-maui-api   v1.0.0   ...
```

## Troubleshooting

### Problem: Git metadata shows "unknown"

**Solution:**
```bash
# Make sure you're sourcing the script, not executing it
source ./set-git-metadata.sh  # ✅ Correct

# Not:
./set-git-metadata.sh  # ❌ Won't export to parent shell
```

### Problem: Image tag is always "latest"

**Solution:**
```bash
# Check DD_GIT_TAG is exported
echo $DD_GIT_TAG

# If empty, source the script
source ./set-git-metadata.sh

# Or set manually
export DD_GIT_TAG=v1.0.0
docker-compose build
```

### Problem: Labels not in image

**Solution:**
```bash
# Rebuild with no cache
docker-compose build --no-cache api

# Verify args were passed
docker-compose config | grep -A 10 "args:"
```

## Integration with Existing Features

### Works with APM Tracing ✅
Git metadata is automatically added to all traces via environment variables read by Datadog tracer.

### Works with Log Correlation ✅
Logs include Git metadata through the same environment variables:
```json
{
  "dd_service": "datadog-maui-api",
  "dd_version": "e7234e5",
  "git_commit": "e7234e5094f719fdb50eef4f5449fd0afe252d99"
}
```

### Works with RUM Correlation ✅
Mobile app correlation IDs link to backend traces, which now include Git metadata for full visibility.

## Documentation References

- [GIT_METADATA_INTEGRATION.md](GIT_METADATA_INTEGRATION.md) - Detailed guide with CI/CD examples
- [BUILD.md](BUILD.md) - Build commands and troubleshooting
- [docker-compose.yml](docker-compose.yml) - Build configuration
- [Api/Dockerfile](Api/Dockerfile) - Dockerfile with Git metadata support
- [Datadog Git Metadata Docs](https://docs.datadoghq.com/tracing/version_tracking/)
- [OCI Image Spec](https://github.com/opencontainers/image-spec/blob/main/annotations.md)

---

## Status: ✅ FULLY OPERATIONAL

**Verified:** 2025-12-31

### Configuration ✅
- Build args configured in docker-compose.yml
- Dockerfile ARG and ENV directives added
- Docker labels following OCI and Datadog standards
- Helper script created and tested

### Functionality ✅
- Image automatically tagged with Git metadata
- Environment variables passed to running container
- Labels embedded in Docker image
- Datadog tracer reads Git metadata from environment
- Traces, logs, and metrics tagged with Git info

### Documentation ✅
- Comprehensive integration guide
- Build instructions
- CI/CD examples (GitHub Actions, GitLab CI, Jenkins)
- Troubleshooting guide
- Verification steps

### Testing ✅
- Helper script extracts Git metadata correctly
- Environment variables set in container
- Docker labels present in image
- Image tagged with Git tag/commit SHA

**Next Steps:**
1. Deploy to production with CI/CD pipeline
2. Configure Datadog Source Code Integration for direct GitHub/GitLab links
3. Set up deployment tracking monitors in Datadog
4. Create dashboards showing metrics by Git version

**Quick Start:**
```bash
source ./set-git-metadata.sh && docker-compose build && docker-compose up -d
```
