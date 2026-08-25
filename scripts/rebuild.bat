@echo off
cd /d "D:\projects\claude-permission app"

echo Cleaning...
dotnet clean

echo Building...
dotnet publish src/Windows/ClaudePermissionAssistant.App/ClaudePermissionAssistant.App.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o publish/win-x64

echo Cleaning up debug files...
del /q publish\win-x64\*.pdb

echo.
echo ✅ Build complete!
echo Run: publish\win-x64\ClaudePermissionAssistant.exe
pause
