#!/bin/bash

#
# Upload iOS dSYM files to Datadog for crash symbolication
#
# Usage:
#   ./upload-dsyms.sh [version]
#
# Environment Variables:
#   DATADOG_API_KEY - Your Datadog API key (required)
#   DD_SITE         - Datadog site (default: datadoghq.com)
#
# Example:
#   export DATADOG_API_KEY=your_api_key
#   ./upload-dsyms.sh 1.2.3
#

set -e

# Configuration
SERVICE_NAME="com.datadog.mauiapp"
BUILD_DIR="bin/Release/net10.0-ios/ios-arm64"
VERSION="${1:-$(date +%Y%m%d-%H%M%S)}"
DD_SITE="${DD_SITE:-datadoghq.com}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "🐕 Datadog dSYM Upload Script"
echo "=============================="
echo ""

# Check if DATADOG_API_KEY is set
if [ -z "$DATADOG_API_KEY" ]; then
    echo -e "${RED}❌ Error: DATADOG_API_KEY environment variable is not set${NC}"
    echo ""
    echo "Please set your Datadog API key:"
    echo "  export DATADOG_API_KEY=your_api_key_here"
    echo ""
    exit 1
fi

# Check if datadog-ci is installed
if ! command -v datadog-ci &> /dev/null; then
    echo -e "${YELLOW}⚠️  Datadog CLI not found. Installing...${NC}"
    npm install -g @datadog/datadog-ci
    echo ""
fi

# Check if build directory exists
if [ ! -d "$BUILD_DIR" ]; then
    echo -e "${RED}❌ Error: Build directory not found: $BUILD_DIR${NC}"
    echo ""
    echo "Please build the Release version first:"
    echo "  dotnet build -c Release -f net10.0-ios -p:RuntimeIdentifier=ios-arm64"
    echo ""
    exit 1
fi

# Find all dSYM files
echo "🔍 Looking for dSYM files in $BUILD_DIR..."
DSYM_FILES=$(find "$BUILD_DIR" -name "*.dSYM" -type d)

if [ -z "$DSYM_FILES" ]; then
    echo -e "${RED}❌ Error: No dSYM files found in $BUILD_DIR${NC}"
    echo ""
    echo "dSYM files are generated during Release builds. Make sure:"
    echo "  1. You built in Release mode: -c Release"
    echo "  2. MtouchSymbolsList=true in the csproj"
    echo "  3. Building for physical device: ios-arm64"
    echo ""
    exit 1
fi

echo -e "${GREEN}✅ Found dSYM files:${NC}"
echo "$DSYM_FILES" | while read -r dsym; do
    echo "  - $(basename "$dsym")"

    # Get UUID for reference
    UUID=$(xcrun dwarfdump --uuid "$dsym" 2>/dev/null | head -n 1 | awk '{print $2}')
    if [ -n "$UUID" ]; then
        echo "    UUID: $UUID"
    fi
done
echo ""

# Upload dSYMs to Datadog
echo "📤 Uploading dSYMs to Datadog..."
echo "  Service: $SERVICE_NAME"
echo "  Version: $VERSION"
echo "  Site: $DD_SITE"
echo ""

# Get repository URL from git (if available)
REPO_URL=""
if git rev-parse --git-dir > /dev/null 2>&1; then
    REPO_URL=$(git config --get remote.origin.url 2>/dev/null || echo "")
fi

# Build upload command
UPLOAD_CMD="datadog-ci dsyms upload"
UPLOAD_CMD="$UPLOAD_CMD --service $SERVICE_NAME"
UPLOAD_CMD="$UPLOAD_CMD --version $VERSION"

if [ -n "$REPO_URL" ]; then
    UPLOAD_CMD="$UPLOAD_CMD --repository-url $REPO_URL"
fi

# Add all dSYM files to the command
echo "$DSYM_FILES" | while read -r dsym; do
    UPLOAD_CMD="$UPLOAD_CMD \"$dsym\""
done

# Execute upload
eval $UPLOAD_CMD

if [ $? -eq 0 ]; then
    echo ""
    echo -e "${GREEN}✅ dSYM upload successful!${NC}"
    echo ""
    echo "Next steps:"
    echo "  1. Verify upload:"
    echo "     datadog-ci dsyms list --service $SERVICE_NAME"
    echo ""
    echo "  2. Test crash reporting on a physical device"
    echo ""
    echo "  3. View crashes in Datadog:"
    echo "     https://app.$DD_SITE/rum/error-tracking"
    echo ""
else
    echo ""
    echo -e "${RED}❌ dSYM upload failed${NC}"
    echo ""
    echo "Check the error message above for details."
    echo ""
    exit 1
fi

# Optional: List uploaded dSYMs
echo "📋 Listing uploaded dSYMs for verification..."
datadog-ci dsyms list --service "$SERVICE_NAME" | head -n 20

echo ""
echo -e "${GREEN}🎉 Done!${NC}"
