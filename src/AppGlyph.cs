using System;using System.Drawing;using System.Drawing.Drawing2D;using System.IO;
namespace GameCoach {
// Original vector artwork: a listening crescent above the three-way crossroads.
static class AppGlyph {
 internal static string ForPage(string page){switch(page){case "Coach":return "mic";case "Current build":return "layers";case "Sandbox":return "flask";case "Activity":return "chart";case "Run history":return "history";case "Settings":return "settings";case "How to use":return "book";default:return "info";}}
 internal static Bitmap Brand(int size){var b=new Bitmap(size,size);using(var g=Graphics.FromImage(b)){g.SmoothingMode=SmoothingMode.AntiAlias;g.ScaleTransform(size/256f,size/256f);using(var p=new GraphicsPath()){p.AddArc(8,8,72,72,180,90);p.AddArc(176,8,72,72,270,90);p.AddArc(176,176,72,72,0,90);p.AddArc(8,176,72,72,90,90);p.CloseFigure();using(var brush=new LinearGradientBrush(new Point(0,0),new Point(256,256),Color.FromArgb(25,61,65),Color.FromArgb(8,21,29)))g.FillPath(brush,p);using(var pen=new Pen(Color.FromArgb(87,134,123),3))g.DrawPath(pen,p);}
  using(var mint=new SolidBrush(Theme.Mint))using(var dark=new SolidBrush(Color.FromArgb(20,45,50)))using(var gold=new Pen(Theme.Gold,13)){gold.StartCap=gold.EndCap=LineCap.Round;g.FillEllipse(mint,72,35,105,105);g.FillEllipse(dark,103,24,93,94);g.DrawLine(gold,128,137,128,218);g.DrawLine(gold,128,181,69,145);g.DrawLine(gold,128,181,187,145);g.FillEllipse(mint,60,136,18,18);g.FillEllipse(mint,178,136,18,18);g.FillPolygon(mint,new[]{new Point(187,39),new Point(193,52),new Point(207,58),new Point(193,64),new Point(187,78),new Point(181,64),new Point(167,58),new Point(181,52)});}}
 return b;}
 internal static Icon WindowIcon(){string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","game-coach.ico");if(!File.Exists(path))path=Path.Combine(Store.Root,"assets","game-coach.ico");return File.Exists(path)?new Icon(path):SystemIcons.Application;}
 internal static Bitmap Draw(string key,int size,Color color){var b=new Bitmap(Math.Max(16,size),Math.Max(16,size));using(var g=Graphics.FromImage(b)){g.SmoothingMode=SmoothingMode.AntiAlias;g.ScaleTransform(b.Width/24f,b.Height/24f);using(var p=new Pen(color,1.7f)){p.StartCap=p.EndCap=LineCap.Round;p.LineJoin=LineJoin.Round;Action<float,float,float,float> line=(x,y,a,z)=>g.DrawLine(p,x,y,a,z);
 switch(key){case "mic":g.DrawArc(p,8,3,8,11,180,180);line(8,7,8,10);line(16,7,16,10);g.DrawArc(p,8,6,8,9,0,180);g.DrawArc(p,4,6,16,13,0,180);line(12,19,12,22);line(8,22,16,22);break;
 case "play":g.DrawPolygon(p,new[]{new PointF(7,4),new PointF(20,12),new PointF(7,20)});break;
 case "pause":line(8,5,8,19);line(16,5,16,19);break;
 case "mute":g.DrawPolygon(p,new[]{new PointF(3,9),new PointF(7,9),new PointF(12,5),new PointF(12,19),new PointF(7,15),new PointF(3,15)});line(17,9,22,15);line(22,9,17,15);break;
 case "send":g.DrawPolygon(p,new[]{new PointF(3,4),new PointF(22,12),new PointF(3,20),new PointF(7,12)});line(7,12,18,12);break;
 case "layers":for(int y=0;y<3;y++){line(3,8+y*5,12,3+y*5);line(12,3+y*5,21,8+y*5);if(y>0){line(3,8+y*5,12,13+y*5);line(12,13+y*5,21,8+y*5);}}break;
 case "chart":line(4,3,4,21);line(4,21,22,21);line(8,16,8,11);line(13,16,13,7);line(18,16,18,3);break;
 case "history":g.DrawArc(p,4,4,17,17,215,305);line(3,4,3,10);line(3,10,9,10);line(12,7,12,13);line(12,13,16,15);break;
 case "refresh":g.DrawArc(p,4,4,16,16,30,285);line(20,4,20,10);line(20,10,14,10);break;
 case "settings":g.DrawEllipse(p,8,8,8,8);for(int i=0;i<8;i++){double a=i*Math.PI/4;line(12+(float)Math.Cos(a)*8,12+(float)Math.Sin(a)*8,12+(float)Math.Cos(a)*10,12+(float)Math.Sin(a)*10);}break;
 case "flask":line(9,3,15,3);line(10,3,10,9);line(14,3,14,9);g.DrawLines(p,new[]{new Point(10,9),new Point(4,19),new Point(5,21),new Point(19,21),new Point(20,19),new Point(14,9)});line(7,15,17,15);break;
 case "book":g.DrawRectangle(p,4,3,16,18);line(8,3,8,21);line(11,8,17,8);line(11,12,17,12);break;
 case "download":line(12,3,12,15);line(7,11,12,16);line(12,16,17,11);g.DrawLines(p,new[]{new Point(4,17),new Point(4,21),new Point(20,21),new Point(20,17)});break;
 case "share":g.DrawEllipse(p,3,9,5,5);g.DrawEllipse(p,16,2,5,5);g.DrawEllipse(p,16,17,5,5);line(8,10,16,5);line(8,14,16,19);break;
 case "expand":line(6,9,12,15);line(12,15,18,9);break;
 case "collapse":line(6,15,12,9);line(12,9,18,15);break;
 case "check":line(4,12,10,18);line(10,18,21,5);break;
 case "close":line(6,6,18,18);line(6,18,18,6);break;
 default:g.DrawEllipse(p,3,3,18,18);line(12,11,12,17);g.DrawEllipse(p,11.5f,6,1,1);break;}}}return b;}
}
}
