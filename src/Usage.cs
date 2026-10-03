using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
namespace GameCoach {
public class UsageEvent {public string id,provider,operation,model,outcome;public DateTime time;public double milliseconds;public long? input,output,cached,audioInput,audioOutput;}
// Only timings, model IDs and provider-reported usage are retained. No prompts, audio or credentials.
static class UsageMeter {
 static readonly object gate=new object();static string loadedPath="";static List<UsageEvent> events=new List<UsageEvent>();
 static readonly DateTime session=DateTime.UtcNow;internal static DateTime Session{get{return session;}}
 static string FileName{get{return Path.Combine(Store.Root,"usage-history.json");}}
 static void Load(){if(loadedPath==FileName)return;loadedPath=FileName;events=new List<UsageEvent>();try{var file=new FileInfo(loadedPath);if(file.Exists&&file.Length<6*1024*1024)events=Store.Json.Deserialize<List<UsageEvent>>(File.ReadAllText(loadedPath))??events;}catch{}}
 internal static UsageEvent[] Read(){lock(gate){Load();return events.ToArray();}}
 internal static void Add(UsageEvent item){lock(gate){try{Load();events.Add(item);if(events.Count>5000)events.RemoveRange(0,events.Count-5000);string temp=FileName+".tmp";File.WriteAllText(temp,Store.Json.Serialize(events));if(File.Exists(FileName))File.Replace(temp,FileName,null);else File.Move(temp,FileName);}catch(Exception ex){Diagnostics.Log("Usage recording deferred: "+ex.GetType().Name);}}}
 internal static Dictionary<string,object> Dict(object value){return value as Dictionary<string,object>;}
 internal static object Get(object root,string key){var d=Dict(root);object value;return d!=null&&d.TryGetValue(key,out value)?value:null;}
 static long? Number(object value){if(value==null)return null;try{return Convert.ToInt64(value);}catch{return null;}}
 internal static void Apply(UsageEvent ev,object root){var usage=Get(root,"usage");if(usage==null)return;ev.input=Number(Get(usage,"input_tokens"))??Number(Get(usage,"prompt_tokens"));ev.output=Number(Get(usage,"output_tokens"))??Number(Get(usage,"completion_tokens"));var ins=Get(usage,"input_token_details")??Get(usage,"input_tokens_details")??Get(usage,"prompt_tokens_details");var outs=Get(usage,"output_token_details")??Get(usage,"output_tokens_details")??Get(usage,"completion_tokens_details");ev.cached=Number(Get(ins,"cached_tokens"));ev.audioInput=Number(Get(ins,"audio_tokens"));ev.audioOutput=Number(Get(outs,"audio_tokens"));}
 internal static string Model(string payload){try{return Convert.ToString(Get(Store.Json.DeserializeObject(payload),"model"));}catch{return "unknown";}}
 internal sealed class Request : IDisposable {
  internal readonly UsageEvent Data;readonly Stopwatch clock=Stopwatch.StartNew();readonly CancellationToken token;bool disposed;
  internal Request(string provider,string operation,string model,CancellationToken token){this.token=token;Data=new UsageEvent{id=Guid.NewGuid().ToString("N"),time=DateTime.UtcNow,provider=provider,operation=operation,model=model,outcome="failed / incomplete"};}
  internal void Observe(object root){Apply(Data,root);}internal void Success(){Data.outcome="completed";}
  public void Dispose(){if(disposed)return;disposed=true;Data.milliseconds=clock.Elapsed.TotalMilliseconds;if(token.IsCancellationRequested&&Data.outcome!="completed")Data.outcome="cancelled";Add(Data);}
 }
 internal static Request Begin(string provider,string operation,string model,CancellationToken token){return new Request(provider,operation,model,token);}
}
class UsagePanel : UserControl {
 readonly Label totals,description,runtime;readonly UsageChart chart;readonly ListView recent;readonly ComboBox period,metric;readonly System.Windows.Forms.Timer refresh=new System.Windows.Forms.Timer{Interval=2000};bool popout;
 public UsagePanel(bool window=false){popout=window;AutoScroll=true;Dock=DockStyle.Fill;BackColor=Theme.Bg;ForeColor=Theme.Text;Padding=new Padding(20);var table=Theme.Stack();table.Dock=DockStyle.Fill;table.AutoSize=false;Controls.Add(table);Theme.ScrollLayout(this,table,560);
  Theme.Row(table,Theme.Heading("Activity",24));Theme.Row(table,Theme.Copy("See which work stays local and which requests use the cloud."));
  var bar=Theme.Flow();period=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=(int)(155*Theme.Scale),AccessibleName="Activity time range"};period.Items.AddRange(new object[]{"This app session","Last hour","Last 24 hours"});period.SelectedIndex=0;metric=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=(int)(195*Theme.Scale),AccessibleName="Graph metric"};metric.Items.AddRange(new object[]{"Model request time (s)","Reported tokens"});metric.SelectedIndex=0;bar.Controls.Add(period);bar.Controls.Add(metric);if(!window)bar.Controls.Add(Theme.Action("Pop out graph","chart","Open the activity view in a separate window. Viewing the graph makes no model requests.",(s,e)=>{var f=new Form{Text="Game Coach · Hybrid activity",Icon=AppGlyph.WindowIcon(),ClientSize=new Size(Math.Min((int)(980*Theme.Scale),Screen.PrimaryScreen.WorkingArea.Width-50),Math.Min((int)(700*Theme.Scale),Screen.PrimaryScreen.WorkingArea.Height-70)),MinimumSize=new Size((int)(700*Theme.Scale),(int)(530*Theme.Scale)),StartPosition=FormStartPosition.CenterParent,BackColor=Theme.Bg,Font=Font};f.Controls.Add(new UsagePanel(true));f.Show(FindForm());}));Theme.Row(table,bar);
  totals=Theme.Heading("No recorded requests yet",16);Theme.Row(table,totals);runtime=Theme.Copy("");Theme.Row(table,runtime);
  chart=new UsageChart{Dock=DockStyle.Fill,MinimumSize=new Size(300,170)};int row=table.RowCount++;table.RowStyles.Add(new RowStyle(SizeType.Percent,58));table.Controls.Add(chart,0,row);
  description=Theme.Copy("Mint = local model  ·  Gold = cloud API. Request time includes queues and transfer; it is not GPU utilization.");Theme.Row(table,description);
  recent=new ListView{Dock=DockStyle.Fill,View=View.Details,FullRowSelect=true,GridLines=false,BackColor=Theme.Card,ForeColor=Theme.Text,BorderStyle=BorderStyle.None,HeaderStyle=ColumnHeaderStyle.Nonclickable,MinimumSize=new Size(0,100)};recent.OwnerDraw=true;recent.DrawColumnHeader+=(s,e)=>{using(var brush=new SolidBrush(Theme.Line))e.Graphics.FillRectangle(brush,e.Bounds);TextRenderer.DrawText(e.Graphics,e.Header.Text,recent.Font,new Rectangle(e.Bounds.X+6,e.Bounds.Y,e.Bounds.Width-8,e.Bounds.Height),Theme.Text,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);};recent.DrawItem+=(s,e)=>e.DrawDefault=true;recent.DrawSubItem+=(s,e)=>e.DrawDefault=true;foreach(var c in new[]{"Time","Service / task","Seconds","Tokens in / out","Outcome"})recent.Columns.Add(c);row=table.RowCount++;table.RowStyles.Add(new RowStyle(SizeType.Percent,42));table.Controls.Add(recent,0,row);recent.SizeChanged+=(s,e)=>{int unit=(int)(70*Theme.Scale),w=Math.Max(unit*9,recent.ClientSize.Width-20);int[] widths={unit,w-unit*6,unit,unit*2,unit*2};for(int i=0;i<5;i++)recent.Columns[i].Width=widths[i];};
  Theme.Row(table,Theme.Copy("Measured from this update onward · latest 5,000 requests retained. Unreported tokens are shown as —. Hover a request for the model used."));
  Theme.Hint(period,"Choose the time window shown in the graph and request list.");Theme.Hint(metric,"Request time includes inference, queueing and transfer. Tokens are included only when the provider reports them.");Theme.Hint(chart,"Mint bars are local inference; gold bars are cloud requests. This is request activity, not GPU utilization.");Theme.StyleInputs(this);period.SelectedIndexChanged+=(s,e)=>UpdateView();metric.SelectedIndexChanged+=(s,e)=>UpdateView();refresh.Tick+=(s,e)=>{if(Visible)UpdateView();};refresh.Start();VisibleChanged+=(s,e)=>{if(Visible)UpdateView();};UpdateView();
 }
 void UpdateView(){DateTime end=DateTime.UtcNow,start=period.SelectedIndex==0?UsageMeter.Session:period.SelectedIndex==1?end.AddHours(-1):end.AddHours(-24);var events=UsageMeter.Read().Where(x=>x.time>=start&&x.time<=end).ToArray();var local=events.Where(x=>x.provider=="local").ToArray();var cloud=events.Where(x=>x.provider=="cloud").ToArray();int count=local.Length+cloud.Length;double share=count==0?0:100.0*local.Length/count;long known=cloud.Sum(x=>(x.input??0)+(x.output??0));
  totals.Text=local.Length+" local requests     ·     "+cloud.Length+" cloud requests     ·     "+(count==0?"—":share.ToString("0")+"%")+" of requests local";
  runtime.Text="Cloud tokens reported: "+known.ToString("N0")+" ("+cloud.Count(x=>x.input.HasValue||x.output.HasValue)+"/"+cloud.Length+" requests)     |     Local request time: "+local.Sum(x=>x.milliseconds/1000).ToString("0.0")+" s";
  chart.Samples=events;chart.From=start;chart.To=end;chart.Tokens=metric.SelectedIndex==1;chart.Invalidate();
  recent.BeginUpdate();recent.Items.Clear();foreach(var x in events.Reverse().Take(40)){var item=new ListViewItem(x.time.ToLocalTime().ToString("HH:mm:ss"));item.SubItems.Add(x.provider+" · "+x.operation);item.SubItems.Add((x.milliseconds/1000).ToString("0.00"));item.SubItems.Add((x.input.HasValue?x.input.Value.ToString("N0"):"—")+" / "+(x.output.HasValue?x.output.Value.ToString("N0"):"—"));item.SubItems.Add(x.outcome);item.ToolTipText=x.model;recent.Items.Add(item);}recent.ShowItemToolTips=true;recent.EndUpdate();
 }
 protected override void Dispose(bool disposing){if(disposing)refresh.Dispose();base.Dispose(disposing);}
}
class UsageChart : Control {
 internal UsageEvent[] Samples=new UsageEvent[0];internal DateTime From,To;internal bool Tokens;
 internal UsageChart(){DoubleBuffered=true;BackColor=Theme.Card;}
 protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;int left=52,top=25,w=Math.Max(80,Width-75),h=Math.Max(55,Height-62),bins=30;var local=new double[bins];var cloud=new double[bins];double span=Math.Max(60,(To-From).TotalSeconds);
  foreach(var x in Samples){int b=Math.Min(bins-1,Math.Max(0,(int)((x.time-From).TotalSeconds/span*bins)));double v=Tokens?(x.input??0)+(x.output??0):x.milliseconds/1000;if(x.provider=="local")local[b]+=v;else if(x.provider=="cloud")cloud[b]+=v;}
  double max=Math.Max(1,Math.Max(local.Max(),cloud.Max()));using(var pen=new Pen(Theme.Line))using(var muted=new SolidBrush(Theme.Muted))using(var mint=new SolidBrush(Theme.Mint))using(var gold=new SolidBrush(Theme.Gold)){
   for(int i=0;i<=4;i++){float y=top+h-h*i/4f;g.DrawLine(pen,left,y,left+w,y);g.DrawString((max*i/4).ToString("0.#"),Font,muted,2,y-8);}
   float step=(float)w/bins;for(int i=0;i<bins;i++){float a=(float)(local[i]/max*h),b=(float)(cloud[i]/max*h);g.FillRectangle(mint,left+i*step+1,top+h-a,Math.Max(2,step*.37f),a);g.FillRectangle(gold,left+i*step+step*.45f,top+h-b,Math.Max(2,step*.37f),b);}
   string format=(To-From).TotalHours>=12?"MMM d HH:mm":"HH:mm";string end=To.ToLocalTime().ToString(format);g.DrawString(From.ToLocalTime().ToString(format),Font,muted,left,top+h+9);g.DrawString(end,Font,muted,left+w-g.MeasureString(end,Font).Width,top+h+9);
   if(Samples.Length==0)g.DrawString("Activity will appear as you play",Font,muted,left+25,top+h*.45f);
  }
 }
}
}
