using System;
using System.Collections.Generic;
using System.Linq;

namespace LumaNib;
internal interface IHotKeyBackend
{
    bool Register(IntPtr handle,int id,uint modifiers,uint key);
    void Unregister(IntPtr handle,int id);
}
internal sealed class Win32HotKeyBackend : IHotKeyBackend
{
    public bool Register(IntPtr handle,int id,uint modifiers,uint key) => Native.RegisterHotKey(handle,id,modifiers,key);
    public void Unregister(IntPtr handle,int id) => Native.UnregisterHotKey(handle,id);
}
internal sealed class HotKeys : IDisposable
{
    readonly IntPtr handle;
    readonly IHotKeyBackend backend;
    Dictionary<int,Shortcut> active=new();
    Shortcut[] configured=Array.Empty<Shortcut>();
    int nextId=100;
    bool suspended;
    public HotKeys(IntPtr handle,IHotKeyBackend? backend=null) { this.handle=handle;this.backend=backend??new Win32HotKeyBackend(); }
    public int ActionFor(int id) => !suspended && active.TryGetValue(id,out var shortcut) ? Array.IndexOf(configured,shortcut) : -1;
    public string? Configure(Shortcut[] values)
    {
        if(!Preferences.ValidShortcuts(values)) return L10n.T("快捷键不能重复；至少包含 Ctrl、Alt 或 Win。Esc 和 F12 为保留键。");
        var staged=new Dictionary<int,Shortcut>();
        // Keep existing registrations until every new combination has succeeded.
        foreach(var shortcut in values) {
            var previous=active.FirstOrDefault(kv=>kv.Value==shortcut);
            if(previous.Key!=0) { staged.Add(previous.Key,shortcut); continue; }
            int id=nextId++; if(nextId>0xBFFE) nextId=100;
            if(!backend.Register(handle,id,shortcut.Modifiers|0x4000,(uint)shortcut.Key)) {
                foreach(int added in staged.Keys.Where(k=>!active.ContainsKey(k))) backend.Unregister(handle,added);
                return shortcut.Label+L10n.T(" 无法注册，可能被系统或其他软件占用。原快捷键保持不变。");
            }
            staged.Add(id,shortcut);
        }
        foreach(int old in active.Keys.Where(k=>!staged.ContainsKey(k))) backend.Unregister(handle,old);
        active=staged; configured=values.ToArray(); suspended=false; return null;
    }
    public void Suspend() { if(suspended) return; foreach(int id in active.Keys) backend.Unregister(handle,id); active.Clear(); suspended=true; }
    public string? Resume()
    {
        if(!suspended) return null;
        var error=Configure(configured.Length==4?configured:Preferences.DefaultShortcuts());
        return error is null ? null : L10n.T("快捷键恢复失败，可能在录入期间被其他软件占用。请重新应用可用的组合。");
    }
    public void Dispose() { foreach(int id in active.Keys) backend.Unregister(handle,id); active.Clear(); }
}
