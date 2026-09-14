#!/bin/zsh
set -euo pipefail
PROJECT_DIR="${0:A:h}"
APP_DIR="$PROJECT_DIR/LumaNib.app"
mkdir -p "$APP_DIR/Contents/MacOS" "$APP_DIR/Contents/Resources" "$PROJECT_DIR/work"
xcrun swiftc -swift-version 5 -O -whole-module-optimization \
  -target arm64-apple-macos14.0 \
  -framework AppKit -framework SwiftUI -framework Carbon -framework ServiceManagement \
  "$PROJECT_DIR"/Sources/*.swift \
  -o "$APP_DIR/Contents/MacOS/LumaNib"
cat > "$APP_DIR/Contents/Info.plist" <<'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
<key>CFBundleExecutable</key><string>LumaNib</string>
<key>CFBundleIdentifier</key><string>local.musa.glowpointer</string>
<key>CFBundleDevelopmentRegion</key><string>en</string>
<key>CFBundleLocalizations</key><array><string>en</string><string>zh-Hans</string><string>zh-Hant</string></array>
<key>CFBundleName</key><string>LumaNib</string>
<key>CFBundleDisplayName</key><string>LumaNib</string>
<key>CFBundlePackageType</key><string>APPL</string>
<key>CFBundleShortVersionString</key><string>1.59</string>
<key>CFBundleVersion</key><string>1.59</string>
<key>LSMinimumSystemVersion</key><string>14.0</string>
<key>LSUIElement</key><true/>
<key>NSHighResolutionCapable</key><true/>
<key>CFBundleIconFile</key><string>AppIcon</string>
<key>NSHumanReadableCopyright</key><string>© 2026 宫文。保留所有权利。</string>
<key>LumaNibProjectURL</key><string>https://github.com/GongWenAI/LumaNib</string>
</dict></plist>
PLIST
if [[ -f "$PROJECT_DIR/Resources/AppIcon.icns" ]]; then
  cp "$PROJECT_DIR/Resources/AppIcon.icns" "$APP_DIR/Contents/Resources/AppIcon.icns"
fi
mkdir -p "$APP_DIR/Contents/Resources/en.lproj" "$APP_DIR/Contents/Resources/zh-Hans.lproj" "$APP_DIR/Contents/Resources/zh-Hant.lproj"
cat > "$APP_DIR/Contents/Resources/en.lproj/InfoPlist.strings" <<'STRINGS'
"NSHumanReadableCopyright" = "© 2026 宫文. All rights reserved.";
STRINGS
cat > "$APP_DIR/Contents/Resources/zh-Hans.lproj/InfoPlist.strings" <<'STRINGS'
"NSHumanReadableCopyright" = "© 2026 宫文。保留所有权利。";
STRINGS
cp "$APP_DIR/Contents/Resources/zh-Hans.lproj/InfoPlist.strings" "$APP_DIR/Contents/Resources/zh-Hant.lproj/InfoPlist.strings"
codesign --force --sign - --identifier local.musa.glowpointer "$APP_DIR"
codesign --verify --strict "$APP_DIR"
print -r -- "$APP_DIR"
