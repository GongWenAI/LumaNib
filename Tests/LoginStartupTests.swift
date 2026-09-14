import AppKit
import ServiceManagement
import Carbon

final class FakeLoginItem: LoginItemService {
    var status: SMAppService.Status = .notRegistered
    var registerCount = 0
    var unregisterCount = 0
    var nextStatus: SMAppService.Status = .enabled
    var fail = false
    func register() throws {
        registerCount += 1
        if fail { throw NSError(domain: "Test", code: 1) }
        status = nextStatus
    }
    func unregister() throws {
        unregisterCount += 1
        if fail { throw NSError(domain: "Test", code: 2) }
        status = .notRegistered
    }
}
@main struct LoginStartupTests {
    static func main() {
        var count = 0
        func check(_ value: Bool, _ label: String) {
            precondition(value, label); count += 1; print("PASS: " + label)
        }
        let fake = FakeLoginItem()
        let startup = LoginStartup(service: fake)
        check(!startup.requested && fake.registerCount == 0, "Default state does not register a login item")
        startup.setEnabled(true)
        check(startup.requested && fake.registerCount == 1, "Explicit enable registers and reads back")
        startup.setEnabled(true)
        check(fake.registerCount == 1, "Enabling again does not duplicate registration")
        startup.setEnabled(false)
        check(!startup.requested && fake.unregisterCount == 1, "Disable unregisters")
        startup.setEnabled(false)
        check(fake.unregisterCount == 1, "Disabling absent item is harmless")
        fake.nextStatus = .requiresApproval
        startup.setEnabled(true)
        check(startup.status == .requiresApproval && startup.requested, "Pending system approval is distinct from enabled")
        fake.status = .enabled; startup.refresh()
        check(startup.status == .enabled, "System changes are read back")
        fake.fail = true; startup.setEnabled(false)
        check(startup.requested && !startup.error.isEmpty, "Failed removal retains real status and reports an error")
        fake.fail = false; startup.setEnabled(false)
        fake.fail = true; startup.setEnabled(true)
        check(!startup.requested && !startup.error.isEmpty, "Failed registration stays off")
        fake.fail = false; fake.status = .notFound; startup.refresh()
        check(!startup.requested && startup.status == .notFound, "Missing service is not presented as enabled")
        check(LaunchBehavior.showsSettings(arguments: [], event: nil), "Manual launch shows settings")
        check(!LaunchBehavior.showsSettings(arguments: ["--background"], event: nil), "Background launch does not show settings")
        func event(reason: OSType, id: AEEventID = AEEventID(kAEOpenApplication)) -> NSAppleEventDescriptor {
            let result = NSAppleEventDescriptor(eventClass: AEEventClass(kCoreEventClass), eventID: id, targetDescriptor: nil, returnID: AEReturnID(kAutoGenerateReturnID), transactionID: AETransactionID(kAnyTransactionID))
            result.setParam(NSAppleEventDescriptor(enumCode: reason), forKeyword: AEKeyword(keyAEPropData))
            return result
        }
        check(!LaunchBehavior.showsSettings(arguments: [], event: event(reason: OSType(keyAELaunchedAsLogInItem))), "Login launch is silent")
        check(!LaunchBehavior.showsSettings(arguments: [], event: event(reason: OSType(keyAELaunchedAsServiceItem))), "Service launch is silent")
        check(LaunchBehavior.showsSettings(arguments: [], event: event(reason: 0)), "Ordinary open event shows settings")
        check(!LaunchBehavior.isLoginEvent(event(reason: OSType(keyAELaunchedAsLogInItem), id: AEEventID(kAEOpenDocuments))), "Other Apple events are not login launches")
        print("\(count) startup assertions passed using a fake service. Real login/reboot not exercised.")
    }
}
