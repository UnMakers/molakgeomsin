using System.Collections;
using System.Text;
using Naninovel;
using Naninovel.Commands;
using UnityEngine;

/// <summary>
/// 0.4.94 인연별 이야기 모드 통짜 시험(편집기 재생 중에만 부른다). MolakStoryTest.Run(편, 분)
///  · 제목 화면의 「○○ 편」을 누른 것과 같이 시작해, 넘기기로 끝까지 읽힌다. 선택은 MolakStoryAuto 가 한다(이 도구는 고르지 않는다).
///  · Build/story_r{편}.txt 에 마디(@chapter)·사건·고른 말·끝(@ending)·생을 적는다. 진짜 끝(g_trueclear) 이나 시간 끝에서 멈춘다.
/// </summary>
public class MolakStoryTest : MonoBehaviour
{
    public static string Status = "idle";
    static MolakStoryTest inst;
    static string LogPath (int r) => System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), "Build", "story_r" + r + ".txt");
    int route; float until; string lastPick = ""; float lastDay = -1f; string path;
    IScriptPlayer sp;

    public static void Run (int route, float minutes)
    {
        if (inst == null) { var go = new GameObject("MolakStoryTest"); DontDestroyOnLoad(go); inst = go.AddComponent<MolakStoryTest>(); }
        inst.StopAllCoroutines();
        inst.route = route; inst.until = Time.realtimeSinceStartup + minutes * 60f; inst.path = LogPath(route);
        System.IO.File.WriteAllText(inst.path, "# 인연별 이야기 모드 시험 · 편 " + route + "\n", new UTF8Encoding(false));
        Status = "running";
        inst.StartCoroutine(inst.Go());
    }
    public static void Stop () { if (inst != null) { inst.StopAllCoroutines(); Status = "stopped"; var p = Engine.GetService<IScriptPlayer>(); if (p != null) { p.SetSkip(false); p.SetAutoPlay(false); } } }

    void W (string s) { try { System.IO.File.AppendAllText(path, s + "\n", new UTF8Encoding(false)); } catch { } }

    IEnumerator Go ()
    {
        sp = Engine.GetService<IScriptPlayer>();
        var st = Engine.GetService<IStateManager>();
        typeof(MolakStoryBook).GetMethod("FreshRoute", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, new object[] { route });
        MolakStats.Set("g_storypick", 1f);
        sp.OnExecute -= OnExec; sp.OnExecute += OnExec;
        bool started = false;
        st.ResetState(new string[0], () => { started = true; return ScriptTrackExtensions.LoadAndPlay(sp.MainTrack, "Entry"); });
        float t0 = Time.realtimeSinceStartup;
        while (!started && Time.realtimeSinceStartup - t0 < 20f) yield return null;
        float lastMove = Time.realtimeSinceStartup; int lastIdx = -1;
        while (Time.realtimeSinceStartup < until)
        {
            yield return new WaitForSecondsRealtime(0.1f);
            if (MolakName.Open) MolakName.AutoFill("연호");
            sp.SetAutoPlay(true); sp.SetSkip(true);
            var tr = sp.MainTrack;
            if (tr.PlayedIndex != lastIdx) { lastIdx = tr.PlayedIndex; lastMove = Time.realtimeSinceStartup; }
            float day = MolakStats.Get("day");
            if (day != lastDay) { lastDay = day; W("— 날 " + day + "  검" + MolakStats.Get("sw") + " 기" + MolakStats.Get("ng") + " 사술" + MolakStats.Get("curse") + " 정" + MolakStats.Get("aff") + "/" + MolakStats.Get("amhaff") + "/" + MolakStats.Get("cmaff") + "/" + MolakStats.Get("chamaff")); }
            if (MolakStoryAuto.LastPick != lastPick) { lastPick = MolakStoryAuto.LastPick; W("   ▸ " + lastPick); lastMove = Time.realtimeSinceStartup; }
            if (MolakStats.Get("g_trueclear") > 0f) { W("== 진짜 끝 · " + MolakStats.Str("deathcause")); Status = "true:" + MolakStats.Str("deathcause"); break; }
            if (Time.realtimeSinceStartup - lastMove > 25f)
            {
                var cmd = tr.PlayedCommand;
                W("== 멈춤 " + (tr.PlayedScript != null ? tr.PlayedScript.Path : "-") + " idx=" + tr.PlayedIndex + " cmd=" + (cmd != null ? cmd.GetType().Name + "@" + cmd.PlaybackSpot : "null"));
                Status = "stall"; break;
            }
        }
        if (Status == "running") { W("== 시간 끝"); Status = "timeout"; }
        sp.OnExecute -= OnExec;
        sp.SetSkip(false); sp.SetAutoPlay(false);
    }

    void OnExec (IScriptTrack track)
    {
        var cmd = track != null ? track.PlayedCommand : null;
        if (cmd == null) return;
        if (cmd is ChapterCommand cc && cc.Text != null && cc.Text.HasValue) W("【" + cc.Text.Value + "】 " + (track.PlayedScript != null ? track.PlayedScript.Path : ""));
        else if (cmd is EndingCommand) W("== 끝 · " + MolakStats.Str("deathcause") + " (생 " + MolakStats.Get("g_runs") + ")");
        else if (cmd is Gosub g && g.Path != null && g.Path.HasValue && (g.Path.Value.Contains("MolakEvents") || g.Path.Value.Contains("#E_"))) W("  · 사건 " + (g.Path.Value.Contains("{") ? MolakStats.Str("nextevent") : g.Path.Value));
    }
}
