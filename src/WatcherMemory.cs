using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;

namespace GameCoach {
public class OfferDetail {
 public string name,rarity,effect;
 public string Describe(){return name+(string.IsNullOrWhiteSpace(rarity)?"":" ("+rarity+")")+(string.IsNullOrWhiteSpace(effect)?" — effect unreadable":" — "+effect);}
}
static class OfferMemory {
 public static string Identity(string value){return Regex.Replace((value??"").ToLowerInvariant(),@"[^\p{L}\p{Nd}]+"," ").Trim();}
 public static string Signature(OfferDetail[] offers){return string.Join("|",(offers??new OfferDetail[0]).Where(x=>x!=null&&!string.IsNullOrWhiteSpace(x.name)).Select(x=>Identity(x.name)).Distinct().OrderBy(x=>x,StringComparer.Ordinal));}
 public static void Gap(PickupWindow window){var run=RunMemory.Load();if(run.id!=window.Run||window.Recorded)return;if(run.watcherGaps==null)run.watcherGaps=new List<string>();string note=window.Choice.Time.ToString("o")+": no visual pickup confirmation for "+string.Join(", ",window.Offers.Select(x=>x.name))+". The game-save snapshot or a later player report may resolve this.";if(run.watcherGaps.Contains(note))return;run.watcherGaps.Add(note);while(run.watcherGaps.Count>20)run.watcherGaps.RemoveAt(0);RunMemory.Save(run);}
 public static OfferDetail[] Latest(){var e=RunMemory.Load().events.LastOrDefault(x=>x.offers!=null&&x.offers.Length>0);return e==null?new OfferDetail[0]:e.offers;}
 public static OfferDetail Match(string name,OfferDetail[] offers){string id=Identity(name);return (offers??new OfferDetail[0]).FirstOrDefault(x=>x!=null&&!string.IsNullOrWhiteSpace(x.name)&&(Identity(x.name)==id||Identity(x.Describe())==id));}
 public static BuildUpdate[] PlayerReport(string text){
  if(!AnswerStream.ChoiceReport(text))return new BuildUpdate[0];string words=" "+Identity(text)+" ";var matches=Latest().Where(x=>words.Contains(" "+Identity(x.name)+" ")).ToArray();
  if(matches.Length!=1)return new BuildUpdate[0];var item=matches[0];return new[]{new BuildUpdate{category="acquired",fact=item.Describe()+"; selected by player.",evidence="player"}};
 }
 public static bool Known(string name){string id=Identity(name);var run=RunMemory.Load();if(run.gameState!=null&&(run.gameState.owned??new SavedTrait[0]).Any(t=>Identity(t.name)==id))return true;return run.events.SelectMany(x=>x.updates??new BuildUpdate[0]).Any(x=>(x.category=="acquired"||x.category=="equipped")&&Regex.IsMatch(Identity(x.fact),@"(?:^| )"+Regex.Escape(id)+@"(?: |$)"));}
 public static string Context(){
  var run=RunMemory.Load();var lastOffer=run.events.FindLastIndex(x=>(x.updates??new BuildUpdate[0]).Any(u=>u.category=="offered"));
  // Only old unchosen offers are omitted. Every build fact, correction, preference and player report stays chronological.
  var events=run.events.Select((e,i)=>new{player=e.player=="Local choice-screen observation"?null:e.player,updates=(e.updates??new BuildUpdate[0]).Where(u=>u.category!="offered"||i==lastOffer).ToArray()}).Where(e=>!string.IsNullOrEmpty(e.player)||e.updates.Length>0).ToArray();
  return Store.Json.Serialize(new{coachJournal=RunLab.Context(),run.id,run.game,run.legacy,run.gameState,run.saveSyncStatus,run.watcherGaps,events});
 }
}
static class WatcherState {
 static string run="",message="Watcher has not confirmed any new pickup.";
 public static void Set(string id,string value){run=id;message=value;}
 public static string ForRun(string id){return run==id?message:"No current automatic pickup confirmation.";}
}
sealed class BufferedFrame {
 public long Sequence;public DateTime Time;public byte[] Image,Small;public ChoiceScreenGate Layout;public Rectangle Bounds;public BoonMenuRead Menu;
}
sealed class FrameBuffer {
 readonly List<BufferedFrame> frames=new List<BufferedFrame>();long sequence,bytes;
 public int Count{get{return frames.Count;}}public long Bytes{get{return bytes;}}
 public BufferedFrame Add(Bitmap bitmap,Rectangle bounds,DateTime now,byte[] small){var f=new BufferedFrame{Sequence=++sequence,Time=now,Image=GameWindow.Encode(bitmap),Small=small,Layout=new ChoiceScreenGate(bitmap),Bounds=bounds};frames.Add(f);bytes+=f.Image.Length;while(frames.Count>0&&(frames.Count>100||bytes>32*1024*1024||now-frames[0].Time>TimeSpan.FromSeconds(50))){bytes-=frames[0].Image.Length;frames.RemoveAt(0);}return f;}
 public BufferedFrame[] After(long sequence){return frames.Where(x=>x.Sequence>sequence).ToArray();}
 public void Clear(){frames.Clear();bytes=0;}
}
sealed class PickupWindow {
 public readonly string Run;public readonly BufferedFrame Choice;public readonly OfferDetail[] Offers;
 public readonly List<BufferedFrame> Frames=new List<BufferedFrame>();public DateTime Exit;public int Read;public bool Closed,Recorded;long last;BufferedFrame firstMismatch;
 public PickupWindow(string run,BufferedFrame choice,OfferDetail[] offers){Run=run;Choice=choice;Offers=offers;last=choice.Sequence;}
 public void Add(IEnumerable<BufferedFrame> frames){if(Closed)return;foreach(var f in frames){if(f.Sequence<=last)continue;last=f.Sequence;if(f.Bounds!=Choice.Bounds){Closed=true;break;}bool menu=Choice.Layout.Matches(f.Layout);if(Exit==default(DateTime)){if(menu){firstMismatch=null;continue;}if(firstMismatch==null){firstMismatch=f;continue;}Exit=firstMismatch.Time;Frames.Add(firstMismatch);}if(f.Time-Exit>TimeSpan.FromSeconds(6)){Closed=true;break;}if(!menu&&Frames.Count<12)Frames.Add(f);}}
 public BufferedFrame[] Take(){if(Recorded)return new BufferedFrame[0];var available=Frames.Skip(Read).Take(3).ToArray();if(available.Length<2&&!Closed)return new BufferedFrame[0];Read+=available.Length;return available;}
 public bool Done{get{return Recorded||(Closed&&Read>=Frames.Count);}}
}
public class PickupEvidence {public int frame;public string name,proof,kind;public double confidence;}
public class PickupResult {public PickupEvidence[] sightings;}
sealed class PickupVerifier {
 readonly Dictionary<string,Tuple<long,DateTime>> seen=new Dictionary<string,Tuple<long,DateTime>>();
 public SeenItem[] Accept(PickupResult result,BufferedFrame[] frames,OfferDetail[] offers){var confirmed=new List<SeenItem>();foreach(var e in result==null?new PickupEvidence[0]:result.sightings??new PickupEvidence[0]){
  if(e==null||double.IsNaN(e.confidence)||e.confidence<.90||e.confidence>1||e.frame<0||e.frame>=frames.Length||string.IsNullOrWhiteSpace(e.proof)||(e.kind!="acquisition_confirmation"&&e.kind!="inventory"))continue;
  var offer=OfferMemory.Match(e.name,offers);if(offer==null)continue;string key=OfferMemory.Identity(offer.name);var frame=frames[e.frame];Tuple<long,DateTime> earlier;
  if(seen.TryGetValue(key,out earlier)&&frame.Sequence!=earlier.Item1&&Math.Abs((frame.Time-earlier.Item2).TotalMilliseconds)>=200){confirmed.Add(new SeenItem{name=offer.name,proof=e.kind+": "+e.proof});}else if(!seen.ContainsKey(key))seen[key]=Tuple.Create(frame.Sequence,frame.Time);
 }return confirmed.GroupBy(x=>OfferMemory.Identity(x.name)).Select(x=>x.First()).ToArray();}
}
}
