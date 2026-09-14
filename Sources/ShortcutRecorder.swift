import AppKit
import SwiftUI

final class ShortcutRecorderView: NSView {
    var shortcut = Shortcut(key: "D") { didSet { needsDisplay = true; updateAccessibility() } }
    var onRecord: ((Shortcut) -> Void)?
    var onCapture: ((Bool) -> Void)?
    private var recording = false
    private var keyMonitor: Any?
    private var resignObserver: NSObjectProtocol?
    private var notice = ""
    override var acceptsFirstResponder: Bool { true }
    override var intrinsicContentSize: NSSize { NSSize(width: 220, height: 38) }

    override init(frame frameRect: NSRect) {
        super.init(frame: frameRect)
        setAccessibilityElement(true)
        setAccessibilityRole(.button)
        updateAccessibility()
    }
    required init?(coder: NSCoder) { fatalError("init(coder:) has not been implemented") }
    private func updateAccessibility() {
        setAccessibilityLabel(recording ? L("正在录入快捷键，按 Esc 取消") : L("录入快捷键 ") + shortcut.label)
        setAccessibilityValue(recording ? L("请按组合键") : shortcut.label)
        setAccessibilityHelp(L("点按后直接按下想使用的键盘组合"))
    }
    override func accessibilityPerformPress() -> Bool { startRecording(); return true }
    override func mouseDown(with event: NSEvent) { startRecording() }
    override func resignFirstResponder() -> Bool { stopRecording(); return true }
    override func viewWillMove(toWindow newWindow: NSWindow?) {
        if newWindow == nil { stopRecording() }
        super.viewWillMove(toWindow: newWindow)
    }

    private func startRecording() {
        guard !recording else { return }
        window?.makeFirstResponder(self)
        recording = true; notice = ""; needsDisplay = true; updateAccessibility()
        onCapture?(true)
        resignObserver = NotificationCenter.default.addObserver(forName: NSWindow.didResignKeyNotification, object: window, queue: .main) { [weak self] _ in
            self?.stopRecording()
        }
        keyMonitor = NSEvent.addLocalMonitorForEvents(matching: [.keyDown, .flagsChanged]) { [weak self] event in
            guard let self = self, self.recording, event.window === self.window else { return event }
            if event.type == .flagsChanged { return event }
            if event.keyCode == 53 { self.stopRecording(); return nil }
            if let shortcut = Shortcut.from(event: event) {
                self.shortcut = shortcut
                self.onRecord?(shortcut)
                self.stopRecording()
            } else {
                self.notice = L("请加上 ⌃、⌥ 或 ⌘")
                self.needsDisplay = true
            }
            return nil
        }
    }
    func stopRecording() {
        guard recording else { return }
        recording = false
        if let monitor = keyMonitor { NSEvent.removeMonitor(monitor); keyMonitor = nil }
        if let observer = resignObserver { NotificationCenter.default.removeObserver(observer); resignObserver = nil }
        notice = ""; needsDisplay = true; updateAccessibility()
        onCapture?(false)
    }
    override func draw(_ dirtyRect: NSRect) {
        let box = NSBezierPath(roundedRect: bounds.insetBy(dx: 1, dy: 1), xRadius: 8, yRadius: 8)
        NSColor.white.withAlphaComponent(recording ? 0.09 : 0.045).setFill(); box.fill()
        (recording ? Palette.cyan.color : NSColor.white.withAlphaComponent(0.15)).setStroke()
        box.lineWidth = recording ? 1.5 : 1; box.stroke()
        let text = recording ? (notice.isEmpty ? L("请按组合键…  Esc 取消") : notice) : shortcut.label
        let paragraph = NSMutableParagraphStyle(); paragraph.alignment = .center
        let attrs: [NSAttributedString.Key: Any] = [
            .font: NSFont.systemFont(ofSize: recording ? 12 : 14, weight: .medium),
            .foregroundColor: recording ? Palette.cyan.color : NSColor.labelColor,
            .paragraphStyle: paragraph
        ]
        (text as NSString).draw(in: NSRect(x: 8, y: bounds.midY-9, width: bounds.width-16, height: 20), withAttributes: attrs)
    }
    deinit {
        if let monitor = keyMonitor { NSEvent.removeMonitor(monitor) }
        if let observer = resignObserver { NotificationCenter.default.removeObserver(observer) }
    }
}

struct ShortcutRecorder: NSViewRepresentable {
    @Binding var shortcut: Shortcut
    var onCapture: (Bool) -> Void
    func makeNSView(context: Context) -> ShortcutRecorderView { ShortcutRecorderView() }
    func updateNSView(_ view: ShortcutRecorderView, context: Context) {
        view.shortcut = shortcut
        view.onRecord = { shortcut = $0 }
        view.onCapture = onCapture
    }
    static func dismantleNSView(_ view: ShortcutRecorderView, coordinator: ()) { view.stopRecording() }
}
