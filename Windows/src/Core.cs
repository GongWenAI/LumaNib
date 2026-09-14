using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace LumaNib;

public readonly record struct ScreenPoint(double X, double Y)
{
    public double Distance(ScreenPoint p) => Math.Sqrt((X-p.X)*(X-p.X)+(Y-p.Y)*(Y-p.Y));
    public ScreenPoint Local(double x, double y, double scale) => new((X-x)/scale, (Y-y)/scale);
}
public readonly record struct Shortcut(int Key, uint Modifiers)
{
    // Win32: Alt=1, Ctrl=2, Shift=4, Win=8. Esc and modifier-only keys are reserved.
    public bool Valid => Key is >= 8 and <= 254 && Key != 27 && Key != 123 &&
        Key is not (16 or 17 or 18 or 91 or 92 or 160 or 161 or 162 or 163 or 164 or 165) &&
        (Modifiers & ~15u) == 0 && (Modifiers & 11u) != 0;
    public string Label
    {
        get
        {
            string key = Key switch {
                8 => "Backspace", 9 => "Tab", 13 => "Enter", 32 => "Space", 33 => "Page Up", 34 => "Page Down",
                35 => "End", 36 => "Home", 37 => "←", 38 => "↑", 39 => "→", 40 => "↓", 45 => "Insert", 46 => "Delete",
                >= 112 and <= 135 => "F"+(Key-111), >= 96 and <= 105 => "Num "+(Key-96),
                106 => "Num *", 107 => "Num +", 109 => "Num -", 110 => "Num .", 111 => "Num /",
                186 => ";", 187 => "=", 188 => ",", 189 => "-", 190 => ".", 191 => "/", 192 => "`", 219 => "[", 220 => "\\", 221 => "]", 222 => "'",
                >= 48 and <= 90 => ((char)Key).ToString(), _ => "VK "+Key
            };
            return ((Modifiers&2)!=0 ? "Ctrl + " : "") + ((Modifiers&1)!=0 ? "Alt + " : "") +
                ((Modifiers&4)!=0 ? "Shift + " : "") + ((Modifiers&8)!=0 ? "Win + " : "") + key;
        }
    }
}
public sealed class Preferences
{
    public bool RingEnabled { get; set; } = true;
    public double RingRadius { get; set; } = 21;
    public double RingWidth { get; set; } = 3;
    public int LeftColor { get; set; }
    public int RightColor { get; set; } = 4;
    public int PenColor { get; set; } = 2;
    public double PenWidth { get; set; } = 4;
    public double PenGlow { get; set; } = 12;
    public bool HoldMode { get; set; }
    public bool AutoFade { get; set; }
    public double FadeDelay { get; set; } = 5;
    public Shortcut[] Shortcuts { get; set; } = DefaultShortcuts();
    public static Shortcut[] DefaultShortcuts() => new[] { new Shortcut(82,3), new Shortcut(68,3), new Shortcut(90,3), new Shortcut(88,3) };
    static double Bound(double n, double min, double max, double fallback) => double.IsFinite(n) ? Math.Clamp(n,min,max) : fallback;
    public static bool ValidShortcuts(Shortcut[]? values) => values is { Length: 4 } && values.All(s=>s.Valid) && values.Distinct().Count()==4;
    public void Sanitize()
    {
        RingRadius=Bound(RingRadius,12,42,21); RingWidth=Bound(RingWidth,2,6,3);
        PenWidth=Bound(PenWidth,2,14,4); PenGlow=Bound(PenGlow,0,24,12); FadeDelay=Bound(FadeDelay,1,30,5);
        LeftColor=Math.Clamp(LeftColor,0,5); RightColor=Math.Clamp(RightColor,0,5); PenColor=Math.Clamp(PenColor,0,5);
        if (!ValidShortcuts(Shortcuts)) Shortcuts=DefaultShortcuts();
    }
}
public sealed class PreferenceStore
{
    public string DirectoryPath { get; }
    public string FilePath => Path.Combine(DirectoryPath,"settings.json");
    public PreferenceStore(string directory) { DirectoryPath=directory; }
    public Preferences Load(out string? warning)
    {
        warning=null;
        if (!File.Exists(FilePath)) return new();
        try { var p=JsonSerializer.Deserialize<Preferences>(File.ReadAllText(FilePath)) ?? throw new InvalidDataException(); p.Sanitize(); return p; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException) {
            warning=L10n.T("设置文件无法读取，本次使用默认设置。原文件仍保留在：")+FilePath; return new();
        }
    }
    public void Save(Preferences p)
    {
        Directory.CreateDirectory(DirectoryPath);
        string temp=FilePath+".tmp";
        File.WriteAllText(temp,JsonSerializer.Serialize(p,new JsonSerializerOptions { WriteIndented=true }));
        File.Move(temp,FilePath,true);
    }
}
public sealed class Stroke
{
    public Guid Id { get; }=Guid.NewGuid();
    public List<ScreenPoint> Points { get; }=new();
    public int Color { get; init; }
    // Physical pixel widths retain their appearance across mixed-DPI monitor boundaries.
    public double Width { get; init; }
    public double Glow { get; init; }
    public double? FadeStartsAt { get; set; }
    public double Opacity(double now) => FadeStartsAt is double start ? Math.Clamp(1-(now-start)/0.65,0,1) : 1;
}
public sealed class InkModel
{
    public List<Stroke> Strokes { get; }=new();
    public Stroke? Current { get; private set; }
    public int Count => Strokes.Count+(Current is null?0:1);
    public void Begin(ScreenPoint point, Preferences p, double scale)
    {
        Current=new Stroke { Color=p.PenColor, Width=p.PenWidth*scale, Glow=p.PenGlow*scale };
        Current.Points.Add(point);
    }
    public void Append(ScreenPoint p)
    {
        if (Current is not null && Current.Points[^1].Distance(p)>=0.65) Current.Points.Add(p);
    }
    public void Finish(Preferences p,double now)
    {
        if(Current is null) return;
        Current.FadeStartsAt=p.AutoFade?now+p.FadeDelay:null;
        Strokes.Add(Current); Current=null;
    }
    public void Undo() { if(Current is not null) Current=null; else if(Strokes.Count>0) Strokes.RemoveAt(Strokes.Count-1); }
    public void Clear() { Current=null; Strokes.Clear(); }
    public void UpdateFade(Preferences p,double now) { foreach(var s in Strokes) s.FadeStartsAt=p.AutoFade?now+p.FadeDelay:null; }
    public bool Expire(double now) => Strokes.RemoveAll(s=>s.Opacity(now)<=0)>0;
}
