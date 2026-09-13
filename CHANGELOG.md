# LumaNib release notes · 版本记录

## 1.59 — Launch at login · 登录后自动启动

- Added **General → Launch at login** on macOS and Windows, off by default.
- Login startup runs in the menu bar or tray without opening settings. Manual launches still open settings.
- Shows registration status and errors, with a shortcut to system startup settings. Windows can update its startup path after the app folder moves.
- Chinese and English UI and guides updated; existing appearance and shortcut settings retained.

**Validation:** Mac build and 67 assertions passed; Mac login-item registration and removal were exercised on M2 / macOS 26.5. English General settings were visually checked. Windows cross-build and 79 logic/mock tests passed. Actual logout/reboot login and native Windows 10/11 startup remain unverified.

### 简体中文

- Mac 和 Windows 新增“通用 → 登录后自动启动”，默认关闭。
- 登录后在菜单栏或托盘运行，不弹设置窗口；手动打开仍显示设置。
- 显示登记状态和错误，并提供系统启动设置入口；Windows 移动目录后可更新启动路径。
- 更新中英文界面与说明，保留已有外观和快捷键设置。

**验证范围：** Mac 构建及 67 项断言通过，并在 M2 / macOS 26.5 实际开启、关闭登录项，核对英文通用页面。Windows 交叉编译和 79 项逻辑/模拟测试通过。尚未实际注销或重启验证登录启动，也未完成 Windows 10/11 实机测试。

## 1.58 — Desktop preview

**Release status:** public desktop preview on GitHub Releases; not published to an app store.

- Independent left/right ring colors, including the dim idle state; corresponding mouse clicks produce a glow.
- A fluorescent freehand pen with adjustable color, width, glow and optional timed fading.
- Toggle or hold-to-draw modes, undo, clear and Esc to stop drawing.
- Direct capture of custom global shortcut combinations.
- Automatic Chinese/English UI based on the system display language; existing preferences are retained.
- A shared cyan/orange split-ring icon, copyright attribution to 宫文 and a project link in Help.
- Mac Apple Silicon and Windows x64 packages, with Chinese and English guides. The Windows package includes its runtime.

The Mac UI has been checked on M2 / macOS 26.5. Windows has been cross-built and logic-tested; native Windows use and OBS recordings remain unverified. Long-session and broad device testing are incomplete.

### 简体中文

**状态：GitHub 公开桌面预览版，未在应用商店发布。**

左右半环独立选色，未点击时也显示对应颜色；支持荧光手绘、粗细与发光调节、自动淡出、撤销及清空。快捷键可直接录入，支持按一下切换或按住绘制，Esc 退出。界面自动跟随系统中英文，并保留已有设置。

Mac 与 Windows 采用统一蓝橙双半环图标，帮助页包含作者宫文和项目链接；下载包附中英文说明，Windows 包自带运行库。

Mac 已在 M2 / macOS 26.5 核对界面；Windows 已交叉编译与逻辑测试，实际操作和 OBS 实录仍待验证。尚未完成长时间使用和广泛设备测试。
