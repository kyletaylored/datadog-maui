.PHONY: help api-build api-start api-stop api-restart api-logs api-status api-test api-clean \
        app-clean app-restore app-build-android app-build-ios app-run-android app-run-ios \
        docker-build docker-start docker-stop docker-restart docker-logs docker-clean \
        datadog-build-android datadog-build-ios datadog-build-all \
        agent-logs logs-all api-build-simple status integration-test docs \
        app-release-android-dry app-release-ios-dry app-release-android app-release-ios \
        run-android run-ios test clean all

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
	@echo "  make app-clean       Clean app build artifacts and NuGet caches"
	@echo "  make app-restore     Restore NuGet packages"
	@echo "  make app-build-android   Build Android app (Debug)"
	@echo "  make app-build-ios       Build iOS app (Debug)"
	@echo "  make app-run-android     Build and run on Android emulator"
	@echo "  make app-run-ios         Build and run on iOS simulator"
	@echo "  make run-android         Alias for app-run-android"
	@echo "  make run-ios             Alias for app-run-ios"
	@echo "  make app-logs-android    View Android logs (filtered for Datadog)"
	@echo "  make app-logs-android-all View all Android logs"
	@echo "  make app-logs-clear      Clear Android logs"
	@echo ""
	@echo "Release Build Commands (Symbol Upload):"
	@echo "  make app-release-android-dry   Publish Android Release (dry-run, no upload)"
	@echo "  make app-release-ios-dry       Publish iOS Release (dry-run, no upload)"
	@echo "  make app-release-android       Publish Android Release (with symbol upload)"
	@echo "  make app-release-ios           Publish iOS Release (with symbol upload)"
	@echo ""
	@echo "Datadog Commands:"
	@echo "  make datadog-build-android   Build Datadog Android bindings"
	@echo "  make datadog-build-ios       Build Datadog iOS bindings"
	@echo "  make datadog-build-all       Build all Datadog bindings"
	@echo ""
	@echo "Azure Deployment Commands:"
	@echo "  make azure-deploy        Deploy API to Azure App Service"
	@echo "  make azure-logs          View Azure App Service logs"
	@echo "  make azure-status        Show Azure App Service status"
	@echo "  make azure-health        Test Azure deployment health"
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
	-cd MauiApp && dotnet clean 2>/dev/null || echo "⚠️  Clean skipped (packages not restored)"
	@echo "🗑️  Clearing NuGet caches..."
	@dotnet nuget locals all --clear
	@echo "🗑️  Removing build artifacts..."
	@rm -rf MauiApp/bin MauiApp/obj
	@echo "✅ Clean complete (run 'make app-restore' before building)"

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
# Release Build Commands (Symbol Upload Testing)
# =============================================================================

app-release-android-dry:
	@echo "🔨 Publishing Android Release (dry-run mode)..."
	@echo "   Symbol upload will be simulated (no actual upload)"
	@echo "   Note: Symbol upload runs during 'publish', not 'build'"
	@echo ""
	@bash -c 'source ./scripts/set-mobile-env.sh > /dev/null 2>&1 && cd MauiApp && dotnet publish -c Release -f net10.0-android' 2>&1 | tee /tmp/android-release.log
	@echo ""
	@echo "📋 Symbol Upload Summary:"
	@grep -i "datadog" /tmp/android-release.log | grep -i "symbol\|mapping\|upload\|dry" || echo "   ⚠️  No symbol upload output found"
	@echo ""
	@if grep -q "DatadogUploadSymbols" /tmp/android-release.log; then \
		echo "✅ Android Release publish complete (dry-run)"; \
	else \
		echo "⚠️  Symbol upload task did not run"; \
		echo "   Check that Datadog.MAUI.Symbols package is installed"; \
	fi
	@echo "   To test with actual upload, run: make app-release-android"

app-release-ios-dry:
	@echo "🔨 Publishing iOS Release (dry-run mode)..."
	@echo "   Symbol upload will be simulated (no actual upload)"
	@echo "   Note: Symbol upload runs during 'publish', not 'build'"
	@echo "   Target: iOS device (arm64)"
	@echo ""
	@bash -c 'source ./scripts/set-mobile-env.sh > /dev/null 2>&1 && cd MauiApp && dotnet publish -c Release -f net10.0-ios -r ios-arm64' 2>&1 | tee /tmp/ios-release.log
	@echo ""
	@echo "📋 Symbol Upload Summary:"
	@grep -i "datadog" /tmp/ios-release.log | grep -i "symbol\|dsym\|upload\|dry" || echo "   ⚠️  No symbol upload output found"
	@echo ""
	@if grep -q "DatadogUploadSymbols" /tmp/ios-release.log; then \
		echo "✅ iOS Release publish complete (dry-run)"; \
	else \
		echo "⚠️  Symbol upload task did not run"; \
		echo "   Check that Datadog.MAUI.Symbols package is installed"; \
	fi
	@echo "   To test with actual upload, run: make app-release-ios"

app-release-android:
	@echo "🔨 Publishing Android Release with symbol upload..."
	@echo ""
	@if [ -z "$$DD_API_KEY" ]; then \
		echo "❌ Error: DD_API_KEY environment variable not set"; \
		echo ""; \
		echo "Please set your Datadog API key:"; \
		echo "  export DD_API_KEY='your-api-key'"; \
		echo ""; \
		echo "To test without uploading, run: make app-release-android-dry"; \
		exit 1; \
	fi
	@echo "✅ DD_API_KEY is set"
	@DRY_RUN=$$(grep -o '<DatadogDryRun>[^<]*</DatadogDryRun>' MauiApp/DatadogMauiApp.csproj | sed 's/<[^>]*>//g' || echo "not set"); \
	if [ "$$DRY_RUN" = "true" ]; then \
		echo "⚠️  DatadogDryRun is currently: true"; \
		echo "   Symbols will NOT be uploaded (dry-run mode)"; \
		echo "   To enable actual upload, edit MauiApp/DatadogMauiApp.csproj:"; \
		echo "   Change <DatadogDryRun>true</DatadogDryRun> to false"; \
	elif [ "$$DRY_RUN" = "false" ]; then \
		echo "✅ DatadogDryRun is: false"; \
		echo "   Symbols WILL be uploaded to Datadog"; \
	else \
		echo "⚠️  DatadogDryRun not found in csproj (defaults to false)"; \
		echo "   Symbols WILL be uploaded to Datadog"; \
	fi
	@echo ""
	@bash -c 'source ./scripts/set-mobile-env.sh > /dev/null 2>&1 && cd MauiApp && DD_API_KEY=$$DD_API_KEY dotnet publish -c Release -f net10.0-android' 2>&1 | tee /tmp/android-release.log
	@echo ""
	@echo "📋 Symbol Upload Summary:"
	@grep -i "datadog" /tmp/android-release.log | grep -i "symbol\|mapping\|upload" || echo "   No symbol upload output found"
	@echo ""
	@echo "✅ Android Release publish complete"

app-release-ios:
	@echo "🔨 Publishing iOS Release with symbol upload..."
	@echo ""
	@if [ -z "$$DD_API_KEY" ]; then \
		echo "❌ Error: DD_API_KEY environment variable not set"; \
		echo ""; \
		echo "Please set your Datadog API key:"; \
		echo "  export DD_API_KEY='your-api-key'"; \
		echo ""; \
		echo "To test without uploading, run: make app-release-ios-dry"; \
		exit 1; \
	fi
	@echo "✅ DD_API_KEY is set"
	@DRY_RUN=$$(grep -o '<DatadogDryRun>[^<]*</DatadogDryRun>' MauiApp/DatadogMauiApp.csproj | sed 's/<[^>]*>//g' || echo "not set"); \
	if [ "$$DRY_RUN" = "true" ]; then \
		echo "⚠️  DatadogDryRun is currently: true"; \
		echo "   Symbols will NOT be uploaded (dry-run mode)"; \
		echo "   To enable actual upload, edit MauiApp/DatadogMauiApp.csproj:"; \
		echo "   Change <DatadogDryRun>true</DatadogDryRun> to false"; \
	elif [ "$$DRY_RUN" = "false" ]; then \
		echo "✅ DatadogDryRun is: false"; \
		echo "   Symbols WILL be uploaded to Datadog"; \
	else \
		echo "⚠️  DatadogDryRun not found in csproj (defaults to false)"; \
		echo "   Symbols WILL be uploaded to Datadog"; \
	fi
	@echo "   Target: iOS device (arm64)"
	@echo ""
	@bash -c 'source ./scripts/set-mobile-env.sh > /dev/null 2>&1 && cd MauiApp && DD_API_KEY=$$DD_API_KEY dotnet publish -c Release -f net10.0-ios -r ios-arm64' 2>&1 | tee /tmp/ios-release.log
	@echo ""
	@echo "📋 Symbol Upload Summary:"
	@grep -i "datadog" /tmp/ios-release.log | grep -i "symbol\|dsym\|upload" || echo "   No symbol upload output found"
	@echo ""
	@echo "✅ iOS Release publish complete"

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

# =============================================================================
# Convenience Aliases
# =============================================================================

run-android: app-run-android
run-ios: app-run-ios

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
# Azure Deployment Commands
# =============================================================================

azure-deploy:
	@echo "🚀 Deploying to Azure App Service..."
	@bash ./scripts/deploy-azure.sh

azure-logs:
	@echo "📋 Showing Azure App Service logs..."
	@az webapp log tail --name $${AZURE_APP_NAME:-datadog-maui-api} --resource-group $${AZURE_RESOURCE_GROUP:-datadog-maui-rg}

azure-status:
	@echo "📊 Azure App Service Status:"
	@az webapp show --name $${AZURE_APP_NAME:-datadog-maui-api} --resource-group $${AZURE_RESOURCE_GROUP:-datadog-maui-rg} --query "{name:name,state:state,hostNames:defaultHostName}" -o table

azure-health:
	@echo "🏥 Testing Azure deployment health..."
	@curl -s https://$${AZURE_APP_NAME:-datadog-maui-api}.azurewebsites.net/health | jq || echo "❌ Failed"

# =============================================================================
# Datadog Commands
# =============================================================================
# Note: Datadog bindings are now installed via NuGet packages automatically
# The datadog-dotnet-mobile-sdk-bindings repository is only for reference
