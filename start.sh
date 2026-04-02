#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "============================================"
echo "  Employee Pair Analyzer"
echo "============================================"
echo

# Install frontend dependencies if needed
if [ ! -d "$SCRIPT_DIR/frontend/node_modules" ]; then
    echo "[Setup] Installing frontend dependencies..."
    (cd "$SCRIPT_DIR/frontend" && npm install)
    echo
fi

# Start backend
echo "[1/2] Starting .NET backend on https://localhost:7001..."
(cd "$SCRIPT_DIR/backend/EmployeesApi" && dotnet run) &
BE_PID=$!

# Give the backend a moment to bind its port before the browser opens
sleep 2

# Start frontend
echo "[2/2] Starting React frontend on https://localhost:5173..."
(cd "$SCRIPT_DIR/frontend" && npm run dev) &
FE_PID=$!

echo
echo "============================================"
echo "  Both services are running."
echo "  Backend  : https://localhost:7001"
echo "  Frontend : https://localhost:5173"
echo "============================================"
echo
echo "Open https://localhost:5173 in your browser."
echo "Press Ctrl+C to stop both services."
echo

cleanup() {
    echo
    echo "Shutting down..."
    kill "$BE_PID" "$FE_PID" 2>/dev/null || true
    exit 0
}

trap cleanup INT TERM

wait "$BE_PID" "$FE_PID"
