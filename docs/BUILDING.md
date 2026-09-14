# Build LumaNib 1.59

[Home](../README.md) · [简体中文](#简体中文) · [License](../LICENSE)

The source for both desktop apps is available under the **LumaNib Source-Available
License — No Sale**. You may use, modify, build and share it free of charge.
Selling the original or a derivative version requires the author's written
permission. This is not MIT or an OSI-approved open-source license.

## Get the source

```sh
git clone https://github.com/GongWenAI/LumaNib.git
cd LumaNib
```

The application source corresponds to 1.59. Build helpers use repository-relative
paths. The existing `v1.59` tag predates source publication, so use `main` or the
explicit `LumaNib-1.59-source.zip` release asset to obtain the source. GitHub's
automatically generated source archives for that older tag contain only the
product pages. Published application binaries were not changed by this source
publication.

## macOS

Use an Apple Silicon Mac with macOS 14+, Xcode Command Line Tools including Swift,
and Python 3 for catalog generation and ZIP packaging. The tested toolchain was
on M2 / macOS 26.5. No third-party application libraries are needed.

```sh
python3 Localization/generate.py
zsh build.sh
zsh test.sh
# Optional distribution ZIP, containing LICENSE and COPYRIGHT.txt:
python3 package-mac.py
```

The app is `LumaNib.app`; the optional archive is in `dist/`. The build uses an
ad hoc signature, not Apple notarization. Building does not launch the app or
register a login item. The app uses the existing `local.musa.glowpointer` bundle
identifier and preferences key so updates preserve settings. Use a different
identifier for an independently branded fork or isolated UI experiment.

## Windows

Install the .NET 10 SDK for development. End users of self-contained release
packages do not need the SDK. From the repository root, in PowerShell:

```powershell
dotnet publish Windows/src/LumaNib.csproj -c Release -r win-x64 --self-contained true -p:PublishReadyToRun=false -o Windows/dist/LumaNib-1.59-win-x64
dotnet run --project Windows/tests/CoreTests.csproj -c Release
```

Cross-building on a Mac with .NET 10 and zsh is also supported:

```sh
zsh Windows/build.sh
zsh Windows/test.sh
```

The scripts use `DOTNET_ROOT` if set, then `dotnet` from PATH, then an existing
`Windows/work/tools/dotnet` installation. They do not install an SDK themselves.
NuGet restore may download the Windows targeting/runtime packs. Set
`NUGET_PACKAGES` to reuse your own cache; the zsh scripts otherwise use
`Windows/work/nuget-packages`.

Optional packaging, after publishing (Python 3 required):

```sh
python3 Windows/package.py
```

This produces `dist/LumaNib-1.59-Windows-x64.zip`, including the runtime, guides,
self-test launcher and license notices. The packager searches `NUGET_PACKAGES`,
then `Windows/work/nuget-packages`, then the user's `.nuget/packages` for the
runtime licenses. Keep the complete output folder together. The app uses only
Microsoft .NET/WPF/Windows APIs; those frameworks retain their own licenses.

## Code map and checks

| Path | Contents |
| --- | --- |
| `Sources/` | Swift application, overlay, settings, shortcuts and login startup |
| `Tests/` | macOS logic/render checks and optional developer preview harnesses |
| `Resources/` | Mac icon and original PNG |
| `Windows/src/` | C# WPF application, Windows API interop and icon |
| `Windows/tests/` | Portable logic and mock tests; no real Windows input |
| `Localization/` | Shared Chinese/English catalog and generator |

Edit `Localization/strings.json` and regenerate both compiled catalogs. Mac tests
cover 67 assertions; Windows tests cover 79. The extra Swift preview harnesses
are separate programs, not included in the main app or ordinary test script.
Tests do not log out, reboot or change the system's startup entries. Windows
logic tests do not validate native rendering, registry operations or actual
keyboard/mouse input. Native Windows 10/11 use, OBS recordings and long sessions
remain to be tested. Mac login-item registration/removal was checked separately;
actual logout/reboot startup has not been tested.

Build outputs, SDKs, caches, signing secrets, logs and local settings are excluded
from Git. Distributions must keep `LICENSE` and `COPYRIGHT.txt`, plus applicable
third-party notices. Mark modified builds as modified.

# 简体中文

源码允许免费使用、修改和免费分发，未经作者宫文书面许可，禁止销售原版或
修改版。完整条款见 [LICENSE](../LICENSE)，不是 MIT 许可。

使用上方 `git clone` 获取 `main` 分支。现有 `v1.59` 标签创建于公开源码之前，
GitHub 自动生成的该标签源码包只有当时的介绍页；完整源码请用 `main`，或
Releases 中单独提供的 `LumaNib-1.59-source.zip`。此次公开源码没有更换已有
Mac、Windows 程序包。

- **Mac：** 需要 Apple Silicon、macOS 14 以上、Xcode 命令行工具和 Python 3。
  在根目录运行上方 Mac 命令，生成 `LumaNib.app`。只做本地临时签名，不含
  Apple 公证。构建不会启动应用或启用登录项。
- **Windows：** 开发者需要 .NET 10 SDK；普通用户使用自包含下载包不需要。
  在仓库根目录运行上方 PowerShell 命令。Mac 也能用 `Windows/build.sh`
  交叉编译。首次还原可能下载微软运行库；SDK、缓存不会提交到 Git。
- **打包：** Mac 用 `package-mac.py`，Windows 用 `Windows/package.py`，
  输出在根目录 `dist/`。程序包附许可文件，Windows 还附微软运行库许可。
- **中英文：** 修改 `Localization/strings.json`，运行生成脚本，同步 Swift
  和 C# 字符串表。
- **验证范围：** 测试脚本覆盖 Mac 的 67 项断言和 Windows 的 79 项逻辑/
  模拟检查，不能代替 Windows 实机、OBS 实录或长时间测试。未做实际注销、
  重启后的自启验证。

分发时保留许可和版权声明，修改版须标注经过修改。另起名字发布修改版时，
建议改用自己的应用标识，避免与原版共用设置。
