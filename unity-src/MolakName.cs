using Naninovel;
using UnityEngine;

/// <summary>
/// 0.4.75 주인공 이름 — 플레이어가 직접 적는다.
/// 청운 명부의 맨 끝 줄에 적히는 이름이다. 회귀해도 남는다(g_ 전역 변수).
/// 대본에서는 {g_pname} 과 조사 짝 {g_pname_a}(부름: 아/야) {g_pname_i}(이/가) {g_pname_eun}(은/는) {g_pname_eul}(을/를) 로 쓴다.
/// </summary>
public class MolakName : MonoBehaviour
{
    public const int MaxLen = 6;
    public static bool Open { get; private set; }

    static MolakName inst;
    string text = "";
    string warn = "";
    TouchScreenKeyboard kb;
    GUIStyle sTitle, sSub, sField, sBtn, sWarn;
    float openedAt;

    public static string Name { get { return MolakStats.Str("g_pname", ""); } }
    public static bool HasName { get { return Name.Trim().Length > 0; } }

    /// <summary>이름과 조사 짝을 한꺼번에 적는다.</summary>
    public static void Apply (string name)
    {
        name = Clean(name);
        if (name.Length == 0) return;
        bool batchim = HasBatchim(name[name.Length - 1]);
        MolakStats.SetStr("g_pname", name);
        MolakStats.SetStr("g_pname_a", name + (batchim ? "아" : "야"));
        MolakStats.SetStr("g_pname_i", name + (batchim ? "이" : "가"));
        MolakStats.SetStr("g_pname_eun", name + (batchim ? "은" : "는"));
        MolakStats.SetStr("g_pname_eul", name + (batchim ? "을" : "를"));
    }

    /// <summary>검증 도구용: 화면을 거치지 않고 이름을 적고 닫는다.</summary>
    public static void AutoFill (string name)
    {
        Apply(name);
        if (inst != null && inst.kb != null) { inst.kb.active = false; inst.kb = null; }
        Open = false;
    }

    /// <summary>저장에 이름만 있고 조사 짝이 없을 때(옛 저장) 다시 맞춘다.</summary>
    public static void EnsurePairs ()
    {
        if (HasName && MolakStats.Str("g_pname_a", "").Length == 0) Apply(Name);
    }

    public static string Clean (string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new System.Text.StringBuilder();
        foreach (var c in s)
        {
            if (char.IsWhiteSpace(c) || c == '{' || c == '}' || c == '<' || c == '>' || c == '"' || c == '\\') continue;
            sb.Append(c);
            if (sb.Length >= MaxLen) break;
        }
        return sb.ToString();
    }

    static bool HasBatchim (char c)
    {
        if (c >= 0xAC00 && c <= 0xD7A3) return (c - 0xAC00) % 28 != 0;
        if (c >= '0' && c <= '9') return "013678".IndexOf(c) >= 0;   // 영·일·삼·육·칠·팔
        return false;
    }

    public static void Show ()
    {
        if (inst == null)
        {
            var go = new GameObject("MolakName");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<MolakName>();
        }
        inst.text = Name;
        inst.warn = "";
        inst.openedAt = Time.realtimeSinceStartup;
        Open = true;
        if (Application.isMobilePlatform && TouchScreenKeyboard.isSupported)
            inst.kb = TouchScreenKeyboard.Open(inst.text, TouchScreenKeyboardType.Default, false, false, false, false, "이름", MaxLen);
        else
        {
            var k = UnityEngine.InputSystem.Keyboard.current;
            if (k != null) k.SetIMEEnabled(true);
        }
    }

    void Commit ()
    {
        var n = Clean(text);
        if (n.Length == 0) { warn = "이름이 비어 있다. 한 글자 이상 적어야 한다."; return; }
        Apply(n);
        if (kb != null) { kb.active = false; kb = null; }
        Open = false;
    }

    // 0.4.80 명부의 빛(MolakPaper 의 낮 빛과 같다)
    static readonly Color Paper = new Color(0.961f, 0.941f, 0.890f, 1f), Card = new Color(1f, 0.988f, 0.961f, 1f),
        Ink = new Color(0.122f, 0.102f, 0.078f, 1f), SubC = new Color(0.431f, 0.369f, 0.298f, 1f), Red = new Color(0.549f, 0.161f, 0.122f, 1f);

    void Styles ()
    {
        if (sTitle != null) return;
        var f = MolakGui.Gowun;
        sTitle = new GUIStyle { font = MolakTypography.Heading(f), alignment = TextAnchor.MiddleCenter, wordWrap = true };
        // 0.4.80 창의 결: 명부 한 장 — 종이 바탕, 먹 글자, 맨 끝 줄은 검붉은 띠의 흰 칸, 먹색 「적는다」(명부라 늘 낮 빛)
        sTitle.normal.textColor = Ink;
        sSub = new GUIStyle(sTitle) { font = f };
        sSub.normal.textColor = SubC;
        sWarn = new GUIStyle(sSub);
        sWarn.normal.textColor = Red;
        sField = new GUIStyle(GUI.skin.label) { font = f, alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Clip };   // 바탕 없는 칸(textField 피부의 짙은 바탕을 쓰지 않는다)
        sField.normal.textColor = sField.focused.textColor = sField.hover.textColor = Ink;
        sField.normal.background = sField.focused.background = sField.hover.background = sField.active.background = null;
        sField.onNormal.background = sField.onFocused.background = sField.onHover.background = sField.onActive.background = null;
        sField.active.textColor = sField.onNormal.textColor = sField.onFocused.textColor = sField.onHover.textColor = sField.onActive.textColor = Ink;
        sField.border = new RectOffset(0, 0, 0, 0);
        sBtn = new GUIStyle(GUI.skin.label) { font = f, alignment = TextAnchor.MiddleCenter };
        MolakGui.Ink(sBtn, Paper);
    }

    void Update ()
    {
        if (!Open || kb == null) return;
        text = Clean(kb.text);
        if (kb.status == TouchScreenKeyboard.Status.Done) Commit();
        else if (kb.status == TouchScreenKeyboard.Status.Canceled || kb.status == TouchScreenKeyboard.Status.LostFocus)
            kb = null;   // 닫혀도 칸을 누르면 다시 연다
    }

    void OnGUI ()
    {
        if (!Open) return;
        Styles();
        GUI.depth = MolakGui.DepthBlackout - 10;
        float u = MolakGui.U, W = Screen.width, H = Screen.height;
        var old = GUI.color;
        GUI.color = Paper;
        GUI.DrawTexture(new Rect(0, 0, W, H), MolakGui.Px);
        GUI.color = old;

        sTitle.fontSize = Mathf.RoundToInt(64 * u);
        sSub.fontSize = sWarn.fontSize = Mathf.RoundToInt(34 * u);
        sField.fontSize = Mathf.RoundToInt(58 * u);
        sBtn.fontSize = Mathf.RoundToInt(42 * u);

        float cw = Mathf.Min(W - 64 * u, 900 * u), cx = (W - cw) * 0.5f, y = H * 0.30f;
        GUI.Label(new Rect(cx, y, cw, 90 * u), "청운 명부, 맨 끝 줄", sTitle); y += 110 * u;
        GUI.Label(new Rect(cx, y, cw, 130 * u), "마당을 쓸던 막내에게도 이름이 있었다.\n사형들은 늘 막내라고만 불렀다.", sSub); y += 170 * u;

        var fr = new Rect(cx + cw * 0.15f, y, cw * 0.7f, 110 * u);
        {   // 맨 끝 줄: 흰 칸 · 검붉은 띠 · 아래 가는 줄
            var o2 = GUI.color;
            GUI.color = Card; GUI.DrawTexture(fr, MolakGui.Px);
            GUI.color = new Color(0.55f, 0.48f, 0.40f, 0.35f); GUI.DrawTexture(new Rect(fr.x, fr.yMax - Mathf.Max(1f, u), fr.width, Mathf.Max(1f, u)), MolakGui.Px);
            GUI.color = Red; GUI.DrawTexture(new Rect(fr.x, fr.y, Mathf.Max(2f, 10f * u), fr.height), MolakGui.Px);
            GUI.color = o2;
        }
        if (kb != null || Application.isMobilePlatform)
        {
            // 휴대폰: 칸을 누르면 자판이 뜬다
            if (GUI.Button(fr, text.Length > 0 ? text : "이름을 누른다", sField))
                kb = TouchScreenKeyboard.Open(text, TouchScreenKeyboardType.Default, false, false, false, false, "이름", MaxLen);
        }
        else
        {
            GUI.SetNextControlName("molakname");
            var ss = GUI.skin.settings; Color sc = ss.selectionColor, cc = ss.cursorColor;
            ss.selectionColor = new Color(Red.r, Red.g, Red.b, 0.25f); ss.cursorColor = Ink;
            text = Clean(GUI.TextField(fr, text, MaxLen, sField));
            ss.selectionColor = sc; ss.cursorColor = cc;
            if (Time.realtimeSinceStartup - openedAt < 0.3f) GUI.FocusControl("molakname");
            var e = Event.current;
            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)) { Commit(); e.Use(); }
        }
        y += 140 * u;
        GUI.Label(new Rect(cx, y, cw, 60 * u), warn.Length > 0 ? warn : "회귀해도 이 이름은 남는다.", warn.Length > 0 ? sWarn : sSub); y += 100 * u;

        var old2 = GUI.color;
        GUI.color = Ink;
        var br = new Rect(cx + cw * 0.3f, y, cw * 0.4f, 100 * u);
        GUI.DrawTexture(br, MolakGui.Px);
        GUI.color = old2;
        if (GUI.Button(br, "적는다", sBtn)) Commit();
    }
}

/// <summary>@pname — 이름이 없을 때만(또는 again:true면 다시) 이름을 받는다. 받을 때까지 대본을 멈춘다.</summary>
[Alias("pname")]
public class PnameCommand : Command
{
    public BooleanParameter Again;
    public override async Awaitable Execute (ExecutionContext ctx)
    {
        MolakName.EnsurePairs();
        if (MolakName.HasName && !(Assigned(Again) && Again.Value)) return;
        MolakName.Show();
        while (MolakName.Open) await Awaitable.NextFrameAsync();
    }
}
