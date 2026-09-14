using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LumaNib;
internal sealed class SettingsWindow : Window
{
    readonly Controller app;
    readonly StackPanel page=new(){Margin=new Thickness(28,0,28,20)};
    readonly TextBlock status=new();
    readonly TextBlock count=new();
    readonly Button draw,undo,clear;
    readonly Button[] tabs=new Button[5];
    readonly Button[] recorders=new Button[4];
    readonly TextBlock shortcutMessage=new(){TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,0)};
    Shortcut[] draft;
    int recording=-1;
    CheckBox? ringToggle;
    CheckBox? startupToggle;
    TextBlock? startupStatus;
    Button? startupRepair;
    bool refreshingStartup;
    static readonly Color Accent=Color.FromRgb(51,217,242);
    static readonly Brush Muted=Paint.Brush(Color.FromRgb(157,164,179));
    static readonly Brush Surface=Paint.Brush(Color.FromRgb(28,32,42));
    public bool AllowClose { get; set; }
    Preferences P=>app.Prefs;
    public SettingsWindow(Controller app)
    {
        this.app=app; draft=P.Shortcuts.ToArray();
        Title="LumaNib"; Width=736; Height=728; MinWidth=720; MinHeight=570;
        WindowStartupLocation=WindowStartupLocation.CenterScreen;
        Background=Paint.Brush(Color.FromRgb(17,20,28)); Foreground=Brushes.White;
        FontFamily=new FontFamily("Segoe UI, Microsoft YaHei UI"); FontSize=13;
        var stream=System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/AppIcon.ico"));
        if(stream is not null) Icon=System.Windows.Media.Imaging.BitmapFrame.Create(stream.Stream);
        Resources=(ResourceDictionary)XamlReader.Parse(Styles);
        var root=new DockPanel(); Content=root;
        var header=new Grid{Margin=new Thickness(28,22,28,20)};
        header.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto}); header.ColumnDefinitions.Add(new ColumnDefinition()); header.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto});
        var logo=new System.Windows.Controls.Image{Source=Icon,Width=54,Height=54,Stretch=Stretch.Uniform,Margin=new Thickness(0,0,14,0)};
        RenderOptions.SetBitmapScalingMode(logo,BitmapScalingMode.HighQuality);
        System.Windows.Automation.AutomationProperties.SetName(logo,"LumaNib");
        header.Children.Add(logo);
        var name=new StackPanel{VerticalAlignment=VerticalAlignment.Center}; name.Children.Add(Text("LumaNib",25,true)); name.Children.Add(Text(L10n.T("让每一次指向，都清楚可见。"),12,false,Muted)); Grid.SetColumn(name,1); header.Children.Add(name);
        status.FontSize=11; status.Foreground=Paint.Brush(Accent); status.VerticalAlignment=VerticalAlignment.Center; Grid.SetColumn(status,2); header.Children.Add(status);
        DockPanel.SetDock(header,Dock.Top); root.Children.Add(header);
        var tabBar=new System.Windows.Controls.Primitives.UniformGrid{Columns=5,Margin=new Thickness(28,0,28,20)};
        string[] labels={L10n.T("鼠标圆环"),L10n.T("荧光画笔"),L10n.T("全局快捷键"),L10n.T("通用"),L10n.T("使用说明")};
        for(int i=0;i<tabs.Length;i++) { int index=i; tabs[i]=Button(labels[i],()=>SelectPage(index)); tabs[i].Margin=new Thickness(1); tabBar.Children.Add(tabs[i]); }
        DockPanel.SetDock(tabBar,Dock.Top); root.Children.Add(tabBar);
        var footer=new DockPanel{Margin=new Thickness(28,14,28,16)};
        count.Foreground=Muted; count.FontSize=11; count.VerticalAlignment=VerticalAlignment.Center;
        var actions=new StackPanel{Orientation=Orientation.Horizontal};
        undo=Button(L10n.T("撤销"),()=>app.Engine.Undo()); clear=Button(L10n.T("清空笔迹"),()=>app.Engine.Clear()); draw=Button(L10n.T("开始画笔"),()=>app.Engine.Toggle(),true);
        actions.Children.Add(undo); actions.Children.Add(clear); actions.Children.Add(draw);
        DockPanel.SetDock(actions,Dock.Right); footer.Children.Add(actions); footer.Children.Add(count);
        var footerBorder=new Border{BorderBrush=Paint.Brush(Colors.White,0.08),BorderThickness=new Thickness(0,1,0,0),Child=footer};
        DockPanel.SetDock(footerBorder,Dock.Bottom); root.Children.Add(footerBorder);
        root.Children.Add(new ScrollViewer{Content=page,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled});
        Activated+=(_,_)=>RefreshStartup();
        PreviewKeyDown+=RecordKey; Deactivated+=(_,_)=>CancelCapture();
        Closing+=(_,e)=>{ CancelCapture(); if(!AllowClose) { e.Cancel=true; Hide(); } };
        app.Engine.Changed+=RefreshStatus;
        SelectPage(0); RefreshStatus();
    }
    public void RefreshStatus()
    {
        if(ringToggle is not null && ringToggle.IsChecked!=P.RingEnabled) ringToggle.IsChecked=P.RingEnabled;
        status.Text=app.Engine.Drawing?L10n.T("● 画笔使用中"):L10n.T("● 托盘运行中");
        count.Text=app.Engine.Ink.Count+L10n.T(" 笔标注");
        undo.IsEnabled=clear.IsEnabled=app.Engine.Ink.Count>0;
        draw.Content=app.Engine.Drawing?L10n.T("退出画笔 · Esc"):L10n.T("开始画笔");
    }
    static TextBlock Text(string s,double size=13,bool bold=false,Brush? brush=null) => new(){Text=s,FontSize=size,FontWeight=bold?FontWeights.SemiBold:FontWeights.Normal,Foreground=brush??Brushes.White,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,5)};
    static Button Button(string label,Action action,bool accent=false)
    {
        var b=new Button{Content=label,Margin=new Thickness(0,0,8,0)};
        if(accent) { b.Background=Paint.Brush(Accent); b.Foreground=Paint.Brush(Color.FromRgb(12,26,30)); }
        b.Click+=(_,_)=>action(); return b;
    }
    static Border Card(UIElement child) => new(){Child=child,Padding=new Thickness(18),CornerRadius=new CornerRadius(14),Background=Surface,BorderBrush=Paint.Brush(Colors.White,0.06),BorderThickness=new Thickness(1),Margin=new Thickness(0,0,0,16)};
    static void Divider(Panel panel) => panel.Children.Add(new Border{Height=1,Background=Paint.Brush(Colors.White,0.08),Margin=new Thickness(0,10,0,14)});
    static Grid Row(string title)
    {
        var g=new Grid{Margin=new Thickness(0,0,0,16)};
        g.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(140)}); g.ColumnDefinitions.Add(new ColumnDefinition());
        var text=Text(title); text.VerticalAlignment=VerticalAlignment.Center; text.Margin=new Thickness(0); g.Children.Add(text); return g;
    }
    void SelectPage(int n)
    {
        CancelCapture(); ringToggle=null; startupToggle=null; page.Children.Clear();
        for(int i=0;i<tabs.Length;i++) tabs[i].Background=i==n?Paint.Brush(Color.FromRgb(55,65,81)):Surface;
        switch(n) {case 0:RingPage();break;case 1:PenPage();break;case 2:ShortcutPage();break;case 3:GeneralPage();break;default:HelpPage();break;}
    }
    void Toggle(StackPanel parent,string title,bool initial,Action<bool> changed)
    {
        var box=new CheckBox{Content=title,IsChecked=initial,Foreground=Brushes.White,Margin=new Thickness(0,0,0,14),FontSize=13};
        box.Checked+=(_,_)=>changed(true); box.Unchecked+=(_,_)=>changed(false); parent.Children.Add(box);
    }
    void Slider(StackPanel parent,string title,double value,double min,double max,Action<double> changed,string suffix="pt",double multiplier=1)
    {
        var row=Row(title); var inner=new Grid(); inner.ColumnDefinitions.Add(new ColumnDefinition()); inner.ColumnDefinitions.Add(new ColumnDefinition{Width=new GridLength(60)});
        var slider=new Slider{Minimum=min,Maximum=max,Value=value,TickFrequency=1,IsSnapToTickEnabled=true,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,12,0)};
        var readout=Text($"{value*multiplier:0} {suffix}",12,false,Muted); readout.TextAlignment=TextAlignment.Right; readout.VerticalAlignment=VerticalAlignment.Center; readout.Margin=new Thickness(0);
        slider.ValueChanged+=(_,e)=>{ readout.Text=$"{e.NewValue*multiplier:0} {suffix}"; changed(e.NewValue); };
        inner.Children.Add(slider); Grid.SetColumn(readout,1); inner.Children.Add(readout); Grid.SetColumn(inner,1); row.Children.Add(inner); parent.Children.Add(row);
    }
    void ColorsRow(StackPanel parent,string title,int current,Action<int> changed)
    {
        var row=Row(title); var colors=new Grid(); colors.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto}); colors.ColumnDefinitions.Add(new ColumnDefinition());
        var swatches=new StackPanel{Orientation=Orientation.Horizontal}; var selection=Text(Paint.Names[current],11,false,Muted); selection.HorizontalAlignment=HorizontalAlignment.Right; selection.VerticalAlignment=VerticalAlignment.Center;
        var markers=new Border[6];
        for(int i=0;i<6;i++) {
            int index=i;
            var dot=new Ellipse{Fill=Paint.Brush(Paint.Colors[i]),Width=23,Height=23};
            markers[i]=new Border{Child=dot,BorderBrush=i==current?Brushes.White:Brushes.Transparent,BorderThickness=new Thickness(2),CornerRadius=new CornerRadius(18),Padding=new Thickness(3)};
            var b=new Button{Content=markers[i],Padding=new Thickness(0),Background=Brushes.Transparent,Margin=new Thickness(0,0,9,0),ToolTip=Paint.Names[i],BorderThickness=new Thickness(0)};
            System.Windows.Automation.AutomationProperties.SetName(b,title+Paint.Names[i]);
            b.Click+=(_,_)=>{for(int j=0;j<6;j++) markers[j].BorderBrush=j==index?Brushes.White:Brushes.Transparent; selection.Text=Paint.Names[index]; changed(index);}; swatches.Children.Add(b);
        }
        colors.Children.Add(swatches); Grid.SetColumn(selection,1); colors.Children.Add(selection); Grid.SetColumn(colors,1); row.Children.Add(colors); parent.Children.Add(row);
    }
    void RingPage()
    {
        var preview=new RingVisual{Prefs=P,Width=190,Height=146};
        var intro=new Grid{Margin=new Thickness(0,0,0,18)}; intro.ColumnDefinitions.Add(new ColumnDefinition{Width=GridLength.Auto}); intro.ColumnDefinitions.Add(new ColumnDefinition());
        var box=new Border{Child=preview,Background=Paint.Brush(Colors.Black,0.2),CornerRadius=new CornerRadius(16),Margin=new Thickness(0,0,20,0)}; intro.Children.Add(box);
        var words=new StackPanel{VerticalAlignment=VerticalAlignment.Center}; words.Children.Add(Text(L10n.T("双半环 · 跟随鼠标"),16,true)); words.Children.Add(Text(L10n.T("左键点亮左半环，右键点亮右半环。\n按住时持续发光，松开后轻轻淡出。"),12,false,Muted));
        var buttons=new StackPanel{Orientation=Orientation.Horizontal,Margin=new Thickness(0,8,0,0)};
        void Flash(bool left) {
            if(left) preview.LeftLight=1; else preview.RightLight=1; preview.InvalidateVisual();
            var t=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(450)}; t.Tick+=(_,_)=>{t.Stop();if(left)preview.LeftLight=0;else preview.RightLight=0;preview.InvalidateVisual();}; t.Start();
        }
        buttons.Children.Add(Button(L10n.T("预览左键"),()=>Flash(true))); buttons.Children.Add(Button(L10n.T("预览右键"),()=>Flash(false))); words.Children.Add(buttons); Grid.SetColumn(words,1); intro.Children.Add(words); page.Children.Add(intro);
        void Update() { preview.InvalidateVisual(); app.PreferencesChanged(); }
        var controls=new StackPanel();
        Toggle(controls,L10n.T("显示跟随圆环"),P.RingEnabled,v=>{P.RingEnabled=v;Update();}); ringToggle=(CheckBox)controls.Children[0]; Divider(controls);
        Slider(controls,L10n.T("圆环大小"),P.RingRadius,12,42,v=>{P.RingRadius=v;Update();},"pt",2);
        Slider(controls,L10n.T("圆环粗细"),P.RingWidth,2,6,v=>{P.RingWidth=v;Update();});
        ColorsRow(controls,L10n.T("左半环颜色"),P.LeftColor,v=>{P.LeftColor=v;Update();}); ColorsRow(controls,L10n.T("右半环颜色"),P.RightColor,v=>{P.RightColor=v;Update();});
        page.Children.Add(Card(controls)); page.Children.Add(Text(L10n.T("ⓘ  画笔模式会暂时隐藏圆环；退出画笔后自动恢复。"),11,false,Muted));
    }
    void PenPage()
    {
        var preview=new PenPreview{Prefs=P,Height=90}; page.Children.Add(new Border{Child=preview,Background=Paint.Brush(Colors.Black,0.2),CornerRadius=new CornerRadius(16),Margin=new Thickness(0,0,0,18)});
        void Update(bool fade=false,bool mode=false) { preview.InvalidateVisual(); app.PreferencesChanged(fade,mode); }
        var controls=new StackPanel();
        ColorsRow(controls,L10n.T("笔迹颜色"),P.PenColor,v=>{P.PenColor=v;Update();});
        Slider(controls,L10n.T("画笔粗细"),P.PenWidth,2,14,v=>{P.PenWidth=v;Update();}); Slider(controls,L10n.T("发光强度"),P.PenGlow,0,24,v=>{P.PenGlow=v;Update();},""); Divider(controls);
        var row=Row(L10n.T("画笔操作")); var modeButtons=new StackPanel{Orientation=Orientation.Horizontal};
        var toggle=new RadioButton{Content=L10n.T("按一下切换"),IsChecked=!P.HoldMode,Foreground=Brushes.White,Margin=new Thickness(0,0,24,0),GroupName="mode"};
        var hold=new RadioButton{Content=L10n.T("按住绘制"),IsChecked=P.HoldMode,Foreground=Brushes.White,GroupName="mode"};
        var instruction=Text("",11,false,Muted);
        void Instructions() => instruction.Text=P.HoldMode?L10n.F("按住 {0}，再用左键拖动画线；松开快捷键退出。",P.Shortcuts[1].Label):L10n.F("按 {0} 开始画，按 Esc 或再按一次快捷键退出。",P.Shortcuts[1].Label);
        toggle.Checked+=(_,_)=>{P.HoldMode=false;Update(mode:true);Instructions();}; hold.Checked+=(_,_)=>{P.HoldMode=true;Update(mode:true);Instructions();};
        modeButtons.Children.Add(toggle);modeButtons.Children.Add(hold);Grid.SetColumn(modeButtons,1);row.Children.Add(modeButtons);controls.Children.Add(row); Instructions();controls.Children.Add(instruction); Divider(controls);
        var fadeControls=new StackPanel();
        Slider(fadeControls,L10n.T("保留时间"),P.FadeDelay,1,30,v=>{P.FadeDelay=v;Update(fade:true);},L10n.T("秒"));
        fadeControls.Visibility=P.AutoFade?Visibility.Visible:Visibility.Collapsed;
        Toggle(controls,L10n.T("笔迹自动淡出"),P.AutoFade,v=>{P.AutoFade=v;fadeControls.Visibility=v?Visibility.Visible:Visibility.Collapsed;Update(fade:true);});
        controls.Children.Add(fadeControls); controls.Children.Add(Text(L10n.T("关闭自动淡出后，笔迹保留到手动清空。退出画笔后仍可正常点击下面的软件。"),11,false,Muted)); page.Children.Add(Card(controls));
    }
    void ShortcutPage()
    {
        draft=P.Shortcuts.ToArray(); page.Children.Add(Text(L10n.T("在其他软件中也能直接使用"),16,true)); page.Children.Add(Text(L10n.T("点一下输入框，直接按你想要的组合键，再点“应用快捷键”。"),12,false,Muted));
        var controls=new StackPanel(); string[] names={L10n.T("开关鼠标圆环"),L10n.T("画笔模式"),L10n.T("撤销上一笔"),L10n.T("清空全部笔迹")};
        for(int i=0;i<4;i++) {
            int index=i; var row=Row(names[i]); recorders[i]=Button(draft[i].Label,()=>StartCapture(index)); recorders[i].MinWidth=240;recorders[i].HorizontalAlignment=HorizontalAlignment.Right;
            Grid.SetColumn(recorders[i],1);row.Children.Add(recorders[i]);controls.Children.Add(row);
        }
        Divider(controls); var esc=Row(L10n.T("退出画笔")); var label=Text("Esc",13,false,Muted);label.HorizontalAlignment=HorizontalAlignment.Right;Grid.SetColumn(label,1);esc.Children.Add(label);controls.Children.Add(esc);page.Children.Add(Card(controls));
        var actions=new DockPanel();var apply=Button(L10n.T("应用快捷键"),()=>{CancelCapture();shortcutMessage.Text=app.ApplyShortcuts(draft)??L10n.T("快捷键已应用。");},true);DockPanel.SetDock(apply,Dock.Right);actions.Children.Add(apply);
        actions.Children.Add(Button(L10n.T("恢复默认组合"),()=>{CancelCapture();draft=Preferences.DefaultShortcuts();RefreshRecorders();shortcutMessage.Text=L10n.T("点“应用快捷键”保存默认组合。");}));page.Children.Add(actions);
        shortcutMessage.Text=app.ShortcutError;page.Children.Add(shortcutMessage);page.Children.Add(Text(L10n.T("组合中至少包含 Ctrl、Alt 或 Win，可搭配字母、数字、方向键等。系统保留或已占用的组合可能无法使用。Esc 用于取消录入和退出画笔，F12 为系统保留键。"),11,false,Muted));
    }
    void StartCapture(int index)
    {
        CancelCapture(); app.Engine.End(); app.Keys.Suspend(); recording=index; shortcutMessage.Text=L10n.T("请直接按下组合键，Esc 取消。"); recorders[index].Content=L10n.T("请按快捷键…"); recorders[index].Focus();
    }
    void RefreshRecorders() { for(int i=0;i<4;i++) if(recorders[i] is not null) recorders[i].Content=draft[i].Label; }
    public void CancelCapture()
    {
        if(recording<0) return;
        recording=-1;RefreshRecorders();string? error=app.Keys.Resume(); app.ShortcutError=error??""; if(error is not null) shortcutMessage.Text=error;
    }
    void RecordKey(object sender,KeyEventArgs e)
    {
        if(recording<0) return;
        e.Handled=true; var key=e.Key==Key.System?e.SystemKey:e.Key;
        if(key==Key.Escape) {CancelCapture();shortcutMessage.Text=L10n.T("已取消录入。");return;}
        if(key is Key.LeftAlt or Key.RightAlt or Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin) return;
        var shortcut=new Shortcut(KeyInterop.VirtualKeyFromKey(key),(uint)Keyboard.Modifiers);
        if(!shortcut.Valid) {shortcutMessage.Text=L10n.T("至少包含 Ctrl、Alt 或 Win；Esc 和 F12 不能用作自定义快捷键。");return;}
        draft[recording]=shortcut;CancelCapture();shortcutMessage.Text=L10n.T("已录入，点“应用快捷键”保存。");
    }
    void GeneralPage()
    {
        page.Children.Add(Text(L10n.T("启动设置"),16,true));
        var controls=new StackPanel();
        startupToggle=new CheckBox{Content=L10n.T("登录后自动启动"),Foreground=Brushes.White,Margin=new Thickness(0,0,0,14),FontSize=13};
        startupToggle.Checked+=(_,_)=>ChangeStartup(true);
        startupToggle.Unchecked+=(_,_)=>ChangeStartup(false);
        controls.Children.Add(startupToggle);
        controls.Children.Add(Text(L10n.T("默认关闭。开启后，登录电脑时自动在系统托盘运行，不弹出设置窗口。"),12,false,Muted));
        startupStatus=Text("",12,false,Muted);controls.Children.Add(startupStatus);
        startupRepair=Button(L10n.T("更新自启路径"),()=>ChangeStartup(true));controls.Children.Add(startupRepair);
        controls.Children.Add(Button(L10n.T("打开系统启动应用设置"),()=>{
            try {System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:startupapps"){UseShellExecute=true});}
            catch(Exception) {startupStatus.Text=L10n.T("无法打开系统设置，请在 Windows 设置或任务管理器中查看启动应用。");}
        }));
        page.Children.Add(Card(controls));
        page.Children.Add(Text(L10n.T("只影响当前用户。若系统禁用了 LumaNib 自启，请在启动应用设置中重新允许。"),11,false,Muted));
        page.Children.Add(Text(L10n.T("请把完整程序文件夹放在固定位置后开启；移动文件夹或升级到新路径后，请更新自启路径。外置磁盘需在登录时可访问。"),11,false,Muted));
        RefreshStartup();
    }
    public void RefreshStartup()
    {
        if(startupToggle is null || startupStatus is null || startupRepair is null) return;
        refreshingStartup=true;
        try {
            var state=app.Startup.Read();startupToggle.IsChecked=state.Registered;startupToggle.IsEnabled=true;
            startupRepair.Visibility=state.Registered&&!state.CurrentPath?Visibility.Visible:Visibility.Collapsed;
            startupStatus.Text=state.Registered ? (state.CurrentPath ? L10n.T("已添加登录启动项；若系统禁用此项，将不会自动运行。") : L10n.T("登录启动项指向其他位置，请更新为当前程序路径。")) : L10n.T("登录启动已关闭。");
        } catch(Exception) {
            startupToggle.IsEnabled=false;startupRepair.Visibility=Visibility.Collapsed;
            startupStatus.Text=L10n.T("无法读取当前用户的登录启动设置。");
        } finally {refreshingStartup=false;}
    }
    void ChangeStartup(bool enabled)
    {
        if(refreshingStartup)return;
        string? error=null;
        try {app.Startup.SetEnabled(enabled);}catch(Exception ex) {error=L10n.T("无法更改登录启动设置：")+ex.Message;}
        RefreshStartup();
        if(error is not null && startupStatus is not null)startupStatus.Text=error;
    }

    void HelpPage()
    {
        var controls=new StackPanel();
        void Step(string title,string body) {controls.Children.Add(Text(title,14,true));controls.Children.Add(Text(body,12,false,Muted));}
        Step(L10n.T("1  圆环跟随，左右键分别发光"),L10n.T("到“鼠标圆环”调整两侧颜色和大小，未点击时也分别显示所选颜色。"));Divider(controls);
        Step(L10n.T("2  按快捷键，左键拖动画线"),L10n.F("按 {0} 进入画笔，Esc 退出。退出后笔迹保留，不会挡住鼠标点击。",P.Shortcuts[1].Label));Divider(controls);
        Step(L10n.T("3  OBS 捕获整个显示器"),L10n.T("使用“显示器采集”。窗口采集和游戏采集可能遗漏效果；先录 10 秒确认圆环、笔迹和声音。"));Divider(controls);
        Step(L10n.T("4  关闭设置，继续在系统托盘运行"),L10n.T("双击托盘图标可打开设置，右键可开关效果或退出。退出程序会清除屏幕笔迹。"));
        page.Children.Add(Card(controls));
        page.Children.Add(Text(L10n.T("普通桌面和无边框窗口是主要使用场景。独占全屏、管理员程序、UAC 安全桌面可能限制覆盖或输入监听。"),11,false,Muted));
        page.Children.Add(Text(L10n.T("LumaNib 不录屏，不主动联网，不保存笔迹或键盘输入内容。点击项目地址时会在浏览器中打开 GitHub。设置保存在当前用户的本地应用数据目录。"),11,false,Muted));
        page.Children.Add(Text("LumaNib 1.59 · Windows x64 · Windows 10 / 11",12,false,Muted));
        page.Children.Add(Text(L10n.T("© 2026 宫文 · 保留所有权利"),12,false,Muted));
        var projectLine=new TextBlock{FontSize=12,Margin=new Thickness(0,0,0,10)};
        projectLine.Inlines.Add(new System.Windows.Documents.Run(L10n.T("项目地址：")));
        var projectLink=new System.Windows.Documents.Hyperlink(new System.Windows.Documents.Run("github.com/GongWenAI/LumaNib")) {
            NavigateUri=new Uri("https://github.com/GongWenAI/LumaNib"), Foreground=Paint.Brush(Accent)
        };
        projectLink.RequestNavigate+=(_,e)=>{
            e.Handled=true;
            try {System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri){UseShellExecute=true});}
            catch(Exception) {MessageBox.Show(L10n.T("无法打开浏览器，请访问：https://github.com/GongWenAI/LumaNib"),"LumaNib");}
        };
        projectLine.Inlines.Add(projectLink);page.Children.Add(projectLine);
        page.Children.Add(Text(L10n.T("此版本尚待 Windows 实机测试，Win11 未实机验证。"),11,false,Muted));
    }
    const string Styles="""
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
 <Style TargetType="Button">
  <Setter Property="Background" Value="#2B303E"/><Setter Property="Foreground" Value="#F3F5FA"/>
  <Setter Property="Padding" Value="13,8"/><Setter Property="BorderThickness" Value="0"/><Setter Property="Cursor" Value="Hand"/>
  <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="Button">
   <Border x:Name="Box" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" CornerRadius="7" Padding="{TemplateBinding Padding}">
    <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center"/>
   </Border>
   <ControlTemplate.Triggers><Trigger Property="IsMouseOver" Value="True"><Setter TargetName="Box" Property="Opacity" Value="0.8"/></Trigger>
    <Trigger Property="IsKeyboardFocused" Value="True"><Setter TargetName="Box" Property="BorderBrush" Value="#33D9F2"/><Setter TargetName="Box" Property="BorderThickness" Value="1"/></Trigger>
    <Trigger Property="IsEnabled" Value="False"><Setter TargetName="Box" Property="Opacity" Value="0.35"/></Trigger>
   </ControlTemplate.Triggers>
  </ControlTemplate></Setter.Value></Setter>
 </Style>
 <Style TargetType="ToolTip"><Setter Property="Background" Value="#292F3D"/><Setter Property="Foreground" Value="White"/></Style>
</ResourceDictionary>
""";
}
