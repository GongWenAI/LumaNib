import AppKit
import SwiftUI

private let accent = Color(red: 0.2, green: 0.85, blue: 0.95)

struct RingPreview: NSViewRepresentable {
    var prefs: Preferences
    var left: Bool
    var right: Bool
    func makeNSView(context: Context) -> RingView { RingView(frame: NSRect(x: 0, y: 0, width: 180, height: 150)) }
    func updateNSView(_ view: RingView, context: Context) {
        view.prefs = prefs; view.leftDown = left; view.rightDown = right; view.needsDisplay = true
    }
}

final class PenPreviewView: NSView {
    var prefs = Preferences()
    override var isOpaque: Bool { false }
    override func draw(_ dirtyRect: NSRect) {
        NSColor.clear.setFill(); dirtyRect.fill(using: .copy)
        let points = (0...180).map { i -> NSPoint in
            let t = Double(i)/180
            return NSPoint(x: 32 + t * (bounds.width-64), y: bounds.midY + sin(t * .pi * 3.6) * 19)
        }
        drawStroke(Stroke(points: points, color: prefs.penColor, width: prefs.penWidth, glow: prefs.penGlow), origin: .zero, opacity: 1)
    }
}
struct PenPreview: NSViewRepresentable {
    var prefs: Preferences
    func makeNSView(context: Context) -> PenPreviewView { PenPreviewView() }
    func updateNSView(_ view: PenPreviewView, context: Context) { view.prefs = prefs; view.needsDisplay = true }
}

struct SettingsView: View {
    @ObservedObject var model: AppModel
    @State private var page = 0
    @StateObject private var loginStartup = LoginStartup()
    @State private var previewLeft = false
    @State private var previewRight = false
    @State private var shortcutDraft: [Shortcut] = []
    @State private var shortcutSaved = false

    init(model: AppModel, initialPage: Int = 0) {
        self.model = model
        _page = State(initialValue: initialPage)
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            HStack(spacing: 14) {
                Image(nsImage: NSImage(named: NSImage.Name("AppIcon")) ?? NSImage())
                    .resizable()
                    .interpolation(.high)
                    .scaledToFit()
                    .frame(width: 56, height: 56)
                    .accessibilityLabel("LumaNib")
                VStack(alignment: .leading, spacing: 4) {
                    Text("LumaNib").font(.system(size: 25, weight: .semibold))
                    Text(L("让每一次指向，都清楚可见。"))
                        .font(.system(size: 12)).foregroundStyle(.secondary)
                }
                Spacer()
                HStack(spacing: 6) {
                    Circle().fill(model.drawing ? Color.orange : accent).frame(width: 6, height: 6)
                    Text(model.drawing ? L("画笔使用中") : L("菜单栏运行中")).font(.system(size: 11))
                }.padding(.horizontal, 11).padding(.vertical, 7)
                    .background(Color.white.opacity(0.05), in: Capsule())
            }.padding(.horizontal, 28).padding(.top, 20).padding(.bottom, 22)

            Picker(L("设置分类"), selection: $page) {
                Text(L("鼠标圆环")).tag(0)
                Text(L("荧光画笔")).tag(1)
                Text(L("全局快捷键")).tag(2)
                Text(L("通用")).tag(3)
                Text(L("使用说明")).tag(4)
            }.pickerStyle(.segmented).labelsHidden().padding(.horizontal, 28).padding(.bottom, 20)

            ScrollView {
                VStack(alignment: .leading, spacing: 18) {
                    if page == 0 { ringPage }
                    else if page == 1 { penPage }
                    else if page == 2 { shortcutPage }
                    else if page == 3 { generalPage }
                    else { helpPage }
                }.padding(.horizontal, 28).padding(.bottom, 20)
            }
            Divider().opacity(0.5)
            HStack(spacing: 12) {
                Text(LF("{0} 笔标注", model.strokeCount)).font(.system(size: 11)).foregroundStyle(.secondary)
                Spacer()
                Button(L("撤销")) { model.onUndo?() }.disabled(model.strokeCount == 0)
                Button(L("清空笔迹")) { model.onClear?() }.disabled(model.strokeCount == 0)
                Button(model.drawing ? L("退出画笔 · Esc") : L("开始画笔")) { model.onToggleDrawing?() }
                    .buttonStyle(.borderedProminent).tint(accent).foregroundStyle(.black)
            }.padding(.horizontal, 28).padding(.vertical, 16)
        }
        .frame(width: 720, height: 660)
        .background(Color(red: 0.065, green: 0.08, blue: 0.11))
        .preferredColorScheme(.dark)
        .onAppear { shortcutDraft = model.prefs.shortcuts }
    }

    private var ringPage: some View {
        VStack(alignment: .leading, spacing: 16) {
            HStack(spacing: 16) {
                ZStack {
                    RoundedRectangle(cornerRadius: 16).fill(Color.black.opacity(0.20))
                    RingPreview(prefs: model.prefs, left: previewLeft, right: previewRight)
                    Image(systemName: "cursorarrow").font(.system(size: 18)).offset(x: 5, y: 8)
                }.frame(width: 190, height: 146)
                VStack(alignment: .leading, spacing: 12) {
                    Text(L("双半环 · 跟随鼠标")).font(.system(size: 16, weight: .semibold))
                    Text(L("左键点亮左半环，右键点亮右半环。\n按住时持续发光，松开后轻轻淡出。"))
                        .font(.system(size: 12)).foregroundStyle(.secondary).lineSpacing(4)
                    HStack {
                        Button(L("预览左键")) { flashPreview(left: true) }
                        Button(L("预览右键")) { flashPreview(left: false) }
                    }
                }
                Spacer()
            }
            card {
                Toggle(L("显示跟随圆环"), isOn: $model.prefs.ringEnabled).toggleStyle(.switch).tint(accent)
                Divider()
                valueSlider(L("圆环大小"), value: $model.prefs.ringRadius, range: 12...42, unit: "pt", multiplier: 2)
                valueSlider(L("圆环粗细"), value: $model.prefs.ringWidth, range: 2...6, unit: "pt")
                colorRow(L("左半环颜色"), selection: $model.prefs.ringColor)
                colorRow(L("右半环颜色"), selection: $model.prefs.rightColor)
            }
            note(L("画笔模式会暂时隐藏圆环；退出画笔后自动恢复。"))
        }
    }

    private var penPage: some View {
        VStack(alignment: .leading, spacing: 16) {
            PenPreview(prefs: model.prefs).frame(height: 90)
                .background(Color.black.opacity(0.2), in: RoundedRectangle(cornerRadius: 16))
            card {
                colorRow(L("笔迹颜色"), selection: $model.prefs.penColor)
                valueSlider(L("画笔粗细"), value: $model.prefs.penWidth, range: 2...14, unit: "pt")
                valueSlider(L("发光强度"), value: $model.prefs.penGlow, range: 0...24, unit: "")
                Divider()
                HStack {
                    Text(L("画笔操作")).frame(width: 120, alignment: .leading)
                    Picker(L("画笔操作"), selection: $model.prefs.drawMode) {
                        ForEach(DrawMode.allCases) { Text($0.displayName).tag($0) }
                    }.labelsHidden().pickerStyle(.segmented)
                }
                Text(model.prefs.drawMode == .toggle ? LF("按 {0} 开始画，按 Esc 或再按一次快捷键退出。", model.prefs.shortcuts[1].label) : LF("按住 {0}，再用鼠标左键拖动画线；松开快捷键退出。", model.prefs.shortcuts[1].label))
                    .font(.system(size: 11)).foregroundStyle(.secondary)
                Divider()
                Toggle(L("笔迹自动淡出"), isOn: $model.prefs.autoFade).toggleStyle(.switch).tint(accent)
                if model.prefs.autoFade {
                    valueSlider(L("保留时间"), value: $model.prefs.fadeDelay, range: 1...30, unit: L("秒"))
                } else {
                    Text(L("笔迹保留到手动清空，退出画笔后仍可正常点击下面的软件。"))
                        .font(.system(size: 11)).foregroundStyle(.secondary)
                }
            }
        }
    }

    private var shortcutPage: some View {
        VStack(alignment: .leading, spacing: 16) {
            Text(L("在其他软件中也能直接使用")).font(.system(size: 16, weight: .semibold))
            Text(L("点一下输入框，直接按你想要的组合键，再点“应用快捷键”。"))
                .font(.system(size: 12)).foregroundStyle(.secondary)
            card {
                if shortcutDraft.count == 4 {
                    ForEach(0..<4, id: \.self) { index in
                        HStack {
                            Text([L("开关鼠标圆环"), L("画笔模式"), L("撤销上一笔"), L("清空全部笔迹")][index])
                            Spacer()
                            ShortcutRecorder(shortcut: $shortcutDraft[index]) { recording in
                                shortcutSaved = false
                                model.onCaptureShortcut?(recording)
                            }.frame(width: 220, height: 38)
                        }
                        if index < 3 { Divider() }
                    }
                }
                Divider()
                HStack {
                    Text(L("退出画笔")); Spacer(); Text("Esc").foregroundStyle(.secondary)
                }
            }
            HStack {
                Button(L("恢复默认组合")) { shortcutDraft = Preferences().shortcuts; shortcutSaved = false }
                Spacer()
                Button(L("应用快捷键")) {
                    model.applyShortcuts(shortcutDraft)
                    shortcutSaved = model.shortcutError.isEmpty
                }.buttonStyle(.borderedProminent).tint(accent).foregroundStyle(.black)
            }
            if !model.shortcutError.isEmpty {
                Text(model.shortcutError).font(.system(size: 12)).foregroundStyle(.orange)
            } else if shortcutSaved {
                Text(L("快捷键已应用。")).font(.system(size: 12)).foregroundStyle(accent)
            }
            note(L("组合中至少包含 ⌃ Control、⌥ Option 或 ⌘ Command，可搭配字母、数字、方向键等。系统保留的组合可能无法录入。\nEsc 用于取消录入和退出画笔，平时不影响其他软件。"))
        }
    }

    private var generalPage: some View {
        VStack(alignment: .leading, spacing: 16) {
            Text(L("启动设置")).font(.system(size: 16, weight: .semibold))
            card {
                Toggle(L("登录后自动启动"), isOn: Binding(get: { loginStartup.requested }, set: { loginStartup.setEnabled($0) }))
                    .toggleStyle(.switch).tint(accent)
                Text(L("默认关闭。开启后，登录电脑时自动在菜单栏运行，不弹出设置窗口。"))
                    .font(.system(size: 12)).foregroundStyle(.secondary)
                if loginStartup.status == .requiresApproval {
                    Text(L("已登记，等待系统允许。请在“登录项”中开启 LumaNib。"))
                        .font(.system(size: 12)).foregroundStyle(.orange)
                } else if loginStartup.status == .notFound {
                    Text(L("尚未添加登录启动项，开启开关后由系统登记。"))
                        .font(.system(size: 12)).foregroundStyle(.secondary)
                } else {
                    Text(loginStartup.requested ? L("已启用登录启动。") : L("登录启动已关闭。"))
                        .font(.system(size: 12)).foregroundStyle(.secondary)
                }
                if !loginStartup.error.isEmpty {
                    Text(loginStartup.error).font(.system(size: 12)).foregroundStyle(.orange)
                }
                Button(L("打开系统登录项设置")) { loginStartup.openSystemSettings() }
            }
            note(L("建议先把应用放到“应用程序”文件夹，再开启自启。若放在外置磁盘，登录时磁盘必须已连接并可访问。"))
        }
        .onAppear { loginStartup.refresh() }
        .onReceive(NotificationCenter.default.publisher(for: NSApplication.didBecomeActiveNotification)) { _ in loginStartup.refresh() }
    }

    private var helpPage: some View {
        VStack(alignment: .leading, spacing: 16) {
            card {
                helpRow("1", L("圆环跟随，左右键分别发光"), L("打开程序后即可使用。到“鼠标圆环”调整颜色和大小。"))
                Divider()
                helpRow("2", L("按快捷键，左键拖动画线"), LF("按 {0} 进入画笔，Esc 退出。退出后笔迹保留，但不会挡住鼠标点击。", model.prefs.shortcuts[1].label))
                Divider()
                helpRow("3", L("OBS 捕获整个显示器"), L("选择“macOS 屏幕采集”中的显示器捕获。单窗口捕获可能遗漏圆环和笔迹。先录 10 秒确认效果。"))
                Divider()
                helpRow("4", L("关闭设置，继续在菜单栏运行"), L("菜单栏的鼠标图标可以重新打开设置或退出程序。退出程序会清除所有屏幕笔迹。"))
            }
            note(L("圆环和笔迹会直接录进视频。程序在本机运行，不主动联网，不保存屏幕、按键内容或笔迹；外观与快捷键设置保存在本机，开启登录启动时会向系统登记启动项。点击项目地址时会在浏览器中打开 GitHub。"))
            Text("LumaNib 1.59 · Apple Silicon · macOS 14+")
                .font(.system(size: 11)).foregroundStyle(.tertiary)
            Text(L("© 2026 宫文 · 保留所有权利"))
                .font(.system(size: 11)).foregroundStyle(.secondary)
            Link(L("项目地址：github.com/GongWenAI/LumaNib"), destination: URL(string: "https://github.com/GongWenAI/LumaNib")!)
                .font(.system(size: 11)).tint(accent)
        }
    }

    private func flashPreview(left: Bool) {
        if left { previewLeft = true } else { previewRight = true }
        DispatchQueue.main.asyncAfter(deadline: .now()+0.45) {
            if left { previewLeft = false } else { previewRight = false }
        }
    }
    private func card<Content: View>(@ViewBuilder content: () -> Content) -> some View {
        VStack(alignment: .leading, spacing: 14, content: content)
            .font(.system(size: 12)).padding(18).frame(maxWidth: .infinity, alignment: .leading)
            .background(Color.white.opacity(0.04), in: RoundedRectangle(cornerRadius: 14))
            .overlay(RoundedRectangle(cornerRadius: 14).stroke(Color.white.opacity(0.055), lineWidth: 1))
    }
    private func valueSlider(_ title: String, value: Binding<Double>, range: ClosedRange<Double>, unit: String, multiplier: Double = 1) -> some View {
        HStack(spacing: 14) {
            Text(title).frame(width: 120, alignment: .leading)
            Slider(value: value, in: range, step: 1).tint(accent)
            Text("\(Int(value.wrappedValue * multiplier)) \(unit)")
                .monospacedDigit().foregroundStyle(.secondary).frame(width: 52, alignment: .trailing)
        }
    }
    private func colorRow(_ title: String, selection: Binding<Palette>) -> some View {
        HStack(spacing: 12) {
            Text(title).frame(width: 120, alignment: .leading)
            ForEach(Palette.allCases) { palette in
                Button { selection.wrappedValue = palette } label: {
                    Circle().fill(Color(nsColor: palette.color)).frame(width: 22, height: 22)
                        .overlay(Circle().stroke(.white, lineWidth: selection.wrappedValue == palette ? 2 : 0).padding(-3))
                        .frame(width: 28, height: 28)
                }.buttonStyle(.plain).help(palette.displayName).accessibilityLabel(title + palette.displayName)
            }
            Spacer()
            Text(selection.wrappedValue.displayName).font(.system(size: 11)).foregroundStyle(.secondary)
        }
    }
    private func note(_ text: String) -> some View {
        HStack(alignment: .top, spacing: 8) {
            Image(systemName: "info.circle").foregroundStyle(accent.opacity(0.8))
            Text(text).foregroundStyle(.secondary).lineSpacing(4)
        }.font(.system(size: 11)).fixedSize(horizontal: false, vertical: true)
    }
    private func helpRow(_ number: String, _ title: String, _ body: String) -> some View {
        HStack(alignment: .top, spacing: 12) {
            Text(number).font(.system(size: 11, weight: .semibold)).foregroundStyle(accent)
                .frame(width: 24, height: 24).background(accent.opacity(0.1), in: Circle())
            VStack(alignment: .leading, spacing: 5) {
                Text(title).fontWeight(.medium)
                Text(body).font(.system(size: 11)).foregroundStyle(.secondary).lineSpacing(3)
            }
        }
    }
}
