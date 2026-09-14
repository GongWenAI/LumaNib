using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace LumaNib;
internal sealed class OverlayWindow : Window
{
    public IntPtr Handle { get; private set; }
    public OverlayWindow(FrameworkElement content)
    {
        WindowStyle=WindowStyle.None; AllowsTransparency=true; Background=Brushes.Transparent;
        ResizeMode=ResizeMode.NoResize; ShowActivated=false; ShowInTaskbar=false; Topmost=true;
        Width=180; Height=180; Content=content; IsHitTestVisible=false;
        SourceInitialized+=(_,_)=>{
            Handle=new WindowInteropHelper(this).Handle;
            long style=Native.GetWindowLongPtr(Handle,-20).ToInt64();
            Native.SetWindowLongPtr(Handle,-20,new IntPtr(style|Native.WS_EX_TOOLWINDOW|Native.WS_EX_TRANSPARENT|Native.WS_EX_NOACTIVATE));
        };
    }
    public void Place(int x,int y,int w,int h) => Native.SetWindowPos(Handle,Native.Topmost,x,y,w,h,0x0010);
    public void Raise() { if(IsVisible) Native.SetWindowPos(Handle,Native.Topmost,0,0,0,0,0x0010|0x0001|0x0002); }
}
internal sealed class OverlayEngine : IDisposable
{
    readonly InputCapture input;
    readonly DispatcherTimer timer;
    readonly Stopwatch clock=Stopwatch.StartNew();
    readonly RingVisual ring=new();
    readonly OverlayWindow pointer;
    readonly List<(OverlayWindow window,InkVisual visual)> screens=new();
    public readonly InkModel Ink=new();
    public Preferences Prefs { get; private set; }
    public bool Drawing { get; private set; }
    public bool Holding { get; private set; }
    public bool SessionHidden { get; private set; }
    public event Action? Changed;
    public event Action? DrawingStarted;
    bool previousLeft,previousRight;
    double leftRelease=double.NegativeInfinity,rightRelease=double.NegativeInfinity;
    double lastRaise;
    Native.POINT lastPosition=new(){X=int.MinValue,Y=int.MinValue};
    IntPtr lastMonitor;
    bool dirty=true;
    public OverlayEngine(Preferences prefs)
    {
        Prefs=prefs; ring.Prefs=prefs; pointer=new OverlayWindow(ring); pointer.Show(); pointer.Hide();
        input=new InputCapture();
        RebuildDisplays();
        timer=new DispatcherTimer(DispatcherPriority.Render){Interval=TimeSpan.FromMilliseconds(16)};
        timer.Tick+=Tick; timer.Start();
    }
    public void Apply(Preferences p,bool fadeChanged=false,bool modeChanged=false)
    {
        if(modeChanged) End();
        Prefs=p; ring.Prefs=p; ring.InvalidateVisual();
        if(fadeChanged) Ink.UpdateFade(p,clock.Elapsed.TotalSeconds);
        dirty=true; Visibility(); Changed?.Invoke();
    }
    public void RebuildDisplays()
    {
        End();
        foreach(var panel in screens) panel.window.Close(); screens.Clear();
        foreach(var monitor in Native.Monitors()) {
            var visual=new InkVisual(monitor); var window=new OverlayWindow(visual);
            window.Show(); window.Place(monitor.X,monitor.Y,monitor.Width,monitor.Height); window.Hide(); screens.Add((window,visual));
        }
        lastMonitor=IntPtr.Zero; lastPosition.X=int.MinValue; dirty=true; Visibility();
    }
    public void Begin(bool hold=false)
    {
        if(SessionHidden||Drawing) return;
        DrawingStarted?.Invoke();
        Holding=hold; Drawing=true; input.Capturing=true; Visibility(); Changed?.Invoke();
    }
    public void End()
    {
        if(!Drawing) return;
        // Drain points already delivered before invalidating this capture epoch.
        Drain(false);
        input.Capturing=false; Ink.Finish(Prefs,clock.Elapsed.TotalSeconds);
        Drawing=false; Holding=false; dirty=true; Visibility(); Changed?.Invoke();
    }
    public void Toggle() { if(Drawing) End(); else Begin(); }
    public void Undo() { Drain(false); Ink.Undo(); dirty=true; Visibility(); Changed?.Invoke(); }
    public void Clear() { Drain(false); Ink.Clear(); dirty=true; Visibility(); Changed?.Invoke(); }
    public void HideSession(bool hidden) { End(); SessionHidden=hidden; Visibility(); }
    void Drain(bool honorEscape)
    {
        bool escape=false;
        while(input.TryRead(out var e)) {
            if(e.Epoch!=input.Epoch||!Drawing) continue;
            switch(e.Kind) {
                case 1:
                    Ink.Finish(Prefs,clock.Elapsed.TotalSeconds);
                    var handle=Native.MonitorFromPoint(new Native.POINT{X=(int)e.Point.X,Y=(int)e.Point.Y},2);
                    Ink.Begin(e.Point,Prefs,Native.Dpi(handle)); dirty=true; break;
                case 0: Ink.Append(e.Point); dirty=true; break;
                case 2: Ink.Append(e.Point); Ink.Finish(Prefs,clock.Elapsed.TotalSeconds); dirty=true; Changed?.Invoke(); break;
                case 3: escape=true; break;
            }
        }
        if(escape&&honorEscape) End();
    }
    void Visibility()
    {
        bool showInk=!SessionHidden&&Ink.Count>0;
        foreach(var panel in screens) {
            if(showInk) { if(!panel.window.IsVisible) panel.window.Show(); }
            else panel.window.Hide();
        }
        if(!SessionHidden&&Prefs.RingEnabled&&!Drawing) {
            if(!pointer.IsVisible) { pointer.Show(); lastPosition.X=int.MinValue; }
        } else pointer.Hide();
    }
    void Tick(object? sender,EventArgs args)
    {
        input.Beat();
        if(SessionHidden) return;
        Drain(true);
        if(Holding&&!Native.Held(Prefs.Shortcuts[1])) End();
        double now=clock.Elapsed.TotalSeconds;
        if(Prefs.RingEnabled&&!Drawing&&Native.GetCursorPos(out var p)) {
            IntPtr monitor=Native.MonitorFromPoint(p,2);
            if(p.X!=lastPosition.X||p.Y!=lastPosition.Y||monitor!=lastMonitor) {
                double dpi=Native.Dpi(monitor); int size=(int)Math.Round(180*dpi);
                pointer.Place(p.X-size/2,p.Y-size/2,size,size); lastPosition=p; lastMonitor=monitor;
            }
            bool left=Native.Down(1),right=Native.Down(2);
            if(previousLeft&&!left) leftRelease=now;
            if(previousRight&&!right) rightRelease=now;
            double l=left?1:Math.Max(0,1-(now-leftRelease)/0.16),r=right?1:Math.Max(0,1-(now-rightRelease)/0.16);
            if(l!=ring.LeftLight||r!=ring.RightLight) { ring.LeftLight=l; ring.RightLight=r; ring.InvalidateVisual(); }
            previousLeft=left; previousRight=right;
        }
        if(Prefs.AutoFade&&Ink.Expire(now)) { dirty=true; Changed?.Invoke(); }
        bool fading=Prefs.AutoFade&&Ink.Strokes.Any(s=>s.FadeStartsAt<=now);
        if(dirty||fading) { foreach(var panel in screens) panel.visual.Sync(Ink,now); dirty=false; Visibility(); }
        if(now-lastRaise>1) { foreach(var panel in screens) panel.window.Raise(); pointer.Raise(); lastRaise=now; }
    }
    public void Dispose() { timer.Stop(); input.Dispose(); pointer.Close(); foreach(var panel in screens) panel.window.Close(); }
}
