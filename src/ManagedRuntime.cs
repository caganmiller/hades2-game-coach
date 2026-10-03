using System;using System.IO;using System.Linq;using System.Diagnostics;using System.Threading;using System.Threading.Tasks;using System.Net;using System.Net.Sockets;using System.Runtime.InteropServices;using System.Security.Cryptography;using System.Collections.Generic;
namespace GameCoach {
// A single shared startup task; cancelling a caller never interrupts another caller's model load.
static class ManagedRuntime {
 static readonly object gate=new object();static readonly CancellationTokenSource lifetime=new CancellationTokenSource();
 static readonly System.Collections.Generic.Queue<string> startupLog=new System.Collections.Generic.Queue<string>();static bool loading;
 static Task<LocalConfig> startup;static Process owned;static IntPtr job;static bool closed;static string state="Local model has not started.";
 internal static string Status{get{lock(gate)return state;}}internal static event Action<string> Changed;
 static void Set(string value){lock(gate)state=value;var changed=Changed;if(changed!=null)changed(value);}
 internal static int OwnedPid{get{lock(gate){try{return owned!=null&&!owned.HasExited?owned.Id:0;}catch{return 0;}}}}
 internal static void Discover(LocalConfig c){
  string hermes=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"hermes");
  if(string.IsNullOrWhiteSpace(c.RuntimePath)){string bundled=Path.Combine(Store.Root,"runtime","llama-server.exe");if(File.Exists(bundled))c.RuntimePath=bundled;else{string tools=Path.Combine(hermes,"tools");if(Directory.Exists(tools)){ushort emulated,native;bool arm=IsWow64Process2(Process.GetCurrentProcess().Handle,out emulated,out native)&&native==0xAA64;string suffix=arm?"win32-arm64":"win32-x64";c.RuntimePath=Directory.GetDirectories(tools,"llamacpp-*-"+suffix).OrderByDescending(Directory.GetLastWriteTimeUtc).Select(d=>Path.Combine(d,"llama-server.exe")).FirstOrDefault(File.Exists)??"";}}}
  if(string.IsNullOrWhiteSpace(c.ModelPath)){string bundled=Path.Combine(Store.Root,"models",c.Model+".gguf");c.ModelPath=File.Exists(bundled)?bundled:Path.Combine(hermes,"models",c.Model+".gguf");}
  if(string.IsNullOrWhiteSpace(c.ProjectorPath)){string name="mmproj-Qwen3.6-35B-A3B-BF16.gguf";if(c.Model.StartsWith("Qwen3.6-35B-A3B",StringComparison.Ordinal)){string bundled=Path.Combine(Store.Root,"models",name);c.ProjectorPath=File.Exists(bundled)?bundled:Path.Combine(hermes,"models","assets",name);}}
 }
 internal static void Validate(LocalConfig c){LocalInference.Endpoint(c.Endpoint);if(!c.Managed)return;foreach(var f in new[]{new[]{c.RuntimePath,"llama-server.exe"},new[]{c.ModelPath,"GGUF model"},new[]{c.ProjectorPath,"matching vision projector"}})if(string.IsNullOrWhiteSpace(f[0])||!File.Exists(f[0]))throw new IOException("Choose the "+f[1]+" in Settings → Local watcher → Model startup. See How to use → How to install.");if(Path.GetFileName(c.RuntimePath)!="llama-server.exe")throw new IOException("The local engine must be llama-server.exe.");if(c.ContextSize<8192||c.ContextSize>262144)throw new IOException("Local context size must be between 8192 and 262144.");}
 internal static async Task<LocalConfig> Ensure(CancellationToken token){
  Task<LocalConfig> pending;lock(gate){if(closed)throw new OperationCanceledException();if(startup==null||startup.IsCanceled||startup.IsFaulted||(startup.Status==TaskStatus.RanToCompletion&&(owned==null||owned.HasExited)))startup=Task.Run(()=>Boot(lifetime.Token));pending=startup;}
  var cancelled=new TaskCompletionSource<bool>();using(token.Register(()=>cancelled.TrySetResult(true))){if(await Task.WhenAny(pending,cancelled.Task)!=pending)token.ThrowIfCancellationRequested();token.ThrowIfCancellationRequested();return await pending;}
 }
 static async Task<LocalConfig> Boot(CancellationToken token){try{
  var cfg=LocalInference.Load();if(!cfg.Managed){Set("Connecting to your external local server…");await LocalInference.CheckConnection(cfg,token);Set("Ready · external local server");return cfg;}
  Discover(cfg);Validate(cfg);
  // Reuse a healthy Hermes instance instead of loading the same weights twice. Never own or stop it.
  string sharedSecret;var shared=HermesConnection(cfg,out sharedSecret);if(shared!=null){try{await LocalInference.CheckConnection(shared,token,sharedSecret);LocalInference.Save(shared,sharedSecret);Set("Ready · shared local model (Hermes owns this process)");return shared;}catch(OperationCanceledException){token.ThrowIfCancellationRequested();}catch{}}
  token.ThrowIfCancellationRequested();Set("Loading local model… Game Coach is starting the engine.");
  int port=AvailablePort(18435);cfg.Endpoint="http://127.0.0.1:"+port+"/v1";
  byte[] random=new byte[32];using(var rng=RandomNumberGenerator.Create())rng.GetBytes(random);string secret=Convert.ToBase64String(random);LocalInference.Save(cfg,secret);
  var info=new ProcessStartInfo{FileName=cfg.RuntimePath,WorkingDirectory=Path.GetDirectoryName(cfg.RuntimePath),UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardError=true,RedirectStandardOutput=true};
  info.Arguments="--host 127.0.0.1 --port "+port+" --model "+Q(cfg.ModelPath)+" --mmproj "+Q(cfg.ProjectorPath)+" --alias "+Q(cfg.Model)+" --ctx-size "+cfg.ContextSize+" --parallel 1 --n-gpu-layers auto --flash-attn on --cache-type-k q8_0 --cache-type-v q8_0 --batch-size 4096 --ubatch-size 2048 --no-webui --log-verbosity 1";
  if(cfg.Model.StartsWith("Qwen3.6-35B-A3B",StringComparison.Ordinal))info.Arguments+=" --spec-type draft-mtp --spec-draft-n-max 2 --backend-sampling --spec-draft-backend-sampling";
  // No key in command-line arguments or temporary plaintext files.
  foreach(string name in info.EnvironmentVariables.Keys.Cast<string>().Where(k=>k.StartsWith("LLAMA_",StringComparison.Ordinal)).ToArray())info.EnvironmentVariables.Remove(name);
  info.EnvironmentVariables["LLAMA_API_KEY"]=secret;
  loading=true;lock(startupLog)startupLog.Clear();var process=new Process{StartInfo=info};process.OutputDataReceived+=(s,e)=>{};process.ErrorDataReceived+=(s,e)=>{if(loading&&e.Data!=null){lock(startupLog){startupLog.Enqueue(e.Data.Replace(secret,"[local credential]"));while(startupLog.Count>60)startupLog.Dequeue();}}};
  lock(gate){token.ThrowIfCancellationRequested();ReleaseOwned();job=CreateKillJob();try{if(!process.Start())throw new IOException("Local engine did not start.");owned=process;if(!AssignProcessToJobObject(job,process.Handle))throw new IOException("Could not attach local engine to Game Coach for cleanup.");process.BeginOutputReadLine();process.BeginErrorReadLine();}catch{ReleaseOwned();throw;}}
  var elapsed=Stopwatch.StartNew();while(elapsed.Elapsed<TimeSpan.FromMinutes(4)){token.ThrowIfCancellationRequested();if(process.HasExited)throw new IOException("Local engine exited (code "+process.ExitCode+"). Check the engine architecture, GPU driver and matching model/projector in Model startup.");try{await LocalInference.CheckConnection(cfg,token);loading=false;Set("Ready · Game Coach manages the local model");Diagnostics.Log("App-managed local model ready in "+elapsed.ElapsedMilliseconds+" ms.");return cfg;}catch(OperationCanceledException){token.ThrowIfCancellationRequested();}catch{}await Task.Delay(750,token);}
  throw new IOException("Local model startup exceeded four minutes. Check available GPU memory and Model startup settings.");
 }catch(Exception ex){loading=false;lock(startupLog){if(startupLog.Count>0)File.WriteAllLines(Path.Combine(Store.Root,"local-startup.log"),startupLog.ToArray());}lock(gate)ReleaseOwned();Set(ex is OperationCanceledException?"Local model stopped.":ex.Message);throw;}}
 static LocalConfig HermesConnection(LocalConfig original,out string secret){secret=null;try{string path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"hermes","runtimes","llamacpp","server.json");if(!File.Exists(path))return null;var data=Store.Json.Deserialize<Dictionary<string,object>>(File.ReadAllText(path));var process=Process.GetProcessById(Convert.ToInt32(data["pid"]));if(process.ProcessName!="llama-server"||!string.Equals(process.MainModule.FileName,Convert.ToString(data["executable"]),StringComparison.OrdinalIgnoreCase))return null;double created=Convert.ToDouble(data["create_time"]);if(Math.Abs((process.StartTime.ToUniversalTime()-new DateTime(1970,1,1)).TotalSeconds-created)>3)return null;var cfg=Store.Json.Deserialize<LocalConfig>(Store.Json.Serialize(original));cfg.Endpoint=LocalInference.Endpoint(Convert.ToString(data["base_url"])).AbsoluteUri.TrimEnd('/');secret=Convert.ToString(data["api_key"]);return cfg;}catch{return null;}}
 static int AvailablePort(int preferred){var probe=new TcpListener(IPAddress.Loopback,preferred);try{probe.Start();return preferred;}catch(SocketException){probe.Stop();probe=new TcpListener(IPAddress.Loopback,0);probe.Start();return ((IPEndPoint)probe.LocalEndpoint).Port;}finally{probe.Stop();}}
 static string Q(string text){if(text.Contains('"')||text.Contains('\r')||text.Contains('\n'))throw new IOException("Invalid character in local model path or name.");return "\""+text+"\"";}
 // Closing the job handle also covers unexpected app termination and avoids orphaned GPU allocations.
 static void ReleaseOwned(){if(owned!=null){try{if(!owned.HasExited){owned.Kill();owned.WaitForExit(3000);}}catch{}owned.Dispose();owned=null;}if(job!=IntPtr.Zero){CloseHandle(job);job=IntPtr.Zero;}}
 internal static void Shutdown(){lock(gate){closed=true;lifetime.Cancel();ReleaseOwned();}Set("Local model stopped.");}
 internal static void Invalidate(){lock(gate){if(startup!=null&&!startup.IsCompleted)throw new InvalidOperationException("Wait for model startup to finish before changing its settings.");ReleaseOwned();startup=null;}}
 [StructLayout(LayoutKind.Sequential)]struct BasicLimits{public long ProcessTime,JobTime;public uint Flags;public UIntPtr MinWorkingSet,MaxWorkingSet;public uint ActiveProcesses;public UIntPtr Affinity;public uint Priority,Scheduling;}
 [StructLayout(LayoutKind.Sequential)]struct IoCounters{public ulong ReadOps,WriteOps,OtherOps,ReadBytes,WriteBytes,OtherBytes;}
 [StructLayout(LayoutKind.Sequential)]struct ExtendedLimits{public BasicLimits Basic;public IoCounters Io;public UIntPtr ProcessMemory,JobMemory,PeakProcessMemory,PeakJobMemory;}
 static IntPtr CreateKillJob(){IntPtr h=CreateJobObject(IntPtr.Zero,null);if(h==IntPtr.Zero)throw new IOException("Could not create the local engine lifecycle job.");var limits=new ExtendedLimits();limits.Basic.Flags=0x2000;int size=Marshal.SizeOf(limits);IntPtr p=Marshal.AllocHGlobal(size);try{Marshal.StructureToPtr(limits,p,false);if(!SetInformationJobObject(h,9,p,(uint)size))throw new IOException("Could not configure the local engine lifecycle job.");return h;}catch{CloseHandle(h);throw;}finally{Marshal.FreeHGlobal(p);}}
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern IntPtr CreateJobObject(IntPtr unused,string name);
 [DllImport("kernel32.dll")]static extern bool SetInformationJobObject(IntPtr job,int kind,IntPtr info,uint size);
 [DllImport("kernel32.dll")]static extern bool AssignProcessToJobObject(IntPtr job,IntPtr process);
 [DllImport("kernel32.dll")]static extern bool CloseHandle(IntPtr handle);
 [DllImport("kernel32.dll")]static extern bool IsWow64Process2(IntPtr process,out ushort processMachine,out ushort nativeMachine);
}
}
