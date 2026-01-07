# Documentation Index

## Setup Guides

📁 **[setup/](setup/)**

- **[DATADOG_AGENT_SETUP.md](setup/DATADOG_AGENT_SETUP.md)** - Complete Datadog Agent configuration guide
- **[SETUP_COMPLETE.md](setup/SETUP_COMPLETE.md)** - Current setup status and verification

## iOS Development

📁 **[ios/](ios/)**

- **[BUILD_CONFIGURATION.md](ios/BUILD_CONFIGURATION.md)** - Critical iOS build configuration for Xcode 26.0
- **[SDK_VERSION_FIX.md](ios/SDK_VERSION_FIX.md)** - Quick reference for iOS SDK 26.0 workaround
- **[CRASH_REPORTING.md](ios/CRASH_REPORTING.md)** - Complete dSYM crash reporting guide
- **[scripts/upload-dsyms.sh](ios/scripts/upload-dsyms.sh)** - Automation script for uploading dSYMs to Datadog

## Deployment

📁 **[deployment/](deployment/)**

- **[AZURE_QUICK_START.md](deployment/AZURE_QUICK_START.md)** - Quick decision guide for Azure deployment options
- **[AZURE_FUNCTIONS_MIGRATION.md](deployment/AZURE_FUNCTIONS_MIGRATION.md)** - Complete guide for migrating to Azure Functions
- **[DOCKERFILE_COMPARISON.md](deployment/DOCKERFILE_COMPARISON.md)** - Comparison of standard vs Azure Functions Dockerfiles

## Feature Guides

📁 **[guides/](guides/)**

- **[BUILD.md](guides/BUILD.md)** - Building the Docker images with Git metadata
- **[TRACE_LOG_CORRELATION.md](guides/TRACE_LOG_CORRELATION.md)** - Trace and log correlation configuration
- **[GIT_METADATA_INTEGRATION.md](guides/GIT_METADATA_INTEGRATION.md)** - Git metadata for Datadog CI integration

## Technical Reference

📁 **[reference/](reference/)**

- **[CORRELATION_SUCCESS.md](reference/CORRELATION_SUCCESS.md)** - Log correlation verification and examples
- **[GIT_METADATA_COMPLETE.md](reference/GIT_METADATA_COMPLETE.md)** - Git metadata completion status

## Archive

📁 **[archive/](archive/)** - Historical documentation and migration guides

---

## Quick Links

### Getting Started
1. [Main README](../README.md) - Project overview
2. [Quickstart Guide](../QUICKSTART.md) - Get up and running quickly
3. [Setup Status](setup/SETUP_COMPLETE.md) - Verify your configuration

### Common Tasks
- [Build the API](guides/BUILD.md)
- [Configure iOS builds](ios/BUILD_CONFIGURATION.md)
- [Fix iOS SDK version issues](ios/SDK_VERSION_FIX.md)
- [Set up iOS crash reporting](ios/CRASH_REPORTING.md)
- [Deploy to Azure](deployment/AZURE_QUICK_START.md)
- [Configure Datadog Agent](setup/DATADOG_AGENT_SETUP.md)
- [Set up log correlation](guides/TRACE_LOG_CORRELATION.md)
- [Add Git metadata](guides/GIT_METADATA_INTEGRATION.md)

### Verification
- [Check correlation](reference/CORRELATION_SUCCESS.md)
- [Verify Git metadata](reference/GIT_METADATA_COMPLETE.md)
- [View setup status](setup/SETUP_COMPLETE.md)
