import AppKit

@main struct OverlayTests {
    static func main() throws {
        _ = NSApplication.shared
        var assertions = 0
        func check(_ condition: @autoclosure () -> Bool, _ text: String) {
            if !condition() { fatalError("FAIL: " + text) }
            assertions += 1; print("PASS: " + text)
        }
        let engine = OverlayEngine()
        // Exercise the real stroke engine with its windows hidden, without moving the user's mouse.
        engine.sessionHidden = true
        var prefs = Preferences(); prefs.ringEnabled = false; prefs.autoFade = false
        engine.apply(prefs)
        engine.beginStroke(at: NSPoint(x: 20, y: 20))
        check(engine.currentStroke == nil, "A stroke cannot start outside drawing mode")
        engine.setDrawing(true)
        engine.beginStroke(at: NSPoint(x: 40, y: 100))
        for i in 1...180 {
            engine.appendPoint(NSPoint(x: 40 + CGFloat(i)*2, y: 100 + sin(CGFloat(i)/18)*30))
        }
        check(engine.currentStroke?.points.count == 181, "Pointer samples produce a continuous stroke")
        engine.finishStroke()
        check(engine.strokes.count == 1 && engine.currentStroke == nil, "Mouse-up commits exactly one stroke")
        engine.setDrawing(false)
        check(engine.strokes.count == 1, "Exiting drawing mode preserves ink")

        let preview = InkView(frame: NSRect(x: 0, y: 0, width: 440, height: 200))
        preview.engine = engine
        let bitmap = preview.bitmapImageRepForCachingDisplay(in: preview.bounds)!
        preview.cacheDisplay(in: preview.bounds, to: bitmap)
        try bitmap.representation(using: .png, properties: [:])!.write(to: URL(fileURLWithPath: CommandLine.arguments[1]))
        var visiblePixels = 0
        for x in stride(from: 0, to: bitmap.pixelsWide, by: 4) {
            for y in stride(from: 0, to: bitmap.pixelsHigh, by: 4) {
                if (bitmap.colorAt(x: x, y: y)?.alphaComponent ?? 0) > 0.02 { visiblePixels += 1 }
            }
        }
        check(visiblePixels > 100, "The actual InkView renders visible ink into a bitmap")
        prefs.autoFade = true; prefs.fadeDelay = 3
        engine.apply(prefs)
        check(engine.strokes[0].fadeStartsAt != nil, "Enabling fade assigns an expiry to existing ink")
        prefs.autoFade = false; engine.apply(prefs)
        check(engine.strokes[0].fadeStartsAt == nil, "Disabling fade cancels an existing expiry")
        engine.setDrawing(true)
        engine.beginStroke(at: NSPoint(x: 150, y: 30)); engine.finishStroke()
        check(engine.strokes.count == 2, "A click alone creates a visible dot stroke")
        engine.undo()
        check(engine.strokes.count == 1, "Undo removes only the most recent completed stroke")
        engine.beginStroke(at: NSPoint(x: 120, y: 30)); engine.undo()
        check(engine.currentStroke == nil && engine.strokes.count == 1, "Undo during drawing cancels only the active stroke")
        engine.clear()
        check(engine.strokes.isEmpty && engine.currentStroke == nil, "Clear removes completed and in-progress ink")
        engine.setDrawing(false); engine.stop()
        print("\(assertions) engine assertions passed. Physical mouse input is not simulated by this test.")
    }
}
