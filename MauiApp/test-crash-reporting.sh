#!/bin/bash

#
# iOS Crash Reporting Test Script
#
# This script automates the process of:
# 1. Building a Release version with dSYMs
# 2. Uploading dSYMs to Datadog
# 3. Running the app to test crash reporting
#
# Usage:
#   ./test-crash-reporting.sh [version]
#
# Example:
#   export DATADOG_API_KEY=your_api_key
#   ./test-crash-reporting.sh 1.0.0-test
#

set -e

# Get the directory where the script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Change to script directory
cd "$SCRIPT_DIR"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🐕 iOS Crash Reporting Test Script${NC}"
echo "===================================="
echo ""

# Configuration
VERSION="${1:-$(date +%Y%m%d-%H%M%S)}"
SERVICE_NAME="com.datadog.mauiapp"
BUILD_CONFIG="Release"
TARGET_FRAMEWORK="net10.0-ios"
RUNTIME_ID="iossimulator-arm64"
PROJECT_FILE="$SCRIPT_DIR/DatadogMauiApp.csproj"
BIN_DIR="$SCRIPT_DIR/bin"

echo -e "${BLUE}Configuration:${NC}"
echo "  Script Dir: $SCRIPT_DIR"
echo "  Project: $(basename "$PROJECT_FILE")"
echo "  Version: $VERSION"
echo "  Service: $SERVICE_NAME"
echo "  Build: $BUILD_CONFIG"
echo "  Target: $TARGET_FRAMEWORK ($RUNTIME_ID)"
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

echo -e "${GREEN}✅ Datadog API key found${NC}"
echo ""

# Step 1: Clean previous builds
echo -e "${YELLOW}📦 Step 1: Cleaning previous builds...${NC}"
dotnet clean "$PROJECT_FILE" -c $BUILD_CONFIG -f $TARGET_FRAMEWORK
rm -rf "$BIN_DIR/$BUILD_CONFIG/$TARGET_FRAMEWORK"
echo -e "${GREEN}✅ Clean complete${NC}"
echo ""

# Step 2: Build Release version
echo -e "${YELLOW}🔨 Step 2: Building Release version with dSYMs...${NC}"
dotnet build "$PROJECT_FILE" -c $BUILD_CONFIG -f $TARGET_FRAMEWORK -p:RuntimeIdentifier=$RUNTIME_ID

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Build successful${NC}"
else
    echo -e "${RED}❌ Build failed${NC}"
    exit 1
fi
echo ""

# Step 3: Find dSYM files
echo -e "${YELLOW}🔍 Step 3: Locating dSYM files...${NC}"
DSYM_DIR="$BIN_DIR/$BUILD_CONFIG/$TARGET_FRAMEWORK/$RUNTIME_ID"
DSYM_FILES=$(find "$DSYM_DIR" -name "*.dSYM" -type d 2>/dev/null)

if [ -z "$DSYM_FILES" ]; then
    echo -e "${RED}❌ No dSYM files found in $DSYM_DIR${NC}"
    echo ""
    echo "Check that:"
    echo "  1. Build configuration is Release"
    echo "  2. MtouchSymbolsList=true in csproj"
    echo "  3. Building for simulator or device (not AnyCPU)"
    echo ""
    exit 1
fi

echo -e "${GREEN}✅ Found dSYM files.${NC}"

# Step 4: Check for datadog-ci
if ! command -v datadog-ci &> /dev/null; then
    echo -e "${YELLOW}⚠️  Datadog CLI not found. Installing...${NC}"
    npm install -g @datadog/datadog-ci
    echo ""
fi

echo -e "${GREEN}✅ Datadog CLI available${NC}"
echo ""

# Step 5: Create dSYM zip archive
echo -e "${YELLOW}📦 Step 5: Creating dSYM zip archive...${NC}"
DSYM_ZIP="$SCRIPT_DIR/dsyms-$VERSION.zip"

# Remove old zip if exists
rm -f "$DSYM_ZIP"

# Create zip from dSYM directory
cd "$DSYM_DIR"
# Use find to avoid shell glob expansion issues
find . -name "*.dSYM" -type d -print0 | xargs -0 zip -r "$DSYM_ZIP"

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ Created dSYM archive: $(basename "$DSYM_ZIP")${NC}"
    echo "   Size: $(du -h "$DSYM_ZIP" | cut -f1)"
else
    echo -e "${RED}❌ Failed to create dSYM archive${NC}"
    exit 1
fi

# Return to script directory
cd "$SCRIPT_DIR"
echo ""

# Step 6: Upload dSYMs to Datadog
echo -e "${YELLOW}📤 Step 6: Uploading dSYMs to Datadog...${NC}"
echo "  Service: $SERVICE_NAME"
echo "  Version: $VERSION"
echo "  Method: Directory upload"
echo ""

# Build upload command (using directory)
UPLOAD_CMD="datadog-ci dsyms upload $DSYM_DIR"

# Execute upload
echo "Upload command: $UPLOAD_CMD"
eval $UPLOAD_CMD

if [ $? -eq 0 ]; then
    echo ""
    echo -e "${GREEN}✅ dSYM upload successful!${NC}"
else
    echo ""
    echo -e "${YELLOW}⚠️  Directory upload failed, trying zip upload...${NC}"
    echo ""

    # Try zip upload as fallback
    ZIP_UPLOAD_CMD="datadog-ci dsyms upload $DSYM_ZIP"

    echo "Zip upload command: $ZIP_UPLOAD_CMD"
    eval $ZIP_UPLOAD_CMD

    if [ $? -eq 0 ]; then
        echo ""
        echo -e "${GREEN}✅ dSYM zip upload successful!${NC}"
    else
        echo ""
        echo -e "${RED}❌ Both directory and zip uploads failed${NC}"
        exit 1
    fi
fi
echo ""

# Step 7: Summary and instructions
echo -e "${GREEN}🎉 Setup Complete!${NC}"
echo ""
echo -e "${BLUE}📦 dSYM Archive Created:${NC}"
echo "   File: $(basename "$DSYM_ZIP")"
echo "   Path: $DSYM_ZIP"
echo ""
echo -e "${YELLOW}💡 Manual Upload Options:${NC}"
echo ""
echo "Upload from zip file:"
echo -e "   ${GREEN}npx @datadog/datadog-ci dsyms upload \"$DSYM_ZIP\" \\\\${NC}"
echo -e "   ${GREEN}     --service $SERVICE_NAME --version $VERSION${NC}"
echo ""
echo "Upload from directory:"
echo -e "   ${GREEN}npx @datadog/datadog-ci dsyms upload \"$DSYM_DIR\" \\\\${NC}"
echo -e "   ${GREEN}     --service $SERVICE_NAME --version $VERSION${NC}"
echo ""
echo "Verify upload:"
echo -e "   ${GREEN}npx @datadog/datadog-ci dsyms list --service $SERVICE_NAME${NC}"
echo ""
echo -e "${BLUE}Next Steps for Testing:${NC}"
echo ""
echo "1. Run the app on iOS Simulator:"
echo -e "   ${YELLOW}cd \"$SCRIPT_DIR\" && dotnet build -t:Run -c $BUILD_CONFIG -f $TARGET_FRAMEWORK${NC}"
echo ""
echo -e "2. In the app, tap the ${RED}\"Test Crash (iOS dSYM)\"${NC} button"
echo ""
echo "3. Confirm the crash dialog"
echo ""
echo "4. Wait 1-5 minutes for crash to appear in Datadog"
echo ""
echo "5. Check Datadog Error Tracking:"
echo -e "   ${BLUE}https://app.datadoghq.com/rum/error-tracking${NC}"
echo ""
echo -e "6. Verify the crash has a ${GREEN}symbolicated stack trace${NC} with:"
echo "   - File name: DashboardPage.xaml.cs"
echo "   - Method: OnTestCrashClicked"
echo "   - Line numbers visible"
echo ""
echo -e "${GREEN}✅ If symbolication worked, you'll see readable code locations!${NC}"
echo ""
