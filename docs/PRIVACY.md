# Privacy statement · 隐私说明

**LumaNib 1.58 · Desktop apps · Updated September 14, 2026**  
Publisher / 作者：**宫文**  
[Project](https://github.com/GongWenAI/LumaNib) · [Support](../SUPPORT.md) · [简体中文](#简体中文)

## Scope

This statement describes the current LumaNib desktop apps for macOS and Windows. It does not describe a future Android release or an app-store build that has not been published. It should be reviewed when those products change.

## What happens on your computer

- **Mouse and shortcut input:** the app reads the cursor position, mouse button state and relevant shortcut events to display the ring, draw ink and respond to commands. Shortcut capture reads the combination you enter into its field. The app does not store a history of your typing.
- **Ink:** strokes are held in memory while the app runs. Clearing ink, its configured expiry or quitting removes it. There is no built-in ink export or screen recording.
- **Preferences:** appearance, drawing and shortcut settings are stored locally. Mac uses the app's user preferences; Windows uses `%LOCALAPPDATA%\LumaNib\settings.json`.
- **Diagnostics on Windows:** an application error may create a local `last-error.txt` under `%LOCALAPPDATA%\LumaNib`. If you explicitly run the self-test, it writes generated rendering images and a report under `SelfTest`. These are not screenshots of your desktop. Error logs can contain technical details and local file paths; review them before sharing.

## Network, accounts and third parties

The current app contains no account system, advertising, analytics SDK or automatic telemetry upload. It does not automatically send mouse activity, shortcuts, ink or preferences to the publisher. It does not request screen-recording or microphone input to perform its own work.

When you click the project link, your default browser opens GitHub. That visit and any information you submit there are handled by GitHub under [GitHub's privacy statement](https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement). If you record your display using OBS or another app, that recorder handles the resulting video independently of LumaNib.

## Support information and removal

You choose whether to submit a support issue or attach a screenshot/log. Reports may contain the details you provide and are visible to people with access to the repository. If the repository is made public, its issues may be public too. Do not submit passwords, private recordings or unreviewed logs. Contact the publisher through the [support instructions](../SUPPORT.md) for corrections or removal requests relating to submitted information; GitHub also provides its own account and privacy controls.

Quitting clears in-memory ink. Local preferences and diagnostics remain on your computer until removed. Uninstalling the app may leave those settings or logs behind.

---

# 简体中文

## 适用范围

本说明适用于当前 LumaNib 1.58 的 macOS、Windows 桌面版。未来 Android 版或尚未发布的商店版不在本说明范围内，发布时需按实际功能重新核对。

## 电脑上处理的数据

- **鼠标和快捷键：** 为显示圆环、绘制笔迹和响应命令，软件读取鼠标位置、按键状态和相关快捷键事件；录入快捷键时读取你在输入框中按下的组合，不保存日常打字历史。
- **屏幕笔迹：** 保存在软件运行内存中，清空、设定的淡出结束或退出软件后移除。软件没有笔迹导出或录屏功能。
- **偏好设置：** 外观、绘制和快捷键设置在本机保存。Mac 使用应用用户偏好设置；Windows 使用 `%LOCALAPPDATA%\LumaNib\settings.json`。
- **Windows 诊断文件：** 出错时可能在 `%LOCALAPPDATA%\LumaNib` 保存 `last-error.txt`。主动运行自检时，会在 `SelfTest` 下生成渲染测试图和报告，测试图不是桌面截图。错误日志可能包含技术信息和本机路径，请先检查再分享。

## 联网与第三方

当前软件没有内置账号系统、广告、统计分析 SDK 或自动上传诊断信息的服务，不会自动向作者发送鼠标活动、快捷键、笔迹或偏好设置，也不为自身功能请求录屏或麦克风输入。

点击项目链接时，默认浏览器会访问 GitHub，该访问及你主动提交的信息适用 [GitHub 隐私声明](https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement)。使用 OBS 等软件录制显示器时，视频由对应录屏软件处理。

## 反馈与删除

是否提交反馈、截图或日志，由你自行决定。提交内容对拥有仓库访问权限的人可见；仓库若改为公开，Issue 也可能公开。不要提交密码、私人录屏或未检查的日志。涉及已提交信息的更正、删除请求，可通过 [使用支持](../SUPPORT.md#简体中文) 联系作者，也可使用 GitHub 提供的账号与隐私管理功能。

退出软件会清除内存中的笔迹。本地设置和诊断文件会保留到手动删除，卸载程序不一定同时删除这些文件。
