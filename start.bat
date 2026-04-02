@echo off
setlocal
cd /d "%~dp0"

echo ============================================
echo   Employee Pair Analyzer
echo ============================================
echo.

REM Install frontend dependencies if not already present
if not exist "frontend\node_modules" (
    echo [Setup] Installing frontend dependencies...
    cd frontend
    call npm install
    cd ..
    echo.
)


echo [1/2] Starting .NET backend  ^(https://localhost:7001^)...
start "Employee Analyzer - Backend" cmd /k "cd /d "%~dp0backend\EmployeesApi" && dotnet run"

REM Brief wait so the backend can begin binding its port
timeout /t 2 /nobreak >nul

echo [2/2] Starting React frontend  ^(https://localhost:5173^)...
start "Employee Analyzer - Frontend" cmd /k "cd /d "%~dp0frontend" && npm run dev"

echo.
echo ============================================
echo   Both services are starting up.
echo   Backend  : https://localhost:7001
echo   Frontend : https://localhost:5173
echo ============================================
echo.
echo Open https://localhost:5173 in your browser.
echo Close the two service windows to stop.
echo.
pause
