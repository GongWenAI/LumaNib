import AppKit

// Test executable only: a process-local UI language, with isolated bundle preferences.
// It does not change the computer's language or the user's production settings.
UserDefaults.standard.setVolatileDomain(["AppleLanguages": ["en"]], forName: UserDefaults.argumentDomain)
let app = NSApplication.shared
app.setActivationPolicy(.accessory)
let delegate = AppDelegate()
app.delegate = delegate
app.run()
