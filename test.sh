#!/bin/zsh
set -euo pipefail
PROJECT_DIR="${0:A:h}"
mkdir -p "$PROJECT_DIR/work"
xcrun swiftc -swift-version 5 -O -framework AppKit -framework Carbon \
  "$PROJECT_DIR/Sources/Localization.swift" "$PROJECT_DIR/Sources/Core.swift" "$PROJECT_DIR/Sources/HotKeys.swift" \
  "$PROJECT_DIR/Tests/CoreTests.swift" -o "$PROJECT_DIR/work/CoreTests"
"$PROJECT_DIR/work/CoreTests"
xcrun swiftc -swift-version 5 -O -framework AppKit -framework Carbon \
  "$PROJECT_DIR/Sources/Localization.swift" "$PROJECT_DIR/Sources/Core.swift" "$PROJECT_DIR/Sources/Overlay.swift" \
  "$PROJECT_DIR/Tests/OverlayTests.swift" -o "$PROJECT_DIR/work/OverlayTests"
"$PROJECT_DIR/work/OverlayTests" "$PROJECT_DIR/work/ink-render-check.png"
xcrun swiftc -swift-version 5 -O -framework AppKit -framework Carbon -framework ServiceManagement \
  "$PROJECT_DIR/Sources/Localization.swift" "$PROJECT_DIR/Sources/LoginStartup.swift" \
  "$PROJECT_DIR/Tests/LoginStartupTests.swift" -o "$PROJECT_DIR/work/LoginStartupTests"
"$PROJECT_DIR/work/LoginStartupTests"
