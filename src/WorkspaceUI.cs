using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace GameCoach {
static class Theme {
 internal static readonly ToolTip Tips=new ToolTip{AutoPopDelay=18000,InitialDelay=420,ReshowDelay=120,ShowAlways=true};
 internal static int P(float value){return (int)Math.Round(value*Scale);}
 internal static void Hint(Control c,string text){if(c.AccessibleDescription==text)return;c.AccessibleDescription=text;Tips.SetToolTip(c,text);}
 internal static Button Action(string text,string icon,string hint,EventHandler click,bool primary=false){var b=Button(text,click);b.Image=AppGlyph.Draw(icon,P(18),primary?Bg:Mint);b.ImageAlign=ContentAlignment.MiddleLeft;b.TextImageRelation=TextImageRelation.ImageBeforeText;Hint(b,hint);if(primary){b.BackColor=Mint;b.ForeColor=Bg;b.FlatAppearance.BorderColor=Mint;}return b;}
 internal static TableLayoutPanel Section(string title,string caption=""){var card=Stack();card.BackColor=Card;card.Padding=new Padding(P(18));card.Margin=new Padding(0,0,0,P(14));Row(card,Heading(title,14));if(caption.Length>0)Row(card,Copy(caption));return card;}
 internal static Control Fold(string title,string explanation,Control body){var box=Stack();var toggle=Action(title,"expand",explanation,null);body.Visible=false;toggle.Click+=(s,e)=>{body.Visible=!body.Visible;var old=toggle.Image;toggle.Image=AppGlyph.Draw(body.Visible?"collapse":"expand",P(18),Mint);if(old!=null)old.Dispose();};Row(box,toggle);Row(box,body);return box;}
 internal static void Reflow(Control root){root.SuspendLayout();ReflowLabels(root);root.ResumeLayout(true);}
 static void ReflowLabels(Control root){foreach(Control child in root.Controls){var label=child as Label;if(label!=null&&label.AutoSize&&label.MaximumSize.Width>0){var size=new Size(Math.Max(150,root.ClientSize.Width-root.Padding.Horizontal-12),0);if(label.MaximumSize!=size)label.MaximumSize=size;}ReflowLabels(child);}}

 internal static float Scale=1;
 internal static readonly Color Bg=Color.FromArgb(12,20,25),Card=Color.FromArgb(21,33,39),Text=Color.FromArgb(236,240,236),Muted=Color.FromArgb(155,174,177),Mint=Color.FromArgb(126,221,183),Gold=Color.FromArgb(221,199,146),Line=Color.FromArgb(44,62,68);
 internal static TableLayoutPanel Stack(int columns=1){var t=new TableLayoutPanel{ColumnCount=columns,RowCount=0,Dock=DockStyle.Top,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Margin=new Padding(0)};for(int i=0;i<columns;i++)t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100f/columns));return t;}
 internal static void Row(TableLayoutPanel t,Control c){int row=t.RowCount++;t.RowStyles.Add(new RowStyle(SizeType.AutoSize));c.Dock=DockStyle.Top;c.Margin=new Padding(0,0,0,10);t.Controls.Add(c,0,row);}
 internal static Label Copy(string text){return new WrappingLabel{Text=text,UseMnemonic=false,AutoSize=true,ForeColor=Muted,Margin=new Padding(0,0,0,8)};}

 internal static Label Heading(string text,float size){var l=Copy(text);l.Font=new Font("Segoe UI Semibold",size);l.ForeColor=Text;return l;}
 internal static FlowLayoutPanel Flow(){return new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Dock=DockStyle.Top,WrapContents=true,Margin=new Padding(0)};}
 internal static Button Button(string text,EventHandler click){var b=new CalmButton{Text=text,UseMnemonic=false,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Padding=new Padding(P(11),P(6),P(11),P(6)),Margin=new Padding(0,0,P(8),P(6)),FlatStyle=FlatStyle.Flat,BackColor=Card,ForeColor=Text,Cursor=Cursors.Hand,AccessibleName=text};b.FlatAppearance.BorderColor=Line;b.FlatAppearance.MouseOverBackColor=Color.FromArgb(35,54,59);b.Click+=click;return b;}
 internal static RichTextBox ReadBox(){return new RichTextBox{Dock=DockStyle.Fill,ReadOnly=true,BackColor=Card,ForeColor=Text,BorderStyle=BorderStyle.None,Font=new Font("Segoe UI",11),ScrollBars=RichTextBoxScrollBars.Vertical,DetectUrls=true,Margin=new Padding(8)};}
 internal static Panel Page(){return new Panel{Dock=DockStyle.Fill,BackColor=Bg,ForeColor=Text,Padding=new Padding(20),AutoScroll=true};}
 internal static void ScrollLayout(ScrollableControl owner,TableLayoutPanel table,int minimum){int height=(int)(minimum*Scale);owner.AutoScroll=true;owner.AutoScrollMinSize=new Size(0,height+owner.Padding.Vertical);table.Dock=DockStyle.Top;table.AutoSize=false;table.MinimumSize=new Size(0,height);Action fit=()=>table.Height=Math.Max(height,owner.ClientSize.Height-owner.Padding.Vertical);owner.SizeChanged+=(s,e)=>fit();owner.VisibleChanged+=(s,e)=>fit();fit();}
 internal static void StyleInputs(Control root){foreach(Control c in root.Controls){if(c is TextBox||c is ComboBox){c.BackColor=Card;c.ForeColor=Text;if(c is TextBox)((TextBox)c).BorderStyle=BorderStyle.FixedSingle;if(c is ComboBox)((ComboBox)c).FlatStyle=FlatStyle.Flat;}StyleInputs(c);}}
}
class WrappingLabel:Label {
 Control observed;
 protected override void OnParentChanged(EventArgs e){if(observed!=null)observed.SizeChanged-=Wrap;observed=Parent;if(observed!=null)observed.SizeChanged+=Wrap;base.OnParentChanged(e);Wrap(this,EventArgs.Empty);}
 void Wrap(object sender,EventArgs e){if(IsDisposed||Disposing||observed==null||observed.IsDisposed)return;var value=new Size(Math.Max(150,observed.ClientSize.Width-observed.Padding.Horizontal-12),0);if(MaximumSize!=value)MaximumSize=value;}
 protected override void Dispose(bool disposing){if(disposing&&observed!=null){observed.SizeChanged-=Wrap;observed=null;}base.Dispose(disposing);}
}
class CalmButton:Button {
 bool hovered,pressed;internal CalmButton(){DoubleBuffered=true;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
 protected override void OnMouseEnter(EventArgs e){hovered=true;Invalidate();base.OnMouseEnter(e);}protected override void OnMouseLeave(EventArgs e){hovered=false;pressed=false;Invalidate();base.OnMouseLeave(e);}protected override void OnMouseDown(MouseEventArgs e){pressed=true;Invalidate();base.OnMouseDown(e);}protected override void OnMouseUp(MouseEventArgs e){pressed=false;Invalidate();base.OnMouseUp(e);}
 public override Size GetPreferredSize(Size proposed){var size=base.GetPreferredSize(proposed);if(Image!=null)size.Width+=Theme.P(7);return size;}
 protected override void OnPaint(PaintEventArgs e){var g=e.Graphics;g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;g.Clear(Parent==null?Theme.Bg:Parent.BackColor);var bounds=new Rectangle(1,1,Math.Max(1,Width-3),Math.Max(1,Height-3));int radius=Math.Min(Theme.P(6),bounds.Height/2);using(var path=new System.Drawing.Drawing2D.GraphicsPath()){path.AddArc(bounds.Left,bounds.Top,radius*2,radius*2,180,90);path.AddArc(bounds.Right-radius*2,bounds.Top,radius*2,radius*2,270,90);path.AddArc(bounds.Right-radius*2,bounds.Bottom-radius*2,radius*2,radius*2,0,90);path.AddArc(bounds.Left,bounds.Bottom-radius*2,radius*2,radius*2,90,90);path.CloseFigure();Color fill=BackColor;if(hovered&&Enabled)fill=pressed?Theme.Line:ControlPaint.Light(BackColor,.12f);using(var brush=new SolidBrush(fill))g.FillPath(brush,path);using(var pen=new Pen(Focused&&ShowFocusCues?Theme.Gold:FlatAppearance.BorderColor,Focused&&ShowFocusCues?2:1))g.DrawPath(pen,path);}
 var color=Enabled?ForeColor:Theme.Muted;int gap=Image==null?0:Theme.P(8),iw=Image==null?0:Image.Width;Size text=TextRenderer.MeasureText(g,Text,Font,new Size(Math.Max(1,Width-Padding.Horizontal-iw-gap),Height),TextFormatFlags.NoPadding);bool left=TextAlign==ContentAlignment.MiddleLeft;int x=left?Padding.Left:Math.Max(Padding.Left,(Width-text.Width-iw-gap)/2);if(Image!=null){int y=(Height-Image.Height)/2;if(Enabled)g.DrawImage(Image,x,y,Image.Width,Image.Height);else ControlPaint.DrawImageDisabled(g,Image,x,y,BackColor);x+=iw+gap;}TextRenderer.DrawText(g,Text,Font,new Rectangle(x,0,Math.Max(1,Width-x-Padding.Right),Height),color,TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix|TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak);
 }
}
class WorkspaceNav : UserControl {
 readonly FlowLayoutPanel nav;readonly Panel pages;readonly System.Collections.Generic.List<Control> views=new System.Collections.Generic.List<Control>();readonly System.Collections.Generic.List<Button> buttons=new System.Collections.Generic.List<Button>();
 internal WorkspaceNav(float dpi){Dock=DockStyle.Fill;BackColor=Theme.Bg;nav=new FlowLayoutPanel{Dock=DockStyle.Left,Width=(int)(180*dpi),FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(0,Theme.P(12),Theme.P(10),0),BackColor=Theme.Bg};pages=new Panel{Dock=DockStyle.Fill,BackColor=Theme.Bg};Controls.Add(pages);Controls.Add(nav);}
 internal void Add(string name,Control page){page.Dock=DockStyle.Fill;page.Visible=false;int index=views.Count;views.Add(page);pages.Controls.Add(page);var b=Theme.Action(name,AppGlyph.ForPage(name),"Open "+name.ToLowerInvariant()+".",(s,e)=>Select(index));b.AutoSize=false;b.BackColor=Theme.Bg;b.ForeColor=Theme.Muted;b.FlatAppearance.BorderColor=Theme.Bg;b.Width=nav.Width-Theme.P(16);b.Height=Theme.P(45);b.Padding=new Padding(Theme.P(10),0,Theme.P(7),0);b.TextAlign=ContentAlignment.MiddleLeft;b.Font=new Font("Segoe UI Semibold",10);nav.Controls.Add(b);buttons.Add(b);if(index==0)Select(0);}
 internal void Select(int index){for(int i=0;i<views.Count;i++){views[i].Visible=i==index;buttons[i].BackColor=i==index?Theme.Card:Theme.Bg;buttons[i].ForeColor=i==index?Theme.Mint:Theme.Muted;buttons[i].FlatAppearance.BorderColor=i==index?Theme.Line:Theme.Bg;}views[index].BringToFront();}
}
partial class Coach {
 Label buildSummary;RunArchivePanel runArchive;DateTime nextHistorySync;
 void UpdateBuildSummary(){if(buildCards!=null)buildCards.RefreshBuild();if(buildSummary==null)return;var b=RunMemory.Load().gameState;buildSummary.Text=b==null?"Waiting for a saved Hades II build. Start the run or refresh below.":(b.weapon==null?"Weapon unknown":b.weapon.name+"  /  "+b.weapon.aspectName+" "+b.weapon.aspectRank)+"\r\n"+b.owned.Count(t=>t.category=="arcana")+" active Arcana   ·   "+b.choices.Length+" recorded selections"+"\r\nLatest new boon: "+(b.latestBoon==null?"none recorded":b.latestBoon.name)+"   ·   Checkpoint "+DateTime.Parse(b.savedAt).ToLocalTime().ToString("HH:mm:ss");}
}
}
