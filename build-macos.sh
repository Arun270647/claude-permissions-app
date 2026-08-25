#!/bin/bash

# Build script for macOS
# Builds both Intel and Apple Silicon versions
# Creates .app bundles and packages them as .dmg files

set -e

VERSION="1.0.0"
APP_NAME="ClaudePermissionAssistant"
BUILD_DIR="publish"
OUTPUT_DIR="releases"

echo "🍎 Building Claude Permission Assistant for macOS..."

# Clean previous builds
rm -rf $BUILD_DIR
rm -rf $OUTPUT_DIR
mkdir -p $OUTPUT_DIR

# Function to create .app bundle
create_app_bundle() {
    local ARCH=$1
    local EXECUTABLE_PATH=$2
    local APP_BUNDLE="${BUILD_DIR}/${APP_NAME}.app"

    echo "📦 Creating .app bundle for ${ARCH}..."

    # Create bundle structure
    mkdir -p "${APP_BUNDLE}/Contents/MacOS"
    mkdir -p "${APP_BUNDLE}/Contents/Resources"

    # Copy executable
    cp "${EXECUTABLE_PATH}" "${APP_BUNDLE}/Contents/MacOS/${APP_NAME}"
    chmod +x "${APP_BUNDLE}/Contents/MacOS/${APP_NAME}"

    # Create Info.plist
    cat > "${APP_BUNDLE}/Contents/Info.plist" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleDevelopmentRegion</key>
    <string>en</string>
    <key>CFBundleExecutable</key>
    <string>${APP_NAME}</string>
    <key>CFBundleIdentifier</key>
    <string>com.claudepermission.assistant</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
    <key>CFBundleName</key>
    <string>${APP_NAME}</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>${VERSION}</string>
    <key>CFBundleVersion</key>
    <string>${VERSION}</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.15</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>LSUIElement</key>
    <false/>
</dict>
</plist>
EOF

    echo "✅ App bundle created: ${APP_BUNDLE}"
}

# Function to create DMG
create_dmg() {
    local ARCH=$1
    local APP_BUNDLE="${BUILD_DIR}/${APP_NAME}.app"
    local DMG_NAME="${APP_NAME}-macOS-${ARCH}-v${VERSION}.dmg"
    local TEMP_DMG="${BUILD_DIR}/temp.dmg"

    echo "💿 Creating DMG for ${ARCH}..."

    # Create temporary DMG
    hdiutil create -volname "${APP_NAME}" \
                   -srcfolder "${APP_BUNDLE}" \
                   -ov -format UDRW \
                   "${TEMP_DMG}"

    # Convert to compressed DMG
    hdiutil convert "${TEMP_DMG}" \
                    -format UDZO \
                    -o "${OUTPUT_DIR}/${DMG_NAME}"

    # Clean up temp DMG
    rm "${TEMP_DMG}"

    echo "✅ DMG created: ${OUTPUT_DIR}/${DMG_NAME}"
}

# Function to create ZIP
create_zip() {
    local ARCH=$1
    local APP_BUNDLE="${BUILD_DIR}/${APP_NAME}.app"
    local ZIP_NAME="${APP_NAME}-macOS-${ARCH}-v${VERSION}.zip"

    echo "📦 Creating ZIP for ${ARCH}..."

    cd "${BUILD_DIR}"
    zip -r "../${OUTPUT_DIR}/${ZIP_NAME}" "${APP_NAME}.app"
    cd ..

    echo "✅ ZIP created: ${OUTPUT_DIR}/${ZIP_NAME}"
}

# Build for Apple Silicon (arm64)
echo ""
echo "🔨 Building for Apple Silicon (arm64)..."
dotnet publish src/macOS/ClaudePermissionAssistant.MacApp/ClaudePermissionAssistant.MacApp.csproj \
    -c Release \
    -r osx-arm64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:Version=${VERSION} \
    -o ${BUILD_DIR}/osx-arm64

create_app_bundle "arm64" "${BUILD_DIR}/osx-arm64/${APP_NAME}"
create_zip "arm64"
# create_dmg "arm64"  # Uncomment if you want DMG files (requires macOS)

# Clean up before next build
rm -rf "${BUILD_DIR}/${APP_NAME}.app"

# Build for Intel (x64)
echo ""
echo "🔨 Building for Intel (x64)..."
dotnet publish src/macOS/ClaudePermissionAssistant.MacApp/ClaudePermissionAssistant.MacApp.csproj \
    -c Release \
    -r osx-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:Version=${VERSION} \
    -o ${BUILD_DIR}/osx-x64

create_app_bundle "x64" "${BUILD_DIR}/osx-x64/${APP_NAME}"
create_zip "x64"
# create_dmg "x64"  # Uncomment if you want DMG files (requires macOS)

echo ""
echo "🎉 Build complete!"
echo ""
echo "📦 Created packages:"
ls -lh ${OUTPUT_DIR}/

echo ""
echo "📍 Output directory: ${OUTPUT_DIR}/"
echo ""
echo "🚀 Upload these files to GitHub Releases!"
