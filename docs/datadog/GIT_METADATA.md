# Git Metadata Integration for Datadog CI

## Overview

The Docker image now includes Git repository metadata, enabling Datadog's CI Visibility features to correlate APM traces, logs, and metrics with source code commits, tags, and repository information.

## What's Configured

### 1. Docker Image Naming and Tagging

**File:** [docker-compose.yml](docker-compose.yml)

```yaml
api:
  image: datadog-maui-api:${DD_GIT_TAG:-latest}
```

The image is automatically tagged with the Git tag or commit SHA, making it easy to identify which code version is running.

### 2. Git Metadata Build Args

**File:** [docker-compose.yml](docker-compose.yml)

```yaml
build:
  args:
    - DD_GIT_REPOSITORY_URL=${DD_GIT_REPOSITORY_URL:-https://github.com/yourusername/datadog-maui}
    - DD_GIT_COMMIT_SHA=${DD_GIT_COMMIT_SHA:-unknown}
    - DD_GIT_TAG=${DD_GIT_TAG:-latest}
    - BUILD_DATE=${BUILD_DATE:-2025-12-31}
```

These build args are passed to the Dockerfile and used to set environment variables and labels.

### 3. Runtime Environment Variables

**File:** [Api/Dockerfile](Api/Dockerfile)

```dockerfile
ENV DD_GIT_REPOSITORY_URL=${DD_GIT_REPOSITORY_URL}
ENV DD_GIT_COMMIT_SHA=${DD_GIT_COMMIT_SHA}
ENV DD_GIT_TAG=${DD_GIT_TAG}
```

The Datadog tracer reads these environment variables and automatically tags all traces, metrics, and logs with Git metadata.

### 4. Docker Image Labels

**File:** [Api/Dockerfile](Api/Dockerfile)

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

These labels follow both OCI standards and Datadog conventions, making the image metadata queryable.

## Building with Git Metadata

### Option 1: Using the Helper Script (Recommended)

```bash
# Set Git metadata as environment variables
source ./set-git-metadata.sh

# Build the image
docker-compose build

# Start the containers
docker-compose up -d
```

The script automatically extracts:
- **DD_GIT_COMMIT_SHA**: Current commit SHA (e.g., `e7234e5094f719fdb50eef4f5449fd0afe252d99`)
- **DD_GIT_TAG**: Current Git tag or short commit SHA (e.g., `v1.0.0` or `e7234e5`)
- **DD_GIT_REPOSITORY_URL**: Remote origin URL (e.g., `https://github.com/yourusername/datadog-maui`)
- **BUILD_DATE**: Current UTC timestamp in ISO 8601 format

### Option 2: Manual Build

```bash
# Export environment variables manually
export DD_GIT_COMMIT_SHA=$(git rev-parse HEAD)
export DD_GIT_TAG=$(git describe --tags --exact-match 2>/dev/null || git rev-parse --short HEAD)
export DD_GIT_REPOSITORY_URL=$(git config --get remote.origin.url)
export BUILD_DATE=$(date -u +"%Y-%m-%dT%H:%M:%SZ")

# Build
docker-compose build
```

### Option 3: Override in CI/CD

```bash
# In your CI/CD pipeline (GitHub Actions, GitLab CI, etc.)
DD_GIT_REPOSITORY_URL=$GITHUB_REPOSITORY_URL \
DD_GIT_COMMIT_SHA=$GITHUB_SHA \
DD_GIT_TAG=$GITHUB_REF_NAME \
BUILD_DATE=$(date -u +"%Y-%m-%dT%H:%M:%SZ") \
docker-compose build
```

## Verifying Git Metadata

### 1. Check Docker Image Labels

```bash
docker image inspect datadog-maui-api:latest | jq '.[0].Config.Labels'
```

Expected output:
```json
{
  "org.opencontainers.image.created": "2025-12-31T21:30:00Z",
  "org.opencontainers.image.source": "https://github.com/yourusername/datadog-maui",
  "org.opencontainers.image.revision": "e7234e5094f719fdb50eef4f5449fd0afe252d99",
  "org.opencontainers.image.version": "latest",
  "com.datadoghq.tags.service": "datadog-maui-api",
  "git.repository_url": "https://github.com/yourusername/datadog-maui",
  "git.commit.sha": "e7234e5094f719fdb50eef4f5449fd0afe252d99"
}
```

### 2. Check Container Environment Variables

```bash
docker exec datadog-maui-api env | grep DD_GIT
```

Expected output:
```
DD_GIT_REPOSITORY_URL=https://github.com/yourusername/datadog-maui
DD_GIT_COMMIT_SHA=e7234e5094f719fdb50eef4f5449fd0afe252d99
DD_GIT_TAG=latest
```

### 3. Check Datadog Tags in Traces

In the Datadog APM UI, traces from `datadog-maui-api` will have these tags:
- `git.repository_url`
- `git.commit.sha`
- `git.tag`

You can filter traces by commit:
```
service:datadog-maui-api git.commit.sha:e7234e5094f719fdb50eef4f5449fd0afe252d99
```

## Benefits

### 1. Source Code Correlation
- Click on a trace in Datadog → jump directly to the source code commit
- See which version of code produced errors
- Track performance changes across commits

### 2. Deployment Tracking
- Automatically track deployments in Datadog
- Compare metrics before/after deployments
- Correlate spikes with specific code changes

### 3. Image Provenance
- Know exactly what code is in each container
- Audit trail for compliance
- Quick rollback identification

### 4. CI/CD Integration
- Link CI pipeline runs to APM traces
- Track build artifacts through deployment
- Unified observability across CI and production

## Integration with Datadog Features

### APM Traces
Every trace automatically includes Git metadata tags:
```json
{
  "service": "datadog-maui-api",
  "git.repository_url": "https://github.com/yourusername/datadog-maui",
  "git.commit.sha": "e7234e5094f719fdb50eef4f5449fd0afe252d99",
  "git.tag": "v1.0.0"
}
```

### Logs
Logs structured with Git context:
```json
{
  "dd_service": "datadog-maui-api",
  "dd_version": "v1.0.0",
  "git_commit": "e7234e5094f719fdb50eef4f5449fd0afe252d99"
}
```

### Deployment Tracking
Datadog automatically detects new versions and creates deployment markers when `DD_VERSION` changes.

### Error Tracking
Errors are grouped by version, making it easy to see which commit introduced a bug:
- **Errors by Version**: See error rates for each Git tag
- **Blame View**: Click error → see commit that introduced it
- **Diff View**: Compare error-free vs problematic commits

## CI/CD Pipeline Examples

### GitHub Actions

```yaml
name: Build and Deploy

on:
  push:
    branches: [main]
    tags: ['v*']

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Build with Git metadata
        env:
          DD_GIT_REPOSITORY_URL: ${{ github.server_url }}/${{ github.repository }}
          DD_GIT_COMMIT_SHA: ${{ github.sha }}
          DD_GIT_TAG: ${{ github.ref_name }}
          BUILD_DATE: ${{ github.event.head_commit.timestamp }}
        run: docker-compose build
```

### GitLab CI

```yaml
build:
  stage: build
  script:
    - export DD_GIT_REPOSITORY_URL=$CI_PROJECT_URL
    - export DD_GIT_COMMIT_SHA=$CI_COMMIT_SHA
    - export DD_GIT_TAG=$CI_COMMIT_TAG
    - export BUILD_DATE=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
    - docker-compose build
```

### Jenkins

```groovy
pipeline {
  agent any

  environment {
    DD_GIT_REPOSITORY_URL = "${GIT_URL}"
    DD_GIT_COMMIT_SHA = "${GIT_COMMIT}"
    DD_GIT_TAG = "${GIT_BRANCH}"
    BUILD_DATE = sh(script: 'date -u +"%Y-%m-%dT%H:%M:%SZ"', returnStdout: true).trim()
  }

  stages {
    stage('Build') {
      steps {
        sh 'docker-compose build'
      }
    }
  }
}
```

## Tagging Strategy

### Development Builds
```bash
# Use short commit SHA as tag
DD_GIT_TAG=$(git rev-parse --short HEAD)  # e.g., e7234e5
```

### Release Builds
```bash
# Use semantic version tag
DD_GIT_TAG=$(git describe --tags --exact-match)  # e.g., v1.0.0
```

### Branch Builds
```bash
# Use branch name with short SHA
DD_GIT_TAG="${BRANCH_NAME}-$(git rev-parse --short HEAD)"  # e.g., feature-xyz-e7234e5
```

## Troubleshooting

### Git metadata not appearing in traces

**Check:**
1. Environment variables are set during build:
   ```bash
   docker exec datadog-maui-api env | grep DD_GIT
   ```

2. Datadog tracer is loaded:
   ```bash
   docker-compose logs api | grep "Datadog Tracer"
   ```

3. Tags are being sent to agent:
   ```bash
   docker exec datadog-agent agent status | grep git
   ```

### Image not being tagged correctly

**Check:**
1. Environment variable is set before build:
   ```bash
   echo $DD_GIT_TAG
   ```

2. Default value is used if not set:
   ```bash
   docker images | grep datadog-maui-api
   # Should show: datadog-maui-api:latest (or your tag)
   ```

### Labels not appearing in image

**Check:**
1. Build args were passed:
   ```bash
   docker-compose config
   # Look for 'args:' section under api service
   ```

2. Rebuild image:
   ```bash
   docker-compose build --no-cache api
   ```

## Best Practices

### 1. Always Use Source Control
Even for local development, initialize Git to get automatic metadata:
```bash
git init
git add .
git commit -m "Initial commit"
```

### 2. Tag Releases
Use semantic versioning for releases:
```bash
git tag -a v1.0.0 -m "Release version 1.0.0"
git push origin v1.0.0
```

### 3. Use Short SHAs for Dev
During development, short commit SHAs are more readable:
```bash
DD_GIT_TAG=$(git rev-parse --short HEAD)
```

### 4. Set in CI/CD
Always populate Git metadata in CI/CD pipelines for production builds.

### 5. Include Build Date
Build timestamps help with troubleshooting and auditing:
```bash
BUILD_DATE=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
```

## Documentation References

- [Datadog Git Metadata](https://docs.datadoghq.com/tracing/version_tracking/)
- [OCI Image Spec](https://github.com/opencontainers/image-spec/blob/main/annotations.md)
- [Datadog Deployment Tracking](https://docs.datadoghq.com/tracing/deployment_tracking/)

---

**Status:** ✅ Fully Configured

**Files Modified:**
- [docker-compose.yml](docker-compose.yml) - Added build args and image name/tag
- [Api/Dockerfile](Api/Dockerfile) - Added ARG, ENV, and LABEL directives

**Files Created:**
- [set-git-metadata.sh](set-git-metadata.sh) - Helper script for local builds

**Usage:**
```bash
# Local development
source ./set-git-metadata.sh && docker-compose build

# CI/CD
# Set DD_GIT_* environment variables in your pipeline
docker-compose build
```
