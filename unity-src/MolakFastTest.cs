#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using Naninovel;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
/// 0.4.91 점검 도구: '빨리'를 켜고 진짜 마우스 누름(입력 시스템 이벤트)으로 글자리를 눌러 가며 장면을 넘긴다. 선택지는 첫 번째 열린 것을 눌러 고른다.
/// 프레임마다 보이는 본문(글·본문 알파·덮개)을 적고, 누르지 않았는데 글이 바뀐 때·가려졌던 글이 갑자기 보인 때를 ★로 표시한다.
/// MolakFastTest.Run("Molak15", "Ch1", "mn=5", 40) → Build/fasttest.txt
/// </summary>
public class MolakFastTest : MonoBehaviour
{
    public static string Status = "-";
    static readonly System.Text.StringBuilder Log = new System.Text.StringBuilder();

    public static void Run (string script, string label, string sets, float seconds, float tapEvery = 0.9f)
    {
        var go = new GameObject("MolakFastTest");
        go.AddComponent<MolakFastTest>().StartCoroutine(Go(go, script, label, sets ?? "", seconds, tapEvery));
    }

    static TMPro.TMP_Text Dlg ()
    {
        var m = typeof(MolakFlow).GetMethod("DialogueText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return m != null ? m.Invoke(null, null) as TMPro.TMP_Text : null;
    }
    static string Short (string s) { s = System.Text.RegularExpressions.Regex.Replace(s ?? "", "<[^>]+>", "").Replace("\n", "⏎").Replace("​", ""); return s.Length > 40 ? "…" + s.Substring(s.Length - 40) : s; }

    static IEnumerator Go (GameObject self, string script, string label, string sets, float seconds, float tapEvery)
    {
        Log.Length = 0; Status = "run";
        var sp = Engine.GetService<IScriptPlayer>();
        var oldBeh = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        foreach (var kv in sets.Split(';', ',')) { var p = kv.Split('='); if (p.Length == 2) { float f; if (float.TryParse(p[1], out f)) MolakStats.Set(p[0].Trim(), f); else MolakStats.SetStr(p[0].Trim(), p[1].Trim()); } }
        MolakFlow.Trace = new System.Text.StringBuilder();
        var task = ScriptTrackExtensions.LoadAndPlayAtLabel(sp.MainTrack, script, label);
        yield return new WaitForSecondsRealtime(0.3f);
        MolakPlayMode.SetFast(true);
        var mouse = Mouse.current;
        float t0 = Time.unscaledTime, nextTap = t0 + tapEvery, lastTap = -9f;
        string lastTxt = null; float lastAlpha = 1f; int lastVis = -1, lastCc = 0;
        while (Time.unscaledTime - t0 < seconds)
        {
            if (!MolakPlayMode.Fast) MolakPlayMode.SetFast(true);
            float now = Time.unscaledTime;
            var d = Dlg();
            string txt = d != null ? d.text : "";
            int vis = d != null ? Mathf.Min(d.maxVisibleCharacters, d.textInfo.characterCount) : 0;
            float a = MolakScroll.BodyAlpha;
            bool tapRecent = now - lastTap < 0.25f;
            string mark = "";
            if (txt != lastTxt && lastTxt != null && !tapRecent) mark += " ★누르지 않았는데 글이 바뀜";
            if (a > 0.5f && lastAlpha <= 0.5f && vis > 0) mark += " ★가려졌던 글이 보임";
            int cc = MolakTheme.ChoiceCount;
            if (txt != lastTxt || Mathf.Abs(a - lastAlpha) > 0.3f || mark.Length > 0 || cc != lastCc)
                Log.AppendLine(string.Format("{0,6:0.00} 보임{1:0.0} 덮개{2:0.0} 선택지{7} 극장{8} [{3}/{4}] {5}{6}", now - t0, a, MolakVfx.ChapterCover, vis, d != null ? d.textInfo.characterCount : 0, Short(txt), mark, cc, MolakTheme.TheaterNow ? 1 : 0));
            lastCc = cc;
            lastTxt = txt; lastAlpha = a; lastVis = vis;
            if (now >= nextTap)
            {
                nextTap = now + tapEvery;
                var btn = MolakTheme.ChoiceCount == 0 ? null : FindObjectsByType<Naninovel.UI.ChoiceHandlerButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                    .Where(b => b.isActiveAndEnabled && !b.Choice.Locked && b.GetComponent<UnityEngine.UI.Button>().interactable).OrderBy(b => -b.transform.position.y).FirstOrDefault();
                Vector2 p;
                if (btn != null)
                {
                    var rt = btn.transform as RectTransform; var cv = btn.GetComponentInParent<Canvas>().rootCanvas;
                    p = RectTransformUtility.WorldToScreenPoint(cv.renderMode == RenderMode.ScreenSpaceOverlay ? null : cv.worldCamera, rt.TransformPoint(rt.rect.center));
                    Log.AppendLine(string.Format("{0,6:0.00} [선택] {1}", now - t0, Short(btn.GetComponentInChildren<TMPro.TMP_Text>().text)));
                }
                else { var tt = MolakScroll.TheaterTextGui; p = tt.width > 1f ? new Vector2(tt.center.x, Screen.height - tt.center.y) : new Vector2(Screen.width * 0.5f, Screen.height * 0.3f); Log.AppendLine(string.Format("{0,6:0.00} 누름", now - t0)); }
                InputSystem.QueueStateEvent(mouse, new MouseState { position = p, buttons = 1 });
                yield return null; yield return null; yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = p, buttons = 0 });
                lastTap = Time.unscaledTime;
            }
            yield return null;
        }
        Log.AppendLine("--- 흐름 기록");
        Log.Append(MolakFlow.Trace.ToString());
        MolakFlow.Trace = null;
        InputSystem.settings.editorInputBehaviorInPlayMode = oldBeh;
        MolakPlayMode.SetFast(false);
        System.IO.File.WriteAllText("Build/fasttest.txt", Log.ToString());
        Status = "done";
        Destroy(self);
    }
}
#endif
