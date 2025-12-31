# Changelog

## 2025-12-31 - Git Metadata Integration & Documentation Organization

### Added

#### Git Metadata Integration
- **docker-compose.yml**: Added build args for Git metadata (DD_GIT_REPOSITORY_URL, DD_GIT_COMMIT_SHA, DD_GIT_TAG, BUILD_DATE)
- **docker-compose.yml**: Added image naming with tag: `datadog-maui-api:${DD_GIT_TAG:-latest}`
- **Api/Dockerfile**: Added ARG directives for Git metadata
- **Api/Dockerfile**: Added ENV directives to pass Git metadata to runtime
- **Api/Dockerfile**: Added Docker labels (OCI standard + Datadog-specific)
- **set-git-metadata.sh**: Helper script to extract and export Git metadata automatically

#### Makefile Enhancements
- Updated `api-build` to use docker-compose and automatically set Git metadata
- Added `api-build-simple` for legacy single-container build
- Updated `api-start` to use docker-compose (starts both API and Datadog Agent)
- Updated `api-stop` to use docker-compose
- Updated `api-logs` to use docker-compose
- Added `agent-logs` command to view Datadog Agent logs
- Added `logs-all` command to view all container logs
- Updated `api-status` to show docker-compose status and Datadog Agent info
- Updated `api-clean` to use docker-compose with volume cleanup
- Updated help text to reflect new commands

#### Documentation Organization
Created organized documentation structure:
```
docs/
├── README.md                    # Documentation index
├── setup/                       # Setup guides
│   ├── DATADOG_AGENT_SETUP.md
│   └── SETUP_COMPLETE.md
├── guides/                      # Feature guides
│   ├── BUILD.md
│   ├── TRACE_LOG_CORRELATION.md
│   └── GIT_METADATA_INTEGRATION.md
├── reference/                   # Technical reference
│   ├── CORRELATION_SUCCESS.md
│   └── GIT_METADATA_COMPLETE.md
└── archive/                     # Historical docs
    └── (17 archived files)
```

New documentation files:
- **docs/README.md**: Documentation index with quick links
- **docs/guides/BUILD.md**: Build instructions with Git metadata
- **docs/guides/GIT_METADATA_INTEGRATION.md**: Complete Git metadata guide
- **docs/reference/GIT_METADATA_COMPLETE.md**: Git metadata completion status

### Changed

#### Configuration Files
- **docker-compose.yml**: Modified API service to include build args and image name
- **Makefile**: Updated commands to use docker-compose instead of standalone docker commands
- **README.md**: Updated documentation links to point to new docs structure

#### Documentation
Moved files to organized structure:
- **Setup guides** → `docs/setup/`
- **Feature guides** → `docs/guides/`
- **Reference docs** → `docs/reference/`
- **Historical docs** → `docs/archive/`

### Benefits

#### For Development
- Git metadata automatically embedded in Docker images
- Easy-to-use Makefile commands for common tasks
- Organized documentation structure
- Helper script for local development builds

#### For Datadog Integration
- All traces, logs, and metrics tagged with Git commit SHA
- Deployment tracking based on Git tags
- Source code correlation in Datadog UI
- Error tracking by version

#### For CI/CD
- Standard build args for Git metadata
- Docker labels following OCI standards
- Ready for GitHub Actions, GitLab CI, Jenkins
- Consistent image tagging strategy

## Previous Changes

### 2025-12-31 - Trace and Log Correlation
- Added JSON console logging with scope support
- Configured `DD_LOGS_INJECTION=true` for automatic trace ID injection
- Verified trace and log correlation working

### 2025-12-31 - Datadog Agent Setup
- Added Datadog Agent container to docker-compose.yml
- Configured API to send traces to agent
- Updated Dockerfile to install Datadog .NET tracer v3.34.0 via .deb package
- Verified automatic instrumentation working

### Earlier
- Initial project setup with .NET MAUI mobile app
- ASP.NET Core API backend
- Docker containerization
- Basic Datadog integration
