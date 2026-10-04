using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.IO.Compression;

namespace GameCoach {
class DistributionChecks {
    static void Check(bool ok, string label) { if (!ok) throw new Exception(label); Console.WriteLine("PASS " + label); }
    static bool Rejects(string endpoint) { try { LocalInference.Endpoint(endpoint); return false; } catch { return true; } }
    [STAThread] static int Main(string[] args) {
        try {
            Environment.SetEnvironmentVariable("OPENAI_API_KEY", null);
            Store.Root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test-output", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Store.Root);
            Native.SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            SpeechSourceChecks.Run();
            Check(Store.Key() == "" && !File.Exists(Path.Combine(Store.Root, "api-key.dpapi")), "new installation has no API credential");
            Check(RunMemory.Load().gameState == null && RunArchive.All().Length == 0, "new installation has no player build or run history");
            var settings = Store.Load();
            Check(settings.Model == "gpt-6-luna" && settings.Display == "" && settings.Voice == "marin", "fresh settings use generic defaults");
            var local = LocalInference.Load();
            Check(local.RuntimePath == "" && local.ModelPath == "" && local.ProjectorPath == "", "local configuration contains no developer machine paths");
            Check(Rejects("https://example.com/v1") && Rejects("http://example.com/v1") && Rejects("http://user:pass@127.0.0.1/v1") && Rejects("http://127.0.0.1/v1?key=example"), "watcher rejects remote and credential-bearing endpoints");
            Check(LocalInference.Endpoint("http://127.0.0.1:18435/v1").Host == "127.0.0.1", "watcher accepts loopback inference");
            Check(RecapExports.PythonRuntime() == "", "PDF setup does not fall back to a developer-only installation");
            Check(BoonArt.Get("synthetic-unknown-item") == null, "missing optional game artwork is safe");
            if (args.Contains("--ui") || Path.GetFileNameWithoutExtension(Application.ExecutablePath) == "DistributionPreview") {
                SaveMemorySync.SaveDirectory = Path.Combine(Store.Root, "no-game-saves");
                using (var form = new Coach()) {
                    Console.WriteLine("Clean preview form created.");
                    typeof(Coach).GetField("nextHistorySync", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(form, DateTime.MaxValue);
                    form.Text = "Game Coach · Clean installation preview";
                    Application.Run(form);
                }
                Check(Vision.Requests == 0, "clean UI preview made no cloud requests");
                return 0;
            }
            var run = new RunRecap {
                profile = "synthetic", number = 1, result = "Completed", cleared = true,
                route = "Underworld", weapon = "Witch's Staff", aspect = "Aspect of Melinoë",
                seconds = 1234, fear = 0, damageTaken = 100,
                items = new[] {
                    new RecapItem { id = "synthetic-boon", name = "Synthetic boon", category = "Attack", rarity = "Epic", level = 2, description = "Synthetic effect for export verification." },
                    new RecapItem { id = "synthetic-keepsake", name = "Synthetic keepsake", category = "Keepsake", rarity = "Common", rank = 1 },
                    new RecapItem { id = "synthetic-arcana", name = "Synthetic Arcana", category = "Arcana", rank = 2 }
                }
            };
            string html = RunArchive.Html(run);
            Check(html.Contains("data-filter=") && html.Contains("pointerenter") && html.Contains("Escape"), "HTML retains interactive filters and item details without imported icons");
            Check(html.Contains(AppCredits.Email) && html.Contains(AppCredits.Creator), "report retains intentional creator credit");
            Check(!html.Contains("src='http") && !html.Contains("file:///"), "HTML does not depend on remote images or developer paths");
            var hostile = new RunRecap { profile = "synthetic", number = 2, result = "Failed", route = "Underworld", weapon = "Test", aspect = "Test", items = new[] { new RecapItem { id = "unknown", category = "Attack", name = "</script><img src=x onerror=alert(1)>", description = "</script><script>alert(2)</script>" } } };
            string escaped = RunArchive.Html(hostile);
            Check(!escaped.Contains("<img src=x") && !escaped.Contains("<script>alert(2)"), "report escapes untrusted item text");
            RunArchive.Sync(new SavedBuild { historyOnly = true, profile = "synthetic", completedRuns = 1, completedHistory = new[] { run } });
            Check(RunArchive.All().Length == 1 && RunArchive.All()[0].profile == "synthetic", "synthetic completed run archives without a game save");
            string png = RunArchive.ExportCard(run);
            using (var image = Image.FromFile(png)) Check(image.Width > 500 && image.Height > 500, "share image renders without game artwork");
            using (var tile = BoonArt.Tile(run.items[0])) { Check(tile.Text.Contains("Synthetic boon"), "missing-icon item tile remains readable"); }
            int pdfAt = Array.IndexOf(args, "--pdf-python");
            if (pdfAt >= 0 && pdfAt + 1 < args.Length) {
                File.WriteAllText(Path.Combine(Store.Root, "pdf-python.txt"), args[pdfAt + 1]);
                string bundle = System.Threading.Tasks.Task.Run(() => RecapExports.Bundle(run)).GetAwaiter().GetResult();
                using (var zip = ZipFile.OpenRead(bundle)) Check(zip.Entries.Count == 4 && zip.Entries.Any(e => e.Name.EndsWith(".pdf")) && !zip.Entries.Any(e => e.Name.EndsWith(".json")), "share ZIP contains only HTML, PDF, PNG and text");
            }
            Check(Vision.Requests == 0 && Vision.Captures == 0, "checks use no cloud requests, microphone, or screen capture");
            Console.WriteLine("Synthetic outputs: " + Store.Root);
            return 0;
        } catch (Exception ex) {
            Console.Error.WriteLine("FAIL " + ex);
            return 1;
        }
    }
}
}
