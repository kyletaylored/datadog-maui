# Documentation Reorganization Plan

## Current Issues

1. **Duplicate Azure content**: Root `AZURE_DEPLOYMENT.md` vs `deployment/` folder
2. **Mixed mobile docs**: iOS-specific in `ios/`, generic in `guides/`
3. **Backend docs scattered**: IIS, .NET comparison, Windows testing in root
4. **Authentication guide**: Overlaps with main docs, not platform-specific
5. **Reference folder**: Contains status docs, not reference material

## Proposed Structure

```
docs/
├── README.md                           # Index (keep, update)
│
├── backend/                            # NEW: Backend API documentation
│   ├── README.md                       # Backend docs index
│   ├── DOTNET_COMPARISON.md           # Move from root
│   ├── AUTHENTICATION.md              # Move/merge AUTHENTICATION_AND_TRACING
│   ├── IIS_DEPLOYMENT.md              # Move from root
│   ├── IIS_TROUBLESHOOTING.md         # Move from root
│   └── WINDOWS_SERVER_TESTING.md      # Move from root
│
├── mobile/                             # NEW: Mobile app documentation
│   ├── README.md                       # Mobile docs index
│   ├── ANDROID.md                      # NEW: Android-specific guide
│   ├── IOS.md                          # NEW: iOS overview/quickstart
│   ├── BUILD_CONFIGURATION.md         # Move from guides/
│   ├── DEBUGGING.md                    # Move from guides/
│   ├── RUM_CONFIGURATION.md           # Move from guides/
│   └── ios/                            # iOS-specific (keep)
│       ├── BUILD_CONFIGURATION.md
│       ├── CRASH_REPORTING.md
│       ├── SDK_VERSION_FIX.md
│       └── scripts/
│
├── deployment/                         # Keep, consolidate
│   ├── README.md                       # NEW: Deployment index
│   ├── AZURE.md                        # MERGE: AZURE_DEPLOYMENT + AZURE_QUICK_START
│   ├── AZURE_FUNCTIONS.md             # RENAME from AZURE_FUNCTIONS_MIGRATION
│   ├── DOCKERFILE_COMPARISON.md       # Keep
│   └── IIS.md                          # Link to ../backend/IIS_DEPLOYMENT.md
│
├── datadog/                            # NEW: Datadog configuration
│   ├── README.md                       # Datadog docs index
│   ├── AGENT_SETUP.md                 # Move from setup/
│   ├── RUM.md                          # Platform-agnostic RUM guide
│   ├── TRACE_LOG_CORRELATION.md       # Move from guides/
│   └── GIT_METADATA.md                # Move from guides/
│
├── guides/                             # Keep, slimmed down
│   └── BUILD.md                        # Keep (Docker build with metadata)
│
├── setup/                              # REMOVE: Merge into relevant sections
│   ├── DATADOG_AGENT_SETUP.md         # → datadog/AGENT_SETUP.md
│   └── SETUP_COMPLETE.md              # → DELETE (outdated status)
│
├── reference/                          # REMOVE: Status docs, not reference
│   ├── CORRELATION_SUCCESS.md         # → DELETE or archive
│   └── GIT_METADATA_COMPLETE.md       # → DELETE or archive
│
└── archive/                            # Keep as-is
    └── (historical docs)
```

## Actions Required

### 1. CREATE New Directories
```bash
mkdir -p docs/backend
mkdir -p docs/mobile
mkdir -p docs/mobile/ios  # Move existing ios/ content here
mkdir -p docs/datadog
```

### 2. MOVE Files

**To docs/backend/:**
- `docs/DOTNET_COMPARISON.md` → `docs/backend/DOTNET_COMPARISON.md`
- `docs/IIS_DEPLOYMENT.md` → `docs/backend/IIS_DEPLOYMENT.md`
- `docs/IIS_TROUBLESHOOTING.md` → `docs/backend/IIS_TROUBLESHOOTING.md`
- `docs/WINDOWS_SERVER_TESTING.md` → `docs/backend/WINDOWS_SERVER_TESTING.md`

**To docs/mobile/:**
- `docs/guides/MOBILE_BUILD_CONFIGURATION.md` → `docs/mobile/BUILD_CONFIGURATION.md`
- `docs/guides/MOBILE_DEBUGGING.md` → `docs/mobile/DEBUGGING.md`
- `docs/guides/RUM_CONFIGURATION.md` → `docs/mobile/RUM_CONFIGURATION.md`
- `docs/ios/*` → `docs/mobile/ios/*`

**To docs/datadog/:**
- `docs/setup/DATADOG_AGENT_SETUP.md` → `docs/datadog/AGENT_SETUP.md`
- `docs/guides/TRACE_LOG_CORRELATION.md` → `docs/datadog/TRACE_LOG_CORRELATION.md`
- `docs/guides/GIT_METADATA_INTEGRATION.md` → `docs/datadog/GIT_METADATA.md`

### 3. MERGE/CONSOLIDATE

**docs/deployment/AZURE.md** (merge these):
- `docs/AZURE_DEPLOYMENT.md` (App Service focus)
- `docs/deployment/AZURE_QUICK_START.md` (decision guide)
→ Single comprehensive Azure guide

**docs/backend/AUTHENTICATION.md** (simplify):
- Extract platform-agnostic auth info from `docs/AUTHENTICATION_AND_TRACING.md`
- Remove redundant content already in code or main README

**docs/datadog/RUM.md** (new):
- Platform-agnostic RUM concepts
- Link to platform-specific: `../mobile/RUM_CONFIGURATION.md`

### 4. DELETE/ARCHIVE

**Delete (outdated status docs):**
- `docs/setup/SETUP_COMPLETE.md`
- `docs/reference/CORRELATION_SUCCESS.md`
- `docs/reference/GIT_METADATA_COMPLETE.md`

**Consider archiving:**
- `docs/AUTHENTICATION_AND_TRACING.md` (if still useful for reference)

### 5. CREATE Index Files

**docs/backend/README.md**: Backend API documentation index
**docs/mobile/README.md**: Mobile app documentation index
**docs/datadog/README.md**: Datadog configuration index
**docs/deployment/README.md**: Update deployment index

### 6. UPDATE Root Index

Update `docs/README.md` to reflect new structure:
- Clear sections: Backend, Mobile, Deployment, Datadog
- Remove setup/ and reference/ sections
- Update all links

### 7. UPDATE Root README

Update `/README.md` links to match new structure

## Benefits

1. **Clear Organization**: Backend vs Mobile vs Deployment vs Datadog
2. **Less Duplication**: Merged overlapping guides
3. **Easier Navigation**: Logical grouping by concern
4. **Less Clutter**: Removed outdated status docs
5. **Platform-Specific**: iOS docs clearly nested under mobile
6. **Discoverable**: READMEs in each section for navigation

## Migration Commands

```bash
# Create new directories
mkdir -p docs/{backend,mobile/ios,datadog}

# Move backend docs
mv docs/DOTNET_COMPARISON.md docs/backend/
mv docs/IIS_DEPLOYMENT.md docs/backend/
mv docs/IIS_TROUBLESHOOTING.md docs/backend/
mv docs/WINDOWS_SERVER_TESTING.md docs/backend/

# Move mobile docs
mv docs/guides/MOBILE_BUILD_CONFIGURATION.md docs/mobile/BUILD_CONFIGURATION.md
mv docs/guides/MOBILE_DEBUGGING.md docs/mobile/DEBUGGING.md
mv docs/guides/RUM_CONFIGURATION.md docs/mobile/RUM_CONFIGURATION.md
mv docs/ios/* docs/mobile/ios/
rmdir docs/ios

# Move datadog docs
mv docs/setup/DATADOG_AGENT_SETUP.md docs/datadog/AGENT_SETUP.md
mv docs/guides/TRACE_LOG_CORRELATION.md docs/datadog/TRACE_LOG_CORRELATION.md
mv docs/guides/GIT_METADATA_INTEGRATION.md docs/datadog/GIT_METADATA.md

# Remove empty/obsolete directories
rmdir docs/setup
rmdir docs/reference

# Delete outdated files
rm docs/AUTHENTICATION_AND_TRACING.md
rm docs/AZURE_DEPLOYMENT.md  # After merging
```
