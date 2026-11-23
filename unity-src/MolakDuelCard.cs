using UnityEngine;

/// <summary>비무 결과 카드 — 승/패 한자 인장 + 상대 이름 + 서열 변동. 탭하면 닫힌다(탭이 대사로 새지 않도록 가드).</summary>
public class MolakDuelCard : MonoBehaviour
{
    static MolakDuelCard instance;
    Font gowun;
    GUIStyle sHanja, sName, sLine, sSub;
    float builtU = -1f;
    static bool cWin; static string cName = "", cLine = "", cSub = ""; static float cT = -1f;
    const float Dur = 2.6f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap ()
    {
        if (instance != null) return;
        var go = new GameObject("MolakDuelCard");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<MolakDuelCard>();
    }

    public static bool Visible => cT >= 0f;
    /// <summary>점검 도구가 누름 대신 쓴다(누른 것과 같이 걷힌다).</summary>
    public static void Dismiss () { if (instance != null && cT >= 0f && instance.outAt < 0f) { instance.outAt = cT; MolakInputGuard.MuteFor(0.2f); } }

    public static void Show (bool win, string foe, string line, string sub)
    {
        cWin = win; cName = foe; cLine = line; cSub = sub ?? ""; cT = 0f; if (win) MolakSfx.Win(); else MolakSfx.Lose(); if (instance != null) instance.outAt = -1f;
    }

    static Texture2D Px { get { return MolakGui.Px; } }
    void Box (Rect r, Color c) { var g = GUI.color; GUI.color = new Color(c.r, c.g, c.b, c.a * GUI.color.a); GUI.DrawTexture(r, Px); GUI.color = g; }

    void EnsureStyles (float u)
    {
        if (sHanja != null && Mathf.Approximately(builtU, u)) return;
        builtU = u;
        if (gowun == null) gowun = MolakGui.Gowun;
        System.Func<int, GUIStyle> mk = size =>
        {
            var s = new GUIStyle(GUI.skin.label){fontSize=Mathf.RoundToInt(size*u),alignment=TextAnchor.MiddleCenter,richText=true};
            if (gowun != null) s.font = gowun;
            return s;
        };
        sHanja = mk(120); sName = mk(36); sLine = mk(30); sSub = mk(26);
    }

    void Update ()
    {
        if (cT < 0f) return;
        // 화면 전체 가드: 카드가 떠 있는 동안 탭이 대사 진행으로 새지 않는다
        MolakInputGuard.Guard(new Rect(0, 0, Screen.width, Screen.height));
        // 0.4.86 시계·자동 걷힘·닫힘은 프레임마다(예전엔 OnGUI 의 Repaint 에서만 — 화면이 다시 그려지지 않으면 카드가 끝내 안 닫혀 대본이 멈췄다)
        cT += Time.unscaledDeltaTime;
        var sp = MolakVn.Engine.Initialized ? MolakVn.Engine.GetService<MolakVn.IScriptPlayer>() : null;
        float auto = sp != null && sp.Skipping ? 0.6f : MolakPlayMode.Auto ? 2.2f : -1f;
        if (outAt < 0f && auto > 0f && cT > auto) { outAt = cT; MolakInputGuard.MuteFor(0.2f); }
        if (outAt >= 0f && cT - outAt > 0.35f) { cT = -1f; outAt = -1f; }
    }

    // 0.4.68 결과판은 글 위에 겹쳐 뜨지 않는다(언메이커: "촌스럽게 화면을 가린다"): 글자리를 빈 종이로 덮고 그 안에 가라앉은 먹빛으로.
    // 누를 때까지 남고(다음 글은 MolakPlayMode 가 붙잡는다), 넘기기 중엔 0.6초, 자동이면 2.2초 뒤 저절로 걷힌다.
    float outAt = -1f;
    void OnGUI ()
    {
        if (cT < 0f) return;
        if (outAt < 0f)
        {
            bool tap = Event.current.type == EventType.MouseDown && cT > 0.35f;
            if (tap) { Event.current.Use(); outAt = cT; MolakInputGuard.MuteFor(0.2f); }
        }
        float u = MolakTheme.U;
        EnsureStyles(u);
        GUI.depth = MolakGui.DepthVfx - 6;
        float a = cT < 0.25f ? cT / 0.25f : 1f;
        if (outAt >= 0f) a = Mathf.Clamp01(1f - (cT - outAt) / 0.35f);
        var ta = MolakVfx.TextArea();
        // 0.4.80 창의 결: 글자리를 종이로 덮고 가운데에 흰 카드 한 장(왼쪽 띠). 勝은 검붉은 인장, 敗는 먹 인장.
        var accent = cWin ? MolakPaper.Accent : MolakPaper.Ink;
        var g = GUI.color;
        var gr = MolakPaper.Ground; Box(ta, new Color(gr.r, gr.g, gr.b, 0.985f * a));
        float cy = ta.y + ta.height * 0.5f - 170f * u;
        GUI.color = new Color(1, 1, 1, a);
        MolakPaper.CardFace(new Rect(ta.x + 30f * u, cy - 34f * u, ta.width - 60f * u, 400f * u), accent, u);
        MolakGui.Ink(sName, MolakPaper.Sub);
        GUI.Label(new Rect(ta.x, cy, ta.width, 52 * u), "— " + cName + " —", sName);
        // 0.4.85 인장(먹 원 · 붉은 획): 勝은 주홍, 敗는 검붉고 원이 끊긴다
        var pivot = new Vector2(ta.center.x, cy + 150 * u);
        var box = new Rect(pivot.x - 120 * u, pivot.y - 120 * u, 240 * u, 240 * u);
        MolakSeal.Draw(box, cWin ? "勝" : "敗", cT, a, MolakGui.Hanja, MolakPaper.Card, !MolakTheme.Night);
        MolakGui.Ink(sLine, MolakPaper.Ink);
        GUI.Label(new Rect(ta.x, cy + 262 * u, ta.width, 46 * u), cLine, sLine);
        MolakGui.Ink(sSub, cWin ? MolakPaper.Accent : MolakPaper.Sub);
        if (!string.IsNullOrEmpty(cSub)) GUI.Label(new Rect(ta.x, cy + 306 * u, ta.width, 44 * u), cSub, sSub);
        var spg = MolakVn.Engine.Initialized ? MolakVn.Engine.GetService<MolakVn.IScriptPlayer>() : null;
        bool autoOn = (spg != null && spg.Skipping) || MolakPlayMode.Auto;
        if (!autoOn && outAt < 0f && cT > 0.8f) { var sb = MolakPaper.Sub; MolakGui.Ink(sSub, new Color(sb.r, sb.g, sb.b, Mathf.Clamp01((cT - 0.8f) / 0.4f))); GUI.Label(new Rect(ta.x, ta.yMax - 46f * u, ta.width, 34f * u), "눌러서 넘긴다", sSub); }
        GUI.color = g;
    }
}
