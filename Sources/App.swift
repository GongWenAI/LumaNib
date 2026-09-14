import AppKit
import SwiftUI
import Carbon

final class AppModel: ObservableObject {
    @Published var prefs: Preferences {
        didSet {
            if let data = try? JSONEncoder().encode(prefs) { UserDefaults.standard.set(data, forKey: "preferences.v1") }
            onPreferences?(prefs)
        }
    }
    @Published var drawing = false
    @Published var strokeCount = 0
    @Published var shortcutError = ""
    var onPreferences: ((Preferences) -> Void)?
    var onToggleDrawing: (() -> Void)?
    var onClear: (() -> Void)?
    var onUndo: (() -> Void)?
    var onApplyShortcuts: (([Shortcut]) -> String?)?
    var onCaptureShortcut: ((Bool) -> Void)?
    init() {
        var saved = (UserDefaults.standard.data(forKey: "preferences.v1").flatMap { try? JSONDecoder().decode(Preferences.self, from: $0) }) ?? Preferences()
        saved.sanitize()
        prefs = saved
    }
    func applyShortcuts(_ shortcuts: [Shortcut]) {
        if let error = onApplyShortcuts?(shortcuts) { shortcutError = error; return }
        shortcutError = ""
        prefs.shortcuts = shortcuts
    }
}

final class AppDelegate: NSObject, NSApplicationDelegate, NSMenuDelegate {
    let model = AppModel()
    var engine: OverlayEngine!
    var hotKeys: HotKeys!
    var statusItem: NSStatusItem!
    var settingsWindow: NSWindow?
    var holding = false
    var lastDrawMode: DrawMode = .toggle

    func applicationDidFinishLaunching(_ notification: Notification) {
        // A second launch opens the existing menu-bar app instead of registering keys twice.
        let peers = NSRunningApplication.runningApplications(withBundleIdentifier: Bundle.main.bundleIdentifier ?? "local.musa.glowpointer")
        if peers.contains(where: { $0.processIdentifier != ProcessInfo.processInfo.processIdentifier }) {
            NSApp.terminate(nil); return
        }
        let mainMenu = NSMenu()
        let appItem = NSMenuItem()
        let appMenu = NSMenu(title: "LumaNib")
        let quitItem = NSMenuItem(title: L("退出LumaNib"), action: #selector(quit), keyEquivalent: "q")
        quitItem.target = self
        appMenu.addItem(quitItem); appItem.submenu = appMenu; mainMenu.addItem(appItem)
        let windowItem = NSMenuItem()
        let windowMenu = NSMenu(title: L("窗口"))
        windowMenu.addItem(withTitle: L("关闭设置"), action: #selector(NSWindow.performClose(_:)), keyEquivalent: "w")
        windowItem.submenu = windowMenu; mainMenu.addItem(windowItem)
        NSApp.mainMenu = mainMenu
        engine = OverlayEngine()
        hotKeys = HotKeys()
        lastDrawMode = model.prefs.drawMode
        model.onPreferences = { [weak self] prefs in
            guard let self = self else { return }
            if self.lastDrawMode != prefs.drawMode { self.endDrawing(); self.lastDrawMode = prefs.drawMode }
            self.engine.apply(prefs)
            self.updateStatus()
        }
        model.onToggleDrawing = { [weak self] in self?.toggleDrawing() }
        model.onUndo = { [weak self] in self?.engine.undo() }
        model.onClear = { [weak self] in self?.engine.clear() }
        model.onApplyShortcuts = { [weak self] shortcuts in
            self?.endDrawing()
            return self?.hotKeys.configure(shortcuts)
        }
        model.onCaptureShortcut = { [weak self] recording in
            guard let self = self else { return }
            self.endDrawing()
            if recording { self.hotKeys.suspend() }
            else { self.model.shortcutError = self.hotKeys.resume() ?? "" }
        }
        engine.onEscape = { [weak self] in self?.endDrawing() }
        engine.onStrokeCount = { [weak self] count in self?.model.strokeCount = count }
        engine.onHoldWatchdog = { [weak self] in
            guard let self = self, self.holding else { return }
            let flags = NSEvent.modifierFlags
            let shortcut = self.model.prefs.shortcuts[1]
            if !flags.isSuperset(of: shortcut.requiredFlags) {
                self.endDrawing()
            }
        }
        hotKeys.onEvent = { [weak self] id, down in self?.handleHotKey(id, down: down) }
        model.shortcutError = hotKeys.configure(model.prefs.shortcuts) ?? ""
        engine.apply(model.prefs)
        makeStatusItem()
        if LaunchBehavior.showsSettings(arguments: CommandLine.arguments, event: NSAppleEventManager.shared().currentAppleEvent) { showSettings() }
    }

    func applicationShouldHandleReopen(_ sender: NSApplication, hasVisibleWindows flag: Bool) -> Bool {
        showSettings(); return true
    }

    private func makeStatusItem() {
        statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
        let menu = NSMenu(); menu.delegate = self; statusItem.menu = menu
        updateStatus()
    }

    func updateStatus() {
        guard statusItem != nil else { return }
        let image = NSImage(named: NSImage.Name("AppIcon"))?.copy() as? NSImage
        image?.size = NSSize(width: 18, height: 18)
        image?.isTemplate = false
        statusItem.button?.image = image
        statusItem.button?.title = model.drawing ? L(" 画笔") : ""
        statusItem.button?.toolTip = "LumaNib · " + (model.drawing ? L("正在画线，Esc 退出") : L("鼠标高亮与屏幕画笔"))
    }

    func menuWillOpen(_ menu: NSMenu) {
        menu.removeAllItems()
        let title = NSMenuItem(title: "LumaNib  1.59", action: nil, keyEquivalent: "")
        title.isEnabled = false; menu.addItem(title)
        menu.addItem(.separator())
        addMenu(menu, title: L("鼠标圆环") + "  " + model.prefs.shortcuts[0].label, action: #selector(toggleRing), checked: model.prefs.ringEnabled)
        addMenu(menu, title: (model.drawing ? L("退出画笔") : L("开始画笔")) + "  " + model.prefs.shortcuts[1].label, action: #selector(toggleDrawing))
        addMenu(menu, title: L("撤销上一笔") + "  " + model.prefs.shortcuts[2].label, action: #selector(undoStroke))
        addMenu(menu, title: L("清空笔迹") + "  " + model.prefs.shortcuts[3].label, action: #selector(clearStrokes))
        menu.addItem(.separator())
        addMenu(menu, title: L("设置与使用说明…"), action: #selector(showSettings))
        menu.addItem(.separator())
        addMenu(menu, title: L("退出LumaNib"), action: #selector(quit))
    }
    private func addMenu(_ menu: NSMenu, title: String, action: Selector, checked: Bool = false) {
        let item = NSMenuItem(title: title, action: action, keyEquivalent: "")
        item.target = self; item.state = checked ? .on : .off; menu.addItem(item)
    }
    @objc func toggleRing() { model.prefs.ringEnabled.toggle() }
    @objc func undoStroke() { engine.undo() }
    @objc func clearStrokes() { engine.clear() }
    @objc func quit() { NSApp.terminate(nil) }

    func handleHotKey(_ id: UInt32, down: Bool) {
        if id == 2 {
            if model.prefs.drawMode == .toggle {
                if down { toggleDrawing() }
            } else if down {
                holding = true; beginDrawing()
            } else { endDrawing() }
            return
        }
        guard down else { return }
        switch id {
        case 1: toggleRing()
        case 3: engine.undo()
        case 4: engine.clear()
        case 99: endDrawing()
        default: break
        }
    }
    @objc func toggleDrawing() {
        if model.drawing { endDrawing() } else { holding = false; beginDrawing() }
    }
    func beginDrawing() {
        model.drawing = true
        hotKeys.captureEscape(true)
        engine.setDrawing(true)
        updateStatus()
    }
    func endDrawing() {
        holding = false
        model.drawing = false
        engine?.setDrawing(false)
        hotKeys?.captureEscape(false)
        updateStatus()
    }

    @objc func showSettings() {
        endDrawing()
        if settingsWindow == nil {
            let window = NSWindow(contentRect: NSRect(x: 0, y: 0, width: 720, height: 660),
                                  styleMask: [.titled, .closable, .miniaturizable], backing: .buffered, defer: false)
            window.title = "LumaNib"
            window.titlebarAppearsTransparent = true
            window.backgroundColor = NSColor(srgbRed: 0.065, green: 0.08, blue: 0.11, alpha: 1)
            window.isReleasedWhenClosed = false
            window.contentView = NSHostingView(rootView: SettingsView(model: model))
            window.center()
            settingsWindow = window
        }
        NSApp.activate(ignoringOtherApps: true)
        settingsWindow?.makeKeyAndOrderFront(nil)
    }

    func applicationWillTerminate(_ notification: Notification) {
        engine?.stop(); hotKeys?.stop()
        if statusItem != nil { NSStatusBar.system.removeStatusItem(statusItem) }
    }
}
