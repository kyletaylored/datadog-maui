# Git Setup Guide

## .gitignore Created ✅

A comprehensive `.gitignore` file has been created for this project.

---

## What's Ignored

### Build Artifacts
- `bin/` and `obj/` directories
- Debug/Release builds
- Compiled binaries (`.dll`, `.exe`)
- NuGet packages directory

### IDE & Editor Files
- `.vs/` - Visual Studio
- `.vscode/` - VS Code
- `.idea/` - JetBrider
- `*.suo`, `*.user` - User settings

### Platform-Specific
- Android: `.gradle/`, `*.apk`, `*.aab`
- iOS: `xcuserdata/`, `*.ipa`, `Pods/`
- macOS: `.DS_Store`

### Temporary Files
- `*.tmp`, `*.bak`, `*.swp`
- MSBuild temp files
- SDK resolver backup files (`.csproj.SdkResolver.*.proj.Backup.tmp`)

### Sensitive Data
- `appsettings.local.json`
- `.env` files
- Certificate files (`.pfx`, `.key`, `.pem`)
- `google-services.json`

---

## What's Tracked

### Source Code ✅
- All `.cs` files
- All `.xaml` files
- `.csproj` project files

### Configuration ✅
- `appsettings.json` (non-sensitive)
- `Dockerfile`
- `docker-compose.yml`
- `Makefile`

### Documentation ✅
- All `.md` files
- `README.md`, guides, etc.

### Resources ✅
- SVG icons and splash screens
- XAML styles

### Scripts ✅
- `manage-api.sh`
- `Makefile`

---

## Initialize Git Repository

The repository has already been initialized. To set it up properly:

```bash
# Already done:
git init

# Add all files
git add .

# Commit
git commit -m "Initial commit: Datadog MAUI app with containerized API"
```

---

## Verify What's Being Tracked

```bash
# See what will be committed
git status

# See what's ignored
git status --ignored

# Dry run to see what would be added
git add -n .
```

---

## Recommended First Commit

```bash
git add .
git commit -m "Initial commit: Cross-platform MAUI app with Docker API

- .NET MAUI mobile app (Android/iOS)
- ASP.NET Core Web API with 4 endpoints
- Docker containerization
- Makefile for all operations
- Comprehensive documentation
- Build tested and working"
```

---

## Common Git Workflows

### After Making Changes

```bash
# See what changed
git status
git diff

# Add and commit
git add .
git commit -m "Description of changes"
```

### Before Making Experimental Changes

```bash
# Create a branch
git checkout -b feature/new-feature

# Make changes...

# Commit
git add .
git commit -m "Add new feature"

# Switch back to main
git checkout main

# Merge if successful
git merge feature/new-feature
```

### View History

```bash
git log --oneline --graph
git log --stat
```

---

## Remote Repository Setup (Optional)

If you want to push to GitHub/GitLab/etc:

```bash
# Add remote
git remote add origin <your-repo-url>

# Push to remote
git branch -M main
git push -u origin main
```

### GitHub Example

```bash
# Create repo on github.com, then:
git remote add origin https://github.com/yourusername/datadog-maui.git
git branch -M main
git push -u origin main
```

---

## Checking Ignored Files

```bash
# List all ignored files
git status --ignored

# Check if specific file is ignored
git check-ignore -v MauiApp/bin/Debug/file.dll

# See gitignore rules
cat .gitignore
```

---

## What's Currently Ignored (Examples)

The `.gitignore` is properly ignoring:

```
!! .claude/                     # Claude settings
!! Api/obj/                     # Build artifacts
!! MauiApp/bin/                 # Compiled output
!! MauiApp/obj/                 # Build artifacts
!! *.SdkResolver.*.tmp          # Temporary SDK files
```

All source code, documentation, and configuration files are tracked. ✅

---

## Important Notes

### Don't Commit These (Already in .gitignore)

❌ **Build artifacts** - `bin/`, `obj/`
❌ **User settings** - `.vs/`, `*.user`
❌ **Secrets** - `.env`, `appsettings.local.json`
❌ **Packages** - `packages/`, `node_modules/`
❌ **Binaries** - `*.dll`, `*.exe`, `*.apk`

### Do Commit These (Tracked)

✅ **Source code** - `.cs`, `.xaml`
✅ **Project files** - `.csproj`
✅ **Configuration** - `Dockerfile`, `Makefile`
✅ **Documentation** - `*.md`
✅ **Resources** - SVG files, styles

---

## Troubleshooting

### Accidentally Committed bin/ or obj/

```bash
# Remove from git but keep locally
git rm -r --cached MauiApp/bin/
git rm -r --cached MauiApp/obj/

# Commit the removal
git commit -m "Remove build artifacts from git"
```

### File Not Being Ignored

```bash
# Check gitignore rules for file
git check-ignore -v path/to/file

# Force remove from git
git rm --cached path/to/file
git commit -m "Remove tracked file"
```

### Need to Ignore Additional Files

Edit `.gitignore` and add patterns:

```bash
# Edit .gitignore
nano .gitignore

# Add patterns like:
*.log
temp/
local-config.json

# Commit changes
git add .gitignore
git commit -m "Update gitignore"
```

---

## Best Practices

### Commit Messages

```bash
# Good commit messages
git commit -m "Add user authentication feature"
git commit -m "Fix API endpoint validation bug"
git commit -m "Update documentation for Makefile"

# Include details for complex changes
git commit -m "Refactor ApiService for better error handling

- Add retry logic with exponential backoff
- Improve error messages for network failures
- Add comprehensive logging"
```

### Commit Frequency

- Commit after completing a logical unit of work
- Commit before trying something experimental
- Commit working code (builds successfully)

### Branch Strategy

```bash
main          # Stable, working code
develop       # Integration branch
feature/*     # New features
bugfix/*      # Bug fixes
hotfix/*      # Urgent fixes
```

---

## Summary

✅ **`.gitignore` created** - Comprehensive rules for .NET MAUI
✅ **Source tracked** - All code and configuration
✅ **Artifacts ignored** - bin/, obj/, build files
✅ **Secrets excluded** - .env, sensitive configs
✅ **Ready to commit** - `git add .` when ready

---

## Quick Commands

```bash
git status                # What's changed
git status --ignored      # What's ignored
git add .                 # Stage all changes
git commit -m "message"   # Commit
git log --oneline         # View history
git diff                  # See changes
```

---

**Your repository is properly configured and ready for version control!** 🎉
