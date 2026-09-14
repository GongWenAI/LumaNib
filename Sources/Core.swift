import AppKit
import Carbon

enum Palette: String, CaseIterable, Codable, Identifiable {
    case cyan = "冰蓝", mint = "薄荷", yellow = "荧黄", pink = "玫红", orange = "橙色", purple = "紫色"
    var id: String { rawValue }
    var displayName: String { L(rawValue) }
    var color: NSColor {
        switch self {
        case .cyan: return NSColor(srgbRed: 0.10, green: 0.85, blue: 1, alpha: 1)
        case .mint: return NSColor(srgbRed: 0.18, green: 1, blue: 0.65, alpha: 1)
        case .yellow: return NSColor(srgbRed: 1, green: 0.89, blue: 0.12, alpha: 1)
        case .pink: return NSColor(srgbRed: 1, green: 0.22, blue: 0.61, alpha: 1)
        case .orange: return NSColor(srgbRed: 1, green: 0.48, blue: 0.13, alpha: 1)
        case .purple: return NSColor(srgbRed: 0.70, green: 0.40, blue: 1, alpha: 1)
        }
    }
}

enum DrawMode: String, CaseIterable, Codable, Identifiable {
    case toggle = "按一下切换", hold = "按住绘制"
    var id: String { rawValue }
    var displayName: String { L(rawValue) }
}

struct Shortcut: Codable, Equatable {
    var key: String
    var modifiers: String = "⌃⌥"
    var recordedKeyCode: UInt32? = nil
    static let keyCodes: [String: UInt32] = [
        "A": 0, "S": 1, "D": 2, "F": 3, "H": 4, "G": 5, "Z": 6, "X": 7,
        "C": 8, "V": 9, "B": 11, "Q": 12, "W": 13, "E": 14, "R": 15,
        "Y": 16, "T": 17, "O": 31, "U": 32, "I": 34, "P": 35, "L": 37,
        "J": 38, "K": 40, "N": 45, "M": 46
    ]
    var label: String {
        let name = key.hasPrefix("键码") ? L("键码") + key.dropFirst(2) : L(key)
        return modifiers + name
    }
    var identity: String { "\(keyCode):\(carbonModifiers)" }
    var carbonModifiers: UInt32 {
        var value: UInt32 = 0
        if modifiers.contains("⌃") { value |= UInt32(controlKey) }
        if modifiers.contains("⌥") { value |= UInt32(optionKey) }
        if modifiers.contains("⇧") { value |= UInt32(shiftKey) }
        if modifiers.contains("⌘") { value |= UInt32(cmdKey) }
        return value
    }
    var keyCode: UInt32 { recordedKeyCode ?? Self.keyCodes[key] ?? 2 }
    var isValid: Bool {
        keyCode < 128 && keyCode != 53 && (recordedKeyCode != nil || Self.keyCodes[key] != nil)
            && (carbonModifiers & UInt32(controlKey | optionKey | cmdKey)) != 0
    }
    var requiredFlags: NSEvent.ModifierFlags {
        var result: NSEvent.ModifierFlags = []
        if modifiers.contains("⌃") { result.insert(.control) }
        if modifiers.contains("⌥") { result.insert(.option) }
        if modifiers.contains("⇧") { result.insert(.shift) }
        if modifiers.contains("⌘") { result.insert(.command) }
        return result
    }
    static func from(event: NSEvent) -> Shortcut? {
        let flags = event.modifierFlags.intersection([.control, .option, .shift, .command])
        guard !flags.intersection([.control, .option, .command]).isEmpty, event.keyCode != 53 else { return nil }
        var modifiers = ""
        if flags.contains(.control) { modifiers += "⌃" }
        if flags.contains(.option) { modifiers += "⌥" }
        if flags.contains(.shift) { modifiers += "⇧" }
        if flags.contains(.command) { modifiers += "⌘" }
        let special: [UInt16: String] = [
            36: "↩", 48: "⇥", 49: "空格", 51: "⌫", 76: "⌤", 117: "⌦",
            123: "←", 124: "→", 125: "↓", 126: "↑", 115: "Home", 119: "End", 116: "Page Up", 121: "Page Down",
            122: "F1", 120: "F2", 99: "F3", 118: "F4", 96: "F5", 97: "F6", 98: "F7", 100: "F8",
            101: "F9", 109: "F10", 103: "F11", 111: "F12", 105: "F13", 107: "F14", 113: "F15",
            106: "F16", 64: "F17", 79: "F18", 80: "F19", 90: "F20"
        ]
        let physicalLetter = keyCodes.first(where: { $0.value == UInt32(event.keyCode) })?.key
        let name = special[event.keyCode] ?? physicalLetter ?? event.charactersIgnoringModifiers?.uppercased() ?? "键码\(event.keyCode)"
        return Shortcut(key: name, modifiers: modifiers, recordedKeyCode: UInt32(event.keyCode))
    }
}

struct Preferences: Codable {
    var ringEnabled = true
    var ringRadius: Double = 21
    var ringWidth: Double = 3
    var ringColor: Palette = .cyan
    var rightColor: Palette = .orange
    var penColor: Palette = .yellow
    var penWidth: Double = 4
    var penGlow: Double = 12
    var drawMode: DrawMode = .toggle
    var autoFade = false
    var fadeDelay: Double = 5
    var shortcuts = [Shortcut(key: "R"), Shortcut(key: "D"), Shortcut(key: "Z"), Shortcut(key: "X")]
    mutating func sanitize() {
        ringRadius = min(42, max(12, ringRadius.isFinite ? ringRadius : 21))
        ringWidth = min(6, max(2, ringWidth.isFinite ? ringWidth : 3))
        penWidth = min(14, max(2, penWidth.isFinite ? penWidth : 4))
        penGlow = min(24, max(0, penGlow.isFinite ? penGlow : 12))
        fadeDelay = min(30, max(1, fadeDelay.isFinite ? fadeDelay : 5))
        if shortcuts.count != 4 || Set(shortcuts.map(\.identity)).count != 4 || shortcuts.contains(where: { !$0.isValid }) {
            shortcuts = Preferences().shortcuts
        }
    }
}

struct Stroke {
    var points: [NSPoint]
    var color: Palette
    var width: CGFloat
    var glow: CGFloat
    var finishedAt: TimeInterval?
    // Deadline is set when the stroke finishes, or when the fade setting changes.
    var fadeStartsAt: TimeInterval?
    static let fadeDuration: TimeInterval = 0.65
    func opacity(at now: TimeInterval) -> CGFloat {
        guard let start = fadeStartsAt else { return 1 }
        return CGFloat(min(1, max(0, 1 - (now - start) / Self.fadeDuration)))
    }
    var bounds: NSRect {
        guard let first = points.first else { return .zero }
        var minX = first.x, maxX = first.x, minY = first.y, maxY = first.y
        for p in points { minX = min(minX, p.x); maxX = max(maxX, p.x); minY = min(minY, p.y); maxY = max(maxY, p.y) }
        return NSRect(x: minX, y: minY, width: max(1, maxX-minX), height: max(1, maxY-minY))
            .insetBy(dx: -(glow * 3 + width + 4), dy: -(glow * 3 + width + 4))
    }
}

enum RingGeometry {
    static let left = (start: CGFloat(98), end: CGFloat(262))
    static let right = (start: CGFloat(278), end: CGFloat(442))
    static func path(center: NSPoint, radius: CGFloat, leftHalf: Bool) -> NSBezierPath {
        let angles = leftHalf ? left : right
        let path = NSBezierPath()
        path.appendArc(withCenter: center, radius: radius, startAngle: angles.start, endAngle: angles.end)
        path.lineCapStyle = .round
        return path
    }
}

func glowingStroke(_ path: NSBezierPath, color: NSColor, width: CGFloat, glow: CGFloat, opacity: CGFloat = 1) {
    NSGraphicsContext.saveGraphicsState()
    defer { NSGraphicsContext.restoreGraphicsState() }
    path.lineCapStyle = .round
    path.lineJoinStyle = .round
    if glow > 0 {
        let shadow = NSShadow()
        shadow.shadowColor = color.withAlphaComponent(0.85 * opacity)
        shadow.shadowBlurRadius = glow
        shadow.shadowOffset = .zero
        shadow.set()
        path.lineWidth = width
        color.withAlphaComponent(opacity * 0.85).setStroke()
        path.stroke()
        shadow.shadowBlurRadius = glow * 0.38
        shadow.set()
        color.withAlphaComponent(opacity).setStroke()
        path.stroke()
    }
    let noShadow = NSShadow(); noShadow.shadowColor = nil; noShadow.set()
    path.lineWidth = width
    color.withAlphaComponent(opacity).setStroke()
    path.stroke()
    if glow > 0 {
        path.lineWidth = max(0.7, width * 0.3)
        (color.blended(withFraction: 0.62, of: .white) ?? .white).withAlphaComponent(opacity).setStroke()
        path.stroke()
    }
}

func drawStroke(_ stroke: Stroke, origin: NSPoint, opacity: CGFloat) {
    guard let first = stroke.points.first else { return }
    let path = NSBezierPath()
    path.move(to: NSPoint(x: first.x-origin.x, y: first.y-origin.y))
    if stroke.points.count == 1 {
        path.line(to: NSPoint(x: first.x-origin.x+0.01, y: first.y-origin.y))
    } else {
        // Rounded joins avoid sharp corners without changing the user's stroke shape.
        for p in stroke.points.dropFirst() { path.line(to: NSPoint(x: p.x-origin.x, y: p.y-origin.y)) }
    }
    glowingStroke(path, color: stroke.color.color, width: stroke.width, glow: stroke.glow, opacity: opacity)
}
