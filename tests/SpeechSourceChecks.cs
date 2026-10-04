using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace GameCoach {
static class SpeechSourceChecks {
 static int checks;
 static void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
 static string Compact(string s){return System.Text.RegularExpressions.Regex.Replace(s,@"\s+"," ").Trim();}
 static void Case(string input,string expected){
  string actual=SpeechText.ForSpeech(input);
  Check(actual==expected,"Speech mismatch: "+input+" -> "+actual+" (expected "+expected+")");
  Check(SpeechText.ForSpeech(actual)==expected,"Speech cleanup must be idempotent");
 }
 static void Stream(string advice){
  string json=Store.Json.Serialize(new{advice=advice,updates=new object[0]});
  string expected=SpeechText.ForSpeech(advice);
  for(int step=1;step<=json.Length;step++){
   var parser=new AdviceChunks();var raw=new List<string>();var spoken=new List<string>();
   for(int end=step;end<json.Length;end+=step)foreach(string chunk in parser.Read(json.Substring(0,end))){raw.Add(chunk);string audio=SpeechText.ForSpeech(chunk);if(audio.Length>0)spoken.Add(audio);}
   foreach(string chunk in parser.Read(json)){raw.Add(chunk);string audio=SpeechText.ForSpeech(chunk);if(audio.Length>0)spoken.Add(audio);}
   Check(Compact(string.Join(" ",raw))==Compact(advice),"Written advice and source URLs must remain intact at step "+step);
   Check(raw.Count<=2,"Streaming must keep at most two chunks");
   Check(Compact(string.Join(" ",spoken))==expected,"Stream mismatch at step "+step+": "+string.Join(" | ",spoken));
   Check(!parser.Read(json).Any(),"Repeated completion must not repeat speech");
  }
 }
 internal static void Run(){
  Case(null,"");Case("","");Case("https://hades.fandom.com/wiki/Hades_II","");
  Case("([hades.fandom.com](https://hades.fandom.com/wiki/Oath_of_the_Unseen?utm_source=openai))","");
  Case("Take **Vow of Time** at +1. ([hades.fandom.com](https://hades.fandom.com/wiki/Oath_of_the_Unseen?utm_source=openai))","Take Vow of Time at +1.");
  Case("Take [Wave Flourish](https://hades.fandom.com/wiki/Poseidon_(Hades_II)). It adds 40% damage.","Take Wave Flourish. It adds 40% damage.");
  Case("Take [Wave Flourish](https://hades.fandom.com/wiki/Poseidon_\\(Hades_II\\) \"Wiki page\").","Take Wave Flourish.");
  Case("Keep your current attack. (Source: https://example.com/wiki/A_(B)) Move on.","Keep your current attack. Move on.");
  Case("Take the boon. <https://example.org/boon?q=1#details> Then go left.","Take the boon. Then go left.");
  Case("Take the boon. www.example.org/guide Then go left.","Take the boon. Then go left.");
  Case("Take the boon. hades.fandom.com/wiki/Boon Then go left.","Take the boon. Then go left.");
  Case("Take the boon [1], then go left [^wiki].","Take the boon, then go left.");
  Case("Take the boon [1, 2].\n[1]: https://example.com\n[2]: https://example.org","Take the boon.");
  Case("Take the boon.\nWeb sources consulted:\nWiki: https://example.com\nAnother: https://example.org","Take the boon.");
  Case("Take the boon.\nSources: [Hades II Wiki](https://example.com)","Take the boon.");
  Case("Take the boon. \uE200cite\uE202turn0search0\uE201 Then go left.","Take the boon. Then go left.");
  Case("Take the boon.【1†Wiki†https://example.com】 Then go left.","Take the boon. Then go left.");
  Case("Your Ω Attack costs 20 Magick, deals +40% damage, and lasts 1.5 seconds. Keep [Attack] and _Special_ at level 2.","Your Ω Attack costs 20 Magick, deals +40% damage, and lasts 1.5 seconds. Keep [Attack] and Special at level 2.");
  Case(VoiceCueCache.Thinking,VoiceCueCache.Thinking);Case(VoiceCueCache.GotIt,VoiceCueCache.GotIt);
  Stream("Take Wave Flourish. Its 40% damage supports your build.");
  Stream("Take \"Wave Flourish\". It helps your Ω Special every 1.5 seconds.");
  Stream("Take **Vow of Time** at +1. ([hades.fandom.com](https://hades.fandom.com/wiki/Oath_of_the_Unseen?utm_source=openai))");
  Stream("Choose [Wave Flourish. It supports your build](https://example.com/boon_(Hades_II)). Then go left.");
  Stream("Take the boon (see https://example.com/wiki/A_(B). It is current). Then go left.");
  Stream("Take the boon. ([hades.fandom.com](https://hades.fandom.com/wiki/Boon)) Then go left.");
  Stream("Take the boon. \uE200cite\uE202turn0search0\uE201 Then go left.");
  Stream("Take the boon.【1†Wiki. Source†https://example.com】 Then go left.");
  Stream("Take the boon.\nSources: [Hades II Wiki](https://example.com)");
  var early=new AdviceChunks();var first=early.Read("{\"advice\":\"Take Wave Flourish. ").ToArray();
  Check(first.Length==1&&first[0]=="Take Wave Flourish.","Ordinary first sentence must stream before advice completes");
  Check(!new AdviceChunks().Read("{\"advice\":\"Choose [Wave Flourish. ").Any(),"Partial link label must not leak into speech");
  using(var reply=new ReplyAudio("offline-test-placeholder","marin",0,0)){
   reply.Enqueue("([hades.fandom.com](https://hades.fandom.com/wiki/Boon))");
   Check(reply.Enqueued==0,"Citation-only chunks must not generate audio");
   reply.Enqueue("Take **Wave Flourish**. ([hades.fandom.com](https://hades.fandom.com/wiki/Boon))");
   var queue=(Queue<string>)typeof(ReplyAudio).GetField("queue",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(reply);
   Check(queue.Single()=="Take Wave Flourish.","Actual speech queue must receive only sanitized prose");
   reply.Silence();reply.Enqueue("A later streamed sentence.");
   Check(reply.Silenced&&reply.Enqueued==1&&queue.Count==0,"Thanks must cancel queued and future sanitized chunks");
   reply.Finish().GetAwaiter().GetResult();
  }
  int requests=Vision.Requests;
  using(var voice=new CloudVoice.Session("offline-test-placeholder","marin",0))voice.Generate("https://example.com",b=>{throw new Exception("Unexpected audio");},CancellationToken.None).GetAwaiter().GetResult();
  Check(Vision.Requests==requests,"Citation-only voice input must not open an audio request");
  Check(InterruptCommands.AcceptThanks("thanks",.5f,false),"Thanks recognition remains available");
  Console.WriteLine("PASS "+checks+" speech/source and streaming checks; zero cloud calls");
 }
 static int Main(){try{Run();return 0;}catch(Exception ex){Console.Error.WriteLine("FAIL "+ex);return 1;}}
}
}
