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
    bool builtNight; float builtU = -1f;
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
        if (sHead != null && Mathf.Approximately(builtU, u) && builtNight == MolakTheme.Night) return;
        builtU = u; builtNight = MolakTheme.Night;
        if (gowun == null) gowun = MolakGui.Gowun;
        System.Func<int, Color, TextAnchor, GUIStyle> mk = (size, col, an) =>
        {
            var s = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(size * u), alignment = an, richText = true, wordWrap = false };
            if (gowun != null) s.font = gowun;
            MolakGui.Ink(s, col); return s;
        };
        // 0.4.80 창의 결(MolakPaper)
        sHead = mk(40, MolakPaper.Ink, TextAnchor.MiddleCenter);
        sSection = mk(28, MolakPaper.Accent, TextAnchor.MiddleLeft);
        sLabel = mk(32, MolakPaper.Ink, TextAnchor.MiddleLeft);
        sValue = mk(28, MolakPaper.Sub, TextAnchor.MiddleRight);
        sBtn = mk(32, MolakPaper.Ink, TextAnchor.MiddleCenter);
        sHint = mk(24, MolakPaper.Sub, TextAnchor.MiddleCenter);
    }

    float wipeArm = -9f, wipeDone = -9f;
    /// <summary>회귀로 쌓인 전역(g_) 기억을 모두 처음으로. 판 진행 저장은 건드리지 않는다.</summary>
    static async void WipeMemory ()
    {
        try
        {
            foreach (var n in MolakVarList.Globals) if (!n.StartsWith("g_pname")) MolakStats.Set(n, 0f);
            foreach (var n in new[] { "g_pname", "g_pname_a", "g_pname_i", "g_pname_eun", "g_pname_eul" }) MolakStats.SetStr(n, "");   // 0.4.75 기록을 지우면 이름도 다시 적는다
            MolakStats.SetStr("g_lastend", "none"); MolakStats.SetStr("g_lastcause", "none"); MolakStats.SetStr("g_nxform", "none"); MolakStats.SetStr("g_scars", "");
            MolakGloss.Reset();
            MolakAutoLife.Reset();
            await Engine.GetServiceOrErr<IStateManager>().SaveGlobal();
        }
        catch (System.Exception e) { Debug.LogWarning("[MolakSettings] 기억 지우기 실패: " + e.Message); }
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
        MolakPaper.Backdrop(w, h);   // 0.4.80 종이로 통째로 덮는다

        if (credits) { DrawCredits(w, h, u); GUI.color = g0; return; }

        IAudioManager am = Engine.Initialized ? Engine.GetService<IAudioManager>() : null;
        ITextPrinterManager tp = Engine.Initialized ? Engine.GetService<ITextPrinterManager>() : null;

        float cx = MolakPaper.Margin(w, u), cw = w - cx * 2f;
        float rh = 104 * u, sh = 58 * u, gap = 10 * u;
        float y = MolakPaper.Top(h, u);
        MolakPaper.Header(ref y, cx, cw, "환경 설정", "바꾼 값은 바로 적용된다.", u);

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
        // 0.4.70 회귀 뒤 지난 생을 하루 한 줄로 따라 걷기
        if (Toggle(new Rect(cx, y, cw, rh), "회귀 뒤 지난 생 따라 걷기", MolakAutoLife.Enabled, u)) MolakAutoLife.Enabled = !MolakAutoLife.Enabled;
        y += rh + gap;

        // ── 화면
        Section(ref y, cx, cw, sh, "화면");
        // 0.4.82 그림 화면: 온 화면 삽화 위에 옻칠 글자리. 끄면 예전 종이 화면(그때만 먹색 화면을 따로 고른다)
        if (Toggle(new Rect(cx, y, cw, rh), "그림 화면 (온 화면 삽화)", MolakTheme.Cinema, u)) MolakTheme.Cinema = !MolakTheme.Cinema;
        y += rh + gap;
        if (!MolakTheme.Cinema)
        {
            if (Toggle(new Rect(cx, y, cw, rh), "먹색 화면 (밤에 읽기)", MolakTheme.NightRaw, u)) MolakTheme.Night = !MolakTheme.NightRaw;
            y += rh + gap;
        }
        y += 30 * u;

        // 닫기 — 창마다 하나뿐인 먹색 단추. 자리가 남으면 아래쪽에 붙인다.
        var br = MolakPaper.BottomButton(w, h, u);
        float tail = MolakTitle.Showing ? 160 * u : 90 * u;
        if (br.y - tail < y) br.y = y; else br.y -= tail;
        if (MolakPaper.InkButton(br, "닫는다", u)) { MolakSfx.Confirm(); Close(); }
        var cr = new Rect(cx + cw * 0.3f, br.yMax + 14 * u, cw * 0.4f, 64 * u);
        GUI.Label(cr, "<u>제작 정보</u>", sHint);
        if (GUI.Button(cr, GUIContent.none, GUIStyle.none)) { MolakSfx.Confirm(); credits = true; }
        // 0.4.66 모든 생의 기억 지우기(언메이커: 처음부터 다시 했는데 '지난 생엔…'이 나온다) — 첫 화면에서만, 두 번 눌러야
        if (MolakTitle.Showing)
        {
            bool armed = Time.unscaledTime - wipeArm < 4f;
            var wr = new Rect(cx + cw * 0.1f, cr.yMax + 10 * u, cw * 0.8f, 64 * u);
            string wl = wipeDone > 0f && Time.unscaledTime - wipeDone < 3f ? "모든 생의 기억을 지웠다. 이제 정말 처음부터다."
                      : armed ? "<color=#" + ColorUtility.ToHtmlStringRGB(MolakPaper.Accent) + ">한 번 더 누르면 모든 생의 기억(앎·말로·업보)이 사라진다</color>" : "<u>모든 생의 기억 지우기</u>";
            GUI.Label(wr, wl, sHint);
            if (GUI.Button(wr, GUIContent.none, GUIStyle.none))
            {
                if (armed) { wipeArm = -9f; WipeMemory(); wipeDone = Time.unscaledTime; MolakSfx.Death(); }
                else { wipeArm = Time.unscaledTime; MolakSfx.Confirm(); }
            }
        }

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
        float cx = MolakPaper.Margin(w, u), cw = w - cx * 2f;
        float y = MolakPaper.Top(h, u);
        MolakPaper.Header(ref y, cx, cw, "제작 정보", null, u);
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
        var br = MolakPaper.BottomButton(w, h, u);
        float viewH = br.y - 28 * u - y;
        credScroll = GUI.BeginScrollView(new Rect(cx, y, cw, viewH), credScroll, new Rect(0, 0, cw - 20 * u, th + 20 * u), GUIStyle.none, GUIStyle.none);
        GUI.Label(new Rect(0, 0, cw - 20 * u, th), txt, body);
        GUI.EndScrollView();
        if (MolakPaper.InkButton(br, "돌아간다", u)) { MolakSfx.Confirm(); credits = false; }
        GUI.Button(new Rect(0, 0, w, h), GUIContent.none, GUIStyle.none);
    }

    static string SpeedText (float v) { return v < 0.2f ? "느리게" : v < 0.45f ? "조금 느리게" : v < 0.7f ? "보통" : v < 0.95f ? "빠르게" : "한 번에"; }

    void Section (ref float y, float cx, float cw, float sh, string title)
    {
        MolakPaper.Section(ref y, cx, cw, title, builtU);
    }

    void Row (Rect r)
    {
        MolakPaper.CardFace(r, Color.clear, builtU);
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
        MolakPaper.Slider(new Rect(tx, r.y, tw, r.height), v, dragId == id ? u * 1.2f : u);
        string vt = valueText ?? (v <= 0.001f ? "끔" : Mathf.RoundToInt(v * 100f) + "%");
        GUI.Label(new Rect(r.xMax - 150 * u, r.y, 128 * u, r.height), vt, sValue);
        return v;
    }

    bool Toggle (Rect r, string label, bool on, float u)
    {
        Row(r);
        GUI.Label(new Rect(r.x + 28 * u, r.y, r.width * 0.7f, r.height), label, sLabel);
        // 오른쪽 스위치
        float sw = 104 * u, shh = 54 * u;
        MolakPaper.ToggleKnob(new Rect(r.xMax - sw - 28 * u, r.y + (r.height - shh) / 2f, sw, shh), on);
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
        var frame = new Rect(rDec.x, rDec.y, rInc.xMax - rDec.x, rDec.height);
        MolakPaper.Box(new Rect(frame.x, frame.y, frame.width, Mathf.Max(1f, 1.5f * u)), MolakPaper.Hair);
        MolakPaper.Box(new Rect(frame.x, frame.yMax - Mathf.Max(1f, 1.5f * u), frame.width, Mathf.Max(1f, 1.5f * u)), MolakPaper.Hair);
        MolakPaper.Box(new Rect(frame.x, frame.y, Mathf.Max(1f, 1.5f * u), frame.height), MolakPaper.Hair);
        MolakPaper.Box(new Rect(frame.xMax - Mathf.Max(1f, 1.5f * u), frame.y, Mathf.Max(1f, 1.5f * u), frame.height), MolakPaper.Hair);
        GUI.Label(rDec, "－", sBtn); GUI.Label(rInc, "＋", sBtn);
        var vS = new GUIStyle(sValue) { alignment = TextAnchor.MiddleCenter };
        MolakGui.Ink(vS, MolakPaper.Sub);
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
