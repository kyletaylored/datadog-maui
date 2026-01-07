.PHONY: help api-build api-start api-stop api-restart api-logs api-status api-test api-clean \
        app-clean app-restore app-build-android app-build-ios app-run-android app-run-ios \
        docker-build docker-start docker-stop docker-restart docker-logs docker-clean \
        datadog-build-android datadog-build-ios datadog-build-all \
        agent-logs logs-all api-build-simple status integration-test docs \
        test clean all

# Variables
CONTAINER_NAME = datadog-maui-api
IMAGE_NAME = datadog-maui-api
PORT = 5000
MAUI_PROJECT = MauiApp/DatadogMauiApp.csproj
API_PROJECT = Api/DatadogMauiApi.csproj
DATADOG_BINDINGS = datadog-dotnet-mobile-sdk-bindings

# Default target
help:
	@echo "Datadog MAUI Project - Available Commands"
	@echo ""
	@echo "API Commands (Docker + Datadog):"
	@echo "  make api-build       Build Docker images"
	@echo "  make api-start       Start API and Datadog Agent containers"
	@echo "  make api-stop        Stop all containers"
	@echo "  make api-restart     Restart all containers"
	@echo "  make api-logs        Show API logs (follow mode)"
	@echo "  make agent-logs      Show Datadog Agent logs"
	@echo "  make logs-all        Show all logs"
	@echo "  make api-status      Show container and agent status"
	@echo "  make api-test        Test all API endpoints"
	@echo "  make api-clean       Remove containers, images, and volumes"
	@echo ""
	@echo "Mobile App Commands:"
	@echo "  make app-clean       Clean app build artifacts"
	@echo "  make app-restore     Restore NuGet packages"
	@echo "  make app-build-android   Build Android app"
	@echo "  make app-build-ios       Build iOS app"
	@echo "  make app-run-android     Build and run on Android emulator"
	@echo "  make app-run-ios         Build and run on iOS simulator"
	@echo "  make app-logs-android    View Android logs (filtered for Datadog)"
	@echo "  make app-logs-android-all View all Android logs"
	@echo "  make app-logs-clear      Clear Android logs"
	@echo ""
	@echo "Datadog Commands:"
	@echo "  make datadog-build-android   Build Datadog Android bindings"
	@echo "  make datadog-build-ios       Build Datadog iOS bindings"
	@echo "  make datadog-build-all       Build all Datadog bindings"
	@echo ""
	@echo "Quick Commands:"
	@echo "  make all             Build API and Android app"
	@echo "  make test            Start API and run tests"
	@echo "  make clean           Clean everything"
	@echo ""
	@echo "Container URLs:"
	@echo "  Local:    http://localhost:$(PORT)"
	@echo "  Android:  http://10.0.2.2:$(PORT)"
	@echo "  iOS:      http://localhost:$(PORT)"

# =============================================================================
# API / Docker Commands
# =============================================================================

api-build: docker-build
docker-build:
	@echo "🔨 Building Docker images..."
	@bash ./scripts/set-git-metadata.sh > /dev/null 2>&1 || true
	COMPOSE_BAKE=true docker-compose build
	@echo "✅ Images built successfully"

api-build-simple:
	@echo "🔨 Building API Docker image (simple)..."
	cd Api && docker build --load -t $(IMAGE_NAME) .
	@echo "✅ Image built successfully: $(IMAGE_NAME)"

api-start: docker-start
docker-start:
	@echo "🚀 Starting containers with docker-compose..."
	docker-compose up -d
	@echo "✅ All services started"
	@echo "   API: http://localhost:$(PORT)"
	@echo "   Android: http://10.0.2.2:$(PORT)"
	@echo "   iOS: http://localhost:$(PORT)"
	@echo "   Datadog Agent: http://localhost:8126"

api-stop: docker-stop
docker-stop:
	@echo "🛑 Stopping containers with docker-compose..."
	docker-compose down
	@echo "✅ All containers stopped"

api-restart: docker-restart
docker-restart: api-stop
	@sleep 1
	@$(MAKE) api-start

api-logs: docker-logs
docker-logs:
	@echo "📋 Showing API logs (Ctrl+C to exit)..."
	docker-compose logs -f api

agent-logs:
	@echo "📋 Showing Datadog Agent logs (Ctrl+C to exit)..."
	docker-compose logs -f datadog-agent

logs-all:
	@echo "📋 Showing all logs (Ctrl+C to exit)..."
	docker-compose logs -f

api-status: docker-status
docker-status:
	@echo "📊 Container Status:"
	@docker-compose ps
	@echo ""
	@echo "📈 Datadog Agent Status:"
	@docker exec datadog-agent agent status 2>/dev/null | head -20 || echo "❌ Agent not running"

api-test:
	@echo "🧪 Testing API endpoints..."
	@echo ""
	@echo "1️⃣  Health Check:"
	@curl -s http://localhost:$(PORT)/health | jq || echo "❌ Failed"
	@echo ""
	@echo "2️⃣  Config:"
	@curl -s http://localhost:$(PORT)/config | jq || echo "❌ Failed"
	@echo ""
	@echo "3️⃣  Submit Test Data:"
	@curl -s -X POST http://localhost:$(PORT)/data \
		-H "Content-Type: application/json" \
		-d '{"correlationId":"test-'$$(date +%s)'","sessionName":"Test Session","notes":"Automated test","numericValue":42.5}' | jq || echo "❌ Failed"
	@echo ""
	@echo "4️⃣  Get All Data:"
	@curl -s http://localhost:$(PORT)/data | jq || echo "❌ Failed"
	@echo ""

api-clean: docker-clean
docker-clean:
	@echo "🧹 Cleaning up containers and images..."
	docker-compose down --rmi local --volumes
	@echo "✅ Cleanup complete"

# =============================================================================
# Mobile App Commands
# =============================================================================

app-clean:
	@echo "🧹 Cleaning MAUI app..."
	cd MauiApp && dotnet clean
	@echo "✅ Clean complete"

app-restore:
	@echo "📦 Restoring NuGet packages..."
	cd MauiApp && dotnet restore
	@echo "✅ Restore complete"

app-build-android:
	@echo "🔨 Building Android app..."
	@bash -c 'source ./scripts/set-mobile-env.sh > /dev/null 2>&1 && cd MauiApp && /usr/local/share/dotnet/dotnet build -f net10.0-android'
	@echo "✅ Android build complete"

app-build-ios:
	@echo "🔨 Building iOS app..."
	@bash -c 'source ./scripts/set-mobile-env.sh > /dev/null 2>&1 && cd MauiApp && dotnet build -f net10.0-ios'
	@echo "✅ iOS build complete"

app-run-android:
	@echo "🚀 Building and running Android app..."
	@echo "   Make sure Android emulator is running!"
	@bash -c 'source ./scripts/set-mobile-env.sh > /dev/null 2>&1 && cd MauiApp && /usr/local/share/dotnet/dotnet build -t:Run -f net10.0-android'

app-run-ios:
	@echo "🚀 Building and running iOS app..."
	@echo "   Make sure iOS simulator is running!"
	@bash -c 'source ./scripts/set-mobile-env.sh > /dev/null 2>&1 && cd MauiApp && dotnet build -t:Run -f net10.0-ios'

# View Android logs
app-logs-android:
	@echo "📱 Viewing Android logs (filtering for Datadog and app output)..."
	@echo "   Press Ctrl+C to exit"
	@if command -v adb >/dev/null 2>&1; then \
		adb logcat | grep -E "\[Datadog\]|mono-stdout|DatadogMauiApp"; \
	else \
		echo "❌ adb not found in PATH"; \
		echo "Try: ~/Library/Android/sdk/platform-tools/adb logcat | grep '\[Datadog\]'"; \
	fi

# View Android logs (all)
app-logs-android-all:
	@echo "📱 Viewing all Android logs..."
	@echo "   Press Ctrl+C to exit"
	@if command -v adb >/dev/null 2>&1; then \
		adb logcat; \
	else \
		echo "❌ adb not found in PATH"; \
		echo "Try: ~/Library/Android/sdk/platform-tools/adb logcat"; \
	fi

# Clear Android logs
app-logs-clear:
	@echo "🧹 Clearing Android logs..."
	@if command -v adb >/dev/null 2>&1; then \
		adb logcat -c; \
		echo "✅ Logs cleared"; \
	else \
		echo "❌ adb not found in PATH"; \
	fi

# =============================================================================
# Composite Commands
# =============================================================================

all: api-build app-build-android
	@echo ""
	@echo "✅ All builds complete!"
	@echo ""
	@echo "Next steps:"
	@echo "  1. make api-start       # Start the API"
	@echo "  2. make app-run-android # Run the app"

test: api-start
	@echo "⏳ Waiting for API to be ready..."
	@sleep 3
	@$(MAKE) api-test

clean: api-clean app-clean
	@echo "✅ Everything cleaned"

# =============================================================================
# Development Helpers
# =============================================================================

api-dev: api-build api-start api-logs

app-dev-android: app-clean app-restore app-run-android

app-dev-ios: app-clean app-restore app-run-ios

# Quick status check
status: api-status
	@echo ""
	@echo "📱 Android App:"
	@if [ -f "MauiApp/bin/Debug/net10.0-android/DatadogMauiApp.dll" ]; then \
		echo "   ✅ Last build: $$(stat -f '%Sm' -t '%Y-%m-%d %H:%M:%S' MauiApp/bin/Debug/net10.0-android/DatadogMauiApp.dll)"; \
	else \
		echo "   ⚠️  Not built yet"; \
	fi
	@echo ""
	@echo "🍎 iOS App:"
	@if [ -f "MauiApp/bin/Debug/net10.0-ios/iossimulator-arm64/DatadogMauiApp.app/DatadogMauiApp" ]; then \
		echo "   ✅ Last build: $$(stat -f '%Sm' -t '%Y-%m-%d %H:%M:%S' MauiApp/bin/Debug/net10.0-ios/iossimulator-arm64/DatadogMauiApp.app/DatadogMauiApp)"; \
	else \
		echo "   ⚠️  Not built yet"; \
	fi

# Full integration test
integration-test:
	@echo "🧪 Running full integration test..."
	@$(MAKE) api-build
	@$(MAKE) api-start
	@echo "⏳ Waiting for API to be ready..."
	@sleep 3
	@$(MAKE) api-test
	@echo ""
	@echo "✅ API integration test complete!"
	@echo ""
	@echo "📱 To test mobile app:"
	@echo "  1. Start Android emulator"
	@echo "  2. Run: make app-run-android"
	@echo "  3. Submit data in the app"
	@echo "  4. Run: make api-logs"

# View all documentation
docs:
	@echo "📚 Documentation:"
	@echo ""
	@echo "Main Documentation:"
	@echo "  📄 README.md              # Project overview and setup"
	@echo "  📄 QUICKSTART.md          # Quick start guide"
	@echo ""
	@echo "Detailed Guides:"
	@echo "  📁 docs/                  # All detailed documentation"
	@echo "     - setup/               # Setup guides"
	@echo "     - guides/              # Feature guides"
	@echo "     - reference/           # Technical reference"

# =============================================================================
# Datadog Commands
# =============================================================================
# Note: Datadog bindings are now installed via NuGet packages automatically
# The datadog-dotnet-mobile-sdk-bindings repository is only for reference
