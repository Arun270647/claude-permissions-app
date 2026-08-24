#!/bin/bash
# Build script for macOS distribution
# Run this on a Mac

set -e

VERSION="1.0.0"
PROJECT_DIR="$(cd "$(dirname "$0")" && pwd)"

echo "🔨 Building Claude Permission Assistant for macOS v$VERSION"

# Build for Apple Silicon (M1/M2/M3)
echo "Building for Apple Silicon (arm64)..."
dotnet publish src/macOS/ClaudePermissionAssistant.MacApp/ClaudePermissionAssistant.MacApp.csproj \
  -c Release -r osx-arm64 --self-contained \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o publish/osx-arm64

# Build for Intel Macs
echo "Building for Intel Macs (x64)..."
dotnet publish src/macOS/ClaudePermissionAssistant.MacApp/ClaudePermissionAssistant.MacApp.csproj \
  -c Release -r osx-x64 --self-contained \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o publish/osx-x64

# Make executables runnable
chmod +x publish/osx-arm64/ClaudePermissionAssistant
chmod +x publish/osx-x64/ClaudePermissionAssistant

# Rename with version
cd publish/osx-arm64
cp ClaudePermissionAssistant "ClaudePermissionAssistant-macOS-arm64-v$VERSION"
echo "✅ Created: publish/osx-arm64/ClaudePermissionAssistant-macOS-arm64-v$VERSION"

cd ../osx-x64
cp ClaudePermissionAssistant "ClaudePermissionAssistant-macOS-x64-v$VERSION"
echo "✅ Created: publish/osx-x64/ClaudePermissionAssistant-macOS-x64-v$VERSION"

cd "$PROJECT_DIR"

echo ""
echo "✅ Build complete!"
echo ""
echo "📦 Distribution files:"
echo "  - publish/osx-arm64/ClaudePermissionAssistant-macOS-arm64-v$VERSION"
echo "  - publish/osx-x64/ClaudePermissionAssistant-macOS-x64-v$VERSION"
echo ""
echo "Upload these to GitHub Releases for public download"
