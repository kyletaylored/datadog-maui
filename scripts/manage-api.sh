#!/bin/bash

# Datadog MAUI API Management Script

set -e

CONTAINER_NAME="datadog-maui-api"
IMAGE_NAME="datadog-maui-api"
PORT="5000"

function show_usage {
    echo "Datadog MAUI API Management"
    echo ""
    echo "Usage: ./manage-api.sh [command]"
    echo ""
    echo "Commands:"
    echo "  build       Build the Docker image"
    echo "  start       Start the API container"
    echo "  stop        Stop the API container"
    echo "  restart     Restart the API container"
    echo "  logs        Show API logs (follow mode)"
    echo "  status      Show container status"
    echo "  test        Test API endpoints"
    echo "  clean       Stop and remove container and image"
    echo "  help        Show this help message"
    echo ""
}

function build_image {
    echo "Building Docker image..."
    cd Api
    docker build -t $IMAGE_NAME .
    cd ..
    echo "✅ Image built successfully: $IMAGE_NAME"
}

function start_container {
    if docker ps -a --format '{{.Names}}' | grep -q "^${CONTAINER_NAME}$"; then
        echo "Container already exists. Starting..."
        docker start $CONTAINER_NAME
    else
        echo "Creating and starting new container..."
        docker run -d -p $PORT:8080 --name $CONTAINER_NAME $IMAGE_NAME
    fi
    echo "✅ API started at http://localhost:$PORT"
    echo "   Android: http://10.0.2.2:$PORT"
    echo "   iOS: http://localhost:$PORT"
}

function stop_container {
    if docker ps --format '{{.Names}}' | grep -q "^${CONTAINER_NAME}$"; then
        echo "Stopping container..."
        docker stop $CONTAINER_NAME
        echo "✅ Container stopped"
    else
        echo "Container is not running"
    fi
}

function restart_container {
    stop_container
    sleep 1
    start_container
}

function show_logs {
    if docker ps -a --format '{{.Names}}' | grep -q "^${CONTAINER_NAME}$"; then
        echo "Showing logs (Ctrl+C to exit)..."
        docker logs -f $CONTAINER_NAME
    else
        echo "❌ Container does not exist"
        exit 1
    fi
}

function show_status {
    echo "Container Status:"
    if docker ps --format '{{.Names}}' | grep -q "^${CONTAINER_NAME}$"; then
        docker ps --filter "name=${CONTAINER_NAME}" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
        echo ""
        echo "✅ API is running"
    else
        echo "❌ API is not running"
    fi
}

function test_endpoints {
    echo "Testing API endpoints..."
    echo ""

    echo "1. Health Check:"
    curl -s http://localhost:$PORT/health | jq || echo "❌ Failed"
    echo ""

    echo "2. Config:"
    curl -s http://localhost:$PORT/config | jq || echo "❌ Failed"
    echo ""

    echo "3. Submit Test Data:"
    curl -s -X POST http://localhost:$PORT/data \
        -H "Content-Type: application/json" \
        -d '{
            "correlationId": "test-'$(date +%s)'",
            "sessionName": "Test Session",
            "notes": "Automated test",
            "numericValue": 42.5
        }' | jq || echo "❌ Failed"
    echo ""

    echo "4. Get All Data:"
    curl -s http://localhost:$PORT/data | jq || echo "❌ Failed"
    echo ""
}

function clean_all {
    echo "Cleaning up..."

    if docker ps -a --format '{{.Names}}' | grep -q "^${CONTAINER_NAME}$"; then
        echo "Stopping and removing container..."
        docker stop $CONTAINER_NAME 2>/dev/null || true
        docker rm $CONTAINER_NAME 2>/dev/null || true
    fi

    if docker images --format '{{.Repository}}' | grep -q "^${IMAGE_NAME}$"; then
        echo "Removing image..."
        docker rmi $IMAGE_NAME 2>/dev/null || true
    fi

    echo "✅ Cleanup complete"
}

# Main script logic
case "${1:-help}" in
    build)
        build_image
        ;;
    start)
        start_container
        ;;
    stop)
        stop_container
        ;;
    restart)
        restart_container
        ;;
    logs)
        show_logs
        ;;
    status)
        show_status
        ;;
    test)
        test_endpoints
        ;;
    clean)
        clean_all
        ;;
    help|*)
        show_usage
        ;;
esac
