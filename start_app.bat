@echo off
echo Killing existing instances...
taskkill /F /IM DisplayBrightness.exe >nul 2>&1

echo Building and publishing the standalone executable...
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
if %errorlevel% neq 0 (
    echo Build failed!
    pause
    exit /b %errorlevel%
)

echo Starting standalone DisplayBrightness...
start "" "bin\Release\net10.0-windows10.0.19041.0\win-x64\publish\DisplayBrightness.exe"
