using LumaNib;
using System.Text.Json;
int passed=0;
void Check(bool value,string name) { if(!value)throw new Exception("FAILED: "+name);Console.WriteLine("PASS "+name);passed++; }
foreach(string language in new[]{"zh","zh-CN","zh-Hans-CN","zh-Hant-TW","zh_HK","ZH-hans"})
 Check(L10n.Text("鼠标圆环",language)=="鼠标圆环","Chinese UI for "+language);
foreach(string language in new[]{"en-US","en-GB","fr-FR","ja-JP","de-DE",""})
 Check(L10n.Text("鼠标圆环",language)=="Mouse Ring","English fallback for "+language);
var originalUI=System.Globalization.CultureInfo.CurrentUICulture;
var originalCulture=System.Globalization.CultureInfo.CurrentCulture;
try {
 System.Globalization.CultureInfo.CurrentUICulture=new("en-US");
 System.Globalization.CultureInfo.CurrentCulture=new("zh-CN");
 Check(L10n.T("鼠标圆环")=="Mouse Ring","display language takes precedence over regional format");
 Check(L10n.F("按 {0} 开始画，按 Esc 或再按一次快捷键退出。","Ctrl + D")=="Press Ctrl + D to draw. Press Esc or the shortcut again to stop.","English dynamic shortcut instructions");
 System.Globalization.CultureInfo.CurrentUICulture=new("zh-CN");
 Check(L10n.F("{0} 笔标注",3)=="3 笔标注","Chinese dynamic counts");
 Check(L10n.T("LumaNib")=="LumaNib","product name is preserved");
} finally {System.Globalization.CultureInfo.CurrentUICulture=originalUI;System.Globalization.CultureInfo.CurrentCulture=originalCulture;}
var prefs=new Preferences();
Check(Preferences.ValidShortcuts(prefs.Shortcuts),"default shortcuts valid");
Check(prefs.LeftColor!=prefs.RightColor,"default independent ring colors");
Check(new Shortcut(49,3).Label=="Ctrl + Alt + 1","custom number key label");
Check(new Shortcut(37,10).Label=="Ctrl + Win + ←","custom arrow with Win label");
Check(!new Shortcut(65,4).Valid,"Shift alone rejected");
Check(!new Shortcut(27,3).Valid,"Escape reserved");
Check(!new Shortcut(123,3).Valid,"F12 reserved by Windows");
Check(!new Shortcut(17,3).Valid,"modifier alone rejected");
Check(!new Shortcut(65,18).Valid,"unknown modifier bits rejected");
Check(!Preferences.ValidShortcuts(new[]{new Shortcut(65,3),new Shortcut(65,3),new Shortcut(66,3),new Shortcut(67,3)}),"duplicates rejected");
Check(!Preferences.ValidShortcuts(null),"null shortcuts rejected");
prefs.RingRadius=double.NaN;prefs.PenGlow=999;prefs.PenWidth=-8;prefs.FadeDelay=0;prefs.LeftColor=-2;prefs.RightColor=50;
prefs.Sanitize();Check(prefs.RingRadius==21&&prefs.PenGlow==24&&prefs.PenWidth==2&&prefs.FadeDelay==1,"numeric settings sanitized");
Check(prefs.LeftColor==0&&prefs.RightColor==5,"color settings sanitized");
prefs.Shortcuts=new[]{new Shortcut(1,0)};prefs.Sanitize();Check(prefs.Shortcuts.Length==4,"invalid shortcut array reset");
var mapped=new ScreenPoint(-1500,225).Local(-1920,0,1.5);
Check(mapped==new ScreenPoint(280,150),"negative-origin mixed DPI coordinates");
var ink=new InkModel();prefs=new Preferences{AutoFade=true,FadeDelay=3};
ink.Begin(new ScreenPoint(0,0),prefs,1.5);ink.Append(new ScreenPoint(0.1,0.1));
Check(ink.Current!.Points.Count==1,"tiny duplicate motion omitted");
ink.Append(new ScreenPoint(4,5));Check(ink.Current.Points.Count==2,"freehand points preserved");
Check(ink.Current.Width==6&&ink.Current.Glow==18,"width snapshots display scale");
ink.Finish(prefs,10);Check(ink.Strokes.Count==1&&ink.Current is null,"release commits stroke");
Check(ink.Strokes[0].Opacity(12)==1,"fade waits requested delay");
Check(Math.Abs(ink.Strokes[0].Opacity(13.325)-0.5)<0.00001,"fade interpolates over 650ms");
Check(!ink.Expire(13.5),"fading stroke retained");
Check(ink.Expire(13.66)&&ink.Count==0,"expired stroke removed");
prefs.AutoFade=false;ink.Begin(new(1,2),prefs,1);ink.Finish(prefs,20);Check(ink.Strokes[0].Opacity(10000)==1,"persistent ink has no deadline");
ink.Begin(new(3,4),prefs,1);ink.Undo();Check(ink.Count==1&&ink.Current is null,"undo current before committed stroke");
ink.Undo();Check(ink.Count==0,"undo committed stroke");ink.Undo();Check(ink.Count==0,"undo empty is safe");
ink.Begin(new(5,6),prefs,1);ink.Finish(prefs,30);prefs.AutoFade=true;prefs.FadeDelay=5;ink.UpdateFade(prefs,40);
Check(ink.Strokes[0].FadeStartsAt==45,"changing fade rearms existing strokes");prefs.AutoFade=false;ink.UpdateFade(prefs,42);
Check(ink.Strokes[0].FadeStartsAt is null,"disabling fade preserves existing strokes");
ink.Clear();Check(ink.Count==0,"clear removes all strokes");
string dir=Path.Combine(AppContext.BaseDirectory,"settings-test-"+Guid.NewGuid());
try {
 var store=new PreferenceStore(dir);prefs.Shortcuts[1]=new Shortcut(49,10);prefs.LeftColor=3;prefs.RightColor=0;
 store.Save(prefs);var loaded=store.Load(out var warning);
 Check(warning is null&&loaded.Shortcuts[1]==prefs.Shortcuts[1],"custom shortcut persists");
 Check(loaded.LeftColor==3&&loaded.RightColor==0,"left/right colors persist independently");
 File.WriteAllText(store.FilePath,"broken json");store.Load(out warning);Check(warning is not null,"corrupt settings reports warning");
 Check(File.ReadAllText(store.FilePath)=="broken json","load does not overwrite corrupt file");
} finally {if(Directory.Exists(dir))Directory.Delete(dir,true);}
var backend=new FakeKeys();using(var hotkeys=new HotKeys(IntPtr.Zero,backend)) {
 var defaults=Preferences.DefaultShortcuts();Check(hotkeys.Configure(defaults) is null&&backend.Keys.Count==4,"hotkeys initial registration");
 int drawId=backend.Keys.Single(kv=>kv.Value==defaults[1]).Key;Check(hotkeys.ActionFor(drawId)==1,"hotkey dispatch by action");
 var collision=defaults.ToArray();collision[0]=new Shortcut(49,3);collision[1]=new Shortcut(50,3);backend.Blocked.Add(collision[1]);
 Check(hotkeys.Configure(collision) is not null,"occupied shortcut reports conflict");
 Check(backend.Keys.Values.ToHashSet().SetEquals(defaults),"failed configure rolls back staged registrations");
 Check(hotkeys.ActionFor(drawId)==1,"failed configure retains working dispatch");
 var reorder=defaults.Reverse().ToArray();Check(hotkeys.Configure(reorder) is null&&backend.Keys.Count==4,"same combinations can reorder transactionally");
 Check(hotkeys.ActionFor(drawId)==2,"reorder updates actions");
 hotkeys.Suspend();Check(backend.Keys.Count==0&&hotkeys.ActionFor(drawId)==-1,"recording suspends hotkeys");
 Check(hotkeys.Resume() is null&&backend.Keys.Count==4,"cancel recording restores configured keys");
 hotkeys.Suspend();backend.Blocked.Add(reorder[0]);
 Check(hotkeys.Resume() is not null&&backend.Keys.Count==0,"resume conflict reports unavailable keys without partial registration");
 backend.Blocked.Remove(reorder[0]);Check(hotkeys.Resume() is null,"resume can recover after conflict clears");
 var changed=defaults.ToArray();changed[0]=new Shortcut(51,3);Check(hotkeys.Configure(changed) is null,"new free shortcut applied");
 Check(!backend.Keys.Values.Contains(defaults[0])&&backend.Keys.Values.Contains(changed[0]),"obsolete hotkey unregistered");
}
Check(backend.Keys.Count==0,"dispose releases every registered hotkey");

var startupStore=new FakeStartupStore();
var startup=new LoginStartup(@"C:\Apps\荧光 Tools\LumaNib.exe",startupStore);
Check(!startup.Read().Registered&&startupStore.Writes==0,"startup defaults off without writes");
Check(startup.SetEnabled(true).CurrentPath,"startup enable readback matches current executable");
Check(startupStore.Value=="\"C:\\Apps\\荧光 Tools\\LumaNib.exe\" --background","startup command quotes spaces and Unicode and adds background flag");
startup.SetEnabled(false);Check(!startup.Read().Registered,"startup disable removes entry");
startupStore.Value=@"""C:\Old App\LumaNib.exe"" --background";
Check(startup.Read().Registered&&!startup.Read().CurrentPath,"moved installation is detected");
startup.SetEnabled(true);Check(startup.Read().CurrentPath,"startup path can be updated explicitly");
startupStore.Fail=true;
try {startup.SetEnabled(false);Check(false,"expected startup store failure");}catch(IOException) {Check(startupStore.Value is not null,"failed removal preserves prior startup entry");}
startupStore.Fail=false;startup.SetEnabled(false);startup.SetEnabled(false);Check(!startup.Read().Registered,"disable is idempotent");
foreach(var path in new[]{"", "C:\\bad\"name.exe", "C:\\bad\nname.exe", "C:\\App.dll", "C:\\"+new string('x',260)+".exe"}) {
 try {LoginStartup.BuildCommand(path);Check(false,"invalid startup path accepted");} catch(ArgumentException) {Check(true,"invalid or overlong startup path rejected");}
}
Check(LoginStartup.IsBackground(new[]{"--background"})&&!LoginStartup.IsBackground(Array.Empty<string>()),"manual and login launch modes distinguished");
startupStore.IgnoreWrites=true;
try {startup.SetEnabled(true);Check(false,"ignored startup write accepted");}catch(IOException) {Check(!startup.Read().Registered,"failed readback does not report enabled");}
Console.WriteLine($"{passed} assertions passed. Native Windows runtime and OBS not exercised.");
sealed class FakeKeys : IHotKeyBackend {
 public Dictionary<int,Shortcut> Keys=new();public HashSet<Shortcut> Blocked=new();
 public bool Register(IntPtr handle,int id,uint modifiers,uint key) {var s=new Shortcut((int)key,modifiers&15);if(Blocked.Contains(s)||Keys.Values.Contains(s))return false;Keys[id]=s;return true;}
 public void Unregister(IntPtr handle,int id)=>Keys.Remove(id);
}

sealed class FakeStartupStore : IStartupStore {
 public string? Value;public int Writes;public bool Fail;public bool IgnoreWrites;
 public string? Read()=>Value;
 public void Write(string command) {if(Fail)throw new IOException();Writes++;if(!IgnoreWrites)Value=command;}
 public void Remove() {if(Fail)throw new IOException();Value=null;}
}
