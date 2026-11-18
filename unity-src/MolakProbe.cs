#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Linq;
using System.Text;
using Naninovel;
using UnityEngine;

/// <summary>
/// 0.4.63 장면 탐침(에디터 전용 검증 도구). 한 장면을 라벨에서 바로 틀고, 정해 둔 선택지를 차례로 눌러 끝까지 간다.
///  · Run("Molak11", "Gwak3", "aff=8;f_soamet=1", "아내에게|국을") — 변수는 모두 기본값으로 깐 뒤 덮어쓴다. 고를 글이 없으면 첫 선택지.
///  · 끝: @return/스크립트 끝/다른 대본으로 넘어감/선택지 20회/40초. 시나리오 오류는 Log 에 "ERR".
///  · 본문은 '빨리'로 넘기고, 지나간 글 줄을 Log 에 남긴다(어색한 문장 검토용).
/// </summary>
public class MolakProbe : MonoBehaviour
{
    static MolakProbe I;
    public static string Status = "idle";
    public static readonly StringBuilder Log = new StringBuilder();
    string script, label, sets; string[] picks; int pi; bool returned;

    /// <summary>여러 장면을 차례로: "대본,라벨,변수,선택§대본,라벨,변수,선택…"</summary>
    public static void RunMany (string jobs)
    {
        if (I == null) { var go = new GameObject("MolakProbe"); DontDestroyOnLoad(go); I = go.AddComponent<MolakProbe>(); }
        I.StopAllCoroutines(); Log.Length = 0; Status = "running";
        I.StartCoroutine(I.Many(jobs.Split('§')));
    }
    IEnumerator Many (string[] jobs)
    {
        foreach (var j in jobs)
        {
            var p = j.Split(',');
            if (p.Length < 2) continue;
            script = p[0].Trim(); label = p[1].Trim(); sets = p.Length > 2 ? p[2] : ""; picks = p.Length > 3 && p[3].Length > 0 ? p[3].Split('|') : new string[0]; pi = 0;
            Log.AppendLine("=== " + script + "#" + label + " {" + sets + "} " + (p.Length > 3 ? p[3] : ""));
            Status = "running";
            yield return StartCoroutine(Go());
            Status = "running";
        }
        Status = "done";
    }

    public static void Run (string script, string label, string sets, string picks)
    {
        if (I == null) { var go = new GameObject("MolakProbe"); DontDestroyOnLoad(go); I = go.AddComponent<MolakProbe>(); }
        I.StopAllCoroutines();
        I.script = script; I.label = label; I.sets = sets ?? ""; I.picks = string.IsNullOrEmpty(picks) ? new string[0] : picks.Split('|'); I.pi = 0;
        Log.Length = 0; Status = "running";
        I.StartCoroutine(I.Go());
    }

    void OnEnable () { Application.logMessageReceived += OnLog; }
    void OnDisable () { Application.logMessageReceived -= OnLog; }
    void OnLog (string msg, string st, LogType t)
    {
        if (Status != "running") return;
        if (msg.Contains("Failed to return to the last gosub")) { returned = true; return; } // 라벨을 직접 틀어 생기는 탐침 부산물: 장면 끝으로 본다
        if (t == LogType.Error || t == LogType.Exception || msg.Contains("Scenario script")) Log.AppendLine("ERR " + msg.Split('\n')[0]);
    }

    static void SetVar (string k, string v)
    {
        float f;
        if (float.TryParse(v, out f)) MolakStats.Set(k, f); else MolakStats.SetStr(k, v.Trim('"'));
    }

    IEnumerator Go ()
    {
        var st = Engine.GetService<IStateManager>(); var sp = Engine.GetService<IScriptPlayer>();
        bool started = false; returned = false;
        // 0.4.80 앞 작업이 ■ 로 선택지 앞에서 멈췄으면 그 단추가 남아 다음 작업에서 눌리던 것 — 시작 전에 걷는다
        foreach (var ob in FindObjectsByType<Naninovel.UI.ChoiceHandlerButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) if (ob != null) ob.gameObject.SetActive(false);
        st.ResetState(new string[0], () => {
            foreach (var n in MolakVarList.Nums) MolakStats.EnsureNum(n);
            foreach (var n in MolakVarList.Strings) MolakStats.EnsureStr(n);
            foreach (var n in MolakVarList.Globals) if (!n.StartsWith("g_pname")) MolakStats.EnsureNum(n);
            if (!MolakName.HasName) MolakName.Apply("연호");   // 0.4.75 탐침용 이름
            MolakStats.EnsureStr("g_lastend"); MolakStats.EnsureStr("g_lastcause"); MolakStats.EnsureStr("g_nxform"); MolakStats.EnsureStr("g_scars", "");
            MolakStats.Set("hp", 10); MolakStats.Set("hpmax", 10); MolakStats.Set("day", 20); MolakStats.Set("sw", 8); MolakStats.Set("ng", 4); MolakStats.Set("sl", 3); MolakStats.Set("mn", 20);
            foreach (var kv in sets.Split(';')) { var p = kv.Split('='); if (p.Length == 2) SetVar(p[0].Trim(), p[1].Trim()); }
            started = true;
            return ScriptTrackExtensions.LoadAndPlayAtLabel(sp.MainTrack, script, label);
        });
        float t0 = Time.realtimeSinceStartup; int choices = 0; string lastText = "";
        while (!started) yield return null;
        yield return null; yield return null;
        var tr = sp.MainTrack;
        { float tw = Time.realtimeSinceStartup; while (!tr.Playing && Time.realtimeSinceStartup - tw < 3f) yield return null; }   // 0.4.80 새 대본이 실제로 돌기 시작한 뒤부터 본다
        while (Time.realtimeSinceStartup - t0 < 40f)
        {
            MolakPlayMode.SetSkipAll(true);
            if (returned) { Log.AppendLine("■ @return(장면 끝)"); break; }
            if (MolakTurnCard.Showing || MolakFrenzy.Playing) { yield return null; continue; }
            var cur = tr.PlayedScript != null ? tr.PlayedScript.Path : "";
            var dt = DialogueText();
            if (dt != null && dt.text != lastText) { lastText = dt.text; }
            if (cur != script && cur != "" && cur != "MolakDuel") { Log.AppendLine("→ " + cur + " (끝)"); break; }
            if (tr.Playing) { yield return null; continue; }
            var btns = FindObjectsByType<Naninovel.UI.ChoiceHandlerButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(b => b.isActiveAndEnabled).ToArray();
            var lockedLabels = btns.Where(b => b.Choice.Locked).Select(b => "🔒" + (b.GetComponentInChildren<TMPro.TMP_Text>(true)?.text ?? "?").Replace("\n", " ")).ToArray();
            btns = btns.Where(b => !b.Choice.Locked).ToArray();
            if (lockedLabels.Length > 0 && btns.Length > 0) Log.AppendLine("  잠김: " + string.Join(" / ", lockedLabels));
            if (btns.Length == 0)
            {
                yield return new WaitForSecondsRealtime(0.3f);
                if (!tr.Playing && FindObjectsByType<Naninovel.UI.ChoiceHandlerButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length == 0) { Log.AppendLine("■ 멈춤(끝)"); break; }
                continue;
            }
            var labels = btns.Select(b => (b.GetComponentInChildren<TMPro.TMP_Text>(true)?.text ?? "?").Split('\n')[0]).ToArray();
            int k = 0;
            if (pi < picks.Length && picks[pi] == "■") { foreach (var pg in MolakPageBack.Pages) Log.AppendLine("  · 지난 쪽(" + pg.Replace("\n", " ⏎ ") + ")"); Log.AppendLine("  · 쪽(" + (dt != null ? dt.text.Replace("\n", " ⏎ ") : "") + ")"); Log.AppendLine("  · 선택지[" + string.Join(" / ", labels) + "]"); Log.AppendLine("■ 선택지 앞에서 멈춤(화면 확인용)"); MolakPlayMode.SetSkipAll(false); Status = "done"; yield break; }
            if (pi < picks.Length) { for (int i = 0; i < labels.Length; i++) if (labels[i].Contains(picks[pi])) { k = i; break; } pi++; }
            Log.AppendLine("  · 판(" + lastText.Replace("\n", " ⏎ ").Substring(Mathf.Max(0, lastText.Length - 160)) + ")");
            Log.AppendLine("▶ [" + string.Join(" / ", labels) + "] → " + labels[k]);
            float tc = Time.realtimeSinceStartup; int tries = 0;
            while (!tr.Playing && Time.realtimeSinceStartup - tc < 3f)
            {
                if (btns[k] == null || !btns[k].isActiveAndEnabled) break;
                if (tries == 0 || Time.realtimeSinceStartup - tc > 0.5f * tries) { btns[k].GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); tries++; }
                yield return null;
            }
            foreach (var b in btns) if (b != null && b.isActiveAndEnabled && !tr.Playing) b.gameObject.SetActive(false); // 지나간 판의 단추만 — 새로 뜬 단추는 건드리지 않는다
            if (++choices >= 20) { Log.AppendLine("■ 선택 20회"); break; }
        }
        var dt2 = DialogueText();
        Log.AppendLine("  · 마지막 글(" + (dt2 != null ? dt2.text.Replace("\n", " ⏎ ") : "") + ")");
        Log.AppendLine("vars hp=" + MolakStats.Get("hp") + " curse=" + MolakStats.Get("curse") + " sl=" + MolakStats.Get("sl") + " ng=" + MolakStats.Get("ng"));
        MolakPlayMode.SetSkipAll(false);
        Status = "done";
    }

    static TMPro.TMP_Text DialogueText ()
    {
        foreach (var panel in FindObjectsByType<Naninovel.UI.RevealableTextPrinterPanel>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            foreach (var t0 in panel.GetComponentsInChildren<TMPro.TMP_Text>(false))
                if (t0.gameObject.name == "DialogueText") return t0;
        return null;
    }
}
#endif
