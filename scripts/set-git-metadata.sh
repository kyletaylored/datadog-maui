#!/bin/bash

# Extract Git metadata and load .env for docker-compose
# Usage: source ./set-git-metadata.sh && docker-compose build

# Load .env file if it exists
if [ -f .env ]; then
    set -a  # automatically export all variables
    source .env
    set +a
fi

# Extract Git metadata
export DD_GIT_COMMIT_SHA=$(git rev-parse HEAD 2>/dev/null || echo "unknown")
export DD_GIT_TAG=$(git describe --tags --exact-match 2>/dev/null || git rev-parse --short HEAD 2>/dev/null || echo "latest")
export DD_GIT_REPOSITORY_URL=$(git config --get remote.origin.url 2>/dev/null || echo "https://github.com/yourusername/datadog-maui")
export BUILD_DATE=$(date -u +"%Y-%m-%dT%H:%M:%SZ")

echo "✅ Environment variables loaded from .env"
echo ""
echo "Git metadata exported:"
echo "  DD_GIT_COMMIT_SHA:      $DD_GIT_COMMIT_SHA"
echo "  DD_GIT_TAG:             $DD_GIT_TAG"
echo "  DD_GIT_REPOSITORY_URL:  $DD_GIT_REPOSITORY_URL"
echo "  BUILD_DATE:             $BUILD_DATE"
echo ""
echo "RUM credentials loaded:"
echo "  DD_RUM_WEB_CLIENT_TOKEN:    ${DD_RUM_WEB_CLIENT_TOKEN:0:10}... (truncated)"
echo "  DD_RUM_WEB_APPLICATION_ID:  ${DD_RUM_WEB_APPLICATION_ID:0:8}... (truncated)"
echo ""
echo "Now run: docker-compose build"
