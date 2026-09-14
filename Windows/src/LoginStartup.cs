using System;
using System.IO;

namespace LumaNib;
internal interface IStartupStore
{
    string? Read();
    void Write(string command);
    void Remove();
}
internal readonly record struct StartupState(bool Registered,bool CurrentPath);
internal sealed class LoginStartup
{
    readonly IStartupStore store;
    readonly string executable;
    public LoginStartup(string executable,IStartupStore store) {
        this.executable=executable;this.store=store;
    }
    public static string BuildCommand(string executable) {
        // Run entries use a command line, not a shell. Always quote the full EXE path.
        if(string.IsNullOrWhiteSpace(executable) || executable.IndexOfAny(new[]{'"','\r','\n','\0'})>=0 || !executable.EndsWith(".exe",StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(L10n.T("无法识别程序路径，请从完整解压后的 LumaNib.exe 运行。"));
        string command="\""+executable+"\" --background";
        if(command.Length>260) throw new ArgumentException(L10n.T("程序路径过长，请移到较短的固定路径后重试。"));
        return command;
    }
    public StartupState Read() {
        string? value=store.Read();
        return new(!string.IsNullOrEmpty(value),string.Equals(value,"\""+executable+"\" --background",StringComparison.OrdinalIgnoreCase));
    }
    public StartupState SetEnabled(bool enabled) {
        if(enabled) store.Write(BuildCommand(executable));else store.Remove();
        var state=Read();
        if(enabled && !state.CurrentPath || !enabled && state.Registered)
            throw new IOException(L10n.T("系统没有保存登录启动设置，请检查后重试。"));
        return state;
    }
    public static bool IsBackground(string[] args) => Array.Exists(args,a=>a=="--background");
}
