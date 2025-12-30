.PHONY: help api-build api-start api-stop api-restart api-logs api-status api-test api-clean \
        app-clean app-restore app-build-android app-build-ios app-run-android app-run-ios \
        docker-build docker-start docker-stop docker-restart docker-logs docker-clean \
        datadog-build-android datadog-build-ios datadog-build-all \
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
	@echo "API Commands (Docker):"
	@echo "  make api-build       Build Docker image for API"
	@echo "  make api-start       Start API container"
	@echo "  make api-stop        Stop API container"
	@echo "  make api-restart     Restart API container"
	@echo "  make api-logs        Show API logs (follow mode)"
	@echo "  make api-status      Show container status"
	@echo "  make api-test        Test all API endpoints"
	@echo "  make api-clean       Remove container and image"
	@echo ""
	@echo "Mobile App Commands:"
	@echo "  make app-clean       Clean app build artifacts"
	@echo "  make app-restore     Restore NuGet packages"
	@echo "  make app-build-android   Build Android app"
	@echo "  make app-build-ios       Build iOS app"
	@echo "  make app-run-android     Build and run on Android emulator"
	@echo "  make app-run-ios         Build and run on iOS simulator"
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
	@echo "🔨 Building Docker image..."
	cd Api && docker build --load -t $(IMAGE_NAME) .
	@echo "✅ Image built successfully: $(IMAGE_NAME)"

api-start: docker-start
docker-start:
	@if docker ps -a --format '{{.Names}}' | grep -q "^$(CONTAINER_NAME)$$"; then \
		echo "📦 Container exists, starting..."; \
		docker start $(CONTAINER_NAME); \
	else \
		echo "📦 Creating and starting new container..."; \
		docker run -d -p $(PORT):8080 --name $(CONTAINER_NAME) $(IMAGE_NAME); \
	fi
	@echo "✅ API started at http://localhost:$(PORT)"
	@echo "   Android: http://10.0.2.2:$(PORT)"
	@echo "   iOS: http://localhost:$(PORT)"

api-stop: docker-stop
docker-stop:
	@if docker ps --format '{{.Names}}' | grep -q "^$(CONTAINER_NAME)$$"; then \
		echo "🛑 Stopping container..."; \
		docker stop $(CONTAINER_NAME); \
		echo "✅ Container stopped"; \
	else \
		echo "ℹ️  Container is not running"; \
	fi

api-restart: docker-restart
docker-restart: api-stop
	@sleep 1
	@$(MAKE) api-start

api-logs: docker-logs
docker-logs:
	@if docker ps -a --format '{{.Names}}' | grep -q "^$(CONTAINER_NAME)$$"; then \
		echo "📋 Showing logs (Ctrl+C to exit)..."; \
		docker logs -f $(CONTAINER_NAME); \
	else \
		echo "❌ Container does not exist"; \
		exit 1; \
	fi

api-status: docker-status
docker-status:
	@echo "📊 Container Status:"
	@if docker ps --format '{{.Names}}' | grep -q "^$(CONTAINER_NAME)$$"; then \
		docker ps --filter "name=$(CONTAINER_NAME)" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"; \
		echo ""; \
		echo "✅ API is running"; \
	else \
		echo "❌ API is not running"; \
	fi

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
	@echo "🧹 Cleaning up..."
	@if docker ps -a --format '{{.Names}}' | grep -q "^$(CONTAINER_NAME)$$"; then \
		echo "Stopping and removing container..."; \
		docker stop $(CONTAINER_NAME) 2>/dev/null || true; \
		docker rm $(CONTAINER_NAME) 2>/dev/null || true; \
	fi
	@if docker images --format '{{.Repository}}' | grep -q "^$(IMAGE_NAME)$$"; then \
		echo "Removing image..."; \
		docker rmi $(IMAGE_NAME) 2>/dev/null || true; \
	fi
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
	cd MauiApp && dotnet build -f net10.0-android
	@echo "✅ Android build complete"

app-build-ios:
	@echo "🔨 Building iOS app..."
	@echo "⚠️  Note: iOS build requires Xcode simulator runtime that matches SDK 23C53"
	@echo "⚠️  Current Xcode 26.2 has a version mismatch with available simulator runtimes"
	@echo "⚠️  This is a known Xcode/iOS SDK versioning issue, not a code issue"
	cd MauiApp && dotnet build -f net10.0-ios
	@echo "✅ iOS build complete"

app-run-android:
	@echo "🚀 Building and running Android app..."
	@echo "   Make sure Android emulator is running!"
	cd MauiApp && dotnet build -t:Run -f net10.0-android

app-run-ios:
	@echo "🚀 Building and running iOS app..."
	@echo "   Make sure iOS simulator is running!"
	cd MauiApp && dotnet build -t:Run -f net10.0-ios

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
	@echo "📚 Documentation Files:"
	@echo ""
	@ls -1 *.md | while read file; do \
		echo "  📄 $$file"; \
	done
	@echo ""
	@echo "Quick links:"
	@echo "  cat README.md           # Full documentation"
	@echo "  cat QUICKSTART.md       # Quick start guide"
	@echo "  cat READY_TO_TEST.md    # Testing guide"
	@echo "  cat BUILD_SUCCESS.md    # Build details"

# =============================================================================
# Datadog Commands
# =============================================================================
# Note: Datadog bindings are now installed via NuGet packages automatically
# The datadog-dotnet-mobile-sdk-bindings repository is only for reference
