using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace LumaNib;
internal static class Paint
{
    public static readonly string[] Names={L10n.T("冰蓝"),L10n.T("薄荷"),L10n.T("荧黄"),L10n.T("玫红"),L10n.T("橙色"),L10n.T("紫色")};
    public static readonly Color[] Colors={Color.FromRgb(26,217,255),Color.FromRgb(46,255,166),Color.FromRgb(255,227,31),Color.FromRgb(255,56,156),Color.FromRgb(255,122,33),Color.FromRgb(179,102,255)};
    public static SolidColorBrush Brush(Color color,double opacity=1) { var b=new SolidColorBrush(color){Opacity=opacity}; b.Freeze(); return b; }
    static Pen Pen(Color c,double width,double opacity) { var p=new Pen(Brush(c,opacity),width){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round,LineJoin=PenLineJoin.Round}; p.Freeze(); return p; }
    public static void Glow(DrawingContext dc,Geometry path,Color color,double width,double glow,double opacity=1)
    {
        if(glow>0) {
            // Nested translucent strokes approximate a soft halo without full-screen blur surfaces.
            for(int i=8;i>=1;i--) dc.DrawGeometry(null,Pen(color,width+glow*i/4.0,opacity*(0.015+0.04*(9-i)/8.0)),path);
        }
        dc.DrawGeometry(null,Pen(color,width,opacity),path);
        if(glow>0) {
            byte Mix(byte c)=>(byte)(c+(255-c)*0.62);
            dc.DrawGeometry(null,Pen(Color.FromRgb(Mix(color.R),Mix(color.G),Mix(color.B)),Math.Max(0.7,width*0.3),opacity),path);
        }
    }
    public static Geometry Arc(Point center,double radius,bool left)
    {
        var g=new StreamGeometry();
        using(var c=g.Open()) {
            double start=left?98:-82;
            Point At(double a)=>new(center.X+radius*Math.Cos(a*Math.PI/180),center.Y+radius*Math.Sin(a*Math.PI/180));
            c.BeginFigure(At(start),false,false);
            c.ArcTo(At(start+164),new Size(radius,radius),0,false,SweepDirection.Clockwise,true,false);
        }
        g.Freeze(); return g;
    }
    public static void Ring(DrawingContext dc,Point center,Preferences prefs,double left,double right)
    {
        foreach(bool half in new[]{true,false}) {
            double active=half?left:right;
            var path=Arc(center,prefs.RingRadius,half);
            dc.DrawGeometry(null,Pen(System.Windows.Media.Colors.Black,prefs.RingWidth+2,0.25),path);
            Glow(dc,path,Colors[half?prefs.LeftColor:prefs.RightColor],prefs.RingWidth+active*1.2,active>0?4+active*10:0,0.52+active*0.48);
        }
    }
    public static Geometry Line(IReadOnlyList<ScreenPoint> points,int start,int end,double x,double y,double scale)
    {
        var g=new StreamGeometry();
        using(var c=g.Open()) {
            var first=points[start].Local(x,y,scale);
            c.BeginFigure(new Point(first.X,first.Y),false,false);
            if(start==end) c.LineTo(new Point(first.X+0.01,first.Y),true,false);
            else for(int i=start+1;i<=end;i++) { var p=points[i].Local(x,y,scale); c.LineTo(new Point(p.X,p.Y),true,false); }
        }
        g.Freeze(); return g;
    }
}
internal sealed class RingVisual : FrameworkElement
{
    public Preferences Prefs { get; set; }=new();
    public double LeftLight { get; set; }
    public double RightLight { get; set; }
    protected override void OnRender(DrawingContext dc) => Paint.Ring(dc,new Point(ActualWidth/2,ActualHeight/2),Prefs,LeftLight,RightLight);
}
internal sealed class PenPreview : FrameworkElement
{
    public Preferences Prefs { get; set; }=new();
    protected override void OnRender(DrawingContext dc)
    {
        var points=Enumerable.Range(0,181).Select(i=>new ScreenPoint(32+i/180.0*(ActualWidth-64),ActualHeight/2+Math.Sin(i/180.0*Math.PI*3.6)*19)).ToList();
        Paint.Glow(dc,Paint.Line(points,0,180,0,0,1),Paint.Colors[Prefs.PenColor],Prefs.PenWidth,Prefs.PenGlow);
    }
}
internal sealed class InkVisual : FrameworkElement
{
    sealed class CachedStroke
    {
        public ContainerVisual Root=new();
        public List<DrawingVisual> Parts=new();
        public int PointCount;
    }
    const int Chunk=512;
    readonly VisualCollection children;
    readonly Dictionary<Guid,CachedStroke> cache=new();
    public MonitorInfo Monitor { get; set; }
    double scale;
    public InkVisual(MonitorInfo monitor) { Monitor=monitor; children=new VisualCollection(this); IsHitTestVisible=false; ClipToBounds=true; }
    protected override int VisualChildrenCount=>children.Count;
    protected override Visual GetVisualChild(int index)=>children[index];
    public void Sync(InkModel model,double now)
    {
        double actualScale=VisualTreeHelper.GetDpi(this).DpiScaleX;
        if(Math.Abs(actualScale-scale)>0.001) { children.Clear(); cache.Clear(); scale=actualScale; }
        var all=model.Strokes.Concat(model.Current is null?Array.Empty<Stroke>():new[]{model.Current}).ToArray();
        var ids=all.Select(s=>s.Id).ToHashSet();
        foreach(var id in cache.Keys.Where(id=>!ids.Contains(id)).ToArray()) { children.Remove(cache[id].Root); cache.Remove(id); }
        foreach(var stroke in all) {
            if(!cache.TryGetValue(stroke.Id,out var item)) { item=new CachedStroke(); cache.Add(stroke.Id,item); children.Add(item.Root); }
            item.Root.Opacity=stroke.Opacity(now);
            if(item.PointCount==stroke.Points.Count) continue;
            int oldPart=Math.Max(0,(item.PointCount-2)/Chunk);
            int parts=Math.Max(1,(stroke.Points.Count-2)/Chunk+1);
            while(item.Parts.Count<parts) { var visual=new DrawingVisual(); item.Parts.Add(visual); item.Root.Children.Add(visual); }
            for(int n=oldPart;n<parts;n++) {
                int start=n*Chunk,end=Math.Min(stroke.Points.Count-1,start+Chunk);
                using(var dc=item.Parts[n].RenderOpen()) Paint.Glow(dc,Paint.Line(stroke.Points,start,end,Monitor.X,Monitor.Y,scale),Paint.Colors[stroke.Color],stroke.Width/scale,stroke.Glow/scale);
            }
            item.PointCount=stroke.Points.Count;
        }
    }
}
