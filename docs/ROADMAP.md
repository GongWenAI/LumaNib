# Platform plans · 平台计划

[Home](../README.md) · [中文首页](../README.zh-CN.md)

LumaNib's current product is a desktop mouse-highlighting and screen-pen app. The following are plans, not available store listings or promised release dates.

| Platform | Current state | Next milestone |
| :--- | :--- | :--- |
| macOS · Apple Silicon | 1.59 desktop build; Chinese and English UI | Evaluate Mac App Store sandbox compatibility, particularly global input and overlay behavior; prepare distribution signing and review materials |
| Windows · Intel/AMD x64 | 1.59 preview build; Windows 10/11 targets | Test on Windows 10 hardware, then validate Windows 11, scaling and recording behavior |
| Android · Google Play | Planned separate app; no Android build yet | Define touch/pointer interactions and assess Android overlay and input permissions before implementation |

For a **Mac App Store** submission, Apple requires appropriate sandboxing. The current desktop package is not a store submission build, and equivalent global mouse/keyboard behavior must be tested under those restrictions. See [Apple's Mac App Store requirements](https://developer.apple.com/app-store/review/guidelines/#hardware-compatibility).

**Google Play** distributes Android applications. The existing Mac and Windows binaries cannot serve as the Android release. The Android app will need its own implementation, signed release bundle, device testing and privacy review. See [Google Play app setup](https://support.google.com/googleplay/android-developer/answer/9859152?hl=en).

Before a store launch, product screenshots and privacy disclosures will be checked against the submitted build, and support/privacy pages must be publicly accessible. Store badges and download links will be added only when a listing actually exists.

## 简体中文

目前已有 Mac、Windows 桌面版；下列内容是开发计划，不是已经上架，也没有承诺发布日期。

- **Mac App Store：** 先验证沙盒环境下的全局输入和屏幕覆盖行为，再准备分发签名、商店资料和审核。当前临时签名的桌面包不能直接当作商店提交包。
- **Windows：** 先在 Windows 10 实机测试，再验证 Windows 11、多屏缩放与录屏效果。
- **Android / Google Play：** 计划另做 Android 版，先确定触摸、鼠标等交互方式，并评估覆盖层和输入权限。目前没有 Android 安装包。

Apple 要求 Mac App Store 应用适当启用沙盒；Google Play 的发布流程针对 Android 应用。具体依据见上方官方链接。上架前会按实际版本核对截图、隐私说明和可公开访问的支持页面，商店入口将在真实上架后添加。
