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

# Confirm with user
echo -e "${YELLOW}Found secrets to remove:${NC}"
echo ""
echo "  - REDACTED_CLIENT_TOKEN_1 (Android Client Token)"
echo "  - REDACTED_CLIENT_TOKEN_2 (iOS Client Token)"
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

    # Try to install via pip
    if command -v pip3 &> /dev/null; then
        pip3 install git-filter-repo
    elif command -v pip &> /dev/null; then
        pip install git-filter-repo
    elif command -v brew &> /dev/null; then
        brew install git-filter-repo
    else
        echo -e "${RED}❌ Cannot install git-filter-repo automatically.${NC}"
        echo ""
        echo "Please install it manually:"
        echo "  pip install git-filter-repo"
        echo "  OR"
        echo "  brew install git-filter-repo"
        echo ""
        exit 1
    fi
fi

echo -e "${GREEN}✅ git-filter-repo is available${NC}"
echo ""

# Create expressions file for replacement
EXPRESSIONS_FILE=$(mktemp)

# Add all secrets to replace (case-insensitive)
cat > "$EXPRESSIONS_FILE" << 'EOF'
# Datadog Client Tokens
REDACTED_CLIENT_TOKEN_1==>REDACTED_ANDROID_CLIENT_TOKEN
REDACTED_CLIENT_TOKEN_2==>REDACTED_IOS_CLIENT_TOKEN

# Case variations (if any)
REDACTED_CLIENT_TOKEN_1==>REDACTED_ANDROID_CLIENT_TOKEN
REDACTED_CLIENT_TOKEN_2==>REDACTED_IOS_CLIENT_TOKEN
EOF

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
