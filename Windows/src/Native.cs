using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace LumaNib;

internal static class Native
{
    internal const int WM_HOTKEY=0x312, WM_APP=0x8000, WM_QUIT=0x12;
    internal const int WS_EX_TRANSPARENT=0x20, WS_EX_TOOLWINDOW=0x80, WS_EX_NOACTIVATE=0x08000000;
    internal static readonly IntPtr Topmost=new(-1);
    [StructLayout(LayoutKind.Sequential)] internal struct POINT { public int X,Y; }
    [StructLayout(LayoutKind.Sequential)] internal struct RECT { public int Left,Top,Right,Bottom; }
    [StructLayout(LayoutKind.Sequential)] internal struct MSG { public IntPtr hwnd; public uint message; public UIntPtr wParam; public IntPtr lParam; public uint time; public POINT pt; public uint lPrivate; }
    [StructLayout(LayoutKind.Sequential)] internal struct MouseData { public POINT pt; public uint mouseData,flags,time; public UIntPtr extra; }
    [StructLayout(LayoutKind.Sequential)] internal struct KeyData { public uint vkCode,scanCode,flags,time; public UIntPtr extra; }
    internal delegate IntPtr HookProc(int code,IntPtr wParam,IntPtr lParam);
    internal delegate bool MonitorProc(IntPtr monitor,IntPtr hdc,ref RECT rect,IntPtr data);
    [DllImport("user32.dll")] internal static extern bool GetCursorPos(out POINT p);
    [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll",SetLastError=true)] internal static extern bool RegisterHotKey(IntPtr hwnd,int id,uint mods,uint key);
    [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr hwnd,int id);
    [DllImport("user32.dll",SetLastError=true)] internal static extern IntPtr SetWindowsHookEx(int id,HookProc proc,IntPtr module,uint thread);
    [DllImport("user32.dll")] internal static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] internal static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr wParam,IntPtr lParam);
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] internal static extern IntPtr GetModuleHandle(string? name);
    [DllImport("kernel32.dll")] internal static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] internal static extern int GetMessage(out MSG msg,IntPtr hwnd,uint min,uint max);
    [DllImport("user32.dll")] internal static extern bool PeekMessage(out MSG msg,IntPtr hwnd,uint min,uint max,uint remove);
    [DllImport("user32.dll")] internal static extern bool TranslateMessage(ref MSG msg);
    [DllImport("user32.dll")] internal static extern IntPtr DispatchMessage(ref MSG msg);
    [DllImport("user32.dll")] internal static extern bool PostThreadMessage(uint thread,uint msg,UIntPtr wp,IntPtr lp);
    [DllImport("user32.dll")] internal static extern IntPtr GetWindowLongPtr(IntPtr hwnd,int index);
    [DllImport("user32.dll",SetLastError=true)] internal static extern IntPtr SetWindowLongPtr(IntPtr hwnd,int index,IntPtr value);
    [DllImport("user32.dll")] internal static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int width,int height,uint flags);
    [DllImport("user32.dll")] internal static extern uint GetDpiForWindow(IntPtr hwnd);
    [DllImport("user32.dll")] internal static extern bool EnumDisplayMonitors(IntPtr dc,IntPtr clip,MonitorProc proc,IntPtr data);
    [DllImport("shcore.dll")] internal static extern int GetDpiForMonitor(IntPtr monitor,int type,out uint x,out uint y);
    [DllImport("user32.dll")] internal static extern IntPtr MonitorFromPoint(POINT p,uint flags);
    [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern uint RegisterWindowMessage(string message);
    [DllImport("user32.dll")] internal static extern bool PostMessage(IntPtr hwnd,uint message,IntPtr wp,IntPtr lp);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern IntPtr FindWindow(string? cls,string title);
    [DllImport("wtsapi32.dll")] internal static extern bool WTSRegisterSessionNotification(IntPtr hwnd,uint flags);
    [DllImport("wtsapi32.dll")] internal static extern bool WTSUnRegisterSessionNotification(IntPtr hwnd);
    internal static bool Down(int key) => (GetAsyncKeyState(key)&0x8000)!=0;
    internal static bool Held(Shortcut s) => Down(s.Key) && ((s.Modifiers&1)==0||Down(18)) &&
        ((s.Modifiers&2)==0||Down(17)) && ((s.Modifiers&4)==0||Down(16)) && ((s.Modifiers&8)==0||Down(91)||Down(92));
    internal static double Dpi(IntPtr monitor) => GetDpiForMonitor(monitor,0,out uint x,out _)==0 ? x/96.0 : 1;
    internal static List<MonitorInfo> Monitors()
    {
        var list=new List<MonitorInfo>();
        MonitorProc callback=(IntPtr m,IntPtr dc,ref RECT r,IntPtr data)=>{ list.Add(new MonitorInfo(m,r.Left,r.Top,r.Right-r.Left,r.Bottom-r.Top,Dpi(m))); return true; };
        if(!EnumDisplayMonitors(IntPtr.Zero,IntPtr.Zero,callback,IntPtr.Zero)) throw new Win32Exception();
        return list;
    }
}
internal readonly record struct MonitorInfo(IntPtr Handle,int X,int Y,int Width,int Height,double Scale);
