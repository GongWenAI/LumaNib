using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms=System.Windows.Forms;

namespace LumaNib;
internal static class Program
{
    public const string Version="1.59";
    public static readonly string DataDirectory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"LumaNib");
    [STAThread]
    static int Main(string[] args)
    {
        if(!OperatingSystem.IsWindowsVersionAtLeast(10,0,17763)) { MessageBox.Show(L10n.T("LumaNib 需要 Windows 10 1809 或更新版本（64 位）。"),"LumaNib");return 1; }
        using var mutex=new Mutex(true,@"Local\LumaNib.Windows",out bool first);
        if(!first) { if(LoginStartup.IsBackground(args)) return 0; var hwnd=Native.FindWindow(null,"LumaNib.Control"); if(hwnd!=IntPtr.Zero) Native.PostMessage(hwnd,Controller.ShowMessage,IntPtr.Zero,IntPtr.Zero); return 0; }
        var app=new System.Windows.Application { ShutdownMode=ShutdownMode.OnExplicitShutdown };
        Controller? controller=null;
        app.DispatcherUnhandledException+=(_,e)=>{
            try { Directory.CreateDirectory(DataDirectory); File.WriteAllText(Path.Combine(DataDirectory,"last-error.txt"),DateTimeOffset.Now+"\nLumaNib "+Version+"\n"+e.Exception); } catch { }
            e.Handled=true; controller?.Dispose(); controller=null;
            MessageBox.Show(L10n.T("LumaNib 遇到错误，已停止屏幕效果。错误记录：\n")+Path.Combine(DataDirectory,"last-error.txt"),"LumaNib"); app.Shutdown(1);
        };
        try {
            if(args.Contains("--self-test")) { RuntimeChecks.Run(); return 0; }
            controller=new Controller(app); app.Exit+=(_,_)=>{controller?.Dispose();controller=null;};
            controller.Start(!LoginStartup.IsBackground(args)); return app.Run();
        } catch(Exception ex) {
            controller?.Dispose();
            try {Directory.CreateDirectory(DataDirectory);File.WriteAllText(Path.Combine(DataDirectory,"last-error.txt"),ex.ToString());}catch{}
            MessageBox.Show(L10n.T("LumaNib 未能启动：")+ex.Message+L10n.T("\n错误记录：")+Path.Combine(DataDirectory,"last-error.txt"),"LumaNib");return 1;
        } finally { mutex.ReleaseMutex(); }
    }
}
internal sealed class Controller : IDisposable
{
    public static readonly uint ShowMessage=Native.RegisterWindowMessage("LumaNib.ShowSettings.1");
    readonly System.Windows.Application app;
    readonly PreferenceStore store=new(Program.DataDirectory);
    readonly DispatcherTimer saveTimer;
    readonly HwndSource messages;
    readonly Forms.NotifyIcon tray;
    readonly System.Drawing.Icon trayIcon;
    SettingsWindow? settings;
    bool disposed;
    bool saveWarningShown;
    string? loadWarning;
    public Preferences Prefs { get; }
    public OverlayEngine Engine { get; }
    public HotKeys Keys { get; }
    public string ShortcutError { get; set; }="";
    public LoginStartup Startup { get; } = new(Environment.ProcessPath ?? "",new RegistryStartupStore());
    public Controller(System.Windows.Application application)
    {
        app=application; Prefs=store.Load(out loadWarning);
        var parameters=new HwndSourceParameters("LumaNib.Control") { Width=0,Height=0,WindowStyle=0,ExtendedWindowStyle=Native.WS_EX_TOOLWINDOW };
        messages=new HwndSource(parameters); messages.AddHook(Message);
        Keys=new HotKeys(messages.Handle);
        Engine=new OverlayEngine(Prefs);
        Engine.DrawingStarted+=()=>{settings?.CancelCapture();settings?.Hide();};
        Engine.Changed+=UpdateTray;
        saveTimer=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(400)};
        saveTimer.Tick+=(_,_)=>{saveTimer.Stop();Save();};
        var resource=System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/AppIcon.ico")) ?? throw new InvalidDataException(L10n.T("应用图标缺失。"));
        using(resource.Stream) using(var source=new System.Drawing.Icon(resource.Stream)) trayIcon=(System.Drawing.Icon)source.Clone();
        tray=new Forms.NotifyIcon{Icon=trayIcon,Text="LumaNib 1.59",Visible=true};
        tray.DoubleClick+=(_,_)=>ShowSettings();
        var menu=new Forms.ContextMenuStrip();menu.Opening+=(_,_)=>FillMenu(menu);tray.ContextMenuStrip=menu;
        SystemEvents.DisplaySettingsChanged+=DisplaysChanged;
        SystemEvents.SessionSwitch+=SessionSwitch;
        SystemEvents.PowerModeChanged+=PowerChanged;
    }
    public void Start(bool show)
    {
        ShortcutError=Keys.Configure(Prefs.Shortcuts)??"";
        if(show) ShowSettings();
        if(show && loadWarning is not null) MessageBox.Show(loadWarning,"LumaNib");
        if(show && ShortcutError.Length>0) MessageBox.Show(ShortcutError+L10n.T("\n请在“全局快捷键”中重新设置。"),"LumaNib");
    }
    IntPtr Message(IntPtr hwnd,int msg,IntPtr wp,IntPtr lp,ref bool handled)
    {
        if((uint)msg==ShowMessage) { app.Dispatcher.BeginInvoke(new Action(ShowSettings));handled=true; }
        if(msg==Native.WM_HOTKEY) {
            int action=Keys.ActionFor(wp.ToInt32()); handled=true;
            switch(action) {
                case 0: Prefs.RingEnabled=!Prefs.RingEnabled;PreferencesChanged();break;
                case 1: if(Prefs.HoldMode) Engine.Begin(true);else Engine.Toggle();break;
                case 2: Engine.Undo();break;
                case 3: Engine.Clear();break;
            }
        }
        return IntPtr.Zero;
    }
    public void PreferencesChanged(bool fade=false,bool mode=false) { Engine.Apply(Prefs,fade,mode);saveTimer.Stop();saveTimer.Start(); }
    void Save()
    {
        try { store.Save(Prefs);saveWarningShown=false; }
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException) {
            if(!saveWarningShown) {saveWarningShown=true;MessageBox.Show(L10n.T("设置暂时无法保存，当前效果仍可使用。\n")+ex.Message,"LumaNib");}
        }
    }
    public string? ApplyShortcuts(Shortcut[] shortcuts)
    {
        Engine.End(); string? error=Keys.Configure(shortcuts);ShortcutError=error??"";
        if(error is null) {Prefs.Shortcuts=shortcuts.ToArray();PreferencesChanged();}return error;
    }
    public void ShowSettings()
    {
        if(disposed) return;
        Engine.End(); settings??=new SettingsWindow(this);
        settings.RefreshStartup();
        settings.Show();if(settings.WindowState==WindowState.Minimized)settings.WindowState=WindowState.Normal;settings.Activate();
    }
    void UpdateTray() { if(tray is not null) tray.Text=Engine.Drawing?L10n.T("LumaNib 1.59 · 画笔使用中 · Esc 退出"):"LumaNib 1.59"; }
    void FillMenu(Forms.ContextMenuStrip menu)
    {
        foreach(Forms.ToolStripItem item in menu.Items.Cast<Forms.ToolStripItem>().ToArray()) item.Dispose();menu.Items.Clear();
        menu.Items.Add(new Forms.ToolStripMenuItem("LumaNib 1.59"){Enabled=false});menu.Items.Add(new Forms.ToolStripSeparator());
        void Item(string label,Action action,bool check=false) {var item=new Forms.ToolStripMenuItem(label){Checked=check};item.Click+=(_,_)=>action();menu.Items.Add(item);}
        Item(L10n.T("鼠标圆环    ")+Prefs.Shortcuts[0].Label,()=>{Prefs.RingEnabled=!Prefs.RingEnabled;PreferencesChanged();},Prefs.RingEnabled);
        Item((Engine.Drawing?L10n.T("退出画笔    "):L10n.T("开始画笔    "))+Prefs.Shortcuts[1].Label,()=>Engine.Toggle());
        Item(L10n.T("撤销上一笔    ")+Prefs.Shortcuts[2].Label,()=>Engine.Undo());Item(L10n.T("清空笔迹    ")+Prefs.Shortcuts[3].Label,()=>Engine.Clear());
        menu.Items.Add(new Forms.ToolStripSeparator());Item(L10n.T("设置与使用说明…"),ShowSettings);menu.Items.Add(new Forms.ToolStripSeparator());Item(L10n.T("退出 LumaNib"),()=>app.Shutdown());
    }
    void DisplaysChanged(object? sender,EventArgs e) => app.Dispatcher.BeginInvoke(new Action(()=>{if(!disposed)Engine.RebuildDisplays();}));
    void SessionSwitch(object sender,SessionSwitchEventArgs e) => app.Dispatcher.BeginInvoke(new Action(()=>{
        if(disposed)return;
        if(e.Reason is SessionSwitchReason.SessionLock or SessionSwitchReason.SessionLogoff or SessionSwitchReason.ConsoleDisconnect or SessionSwitchReason.RemoteDisconnect)Engine.HideSession(true);
        else if(e.Reason is SessionSwitchReason.SessionUnlock or SessionSwitchReason.ConsoleConnect or SessionSwitchReason.RemoteConnect) {Engine.RebuildDisplays();Engine.HideSession(false);}
    }));
    void PowerChanged(object sender,PowerModeChangedEventArgs e) => app.Dispatcher.BeginInvoke(new Action(()=>{
        if(disposed)return;
        if(e.Mode==PowerModes.Suspend)Engine.HideSession(true);else if(e.Mode==PowerModes.Resume){Engine.RebuildDisplays();Engine.HideSession(false);}
    }));
    public void Dispose()
    {
        if(disposed)return;disposed=true;
        SystemEvents.DisplaySettingsChanged-=DisplaysChanged;SystemEvents.SessionSwitch-=SessionSwitch;SystemEvents.PowerModeChanged-=PowerChanged;
        saveTimer.Stop();Save();Keys.Dispose();Engine.Dispose();
        tray.Visible=false;tray.ContextMenuStrip?.Dispose();tray.Dispose();trayIcon.Dispose();messages.Dispose();
        if(settings is not null){settings.AllowClose=true;settings.Close();}
    }
}
