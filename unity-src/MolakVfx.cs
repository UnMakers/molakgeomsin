using Naninovel;
using UnityEngine;

/// <summary>장면 틴트·화면 이펙트 — @scene / @vfx 명령 + C# 훅.</summary>
public class MolakVfx : MonoBehaviour
{
    static MolakVfx instance;
    // 플래시 상태
    float flashT = -1f; Color flashC; float flashDur = 0.5f;
    // 흔들림
    float shakeT = -1f; float shakeAmp;
    // 날씨(0.4.9): rain · bloodrain · snow · petals · embers — 한 번에 하나
    bool rain; float[] rx; float[] ry; float[] rs; float[] rr;
    public static string WeatherKind = "";
    static Texture2D dot;
    // 장면 틴트 목표
    static Color tintTarget = new Color(0.97f, 0.95f, 0.92f, 1f);
    Color tintCur = tintTarget;

    public static readonly System.Collections.Generic.Dictionary<string, Color> Scenes =
        new System.Collections.Generic.Dictionary<string, Color> {
        { "day",    new Color(0.97f, 0.95f, 0.92f, 1f) },
        { "night",  new Color(0.80f, 0.81f, 0.88f, 1f) },
        { "dusk",   new Color(0.93f, 0.82f, 0.68f, 1f) },
        { "shrine", new Color(0.90f, 0.87f, 0.80f, 1f) },
        { "valley", new Color(0.84f, 0.87f, 0.88f, 1f) },
        { "blood",  new Color(0.92f, 0.78f, 0.74f, 1f) },
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap ()
    {
        if (instance != null) return;
        var go = new GameObject("MolakVfx");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<MolakVfx>();
    }

    static Texture2D Px { get { return MolakGui.Px; } }
    void Box (Rect r, Color c) { var g = GUI.color; GUI.color = c; GUI.DrawTexture(r, Px); GUI.color = g; }

    public static void Scene (string id)
    {
        Color c;
        if (Scenes.TryGetValue(id, out c)) tintTarget = c;
    }
    // 0.4.66 깜빡임 줄이기(언메이커: "화면 깜빡이는 게 너무 과하다")
    //  · 세기 전체를 절반 아래로(FlashScale), 화면 전체 막은 더 옅게 · 가장자리 비네트 위주로
    //  · 켜질 때 0.12초에 걸쳐 스며들고(톡 터지지 않게) 천천히 걷힌다
    //  · 0.6초 안에 또 오면 새로 터뜨리지 않고 지금 빛에 합친다(연달아 번쩍이는 것 막기)
    public const float FlashScale = 0.42f, ShakeScale = 0.6f;
    public static void Flash (Color c, float dur)
    {
        if (instance == null) return;
        c.a *= FlashScale;
        float now = Time.unscaledTime;
        if (instance.flashT >= 0f && now - instance.flashStart < 0.6f)
        {   // 합치기: 더 센 쪽 색, 남은 길이는 늘리지 않는다
            if (c.a > instance.flashC.a * (1f - Mathf.Clamp01(instance.flashT / instance.flashDur))) instance.flashC = new Color(c.r, c.g, c.b, Mathf.Max(c.a, instance.flashC.a));
            return;
        }
        instance.flashC = c; instance.flashDur = Mathf.Max(0.35f, dur); instance.flashT = 0f; instance.flashStart = now;
    }
    float flashStart = -9f;
    public static void Blood () { Flash(new Color(0.62f, 0.08f, 0.05f, 0.5f), 0.7f); Shake(14f); }
    public static void Gold () { Flash(new Color(0.85f, 0.68f, 0.18f, 0.32f), 0.6f); }
    public static void CursePulse () { Flash(new Color(0.42f, 0.22f, 0.58f, 0.4f), 0.9f); MolakSfx.CurseBreath(); }
    public static void Shake (float amp) { amp *= ShakeScale; if (instance != null && (instance.shakeT < 0f || amp >= instance.shakeAmp)) { instance.shakeAmp = amp; instance.shakeT = 0f; } }
    /// <summary>칼이 부딪는 순간: 흰 섬광 한 번 + 짧은 흔들림 + 칼 소리</summary>
    public static void Clash (bool heavy) { Flash(new Color(1f, 0.97f, 0.9f, heavy ? 0.55f : 0.35f), heavy ? 0.35f : 0.22f); Shake(heavy ? 16f : 9f); MolakSfx.Clash(heavy); }

    // 마디 이름(@chapter): 띠 그림 위에 붓 글씨 제목, 2.6초
    float chapT = -1f; string chapText = "", chapSub = "";
    GUIStyle sChap, sChapSub; float chapU = -1f;
    public static void Chapter (string text, string sub) { if (instance == null || string.IsNullOrEmpty(text)) return; instance.chapText = text; instance.chapSub = sub ?? ""; instance.chapT = 0f; MolakSfx.Bell(); }

    // 인장 도장(승부수 판정): 화면 위쪽에 한자 하나가 찍히고 바랜다. 입력은 막지 않는다.
    float stampT = -1f; string stampHanja = "", stampLine = ""; bool stampBig;
    // 0.4.68 인장은 대사와 겹쳐 뜨지 않는다(언메이커): 글자리를 빈 종이로 덮고 그 가운데 크게 찍힌다. 누르면 걷히고, 그다음에 대사가 나온다.
    //  · 다음 글·선택지는 MolakPlayMode 의 실행 전 작업이 인장이 걷힐 때까지 붙잡는다(Holding).
    //  · 넘기기 중엔 0.5초, 자동이면 1.6초 뒤 저절로 걷힌다.
    bool stampHold; float stampAuto;
    public static bool Holding => instance != null && instance.stampT >= 0f && instance.stampHold;
    public static void Stamp (string hanja, string line, bool big)
    {
        if (instance == null) return;
        instance.stampHanja = hanja; instance.stampLine = line; instance.stampBig = big; instance.stampT = 0f; instance.stampOutAt = -1f; MolakSfx.Stamp();
        var sp = Engine.Initialized ? Engine.GetService<IScriptPlayer>() : null;
        bool skipping = sp != null && sp.Skipping;
        instance.stampHold = Engine.Initialized && !MolakTitle.Showing;
        instance.stampAuto = skipping ? 0.5f : MolakPlayMode.Auto ? 1.6f : -1f;
    }
    public static Rect TextArea ()
    {
        float w = Screen.width, h = Screen.height;
        float unit = Mathf.Min(w / 900f, h / 1600f);
        float top = Mathf.Max(0f, (h - 1600f * unit) * 0.5f);
        float bandBottom = top + (MolakArt.BandTop + MolakArt.BandH) * unit;
        float bottom = h - h * 0.12f - 56f * unit;   // 대화 틀 아랫선까지(행낭 띠를 덮지 않게)
        return new Rect(w * 0.04f, bandBottom + 6f * unit, w * 0.92f, Mathf.Max(200f * unit, bottom - bandBottom - 12f * unit));
    }
    GUIStyle sStamp, sStampLine, sStampHint; Font gowun; float stampU = -1f; float stampOutAt = -1f;
    static Texture2D vig;
    /// <summary>가장자리만 짙은 둥근 비네트(심장 박동·사술의 숨결에 쓴다)</summary>
    static Texture2D Vig
    {
        get
        {
            if (vig != null) return vig;
            int S = 128; vig = new Texture2D(S, S, TextureFormat.RGBA32, false); vig.filterMode = FilterMode.Bilinear; vig.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float dx = (x + 0.5f) / S - 0.5f, dy = (y + 0.5f) / S - 0.5f;
                float d = Mathf.Sqrt(dx*dx + dy*dy) * 2f;     // 0 가운데 · 1 가장자리 · 1.41 모서리
                vig.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((d - 0.55f) / 0.75f)));
            }
            vig.Apply(); return vig;
        }
    }
    /// <summary>심장 박동 곡선: 쿵-쿵 두 번 뛰고 쉰다(분당 약 일흔, 숨이 가쁠수록 빨라진다).</summary>
    static float Beat (float t, float bpm)
    {
        float ph = (t * bpm / 60f) % 1f;
        float a = Mathf.Exp(-Mathf.Pow((ph - 0.05f) / 0.05f, 2f)), b = Mathf.Exp(-Mathf.Pow((ph - 0.24f) / 0.06f, 2f)) * 0.6f;
        return Mathf.Clamp01(a + b);
    }
    public static void Rain (bool on) { Weather(on ? "rain" : ""); }
    /// <summary>날씨: "rain" 비, "bloodrain" 핏빛 비, "snow" 눈, "petals" 꽃잎, "embers" 불티, "" 갬</summary>
    public static void Weather (string kind)
    {
        if (instance == null) return;
        kind = kind ?? "";
        if (kind == WeatherKind) return;
        WeatherKind = kind;
        instance.SetRain(kind.Length > 0);
    }
    static Texture2D Dot
    {
        get
        {
            if (dot != null) return dot;
            int S = 32; dot = new Texture2D(S, S, TextureFormat.RGBA32, false); dot.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float dx = (x + 0.5f) / S - 0.5f, dy = (y + 0.5f) / S - 0.5f;
                float d = Mathf.Sqrt(dx*dx + dy*dy) * 2f;
                dot.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((1f - d) * 2.2f)));
            }
            dot.Apply(); return dot;
        }
    }

    void SetRain (bool on)
    {
        rain = on;
        if (on && rx == null)
        {
            int n = 120;
            rx = new float[n]; ry = new float[n]; rs = new float[n]; rr = new float[n];
            for (int i = 0; i < n; i++) { rx[i] = Random.value; ry[i] = Random.value; rs[i] = 0.7f + Random.value*0.8f; rr[i] = Random.value * 360f; }
        }
    }

    void Update ()
    {
        // 장면 틴트를 배경 스프라이트에 서서히 적용
        var tt = tintTarget; if (MolakTheme.Night) tt = new Color(tt.r*0.45f, tt.g*0.45f, tt.b*0.45f, 1f);
        tintCur = Color.Lerp(tintCur, tt, Time.deltaTime * 2.2f);
        var bd = GameObject.Find("MolakBackdrop");
        if (bd != null)
        {
            var sr = bd.GetComponent<SpriteRenderer>();
            if (sr != null && sr.color != tintCur) sr.color = tintCur;
        }
        // 흔들림(카메라)
        if (shakeT >= 0f)
        {
            shakeT += Time.deltaTime;
            var cam = Camera.main;
            if (cam != null)
            {
                float k = Mathf.Max(0f, 1f - shakeT / 0.45f);
                if (k <= 0f) { shakeT = -1f; }
                var off = Random.insideUnitCircle * (shakeAmp * 0.01f) * k;
                cam.transform.position = new Vector3(off.x, off.y, cam.transform.position.z);
            }
        }
    }

    void OnGUI ()
    {
        GUI.depth = MolakGui.DepthVfx;   // 0.4.27: 행낭 띠·HUD 위까지 물들게(설정 -200, 인장 -2500, 먹 화면 -3000 보다는 아래)
        float w = Screen.width, h = Screen.height;
        // 몸과 사술의 상태를 화면 가장자리에: 체력이 바닥이면 붉은 맥이 뛰고, 저주가 깊으면 보랏빛이 숨을 쉰다
        if (Event.current.type == EventType.Repaint && Engine.Initialized && !MolakMenu.IsOpen)
        {
            float hp = MolakStats.Get("hp"), hpm = Mathf.Max(1f, MolakStats.Get("hpmax")), cu = MolakStats.Get("curse");
            float r = hp / hpm;
            if (hp > 0f && r <= 0.3f)
            {
                float k = Mathf.InverseLerp(0.3f, 0.05f, r);                       // 낮을수록 짙고 빠르게
                float a = (0.08f + 0.14f * k) * (0.45f + 0.55f * Beat(Time.time, 64f + 40f * k));
                var g = GUI.color; GUI.color = new Color(0.55f, 0.04f, 0.03f, a); GUI.DrawTexture(new Rect(0, 0, w, h), Vig); GUI.color = g;
            }
            if (cu >= 75f)
            {
                float k = Mathf.InverseLerp(75f, 100f, cu);
                float a = (0.08f + 0.14f * k) * (0.5f + 0.5f * Mathf.Sin(Time.time * (0.9f + 0.8f * k)));
                var g = GUI.color; GUI.color = new Color(0.36f, 0.16f, 0.48f, Mathf.Max(0f, a)); GUI.DrawTexture(new Rect(0, 0, w, h), Vig); GUI.color = g;
            }
        }
        // 플래시(비네트 느낌: 중앙은 옅고 가장자리 진하게 — 상/하/좌/우 밴드)
        if (flashT >= 0f)
        {
            if (Event.current.type == EventType.Repaint) flashT += Time.unscaledDeltaTime;   // OnGUI 는 한 프레임에 여러 번 불린다
            float k = 1f - Mathf.Clamp01(flashT / flashDur);
            k = k * k * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(flashT / 0.12f));   // 스며들었다가 천천히 걷힌다
            if (flashT >= flashDur) flashT = -1f;
            else
            {
                // 0.4.27: 네모 띠 대신 화면 전체를 고르게 물들이고, 가장자리는 둥근 비네트로 조금 더 짙게(본문 창 자리만 빠져 보이던 문제)
                var g = GUI.color;
                GUI.color = new Color(flashC.r, flashC.g, flashC.b, flashC.a * k * 0.35f); GUI.DrawTexture(new Rect(0, 0, w, h), Px);
                GUI.color = new Color(flashC.r, flashC.g, flashC.b, Mathf.Min(1f, flashC.a * k * 1.1f)); GUI.DrawTexture(new Rect(0, 0, w, h), Vig);
                GUI.color = g;
            }
        }
        // 마디 이름
        if (chapT >= 0f)
        {
            chapT += Time.deltaTime;
            const float CD = 2.6f;
            if (chapT > CD) chapT = -1f;
            else
            {
                float u = MolakTheme.U;
                if (sChap == null || !Mathf.Approximately(chapU, u))
                {
                    chapU = u; if (gowun == null) gowun = MolakGui.Gowun;
                    sChap = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(64 * u), alignment = TextAnchor.MiddleCenter };
                    sChapSub = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(26 * u), alignment = TextAnchor.MiddleCenter };
                    sChap.font = MolakTypography.Heading(gowun); sChapSub.font = MolakTypography.Heading(gowun);
                }
                float a = chapT < 0.35f ? chapT / 0.35f : (chapT > CD - 0.7f ? (CD - chapT) / 0.7f : 1f);
                float cy = h * 0.165f;
                // 붓 자국 띠(가운데 짙고 양끝 옅게)
                var g0 = GUI.color;
                for (int i = 0; i < 24; i++)
                {
                    float t = i / 23f, k = 1f - Mathf.Abs(t - 0.5f) * 2f;
                    GUI.color = new Color(0.08f, 0.06f, 0.05f, 0.62f * a * Mathf.Pow(k, 0.6f));
                    GUI.DrawTexture(new Rect(w * (0.08f + 0.84f * t), cy - 58 * u, w * 0.84f / 23f + 1f, 116 * u), Px);
                }
                GUI.color = g0;
                float slide = (1f - Mathf.Clamp01(chapT / 0.5f)) * 24f * u;
                MolakGui.Ink(sChapSub, new Color(MolakTheme.Gold.r, MolakTheme.Gold.g, MolakTheme.Gold.b, 0.95f * a));
                if (chapSub.Length > 0) GUI.Label(new Rect(0, cy - 54 * u, w, 34 * u), chapSub, sChapSub);
                MolakGui.Ink(sChap, new Color(0.96f, 0.92f, 0.85f, a));
                GUI.Label(new Rect(slide, cy - 28 * u, w, 80 * u), chapText, sChap);
            }
        }
        // 승부수 인장
        if (stampT >= 0f)
        {
            if (Event.current.type == EventType.Repaint) stampT += Time.unscaledDeltaTime;
            const float Out = 0.35f;
            if (stampHold)
            {
                MolakInputGuard.Guard(new Rect(0, 0, w, h));
                bool tap = Event.current.type == EventType.MouseDown && stampT > 0.25f;
                bool auto = stampAuto > 0f && stampT > stampAuto;
                if (tap || auto) { if (tap) Event.current.Use(); stampHold = false; stampOutAt = stampT; MolakInputGuard.MuteFor(0.2f); }
            }
            else if (stampOutAt < 0f && stampT > 1.1f) stampOutAt = stampT;   // 붙잡지 않는 인장(제목 화면 등)은 1.1초 뒤 걷힌다
            if (!stampHold && stampOutAt >= 0f && stampT - stampOutAt > Out) { stampT = -1f; stampOutAt = -1f; }
            else
            {
                float u = MolakTheme.U;
                if (sStamp == null || !Mathf.Approximately(stampU, u))
                {
                    stampU = u; if (gowun == null) gowun = MolakGui.Gowun;
                    sStamp = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(150 * u), alignment = TextAnchor.MiddleCenter };
                    if (MolakGui.Hanja != null) sStamp.font = MolakGui.Hanja;
                    sStampLine = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(32 * u), alignment = TextAnchor.MiddleCenter, wordWrap = true };
                    sStampHint = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(22 * u), alignment = TextAnchor.MiddleCenter };
                    if (gowun != null) { sStampLine.font = gowun; sStampHint.font = gowun; }
                }
                float a = stampT < 0.12f ? stampT / 0.12f : 1f;
                if (!stampHold && stampOutAt >= 0f) a = Mathf.Clamp01(1f - (stampT - stampOutAt) / Out);
                bool ok = stampHanja == "通" || stampHanja == "破" || stampHanja == "勢" || stampHanja == "先" || stampHanja == "境";
                var col = ok ? new Color(0.62f, 0.45f, 0.1f, 1f) : new Color(0.62f, 0.12f, 0.08f, 1f);
                var ta = TextArea();
                // 글자리를 빈 종이로: 아래 글이 비치지 않게
                var bg = GUI.color; GUI.color = new Color(0.955f, 0.94f, 0.905f, 0.985f * a); GUI.DrawTexture(ta, Px);
                GUI.color = new Color(0.55f, 0.42f, 0.25f, 0.35f * a);
                GUI.DrawTexture(new Rect(ta.x + ta.width * 0.2f, ta.y + ta.height * 0.5f + 118f * u, ta.width * 0.6f, Mathf.Max(1f, 1.5f * u)), Px);
                GUI.color = bg;
                var piv = new Vector2(ta.center.x, ta.y + ta.height * 0.5f - 30f * u);
                float sc = 1f + Mathf.Clamp01(1f - stampT / 0.18f) * (stampBig ? 0.5f : 0.25f);   // 찍히는 순간 크게 → 제자리
                // 인장 바탕: 붉은 먹 네모(금빛 인장은 옅은 금빛 테두리)
                var box = new Rect(piv.x - 105f * u, piv.y - 105f * u, 210f * u, 210f * u);
                var m = GUI.matrix; GUIUtility.ScaleAroundPivot(new Vector2(sc, sc), piv);
                GUI.color = new Color(col.r, col.g, col.b, 0.92f * a); GUI.DrawTexture(box, Px);
                GUI.color = new Color(1f, 1f, 1f, 0.12f * a); GUI.DrawTexture(new Rect(box.x + 8f * u, box.y + 8f * u, box.width - 16f * u, box.height - 16f * u), Px);
                GUI.color = bg;
                MolakGui.Ink(sStamp, new Color(0.98f, 0.95f, 0.88f, a));
                GUI.Label(box, stampHanja, sStamp);
                GUI.matrix = m;
                MolakGui.Ink(sStampLine, new Color(0.16f, 0.13f, 0.1f, a));
                GUI.Label(new Rect(ta.x + 20f * u, piv.y + 120f * u, ta.width - 40f * u, 90f * u), stampLine, sStampLine);
                if (stampHold && stampAuto < 0f && stampT > 0.6f)
                {
                    MolakGui.Ink(sStampHint, new Color(0.45f, 0.4f, 0.34f, Mathf.Clamp01((stampT - 0.6f) / 0.4f) * a));
                    GUI.Label(new Rect(ta.x, ta.yMax - 46f * u, ta.width, 34f * u), "눌러서 넘긴다", sStampHint);
                }
            }
        }
        // 날씨
        if (rain && rx != null && Event.current.type == EventType.Repaint)
        {
            float u = MolakGui.U, dt = Time.deltaTime, t = Time.time;
            string k = WeatherKind;
            int n = k == "snow" ? 80 : k == "petals" ? 34 : k == "embers" ? 40 : k == "inferno" ? 110 : k == "bloodrain" ? 90 : 70;
            if (k == "inferno")
            {   // 0.4.22 불타는 산장: 아래에서 치솟는 열기(흔들리는 붉은 빛) + 화면 전체가 불빛에 일렁인다
                float fl0 = 0.88f + 0.12f * Mathf.Sin(t * 1.9f) * Mathf.Sin(t * 1.1f + 1f);   // 0.4.66 일렁임을 느리고 얕게(깜빡여 보이지 않게)
                Box(new Rect(0, 0, w, h), new Color(0.55f, 0.12f, 0.02f, 0.07f * fl0));
                for (int b = 0; b < 8; b++)
                {
                    float bh = h * (0.06f + 0.05f * b);
                    Box(new Rect(0, h - bh, w, bh), new Color(0.90f, 0.30f, 0.06f, 0.045f * fl0));
                }
            }
            for (int i = 0; i < n && i < rx.Length; i++)
            {
                if (k == "rain" || k == "bloodrain")
                {
                    float sp = k == "bloodrain" ? 1.25f : 1.1f;
                    ry[i] += dt * sp * rs[i]; rx[i] -= dt * 0.12f * rs[i];
                    if (ry[i] > 1f) { ry[i] -= 1.1f; rx[i] = Random.value * 1.2f; }
                    var rc = k == "bloodrain" ? new Color(0.55f, 0.06f, 0.05f, 0.42f) : new Color(0.35f, 0.38f, 0.45f, 0.35f);
                    Box(new Rect(rx[i] * w, ry[i] * h, 2f * u * (k == "bloodrain" ? 1.3f : 1f), 34f * rs[i] * u), rc);
                }
                else if (k == "snow")
                {
                    ry[i] += dt * 0.07f * rs[i]; rx[i] += Mathf.Sin(t * 0.8f + i) * dt * 0.02f;
                    if (ry[i] > 1.02f) { ry[i] = -0.03f; rx[i] = Random.value; }
                    float sz = (7f + 7f * rs[i]) * u;
                    var g = GUI.color; GUI.color = new Color(1f, 1f, 1f, 0.85f);
                    GUI.DrawTexture(new Rect(rx[i] * w, ry[i] * h, sz, sz), Dot); GUI.color = g;
                }
                else if (k == "petals")
                {
                    ry[i] += dt * 0.06f * rs[i]; rx[i] += (Mathf.Sin(t * 1.1f + i * 1.7f) * 0.03f - 0.012f) * dt;
                    rr[i] += dt * 70f * rs[i];
                    if (ry[i] > 1.03f) { ry[i] = -0.04f; rx[i] = Random.value * 1.1f; }
                    float sw = 18f * u * rs[i], sh = 10f * u * rs[i];
                    var piv = new Vector2(rx[i] * w, ry[i] * h);
                    var m = GUI.matrix; GUIUtility.RotateAroundPivot(rr[i], piv);
                    var g = GUI.color; GUI.color = new Color(0.94f, 0.62f, 0.70f, 0.85f);
                    GUI.DrawTexture(new Rect(piv.x - sw/2f, piv.y - sh/2f, sw, sh), Dot); GUI.color = g; GUI.matrix = m;
                }
                else if (k == "inferno")
                {   // 불티의 두 배: 더 많이, 더 크게, 더 빨리. 셋에 하나는 길게 늘어진 불꽃 혀.
                    ry[i] -= dt * 0.17f * rs[i]; rx[i] += Mathf.Sin(t * 2.6f + i) * dt * 0.05f;
                    if (ry[i] < -0.04f) { ry[i] = 1.03f; rx[i] = Random.value; }
                    float fl = 0.55f + 0.45f * Mathf.Sin(t * 11f + i * 3f);
                    float hot = (i % 4 == 0) ? 1f : 0f;
                    var col = Color.Lerp(new Color(0.95f, 0.38f, 0.08f, 1f), new Color(1f, 0.80f, 0.35f, 1f), hot);
                    var g = GUI.color; GUI.color = new Color(col.r, col.g, col.b, 0.9f * fl * Mathf.Clamp01(ry[i] * 1.4f));
                    if (i % 3 == 0) { float fw = (6f + 6f * rs[i]) * u; GUI.DrawTexture(new Rect(rx[i] * w, ry[i] * h, fw, fw * 3.2f), Dot); }
                    else { float sz = (7f + 10f * rs[i]) * u; GUI.DrawTexture(new Rect(rx[i] * w, ry[i] * h, sz, sz), Dot); }
                    GUI.color = g;
                }
                else if (k == "embers")
                {
                    ry[i] -= dt * 0.09f * rs[i]; rx[i] += Mathf.Sin(t * 2f + i) * dt * 0.03f;
                    if (ry[i] < -0.03f) { ry[i] = 1.02f; rx[i] = Random.value; }
                    float sz = (4f + 5f * rs[i]) * u, fl = 0.55f + 0.45f * Mathf.Sin(t * 9f + i * 3f);
                    var g = GUI.color; GUI.color = new Color(0.95f, 0.45f, 0.12f, 0.75f * fl);
                    GUI.DrawTexture(new Rect(rx[i] * w, ry[i] * h, sz, sz), Dot); GUI.color = g;
                }
            }
        }
    }
}
