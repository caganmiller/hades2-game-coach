using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Speech.Recognition;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GameCoach {
static class InterruptCommands {
 public static string Normalize(string text){return string.Join(" ",new string((text??"").ToLowerInvariant().Select(c=>char.IsLetterOrDigit(c)?c:' ').ToArray()).Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries));}
 public static bool Thanks(string text){string s=Normalize(text);return new[]{"thanks","coach thanks","thanks coach","thank you","thank you coach","okay thanks","ok thanks"}.Contains(s);}
 public static bool AcceptThanks(string text,float confidence,bool hypothesis){return Thanks(text)&&confidence>=(hypothesis?.40f:.22f);}
 public static string[] Phrases(string wake){return new[]{wake,"coach","hey coach","thanks","coach thanks","thanks coach","thank you","thank you coach","okay thanks","ok thanks"}.Distinct().ToArray();}
 public static void Configure(SpeechRecognitionEngine engine){try{engine.UpdateRecognizerSetting("CFGConfidenceRejectionThreshold",20);}catch(ArgumentException){Diagnostics.Log("Recognizer uses its default rejection threshold.");}}
}

// Written advice and run memory keep their sources. Only the audio copy is cleaned.
static class SpeechText {
 static readonly Regex Url = new Regex(@"\b(?:https?://|www\.)[^\s<>\[\]{}""']+|\b(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z]{2,24}\b(?:[/?#][^\s<>\[\]{}""']*)?",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
 public static string ForSpeech(string text){
  if(string.IsNullOrWhiteSpace(text))return "";
  text=Regex.Replace(text,@"\uE200[^\uE201]*(?:\uE201|$)|【[^】]*†[^】]*(?:】|$)"," ");
  text=Regex.Replace(text,@"(?im)^\s*\[(?:\^)?[^\]\r\n]+\]:[ \t]*(?:https?://|www\.)[^\r\n]*"," ");
  text=Regex.Replace(text,@"(?im)^\s*(?:web sources consulted|sources|references|citations):[ \t]*(?:\r?\n|(?=https?://|www\.|\[)).*\z"," ",RegexOptions.Singleline);
  var spoken=new StringBuilder();
  for(int i=0;i<text.Length;i++){
   if(text[i]=='['){
    int labelEnd=Closing(text,i,'[',']'),target=labelEnd+1;
    if(labelEnd>=0&&target<text.Length&&text[target]=='('){
     int linkEnd=Closing(text,target,'(',')');
     if(linkEnd<0){i=text.Length;break;} // Never voice an unfinished destination.
     if(spoken.Length>0&&spoken[spoken.Length-1]=='!')spoken.Length--;
     else spoken.Append(text.Substring(i+1,labelEnd-i-1));
     i=linkEnd;continue;
    }
   }
   spoken.Append(text[i]);
  }
  text=Url.Replace(spoken.ToString(),m=>TrailingPunctuation(m.Value));
  text=Regex.Replace(text,@"\[(?:\^[\w-]+|\d+(?:\s*[,;-]\s*\d+)*)\]"," ");
  text=Regex.Replace(text,@"[*`]|(?<!\w)_{1,3}|_{1,3}(?!\w)","");
  text=Regex.Replace(text,@"[\(\[<]\s*(?:(?:sources?|references?|citations?):?\s*)?[,;:.!?-]*\s*[\)\]>]"," ",RegexOptions.IgnoreCase);
  text=Regex.Replace(text,@"(?im)^\s*(?:sources?|references?|citations?|web sources consulted):?\s*[,;:.!?-]*\s*$"," ");
  text=Regex.Replace(text,@"\s+"," ").Trim();
  text=Regex.Replace(text,@"\s+([,.;:!?])","$1");
  return text.Any(char.IsLetterOrDigit)?text:"";
 }
 static int Closing(string text,int start,char open,char close){int depth=0;for(int i=start;i<text.Length;i++){if(text[i]=='\\'){i++;continue;}if(text[i]==open)depth++;else if(text[i]==close&&--depth==0)return i;}return -1;}
 static string TrailingPunctuation(string url){
  int end=url.Length;while(end>0&&".,;:!?".IndexOf(url[end-1])>=0)end--;
  string punctuation=url.Substring(end);int balance=0;for(int i=0;i<end;i++){if(url[i]=='(')balance++;else if(url[i]==')')balance--;}
  return new string(')',Math.Max(0,-balance))+punctuation;
 }
 // Do not cut a streamed sentence inside a link, citation, or parenthetical.
 public static int FirstSentenceEnd(string text){
  int brackets=0,parens=0,angles=0,citations=0;
  for(int i=0;i+1<text.Length;i++){
   char c=text[i];if(c=='\\'){i++;continue;}
   if(c=='[')brackets++;else if(c==']')brackets=Math.Max(0,brackets-1);
   else if(c=='(')parens++;else if(c==')')parens=Math.Max(0,parens-1);
   else if(c=='<')angles++;else if(c=='>')angles=Math.Max(0,angles-1);
   else if(c=='\uE200'||c=='【')citations++;else if(c=='\uE201'||c=='】')citations=Math.Max(0,citations-1);
   if(i>=12&&brackets==0&&parens==0&&angles==0&&citations==0&&(c=='.'||c=='!'||c=='?')&&char.IsWhiteSpace(text[i+1]))return i+1;
  }
  return -1;
 }
}

// Emits at most two chunks: the first complete sentence, then the rest of the advice.
// JSON escapes are decoded only when complete. Memory is still validated at response.completed.
sealed class AdviceChunks {
 int sent;bool first;
 public IEnumerable<string> Read(string json){
  var match=System.Text.RegularExpressions.Regex.Match(json,@"^\s*\{\s*""advice""\s*:\s*""");if(!match.Success)yield break;
  var text=new StringBuilder();bool complete=false;
  for(int i=match.Length;i<json.Length;i++){
   char c=json[i];if(c=='"'){complete=true;break;}if(c!='\\'){text.Append(c);continue;}
   if(++i>=json.Length)break;c=json[i];if(c=='u'){if(i+4>=json.Length)break;int value;if(!int.TryParse(json.Substring(i+1,4),System.Globalization.NumberStyles.HexNumber,null,out value))yield break;text.Append((char)value);i+=4;}
   else if(c=='n')text.Append('\n');else if(c=='r')text.Append('\r');else if(c=='t')text.Append('\t');else if(c=='b')text.Append('\b');else if(c=='f')text.Append('\f');else if(c=='"'||c=='\\'||c=='/')text.Append(c);else yield break;
  }
  string valueText=text.ToString();
  if(!first&&!complete){int end=SpeechText.FirstSentenceEnd(valueText);if(end>0){sent=end;first=true;yield return valueText.Substring(0,sent).Trim();}}
  if(complete&&valueText.Length>sent){string rest=valueText.Substring(sent).Trim();sent=valueText.Length;first=true;if(rest.Length>0)yield return rest;}
 }
}

static class VoiceCueCache {
 public const string Thinking="Thinking for a sec.";
 public const string GotIt="Got it.";
 public static bool IsCue(string text){return text==Thinking||text==GotIt;}
 static string PathFor(string voice,int rate,string text){using(var hash=SHA256.Create()){string name=BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes("cue-v1|"+CloudVoice.Model+"|"+voice+"|"+rate+"|"+text))).Replace("-","");return Path.Combine(Store.Root,"voice-cache",name+".pcm");}}
 public static byte[] Read(string voice,int rate,string text){if(!IsCue(text))return null;try{var p=PathFor(voice,rate,text);if(!File.Exists(p))return null;var bytes=File.ReadAllBytes(p);return bytes.Length>4800&&bytes.Length<48000*12&&bytes.Length%2==0?bytes:null;}catch(IOException){return null;}}
 public static void Save(string voice,int rate,string text,byte[] pcm){if(!IsCue(text)||pcm.Length<=4800||pcm.Length>=48000*12||pcm.Length%2!=0)return;var p=PathFor(voice,rate,text);Directory.CreateDirectory(Path.GetDirectoryName(p));File.WriteAllBytes(p,pcm);}
 public static async Task Prepare(string key,string voice,int rate,CancellationToken token){foreach(string cue in new[]{Thinking,GotIt})if(Read(voice,rate,cue)==null)using(var session=new CloudVoice.Session(key,voice,rate))using(var pcm=new MemoryStream()){await session.Open(token);await session.Generate(cue,b=>pcm.Write(b,0,b.Length),token);Save(voice,rate,cue,pcm.ToArray());}}
}

// A single reply owns all of its speech, so "thanks" cancels both current and future chunks.
sealed class ReplyAudio : IDisposable {
 readonly object gate=new object();readonly Queue<string> queue=new Queue<string>();readonly SemaphoreSlim ready=new SemaphoreSlim(0);readonly CancellationTokenSource cancel=new CancellationTokenSource();
 readonly string key,voice;readonly int volume,rate;readonly Task worker;bool finished,silenced;public Exception Error{get;private set;}public int Enqueued{get;private set;}
 public bool Silenced{get{lock(gate)return silenced;}}
 public ReplyAudio(string key,string voice,int volume,int rate){this.key=key;this.voice=voice;this.volume=volume;this.rate=rate;worker=Task.Run((Func<Task>)Pump);}
 public void Enqueue(string text){text=SpeechText.ForSpeech(text);lock(gate){if(finished||silenced||text.Length==0)return;queue.Enqueue(text);Enqueued++;ready.Release();}}
 public void Silence(){lock(gate){if(silenced)return;silenced=true;queue.Clear();cancel.Cancel();}}
 public async Task Finish(){lock(gate){if(!finished){finished=true;ready.Release();}}await worker;if(Error!=null)throw new Exception("Voice playback failed: "+Error.Message,Error);}
 async Task Pump(){
  if(volume<=0)return;var token=cancel.Token;
  using(var session=new CloudVoice.Session(key,voice,rate))using(var player=new StreamingSpeaker(volume))using(var stop=token.Register(player.Stop)){
   // Establish the voice connection while transcription/planning and the cached cue run.
   var connection=session.Open(token);var observeConnection=connection.ContinueWith(t=>{var ignored=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);
   try{bool hasAudio=false;while(true){string text=null;lock(gate){if(queue.Count>0)text=queue.Dequeue();else if(finished)break;}if(text==null){await ready.WaitAsync(token);continue;}token.ThrowIfCancellationRequested();var cached=VoiceCueCache.Read(voice,rate,text);
     if(cached!=null){player.Add(cached);hasAudio=true;Diagnostics.Log("Cached voice cue queued");continue;}
     await connection;using(var capture=VoiceCueCache.IsCue(text)?new MemoryStream():null){await session.Generate(text,b=>{if(capture!=null)capture.Write(b,0,b.Length);player.Add(b);hasAudio=true;},token);if(capture!=null)VoiceCueCache.Save(voice,rate,text,capture.ToArray());}
    }if(hasAudio)await player.Drain(token);
   }catch(OperationCanceledException ex){if(!token.IsCancellationRequested){Error=ex;Diagnostics.Log("Reply voice timed out.");}}catch(Exception ex){if(!token.IsCancellationRequested){Error=ex;Diagnostics.Log("Reply voice failed: "+ex.GetType().Name);}}
   finally{cancel.Cancel();}
  }
 }
 public void Dispose(){Silence();}
}

static class CloudVoice {
 public const string Model="gpt-realtime-2.1-mini";
 public static async Task Speak(string key,string text,string voice,int volume,int rate,CancellationToken token){if(volume<=0)return;using(var reply=new ReplyAudio(key,voice,volume,rate))using(var registration=token.Register(reply.Silence)){reply.Enqueue(text);await reply.Finish();token.ThrowIfCancellationRequested();}}
 internal sealed class Session:IDisposable {
  readonly ClientWebSocket ws=new ClientWebSocket();readonly string voice;readonly int rate;
  public Session(string key,string voice,int rate){this.voice=voice;this.rate=rate;ws.Options.SetRequestHeader("Authorization","Bearer "+key);}
  public async Task Open(CancellationToken token){ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;using(var deadline=CancellationTokenSource.CreateLinkedTokenSource(token)){deadline.CancelAfter(TimeSpan.FromSeconds(15));var ct=deadline.Token;Diagnostics.Log("Voice connection started");await ws.ConnectAsync(new Uri("wss://api.openai.com/v1/realtime?model="+Model),ct);string pace=rate<=-2?"slightly slower":rate<0?"a little slower":rate>=2?"slightly faster":rate>0?"a little faster":"natural conversational pace";await Send(new{type="session.update",session=new{type="realtime",model=Model,output_modalities=new[]{"audio"},audio=new{output=new{format=new{type="audio/pcm",rate=24000},voice=voice}},instructions="Speak in a warm, natural, pleasant, friendly American voice. Use "+pace+". Read exactly the provided text; do not add an introduction, confirmation or commentary."}},ct);await Until(ct,"session.updated",null);}}
  public async Task Generate(string text,Action<byte[]> chunk,CancellationToken token){text=SpeechText.ForSpeech(text);if(text.Length==0)return;using(var measured=UsageMeter.Begin("cloud","spoken reply",Model,token))using(var deadline=CancellationTokenSource.CreateLinkedTokenSource(token)){deadline.CancelAfter(TimeSpan.FromSeconds(35));var ct=deadline.Token;Vision.Requests++;if(Vision.OnRequest!=null)Vision.OnRequest();await Send(new{type="response.create",response=new{conversation="none",output_modalities=new[]{"audio"},input=new[]{new{type="message",role="user",content=new[]{new{type="input_text",text=text}}}}}},ct);await Until(ct,"response.done",chunk,measured);measured.Success();Diagnostics.Log("Voice chunk generation completed");}}
  async Task Send(object value,CancellationToken ct){byte[] bytes=Encoding.UTF8.GetBytes(new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(value));await ws.SendAsync(new ArraySegment<byte>(bytes),WebSocketMessageType.Text,true,ct);}
  async Task Until(CancellationToken ct,string wanted,Action<byte[]> audioChunk,UsageMeter.Request measured=null){var buffer=new byte[8192];using(var message=new MemoryStream()){while(true){message.SetLength(0);WebSocketReceiveResult r;do{r=await ws.ReceiveAsync(new ArraySegment<byte>(buffer),ct);if(r.MessageType==WebSocketMessageType.Close)throw new Exception("OpenAI voice connection closed.");message.Write(buffer,0,r.Count);if(message.Length>1000000)throw new Exception("Voice event exceeded limit.");}while(!r.EndOfMessage);var evt=new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Dictionary<string,object>>(Encoding.UTF8.GetString(message.ToArray()));string type=evt.ContainsKey("type")?Convert.ToString(evt["type"]):"";if(type=="error")throw new Exception("OpenAI voice rejected the request.");if(type=="response.output_audio.delta"&&audioChunk!=null)audioChunk(Convert.FromBase64String(Convert.ToString(evt["delta"])));if(type==wanted){if(type=="response.done"){if(measured!=null)measured.Observe(UsageMeter.Get(evt,"response"));var result=evt["response"] as Dictionary<string,object>;if(result==null||Convert.ToString(result["status"])!="completed")throw new Exception("Voice response did not complete.");}return;}}}}
  public void Dispose(){ws.Abort();ws.Dispose();}
 }
}
}
