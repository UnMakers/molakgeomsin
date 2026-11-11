using Naninovel;
using UnityEngine;

/// <summary>
/// 타이틀 — 여백 큰 수묵 한 폭: 고요한 수면 위의 검객, 붉은 달, 달을 감싼 원상(圓相).
/// 원상은 처음 뜰 때 거꾸로 감기며 멈추고(되감기는 시간), 그 뒤로는 아주 천천히 역방향으로 돈다.
/// 바탕은 늘 같고, 겪은 일에 따라 그림자가 하나씩 더해진다:
///  회귀할 때마다 수면의 등불 하나 · 소아와 함께한 결말 → 곁의 쌍검(그녀가 죽은 밤을 겪었으면 반투명)
///  진백의 진실 → 먼 물가의 사형 · 원수 → 물 위의 흰 상복 셋 · 입마 → 물에 꽂힌 검 · 성사 → 등 뒤의 노인
///  교주를 벤 결말 → 달에 금 · 합벽(진엔딩) → 달이 새벽 해로, 두 사람이 나란히.
/// </summary>
public class MolakTitle : MonoBehaviour
{
    static MolakTitle instance;
    static Texture2D px, dot, disk;
    Texture2D night, enso, silSoa, silJin, silOld, silMourn;
    Font gowun, hanjaFont;
    GUIStyle sHanja, sSub, sItem, sItemDim, sTag, sFoot, sLast, sSeal, sWake;
    float builtU = -1f;
    float grace;
    float shownAt = -1f, wakeAt = -1f;
    bool wakeFired;
    int pressed = -1; float pressedAt;

    struct Mote { public float x, y, v, sway, phase, size; }
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
    static Texture2D MakeRound (int n, bool soft)
    {
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false); t.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
        {
            float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f, d = Mathf.Sqrt(dx*dx + dy*dy);
            float a = soft ? Mathf.Pow(Mathf.Clamp01(1f - d), 2f) : Mathf.Clamp01((1f - d) * n * 0.5f);
            t.SetPixel(x, y, new Color(1, 1, 1, a));
        }
        t.Apply(); return t;
    }
    static Texture2D Dot { get { if (dot == null) dot = MakeRound(64, true); return dot; } }
    static Texture2D Disk { get { if (disk == null) disk = MakeRound(256, false); return disk; } }

    void Box (Rect r, Color c) { var g = GUI.color; GUI.color = c * new Color(1,1,1,g.a); GUI.DrawTexture(r, Px); GUI.color = g; }
    void Tex (Rect r, Texture t, Color c) { if (t == null) return; var g = GUI.color; GUI.color = c * new Color(1,1,1,g.a); GUI.DrawTexture(r, t, ScaleMode.StretchToFill); GUI.color = g; }
    float U { get { return MolakTheme.U; } }

    void EnsureStyles (float u)
    {
        if (sItem != null && Mathf.Approximately(builtU, u)) return;
        builtU = u;
        if (gowun == null) gowun = Resources.Load<Font>("GowunBatang");
        if (hanjaFont == null) hanjaFont = Resources.Load<Font>("MolakHanja");
        if (night == null) night = Resources.Load<Texture2D>("MolakTitleNight");
        if (enso == null) enso = Resources.Load<Texture2D>("MolakTitleEnso");
        if (silSoa == null) silSoa = Resources.Load<Texture2D>("MolakTitleSil_soa");
        if (silJin == null) silJin = Resources.Load<Texture2D>("MolakTitleSil_jinbaek");
        if (silOld == null) silOld = Resources.Load<Texture2D>("MolakTitleSil_lawlord");
        if (silMourn == null) silMourn = Resources.Load<Texture2D>("MolakTitleSil_hajinmu");
        System.Func<int, Color, TextAnchor, GUIStyle> mk = (size, col, an) =>
        {
            var s = new GUIStyle(GUI.skin.label){fontSize=Mathf.RoundToInt(size*u),alignment=an,richText=true,wordWrap=false,clipping=TextClipping.Overflow};
            if (gowun != null) s.font = gowun;
            s.normal.textColor = col; return s;
        };
        sHanja = mk(104, new Color(0.94f,0.91f,0.85f,1f), TextAnchor.MiddleCenter);
        sSub = mk(26, new Color(0.80f,0.68f,0.42f,1f), TextAnchor.MiddleCenter);
        sItem = mk(36, new Color(0.15f,0.12f,0.10f,1f), TextAnchor.MiddleRight);
        sItemDim = mk(36, new Color(0.42f,0.39f,0.36f,1f), TextAnchor.MiddleRight);
        sTag = mk(24, new Color(0.30f,0.26f,0.22f,1f), TextAnchor.MiddleRight);
        sFoot = mk(21, new Color(0.22f,0.20f,0.19f,1f), TextAnchor.MiddleLeft);
        sLast = mk(22, new Color(0.55f,0.14f,0.11f,1f), TextAnchor.MiddleLeft);
        sSeal = mk(34, new Color(0.98f,0.94f,0.88f,1f), TextAnchor.MiddleCenter);
        sWake = mk(36, new Color(0.88f,0.82f,0.72f,1f), TextAnchor.MiddleCenter);
        if (hanjaFont != null) { sHanja.font = hanjaFont; sSub.font = hanjaFont; sSeal.font = hanjaFont; }
    }

    bool AnyUIVisible (IUIManager ui)
    {
        var a = ui.GetUI<Naninovel.UI.ISaveLoadUI>() as Naninovel.UI.IManagedUI;
        var b = ui.GetUI<Naninovel.UI.ISettingsUI>() as Naninovel.UI.IManagedUI;
        var c = ui.GetUI<Naninovel.UI.IConfirmationUI>() as Naninovel.UI.IManagedUI;
        return (a != null && a.Visible) || (b != null && b.Visible) || (c != null && c.Visible) || MolakSettings.IsOpen;
    }

    /// <summary>
    /// 붓 한 획 위의 글자 — 버튼마다 마른 붓으로 그은 먹 획(세 가지 붓결을 돌려 쓴다)을 깔고, 그 위에 종이색 글씨.
    /// 첫 획(주 행동)은 주홍 인주색. 손가락이 닿아 있는 동안 획이 짙어지고 살짝 번지며, 떼는 순간 실행한다.
    /// </summary>
    int downIdx = -1;
    Texture2D[] brushes;
    bool Item (int idx, Rect r, string label, bool enabled, float u, float t, bool primary)
    {
        if (brushes == null) brushes = new[]{ Resources.Load<Texture2D>("MolakTitleBrush0"), Resources.Load<Texture2D>("MolakTitleBrush1"), Resources.Load<Texture2D>("MolakTitleBrush2") };
        var e = Event.current;
        if (enabled && e.type == EventType.MouseDown && r.Contains(e.mousePosition)) downIdx = idx;
        if (e.type == EventType.MouseUp && downIdx == idx && !r.Contains(e.mousePosition)) downIdx = -1;
        bool down = downIdx == idx && enabled;
        // 획: 버튼 칸보다 위아래로 조금 크게, 오른쪽 끝(꼬리)은 칸 밖으로 흘린다
        var tex = brushes[idx % brushes.Length];
        float grow = down ? 1.06f : 1f;
        float bw2 = r.width * 1.12f * grow, bh2 = r.height * 1.35f * grow;
        var br = new Rect(r.x - r.width * 0.04f - (bw2 - r.width * 1.12f) * 0.5f, r.center.y - bh2 / 2f, bw2, bh2);
        Color ink = primary ? new Color(0.62f, 0.12f, 0.09f, 1f) : new Color(0.09f, 0.08f, 0.08f, 1f);
        float a = !enabled ? 0.30f : down ? 1f : 0.90f;
        // 획이 처음 그어지는 모양: 메뉴가 뜰 때 왼쪽부터 순서대로 번진다
        float reveal = Mathf.Clamp01((t - shownAt - 1.1f - idx * 0.12f) / 0.45f);
        if (reveal < 1f)
        {
            GUI.BeginGroup(new Rect(br.x, br.y, br.width * Mathf.SmoothStep(0, 1, reveal), br.height));
            Tex(new Rect(0, 0, br.width, br.height), tex, new Color(ink.r, ink.g, ink.b, a));
            GUI.EndGroup();
        }
        else Tex(br, tex, new Color(ink.r, ink.g, ink.b, a));
        var st = new GUIStyle(enabled ? sItem : sItemDim){ alignment = TextAnchor.MiddleCenter };
        st.normal.textColor = enabled ? new Color(0.96f, 0.93f, 0.87f, 1f) : new Color(0.80f, 0.77f, 0.72f, 0.8f);
        // 글씨는 먹이 짙은 앞쪽(획의 왼쪽 60%) 가운데에
        var lr = new Rect(r.x, r.y + (down ? 2*u : 0), r.width * 0.78f, r.height);
        var sh = new GUIStyle(st); sh.normal.textColor = new Color(0, 0, 0, 0.35f);
        GUI.Label(new Rect(lr.x + 1.5f*u, lr.y + 2*u, lr.width, lr.height), label, sh);
        GUI.Label(lr, label, st);
        bool hit = enabled && GUI.Button(r, GUIContent.none, GUIStyle.none);
        if (hit) { pressed = idx; pressedAt = t; downIdx = -1; MolakSfx.Confirm(); }
        return hit;
    }

    static float G (string k) { try { return MolakStats.Get(k); } catch { return 0f; } }

    /// <summary>실루엣(발끝 기준) + 물그림자.</summary>
    void Sil (Texture2D tex, float fx, float fy, float hgt, Color c, float refl)
    {
        if (tex == null) return;
        float wd = hgt * tex.width / (float)tex.height;
        Tex(new Rect(fx - wd / 2f, fy - hgt, wd, hgt), tex, c);
        if (refl > 0f)
        {
            var m = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(1f, -0.55f), new Vector2(fx, fy));
            Tex(new Rect(fx - wd / 2f, fy - hgt, wd, hgt), tex, new Color(c.r, c.g, c.b, c.a * refl));
            GUI.matrix = m;
        }
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
        float day = G("day");
        bool show = !sp.Playing && day < 1f && !AnyUIVisible(ui);
        if (!show) { shownAt = -1f; return; }

        float t = Time.unscaledTime;
        if (shownAt < 0f) { shownAt = t; wakeAt = -1f; wakeFired = false; pressed = -1; motes = null; }
        float since = t - shownAt;
        float u = U; EnsureStyles(u);
        float w = Screen.width, h = Screen.height;

        int runs = (int)G("g_runs"), clears = (int)G("g_clears"), seenEnds = 0;
        foreach (var e in MolakEnd.All) if (G("g_end_" + e.Id) > 0) seenEnds++;
        string lastEnd = MolakStats.Str("g_lastend", "none");
        bool dawn = G("g_hapdone") > 0;
        bool soaWith = dawn || G("g_end_donghaeng") > 0 || G("g_end_wonhaeng") > 0;
        bool soaGhost = !dawn && G("g_soadied") > 0;
        bool jinShore = G("g_jbtruth") > 0;
        bool mourners = G("g_end_wonsu") > 0;
        bool swordInWater = G("g_end_ipma") > 0;
        bool oldMan = G("g_end_seongsa") > 0;
        bool moonCut = G("g_end_sicheon") > 0 || G("g_end_sungeom") > 0;

        float fadeIn = Mathf.Clamp01(since / 1.8f);
        var g0 = GUI.color; GUI.color = Color.white;

        // ---- 바탕: 높이에 맞추되, 물가(그림의 0.879)가 화면 0.88 근처에 오게 ----
        Box(new Rect(0, 0, w, h), new Color(0.07f, 0.075f, 0.075f, 1f));
        float aw = w, ah = h, ax = 0, ay = 0;
        if (night != null)
        {
            float breathe = 1.0f;   // 화면 전체는 움직이지 않는다(달빛·물결만 살짝)
            ah = h * breathe; aw = ah * night.width / (float)night.height;
            if (aw < w * breathe) { aw = w * breathe; ah = aw * night.height / (float)night.width; }
            ax = (w - aw) * 0.5f;
            ay = Mathf.Clamp(h * 0.88f - ah * 0.879f, h - ah, 0f);
            Tex(new Rect(ax, ay, aw, ah), night, new Color(1, 1, 1, fadeIn));
        }
        System.Func<float, float> X = ix => ax + ix * aw;
        System.Func<float, float> Y = iy => ay + iy * ah;
        if (dawn) Box(new Rect(0, 0, w, h), new Color(1f, 0.78f, 0.45f, 0.13f * fadeIn));

        // ---- 달 · 원상 ----
        float mcx = X(0.494f), mcy = Y(0.1213f), mr = aw * 0.0529f;
        Color moonCol = dawn ? new Color(0.98f, 0.80f, 0.45f, 0.95f) : new Color(0.74f, 0.16f, 0.12f, 0.92f);
        Tex(new Rect(mcx - mr * 3.2f, mcy - mr * 3.2f, mr * 6.4f, mr * 6.4f), Dot, new Color(moonCol.r, moonCol.g, moonCol.b, (0.20f + 0.05f * Mathf.Sin(t * 0.6f)) * fadeIn));
        Tex(new Rect(mcx - mr * 1.02f, mcy - mr * 1.02f, mr * 2.04f, mr * 2.04f), Disk, new Color(moonCol.r, moonCol.g, moonCol.b, moonCol.a * fadeIn));
        if (moonCut && !dawn)
        {
            var m = GUI.matrix; GUIUtility.RotateAroundPivot(-28f, new Vector2(mcx, mcy));
            Box(new Rect(mcx - mr * 1.25f, mcy - Mathf.Max(1f, 1.2f*u), mr * 2.5f, Mathf.Max(1.5f, 2.4f*u)), new Color(0.06f, 0.06f, 0.06f, 0.85f * fadeIn));
            GUI.matrix = m;
        }
        if (enso != null)
        {
            // 처음 2.6초: 거꾸로 감기며 멈춘다(되감기). 그 뒤로는 아주 천천히 역방향으로.
            float k = Mathf.Clamp01(since / 2.6f), ease = 1f - Mathf.Pow(1f - k, 3f);
            float ang = -540f * (1f - ease) - since * 1.2f;
            float er = mr * 2.05f * (1.12f - 0.12f * ease);
            var m = GUI.matrix; GUIUtility.RotateAroundPivot(ang, new Vector2(mcx, mcy));
            Color ec = dawn ? new Color(0.62f, 0.48f, 0.22f, 0.85f) : new Color(0.92f, 0.88f, 0.80f, 0.78f);
            Tex(new Rect(mcx - er, mcy - er, er * 2f, er * 2f), enso, new Color(ec.r, ec.g, ec.b, ec.a * Mathf.Clamp01(since / 1.2f)));
            GUI.matrix = m;
        }

        // ---- 겪은 일의 그림자 ----
        float feet = Y(0.879f), heroH = ah * 0.183f;
        if (oldMan) Sil(silOld, X(0.16f), feet, heroH * 1.7f, new Color(0.08f, 0.08f, 0.09f, 0.16f * fadeIn), 0f);
        if (jinShore) Sil(silJin, X(0.68f), Y(0.874f), heroH * 0.30f, new Color(0.10f, 0.10f, 0.11f, 0.55f * fadeIn), 0.35f);
        if (soaWith || soaGhost) Sil(silSoa, X(0.49f), feet, heroH * 0.94f, new Color(0.05f, 0.05f, 0.06f, (soaWith && !soaGhost ? 0.96f : 0.38f) * fadeIn), 0.28f);
        if (swordInWater)
        {
            float sx = X(0.57f), sy0 = Y(0.905f), sy1 = Y(0.84f);
            var m = GUI.matrix; GUIUtility.RotateAroundPivot(7f, new Vector2(sx, sy0));
            Box(new Rect(sx - 1.6f*u, sy1, 3.2f*u, sy0 - sy1), new Color(0.07f, 0.07f, 0.08f, 0.9f * fadeIn));
            Box(new Rect(sx - 9f*u, sy1 + 10f*u, 18f*u, 3f*u), new Color(0.07f, 0.07f, 0.08f, 0.9f * fadeIn));
            GUI.matrix = m;
        }
        if (mourners)
            for (int i = 0; i < 3; i++)
                Sil(silMourn, X(0.70f + i * 0.055f), Y(0.952f - i * 0.006f), heroH * (0.34f - i * 0.02f), new Color(0.93f, 0.92f, 0.89f, 0.85f * fadeIn), 0.3f);

        // ---- 물결: 수면에 가는 빛줄기가 아주 느리게 흐른다 ----
        for (int i = 0; i < 9; i++)
        {
            float hy = Mathf.Repeat(Mathf.Sin(i * 41.7f + 3f) * 9137.1f, 1f), hx = Mathf.Repeat(Mathf.Sin(i * 17.3f + 5f) * 5531.3f, 1f);
            float wy = Y(0.895f + hy * 0.085f);
            float len = aw * (0.06f + hx * 0.10f);
            float wx = Mathf.Repeat(X(0f) + hx * aw + t * (4f + hy * 5f) * u, aw + len) + X(0f) - len;
            float a = (0.10f + 0.10f * Mathf.Sin(t * (0.5f + hx * 0.6f) + i * 1.9f)) * fadeIn;
            Box(new Rect(wx, wy, len, Mathf.Max(1f, 1.4f * u)), new Color(0.97f, 0.96f, 0.92f, Mathf.Max(0f, a)));
        }
        // ---- 수면의 등불: 지나온 생마다 하나 ----
        int lamps = Mathf.Min(runs, 14);
        for (int i = 0; i < lamps; i++)
        {
            float hx = Mathf.Repeat(Mathf.Sin(i * 12.9898f + 1f) * 43758.5453f, 1f), hy = Mathf.Repeat(Mathf.Sin(i * 78.233f + 2f) * 12345.678f, 1f);
            float lx = X(0.06f + hx * 0.88f) + Mathf.Sin(t * 0.3f + i) * 4f * u;
            float ly = Y(0.905f + hy * 0.075f) + Mathf.Sin(t * 0.8f + i * 1.7f) * 1.5f * u;
            float ls = (9f + hy * 7f) * u, flick = 0.8f + 0.2f * Mathf.Sin(t * 2.3f + i * 3f);
            Tex(new Rect(lx - ls * 2.2f, ly - ls * 2.2f, ls * 4.4f, ls * 4.4f), Dot, new Color(1f, 0.66f, 0.30f, 0.30f * flick * fadeIn));
            Box(new Rect(lx - ls * 0.45f, ly - ls * 0.6f, ls * 0.9f, ls * 0.75f), new Color(0.98f, 0.82f, 0.55f, 0.92f * fadeIn));
            Box(new Rect(lx - ls * 0.15f, ly + ls * 0.3f, ls * 0.3f, ls * 1.6f), new Color(1f, 0.75f, 0.45f, 0.22f * flick * fadeIn));
        }

        // ---- 떠다니는 것: 아주 느린 꽃잎(새벽은 금빛 먼지) ----
        int want = 18;
        if (motes == null || motes.Length != want)
        {
            motes = new Mote[want];
            for (int i = 0; i < want; i++) motes[i] = new Mote { x = Random.value * w, y = Random.value * h, v = 6f + Random.value * 10f, sway = 10f + Random.value * 20f, phase = Random.value * 6.28f, size = 3f + Random.value * 4f };
        }
        float dt = Mathf.Min(0.05f, Time.unscaledDeltaTime);
        for (int i = 0; i < motes.Length; i++)
        {
            var mo = motes[i];
            if (Event.current.type == EventType.Repaint)
            {
                mo.y += mo.v * u * dt; mo.x += Mathf.Sin(t * 0.5f + mo.phase) * mo.sway * u * dt;
                if (mo.y > h + 20) { mo.y = -20; mo.x = Random.value * w; }
                motes[i] = mo;
            }
            float sz = mo.size * u * 2f;
            Color pc = dawn ? new Color(1f, 0.86f, 0.55f, 0.55f * fadeIn) : new Color(0.86f, 0.45f, 0.45f, 0.45f * fadeIn);
            Tex(new Rect(mo.x - sz / 2, mo.y - sz / 2, sz, sz * 0.7f), Dot, pc);
        }

        // ---- 제호: 오른쪽 세로쓰기 + 금빛 가는 선 + 前生 回歸 + 낙관 ----
        GUI.color = new Color(1, 1, 1, Mathf.Clamp01((since - 0.6f) / 1.6f));
        string han = "沒落劍神";
        float colX = w - 118*u, cy = Mathf.Max(64*u, h * 0.055f), ch = 118*u;
        for (int i = 0; i < han.Length; i++)
        {
            var r = new Rect(colX - 60*u, cy + i * ch, 120*u, ch);
            var sh = new GUIStyle(sHanja); sh.normal.textColor = new Color(0, 0, 0, 0.45f);
            GUI.Label(new Rect(r.x + 2*u, r.y + 3*u, r.width, r.height), han[i].ToString(), sh);
            GUI.Label(r, han[i].ToString(), sHanja);
        }
        float lineX = colX - 74*u;
        Box(new Rect(lineX, cy + 18*u, Mathf.Max(1f, 1.2f*u), han.Length * ch - 36*u), new Color(0.80f, 0.68f, 0.42f, 0.75f));
        string sub = "前生回歸";
        for (int i = 0; i < sub.Length; i++)
            GUI.Label(new Rect(lineX - 44*u, cy + 40*u + i * 44*u, 40*u, 44*u), sub[i].ToString(), sSub);
        float sealY = cy + han.Length * ch + 14*u, sealS = 54*u;
        Box(new Rect(colX - sealS / 2f, sealY, sealS, sealS), new Color(0.62f, 0.13f, 0.10f, 0.9f));
        GUI.Label(new Rect(colX - sealS / 2f, sealY, sealS, sealS), "回", sSeal);

        // ---- 메뉴: 물안개 위 오른쪽 아래, 먹 글씨 ----
        GUI.color = new Color(1, 1, 1, Mathf.Clamp01((since - 1.2f) / 1.0f));
        // 패 하나 = 손가락 한 마디(약 9mm)보다 크게. 나가기는 폰의 홈·뒤로 가기에 맡긴다.
        float bh = 104*u, gap = 22*u, ih = bh + gap, n = 4;
        float my0 = Mathf.Min(h * 0.56f, h - 200*u - n * ih);
        float rx = w - 52*u, mw = Mathf.Min(w * 0.52f, 560*u);
        Tex(new Rect(rx - mw * 1.3f, my0 - 80*u, mw * 1.6f, n * ih + 230*u), Dot, new Color(0.97f, 0.96f, 0.93f, dawn ? 0.25f : 0.35f));
        string tag = dawn ? "베이고, 돌아가고, 마침내 아침." : "베이고, 돌아가고, 다시 벤다.";
        GUI.Label(new Rect(rx - mw, my0 - 60*u, mw, 40*u), tag, sTag);
        Box(new Rect(rx - 90*u, my0 - 16*u, 90*u, Mathf.Max(1f, 1f*u)), new Color(0.55f, 0.14f, 0.11f, 0.8f));
        bool busy = wakeAt > 0f;
        bool canContinue = state.AnyGameSaveExists || state.QuickLoadAvailable || state.AutoLoadAvailable;
        if (Item(0, new Rect(rx - mw, my0, mw, bh), runs > 0 ? "다시 그 아침으로" : "처음부터", !busy, u, t, true) && !busy) { wakeAt = t; MolakSfx.Rewind(); }
        if (Item(1, new Rect(rx - mw, my0 + ih, mw, bh), "이어하기", canContinue && !busy, u, t, false))
        {
            // 가장 최근 기록(자동·빠른 저장)으로 곧장 — 없으면 저장 목록
            if (!MolakAutoSave.ContinueLatest())
            {
                var sl = ui.GetUI<Naninovel.UI.ISaveLoadUI>() as Naninovel.UI.IManagedUI;
                if (sl != null) sl.ChangeVisibility(true, 0.15f);
            }
        }
        string codexLabel = seenEnds > 0 ? "말로록  <size=" + Mathf.RoundToInt(22*u) + "><color=#C9B58E>末路 " + seenEnds + "/" + MolakEnd.All.Length + "</color></size>" : "말로록";
        if (Item(2, new Rect(rx - mw, my0 + 2 * ih, mw, bh), codexLabel, !busy, u, t, false)) MolakMenu.OpenCodex();
        if (Item(3, new Rect(rx - mw, my0 + 3 * ih, mw, bh), "환경 설정", !busy, u, t, false))
        {
            MolakSettings.Open();
        }

        // ---- 발치: 지난 생의 말로 · 회귀 기록 ----
        // 검객의 물그림자를 피해 오른쪽 아래, 메뉴 바로 밑 안개 자락에 둔다
        float fy = my0 + n * ih + 4*u;
        var sLastR = new GUIStyle(sLast){ alignment = TextAnchor.MiddleRight };
        var sFootR = new GUIStyle(sFoot){ alignment = TextAnchor.MiddleRight };
        if (runs > 0 && !string.IsNullOrEmpty(lastEnd) && lastEnd != "none")
        {
            var le = MolakEnd.Find(lastEnd);
            if (!string.IsNullOrEmpty(le.Hanja)) GUI.Label(new Rect(rx - mw * 1.4f, fy, mw * 1.4f, 30*u), "지난 생 · 末路 " + le.Hanja + " — " + le.Title, sLastR);
        }
        string foot = "제1부 · 운락객잔" + (runs > 0 ? "  ·  회귀 " + runs + "회" : "") + (clears > 0 ? "  ·  등극 " + clears + "회" : "");
        GUI.Label(new Rect(rx - mw * 1.4f, fy + 32*u, mw * 1.4f, 28*u), foot, sFootR);

        // ---- 먹이 덮는다 → "눈을 뜬다." ----
        GUI.color = Color.white;
        if (wakeAt > 0f)
        {
            float k = Mathf.Clamp01((t - wakeAt) / 1.1f);
            Box(new Rect(0, 0, w, h), new Color(0.02f, 0.018f, 0.016f, Mathf.SmoothStep(0, 1, k)));
            var ws = new GUIStyle(sWake); ws.normal.textColor = new Color(0.88f, 0.82f, 0.72f, Mathf.Clamp01((t - wakeAt - 0.6f) / 0.6f));
            GUI.Label(new Rect(0, h * 0.45f, w, 60*u), "눈을 뜬다.", ws);
            if (!wakeFired && t - wakeAt > 1.7f)
            {
                wakeFired = true; grace = 6f;
                var player = sp;
                state.ResetState(new string[0], () => Naninovel.ScriptTrackExtensions.LoadAndPlay(player.MainTrack, "Entry"));
            }
        }
        if (fadeIn < 1f) Box(new Rect(0, 0, w, h), new Color(0.02f, 0.018f, 0.016f, 1f - fadeIn));
        GUI.color = g0;
    }
}
