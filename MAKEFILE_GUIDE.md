# Makefile Quick Reference

## Overview

All project commands are now available through `make`. The Makefile replaces `manage-api.sh` and adds mobile app build commands.

## Quick Start

```bash
# See all available commands
make help

# Build everything
make all

# Start API and run tests
make test

# Check status
make status
```

---

## 🐳 API Commands

### Build & Run

```bash
make api-build        # Build Docker image
make api-start        # Start container
make api-stop         # Stop container
make api-restart      # Restart (stop + start)
```

### Monitoring

```bash
make api-logs         # View logs (follow mode, Ctrl+C to exit)
make api-status       # Check if running
make api-test         # Test all endpoints
```

### Cleanup

```bash
make api-clean        # Remove container and image
```

### Development Workflow

```bash
make api-dev          # Build, start, and show logs (one command)
```

---

## 📱 Mobile App Commands

### Android

```bash
make app-build-android    # Build Android app
make app-run-android      # Build and run on emulator
make app-dev-android      # Clean, restore, and run (full workflow)
```

### iOS

```bash
make app-build-ios        # Build iOS app
make app-run-ios          # Build and run on simulator
make app-dev-ios          # Clean, restore, and run (full workflow)
```

### Maintenance

```bash
make app-clean            # Clean build artifacts
make app-restore          # Restore NuGet packages
```

---

## 🚀 Workflow Commands

### Complete Build

```bash
make all                  # Build API + Android app
```

This runs:
1. `make api-build` - Builds Docker image
2. `make app-build-android` - Builds Android app

### Integration Test

```bash
make test                 # Start API and run tests
```

This runs:
1. `make api-start` - Starts API container
2. Waits 3 seconds
3. `make api-test` - Tests all endpoints

### Full Integration Test

```bash
make integration-test     # Complete integration test
```

This runs:
1. Builds API
2. Starts API
3. Tests all endpoints
4. Shows next steps for mobile testing

### Status Check

```bash
make status               # Show everything
```

Shows:
- API container status
- Android app last build time
- iOS app last build time

### Clean Everything

```bash
make clean                # Clean API + app
```

This runs:
1. `make api-clean` - Removes container and image
2. `make app-clean` - Cleans app build artifacts

---

## 📚 Documentation

```bash
make docs                 # List all documentation files
```

---

## Common Workflows

### 1. First Time Setup

```bash
# Build everything
make all

# Start API
make api-start

# Verify API works
make api-test
```

### 2. Daily Development (API)

```bash
# Make changes to API code...

# Rebuild and restart
make api-build
make api-restart

# Test changes
make api-test

# View logs
make api-logs
```

### 3. Daily Development (Mobile App)

```bash
# Make changes to app code...

# Android
make app-dev-android      # Clean, restore, run

# iOS
make app-dev-ios          # Clean, restore, run
```

### 4. Full Stack Testing

```bash
# Terminal 1: Start API
make api-start
make api-logs             # Keep this running

# Terminal 2: Run mobile app
make app-run-android      # or app-run-ios

# Test in the app, watch logs in Terminal 1
```

### 5. Quick Status Check

```bash
make status               # See what's running and built
```

### 6. Clean Start

```bash
# Clean everything
make clean

# Rebuild from scratch
make all

# Start fresh
make api-start
```

---

## Command Comparison

### Old vs New

| Old Command | New Command | Notes |
|-------------|-------------|-------|
| `./manage-api.sh build` | `make api-build` | Same functionality |
| `./manage-api.sh start` | `make api-start` | Same functionality |
| `./manage-api.sh stop` | `make api-stop` | Same functionality |
| `./manage-api.sh restart` | `make api-restart` | Same functionality |
| `./manage-api.sh logs` | `make api-logs` | Same functionality |
| `./manage-api.sh status` | `make api-status` | Same functionality |
| `./manage-api.sh test` | `make api-test` | Same functionality |
| `./manage-api.sh clean` | `make api-clean` | Same functionality |
| `dotnet build -f net10.0-android` | `make app-build-android` | Shorter! |
| `dotnet build -t:Run -f net10.0-android` | `make app-run-android` | Shorter! |
| `dotnet build -f net10.0-ios` | `make app-build-ios` | Shorter! |
| `dotnet build -t:Run -f net10.0-ios` | `make app-run-ios` | Shorter! |

---

## Tips & Tricks

### Tab Completion

Most shells support tab completion with Make:
```bash
make api-<TAB>        # Shows all api-* commands
make app-<TAB>        # Shows all app-* commands
```

### Running Multiple Commands

```bash
# Sequential
make api-build && make api-start && make api-test

# Using Make's built-in dependency system
make test             # Automatically runs api-start first
```

### Parallel Builds

```bash
# Build API and app simultaneously
make -j2 api-build app-build-android
```

### Quiet Mode

```bash
# Suppress Make's output
make -s api-build     # Only shows command output
```

### Dry Run

```bash
# See what would run without actually running it
make -n api-start
```

---

## Environment Variables

The Makefile uses these variables (can be overridden):

```bash
# Override container name
make CONTAINER_NAME=my-api api-start

# Override port
make PORT=8000 api-start

# Override image name
make IMAGE_NAME=my-custom-api api-build
```

---

## Troubleshooting

### "make: command not found"

Install Make:
```bash
# macOS (usually pre-installed)
xcode-select --install

# Or via Homebrew
brew install make
```

### Permission Denied

Make sure Makefile is readable:
```bash
chmod +r Makefile
```

### Docker Commands Fail

Make sure Docker is running:
```bash
docker ps
```

### App Commands Fail

Make sure .NET SDK is in your PATH:
```bash
dotnet --version    # Should show 10.0.101 or similar
```

---

## Aliases (Optional)

Add to your `~/.zshrc` or `~/.bashrc` for even shorter commands:

```bash
# API shortcuts
alias api='cd /Users/kyle.taylor/server/demo/datadog-maui && make'
alias api-up='make api-start'
alias api-down='make api-stop'
alias api-test='make api-test'
alias api-logs='make api-logs'

# App shortcuts
alias app-android='make app-run-android'
alias app-ios='make app-run-ios'

# Quick status
alias proj-status='make status'
```

Then use:
```bash
api-up              # Instead of: make api-start
api-test            # Instead of: make api-test
app-android         # Instead of: make app-run-android
```

---

## Advanced Usage

### Custom Targets

You can add your own targets to the Makefile:

```makefile
# Add at the end of Makefile
my-custom-workflow:
	@echo "Running custom workflow..."
	@$(MAKE) api-build
	@$(MAKE) api-start
	@$(MAKE) app-build-android
	@echo "✅ Custom workflow complete!"
```

Then run:
```bash
make my-custom-workflow
```

---

## Summary of All Commands

### API
- `make api-build` - Build
- `make api-start` - Start
- `make api-stop` - Stop
- `make api-restart` - Restart
- `make api-logs` - Logs
- `make api-status` - Status
- `make api-test` - Test
- `make api-clean` - Clean
- `make api-dev` - Build + Start + Logs

### Mobile App
- `make app-build-android` - Build Android
- `make app-build-ios` - Build iOS
- `make app-run-android` - Run Android
- `make app-run-ios` - Run iOS
- `make app-clean` - Clean
- `make app-restore` - Restore packages
- `make app-dev-android` - Full Android workflow
- `make app-dev-ios` - Full iOS workflow

### Composite
- `make all` - Build everything
- `make test` - Start API + test
- `make clean` - Clean everything
- `make status` - Show status
- `make integration-test` - Full test
- `make docs` - List documentation
- `make help` - Show help

---

## Quick Reference Card

```
┌─────────────────────────────────────────────────────────┐
│                 Datadog MAUI Makefile                   │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  🐳 API                     📱 Mobile App               │
│  ───────────────            ─────────────               │
│  make api-start             make app-run-android        │
│  make api-stop              make app-run-ios            │
│  make api-logs              make app-build-android      │
│  make api-test              make app-build-ios          │
│  make api-status            make app-clean              │
│                                                         │
│  🚀 Workflows               📊 Info                     │
│  ─────────────              ────────                    │
│  make all                   make status                 │
│  make test                  make help                   │
│  make clean                 make docs                   │
│  make integration-test                                  │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

**Pro Tip**: Run `make help` anytime to see all available commands!
