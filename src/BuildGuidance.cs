using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
namespace GameCoach {
static class BoonReasoning {
 internal const string Instructions=" RELIABLE COMPARISON: Compare every readable offer using its displayed rarity, values, trigger, interval, cost, coverage and replacement loss. Missing rarity is unknown. Higher rarity matters through its actual stronger effect; prefer it when usefulness is comparable. A lower-rarity winner needs a concrete advantage over the strongest higher-rarity rival. Rank dependable marginal power for the equipped weapon, current rotation, Magick and survival before speculative chains. Evaluate resource needs against current equipped recovery before adding more Magick. Maximum Magick is burst capacity, not recovery rate. Cardio Gain supplies conditional Attack/Special on-hit recovery; do not assume unlimited sustain, but do not prefer Silver Wheel solely for Omega use when no capacity shortfall is evident. A checkpoint pool value does not measure combat resource pressure. Additional healing, resource supply or long-term scaling needs a concrete marginal benefit over damage or Guardian survival. A maximum is not expected damage: 'up to 3' does not guarantee three hits, and a chance to repeat must retain that uncertainty. Retaliation requires taking damage: treat it as insurance, never a reason to deliberately get hit or a primary damage engine. Compare triggers per useful combat window, not single-hit numbers from unrelated actions. A repeat interval is not a cooldown for the whole boon. Read area versus single-target coverage literally. Two independent damage effects are not automatically synergistic; name the verified interaction or describe them as complementary. Kill-triggered blasts are not automatic boss damage without adds. An owned boon upgrade replaces its old values; never add both copies. A selected route is a flexible target, not ownership or a reason to discard a better existing core. State the winner and the decisive practical tradeoff briefly. Record visible options without inventing stats. Web is only for a specific unresolved mechanic that could change the decision or an explicit research request.";
 // These distinctions come from the installed TraitText, not older community wording.
 internal static string LocalFacts(){var n=BuildPlanner.Names;var ids=new[]{"ZeusSprintBoon","CastAnywhereBoon","BoltRetaliateBoon"};return " INSTALLED MECHANICS (displayed offer numbers take precedence): "+string.Join("; ",ids.Select(id=>n.Name(id)+": "+n.Description(id,new Dictionary<string,object>())))+". Lightning Lance targets a binding circle and hits foes within it; it is not limited to one foe. Divine Vengeance is conditional retaliation with probabilistic extra strikes, not guaranteed maximum burst. Thunder Rush can repeatedly affect surrounding foes while rushing; value its actual uptime.";}
 internal static LocalScene Guard(LocalScene s,SavedBuild saved=null){if(s==null||s.scene!="choice")return s;string pick=OfferMemory.Identity(s.pick);var n=BuildPlanner.Names;if(saved!=null&&(saved.owned??new SavedTrait[0]).Any(t=>OfferMemory.Identity(t.name)==pick)){s.why="Compare the displayed improvement with your existing boon. This upgrades its values; it does not add another stacking copy.";return s;}
  // These short explanations retain the decisive trigger/coverage conditions
  // even when the generative comparison invents unsupported synergy or totals.
  if(pick==OfferMemory.Identity(n.Name("BoltRetaliateBoon")))s.why="Retaliation insurance after taking damage; additional lightning strikes are chance-based. It supplements your damage without improving safe, repeatable attacks.";
  else if(pick==OfferMemory.Identity(n.Name("CastAnywhereBoon")))s.why="Aim your binding circle safely at range and strike foes within it. Adds controllable placement without needing to take damage.";
  else if(pick==OfferMemory.Identity(n.Name("ZeusSprintBoon")))s.why="Repeated lightning while rushing near foes. Its value depends on time spent rushing in range, rather than one bolt's damage.";
  return s;
 }

}
class BuildMark {internal string title,name,why;internal RectangleF anchor;internal int score;}
public class DoorReward {public string reward;public double confidence,x,y,width,height;}
static class BuildGuidance {
 internal static readonly string[] Rewards={"Zeus","Poseidon","Apollo","Hera","Aphrodite","Demeter","Hephaestus","Hestia","Ares","Hammer","Pom","Heart","Magick","Gold","Shop","Unknown"};
 internal static BuildMark Boon(BoonMenuRead menu,LocalScene advice,SavedBuild b,CoachJournal j){
  var r=BuildPlanner.Selected(b,j);if(menu==null||r==null||!BuildPlanner.Matches(r,b))return null;var targets=BuildPlanner.EffectiveTargets(r,b);var view=BuildPlanner.View(r,b);var marks=new List<BuildMark>();
  for(int i=0;i<targets.Length;i++)foreach(var id in BuildPlanner.AvailableIds(targets[i],b)){
   string name=BuildPlanner.Names.Name(id);if(!menu.Names.Contains(OfferMemory.Identity(name)))continue;var line=menu.Lines.FirstOrDefault(l=>BoonMenuRead.CanonicalName(Regex.Replace(l.Text,@"\b(common|rare|epic|heroic|legendary|duo)\b","",RegexOptions.IgnoreCase))==OfferMemory.Identity(name));if(line==null)continue;
   var step=view.steps[i];bool owned=new[]{"OWNED","ADAPTED","REPORTED"}.Contains(step.status)&&(step.target==name||step.target.StartsWith(name+" (",StringComparison.Ordinal));bool next=step.status=="SEEK";bool winner=advice!=null&&OfferMemory.Identity(advice.pick)==OfferMemory.Identity(name);
   marks.Add(new BuildMark{title=owned?"Upgrade option":next?"Build target":winner?"Build-fit swap":"Compare the swap",name=name,why=owned?"Already in your route · compare the new value":next?targets[i].role+" · "+r.title:"Keep your current core unless this offer improves it",score=(next?100:owned?60:30)-i+(winner?15:0),anchor=new RectangleF((float)line.X,(float)line.Y,(float)line.Width,(float)line.Height)});
  }return marks.OrderByDescending(m=>m.score).FirstOrDefault();
 }
 internal static bool ValidDoor(DoorReward d){return d!=null&&Rewards.Contains(d.reward)&&d.reward!="Unknown"&&!new[]{d.confidence,d.x,d.y,d.width,d.height}.Any(v=>double.IsNaN(v)||double.IsInfinity(v))&&d.confidence>=.92&&d.confidence<=1&&d.x>=.03&&d.y>=.10&&d.width>=.018&&d.width<=.14&&d.height>=.018&&d.height<=.18&&d.x+d.width<=.97&&d.y+d.height<=.88;}
 internal static BuildMark Door(DoorReward[] doors,SavedBuild b,CoachJournal j){
  var r=BuildPlanner.Selected(b,j);if(r==null||!BuildPlanner.Matches(r,b)||doors==null||doors.Length<2||doors.Length>5||doors.Any(d=>!ValidDoor(d)))return null;
  for(int i=0;i<doors.Length;i++)for(int k=i+1;k<doors.Length;k++)if(Math.Abs(doors[i].x-doors[k].x)<.06&&Math.Abs(doors[i].y-doors[k].y)<.06)return null;
  var view=BuildPlanner.View(r,b);var targets=BuildPlanner.EffectiveTargets(r,b);var marks=new List<BuildMark>();foreach(var d in doors){int score=0;string why="",title="Next boon source";
   for(int i=0;i<targets.Length;i++)if(view.steps[i].status=="SEEK"&&BuildPlanner.AvailableIds(targets[i],b).Any(id=>BuildPlanner.God(id)==d.reward)){int value=100-i*8;if(value>score){score=value;why=targets[i].role+" options · rolls vary";}}
   if(d.reward=="Hammer"&&r.hammers.Length>0){score=Math.Max(score,75);why="Weapon upgrade opportunity · compare the offers";title="Develop your weapon";}
   if(d.reward=="Heart"&&b.maxHealth>0&&b.health/b.maxHealth<.4){score=125;why="Low Life · prioritize a survivable route";title="Survival first";}
   if(d.reward=="Pom"&&view.steps.Any(s=>s.status=="OWNED"||s.status=="ADAPTED"||s.status=="REPORTED")){score=45;why="Level an eligible boon · inspect the available upgrades";title="Grow your core";}
   if(score>0)marks.Add(new BuildMark{title=title,name=d.reward,why=why,score=score,anchor=new RectangleF((float)d.x,(float)d.y,(float)d.width,(float)d.height)});
  }var ranked=marks.OrderByDescending(m=>m.score).ToArray();if(ranked.Length==0)return null;return ranked[0];
 }
}
// Track only a small, model-identified reward icon. A moved/vanished icon hides
// advice immediately; no extrapolation through camera cuts, menus or loading.
sealed class RewardAnchor : IDisposable {
 readonly Bitmap patch;readonly RectangleF original;readonly bool textured;RectangleF current;int hits;internal DateTime Seen;internal RectangleF Current{get{return current;}}
 internal RewardAnchor(Bitmap frame,RectangleF area){original=current=area;using(var small=Resize(frame)){var box=Pixels(area,small.Size);patch=small.Clone(box,System.Drawing.Imaging.PixelFormat.Format24bppRgb);}double sum=0,squares=0;int count=0;for(int y=0;y<patch.Height;y+=2)for(int x=0;x<patch.Width;x+=2){var c=patch.GetPixel(x,y);double v=(c.R+c.G+c.B)/3.0;sum+=v;squares+=v*v;count++;}textured=count>0&&Math.Sqrt(Math.Max(0,squares/count-Math.Pow(sum/count,2)))>18;Seen=DateTime.UtcNow;}
 static Bitmap Resize(Bitmap frame){var b=new Bitmap(640,Math.Max(1,640*frame.Height/frame.Width));using(var g=Graphics.FromImage(b))g.DrawImage(frame,new Rectangle(0,0,b.Width,b.Height),0,0,frame.Width,frame.Height,GraphicsUnit.Pixel);return b;}
 static Rectangle Pixels(RectangleF a,Size s){return Rectangle.Intersect(new Rectangle(0,0,s.Width,s.Height),new Rectangle((int)(a.X*s.Width),(int)(a.Y*s.Height),Math.Max(4,(int)(a.Width*s.Width)),Math.Max(4,(int)(a.Height*s.Height))));}
 internal bool Match(Bitmap frame){if(!textured)return false;using(var small=Resize(frame)){var pos=Pixels(current,small.Size);double best=255;Point point=pos.Location;for(int dy=-9;dy<=9;dy+=3)for(int dx=-9;dx<=9;dx+=3){int x=pos.X+dx,y=pos.Y+dy;if(x<0||y<0||x+patch.Width>=small.Width||y+patch.Height>=small.Height)continue;double sum=0;int count=0;for(int yy=1;yy<patch.Height;yy+=3)for(int xx=1;xx<patch.Width;xx+=3){var a=patch.GetPixel(xx,yy);var b=small.GetPixel(x+xx,y+yy);sum+=Math.Abs(a.R-b.R)+Math.Abs(a.G-b.G)+Math.Abs(a.B-b.B);count+=3;}double error=sum/Math.Max(1,count);if(error<best){best=error;point=new Point(x,y);}}
   if(best>24){hits=0;return false;}current=new RectangleF((float)point.X/small.Width,(float)point.Y/small.Height,original.Width,original.Height);Seen=DateTime.UtcNow;return ++hits>=2;
  }}
 public void Dispose(){patch.Dispose();}
}
// A small independent, click-through tag. Physical screen pixels are used for
// both OCR anchors and bounds, avoiding mixed DPI coordinate drift.
class BuildMarkerOverlay : Form {
 [System.Runtime.InteropServices.DllImport("user32.dll")]static extern bool SetWindowDisplayAffinity(IntPtr h,uint a);
 [System.Runtime.InteropServices.DllImport("user32.dll")]static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int w,int height,uint flags);
 internal bool CaptureExcluded;BuildMark mark;Font titleFont,nameFont,bodyFont;float scale;DateTime raise;
 internal BuildMarkerOverlay(){FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;DoubleBuffered=true;AutoScaleMode=AutoScaleMode.None;BackColor=Theme.Card;Opacity=.97;}
 protected override bool ShowWithoutActivation{get{return true;}}
 protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x08000000|0x20|0x80;return p;}}
 protected override void WndProc(ref Message m){if(m.Msg==0x84){m.Result=new IntPtr(-1);return;}base.WndProc(ref m);}
 protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);CaptureExcluded=SetWindowDisplayAffinity(Handle,0x11);}
 internal static Rectangle Placement(Rectangle game,RectangleF anchor,Size card,bool door){int gap=Math.Max(8,game.Width/160);int x=door?game.Left+(int)((anchor.X+anchor.Width/2)*game.Width)-card.Width/2:game.Left+(int)(.82*game.Width);int y=game.Top+(int)(anchor.Y*game.Height)-(door?card.Height+gap:0);if(!door&&x+card.Width>game.Right-gap)x=game.Right-gap-card.Width;if(door&&y<game.Top+gap)y=game.Top+(int)((anchor.Y+anchor.Height)*game.Height)+gap;return new Rectangle(Math.Max(game.Left+gap,Math.Min(x,game.Right-card.Width-gap)),Math.Max(game.Top+gap,Math.Min(y,game.Bottom-card.Height-gap)),card.Width,card.Height);}
 internal void Present(Rectangle game,BuildMark value,bool door){if(value==null){Hide();return;}if(!IsHandleCreated){var h=Handle;}if(!CaptureExcluded)return;float wanted=Math.Max(.8f,Math.Min(1.6f,game.Width/1600f));if(titleFont==null||Math.Abs(wanted-scale)>.01){if(titleFont!=null){titleFont.Dispose();nameFont.Dispose();bodyFont.Dispose();}scale=wanted;titleFont=new Font("Segoe UI Semibold",11*scale,FontStyle.Regular,GraphicsUnit.Pixel);nameFont=new Font("Palatino Linotype",20*scale,FontStyle.Regular,GraphicsUnit.Pixel);bodyFont=new Font("Segoe UI",12*scale,FontStyle.Regular,GraphicsUnit.Pixel);}mark=value;int width=(int)((door?245:260)*scale),pad=(int)(14*scale);int nameHeight=TextRenderer.MeasureText(mark.name,nameFont,new Size(width-pad*2,1000),TextFormatFlags.WordBreak|TextFormatFlags.NoPadding).Height;int bodyHeight=TextRenderer.MeasureText(mark.why,bodyFont,new Size(width-pad*2,1000),TextFormatFlags.WordBreak|TextFormatFlags.NoPadding).Height;Bounds=Placement(game,value.anchor,new Size(width,pad*2+(int)(26*scale)+nameHeight+bodyHeight+(int)(9*scale)),door);Invalidate();if(!Visible)Show();if(DateTime.UtcNow>=raise){SetWindowPos(Handle,new IntPtr(-1),0,0,0,0,0x1|0x2|0x10|0x200);raise=DateTime.UtcNow.AddMilliseconds(750);}}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(mark==null)return;var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;int p=(int)(14*scale);using(var bg=new LinearGradientBrush(ClientRectangle,Color.FromArgb(27,52,48),Theme.Bg,90f))g.FillRectangle(bg,ClientRectangle);using(var pen=new Pen(Theme.Gold,scale))g.DrawRectangle(pen,1,1,Width-3,Height-3);using(var brush=new SolidBrush(Theme.Mint))g.FillRectangle(brush,0,0,4*scale,Height);var flags=TextFormatFlags.WordBreak|TextFormatFlags.NoPadding;TextRenderer.DrawText(g,"◇  "+mark.title.ToUpperInvariant(),titleFont,new Rectangle(p,p,Width-p*2,(int)(20*scale)),Theme.Mint,flags);int y=p+(int)(26*scale);int h=TextRenderer.MeasureText(mark.name,nameFont,new Size(Width-p*2,1000),flags).Height;TextRenderer.DrawText(g,mark.name,nameFont,new Rectangle(p,y,Width-p*2,h),Theme.Gold,flags);y+=h+(int)(9*scale);TextRenderer.DrawText(g,mark.why,bodyFont,new Rectangle(p,y,Width-p*2,Height-y-p),Theme.Text,flags);}
 protected override void Dispose(bool d){if(d&&titleFont!=null){titleFont.Dispose();nameFont.Dispose();bodyFont.Dispose();}base.Dispose(d);}
}
partial class LocalWatcher {
 readonly BuildMarkerOverlay buildMarker=new BuildMarkerOverlay();RewardAnchor doorAnchor;BuildMark doorMark;DateTime doorExpires;
 void HideBuildGuidance(){buildMarker.Hide();if(doorAnchor!=null){doorAnchor.Dispose();doorAnchor=null;}doorMark=null;}
 void PresentBuildBoon(Rectangle bounds){if(!MenuActive||FastActive){buildMarker.Hide();return;}var run=RunMemory.Load();buildMarker.Present(bounds,BuildGuidance.Boon(menuRead,menuAdvice??menuPreview,run.gameState,RunLab.Load()),false);}
 void BeginDoorGuidance(LocalScene scene,BufferedFrame frame){HideBuildGuidance();if(scene.scene!="doors"||scene.confidence<.92||MenuActive||FastActive)return;var run=RunMemory.Load();var mark=BuildGuidance.Door(scene.doors,run.gameState,RunLab.Load());if(mark==null)return;using(var ms=new System.IO.MemoryStream(frame.Image))using(var b=new Bitmap(ms))doorAnchor=new RewardAnchor(b,mark.anchor);doorMark=mark;doorExpires=DateTime.UtcNow.AddSeconds(20);Diagnostics.Log("Build door candidate: "+mark.name+"; checking current reward icon before display.");}
 void TrackBuildGuidance(Bitmap frame,Rectangle bounds){if(doorAnchor==null){if(!MenuActive)buildMarker.Hide();return;}if(MenuActive||FastActive||DateTime.UtcNow>doorExpires){HideBuildGuidance();return;}if(!doorAnchor.Match(frame)){buildMarker.Hide();if(DateTime.UtcNow-doorAnchor.Seen>TimeSpan.FromSeconds(1.5))HideBuildGuidance();return;}doorMark.anchor=doorAnchor.Current;buildMarker.Present(bounds,doorMark,true);}
}
}
