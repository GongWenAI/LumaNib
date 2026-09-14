import AppKit
import Carbon

@main struct CoreTests {
    static func main() throws {
        _ = NSApplication.shared
        var count = 0
        func check(_ condition: @autoclosure () -> Bool, _ message: String) {
            if !condition() { fatalError("FAIL: " + message) }
            count += 1
            print("PASS: " + message)
        }
        for language in ["zh", "zh-CN", "zh-Hans-CN", "zh-Hant-TW", "zh_HK", "ZH-hans"] {
            check(AppLanguage.text("鼠标圆环", language: language) == "鼠标圆环", "Chinese UI for " + language)
        }
        for language in ["en-US", "en-GB", "fr-FR", "ja-JP", "de-DE", ""] {
            check(AppLanguage.text("鼠标圆环", language: language) == "Mouse Ring", "English fallback for " + language)
        }
        check(AppLanguage.text("LumaNib", language: "en") == "LumaNib", "Product name is preserved")
        check(Palette.pink.rawValue == "玫红" && DrawMode.hold.rawValue == "按住绘制", "Persisted enum values remain compatible")
        check(Palette.pink.displayName == L("玫红"), "Color display names use current UI language")
        check(LF("{0} 笔标注", 3) == (AppLanguage.isChinese(AppLanguage.preferred) ? "3 笔标注" : "Strokes: 3"), "Dynamic counts use selected language")
        let space = Shortcut(key: "空格", modifiers: "⌘", recordedKeyCode: 49)
        check(space.label == "⌘" + L("空格") && space.key == "空格" && space.keyCode == 49, "Localized key label preserves stored shortcut")
        let oldData = Data(#"{"ringEnabled":true,"ringRadius":25,"ringWidth":5,"ringColor":"玫红","rightColor":"冰蓝","penColor":"荧黄","penWidth":4,"penGlow":12,"drawMode":"按住绘制","autoFade":false,"fadeDelay":5,"shortcuts":[{"key":"R","modifiers":"⌃⌥"},{"key":"D","modifiers":"⌘"},{"key":"Z","modifiers":"⌃⌥"},{"key":"X","modifiers":"⌃⌥"}]}"#.utf8)
        var oldPrefs = try JSONDecoder().decode(Preferences.self, from: oldData)
        oldPrefs.sanitize()
        check(oldPrefs.ringColor == .pink && oldPrefs.rightColor == .cyan && oldPrefs.shortcuts[1].label == "⌘D" && oldPrefs.drawMode == .hold, "Existing Chinese preferences keep colors, mode and custom keys")
        let p = Preferences()
        let restored = try JSONDecoder().decode(Preferences.self, from: JSONEncoder().encode(p))
        check(restored.shortcuts == p.shortcuts, "Settings round-trip preserves shortcut mappings")
        check(restored.penColor == p.penColor && restored.ringRadius == p.ringRadius, "Settings round-trip preserves appearance")
        check(Set(p.shortcuts.map(\.label)).count == 4, "Default shortcuts are distinct")
        check(p.shortcuts[1].keyCode == UInt32(kVK_ANSI_D), "Drawing shortcut uses the D physical key")
        check(p.shortcuts[1].carbonModifiers == UInt32(controlKey | optionKey), "Default shortcut uses Control and Option")
        var broken = p
        broken.ringRadius = .nan; broken.penWidth = 500; broken.penGlow = -8; broken.fadeDelay = 100
        broken.shortcuts = [Shortcut(key: "R"), Shortcut(key: "R")]
        broken.sanitize()
        check(broken.ringRadius == 21 && broken.penWidth == 14 && broken.penGlow == 0 && broken.fadeDelay == 30,
              "Invalid preferences are safely clamped")
        check(broken.shortcuts == p.shortcuts, "Invalid shortcut collection restores valid defaults")
        let digitEvent = NSEvent.keyEvent(with: .keyDown, location: .zero, modifierFlags: [.command, .shift], timestamp: 0,
                                         windowNumber: 0, context: nil, characters: "1", charactersIgnoringModifiers: "1", isARepeat: false, keyCode: 18)!
        let custom = Shortcut.from(event: digitEvent)!
        check(custom.keyCode == 18 && custom.label == "⇧⌘1", "Recorder accepts a directly entered digit shortcut")
        check(custom.carbonModifiers == UInt32(cmdKey | shiftKey), "Custom shortcuts do not force Control or Option")
        check(custom.requiredFlags == [.command, .shift], "Hold mode checks the actual custom modifiers")
        let customRestored = try JSONDecoder().decode(Shortcut.self, from: JSONEncoder().encode(custom))
        check(customRestored == custom, "Recorded physical key code survives saving and loading")
        let plainEvent = NSEvent.keyEvent(with: .keyDown, location: .zero, modifierFlags: [], timestamp: 0,
                                         windowNumber: 0, context: nil, characters: "d", charactersIgnoringModifiers: "d", isARepeat: false, keyCode: 2)!
        check(Shortcut.from(event: plainEvent) == nil, "Plain typing cannot accidentally become a global shortcut")
        let escapeEvent = NSEvent.keyEvent(with: .keyDown, location: .zero, modifierFlags: [.control], timestamp: 0,
                                          windowNumber: 0, context: nil, characters: "", charactersIgnoringModifiers: "", isARepeat: false, keyCode: 53)!
        check(Shortcut.from(event: escapeEvent) == nil, "Escape remains reserved for exiting or cancelling")

        let left = RingGeometry.path(center: .zero, radius: 21, leftHalf: true)
        let right = RingGeometry.path(center: .zero, radius: 21, leftHalf: false)
        check(left.bounds.maxX < 0 && left.bounds.minX < -20, "Left half remains entirely left of the pointer")
        check(right.bounds.minX > 0 && right.bounds.maxX > 20, "Right half remains entirely right of the pointer")
        check(abs(left.bounds.height-right.bounds.height) < 0.001, "Halves are symmetric with gaps above and below")

        let persistent = Stroke(points: [.zero], color: .cyan, width: 4, glow: 12, finishedAt: 1)
        check(persistent.opacity(at: 100000) == 1, "Persistent ink does not disappear over time")
        let fading = Stroke(points: [.zero], color: .yellow, width: 4, glow: 12, finishedAt: 10, fadeStartsAt: 15)
        check(fading.opacity(at: 14) == 1 && fading.opacity(at: 15) == 1, "Auto-fade respects the complete hold interval")
        check(abs(fading.opacity(at: 15.325)-0.5) < 0.001, "Fade transitions continuously")
        check(fading.opacity(at: 16) == 0, "Expired ink reaches zero opacity")
        let bounds = Stroke(points: [NSPoint(x: -150, y: 100), NSPoint(x: 75, y: -80)], color: .yellow, width: 4, glow: 12).bounds
        check(bounds.contains(NSPoint(x: -190, y: 140)) && bounds.contains(NSPoint(x: 110, y: -110)), "Damage bounds include glow and negative monitor coordinates")

        // Check registration conflict handling without changing the running app's keys.
        let keys = HotKeys()
        let invalid = keys.configure([Shortcut(key: "D"), Shortcut(key: "D"), Shortcut(key: "X"), Shortcut(key: "Z")])
        check(invalid != nil && keys.current.isEmpty, "Duplicate shortcuts are rejected before registration")
        keys.stop()
        print("\(count) assertions passed. UI input, OBS capture and long-duration stability require separate checks.")
    }
}
