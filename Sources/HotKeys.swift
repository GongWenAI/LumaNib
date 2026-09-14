import AppKit
import Carbon

final class HotKeys {
    private var refs: [UInt32: EventHotKeyRef] = [:]
    private var handler: EventHandlerRef?
    var onEvent: ((UInt32, Bool) -> Void)?
    private(set) var current: [Shortcut] = []
    private var pressed = Set<UInt32>()
    static let signature: OSType = 0x474C4F57 // GLOW

    init() {
        var types = [EventTypeSpec(eventClass: OSType(kEventClassKeyboard), eventKind: UInt32(kEventHotKeyPressed)),
                     EventTypeSpec(eventClass: OSType(kEventClassKeyboard), eventKind: UInt32(kEventHotKeyReleased))]
        InstallEventHandler(GetApplicationEventTarget(), { _, event, pointer -> OSStatus in
            guard let event = event, let pointer = pointer else { return OSStatus(eventNotHandledErr) }
            var hotKey = EventHotKeyID()
            let result = GetEventParameter(event, EventParamName(kEventParamDirectObject), EventParamType(typeEventHotKeyID), nil,
                                           MemoryLayout<EventHotKeyID>.size, nil, &hotKey)
            guard result == noErr, hotKey.signature == HotKeys.signature else { return OSStatus(eventNotHandledErr) }
            let owner = Unmanaged<HotKeys>.fromOpaque(pointer).takeUnretainedValue()
            let down = GetEventKind(event) == UInt32(kEventHotKeyPressed)
            if down {
                if owner.pressed.contains(hotKey.id) { return noErr }
                owner.pressed.insert(hotKey.id)
            } else { owner.pressed.remove(hotKey.id) }
            owner.onEvent?(hotKey.id, down)
            return noErr
        }, types.count, &types, Unmanaged.passUnretained(self).toOpaque(), &handler)
    }

    private func register(id: UInt32, code: UInt32, modifiers: UInt32) -> Bool {
        var ref: EventHotKeyRef?
        let result = RegisterEventHotKey(code, modifiers, EventHotKeyID(signature: Self.signature, id: id),
                                        GetApplicationEventTarget(), 0, &ref)
        if result == noErr, let ref = ref { refs[id] = ref; return true }
        return false
    }

    private func clearMain() {
        for id in Array(refs.keys) where id != 99 {
            if let ref = refs.removeValue(forKey: id) { UnregisterEventHotKey(ref) }
        }
        pressed.removeAll()
    }

    func configure(_ shortcuts: [Shortcut]) -> String? {
        guard shortcuts.count == 4, Set(shortcuts.map(\.identity)).count == 4 else { return L("四个快捷键不能重复。") }
        guard shortcuts.allSatisfy(\.isValid) else { return L("快捷键需包含 Control、Option 或 Command，Esc 留作退出画笔。") }
        let old = current
        clearMain()
        for (index, shortcut) in shortcuts.enumerated() {
            if !register(id: UInt32(index+1), code: shortcut.keyCode, modifiers: shortcut.carbonModifiers) {
                clearMain()
                for (i, previous) in old.enumerated() { _ = register(id: UInt32(i+1), code: previous.keyCode, modifiers: previous.carbonModifiers) }
                return LF("{0} 被其他程序占用，请换一个组合。", shortcut.label)
            }
        }
        current = shortcuts
        return nil
    }

    func suspend() { clearMain() }
    func resume() -> String? { configure(current) }

    func captureEscape(_ enabled: Bool) {
        if enabled && refs[99] == nil { _ = register(id: 99, code: 53, modifiers: 0) }
        if !enabled, let ref = refs.removeValue(forKey: 99) { UnregisterEventHotKey(ref) }
    }

    func stop() {
        for ref in refs.values { UnregisterEventHotKey(ref) }
        refs.removeAll()
        if let handler = handler { RemoveEventHandler(handler); self.handler = nil }
    }
    deinit { stop() }
}
