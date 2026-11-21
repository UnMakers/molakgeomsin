#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using Naninovel;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
/// 0.4.90 점검 도구: 진짜 마우스 누름(입력 시스템 이벤트)으로 선택지를 고르고, 그 뒤 프레임마다 본문이 어떻게 바뀌는지 적는다.
/// 「선택지를 고른 직후 한 줄짜리 글이 읽기도 전에 넘어간다」를 재현하려고 만들었다.
/// MolakTapTest.Run("MolakEvents", "E_Acup", "외상", 0.12f) → Build/taptest.txt
/// </summary>
public class MolakTapTest : MonoBehaviour
{
    public static string Status = "-";
    static readonly System.Text.StringBuilder Log = new System.Text.StringBuilder();

    public static void Run (string script, string label, string pick, float hold, float watch = 2.5f)
    {
        var go = new GameObject("MolakTapTest");
        go.AddComponent<MolakTapTest>().StartCoroutine(Go(go, script, label, pick, hold, watch));
    }

    static string Body ()
    {
        var m = typeof(MolakFlow).GetMethod("DialogueText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var t = m != null ? m.Invoke(null, null) as TMPro.TMP_Text : null;
        if (t == null) return "(없음)";
        var s = System.Text.RegularExpressions.Regex.Replace(t.text ?? "", "<[^>]+>", "").Replace("\n", "⏎").Replace("​", "");
        int vis = t.maxVisibleCharacters;
        return (s.Length > 46 ? "…" + s.Substring(s.Length - 46) : s) + " [vis " + (vis > 9999 ? "all" : vis.ToString()) + "/" + t.textInfo.characterCount + "]";
    }

    static IEnumerator Go (GameObject self, string script, string label, string pick, float hold, float watch)
    {
        Log.Length = 0; Status = "run";
        var sp = Engine.GetService<IScriptPlayer>();
        var im = Engine.GetService<IInputManager>();
        var cont = im.GetInput("Continue");
        float t0 = Time.unscaledTime;
        System.Action onCont = () => Log.AppendLine(string.Format("{0,6:0.000}  ** Continue 시작 (muted={1})", Time.unscaledTime - t0, cont.Muted));
        cont.OnStart += onCont;
        var oldBeh = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        MolakFlow.Trace = new System.Text.StringBuilder();
        var task = ScriptTrackExtensions.LoadAndPlayAtLabel(sp.MainTrack, script, label);
        // 선택지가 뜨고 잠금이 풀릴 때까지
        float w0 = Time.unscaledTime; Naninovel.UI.ChoiceHandlerButton[] btns = null;
        while (Time.unscaledTime - w0 < 20f)
        {
            btns = FindObjectsByType<Naninovel.UI.ChoiceHandlerButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(b => b.isActiveAndEnabled && !b.Choice.Locked).OrderBy(b => -b.transform.position.y).ToArray();
            if (btns.Any(b => b.GetComponentInChildren<TMPro.TMP_Text>().text.Contains(pick))) break;
            if (sp.MainTrack.AwaitingInput) { cont.Activate(1f); yield return null; cont.Activate(0f); yield return new WaitForSecondsRealtime(0.3f); continue; }
            yield return null;
        }
        var target = btns == null ? null : btns.FirstOrDefault(b => b.GetComponentInChildren<TMPro.TMP_Text>().text.Contains(pick));
        if (target == null) { Status = "선택지 없음"; Finish(self, cont, onCont, oldBeh); yield break; }
        yield return new WaitForSecondsRealtime(MolakLayout.ChoiceLock + 0.4f);
        var rt = target.transform as RectTransform;
        var cv = target.GetComponentInParent<Canvas>().rootCanvas; var cam = cv.renderMode == RenderMode.ScreenSpaceOverlay ? null : cv.worldCamera;
        Vector2 p = RectTransformUtility.WorldToScreenPoint(cam, rt.TransformPoint(rt.rect.center));
        Log.AppendLine("선택: " + target.GetComponentInChildren<TMPro.TMP_Text>().text + " @ " + p);
        var mouse = Mouse.current;
        t0 = Time.unscaledTime;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = p, buttons = 0 });
        yield return null;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = p, buttons = 1 });
        Log.AppendLine(string.Format("{0,6:0.000}  누름", Time.unscaledTime - t0));
        float hEnd = Time.unscaledTime + hold; string last = "";
        while (Time.unscaledTime < hEnd) { Frame(t0, ref last); yield return null; }
        InputSystem.QueueStateEvent(mouse, new MouseState { position = p, buttons = 0 });
        Log.AppendLine(string.Format("{0,6:0.000}  뗌", Time.unscaledTime - t0));
        float wEnd = Time.unscaledTime + watch;
        while (Time.unscaledTime < wEnd) { Frame(t0, ref last); yield return null; }
        Log.AppendLine("--- 흐름 기록");
        Log.Append(MolakFlow.Trace.ToString());
        Status = "done";
        Finish(self, cont, onCont, oldBeh);
    }

    static void Frame (float t0, ref string last)
    {
        var sp = Engine.GetService<IScriptPlayer>();
        string b = Body() + " | await=" + sp.MainTrack.AwaitingInput;
        if (b != last) { Log.AppendLine(string.Format("{0,6:0.000}  {1}", Time.unscaledTime - t0, b)); last = b; }
    }

    static void Finish (GameObject self, IInputHandle cont, System.Action onCont, UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode oldBeh)
    {
        cont.OnStart -= onCont;
        InputSystem.settings.editorInputBehaviorInPlayMode = oldBeh;
        MolakFlow.Trace = null;
        System.IO.File.WriteAllText("Build/taptest.txt", Log.ToString());
        Destroy(self);
    }
}
#endif
