#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Linq;
using System.Text;
using Naninovel;
using UnityEngine;

/// <summary>
/// 0.4.83 글 드러내기 점검(에디터 전용). 한 장면을 라벨에서 '보통 속도'로 틀고, 글이 다 드러나면 계속을 눌러 끝까지 간다.
/// 새 글마다 몇 프레임에 걸쳐 드러났는지 적는다 — 긴 글이 1~2프레임에 다 나오면 "한꺼번에"로 표시.
/// 마디 이름은 0.8초 뒤 누른 셈 치고 걷는다. 선택지는 첫 번째 열린 것.
///  · Run("Molak01", "Pro1", "", 60) → Status/Log
/// </summary>
public class MolakRevealTest : MonoBehaviour
{
    static MolakRevealTest I;
    public static string Status = "idle";
    public static readonly StringBuilder Log = new StringBuilder();
    public static int Instant, Messages;
    public static readonly StringBuilder Flags = new StringBuilder();   // 0.4.85 화면 점검: 그림·글 어긋남

    public static void Run (string script, string label, string sets, float seconds)
    {
        if (I == null) { var go = new GameObject("MolakRevealTest"); DontDestroyOnLoad(go); I = go.AddComponent<MolakRevealTest>(); }
        I.StopAllCoroutines();
        Log.Length = 0; Flags.Length = 0; Instant = 0; Messages = 0; Status = "running";
        I.StartCoroutine(I.Go(script, label, sets ?? "", seconds));
    }

    /// <summary>0.4.85 여러 장면을 차례로: "대본,라벨,변수,초§…". 화면 어긋남은 Flags 에 모인다.</summary>
    public static void RunMany (string jobs)
    {
        if (I == null) { var go = new GameObject("MolakRevealTest"); DontDestroyOnLoad(go); I = go.AddComponent<MolakRevealTest>(); }
        I.StopAllCoroutines(); Log.Length = 0; Flags.Length = 0; Instant = 0; Messages = 0; Status = "running";
        I.StartCoroutine(I.Many(jobs.Split('§')));
    }
    IEnumerator Many (string[] jobs)
    {
        foreach (var j in jobs)
        {
            var p = j.Split(','); if (p.Length < 2) continue;
            float sec = 60f; if (p.Length > 3) float.TryParse(p[3], out sec);
            Log.AppendLine("=== " + p[0] + "#" + p[1]);
            Status = "running";
            yield return StartCoroutine(Go(p[0].Trim(), p[1].Trim(), p.Length > 2 ? p[2] : "", sec));
        }
        Status = "done";
    }

    static TMPro.TMP_Text Dlg ()
    {
        // 0.4.85 극장판에선 쓰지 않는 기본 인쇄기의 자리표시 글(Lorem ipsum)이 같은 이름으로 남아 있다 — 실제로 찍히는 쪽을 고른다
        TMPro.TMP_Text any = null;
        foreach (var t in FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (t.gameObject.name != "DialogueText") continue;
            if (any == null) any = t;
            if (!t.isActiveAndEnabled || (t.text ?? "").StartsWith("Lorem")) continue;
            return t;
        }
        return any != null && (any.text ?? "").StartsWith("Lorem") ? null : any;
    }

    IEnumerator Go (string script, string label, string sets, float seconds)
    {
        var st = Engine.GetService<IStateManager>(); var sp = Engine.GetService<IScriptPlayer>();
        var pm = Engine.GetService<ITextPrinterManager>(); var im = Engine.GetService<IInputManager>();
        bool started = false;
        MolakPlayMode.SetSkipAll(false);
        st.ResetState(new string[0], () => {
            foreach (var n in MolakVarList.Nums) MolakStats.EnsureNum(n);
            foreach (var n in MolakVarList.Strings) MolakStats.EnsureStr(n);
            foreach (var n in MolakVarList.Globals) if (!n.StartsWith("g_pname")) MolakStats.EnsureNum(n);
            if (!MolakName.HasName) MolakName.Apply("연호");
            MolakStats.EnsureStr("g_lastend"); MolakStats.EnsureStr("g_lastcause"); MolakStats.EnsureStr("g_nxform"); MolakStats.EnsureStr("g_scars", "");
            MolakStats.Set("hp", 10); MolakStats.Set("hpmax", 10); MolakStats.Set("day", 20); MolakStats.Set("sw", 8); MolakStats.Set("ng", 4); MolakStats.Set("sl", 3); MolakStats.Set("mn", 20);
            foreach (var kv in sets.Split(';')) { var p = kv.Split('='); if (p.Length == 2) { float f; if (float.TryParse(p[1], out f)) MolakStats.Set(p[0].Trim(), f); else MolakStats.SetStr(p[0].Trim(), p[1].Trim()); } }
            started = true;
            return ScriptTrackExtensions.LoadAndPlayAtLabel(sp.MainTrack, script, label);
        });
        while (!started) yield return null;
        float t0 = Time.realtimeSinceStartup, chapSeen = -1f;
        string lastText = null; int frames = 0, startLen = 0; bool tracking = false; float revStart = 0f;
        var cont = im.GetInput("Continue"); int stopTries = 0;
        // 0.4.85 화면 점검 상태
        string pScene = MolakArt.DbgScene, pChar = MolakArt.DbgChar, oldCharAtScene = ""; int pLen = 0, afterFrames = 0;
        string gapTrace = ""; float pageClearAt = -9f, textStartAt = -9f, sceneChangedAt = -9f, pressAt = -9f, lastGrowAt = -9f; float charHiAt = -9f, charLoAt = -9f; string charHiName = "";
        System.Action<string> flag = m => { var tr0 = sp.MainTrack; string spot0 = tr0 != null ? tr0.PlaybackSpot.ToString() : ""; Flags.AppendLine(script + " " + spot0 + " | " + m); };
        while (Time.realtimeSinceStartup - t0 < seconds)
        {
            yield return null;
            var tr = sp.MainTrack;
            var printer = pm.GetActor(pm.DefaultPrinterId);
            var dt = Dlg();
            string txt = dt != null ? dt.text : null;
            float prog = printer != null ? printer.RevealProgress : 1f;
            {
                float now = Time.realtimeSinceStartup;
                string sc = MolakArt.DbgScene, ch = MolakArt.DbgChar; float ca = MolakArt.DbgCharA;
                int plen = Plain(txt).Trim().Length;
                bool dlgVisible = dt != null && dt.isActiveAndEnabled && dt.color.a > 0.05f;
                if (plen == 0 && pLen > 0) pageClearAt = now;
                if (plen > 0 && pLen == 0) textStartAt = now;
                if (pressAt > 0f && tr != null) { string c = (tr.PlayedCommand != null ? tr.PlayedCommand.GetType().Name + "@" + tr.PlayedCommand.PlaybackSpot.ToString().Replace(" ", "") : "-") + (tr.Playing ? "" : "(정지)") + (tr.AwaitingInput ? "(대기)" : ""); if (!gapTrace.EndsWith(c + " ")) gapTrace += c + " "; }
                if (plen > pLen) { if (pressAt > 0f && now - pressAt > 1.0f && !MolakVfx.ChapterHolding && MolakArt.DbgDipA < 0.05f && !MolakVfx.Holding) flag("글이 늦게 뜬다 " + (now - pressAt).ToString("0.00") + "초 [" + gapTrace.Trim() + "]"); pressAt = -9f; gapTrace = ""; lastGrowAt = now; }
                // 새 쪽의 글이 이미 떠 있는데 장면 그림이 뒤늦게 바뀐다
                if (sc != pScene && sc != "")
                {
                    if (textStartAt > pageClearAt && pageClearAt > 0f && now - textStartAt > 0.15f && now - textStartAt < 2f && plen > 0 && dlgVisible) flag("장면 그림이 글보다 늦게 바뀜 " + (now - textStartAt).ToString("0.00") + "초 (" + pScene + " → " + sc + ")");
                    sceneChangedAt = now; oldCharAtScene = pChar; afterFrames = 0;
                }
                // 장면이 바뀐 뒤에도 앞 인물이 남아 있다(잔상)
                if (now - sceneChangedAt < 0.6f && oldCharAtScene != "" && ch == oldCharAtScene && ca > 0.3f) { if (++afterFrames == 4) flag("잔상: 장면이 바뀐 뒤에도 " + ch + " 이(가) 남아 있다"); }
                // 같은 인물이 사라졌다 다시 나타남(깜빡임)
                if (ch != "" && ca > 0.85f) { if (charLoAt > charHiAt && charHiName == ch && now - charHiAt < 0.6f) flag("깜빡임: " + ch + " 이(가) 잠깐 사라졌다 다시 나타남"); charHiAt = now; charHiName = ch; }
                else if (ch == "" || ca < 0.15f) { if (charHiAt > 0f && now - charHiAt < 0.1f) charLoAt = now; }
                // 마디 대표 그림이 마디 이름이 걷힌 뒤에도 남아 글을 가린다
                if (!MolakVfx.ChapterHolding && MolakArt.DbgKeyA > 0.3f && plen > pLen) flag("대표 그림이 늦게 사라짐(글이 찍히는데 " + MolakArt.DbgKeyA.ToString("0.00") + ")");
                // 장면 전환 먹이 글 위에 남아 있다
                if (MolakArt.DbgDipA > 0.4f && plen > pLen && dlgVisible) flag("먹 덮개가 덮인 채 글이 찍힘");
                pScene = sc; pChar = ch; pLen = plen;
            }
            // 새 글
            if (txt != null && !ReferenceEquals(txt, lastText) && txt != lastText)
            {
                if (tracking) Finish(frames, startLen, revStart);
                int prevLen = Plain(lastText).Length, nowLen = Plain(txt).Length;
                startLen = lastText != null && Plain(txt).StartsWith(Plain(lastText).TrimEnd()) ? nowLen - prevLen : nowLen;   // 이어 쓴 글이면 늘어난 만큼
                lastText = txt; tracking = true; frames = 0; revStart = prog;
                string spot = tr != null ? tr.PlaybackSpot.ToString() : "";
                Log.Append(spot + " | " + Short(LastPara(txt)) + " | 시작 " + prog.ToString("0.00"));
            }
            if (tracking) { frames++; if (prog >= 0.999f) { Finish(frames, startLen, revStart); tracking = false; } }
            // 마디 이름: 0.8초 뒤 걷는다
            if (MolakVfx.ChapterHolding)
            {
                if (chapSeen < 0f) { chapSeen = Time.realtimeSinceStartup; Log.AppendLine("  [마디 이름] 뒤의 글: " + (dt != null ? Short(LastPara(dt.text)) : "-")); }
                else if (Time.realtimeSinceStartup - chapSeen > 0.8f)
                {
                    var v = FindFirstObjectByType<MolakVfx>(); var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                    typeof(MolakVfx).GetField("chapHold", bf).SetValue(v, false); typeof(MolakVfx).GetField("chapOutT", bf).SetValue(v, 0f);
                    chapSeen = -1f;
                }
                continue;
            }
            // 선택지
            var btns = FindObjectsByType<Naninovel.UI.ChoiceHandlerButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(b => b.isActiveAndEnabled && !b.Choice.Locked && b.GetComponent<UnityEngine.UI.Button>().interactable).ToArray();
            if (btns.Length > 0 && MolakTheme.ChoiceCount > 0)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                btns = btns.Where(b => b != null && b.isActiveAndEnabled).ToArray();
                if (btns.Length > 0) { Log.AppendLine("  [선택] " + Short(btns[0].GetComponentInChildren<TMPro.TMP_Text>().text)); btns[0].GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); pressAt = Time.realtimeSinceStartup; if (CapAfterChoice) { CapAfterChoice = false; StartCoroutine(Cap()); } }
                yield return new WaitForSecondsRealtime(0.3f);
                continue;
            }
            // 다 드러나고 입력을 기다리면 계속
            if (tr != null && tr.AwaitingInput && prog >= 0.999f && !tracking)
            {
                yield return new WaitForSecondsRealtime(0.25f);
                if (cont != null) { cont.Activate(1f); yield return null; cont.Activate(0f); pressAt = Time.realtimeSinceStartup; gapTrace = ""; }
                yield return new WaitForSecondsRealtime(0.1f);
            }
            if (tr != null && !tr.Playing && btns.Length == 0 && !tr.AwaitingInput && Time.realtimeSinceStartup - t0 > 3f)
            {   // 선택지는 마지막 글 뒤 한 번 더 눌러야 뜬다 — 눌러 보고 2초 기다린 뒤에도 없으면 멈춤
                if (stopTries++ < 3) { if (cont != null) { cont.Activate(1f); yield return null; cont.Activate(0f); } yield return new WaitForSecondsRealtime(0.7f); continue; }
                Log.AppendLine("■ 멈춤"); break;
            }
            stopTries = 0;
        }
        Status = "done";
    }

    public static bool CapAfterChoice;
    IEnumerator Cap ()
    {
        System.IO.Directory.CreateDirectory("Build/capc");
        float t0 = Time.realtimeSinceStartup; int n = 0;
        var sp = Engine.GetService<IScriptPlayer>(); var pm = Engine.GetService<ITextPrinterManager>();
        while (Time.realtimeSinceStartup - t0 < 3f)
        {
            var tr = sp.MainTrack; var pr = pm.GetActor(pm.DefaultPrinterId); var d = Dlg();
            Log.AppendLine("  cap" + n + " t=" + (Time.realtimeSinceStartup - t0).ToString("0.00") + " cmd=" + (tr.PlayedCommand != null ? tr.PlayedCommand.GetType().Name + tr.PlayedCommand.PlaybackSpot.LineIndex : "-") + " play=" + tr.Playing + " rev=" + (pr != null ? pr.RevealProgress.ToString("0.00") : "-") + " len=" + Plain(d != null ? d.text : "").Length + " a=" + (d != null ? d.color.a.ToString("0.00") : "-") + " hold=" + MolakTheme.ChoiceCount);
            ScreenCapture.CaptureScreenshot("Build/capc/c" + (n++).ToString("00") + ".png");
            for (int k = 0; k < 6; k++) yield return null;
        }
    }
    static void Finish (int frames, int len, float start)
    {
        Messages++;
        bool inst = len > 12 && frames <= 2;
        if (inst) Instant++;
        Log.AppendLine(" → " + frames + "프레임" + (inst ? "  ★한꺼번에" : ""));
    }
    static string Plain (string t) { return System.Text.RegularExpressions.Regex.Replace(t ?? "", "<[^>]+>", ""); }
    static string LastPara (string t) { var ps = (t ?? "").Split('\n').Where(x => x.Trim().Length > 0).ToArray(); return ps.Length > 0 ? ps[ps.Length - 1] : ""; }
    static string Short (string t) { t = Plain(t).Replace("\n", " ").Trim(); return t.Length > 24 ? t.Substring(0, 24) + "…" : t; }
}
#endif
