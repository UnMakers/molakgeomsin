using Naninovel;
using UnityEngine;

/// <summary>먹 테마 커스텀 타이틀 — 수묵 배경 + 처음부터/이어하기/환경설정/나가기.</summary>
public class MolakTitle : MonoBehaviour
{
    static MolakTitle instance;
    static Texture2D px;
    Texture2D bg;
    Font gowun;
    GUIStyle sHanja, sTitle, sTag, sBtn, sBtnDim, sVer;
    float builtU = -1f;
    float grace; // 시작 직후 재생 전 틈새 동안 타이틀 재표시 방지

    void Awake ()
    {
        foreach (var o in FindObjectsByType<MolakTitle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (o != this) Destroy(o.gameObject);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap ()
    {
        if (instance != null) return;
        var go = new GameObject("MolakTitle");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<MolakTitle>();
    }

    static Texture2D Px { get { if (px == null) { px = new Texture2D(1,1); px.SetPixel(0,0,Color.white); px.Apply(); } return px; } }
    void Box (Rect r, Color c) { var g = GUI.color; GUI.color = c; GUI.DrawTexture(r, Px); GUI.color = g; }
    float U { get { return MolakTheme.U; } }

    void EnsureStyles (float u)
    {
        if (sBtn != null && Mathf.Approximately(builtU, u)) return;
        builtU = u;
        if (gowun == null) gowun = Resources.Load<Font>("GowunBatang");
        if (bg == null) bg = Resources.Load<Texture2D>("MolakInkBg2");
        System.Func<int, Color, TextAnchor, GUIStyle> mk = (size, col, an) =>
        {
            var s = new GUIStyle(GUI.skin.label){fontSize=Mathf.RoundToInt(size*u),alignment=an,richText=true};
            if (gowun != null) s.font = gowun;
            s.normal.textColor = col; return s;
        };
        sHanja = mk(150, new Color(0.14f,0.11f,0.09f,1f), TextAnchor.MiddleCenter);
        sTitle = mk(52, new Color(0.32f,0.26f,0.21f,1f), TextAnchor.MiddleCenter);
        sTag = mk(30, new Color(0.42f,0.35f,0.28f,1f), TextAnchor.MiddleCenter);
        sBtn = mk(38, MolakTheme.Ink, TextAnchor.MiddleCenter);
        sBtnDim = mk(38, new Color(0.45f,0.40f,0.34f,1f), TextAnchor.MiddleCenter);
        sVer = mk(22, new Color(0.40f,0.35f,0.30f,1f), TextAnchor.MiddleCenter);
    }

    bool AnyUIVisible (IUIManager ui)
    {
        var a = ui.GetUI<Naninovel.UI.ISaveLoadUI>() as Naninovel.UI.IManagedUI;
        var b = ui.GetUI<Naninovel.UI.ISettingsUI>() as Naninovel.UI.IManagedUI;
        var c = ui.GetUI<Naninovel.UI.IConfirmationUI>() as Naninovel.UI.IManagedUI;
        return (a != null && a.Visible) || (b != null && b.Visible) || (c != null && c.Visible);
    }

    bool Btn (Rect r, string label, bool enabled, float u)
    {
        bool over = enabled && r.Contains(Event.current.mousePosition);
        Box(r, over ? new Color(0.19f,0.15f,0.12f,0.96f) : new Color(0.13f,0.10f,0.09f,0.92f));
        Box(new Rect(r.x,r.y,r.width,Mathf.Max(1f,1*u)), MolakTheme.Line);
        Box(new Rect(r.x,r.yMax-Mathf.Max(1f,1*u),r.width,Mathf.Max(1f,1*u)), MolakTheme.Line);
        GUI.Label(r, label, enabled ? sBtn : sBtnDim);
        return enabled && GUI.Button(r, GUIContent.none, GUIStyle.none);
    }

    void OnGUI ()
    {
        if (!Engine.Initialized) return;
        var ui = Engine.GetService<IUIManager>();
        var sp = Engine.GetService<IScriptPlayer>();
        var state = Engine.GetService<IStateManager>();
        if (ui == null || sp == null || state == null) return;

        // 기본 타이틀 UI 상시 숨김
        var defTitle = ui.GetUI<Naninovel.UI.ITitleUI>() as Naninovel.UI.IManagedUI;
        if (defTitle != null && defTitle.Visible) defTitle.ChangeVisibility(false, 0f);

        if (grace > 0f) { grace -= Time.unscaledDeltaTime; return; }
        float day = 0f;
        try { day = MolakStats.Get("day"); } catch {}
        bool show = !sp.Playing && day < 1f && !AnyUIVisible(ui);
        if (!show) return;

        float u = U; EnsureStyles(u);
        float w = Screen.width, h = Screen.height;

        // 수묵 배경 (커버 핏)
        Box(new Rect(0,0,w,h), MolakTheme.Bg);
        if (bg != null)
        {
            float scale = Mathf.Max(w/(float)bg.width, h/(float)bg.height);
            float bw = bg.width*scale, bh = bg.height*scale;
            var g = GUI.color; GUI.color = new Color(1,1,1,0.92f);
            GUI.DrawTexture(new Rect((w-bw)/2f,(h-bh)/2f,bw,bh), bg, ScaleMode.StretchToFill);
            GUI.color = g;
        }
        // 메뉴 치수 (버튼 5개가 항상 다 들어가도록 패널 높이를 버튼 수로 계산)
        float bw2 = Mathf.Min(w*0.74f, 720*u), bh2 = 104*u, gap = 20*u;
        int nBtn = 5;
        float menuH = nBtn*bh2 + (nBtn-1)*gap;
        float panelTop = Mathf.Min(h*0.62f, h - menuH - 150*u);
        panelTop = Mathf.Max(panelTop, h*0.45f);
        float bx = (w-bw2)/2f;
        float by = panelTop + Mathf.Max(40*u, (h - panelTop - menuH - 70*u)/2f);

        // 하단 먹 패널 (메뉴 영역)
        Box(new Rect(0,panelTop,w,h-panelTop), new Color(0.063f,0.051f,0.047f,0.88f));

        // 제호 — 상단 종이 영역의 세로 중앙 근처
        float headH = 340*u;
        float hy = Mathf.Clamp(panelTop*0.5f - headH*0.5f, 60*u, panelTop - headH - 40*u);
        GUI.Label(new Rect(0,hy,w,220*u), "沒落劍神", sHanja);
        GUI.Label(new Rect(0,hy+210*u,w,70*u), "몰 락 검 신", sTitle);
        GUI.Label(new Rect(0,hy+290*u,w,46*u), "베이고, 돌아가고, 다시 벤다.", sTag);

        bool canContinue = state.AnyGameSaveExists || state.QuickLoadAvailable;

        if (Btn(new Rect(bx,by,bw2,bh2), "처음부터", true, u))
        {
            grace = 6f;
            var player = sp;
            state.ResetState(new string[0], () => Naninovel.ScriptTrackExtensions.LoadAndPlay(player.MainTrack, "Entry"));
        }
        if (Btn(new Rect(bx,by+(bh2+gap),bw2,bh2), "이어하기", canContinue, u))
        {
            var sl = ui.GetUI<Naninovel.UI.ISaveLoadUI>() as Naninovel.UI.IManagedUI;
            if (sl != null) sl.ChangeVisibility(true, 0.15f);
        }
        if (Btn(new Rect(bx,by+2*(bh2+gap),bw2,bh2), "환경 설정", true, u))
        {
            var st = ui.GetUI<Naninovel.UI.ISettingsUI>() as Naninovel.UI.IManagedUI;
            if (st != null) st.ChangeVisibility(true, 0.15f);
        }
        int seenEnds = 0, runs = 0, clears = 0;
        try {
            var cv = Engine.GetService<ICustomVariableManager>();
            if (cv != null) {
                foreach (var e in MolakEnd.All) { var k = "g_end_" + e.Id; if (cv.VariableExists(k) && cv.GetVariableValue(k).Number > 0) seenEnds++; }
                if (cv.VariableExists("g_runs")) runs = (int)cv.GetVariableValue("g_runs").Number;
                if (cv.VariableExists("g_clears")) clears = (int)cv.GetVariableValue("g_clears").Number;
            }
        } catch {}
        string codexLabel = seenEnds > 0 ? "말로록   <size=" + Mathf.RoundToInt(26*u) + "><color=#9A8A78>末路 " + seenEnds + " / " + MolakEnd.All.Length + "</color></size>" : "말로록";
        if (Btn(new Rect(bx,by+3*(bh2+gap),bw2,bh2), codexLabel, true, u))
            MolakMenu.OpenCodex();
        if (Btn(new Rect(bx,by+4*(bh2+gap),bw2,bh2), "나가기", true, u))
            Application.Quit();

        string foot = "제1부 · 운락객잔" + (runs > 0 ? "  ·  회귀 " + runs + "회" : "") + (clears > 0 ? "  ·  등극 " + clears + "회" : "");
        GUI.Label(new Rect(0,h-50*u,w,36*u), foot, sVer);
    }
}
