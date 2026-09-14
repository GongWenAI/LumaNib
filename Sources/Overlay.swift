import AppKit

final class OverlayPanel: NSPanel {
    var drawing = false
    override var canBecomeKey: Bool { drawing }
    override var canBecomeMain: Bool { false }
    init(frame: NSRect, level: Int) {
        super.init(contentRect: frame, styleMask: [.borderless, .nonactivatingPanel], backing: .buffered, defer: false)
        isOpaque = false
        backgroundColor = .clear
        hasShadow = false
        ignoresMouseEvents = true
        isReleasedWhenClosed = false
        hidesOnDeactivate = false
        isFloatingPanel = true
        becomesKeyOnlyIfNeeded = true
        acceptsMouseMovedEvents = true
        collectionBehavior = [.canJoinAllSpaces, .fullScreenAuxiliary, .stationary, .ignoresCycle]
        self.level = NSWindow.Level(rawValue: level)
        // These are intentional screen overlays, so display recording must be allowed to capture them.
        sharingType = .readOnly
        animationBehavior = .none
    }
}

final class RingView: NSView {
    var prefs = Preferences()
    var leftDown = false
    var rightDown = false
    var leftFlash: CGFloat = 0
    var rightFlash: CGFloat = 0
    override var isOpaque: Bool { false }
    override func draw(_ dirtyRect: NSRect) {
        NSColor.clear.setFill(); dirtyRect.fill(using: .copy)
        let center = NSPoint(x: bounds.midX, y: bounds.midY)
        for leftHalf in [true, false] {
            let active = leftHalf ? (leftDown ? 1 : leftFlash) : (rightDown ? 1 : rightFlash)
            let path = RingGeometry.path(center: center, radius: prefs.ringRadius, leftHalf: leftHalf)
            let color = leftHalf ? prefs.ringColor.color : prefs.rightColor.color
            // A dark under-stroke keeps the ring visible on a white page.
            path.lineWidth = prefs.ringWidth + 2
            NSColor.black.withAlphaComponent(0.25).setStroke(); path.stroke()
            glowingStroke(path, color: color, width: prefs.ringWidth + active * 1.2,
                          glow: active > 0 ? 4 + active * 10 : 0, opacity: 0.52 + active * 0.48)
        }
    }
}

final class InkView: NSView {
    weak var engine: OverlayEngine?
    var screenOrigin = NSPoint.zero
    override var isOpaque: Bool { false }
    override var acceptsFirstResponder: Bool { true }
    override func acceptsFirstMouse(for event: NSEvent?) -> Bool { true }
    override func draw(_ dirtyRect: NSRect) {
        NSColor.clear.setFill(); dirtyRect.fill(using: .copy)
        guard let engine = engine else { return }
        let now = ProcessInfo.processInfo.systemUptime
        let globalRect = dirtyRect.offsetBy(dx: screenOrigin.x, dy: screenOrigin.y)
        for stroke in engine.strokes where stroke.bounds.intersects(globalRect) {
            drawStroke(stroke, origin: screenOrigin, opacity: stroke.opacity(at: now))
        }
        if let stroke = engine.currentStroke, stroke.bounds.intersects(globalRect) {
            drawStroke(stroke, origin: screenOrigin, opacity: 1)
        }
    }
    override func resetCursorRects() {
        if engine?.drawing == true { addCursorRect(bounds, cursor: .crosshair) }
    }
    override func mouseDown(with event: NSEvent) { engine?.beginStroke(at: NSEvent.mouseLocation) }
    override func mouseDragged(with event: NSEvent) { engine?.appendPoint(NSEvent.mouseLocation) }
    override func mouseUp(with event: NSEvent) { engine?.appendPoint(NSEvent.mouseLocation); engine?.finishStroke() }
    // macOS can translate Control + left-click into a contextual/right click.
    // Hold-mode shortcuts often contain Control, so honor the physical left button here.
    override func rightMouseDown(with event: NSEvent) {
        if NSEvent.pressedMouseButtons & 1 != 0 { engine?.beginStroke(at: NSEvent.mouseLocation) }
    }
    override func rightMouseDragged(with event: NSEvent) {
        if NSEvent.pressedMouseButtons & 1 != 0 { engine?.appendPoint(NSEvent.mouseLocation) }
    }
    override func rightMouseUp(with event: NSEvent) {
        if engine?.currentStroke != nil { engine?.appendPoint(NSEvent.mouseLocation); engine?.finishStroke() }
    }
    override func keyDown(with event: NSEvent) {
        if event.keyCode == 53 { engine?.onEscape?() } else { super.keyDown(with: event) }
    }
}

final class OverlayEngine {
    var prefs = Preferences()
    private(set) var drawing = false
    private(set) var strokes: [Stroke] = []
    private(set) var currentStroke: Stroke?
    private var ringPanel: OverlayPanel!
    private var ringView: RingView!
    private var panels: [OverlayPanel] = []
    private var timer: Timer?
    private var observers: [NSObjectProtocol] = []
    private var lastMouse = NSPoint(x: -99999, y: -99999)
    private var previousButtons = 0
    private var leftReleasedAt: TimeInterval = -.infinity
    private var rightReleasedAt: TimeInterval = -.infinity
    private var tickCount = 0
    var onEscape: (() -> Void)?
    var onStrokeCount: ((Int) -> Void)?
    var onHoldWatchdog: (() -> Void)?
    var sessionHidden = false

    init() {
        ringPanel = OverlayPanel(frame: NSRect(x: 0, y: 0, width: 180, height: 180), level: NSWindow.Level.screenSaver.rawValue+1)
        ringView = RingView(frame: NSRect(x: 0, y: 0, width: 180, height: 180))
        ringPanel.contentView = ringView
        rebuildDisplays()
        observers.append(NotificationCenter.default.addObserver(forName: NSApplication.didChangeScreenParametersNotification, object: nil, queue: .main) { [weak self] _ in
            self?.rebuildDisplays()
        })
        let workspace = NSWorkspace.shared.notificationCenter
        observers.append(workspace.addObserver(forName: NSWorkspace.sessionDidResignActiveNotification, object: nil, queue: .main) { [weak self] _ in
            self?.sessionHidden = true; self?.onEscape?(); self?.ringPanel.orderOut(nil)
            self?.panels.forEach { $0.orderOut(nil) }
        })
        observers.append(workspace.addObserver(forName: NSWorkspace.sessionDidBecomeActiveNotification, object: nil, queue: .main) { [weak self] _ in
            self?.sessionHidden = false; self?.updateVisibility()
        })
        let timer = Timer(timeInterval: 1.0/60, repeats: true) { [weak self] _ in self?.tick() }
        RunLoop.main.add(timer, forMode: .common)
        self.timer = timer
    }

    func apply(_ value: Preferences) {
        let fadeChanged = prefs.autoFade != value.autoFade || prefs.fadeDelay != value.fadeDelay
        prefs = value
        ringView.prefs = value; ringView.needsDisplay = true
        if fadeChanged {
            let now = ProcessInfo.processInfo.systemUptime
            for i in strokes.indices { strokes[i].fadeStartsAt = value.autoFade ? now + value.fadeDelay : nil }
            invalidateAll()
        }
        updateVisibility()
    }

    private func rebuildDisplays() {
        // A display change ends the active gesture before rebuilding transparent windows.
        if drawing { onEscape?() }
        panels.forEach { $0.close() }; panels.removeAll()
        for screen in NSScreen.screens {
            let panel = OverlayPanel(frame: screen.frame, level: NSWindow.Level.screenSaver.rawValue)
            let view = InkView(frame: NSRect(origin: .zero, size: screen.frame.size))
            view.engine = self; view.screenOrigin = screen.frame.origin
            panel.contentView = view
            panels.append(panel)
        }
        updateVisibility()
    }

    func setDrawing(_ value: Bool) {
        if drawing == value { return }
        if !value { finishStroke() }
        drawing = value
        for panel in panels {
            panel.drawing = value
            panel.ignoresMouseEvents = !value
            panel.invalidateCursorRects(for: panel.contentView!)
            if !value { panel.resignKey() }
        }
        updateVisibility()
    }

    private func updateVisibility() {
        let showInk = !sessionHidden && (drawing || !strokes.isEmpty || currentStroke != nil)
        for panel in panels {
            if showInk { if !panel.isVisible { panel.orderFrontRegardless() } }
            else { panel.orderOut(nil) }
        }
        if prefs.ringEnabled && !drawing && !sessionHidden {
            if !ringPanel.isVisible { ringPanel.orderFrontRegardless() }
        } else { ringPanel.orderOut(nil) }
    }

    private func tick() {
        guard !sessionHidden else { return }
        onHoldWatchdog?()
        let now = ProcessInfo.processInfo.systemUptime
        if prefs.ringEnabled && !drawing {
            let point = NSEvent.mouseLocation
            if point != lastMouse {
                ringPanel.setFrameOrigin(NSPoint(x: point.x-90, y: point.y-90))
                lastMouse = point
            }
            let buttons = NSEvent.pressedMouseButtons
            if previousButtons & 1 != 0 && buttons & 1 == 0 { leftReleasedAt = now }
            if previousButtons & 2 != 0 && buttons & 2 == 0 { rightReleasedAt = now }
            let left = max(0, 1 - (now-leftReleasedAt)/0.16)
            let right = max(0, 1 - (now-rightReleasedAt)/0.16)
            if buttons != previousButtons || left > 0 || right > 0 || ringView.leftFlash > 0 || ringView.rightFlash > 0 {
                ringView.leftDown = buttons & 1 != 0; ringView.rightDown = buttons & 2 != 0
                ringView.leftFlash = left; ringView.rightFlash = right
                ringView.needsDisplay = true
            }
            previousButtons = buttons
        }
        // Only invalidate ink while it is actually fading, not on every cursor frame.
        tickCount += 1
        if prefs.autoFade && tickCount % 2 == 0 {
            let fading = strokes.filter { ($0.fadeStartsAt ?? .infinity) <= now }
            if !fading.isEmpty {
                for stroke in fading { invalidate(stroke.bounds) }
                let oldCount = strokes.count
                strokes.removeAll { $0.opacity(at: now) <= 0 }
                if oldCount != strokes.count { onStrokeCount?(strokes.count); updateVisibility() }
            }
        }
    }

    func beginStroke(at point: NSPoint) {
        guard drawing else { return }
        currentStroke = Stroke(points: [point], color: prefs.penColor, width: prefs.penWidth, glow: prefs.penGlow)
        invalidate(currentStroke!.bounds)
    }
    func appendPoint(_ point: NSPoint) {
        guard var stroke = currentStroke, let last = stroke.points.last else { return }
        if hypot(point.x-last.x, point.y-last.y) < 0.65 { return }
        stroke.points.append(point)
        currentStroke = stroke
        let margin = stroke.glow * 3 + stroke.width + 4
        invalidate(NSRect(x: min(last.x, point.x), y: min(last.y, point.y), width: abs(last.x-point.x)+1, height: abs(last.y-point.y)+1).insetBy(dx: -margin, dy: -margin))
    }
    func finishStroke() {
        guard var stroke = currentStroke else { return }
        let now = ProcessInfo.processInfo.systemUptime
        stroke.finishedAt = now
        stroke.fadeStartsAt = prefs.autoFade ? now + prefs.fadeDelay : nil
        strokes.append(stroke)
        currentStroke = nil
        onStrokeCount?(strokes.count)
        updateVisibility()
    }
    func undo() {
        if let stroke = currentStroke { currentStroke = nil; invalidate(stroke.bounds) }
        else if let stroke = strokes.popLast() { invalidate(stroke.bounds) }
        onStrokeCount?(strokes.count); updateVisibility()
    }
    func clear() {
        currentStroke = nil; strokes.removeAll(); invalidateAll()
        onStrokeCount?(0); updateVisibility()
    }
    private func invalidate(_ globalRect: NSRect) {
        for panel in panels {
            let local = globalRect.offsetBy(dx: -panel.frame.minX, dy: -panel.frame.minY)
            panel.contentView?.setNeedsDisplay(local)
        }
    }
    private func invalidateAll() { panels.forEach { $0.contentView?.needsDisplay = true } }
    func stop() {
        timer?.invalidate(); timer = nil
        ringPanel.close(); panels.forEach { $0.close() }
        for observer in observers {
            NotificationCenter.default.removeObserver(observer)
            NSWorkspace.shared.notificationCenter.removeObserver(observer)
        }
        observers.removeAll()
    }
    deinit { stop() }
}
