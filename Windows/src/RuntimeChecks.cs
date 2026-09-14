using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LumaNib;
internal static class RuntimeChecks
{
    public static void Run()
    {
        string dir=Path.Combine(Program.DataDirectory,"SelfTest",DateTime.Now.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(dir);
        var report=new StringBuilder();report.AppendLine(L10n.T("LumaNib 1.59 — Windows 本机自检"));report.AppendLine(Environment.OSVersion.ToString());report.AppendLine("64-bit process: "+Environment.Is64BitProcess);
        foreach(var m in Native.Monitors())report.AppendLine($"Display: {m.X},{m.Y} {m.Width}x{m.Height} scale={m.Scale}");
        void Render(string name,FrameworkElement element,int width,int height)
        {
            element.Measure(new Size(width,height));element.Arrange(new Rect(0,0,width,height));element.UpdateLayout();
            var bitmap=new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);bitmap.Render(element);
            var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));using var file=File.Create(Path.Combine(dir,name+".png"));png.Save(file);
            var bytes=new byte[width*height*4];bitmap.CopyPixels(bytes,width*4,0);int visible=0;for(int i=3;i<bytes.Length;i+=4)if(bytes[i]>0)visible++;
            if(visible==0)throw new InvalidOperationException(name+L10n.T(" 渲染为空。"));report.AppendLine(name+": rendered pixels="+visible);
        }
        Render("ring-idle",new RingVisual{Prefs=new Preferences{LeftColor=2,RightColor=3}},180,180);
        Render("ring-left-click",new RingVisual{Prefs=new Preferences{LeftColor=2,RightColor=3},LeftLight=1},180,180);
        Render("pen-glow",new PenPreview{Prefs=new Preferences()},600,120);
        File.WriteAllText(Path.Combine(dir,"report.txt"),report+L10n.T("\n此自检只覆盖 WPF 渲染与显示器枚举，不替代快捷键、画笔操作和 OBS 实录测试。\n"));
        MessageBox.Show(L10n.T("渲染自检已完成，结果保存在：\n")+dir+L10n.T("\n\n请另行测试实际画笔、快捷键和 OBS 录制。"),L10n.T("LumaNib 自检"));
    }
}
