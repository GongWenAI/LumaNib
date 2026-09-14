import AppKit

@main struct RenderAssets {
    static func main() throws {
        _ = NSApplication.shared
        let output = CommandLine.arguments[1]
        let size = NSSize(width: 1024, height: 1024)
        let rep = NSBitmapImageRep(bitmapDataPlanes: nil, pixelsWide: 1024, pixelsHigh: 1024,
                                  bitsPerSample: 8, samplesPerPixel: 4, hasAlpha: true,
                                  isPlanar: false, colorSpaceName: .deviceRGB, bytesPerRow: 0, bitsPerPixel: 0)!
        NSGraphicsContext.saveGraphicsState()
        NSGraphicsContext.current = NSGraphicsContext(bitmapImageRep: rep)
        let background = NSBezierPath(roundedRect: NSRect(x: 32, y: 32, width: 960, height: 960), xRadius: 218, yRadius: 218)
        NSColor(srgbRed: 0.065, green: 0.08, blue: 0.115, alpha: 1).setFill(); background.fill()
        for isLeft in [true, false] {
            let path = RingGeometry.path(center: NSPoint(x: 512, y: 512), radius: 280, leftHalf: isLeft)
            glowingStroke(path, color: isLeft ? Palette.cyan.color : Palette.orange.color, width: 48, glow: 44)
        }
        NSGraphicsContext.restoreGraphicsState()
        try rep.representation(using: .png, properties: [:])!.write(to: URL(fileURLWithPath: output))
        _ = size
    }
}
