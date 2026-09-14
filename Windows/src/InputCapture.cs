using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace LumaNib;
internal readonly record struct PointerEvent(int Kind, ScreenPoint Point, long Epoch);

// Hooks run on their own message-pump thread. Never render, write settings, or wait in a callback.
internal sealed class InputCapture : IDisposable
{
    readonly Thread thread;
    readonly ManualResetEventSlim ready=new(false);
    readonly ConcurrentQueue<PointerEvent> events=new();
    readonly Native.HookProc mouseProc,keyProc;
    IntPtr mouseHook,keyHook;
    uint threadId;
    volatile bool capturing;
    bool swallowedLeft,swallowedRight,swallowedMiddle,swallowedEscape;
    uint swallowedX;
    long epoch;
    long heartbeat=Environment.TickCount64;
    int moveCount;
    Exception? startError;
    public bool Capturing { get=>capturing; set { Interlocked.Increment(ref epoch); Beat(); capturing=value; } }
    public void Beat() => Interlocked.Exchange(ref heartbeat,Environment.TickCount64);
    void Watchdog()
    {
        if(capturing && Environment.TickCount64-Interlocked.Read(ref heartbeat)>2000) {capturing=false;Enqueue(3,default);}
    }
    public long Epoch=>Interlocked.Read(ref epoch);
    public InputCapture()
    {
        mouseProc=Mouse; keyProc=Keyboard;
        thread=new Thread(Run) { IsBackground=true,Name="LumaNib input" };
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        ready.Wait();
        if(startError is not null) throw new InvalidOperationException(L10n.T("无法启用鼠标和 Esc 监听。"),startError);
    }
    void Run()
    {
        try {
            threadId=Native.GetCurrentThreadId();
            Native.PeekMessage(out _,IntPtr.Zero,0,0,0);
            var module=Native.GetModuleHandle(null);
            mouseHook=Native.SetWindowsHookEx(14,mouseProc,module,0);
            if(mouseHook==IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
            keyHook=Native.SetWindowsHookEx(13,keyProc,module,0);
            if(keyHook==IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
            ready.Set();
            while(Native.GetMessage(out var msg,IntPtr.Zero,0,0)>0) { Native.TranslateMessage(ref msg); Native.DispatchMessage(ref msg); }
        } catch(Exception ex) { startError=ex; ready.Set(); }
        finally { if(mouseHook!=IntPtr.Zero) Native.UnhookWindowsHookEx(mouseHook); if(keyHook!=IntPtr.Zero) Native.UnhookWindowsHookEx(keyHook); }
    }
    void Enqueue(int kind,ScreenPoint p)
    {
        if(kind==0 && Interlocked.Increment(ref moveCount)>8192) { Interlocked.Decrement(ref moveCount); return; }
        events.Enqueue(new(kind,p,Epoch));
    }
    IntPtr Mouse(int code,IntPtr wp,IntPtr lp)
    {
        if(code>=0) {
            Watchdog();
            int msg=wp.ToInt32();
            if(capturing || swallowedLeft || swallowedRight || swallowedMiddle || swallowedX!=0) {
                var data=Marshal.PtrToStructure<Native.MouseData>(lp);
                var p=new ScreenPoint(data.pt.X,data.pt.Y);
                if(msg==0x204 && capturing) {swallowedRight=true;return new IntPtr(1);}
                if(msg==0x205 && swallowedRight) {swallowedRight=false;return new IntPtr(1);}
                if(msg==0x207 && capturing) {swallowedMiddle=true;return new IntPtr(1);}
                if(msg==0x208 && swallowedMiddle) {swallowedMiddle=false;return new IntPtr(1);}
                if(msg==0x20B && capturing) {swallowedX|=data.mouseData>>16;return new IntPtr(1);}
                if(msg==0x20C && (swallowedX&(data.mouseData>>16))!=0) {swallowedX&=~(data.mouseData>>16);return new IntPtr(1);}
                if(msg is 0x20A or 0x20E && capturing) return new IntPtr(1);
                if(msg==0x201 && capturing) { swallowedLeft=true; Enqueue(1,p); return new IntPtr(1); }
                if(msg==0x202 && swallowedLeft) { swallowedLeft=false; Enqueue(2,p); return new IntPtr(1); }
                // Let Windows update the cursor position. The swallowed down/up pair prevents normal underlying drags.
                if(msg==0x200 && swallowedLeft && capturing) Enqueue(0,p);
            }
        }
        return Native.CallNextHookEx(mouseHook,code,wp,lp);
    }
    IntPtr Keyboard(int code,IntPtr wp,IntPtr lp)
    {
        if(code>=0) Watchdog();
        if(code>=0 && (capturing||swallowedEscape)) {
            var data=Marshal.PtrToStructure<Native.KeyData>(lp);
            if(data.vkCode==27) {
                int msg=wp.ToInt32();
                if(msg is 0x100 or 0x104 && (capturing||swallowedEscape)) { if(!swallowedEscape) Enqueue(3,default); capturing=false; swallowedEscape=true; return new IntPtr(1); }
                if(msg is 0x101 or 0x105 && swallowedEscape) { swallowedEscape=false; return new IntPtr(1); }
            }
        }
        return Native.CallNextHookEx(keyHook,code,wp,lp);
    }
    public bool TryRead(out PointerEvent e) { bool found=events.TryDequeue(out e); if(found && e.Kind==0) Interlocked.Decrement(ref moveCount); return found; }
    public void Dispose() { capturing=false; Native.PostThreadMessage(threadId,Native.WM_QUIT,UIntPtr.Zero,IntPtr.Zero); thread.Join(1500); ready.Dispose(); }
}
