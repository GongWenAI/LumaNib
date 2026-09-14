import AppKit
import SwiftUI

// Offscreen rendering of the production view. No overlay engine or global hotkeys.
@main struct LocalizationPreview {
    static func main() throws {
        _ = NSApplication.shared
        let output = CommandLine.arguments[1]
        try FileManager.default.createDirectory(atPath: output, withIntermediateDirectories: true)
        let model = AppModel()
        for page in 0..<5 {
            let view = NSHostingView(rootView: SettingsView(model: model, initialPage: page))
            view.frame = NSRect(x: 0, y: 0, width: 720, height: 660)
            let window = NSWindow(contentRect: view.frame, styleMask: [.borderless], backing: .buffered, defer: false)
            window.contentView = view
            view.layoutSubtreeIfNeeded()
            RunLoop.current.run(until: Date().addingTimeInterval(0.15))
            view.layoutSubtreeIfNeeded()
            guard let bitmap = view.bitmapImageRepForCachingDisplay(in: view.bounds) else { fatalError("No bitmap") }
            view.cacheDisplay(in: view.bounds, to: bitmap)
            guard let data = bitmap.representation(using: .png, properties: [:]) else { fatalError("No PNG") }
            try data.write(to: URL(fileURLWithPath: output + "/page-\(page).png"))
        }
        print("Rendered five production settings pages in \(AppLanguage.preferred).")
    }
}
