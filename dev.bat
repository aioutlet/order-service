@echo off
:loop
echo.
echo ============================================
echo Starting order service...
echo ============================================
echo.

REM Check if port 5088 is in use and kill the process
echo Checking port 5088...
for /f "tokens=5" %%a in ('netstat -ano ^| findstr :5088 ^| findstr LISTENING') do (
    echo Port 5088 is in use by PID %%a, killing process...
    taskkill /PID %%a /F >nul 2>&1
    timeout /t 1 >nul
)

echo Starting service on port 5088...
dotnet run --project OrderService.Api/OrderService.Api.csproj

echo.
echo ============================================
echo Service stopped. Press any key to restart or Ctrl+C to exit.
echo ============================================
pause > nul
goto loop

