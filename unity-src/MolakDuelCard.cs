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

    public static void Show (bool win, string foe, string line, string sub)
    {
        cWin = win; cName = foe; cLine = line; cSub = sub ?? ""; cT = 0f; if (instance != null) instance.outAt = -1f;
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
    }

    // 0.4.68 결과판은 글 위에 겹쳐 뜨지 않는다(언메이커: "촌스럽게 화면을 가린다"): 글자리를 빈 종이로 덮고 그 안에 가라앉은 먹빛으로.
    // 누를 때까지 남고(다음 글은 MolakPlayMode 가 붙잡는다), 넘기기 중엔 0.6초, 자동이면 2.2초 뒤 저절로 걷힌다.
    float outAt = -1f;
    void OnGUI ()
    {
        if (cT < 0f) return;
        if (Event.current.type == EventType.Repaint) cT += Time.unscaledDeltaTime;
        const float Out = 0.35f;
        var sp = Naninovel.Engine.Initialized ? Naninovel.Engine.GetService<Naninovel.IScriptPlayer>() : null;
        float auto = sp != null && sp.Skipping ? 0.6f : MolakPlayMode.Auto ? 2.2f : -1f;
        if (outAt < 0f)
        {
            bool tap = Event.current.type == EventType.MouseDown && cT > 0.35f;
            if (tap || (auto > 0f && cT > auto)) { if (tap) Event.current.Use(); outAt = cT; MolakInputGuard.MuteFor(0.2f); }
        }
        if (outAt >= 0f && cT - outAt > Out) { cT = -1f; outAt = -1f; return; }
        float u = MolakTheme.U;
        EnsureStyles(u);
        GUI.depth = MolakGui.DepthVfx - 6;
        float a = cT < 0.25f ? cT / 0.25f : 1f;
        if (outAt >= 0f) a = Mathf.Clamp01(1f - (cT - outAt) / Out);
        var ta = MolakVfx.TextArea();
        var accent = cWin ? new Color(0.62f, 0.45f, 0.1f, 1f) : new Color(0.6f, 0.12f, 0.08f, 1f);
        var g = GUI.color;
        Box(ta, new Color(0.955f, 0.94f, 0.905f, 0.985f * a));
        float cy = ta.y + ta.height * 0.5f - 170f * u;
        GUI.color = new Color(1, 1, 1, a);
        MolakGui.Ink(sName, new Color(0.35f, 0.3f, 0.25f, 1f));
        GUI.Label(new Rect(ta.x, cy, ta.width, 52 * u), "— " + cName + " —", sName);
        float sc = 1f + Mathf.Clamp01(1f - cT / 0.25f) * 0.2f;
        var pivot = new Vector2(ta.center.x, cy + 150 * u);
        var box = new Rect(pivot.x - 95 * u, pivot.y - 95 * u, 190 * u, 190 * u);
        var old = GUI.matrix;
        GUIUtility.ScaleAroundPivot(new Vector2(sc, sc), pivot);
        Box(box, new Color(accent.r, accent.g, accent.b, 0.92f));
        MolakGui.Ink(sHanja, new Color(0.98f, 0.95f, 0.88f, 1f));
        GUI.Label(box, cWin ? "勝" : "敗", sHanja);
        GUI.matrix = old;
        MolakGui.Ink(sLine, new Color(0.16f, 0.13f, 0.1f, 1f));
        GUI.Label(new Rect(ta.x, cy + 262 * u, ta.width, 46 * u), cLine, sLine);
        MolakGui.Ink(sSub, accent);
        if (!string.IsNullOrEmpty(cSub)) GUI.Label(new Rect(ta.x, cy + 306 * u, ta.width, 44 * u), cSub, sSub);
        if (auto < 0f && outAt < 0f && cT > 0.8f) { MolakGui.Ink(sSub, new Color(0.45f, 0.4f, 0.34f, Mathf.Clamp01((cT - 0.8f) / 0.4f))); GUI.Label(new Rect(ta.x, ta.yMax - 46f * u, ta.width, 34f * u), "눌러서 넘긴다", sSub); }
        GUI.color = g;
    }
}
