#!/bin/bash

#
# Git Secret Scrubber
#
# This script removes sensitive data from your Git history using git-filter-repo.
# It's designed to scrub Datadog tokens and other secrets before publishing.
#
# WARNING: This rewrites Git history. Make a backup first!
#

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${RED}⚠️  Git Secret Scrubber${NC}"
echo "====================="
echo ""
echo -e "${YELLOW}This script will REWRITE your Git history.${NC}"
echo -e "${RED}This cannot be undone without a backup!${NC}"
echo ""

# Check if we're in a git repo
if ! git rev-parse --git-dir > /dev/null 2>&1; then
    echo -e "${RED}❌ Not a git repository${NC}"
    exit 1
fi

# Check for uncommitted changes
if ! git diff-index --quiet HEAD --; then
    echo -e "${RED}❌ You have uncommitted changes. Commit or stash them first.${NC}"
    exit 1
fi

# Find tokens dynamically
TOKENS_FILE=$(mktemp)
APP_IDS_FILE=$(mktemp)

# Find client tokens (pub + 32 hex chars)
git log --all --full-history -p | grep -oE "pub[a-f0-9]{32}" | sort -u > "$TOKENS_FILE"

# Find application IDs (UUID format, excluding common test UUIDs)
git log --all --full-history -p | grep -oE "[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}" | \
    grep -v "00000000-0000-0000-0000-000000000000" | \
    grep -v "12345678-1234-1234-1234-123456789012" | \
    sort -u > "$APP_IDS_FILE"

# Confirm with user
TOKEN_COUNT=$(wc -l < "$TOKENS_FILE" | tr -d ' ')
APP_ID_COUNT=$(wc -l < "$APP_IDS_FILE" | tr -d ' ')
TOTAL_COUNT=$((TOKEN_COUNT + APP_ID_COUNT))

echo -e "${YELLOW}Found secrets to remove:${NC}"
echo ""
echo -e "${YELLOW}Client Tokens: $TOKEN_COUNT${NC}"
if [ "$TOKEN_COUNT" -gt 0 ]; then
    cat "$TOKENS_FILE" | while read token; do
        echo "  - $token"
    done
fi
echo ""
echo -e "${YELLOW}Application IDs: $APP_ID_COUNT${NC}"
if [ "$APP_ID_COUNT" -gt 0 ]; then
    cat "$APP_IDS_FILE" | while read appid; do
        echo "  - $appid"
    done
fi
echo ""
echo -e "${YELLOW}Before proceeding:${NC}"
echo "  1. ✅ Create a backup: git clone . ../datadog-maui-backup"
echo "  2. ✅ Rotate these tokens in Datadog"
echo "  3. ✅ Ensure .env is in .gitignore"
echo ""

read -p "Have you completed the above steps? (yes/no): " -r
echo ""
if [[ ! $REPLY =~ ^[Yy]es$ ]]; then
    echo -e "${YELLOW}Aborting. Complete the steps above and run again.${NC}"
    exit 0
fi

# Check if git-filter-repo is installed
if ! command -v git-filter-repo &> /dev/null; then
    echo -e "${YELLOW}📦 Installing git-filter-repo...${NC}"
    echo ""

    # Try Homebrew first (easiest on macOS)
    if command -v brew &> /dev/null; then
        brew install git-filter-repo
    # Try uv (modern Python package manager)
    elif command -v uv &> /dev/null; then
        uv tool install git-filter-repo
    # Try pipx (isolated Python tool installation)
    elif command -v pipx &> /dev/null; then
        pipx install git-filter-repo
    else
        echo -e "${RED}❌ Cannot install git-filter-repo automatically.${NC}"
        echo ""
        echo "Please install it manually:"
        echo "  brew install git-filter-repo"
        echo "  OR"
        echo "  uv tool install git-filter-repo"
        echo "  OR"
        echo "  pipx install git-filter-repo"
        echo ""
        exit 1
    fi
fi

echo -e "${GREEN}✅ git-filter-repo is available${NC}"
echo ""

# Create expressions file for replacement
EXPRESSIONS_FILE=$(mktemp)

# Build replacement expressions dynamically from found tokens and app IDs
{
    echo "***REMOVED***"

    # Replace client tokens
    counter=1
    cat "$TOKENS_FILE" | while read token; do
        if [ ! -z "$token" ]; then
            echo "${token}==>REDACTED_CLIENT_TOKEN_${counter}"
            # Uppercase variant (portable way)
            upper_token=$(echo "$token" | tr '[:lower:]' '[:upper:]')
            echo "${upper_token}==>REDACTED_CLIENT_TOKEN_${counter}"
            counter=$((counter + 1))
        fi
    done

    # Replace application IDs
    app_counter=1
    cat "$APP_IDS_FILE" | while read appid; do
        if [ ! -z "$appid" ]; then
            echo "${appid}==>REDACTED_APP_ID_${app_counter}"
            # Uppercase variant
            upper_appid=$(echo "$appid" | tr '[:lower:]' '[:upper:]')
            echo "${upper_appid}==>REDACTED_APP_ID_${app_counter}"
            app_counter=$((app_counter + 1))
        fi
    done
} > "$EXPRESSIONS_FILE"

# Cleanup temp files
rm -f "$TOKENS_FILE"
rm -f "$APP_IDS_FILE"

echo -e "${YELLOW}🔧 Scrubbing secrets from Git history...${NC}"
echo ""
echo "This may take a few minutes depending on repository size..."
echo ""

# Run git-filter-repo to replace secrets
git-filter-repo \
    --replace-text "$EXPRESSIONS_FILE" \
    --force

# Cleanup
rm -f "$EXPRESSIONS_FILE"

echo ""
echo -e "${GREEN}✅ Secrets have been scrubbed from Git history!${NC}"
echo ""
echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${GREEN}Next Steps:${NC}"
echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""
echo "1. Verify secrets are removed:"
echo -e "   ${BLUE}./scan-secrets.sh${NC}"
echo ""
echo "2. Test that your app still works with new tokens in .env"
echo ""
echo "3. If you have a remote repository, update it:"
echo -e "   ${RED}git push --force --all origin${NC}"
echo -e "   ${RED}git push --force --tags origin${NC}"
echo ""
echo -e "${RED}⚠️  IMPORTANT: All collaborators must re-clone the repository!${NC}"
echo "   Their old clones will have the unmodified history with secrets."
echo ""
echo "4. If this is a GitHub/GitLab repo, consider these additional steps:"
echo "   - Delete the old repository"
echo "   - Create a new repository"
echo "   - Push the cleaned history"
echo "   (This ensures the old history with secrets is truly gone)"
echo ""
echo -e "${YELLOW}5. Verify on GitHub/GitLab:${NC}"
echo "   - Search for the old tokens in the web interface"
echo "   - Check that they no longer appear"
echo ""
