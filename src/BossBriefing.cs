using System;
namespace GameCoach {
static class BossBriefing {
 static string name="",source="";static DateTime seen;
 internal static void Remember(string boss,SavedBuild b){if(BossCards.Tips(boss).Length==0)return;name=boss;source=b==null?"":b.sourceRun;seen=DateTime.UtcNow;}
 internal static string Question(SavedBuild b){string boss=b!=null&&source==b.sourceRun&&DateTime.UtcNow-seen<TimeSpan.FromMinutes(8)?name:BossCards.Name(b);return "Explain the opening tips for "+(boss.Length>0?boss:"the boss visible on screen")+" using my current build. Give a short practical explanation of the most important pattern and my safest damage window. This is a one-time briefing, not live attack timing. If the boss cannot be identified from current evidence, ask which boss. "+(boss.Length>0?"Reference patterns: "+string.Join(" ",BossCards.Tips(boss)):"");}
}
}
