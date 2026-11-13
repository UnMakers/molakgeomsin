using Naninovel;
using UnityEngine;

/// <summary>
/// 환경 설정 — 나니노벨 기본 회색 설정 창 대신 쓰는 먹색 한 장짜리 설정판.
/// 타이틀 '환경 설정'과 ☰ 메뉴 '환경 설정'이 모두 이걸 연다. 폰 손가락 기준(줄 높이 112u, 슬라이더는 줄 전체가 잡힌다).
/// 소리: 배경음 · 효과음 · 진동 / 글: 글자 크기 · 글 나오는 속도 · 자동 진행 대기 / 화면: 먹색.
/// 값은 나니노벨 설정(BGM·SFX 음량, 글 속도, 자동 대기)과 PlayerPrefs(글자 크기·먹색·진동)에 그대로 저장한다.
/// </summary>
public class MolakSettings : MonoBehaviour
{
    static MolakSettings inst;
    bool open;
    float openT;
    int dragId = -1;
    Font gowun;
    GUIStyle sHead, sLabel, sValue, sSection, sBtn, sHint;
    float builtU = -1f;
    static Texture2D disk;

    public static bool IsOpen { get { return inst != null && inst.open; } }

    public static void Open ()
    {
        if (inst == null) Boot();
        inst.open = true; inst.openT = 0f; inst.dragId = -1; inst.credits = false;
        SetInput(false);
    }

    public static void Close ()
    {
        if (inst == null || !inst.open) return;
        inst.open = false; inst.dragId = -1;
        try { var st = Engine.GetService<IStateManager>(); if (st != null) st.SaveSettings(); } catch { }
        PlayerPrefs.Save();
        SetInput(true);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot ()
    {
        if (inst) return;
        var go = new GameObject("MolakSettings"); DontDestroyOnLoad(go);
        inst = go.AddComponent<MolakSettings>();
    }

    static void SetInput (bool v)
    {
        try
        {
            if (!Engine.Initialized) return;
            var im = Engine.GetService<IInputManager>();
            if (im == null) return;
            var p = im.GetType().GetProperty("ProcessInput");
            if (p != null && p.CanWrite) p.SetValue(im, v);
        }
        catch { }
    }

    static Texture2D Px { get { return MolakGui.Px; } }
    static Texture2D Disk
    {
        get
        {
            if (disk != null) return disk;
            int n = 64; disk = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                disk.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((1f - d) * n / 2.5f)));
            }
            disk.Apply(); return disk;
        }
    }
    static void Box (Rect r, Color c) { var g = GUI.color; GUI.color = c * g; GUI.DrawTexture(r, Px); GUI.color = g; }

    void Styles (float u)
    {
        if (sHead != null && Mathf.Approximately(builtU, u)) return;
        builtU = u;
        if (gowun == null) gowun = MolakGui.Gowun;
        System.Func<int, Color, TextAnchor, GUIStyle> mk = (size, col, an) =>
        {
            var s = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(size * u), alignment = an, richText = true, wordWrap = false };
            if (gowun != null) s.font = gowun;
            MolakGui.Ink(s, col); return s;
        };
        sHead = mk(40, MolakTheme.Ink, TextAnchor.MiddleCenter);
        sSection = mk(28, MolakTheme.Gold, TextAnchor.MiddleLeft);
        sHead.font = MolakTypography.Heading(sHead.font); sSection.font = MolakTypography.Heading(sSection.font);
        sLabel = mk(32, MolakTheme.Ink, TextAnchor.MiddleLeft);
        sValue = mk(28, MolakTheme.Ink3, TextAnchor.MiddleRight);
        sBtn = mk(32, MolakTheme.Ink, TextAnchor.MiddleCenter);
        sHint = mk(22, MolakTheme.Ink3, TextAnchor.MiddleCenter);
    }

    void OnGUI ()
    {
        if (!open) return;
        GUI.depth = MolakGui.DepthSettings;   // 타이틀·메뉴·인게임 위
        float u = MolakTheme.U; Styles(u);
        float w = Screen.width, h = Screen.height;
        openT += Time.unscaledDeltaTime;
        float a = Mathf.Clamp01(openT / 0.18f);
        var g0 = GUI.color; GUI.color = new Color(1, 1, 1, a);

        MolakInputGuard.Guard(new Rect(0, 0, w, h));
        Box(new Rect(0, 0, w, h), new Color(0.04f, 0.035f, 0.03f, 0.96f));

        if (credits) { DrawCredits(w, h, u); GUI.color = g0; return; }

        IAudioManager am = Engine.Initialized ? Engine.GetService<IAudioManager>() : null;
        ITextPrinterManager tp = Engine.Initialized ? Engine.GetService<ITextPrinterManager>() : null;

        float cw = Mathf.Min(w * 0.88f, 900 * u), cx = (w - cw) / 2f;
        float rh = 112 * u, sh = 64 * u, gap = 10 * u;
        float total = 120 * u + 3 * sh + 7 * (rh + gap) + 60 * u + 120 * u;
        float y = Mathf.Max(70 * u, (h - total) / 2f);

        GUI.Label(new Rect(cx, y, cw, 90 * u), "환경 설정", sHead);
        Box(new Rect(cx + cw * 0.35f, y + 88 * u, cw * 0.30f, Mathf.Max(1f, 2 * u)), MolakTheme.Gold * new Color(1, 1, 1, 0.6f));
        y += 120 * u;

        // ── 소리
        Section(ref y, cx, cw, sh, "소리");
        float bgm = MolakBgm.Volume;
        float nb = Slider(1, new Rect(cx, y, cw, rh), "배경음", bgm, u);
        if (Mathf.Abs(nb - bgm) > 0.001f) MolakBgm.Volume = nb;
        y += rh + gap;
        float sfx = am != null ? am.SfxVolume : 1f;
        float ns = Slider(2, new Rect(cx, y, cw, rh), "효과음", sfx, u);
        if (am != null && Mathf.Abs(ns - sfx) > 0.001f) { am.SfxVolume = ns; if (ns > 0f && MolakSfx.Muted) MolakSfx.Muted = false; }
        if (dragId == 2 && Event.current.type == EventType.MouseUp) MolakSfx.Tap();
        y += rh + gap;
        if (Toggle(new Rect(cx, y, cw, rh), "진동", MolakSfx.Haptic, u)) { MolakSfx.Haptic = !MolakSfx.Haptic; if (MolakSfx.Haptic) MolakSfx.Vibrate(); }
        y += rh + gap;

        // ── 글
        Section(ref y, cx, cw, sh, "글");
        Stepper(new Rect(cx, y, cw, rh), "글자 크기", Mathf.RoundToInt(MolakTheme.FontScale * 100f) + "%", u,
            () => { if (MolakTheme.FontStep > MolakTheme.FontMin) MolakTheme.FontStep--; },
            () => { if (MolakTheme.FontStep < MolakTheme.FontMax) MolakTheme.FontStep++; });
        y += rh + gap;
        if (tp != null)
        {
            float rs = tp.BaseRevealSpeed;
            float nr = Slider(3, new Rect(cx, y, cw, rh), "글 나오는 속도", rs, u, SpeedText(rs));
            if (Mathf.Abs(nr - rs) > 0.001f) tp.BaseRevealSpeed = nr;
            y += rh + gap;
            float ad = tp.BaseAutoDelay;
            float nd = Slider(4, new Rect(cx, y, cw, rh), "자동 진행 대기", ad, u, ad < 0.2f ? "짧게" : ad < 0.65f ? "보통" : "길게");
            if (Mathf.Abs(nd - ad) > 0.001f) tp.BaseAutoDelay = nd;
            y += rh + gap;
        }

        // ── 화면
        Section(ref y, cx, cw, sh, "화면");
        if (Toggle(new Rect(cx, y, cw, rh), "먹색 화면 (밤에 읽기)", MolakTheme.Night, u)) MolakTheme.Night = !MolakTheme.Night;
        y += rh + gap + 30 * u;

        // 닫기
        var br = new Rect(cx + cw * 0.2f, y, cw * 0.6f, 100 * u);
        bool over = br.Contains(Event.current.mousePosition);
        Box(br, over ? new Color(0.30f, 0.11f, 0.09f, 1f) : new Color(0.45f, 0.13f, 0.10f, 1f));
        GUI.Label(br, "닫기", sBtn);
        if (GUI.Button(br, GUIContent.none, GUIStyle.none)) { MolakSfx.Confirm(); Close(); }
        GUI.Label(new Rect(cx, br.yMax + 8 * u, cw, 40 * u), "바뀐 값은 바로 저장된다.", sHint);
        var cr = new Rect(cx + cw * 0.3f, br.yMax + 56 * u, cw * 0.4f, 64 * u);
        GUI.Label(cr, "<u>제작 정보</u>", sHint);
        if (GUI.Button(cr, GUIContent.none, GUIStyle.none)) { MolakSfx.Confirm(); credits = true; }

        // 빈 자리를 누른 손가락이 뒤의 타이틀·메뉴로 새지 않게
        GUI.Button(new Rect(0, 0, w, h), GUIContent.none, GUIStyle.none);
        if (Event.current.type == EventType.MouseUp || Event.current.rawType == EventType.MouseUp) dragId = -1;
        GUI.color = g0;
    }

    bool credits;
    Vector2 credScroll;

    /// <summary>제작 정보 — 만든 이, 엔진, 그림·소리의 출처(생성형 AI 사용 고지), 플레이 기록 파일 위치.</summary>
    void DrawCredits (float w, float h, float u)
    {
        float cw = Mathf.Min(w * 0.86f, 860 * u), cx = (w - cw) / 2f;
        float y = 90 * u;
        GUI.Label(new Rect(cx, y, cw, 90 * u), "제작 정보", sHead);
        y += 120 * u;
        var body = new GUIStyle(sLabel) { wordWrap = true, alignment = TextAnchor.UpperLeft, fontSize = Mathf.RoundToInt(27 * u) };
        string txt =
            "<b>몰락검신(沒落劍神)</b>  v" + Application.version + "\n" +
            "만든 이 · 언메이커\n\n" +
            "<color=#C9A44C>엔진</color>\nUnity · Naninovel\n\n" +
            "<color=#C9A44C>그림</color>\n삽화·인물·타이틀 그림은 생성형 AI(animagine-xl 4.0, ComfyUI)로 만든 뒤 손질했다.\n\n" +
            "<color=#C9A44C>소리</color>\n효과음은 코드로 합성했다. 배경음은 시험용이며 정식 출시 전 교체한다.\n\n" +
            "<color=#C9A44C>글꼴</color>\n고운바탕(Google Fonts, SIL Open Font License)\n\n" +
            "<color=#C9A44C>플레이 기록</color>\n생마다 한 줄씩 이 기기에만 남는다. 다른 곳으로 보내지 않는다.\n<size=" + Mathf.RoundToInt(20 * u) + ">" + MolakAutoSave.LogPath + "</size>";
        float th = body.CalcHeight(new GUIContent(txt), cw);
        float viewH = h - y - 200 * u;
        credScroll = GUI.BeginScrollView(new Rect(cx, y, cw, viewH), credScroll, new Rect(0, 0, cw - 20 * u, th + 20 * u), GUIStyle.none, GUIStyle.none);
        GUI.Label(new Rect(0, 0, cw - 20 * u, th), txt, body);
        GUI.EndScrollView();
        var br = new Rect(cx + cw * 0.2f, h - 170 * u, cw * 0.6f, 100 * u);
        bool over = br.Contains(Event.current.mousePosition);
        Box(br, over ? new Color(0.22f, 0.19f, 0.16f, 1f) : new Color(0.17f, 0.14f, 0.12f, 1f));
        GUI.Label(br, "돌아간다", sBtn);
        if (GUI.Button(br, GUIContent.none, GUIStyle.none)) { MolakSfx.Confirm(); credits = false; }
        GUI.Button(new Rect(0, 0, w, h), GUIContent.none, GUIStyle.none);
    }

    static string SpeedText (float v) { return v < 0.2f ? "느리게" : v < 0.45f ? "조금 느리게" : v < 0.7f ? "보통" : v < 0.95f ? "빠르게" : "한 번에"; }

    void Section (ref float y, float cx, float cw, float sh, string title)
    {
        GUI.Label(new Rect(cx + 6 * builtU, y, cw, sh), title, sSection);
        Box(new Rect(cx + 90 * builtU, y + sh * 0.5f, cw - 90 * builtU, Mathf.Max(1f, 1 * builtU)), MolakTheme.Line);
        y += sh;
    }

    void Row (Rect r)
    {
        Box(r, MolakTheme.Raise);
        Box(new Rect(r.x, r.yMax - Mathf.Max(1f, builtU), r.width, Mathf.Max(1f, builtU)), MolakTheme.Line);
    }

    /// <summary>줄 전체가 손잡이: 왼쪽 라벨, 오른쪽 막대. 막대 위든 줄 어디든 누르고 끌면 값이 바뀐다.</summary>
    float Slider (int id, Rect r, string label, float v, float u, string valueText = null)
    {
        Row(r);
        float lw = r.width * 0.36f;
        GUI.Label(new Rect(r.x + 28 * u, r.y, lw, r.height), label, sLabel);
        float tx = r.x + lw + 20 * u, tw = r.width - lw - 20 * u - 150 * u, ty = r.y + r.height * 0.5f;
        var e = Event.current;
        var hit = new Rect(tx - 30 * u, r.y, tw + 60 * u, r.height);
        if (e.type == EventType.MouseDown && hit.Contains(e.mousePosition)) dragId = id;
        if (dragId == id && (e.type == EventType.MouseDrag || e.type == EventType.MouseDown || e.type == EventType.MouseUp))
        {
            v = Mathf.Clamp01((e.mousePosition.x - tx) / tw);
            v = Mathf.Round(v * 20f) / 20f;
            if (e.type != EventType.MouseUp) e.Use();
        }
        float th = 6 * u;
        Box(new Rect(tx, ty - th / 2f, tw, th), new Color(0.33f, 0.28f, 0.24f, 1f));
        Box(new Rect(tx, ty - th / 2f, tw * v, th), MolakTheme.Gold);
        float k = dragId == id ? 44 * u : 36 * u;
        var gc = GUI.color; GUI.color = gc * MolakTheme.Ink;
        GUI.DrawTexture(new Rect(tx + tw * v - k / 2f, ty - k / 2f, k, k), Disk);
        GUI.color = gc;
        string vt = valueText ?? (v <= 0.001f ? "끔" : Mathf.RoundToInt(v * 100f) + "%");
        GUI.Label(new Rect(r.xMax - 150 * u, r.y, 128 * u, r.height), vt, sValue);
        return v;
    }

    bool Toggle (Rect r, string label, bool on, float u)
    {
        Row(r);
        GUI.Label(new Rect(r.x + 28 * u, r.y, r.width * 0.7f, r.height), label, sLabel);
        // 오른쪽 스위치
        float sw = 110 * u, shh = 54 * u;
        var srect = new Rect(r.xMax - sw - 28 * u, r.y + (r.height - shh) / 2f, sw, shh);
        Box(srect, on ? new Color(0.45f, 0.36f, 0.12f, 1f) : new Color(0.20f, 0.17f, 0.15f, 1f));
        float k = shh - 10 * u;
        var gc = GUI.color; GUI.color = gc * (on ? MolakTheme.Gold : MolakTheme.Ink3);
        GUI.DrawTexture(new Rect(on ? srect.xMax - k - 5 * u : srect.x + 5 * u, srect.y + 5 * u, k, k), Disk);
        GUI.color = gc;
        bool hitb = GUI.Button(r, GUIContent.none, GUIStyle.none);
        if (hitb) MolakSfx.Confirm();
        return hitb;
    }

    void Stepper (Rect r, string label, string value, float u, System.Action dec, System.Action inc)
    {
        Row(r);
        GUI.Label(new Rect(r.x + 28 * u, r.y, r.width * 0.4f, r.height), label, sLabel);
        float bw = 110 * u;
        var rDec = new Rect(r.xMax - 2 * bw - 140 * u - 28 * u, r.y + 8 * u, bw, r.height - 16 * u);
        var rInc = new Rect(r.xMax - bw - 28 * u, r.y + 8 * u, bw, r.height - 16 * u);
        Box(rDec, new Color(0.17f, 0.14f, 0.12f, 1f)); Box(rInc, new Color(0.17f, 0.14f, 0.12f, 1f));
        GUI.Label(rDec, "－", sBtn); GUI.Label(rInc, "＋", sBtn);
        var vS = new GUIStyle(sValue) { alignment = TextAnchor.MiddleCenter };
        MolakGui.Ink(vS, MolakTheme.Ink);
        GUI.Label(new Rect(rDec.xMax, r.y, 140 * u, r.height), value, vS);
        if (GUI.Button(rDec, GUIContent.none, GUIStyle.none)) { MolakSfx.Confirm(); dec(); }
        if (GUI.Button(rInc, GUIContent.none, GUIStyle.none)) { MolakSfx.Confirm(); inc(); }
    }

    void Update ()
    {
        // 안드로이드 뒤로 가기 / PC Esc → 닫기
        if (!open) return;
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame) Close();
#else
        if (Input.GetKeyDown(KeyCode.Escape)) Close();
#endif
    }
}
