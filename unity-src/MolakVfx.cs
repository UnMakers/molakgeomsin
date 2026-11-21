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
    float chapT = -1f; string chapText = "", chapSub = "", chapDesc = "";
    // 0.4.71 마디 이름이 너무 빨리 지나간다(언메이커) — 누를 때까지 떠 있고, 누르면 걷혀서 그림 띠 왼쪽 위에 작게 남는다(날이 바뀌면 지운다)
    bool chapHold; float chapOutT = -1f; bool chapWasDown; string pinText = ""; float pinDay;
    GUIStyle sPin; float pinU;
    GUIStyle sChap, sChapSub; float chapU = -1f;
    /// <summary>0.4.82 마디 이름이 글자리를 덮는 정도(1 = 글자리 숨김). 떠 있는 동안 1, 누르면 0.6초에 걸쳐 0 으로.</summary>
    /// <summary>0.4.83 마디 이름이 떠 있고 아직 누르지 않았다(@chapter 가 이게 풀릴 때까지 다음 글을 붙잡는다).</summary>
    public static bool ChapterHolding { get { return instance != null && instance.chapT >= 0f && instance.chapHold; } }
    public static float ChapterCover
    {
        get
        {
            if (instance == null || instance.chapT < 0f) return 0f;
            if (instance.chapHold) return 1f;
            return 1f - Mathf.Clamp01(instance.chapOutT / 0.6f);
        }
    }
    public static void Chapter (string text, string sub, string desc = "") { if (instance == null || string.IsNullOrEmpty(text)) return; instance.chapText = text; instance.chapSub = sub ?? ""; instance.chapDesc = desc ?? ""; instance.chapT = 0f; instance.chapHold = true; instance.chapOutT = -1f; instance.pinText = ""; instance.pinDay = MolakStats.Get("day"); MolakSfx.Chapter(); MolakArt.Key(sub); }

    // 인장 도장(승부수 판정): 화면 위쪽에 한자 하나가 찍히고 바랜다. 입력은 막지 않는다.
    float stampT = -1f; string stampHanja = "", stampLine = ""; bool stampBig;
    // 0.4.68 인장은 대사와 겹쳐 뜨지 않는다(언메이커): 글자리를 빈 종이로 덮고 그 가운데 크게 찍힌다. 누르면 걷히고, 그다음에 대사가 나온다.
    //  · 다음 글·선택지는 MolakPlayMode 의 실행 전 작업이 인장이 걷힐 때까지 붙잡는다(Holding).
    //  · 넘기기 중엔 0.5초, 자동이면 1.6초 뒤 저절로 걷힌다.
    bool stampHold; float stampAuto;
    public static bool Holding => instance != null && instance.stampT >= 0f && instance.stampHold;
    /// <summary>인장을 누른 셈 치고 걷는다(점검 도구·처음으로).</summary>
    public static void ReleaseStamp () { if (instance == null || instance.stampT < 0f) return; instance.stampHold = false; instance.stampOutAt = instance.stampT; }
    public static void Stamp (string hanja, string line, bool big)
    {
        if (instance == null) return;
        instance.stampHanja = hanja; instance.stampLine = line; instance.stampBig = big; instance.stampT = 0f; instance.stampOutAt = -1f; MolakSfx.Seal(hanja, big);
        var sp = Engine.Initialized ? Engine.GetService<IScriptPlayer>() : null;
        bool skipping = sp != null && sp.Skipping;
        instance.stampHold = Engine.Initialized && !MolakTitle.Showing;
        instance.stampAuto = skipping ? 0.5f : MolakPlayMode.Auto ? 1.6f : -1f;
    }
    /// <summary>0.4.82 수치 쪽지·낱말 풀이 쪽지를 놓는 칸. 그림 화면이면 글자리 바로 위(이름 인장 위)에서 위로 쌓인다 — 글자리를 가리지 않게.
    /// 아랫변(yMax)이 쪽지의 아랫변이다. 그 밖에는 TextArea 와 같다.</summary>
    public static Rect SlipArea ()
    {
        if (MolakTheme.TheaterNow && MolakScroll.TheaterTextGui.width > 1f)
        {   // 극장판: 그림 창 아래 금줄 바로 위에서 위로(글 칸을 가리지 않게)
            float u0 = MolakGui.U, s0 = MolakLayout.CanvasPx, bottom = MolakLayout.ThArtBottom * s0 - 10f * u0;
            var tr = MolakScroll.TheaterTextGui;
            return new Rect(tr.x, Mathf.Max(0f, bottom - 500f * u0), tr.width, Mathf.Min(bottom, 500f * u0));
        }
        var pr = MolakScroll.CinemaPanelGui;
        if (MolakTheme.CinemaNow && pr.width > 1f)
        {
            float u = MolakGui.U, bottom = pr.y - 44f * u;
            return new Rect(pr.x, Mathf.Max(0f, bottom - 600f * u), pr.width, Mathf.Min(bottom, 600f * u));
        }
        return TextArea();
    }
    /// <summary>쪽지를 먹빛(그림 화면)으로 그리나.</summary>
    /// <summary>0.4.91 인장이 글자리의 글을 걷는 정도(0~1). MolakScroll 이 본문 알파에 곱한다.</summary>
    public static float StampCover;
    /// <summary>0.4.91 언메이커 시험: 그림·극장 화면에서도 인장 원을 검은 붓으로 — 옻칠 바닥에 묻혀 안 보여 흰 붓으로 둔다(언메이커: 흰색으로 해야겠다, 인장 손질은 나중에).</summary>
    public static bool InkBlack = false;
    /// <summary>0.4.91 시험: 인장 자리에만 한지 한 장을 깐다.</summary>
    public static bool SealCard = false;
    public static bool DarkSlip { get { return (MolakTheme.CinemaNow && MolakScroll.CinemaPanelGui.width > 1f) || (MolakTheme.TheaterNow && MolakScroll.TheaterTextGui.width > 1f); } }
    /// <summary>0.4.82 걷힌 마디 이름(극장판 위 띠 첫 줄이 쓴다).</summary>
    public static string PinText { get { return instance != null && instance.chapT < 0f ? instance.pinText : ""; } }
    public static Rect TextArea ()
    {
        {   // 0.4.82 그림 화면: 글자리 판 안쪽(인장·결과판·▼ 같은 글자리 위 연출이 판을 덮는다)
            var pr = MolakScroll.CinemaPanelGui;
            if (MolakTheme.CinemaNow && pr.width > 1f) return pr;
            if (MolakTheme.TheaterNow && MolakScroll.TheaterTextGui.width > 1f) return MolakScroll.TheaterTextGui;   // 극장판: 글 칸
        }
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
    static Texture2D edge;
    /// <summary>0.4.70 화면 가장자리·모서리에만 짙은 띠(가운데는 완전히 비운다). 체력 바닥·사술·번쩍임은 이것으로만 — 화면 전체가 깜빡이면 눈이 아프다(언메이커).</summary>
    static Texture2D Edge
    {
        get
        {
            if (edge != null) return edge;
            int S = 256; edge = new Texture2D(S, S, TextureFormat.RGBA32, false); edge.filterMode = FilterMode.Bilinear; edge.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float fx = (x + 0.5f) / S, fy = (y + 0.5f) / S;
                float ex = Mathf.Min(fx, 1f - fx), ey = Mathf.Min(fy, 1f - fy);
                float side = Mathf.Clamp01(1f - Mathf.Min(ex, ey) / 0.10f); side = side * side;           // 네 변: 안쪽 10% 까지
                float cd = Mathf.Sqrt(ex * ex + ey * ey);
                float corner = Mathf.Clamp01(1f - cd / 0.30f); corner = corner * corner * corner;        // 모서리는 조금 더 깊이
                edge.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(Mathf.Max(side * 0.85f, corner))));
            }
            edge.Apply(); return edge;
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
        MolakAmbience.Weather(kind);
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
        // 0.4.86 인장 시계는 프레임마다 간다. 예전엔 OnGUI 의 Repaint 에서만 늘어서, 화면이 다시 그려지지 않는 동안(창이 가려짐·다른 탭)
        //        시계가 멎고 자동으로 걷히는 인장(건너뛰기·자동 진행)이 끝내 걷히지 않아 대본이 멈췄다(소크 r0486 정책 7에서 봄).
        if (stampT >= 0f)
        {
            stampT += Time.unscaledDeltaTime;
            if (stampHold && ((stampAuto > 0f && stampT > stampAuto) || (MolakPlayMode.SkipAll && stampT > 0.3f))) { stampHold = false; stampOutAt = stampT; MolakInputGuard.MuteFor(0.2f); }
        }
        // 0.4.71 마디 이름: 누르면 걷힌다. 그 누름은 글을 넘기지 않는다.
        {
            Vector2 pp; bool down = MolakGui.PointerScreen(out pp);
            if (chapT >= 0f && chapHold && chapT > 0.45f && down && !chapWasDown && !MolakMenu.IsOpen)
            {
                chapHold = false; chapOutT = 0f; pinText = chapText;
                MolakInputGuard.BlockCurrentPress();
            }
            chapWasDown = down;
            var sp = Engine.Initialized ? Engine.GetService<IScriptPlayer>() : null;
            if (chapT >= 0f && chapHold && sp != null && sp.Skipping) { chapHold = false; chapOutT = 0f; pinText = chapText; }
            if (chapT >= 0f && chapHold && MolakPlayMode.Auto && chapT > 2.6f) { chapHold = false; chapOutT = 0f; pinText = chapText; }   // 0.4.83 자동 진행이면 잠깐 보여 주고 걷는다
            if (pinText.Length > 0 && !Mathf.Approximately(MolakStats.Get("day"), pinDay)) pinText = "";
        }
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

    GUIStyle sCineT, sCineS, sCineD; float cineU = -1f;
    /// <summary>0.4.82 그림 화면의 마디 이름 — 시안 F5 아래 결 그대로.
    /// 금빛 이름표(부제, 자간을 띄워)와 오른쪽으로 스러지는 금 실선 → 그 아래 마디 이름 → 회색 작은 글씨의 한 줄 설명(@chapter desc:).
    /// 실선이 먼저 그어지고 이름이 왼쪽에서 슥 미끄러져 들며 설명이 뒤따라 떠오른다. 누르면 왼쪽으로 조금 물러나며 스러지고 글자리가 떠오른다.</summary>
    void DrawChapterCine (float w, float h, float u, float fadeOut)
    {
        if (sCineT == null || !Mathf.Approximately(cineU, u))
        {
            cineU = u; if (gowun == null) gowun = MolakGui.Gowun;
            var hf = MolakTypography.Heading(gowun);
            sCineT = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(54 * u), alignment = TextAnchor.MiddleLeft, wordWrap = false, clipping = TextClipping.Overflow, richText = true };
            sCineS = new GUIStyle(sCineT) { fontSize = Mathf.RoundToInt(34 * u) };
            sCineD = new GUIStyle(sCineT) { fontSize = Mathf.RoundToInt(30 * u), alignment = TextAnchor.UpperLeft, wordWrap = true };
            sCineT.font = hf; sCineS.font = hf; sCineD.font = gowun;
        }
        float t = chapT;
        float outK = chapHold ? 0f : Mathf.Clamp01(chapOutT / fadeOut);
        float a = Mathf.Clamp01(t / 0.3f) * (1f - outK);
        float W0 = Mathf.Min(w, 1080 * u), X0 = (w - W0) / 2f;
        float lx = X0 + 82 * u, rx = X0 + W0 - 82 * u, cw = rx - lx;
        float back = -26f * u * outK;
        // 아래에서부터 쌓는다: 설명 → 마디 이름 → 이름표·실선
        float descH = chapDesc.Length > 0 ? sCineD.CalcHeight(new GUIContent(chapDesc), cw) : 0f;
        float bottom = h - 86 * u;
        if (MolakTheme.TheaterNow && MolakScroll.TheaterTextGui.width > 1f) bottom = MolakScroll.TheaterTextGui.y + 330 * u;   // 극장판: 글 칸 위쪽에
        float descY = bottom - descH;
        float titleH = 76 * u, titleY = (descH > 0f ? descY - 10 * u : bottom) - titleH;
        float labelH = 46 * u, labelY = titleY - 12 * u - labelH;
        var g0 = GUI.color;
        // 아래쪽을 먹으로 한 번 더 — 그림 위 글씨가 떠 보이게(글자리 판 대신)
        float washTop = labelY - 180 * u;
        for (int i = 0; i < 16; i++)
        {
            float f = i / 15f, y0 = washTop + 180 * u * f;
            GUI.color = new Color(0.067f, 0.059f, 0.055f, 0.5f * a * f * f);
            GUI.DrawTexture(new Rect(0, y0, w, 180 * u / 15f + 1f), Px);
        }
        GUI.color = new Color(0.067f, 0.059f, 0.055f, 0.5f * a);
        GUI.DrawTexture(new Rect(0, labelY, w, h - labelY), Px);
        GUI.color = g0;   // 라벨 글자에도 GUI.color 가 곱해진다 — 칠한 색을 걷고 쓴다
        // 이름표(부제) — 자간을 띄운 금빛 송명
        string sub = chapSub.Length > 0 ? string.Join(" ", System.Array.ConvertAll(chapSub.ToCharArray(), c => c.ToString())) : "";
        float subW = 0f, la = a * Mathf.Clamp01(t / 0.4f);
        if (sub.Length > 0)
        {
            subW = sCineS.CalcSize(new GUIContent(sub)).x;
            MolakGui.Ink(sCineS, new Color(0.91f, 0.78f, 0.47f, la));
            GUI.Label(new Rect(lx + back, labelY, subW + 10 * u, labelH), sub, sCineS);
        }
        // 금 실선: 이름표 오른쪽에서 끝까지 그어지며, 끝으로 갈수록 스러진다
        float lineK = 1f - Mathf.Pow(1f - Mathf.Clamp01((t - 0.1f) / 0.7f), 3f);
        float l0 = lx + (subW > 0f ? subW + 28 * u : 0f) + back, lEnd = rx, l1 = Mathf.Lerp(l0, lEnd, lineK);
        if (l1 > l0 + 1f)
        {
            int n = 40; float seg = (l1 - l0) / n, th = Mathf.Max(1f, 2.4f * u), ly = labelY + labelH * 0.5f;
            for (int i = 0; i < n; i++)
            {
                float f = (l0 + seg * (i + 0.5f) - l0) / Mathf.Max(1f, lEnd - l0);   // 다 그었을 때 기준 자리 — 그어지는 동안 색이 출렁이지 않게
                GUI.color = new Color(0.91f, 0.78f, 0.47f, 0.7f * a * (1f - f));
                GUI.DrawTexture(new Rect(l0 + seg * i, ly - th * 0.5f, seg + 1f, th), Px);
            }
            GUI.color = g0;
        }
        // 마디 이름: 왼쪽에서 미끄러져 든다
        float tIn = Mathf.Clamp01((t - 0.12f) / 0.55f), te = 1f - Mathf.Pow(1f - tIn, 3f);
        MolakGui.Ink(sCineT, new Color(0.957f, 0.929f, 0.878f, a * tIn));
        GUI.Label(new Rect(lx - 60f * u * (1f - te) + back, titleY, cw, titleH), chapText, sCineT);
        // 설명: 회색 작은 글씨, 이름 뒤에 살짝 떠오른다
        if (descH > 0f)
        {
            float dIn = Mathf.Clamp01((t - 0.4f) / 0.5f), de = 1f - Mathf.Pow(1f - dIn, 2f);
            MolakGui.Ink(sCineD, new Color(0.725f, 0.69f, 0.722f, a * dIn));
            GUI.Label(new Rect(lx + back, descY + 10 * u * (1f - de), cw, descH + 4 * u), chapDesc, sCineD);
        }
        if (chapHold && t > 1.2f)
        {
            float blink = 0.35f + 0.3f * Mathf.Sin(Time.unscaledTime * 3f);
            MolakGui.Ink(sCineS, new Color(0.91f, 0.78f, 0.47f, blink));
            var sz = sCineS.fontSize; sCineS.fontSize = Mathf.RoundToInt(24 * u);
            GUI.Label(new Rect(rx - 30 * u, bottom + 14 * u, 40 * u, 40 * u), "▼", sCineS);
            sCineS.fontSize = sz;
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
                // 0.4.70 가장자리·모서리에만, 맥박은 옅게(꺼졌다 켜지지 않고 짙어졌다 옅어지는 정도)
                float a = (0.30f + 0.25f * k) * (0.78f + 0.22f * Beat(Time.time, 64f + 30f * k));
                var g = GUI.color; GUI.color = new Color(0.55f, 0.04f, 0.03f, a); GUI.DrawTexture(new Rect(0, 0, w, h), Edge); GUI.color = g;
            }
            if (cu >= 75f)
            {
                float k = Mathf.InverseLerp(75f, 100f, cu);
                float a = (0.26f + 0.22f * k) * (0.8f + 0.2f * Mathf.Sin(Time.time * (0.9f + 0.8f * k)));
                var g = GUI.color; GUI.color = new Color(0.36f, 0.16f, 0.48f, Mathf.Max(0f, a)); GUI.DrawTexture(new Rect(0, 0, w, h), Edge); GUI.color = g;
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
                // 0.4.70 화면 전체를 물들이지 않는다 — 가장자리·모서리만
                GUI.color = new Color(flashC.r, flashC.g, flashC.b, Mathf.Min(1f, flashC.a * k * 1.4f)); GUI.DrawTexture(new Rect(0, 0, w, h), Edge);
                GUI.color = g;
            }
        }
        // 마디 이름
        if (chapT >= 0f)
        {
            chapT += Time.deltaTime;
            const float FadeOut = 0.6f;
            if (!chapHold) chapOutT += Time.deltaTime;
            if (!chapHold && chapOutT > FadeOut) chapT = -1f;
            else
            {
                if (chapHold) MolakInputGuard.Guard(new Rect(0, 0, w, h));   // 떠 있는 동안 누름은 글로 가지 않는다
                float u = MolakTheme.U;
                if (MolakTheme.CinemaNow || MolakTheme.TheaterNow) { DrawChapterCine(w, h, u, FadeOut); goto chapDone; }
                if (sChap == null || !Mathf.Approximately(chapU, u))
                {
                    chapU = u; if (gowun == null) gowun = MolakGui.Gowun;
                    sChap = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(64 * u), alignment = TextAnchor.MiddleCenter };
                    sChapSub = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(26 * u), alignment = TextAnchor.MiddleCenter };
                    sChap.font = MolakTypography.Heading(gowun); sChapSub.font = MolakTypography.Heading(gowun);
                }
                float a = chapT < 0.35f ? chapT / 0.35f : (!chapHold ? Mathf.Clamp01(1f - chapOutT / FadeOut) : 1f);
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
                if (chapHold && chapT > 1.2f)
                {
                    float blink = 0.35f + 0.25f * Mathf.Sin(Time.unscaledTime * 3f);
                    MolakGui.Ink(sChapSub, new Color(0.96f, 0.92f, 0.85f, blink));
                    GUI.Label(new Rect(0, cy + 34 * u, w, 30 * u), "▼", sChapSub);
                }
            }
        }
        chapDone:
        // 0.4.71 걷힌 마디 이름 — 그림 띠 왼쪽 위에 작게 남는다
        if (pinText.Length > 0 && chapT < 0f && !MolakTitle.Showing && !MolakTheme.TheaterNow)   // 극장판은 위 띠 첫 줄에(HUD)
        {
            float u = MolakTheme.U;
            if (sPin == null || !Mathf.Approximately(pinU, u))
            {
                pinU = u; if (gowun == null) gowun = MolakGui.Gowun;
                sPin = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt((MolakTheme.CinemaNow ? 32 : 24) * u), alignment = TextAnchor.MiddleLeft };
                sPin.font = MolakTypography.Heading(gowun);
            }
            float cu = Mathf.Min(w / 900f, h / 1600f);   // 그림 띠(MolakArt 캔버스 900×1600) 왼쪽 아래 안쪽
            float py = (MolakArt.BandTop + MolakArt.BandH) * cu - 44 * u, px = w * 0.5f - Mathf.Min(w, 900f * cu) * 0.5f + 18 * u;
            if (MolakTheme.CinemaNow)
            {   // 0.4.82 그림 화면: 위 수치 줄 바로 아래 왼쪽
                float W0 = Mathf.Min(w, 1080 * u);
                px = (w - W0) / 2f + 44 * u; py = (MolakLayout.CineTopU + MolakLayout.CineBtnU) * u + 26 * u + (MolakHud.WarnShown ? 40 * u : 0f);
            }
            // 0.4.82 그림 화면은 조금 크게(언메이커: 왼쪽 위 소제목이 더 커도 되겠다)
            int pfs = Mathf.RoundToInt((MolakTheme.CinemaNow ? 32 : 24) * u); if (sPin.fontSize != pfs) sPin.fontSize = pfs;
            float ph = MolakTheme.CinemaNow ? 52 * u : 40 * u;
            var sz = sPin.CalcSize(new GUIContent(pinText));
            var g1 = GUI.color;
            GUI.color = new Color(0.06f, 0.05f, 0.04f, 0.55f);
            GUI.DrawTexture(new Rect(px - 12 * u, py - 4 * u, sz.x + 24 * u, ph), Px);
            GUI.color = g1;
            MolakGui.Ink(sPin, new Color(MolakTheme.Gold.r, MolakTheme.Gold.g, MolakTheme.Gold.b, 0.92f));
            GUI.Label(new Rect(px, py - 4 * u, sz.x + 4 * u, ph), pinText, sPin);
        }
        // 승부수 인장
        if (stampT < 0f) StampCover = 0f;
        if (stampT >= 0f)
        {
            const float Out = 0.35f;
            if (stampHold)
            {
                MolakInputGuard.Guard(new Rect(0, 0, w, h));
                bool tap = Event.current.type == EventType.MouseDown && stampT > 0.25f;
                bool auto = (stampAuto > 0f && stampT > stampAuto) || (MolakPlayMode.SkipAll && stampT > 0.3f);   // 0.4.85 검증 넘기기 중에 찍힌 인장이 끝까지 붙잡던 것
                if (tap || auto) { if (tap) Event.current.Use(); stampHold = false; stampOutAt = stampT; MolakInputGuard.MuteFor(0.2f); }
            }
            else if (stampOutAt < 0f && stampT > 1.1f) stampOutAt = stampT;   // 붙잡지 않는 인장(제목 화면 등)은 1.1초 뒤 걷힌다
            if (!stampHold && stampOutAt >= 0f && stampT - stampOutAt > Out) { stampT = -1f; stampOutAt = -1f; StampCover = 0f; }
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
                var ta = TextArea();
                // 0.4.91 언메이커: 대사를 지우고 인장을 띄운다 — 그림·극장 화면에선 덮개판을 깔지 않고 글자리의 글을 걷는다(MolakScroll 이 StampCover 만큼 본문을 감춘다).
                //   종이 화면은 예전처럼 빈 종이를 덮는다.
                bool dk = DarkSlip;
                if (dk) StampCover = a;
                else { var bg = GUI.color; GUI.color = new Color(0.955f, 0.94f, 0.905f, 0.99f * a); GUI.DrawTexture(ta, Px); GUI.color = bg; }
                var piv = new Vector2(ta.center.x, ta.y + ta.height * 0.5f - 30f * u);
                // 0.4.85 인장(시안 3 「먹 원 · 붉은 획」): 먹 원을 긋고 그 안에 판정의 빛으로 한 글자. 큰 판정은 조금 더 크게.
                float bs = (stampBig ? 340f : 300f) * u;
                var box = new Rect(piv.x - bs / 2f, piv.y - bs / 2f, bs, bs);
                var backC = dk ? new Color(0.098f, 0.082f, 0.07f, 1f) : new Color(0.955f, 0.94f, 0.905f, 1f);
                if (dk && SealCard)
                {   // 0.4.91 시험: 인장 자리에만 한지 한 장(검은 붓 원이 바닥에 묻히지 않게)
                    var card = new Rect(box.x - bs * 0.12f, box.y - bs * 0.12f, bs * 1.24f, bs * 1.24f);
                    var gc = GUI.color; GUI.color = new Color(0.94f, 0.91f, 0.84f, 0.97f * a); GUI.DrawTexture(card, Px); GUI.color = gc;
                    backC = new Color(0.94f, 0.91f, 0.84f, 1f);
                }
                MolakSeal.Draw(box, stampHanja, stampT, a, MolakGui.Hanja, backC, InkBlack || SealCard || !dk);   // 0.4.91 옻칠 바닥이면 흰 붓, 한지(종이 화면·SealCard)면 검은 붓
                MolakGui.Ink(sStampLine, dk ? new Color(0.92f, 0.88f, 0.82f, a) : new Color(0.16f, 0.13f, 0.1f, a));
                GUI.Label(new Rect(ta.x + 20f * u, piv.y + (stampBig ? 180f : 160f) * u, ta.width - 40f * u, 90f * u), stampLine, sStampLine);
                if (stampHold && stampAuto < 0f && stampT > 0.6f)
                {
                    MolakGui.Ink(sStampHint, dk ? new Color(0.66f, 0.60f, 0.53f, Mathf.Clamp01((stampT - 0.6f) / 0.4f) * a) : new Color(0.45f, 0.4f, 0.34f, Mathf.Clamp01((stampT - 0.6f) / 0.4f) * a));
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
