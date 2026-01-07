#!/bin/bash

#
# Git Secret Scanner
#
# This script scans your Git history for sensitive data that should be removed
# before publishing the repository publicly.
#

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}🔍 Git Secret Scanner${NC}"
echo "===================="
echo ""

# Check if we're in a git repo
if ! git rev-parse --git-dir > /dev/null 2>&1; then
    echo -e "${RED}❌ Not a git repository${NC}"
    exit 1
fi

echo -e "${YELLOW}Scanning Git history for sensitive data...${NC}"
echo ""

# Temporary file for results
RESULTS_FILE=$(mktemp)

# Patterns to search for
declare -a PATTERNS=(
    # Datadog tokens (32 hex chars)
    "pub[a-f0-9]{32}"
    "app[a-f0-9]{32}"
    # UUIDs (RUM Application IDs)
    "[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}"
    # Generic API keys
    "api[_-]?key[\"']?\s*[:=]\s*[\"']?[a-zA-Z0-9]{20,}"
    "token[\"']?\s*[:=]\s*[\"']?[a-zA-Z0-9]{20,}"
    # AWS keys
    "AKIA[0-9A-Z]{16}"
    # Generic secrets
    "secret[\"']?\s*[:=]\s*[\"']?[a-zA-Z0-9]{20,}"
    "password[\"']?\s*[:=]\s*[\"']?[a-zA-Z0-9]{20,}"
)

# Search for each pattern
for pattern in "${PATTERNS[@]}"; do
    echo -e "${BLUE}Searching for pattern: ${pattern}${NC}"

    # Search all commits for this pattern
    git log --all --full-history -p | grep -E "$pattern" | grep -v "your_" | grep -v "example" | grep -v "placeholder" >> "$RESULTS_FILE" 2>/dev/null || true
done

echo ""
echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${RED}⚠️  SECRETS FOUND IN GIT HISTORY${NC}"
echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""

# Extract and display unique tokens
echo -e "${RED}🔑 Datadog Client Tokens (pub*):${NC}"
git log --all --full-history -p | grep -oE "pub[a-f0-9]{32}" | sort -u | while read token; do
    echo "  - $token"
done
echo ""

echo -e "${RED}📱 Datadog Application IDs (app*):${NC}"
git log --all --full-history -p | grep -oE "app[a-f0-9]{32}" | sort -u | while read token; do
    echo "  - $token"
done
echo ""

echo -e "${RED}🆔 Application IDs (UUID format):${NC}"
git log --all --full-history -p | grep -oE "[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}" | grep -v "00000000" | sort -u | head -10 | while read token; do
    echo "  - $token"
done
echo ""

# Find which commits contain these secrets
echo -e "${YELLOW}📝 Commits containing secrets:${NC}"
git log --all --full-history --oneline -p -S "REDACTED_CLIENT_TOKEN_1" | grep "^[a-f0-9]" | head -5
git log --all --full-history --oneline -p -S "REDACTED_CLIENT_TOKEN_2" | grep "^[a-f0-9]" | head -5
echo ""

# Cleanup
rm -f "$RESULTS_FILE"

echo ""
echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${RED}⚠️  ACTION REQUIRED${NC}"
echo -e "${YELLOW}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""
echo "These secrets are in your Git history and will be visible if you push to a public repository."
echo ""
echo "Next steps:"
echo "  1. Rotate these credentials in Datadog (generate new tokens)"
echo "  2. Run the scrubbing script: ./scrub-secrets.sh"
echo "  3. Verify secrets are removed: ./scan-secrets.sh"
echo "  4. Force push to update remote: git push --force --all"
echo ""
echo -e "${RED}⚠️  WARNING: Force pushing rewrites history and affects all collaborators${NC}"
echo ""
