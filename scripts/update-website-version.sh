#!/bin/bash
# Update website version.json with new release version
#
# Usage: ./update-website-version.sh <version>
# Example: ./update-website-version.sh 1.0.3

set -e

VERSION=$1

if [ -z "$VERSION" ]; then
    echo "Error: Version required"
    echo "Usage: $0 <version>"
    exit 1
fi

# Remove 'v' prefix if present
VERSION=${VERSION#v}

WEBSITE_DIR="website"
VERSION_FILE="$WEBSITE_DIR/version.json"
RELEASE_DATE=$(date -u +'%Y-%m-%d')

echo "Updating website version to $VERSION..."

# Generate version.json
cat > "$VERSION_FILE" << EOF
{
  "version": "$VERSION",
  "releaseDate": "$RELEASE_DATE",
  "downloadUrls": {
    "windows": "https://github.com/Arun270647/claude-permissions-app/releases/download/v$VERSION/ClaudePrompter-Windows-v$VERSION.exe",
    "macArm64": "https://github.com/Arun270647/claude-permissions-app/releases/download/v$VERSION/ClaudePrompter-macOS-arm64-v$VERSION.dmg",
    "macX64": "https://github.com/Arun270647/claude-permissions-app/releases/download/v$VERSION/ClaudePrompter-macOS-x64-v$VERSION.dmg"
  }
}
EOF

echo "✅ Updated $VERSION_FILE"
echo "Version: $VERSION"
echo "Release Date: $RELEASE_DATE"
