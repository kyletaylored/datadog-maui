#!/bin/bash

# Load and export mobile app environment variables from .env
# Usage: source ./set-mobile-env.sh

# Set up Android SDK environment if not already set
if [ -z "$ANDROID_HOME" ] && [ -d "$HOME/Library/Android/sdk" ]; then
    export ANDROID_HOME="$HOME/Library/Android/sdk"
    export PATH="$ANDROID_HOME/platform-tools:$ANDROID_HOME/tools:$PATH"
fi

# Load .env file if it exists
if [ -f .env ]; then
    # Export Android RUM credentials
    export DD_RUM_ANDROID_CLIENT_TOKEN=$(grep DD_RUM_ANDROID_CLIENT_TOKEN .env | cut -d '=' -f2)
    export DD_RUM_ANDROID_APPLICATION_ID=$(grep DD_RUM_ANDROID_APPLICATION_ID .env | cut -d '=' -f2)

    # Export iOS RUM credentials
    export DD_RUM_IOS_CLIENT_TOKEN=$(grep DD_RUM_IOS_CLIENT_TOKEN .env | cut -d '=' -f2)
    export DD_RUM_IOS_APPLICATION_ID=$(grep DD_RUM_IOS_APPLICATION_ID .env | cut -d '=' -f2)

    echo "✅ Mobile app environment variables exported"
    echo ""
    echo "Android RUM credentials:"
    echo "  DD_RUM_ANDROID_CLIENT_TOKEN:    ${DD_RUM_ANDROID_CLIENT_TOKEN:0:10}... (truncated)"
    echo "  DD_RUM_ANDROID_APPLICATION_ID:  ${DD_RUM_ANDROID_APPLICATION_ID}"
    echo ""
    echo "iOS RUM credentials:"
    echo "  DD_RUM_IOS_CLIENT_TOKEN:        ${DD_RUM_IOS_CLIENT_TOKEN:0:10}... (truncated)"
    echo "  DD_RUM_IOS_APPLICATION_ID:      ${DD_RUM_IOS_APPLICATION_ID}"
    echo ""
    echo "Now you can build the mobile app:"
    echo "  make app-build-android"
    echo "  make app-build-ios"
else
    echo "❌ .env file not found!"
    echo "Create one from .env.example first:"
    echo "  cp .env.example .env"
fi
