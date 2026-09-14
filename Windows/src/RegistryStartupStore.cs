using System.IO;
using Microsoft.Win32;
namespace LumaNib;
internal sealed class RegistryStartupStore : IStartupStore
{
    const string KeyPath=@"Software\Microsoft\Windows\CurrentVersion\Run";
    const string ValueName="LumaNib";
    public string? Read() {
        using var key=Registry.CurrentUser.OpenSubKey(KeyPath);
        return key?.GetValue(ValueName) as string;
    }
    public void Write(string command) {
        using var key=Registry.CurrentUser.CreateSubKey(KeyPath,true) ?? throw new IOException(L10n.T("无法打开当前用户的登录启动设置。"));
        key.SetValue(ValueName,command,RegistryValueKind.String);
    }
    public void Remove() {
        using var key=Registry.CurrentUser.OpenSubKey(KeyPath,true);
        key?.DeleteValue(ValueName,false);
    }
}
