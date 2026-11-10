using Naninovel;
using UnityEngine;

/// <summary>
/// 타이틀 — 붉은 달 아래 타는 산장(밤) / 진엔딩을 본 뒤엔 아침.
/// 배경이 아주 느리게 다가오고, 불티(밤)나 꽃잎(아침)이 흐르고, 달빛이 숨 쉬듯 일렁인다.
/// 제호는 오른쪽 세로쓰기 + 붉은 낙관. 메뉴는 상자 없이 붓글씨 행. 죽을수록 재가 짙어지고, 지난 생의 말로가 한 줄 남는다.
/// '처음부터'는 먹이 화면을 덮으며 "눈을 뜬다."로 넘어간다.
/// </summary>
public class MolakTitle : MonoBehaviour
{
    static MolakTitle instance;
    static Texture2D px, dot, grad;
    Texture2D night, dawn;
    Font gowun, hanjaFont;
    GUIStyle sHanja, sKor, sTag, sItem, sItemDim, sFoot, sLast, sSeal, sWake;
    float builtU = -1f;
    float grace;          // 시작 직후 재생 전 틈새 동안 타이틀 재표시 방지
    float shownAt = -1f;  // 이번에 타이틀이 나타난 시각(페이드 인)
    float wakeAt = -1f;   // '처음부터'를 누른 시각(먹 덮기)
    bool wakeFired;
    int pressed = -1; float pressedAt;

    struct Mote { public float x, y, v, sway, phase, size, life; }
    Mote[] motes;

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
    /// <summary>부드러운 원(불티·달무리).</summary>
    static Texture2D Dot
    {
        get
        {
            if (dot != null) return dot;
            const int n = 64; dot = new Texture2D(n, n, TextureFormat.RGBA32, false); dot.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx*dx + dy*dy)); a = a * a;
                dot.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            dot.Apply(); return dot;
        }
    }
    /// <summary>위는 투명, 아래는 먹색(메뉴 바닥).</summary>
    static Texture2D Grad
    {
        get
        {
            if (grad != null) return grad;
            const int n = 128; grad = new Texture2D(1, n, TextureFormat.RGBA32, false); grad.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < n; y++) { float t = 1f - y / (n - 1f); grad.SetPixel(0, y, new Color(1, 1, 1, Mathf.SmoothStep(0f, 1f, t))); }
            grad.Apply(); return grad;
        }
    }

    static Texture2D corner;
    /// <summary>왼쪽 아래가 짙고 오른쪽·위로 갈수록 투명해지는 먹 번짐(메뉴 바탕).</summary>
    static Texture2D Corner
    {
        get
        {
            if (corner != null) return corner;
            const int n = 128; corner = new Texture2D(n, n, TextureFormat.RGBA32, false); corner.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float fx = x / (n - 1f), fy = y / (n - 1f);           // fy=0 은 아래쪽
                float ax = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((fx - 0.15f) / 0.85f));
                float ay = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((fy - 0.45f) / 0.55f));
                corner.SetPixel(x, y, new Color(1, 1, 1, ax * ay));
            }
            corner.Apply(); return corner;
        }
    }

    void Box (Rect r, Color c) { var g = GUI.color; GUI.color = c * new Color(1,1,1,g.a); GUI.DrawTexture(r, Px); GUI.color = g; }
    void Tex (Rect r, Texture t, Color c) { var g = GUI.color; GUI.color = c * new Color(1,1,1,g.a); GUI.DrawTexture(r, t, ScaleMode.StretchToFill); GUI.color = g; }
    float U { get { return MolakTheme.U; } }

    void EnsureStyles (float u)
    {
        if (sItem != null && Mathf.Approximately(builtU, u)) return;
        builtU = u;
        if (gowun == null) gowun = Resources.Load<Font>("GowunBatang");
        if (hanjaFont == null) hanjaFont = Resources.Load<Font>("MolakHanja");
        if (night == null) night = Resources.Load<Texture2D>("MolakTitleNight");
        if (dawn == null) dawn = Resources.Load<Texture2D>("MolakTitleDawn");
        System.Func<int, Color, TextAnchor, GUIStyle> mk = (size, col, an) =>
        {
            var s = new GUIStyle(GUI.skin.label){fontSize=Mathf.RoundToInt(size*u),alignment=an,richText=true,wordWrap=false,clipping=TextClipping.Overflow};
            if (gowun != null) s.font = gowun;
            s.normal.textColor = col; return s;
        };
        sHanja = mk(128, new Color(0.96f,0.93f,0.86f,1f), TextAnchor.MiddleCenter);
        sKor = mk(32, new Color(0.86f,0.80f,0.70f,1f), TextAnchor.MiddleCenter);
        sTag = mk(30, new Color(0.88f,0.82f,0.72f,1f), TextAnchor.MiddleLeft);
        sItem = mk(42, new Color(0.95f,0.91f,0.84f,1f), TextAnchor.MiddleLeft);
        sItemDim = mk(42, new Color(0.55f,0.50f,0.45f,1f), TextAnchor.MiddleLeft);
        sFoot = mk(22, new Color(0.62f,0.57f,0.50f,1f), TextAnchor.MiddleLeft);
        sLast = mk(24, new Color(0.80f,0.32f,0.26f,1f), TextAnchor.MiddleLeft);
        sSeal = mk(46, new Color(0.98f,0.94f,0.88f,1f), TextAnchor.MiddleCenter);
        if (hanjaFont != null) { sHanja.font = hanjaFont; sSeal.font = hanjaFont; }
        sWake = mk(40, new Color(0.88f,0.82f,0.72f,1f), TextAnchor.MiddleCenter);
    }

    bool AnyUIVisible (IUIManager ui)
    {
        var a = ui.GetUI<Naninovel.UI.ISaveLoadUI>() as Naninovel.UI.IManagedUI;
        var b = ui.GetUI<Naninovel.UI.ISettingsUI>() as Naninovel.UI.IManagedUI;
        var c = ui.GetUI<Naninovel.UI.IConfirmationUI>() as Naninovel.UI.IManagedUI;
        return (a != null && a.Visible) || (b != null && b.Visible) || (c != null && c.Visible);
    }

    /// <summary>상자 없는 붓글씨 행. 눌리면 먹선이 왼쪽에서 그어진다.</summary>
    bool Item (int idx, Rect r, string label, bool enabled, float u, float t)
    {
        bool over = enabled && r.Contains(Event.current.mousePosition);
        // 인장 점
        float d = 14*u;
        Tex(new Rect(r.x, r.center.y - d/2f, d, d), Px, enabled ? new Color(0.70f,0.16f,0.12f, over ? 1f : 0.75f) : new Color(0.4f,0.35f,0.3f,0.5f));
        GUI.Label(new Rect(r.x + 36*u, r.y, r.width - 36*u, r.height), label, enabled ? sItem : sItemDim);
        // 밑줄: 늘 옅게, 누르면 붉게 그어짐
        float baseW = (r.width - 36*u) * 0.62f;
        Box(new Rect(r.x + 36*u, r.yMax - 10*u, baseW, Mathf.Max(1f, 1.5f*u)), new Color(0.9f,0.85f,0.78f, 0.16f));
        if (pressed == idx)
        {
            float k = Mathf.Clamp01((t - pressedAt) / 0.35f);
            Box(new Rect(r.x + 36*u, r.yMax - 12*u, baseW * Mathf.SmoothStep(0, 1, k), Mathf.Max(2f, 4f*u)), new Color(0.72f,0.15f,0.11f,0.95f));
        }
        else if (over) Box(new Rect(r.x + 36*u, r.yMax - 11*u, baseW * 0.35f, Mathf.Max(2f, 3f*u)), new Color(0.72f,0.15f,0.11f,0.8f));
        bool hit = enabled && GUI.Button(r, GUIContent.none, GUIStyle.none);
        if (hit) { pressed = idx; pressedAt = t; }
        return hit;
    }

    void InitMotes (int n, bool dawnMode, float w, float h)
    {
        motes = new Mote[n];
        for (int i = 0; i < n; i++) motes[i] = NewMote(dawnMode, w, h, true);
    }
    Mote NewMote (bool dawnMode, float w, float h, bool anywhere)
    {
        var m = new Mote();
        m.x = Random.value * w;
        m.y = anywhere ? Random.value * h : (dawnMode ? -20f : h + 20f);
        m.v = (dawnMode ? 18f : 26f) + Random.value * (dawnMode ? 22f : 60f);
        m.sway = 8f + Random.value * 26f; m.phase = Random.value * 6.28f;
        m.size = 2.5f + Random.value * (dawnMode ? 9f : 7f);
        m.life = Random.value;
        return m;
    }

    void OnGUI ()
    {
        if (!Engine.Initialized) return;
        var ui = Engine.GetService<IUIManager>();
        var sp = Engine.GetService<IScriptPlayer>();
        var state = Engine.GetService<IStateManager>();
        if (ui == null || sp == null || state == null) return;

        var defTitle = ui.GetUI<Naninovel.UI.ITitleUI>() as Naninovel.UI.IManagedUI;
        if (defTitle != null && defTitle.Visible) defTitle.ChangeVisibility(false, 0f);

        if (grace > 0f) { grace -= Time.unscaledDeltaTime; shownAt = -1f; return; }
        float day = 0f;
        try { day = MolakStats.Get("day"); } catch {}
        bool show = !sp.Playing && day < 1f && !AnyUIVisible(ui);
        if (!show) { shownAt = -1f; return; }

        float t = Time.unscaledTime;
        if (shownAt < 0f) { shownAt = t; wakeAt = -1f; wakeFired = false; pressed = -1; motes = null; }
        float u = U; EnsureStyles(u);
        float w = Screen.width, h = Screen.height;

        int seenEnds = 0, runs = 0, clears = 0; bool trueSeen = false; string lastEnd = "none";
        try
        {
            var cv = Engine.GetService<ICustomVariableManager>();
            foreach (var e in MolakEnd.All) if (MolakStats.Get("g_end_" + e.Id) > 0) seenEnds++;
            runs = (int)MolakStats.Get("g_runs"); clears = (int)MolakStats.Get("g_clears");
            trueSeen = MolakStats.Get("g_hapdone") > 0;
            if (cv != null && cv.VariableExists("g_lastend")) { var v = cv.GetVariableValue("g_lastend"); if (v.Type == CustomVariableValueType.String) lastEnd = v.String; }
        } catch {}
        bool dawnMode = trueSeen && dawn != null;
        var art = dawnMode ? dawn : night;

        float fadeIn = Mathf.Clamp01((t - shownAt) / 1.6f);
        var g0 = GUI.color; GUI.color = new Color(1, 1, 1, 1);

        // ---- 배경: 위에 맞추고, 아주 느리게 다가온다 ----
        Box(new Rect(0, 0, w, h), new Color(0.035f, 0.03f, 0.028f, 1f));
        float artH = h;
        if (art != null)
        {
            float zoom = 1.06f + 0.035f * Mathf.Sin((t - shownAt) * 0.045f);
            float aw = Mathf.Max(w, h * 0.80f * art.width / (float)art.height) * zoom;
            float ah = aw * art.height / (float)art.width;
            float ax = (w - aw) / 2f + Mathf.Sin(t * 0.03f) * 10f * u, ay = -ah * 0.02f;
            artH = ay + ah;
            Tex(new Rect(ax, ay, aw, ah), art, new Color(1, 1, 1, fadeIn));
            // 달무리(밤): 그림 속 달 자리 근처에서 숨 쉬듯
            if (!dawnMode)
            {
                float mx = ax + aw * 0.485f, my = ay + ah * 0.11f, mr = aw * (0.20f + 0.015f * Mathf.Sin(t * 0.9f));
                Tex(new Rect(mx - mr, my - mr, mr * 2, mr * 2), Dot, new Color(0.95f, 0.18f, 0.10f, (0.16f + 0.06f * Mathf.Sin(t * 0.7f)) * fadeIn));
            }
            else
            {
                float mx = ax + aw * 0.55f, my = ay + ah * 0.14f, mr = aw * 0.42f;
                Tex(new Rect(mx - mr, my - mr, mr * 2, mr * 2), Dot, new Color(1f, 0.92f, 0.70f, (0.18f + 0.05f * Mathf.Sin(t * 0.5f)) * fadeIn));
            }
        }
        // 회귀할수록 재가 짙어진다(밤)
        if (!dawnMode && runs > 0) Box(new Rect(0, 0, w, h), new Color(0.05f, 0.04f, 0.035f, Mathf.Min(0.28f, runs * 0.025f) * fadeIn));
        // 메뉴 바닥: 그림 아래쪽을 먹으로 녹인다
        float floorTop = h * 0.78f;
        Tex(new Rect(0, floorTop, w, h - floorTop), Grad, new Color(0.035f, 0.03f, 0.028f, 0.95f));
        // 메뉴 자리: 왼쪽 아래를 먹으로 누른다(가운데의 검객은 남긴다).
        Tex(new Rect(0, h * 0.38f, w * 0.62f, h * 0.62f), Corner, new Color(0.03f, 0.025f, 0.022f, 0.93f * fadeIn));

        // ---- 불티(밤) / 꽃잎(아침) ----
        int want = dawnMode ? 26 : Mathf.Min(110, 46 + runs * 6);
        if (motes == null || motes.Length != want) InitMotes(want, dawnMode, w, h);
        float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
        if (Event.current.type == EventType.Repaint)
            for (int i = 0; i < motes.Length; i++)
            {
                var m = motes[i];
                m.y += (dawnMode ? 1f : -1f) * m.v * u * 2f * dt;
                m.x += Mathf.Sin(t * 0.8f + m.phase) * m.sway * u * dt;
                m.life += dt * 0.25f;
                if (m.y < -30 || m.y > h + 30 || m.x < -30 || m.x > w + 30) m = NewMote(dawnMode, w, h, false);
                motes[i] = m;
                float sz = m.size * u * 2f, flick = 0.55f + 0.45f * Mathf.Sin(t * 3.1f + m.phase * 3f);
                Color c = dawnMode ? new Color(0.98f, 0.78f, 0.82f, 0.55f * fadeIn)
                                   : new Color(1f, 0.42f + 0.25f * flick, 0.12f, (0.35f + 0.5f * flick) * fadeIn * Mathf.Clamp01(m.y / (h * 0.25f)));
                Tex(new Rect(m.x - sz / 2, m.y - sz / 2, sz, sz), Dot, c);
            }

        // ---- 제호: 오른쪽 세로쓰기 + 낙관 ----
        string han = "沒落劍神";
        float colX = w - 150*u, cy = Mathf.Max(70*u, h * 0.06f), ch = 132*u;
        GUI.color = new Color(1, 1, 1, Mathf.Clamp01((t - shownAt - 0.4f) / 1.4f));
        for (int i = 0; i < han.Length; i++)
        {
            var r = new Rect(colX - 70*u, cy + i * ch, 140*u, ch);
            var sh = new GUIStyle(sHanja); sh.normal.textColor = new Color(0, 0, 0, 0.55f);
            GUI.Label(new Rect(r.x + 4*u, r.y + 5*u, r.width, r.height), han[i].ToString(), sh);
            GUI.Label(r, han[i].ToString(), sHanja);
        }
        string kor = "몰락검신";
        for (int i = 0; i < kor.Length; i++)
            GUI.Label(new Rect(colX - 150*u, cy + 40*u + i * 52*u, 50*u, 52*u), kor[i].ToString(), sKor);
        float sealY = cy + han.Length * ch + 18*u, sealS = 78*u;
        Box(new Rect(colX - sealS / 2f, sealY, sealS, sealS), new Color(0.68f, 0.13f, 0.10f, 0.92f));
        Box(new Rect(colX - sealS / 2f + 5*u, sealY + 5*u, sealS - 10*u, sealS - 10*u), new Color(0.75f, 0.17f, 0.13f, 0.92f));
        GUI.Label(new Rect(colX - sealS / 2f, sealY, sealS, sealS), "回", sSeal);

        // ---- 메뉴 ----
        GUI.color = new Color(1, 1, 1, Mathf.Clamp01((t - shownAt - 0.9f) / 1.0f));
        bool canContinue = state.AnyGameSaveExists || state.QuickLoadAvailable;
        float ih = 88*u, mx0 = 56*u, mw = Mathf.Min(w * 0.42f, 460*u);
        int n = 5;
        float my0 = h - 170*u - n * ih;
        float tagY = my0 - 78*u;
        string tag = dawnMode ? "베이고, 돌아가고, 마침내 아침." : "베이고, 돌아가고, 다시 벤다.";
        GUI.Label(new Rect(mx0, tagY, w - mx0 * 2, 50*u), tag, sTag);
        Box(new Rect(mx0, tagY + 54*u, 120*u, Mathf.Max(1f, 2f*u)), new Color(0.72f, 0.15f, 0.11f, 0.85f));

        bool busy = wakeAt > 0f;
        if (Item(0, new Rect(mx0, my0, mw, ih), runs > 0 ? "다시 그 아침으로" : "처음부터", !busy, u, t) && !busy)
            wakeAt = t;
        if (Item(1, new Rect(mx0, my0 + ih, mw, ih), "이어하기", canContinue && !busy, u, t))
        {
            var sl = ui.GetUI<Naninovel.UI.ISaveLoadUI>() as Naninovel.UI.IManagedUI;
            if (sl != null) sl.ChangeVisibility(true, 0.15f);
        }
        string codexLabel = seenEnds > 0 ? "말로록  <size=" + Mathf.RoundToInt(24*u) + "><color=#9A8A78>末路 " + seenEnds + " / " + MolakEnd.All.Length + "</color></size>" : "말로록";
        if (Item(2, new Rect(mx0, my0 + 2 * ih, mw, ih), codexLabel, !busy, u, t)) MolakMenu.OpenCodex();
        if (Item(3, new Rect(mx0, my0 + 3 * ih, mw, ih), "환경 설정", !busy, u, t))
        {
            var st = ui.GetUI<Naninovel.UI.ISettingsUI>() as Naninovel.UI.IManagedUI;
            if (st != null) st.ChangeVisibility(true, 0.15f);
        }
        if (Item(4, new Rect(mx0, my0 + 4 * ih, mw, ih), "나가기", !busy, u, t)) Application.Quit();

        // ---- 발치: 회귀 기록 · 지난 생의 말로 ----
        float fy = h - 96*u;
        if (runs > 0 && lastEnd != "none" && !string.IsNullOrEmpty(lastEnd))
        {
            var le = MolakEnd.Find(lastEnd);
            if (!string.IsNullOrEmpty(le.Hanja))
                GUI.Label(new Rect(mx0, fy - 6*u, w - mx0 * 2, 34*u), "지난 생 · 末路 " + le.Hanja + " — " + le.Title, sLast);
        }
        string foot = "제1부 · 운락객잔" + (runs > 0 ? "  ·  회귀 " + runs + "회" : "") + (clears > 0 ? "  ·  등극 " + clears + "회" : "");
        GUI.Label(new Rect(mx0, fy + 34*u, w - mx0 * 2, 30*u), foot, sFoot);

        // ---- 먹이 덮는다 → "눈을 뜬다." ----
        GUI.color = new Color(1, 1, 1, 1);
        if (wakeAt > 0f)
        {
            float k = Mathf.Clamp01((t - wakeAt) / 1.1f);
            Box(new Rect(0, 0, w, h), new Color(0.02f, 0.018f, 0.016f, Mathf.SmoothStep(0, 1, k)));
            float kt = Mathf.Clamp01((t - wakeAt - 0.6f) / 0.6f);
            var ws = new GUIStyle(sWake); ws.normal.textColor = new Color(0.88f, 0.82f, 0.72f, kt);
            GUI.Label(new Rect(0, h * 0.45f, w, 60*u), "눈을 뜬다.", ws);
            if (!wakeFired && t - wakeAt > 1.7f)
            {
                wakeFired = true; grace = 6f;
                var player = sp;
                state.ResetState(new string[0], () => Naninovel.ScriptTrackExtensions.LoadAndPlay(player.MainTrack, "Entry"));
            }
        }
        // 화면 전체 페이드 인
        if (fadeIn < 1f) Box(new Rect(0, 0, w, h), new Color(0.02f, 0.018f, 0.016f, 1f - fadeIn));
        GUI.color = g0;
    }
}
