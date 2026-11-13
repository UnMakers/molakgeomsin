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
        cWin = win; cName = foe; cLine = line; cSub = sub ?? ""; cT = 0f;
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
        sHanja = mk(132); sName = mk(36); sLine = mk(30); sSub = mk(26);
    }

    void Update ()
    {
        if (cT < 0f) return;
        // 화면 전체 가드: 카드가 떠 있는 동안 탭이 대사 진행으로 새지 않는다
        MolakInputGuard.Guard(new Rect(0, 0, Screen.width, Screen.height));
    }

    void OnGUI ()
    {
        if (cT < 0f) return;
        cT += Time.deltaTime;
        if (cT > Dur) { cT = -1f; return; }
        // 탭으로 닫기(페이드아웃으로 점프)
        if (Event.current.type == EventType.MouseDown && cT > 0.35f && cT < Dur - 0.4f) { cT = Dur - 0.4f; Event.current.Use(); }
        float u = MolakTheme.U;
        EnsureStyles(u);
        float w = Screen.width, h = Screen.height;
        float a = cT < 0.25f ? cT/0.25f : (cT > Dur-0.4f ? (Dur-cT)/0.4f : 1f);
        var accent = cWin ? MolakTheme.Gold : MolakTheme.Blood;
        var g = GUI.color; GUI.color = new Color(1,1,1,a);
        float cw = Mathf.Min(w*0.8f, 700*u), ch = (string.IsNullOrEmpty(cSub) ? 330 : 372)*u;
        float cx = (w-cw)/2f, cy = h*0.30f;
        Box(new Rect(cx,cy,cw,ch), new Color(0.05f,0.04f,0.035f,0.94f));
        Box(new Rect(cx,cy,cw,3*u), accent);
        Box(new Rect(cx,cy+ch-3*u,cw,3*u), accent);
        MolakGui.Ink(sName, MolakTheme.Ink3);
        MolakGui.Ink(sHanja, accent);
        MolakGui.Ink(sLine, new Color(0.90f,0.86f,0.80f,1f));
        MolakGui.Ink(sSub, MolakTheme.Gold);
        GUI.Label(new Rect(cx,cy+22*u,cw,52*u), "— " + cName + " —", sName);
        // 인장: 획이 번지듯 살짝 커지는 입장
        float sc = cWin ? 1f : 1f + Mathf.Clamp01(1f - cT/0.3f) * 0.25f;
        var old = GUI.matrix;
        var pivot = new Vector2(cx + cw/2f, cy + 160*u);
        GUIUtility.ScaleAroundPivot(new Vector2(sc, sc), pivot);
        GUI.Label(new Rect(cx,cy+78*u,cw,170*u), cWin ? "勝" : "敗", sHanja);
        GUI.matrix = old;
        GUI.Label(new Rect(cx,cy+258*u,cw,46*u), cLine, sLine);
        if (!string.IsNullOrEmpty(cSub)) GUI.Label(new Rect(cx,cy+306*u,cw,44*u), cSub, sSub);
        GUI.color = g;
    }
}
