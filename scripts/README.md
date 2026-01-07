# Scripts Directory

This directory contains utility scripts for the Datadog MAUI project.

## Available Scripts

### Development Scripts

#### [manage-api.sh](manage-api.sh)
Legacy API management script (replaced by Makefile).

**Note**: Use `make` commands instead for better functionality:
- `make api-build` - Build API
- `make api-start` - Start API
- `make api-stop` - Stop API
- `make api-logs` - View logs
- `make api-test` - Test endpoints

#### [set-git-metadata.sh](set-git-metadata.sh)
Extracts Git metadata and exports environment variables for Docker builds.

**Usage:**
```bash
source ./scripts/set-git-metadata.sh
make api-build
```

**Exported Variables:**
- `DD_GIT_REPOSITORY_URL` - Git remote URL
- `DD_GIT_COMMIT_SHA` - Current commit SHA
- `DD_GIT_BRANCH` - Current branch name
- `DD_GIT_TAG` - Current tag (if on a tag)
- `DD_GIT_COMMIT_MESSAGE` - Last commit message
- `DD_GIT_COMMIT_AUTHOR_NAME` - Commit author
- `DD_GIT_COMMIT_AUTHOR_EMAIL` - Author email
- `DD_GIT_COMMIT_COMMITTER_NAME` - Committer name
- `DD_GIT_COMMIT_COMMITTER_EMAIL` - Committer email
- `DD_GIT_COMMIT_AUTHOR_DATE` - Author timestamp (ISO 8601)
- `DD_GIT_COMMIT_COMMITTER_DATE` - Committer timestamp (ISO 8601)

**Called by:** Makefile's `api-build` target

#### [set-mobile-env.sh](set-mobile-env.sh)
Sources `.env` file and exports Datadog configuration for mobile app builds.

**Usage:**
```bash
source ./scripts/set-mobile-env.sh
cd MauiApp && dotnet build -f net10.0-android
```

**Exports:**
- `DD_RUM_ANDROID_CLIENT_TOKEN`
- `DD_RUM_ANDROID_APPLICATION_ID`
- `DD_RUM_IOS_CLIENT_TOKEN`
- `DD_RUM_IOS_APPLICATION_ID`

**Called by:** Makefile's `app-build-*` and `app-run-*` targets

### Security Scripts

#### [scan-secrets.sh](scan-secrets.sh)
Scans Git history for sensitive data (API keys, tokens, application IDs).

**Usage:**
```bash
./scripts/scan-secrets.sh
```

**Searches for:**
- Datadog client tokens (`pub[a-f0-9]{32}`)
- Datadog application IDs (`app[a-f0-9]{32}`)
- UUIDs (RUM Application IDs)
- Generic API keys, secrets, passwords

**Output:** Lists all found secrets and affected commits

#### [scrub-secrets.sh](scrub-secrets.sh)
Removes sensitive data from Git history using `git-filter-repo`.

**⚠️ WARNING:** This rewrites Git history and cannot be undone without a backup!

**Usage:**
```bash
# 1. Create backup first
git clone . ../datadog-maui-backup

# 2. Rotate credentials in Datadog

# 3. Run scrubber
./scripts/scrub-secrets.sh

# 4. Verify
./scripts/scan-secrets.sh
```

**Requirements:**
- `git-filter-repo` (installed via brew, uv, or pipx)

**What it does:**
1. Finds all secrets dynamically from Git history
2. Creates replacement expressions
3. Rewrites history to replace secrets with `REDACTED_*`
4. Cleans up Git references

**After running:**
- Force push: `git push --force --all origin`
- All collaborators must re-clone
- Consider deleting and recreating the repository

## Mobile App Scripts

Located in [../MauiApp/](../MauiApp/):

#### [test-crash-reporting.sh](../MauiApp/test-crash-reporting.sh)
Automated iOS crash reporting and dSYM upload workflow.

See [MauiApp/CRASH_TESTING_GUIDE.md](../MauiApp/CRASH_TESTING_GUIDE.md) for details.

## iOS-Specific Scripts

Located in [../docs/ios/scripts/](../docs/ios/scripts/):

#### [upload-dsyms.sh](../docs/ios/scripts/upload-dsyms.sh)
Manual dSYM upload to Datadog for iOS crash symbolication.

See [docs/ios/CRASH_REPORTING.md](../docs/ios/CRASH_REPORTING.md) for details.

## Usage Patterns

### Standard Workflow

```bash
# 1. Set Git metadata and build API
source ./scripts/set-git-metadata.sh
make api-build
make api-start

# 2. Build and run mobile app
source ./scripts/set-mobile-env.sh
make app-run-android
```

### Security Cleanup Before Publishing

```bash
# 1. Scan for secrets
./scripts/scan-secrets.sh

# 2. Create backup
git clone . ../datadog-maui-backup

# 3. Rotate credentials in Datadog

# 4. Scrub secrets
./scripts/scrub-secrets.sh

# 5. Verify
./scripts/scan-secrets.sh

# 6. Force push or create new repo
git push --force --all origin
```

## Script Permissions

All scripts should be executable:
```bash
chmod +x scripts/*.sh
```

## See Also

- [Makefile](../Makefile) - Primary build automation
- [README.md](../README.md) - Project overview
- [QUICKSTART.md](../QUICKSTART.md) - Quick start guide
- [docs/](../docs/) - Detailed documentation
