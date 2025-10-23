@echo off
:loop
echo Building and starting Order Service API with embedded consumer...
dotnet run --project OrderService.Api/OrderService.Api.csproj
echo Service stopped. Press any key to restart or Ctrl+C to exit.
pause > nul
goto loop
