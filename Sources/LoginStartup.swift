import AppKit
import Combine
import ServiceManagement
import Carbon

protocol LoginItemService {
    var status: SMAppService.Status { get }
    func register() throws
    func unregister() throws
}
extension SMAppService: LoginItemService {}

final class LoginStartup: ObservableObject {
    @Published private(set) var status: SMAppService.Status = .notRegistered
    @Published private(set) var error = ""
    private let service: LoginItemService
    init(service: LoginItemService = SMAppService.mainApp) {
        self.service = service
        refresh()
    }
    var requested: Bool { status == .enabled || status == .requiresApproval }
    func refresh() { status = service.status }
    func setEnabled(_ enabled: Bool) {
        error = ""
        refresh()
        do {
            if enabled && !requested { try service.register() }
            else if !enabled && requested { try service.unregister() }
            refresh()
            if enabled && !requested { error = L("未能添加登录启动项，请检查应用位置及系统设置。") }
            if !enabled && requested { error = L("未能移除登录启动项，请在系统设置中检查。") }
        } catch {
            self.error = LF("无法更改登录启动设置：{0}", error.localizedDescription)
            refresh()
        }
    }
    func openSystemSettings() { SMAppService.openSystemSettingsLoginItems() }
}

enum LaunchBehavior {
    static func isLoginEvent(_ event: NSAppleEventDescriptor?) -> Bool {
        guard let event = event, event.eventID == AEEventID(kAEOpenApplication) else { return false }
        let reason = event.paramDescriptor(forKeyword: AEKeyword(keyAEPropData))?.enumCodeValue
        return reason == OSType(keyAELaunchedAsLogInItem) || reason == OSType(keyAELaunchedAsServiceItem)
    }
    static func showsSettings(arguments: [String], event: NSAppleEventDescriptor?) -> Bool {
        !arguments.contains("--background") && !isLoginEvent(event)
    }
}
