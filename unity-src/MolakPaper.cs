using UnityEngine;

/// <summary>
/// 0.4.80 창의 결 — 이야기 모드 장 고르기(MolakStoryBook)에서 뽑은 한 벌.
/// 종이 바탕 · 흰 카드 · 왼쪽 띠 · 붉은 묶음 제목과 가는 줄 · 먹색 「닫는다」 단추 하나.
/// 메뉴·환경 설정·그 밤의 진실·길잡이·기록 판·비무 연습·이름 적기·결과판·수치 쪽지가 이것만 쓴다.
/// 먹색 화면(MolakTheme.Night)이면 같은 자리에 먹 바탕 · 짙은 카드 · 밝은 띠.
/// 읽는 화면의 붓 자국 선택지와 回 테두리 틀은 이 결을 쓰지 않는다(이야기 쪽 결).
/// </summary>
public static class MolakPaper
{
    static bool N { get { return MolakTheme.Night; } }

    // ---- 색 ----
    /// <summary>창 바탕(종이 / 먹)</summary>
    public static Color Ground { get { return N ? new Color(0.086f, 0.075f, 0.059f, 1f) : new Color(0.961f, 0.941f, 0.890f, 1f); } }
    /// <summary>카드 얼굴</summary>
    public static Color Card { get { return N ? new Color(0.133f, 0.114f, 0.090f, 1f) : new Color(1f, 0.988f, 0.961f, 1f); } }
    /// <summary>누른 카드</summary>
    public static Color CardDown { get { return N ? new Color(0.18f, 0.155f, 0.125f, 1f) : new Color(0.93f, 0.905f, 0.85f, 1f); } }
    /// <summary>글자(먹)</summary>
    public static Color Ink { get { return N ? new Color(0.929f, 0.894f, 0.827f, 1f) : new Color(0.122f, 0.102f, 0.078f, 1f); } }
    /// <summary>풀이 · 숫자(먹 옅게)</summary>
    public static Color Sub { get { return N ? new Color(0.659f, 0.604f, 0.525f, 1f) : new Color(0.431f, 0.369f, 0.298f, 1f); } }
    /// <summary>카드 아래 가는 줄</summary>
    public static Color Hair { get { return N ? new Color(0.78f, 0.70f, 0.59f, 0.18f) : new Color(0.55f, 0.48f, 0.40f, 0.35f); } }
    /// <summary>검붉음 — 이야기 · 기록 띠, 묶음 제목, 제목 밑줄</summary>
    public static Color Accent { get { return N ? new Color(0.769f, 0.341f, 0.290f, 1f) : new Color(0.549f, 0.161f, 0.122f, 1f); } }
    /// <summary>쪽빛 — 둘째 생</summary>
    public static Color Blue { get { return N ? new Color(0.435f, 0.533f, 0.761f, 1f) : new Color(0.200f, 0.302f, 0.522f, 1f); } }
    /// <summary>솔빛 — 셋째 생부터</summary>
    public static Color Pine { get { return N ? new Color(0.50f, 0.66f, 0.50f, 1f) : new Color(0.30f, 0.42f, 0.30f, 1f); } }
    /// <summary>비어 있는 칸 · 꺼진 띠</summary>
    public static Color Empty { get { return N ? new Color(0.78f, 0.70f, 0.59f, 0.35f) : new Color(0.55f, 0.48f, 0.40f, 0.55f); } }
    /// <summary>먹색 단추 얼굴(먹색 화면에선 종이빛)</summary>
    public static Color Button { get { return Ink; } }
    /// <summary>먹색 단추 글자</summary>
    public static Color ButtonText { get { return Ground; } }
    /// <summary>미끄럼대 · 꺼진 토글의 바닥</summary>
    public static Color Track { get { return N ? new Color(0.30f, 0.27f, 0.22f, 1f) : new Color(0.867f, 0.827f, 0.757f, 1f); } }

    /// <summary>지금 손가락·마우스가 이 칸을 누르고 있나(GUI 좌표, 그룹 안이면 그룹 기준).</summary>
    public static bool Pressed (Rect r)
    {
        Vector2 pg; if (!MolakGui.PointerGui(out pg)) return false;
        return r.Contains(GUIUtility.ScreenToGUIPoint(pg));
    }

    public static void Box (Rect r, Color c) { var o = GUI.color; GUI.color = new Color(c.r, c.g, c.b, c.a * o.a); GUI.DrawTexture(r, MolakGui.Px); GUI.color = o; }

    // ---- 글꼴(1080 기준 크기) ----
    static GUIStyle sHead, sSub, sTitle, sTitleC, sSmall, sSmallR, sSection, sButton, sBody, sNote;
    static float su = -1f; static bool sn;
    static void Styles (float u)
    {
        if (sHead != null && Mathf.Approximately(su, u) && sn == N) return;
        su = u; sn = N;
        sHead = MolakGui.Label(64, u, TextAnchor.MiddleLeft, false); MolakGui.Ink(sHead, Ink);
        sSub = MolakGui.Label(28, u, TextAnchor.UpperLeft, true); MolakGui.Ink(sSub, Sub);
        sTitle = MolakGui.Label(38, u, TextAnchor.MiddleLeft, false); MolakGui.Ink(sTitle, Ink);
        sTitleC = MolakGui.Label(36, u, TextAnchor.MiddleCenter, false); MolakGui.Ink(sTitleC, Ink);
        sSmall = MolakGui.Label(26, u, TextAnchor.MiddleLeft, false); MolakGui.Ink(sSmall, Sub);
        sSmallR = MolakGui.Label(26, u, TextAnchor.MiddleRight, false); MolakGui.Ink(sSmallR, Sub);
        sSection = MolakGui.Label(30, u, TextAnchor.MiddleLeft, false); MolakGui.Ink(sSection, Accent);
        sButton = MolakGui.Label(36, u, TextAnchor.MiddleCenter, false); MolakGui.Ink(sButton, ButtonText);
        sBody = MolakGui.Label(32, u, TextAnchor.UpperLeft, true); MolakGui.Ink(sBody, Ink);
        sNote = MolakGui.Label(26, u, TextAnchor.UpperCenter, true); MolakGui.Ink(sNote, Sub);
    }
    public static GUIStyle HeadStyle (float u) { Styles(u); return sHead; }
    public static GUIStyle SubStyle (float u) { Styles(u); return sSub; }
    public static GUIStyle TitleStyle (float u) { Styles(u); return sTitle; }
    public static GUIStyle TitleCenter (float u) { Styles(u); return sTitleC; }
    public static GUIStyle SmallStyle (float u) { Styles(u); return sSmall; }
    public static GUIStyle SmallRight (float u) { Styles(u); return sSmallR; }
    public static GUIStyle SectionStyle (float u) { Styles(u); return sSection; }
    public static GUIStyle BodyStyle (float u) { Styles(u); return sBody; }
    public static GUIStyle NoteStyle (float u) { Styles(u); return sNote; }

    // ---- 자리 ----
    /// <summary>창의 좌우 여백(넓은 화면에선 가운데 980 폭)</summary>
    public static float Margin (float W, float u) { return Mathf.Max(48f * u, (W - 980f * u) * 0.5f); }
    /// <summary>창의 첫 줄 높이</summary>
    public static float Top (float H, float u) { return Mathf.Max(70f * u, H * 0.045f); }

    /// <summary>창 바탕을 통째로 덮는다(뒤의 본문이 비치지 않게). a = 열리는 동안의 짙기.</summary>
    public static void Backdrop (float W, float H, float a = 1f)
    {
        // 앞서 그린 창이 GUI.color 의 알파를 낮춰 둔 채 끝나면 바탕이 비쳐 뒤의 본문이 보이던 것 — 바탕만은 GUI.color 와 상관없이 칠한다
        var o = GUI.color; GUI.color = new Color(Ground.r, Ground.g, Ground.b, a);
        GUI.DrawTexture(new Rect(0, 0, W, H), MolakGui.Px);
        GUI.color = o;
    }

    /// <summary>제목 · 한 줄 풀이 · 붉은 밑줄. y 를 그 아래로 옮긴다.</summary>
    public static void Header (ref float y, float m, float cw, string title, string sub, float u)
    {
        Styles(u);
        GUI.Label(new Rect(m, y, cw, 80 * u), title, sHead); y += 84 * u;
        if (!string.IsNullOrEmpty(sub))
        {
            float sh = Mathf.Max(40f * u, sSub.CalcHeight(new GUIContent(sub), cw));
            GUI.Label(new Rect(m, y, cw, sh), sub, sSub); y += sh + 10 * u;
        }
        Box(new Rect(m, y, 120 * u, Mathf.Max(1f, 2f * u)), Accent); y += 26 * u;
    }

    /// <summary>오른쪽 위 닫기(×). 눌리면 참.</summary>
    public static bool CloseX (float W, float m, float y, float u)
    {
        Styles(u);
        var r = new Rect(W - m - 88 * u, y, 88 * u, 88 * u);
        var st = new GUIStyle(sTitleC) { fontSize = Mathf.RoundToInt(48 * u) }; MolakGui.Ink(st, Sub);
        GUI.Label(r, "×", st);
        return GUI.Button(r, GUIContent.none, GUIStyle.none);
    }

    /// <summary>붉은 묶음 제목과 그 뒤로 이어지는 가는 줄. y 를 그 아래로.</summary>
    public static void Section (ref float y, float m, float cw, string name, float u)
    {
        Styles(u);
        var c = new GUIContent(name);
        float tw = Mathf.Min(sSection.CalcSize(c).x, name.Length * 34f * u + 8f * u);   // 처음 그리는 글자는 CalcSize 가 크게 잡혀 줄이 사라지던 것
        GUI.Label(new Rect(m, y, tw + 4 * u, 50 * u), name, sSection);
        Box(new Rect(Mathf.Round(m + tw + 20 * u), Mathf.Round(y + 25 * u), cw - tw - 20 * u, Mathf.Max(1f, Mathf.Round(1.5f * u))), new Color(Accent.r, Accent.g, Accent.b, 0.35f));   // 픽셀에 맞춰야 1px 줄이 반쯤 사라지지 않는다
        y += 58 * u;
    }

    /// <summary>카드 바닥만(얼굴 · 가는 줄 · 왼쪽 띠). bar.a == 0 이면 띠 없음.</summary>
    public static void CardFace (Rect r, Color bar, float u, bool pressed = false)
    {
        Box(r, pressed ? CardDown : Card);
        float hl = Mathf.Max(1f, Mathf.Round(1.5f * u));
        Box(new Rect(r.x, Mathf.Round(r.yMax - hl), r.width, hl), Hair);
        if (bar.a > 0.001f) Box(new Rect(r.x, r.y, Mathf.Max(2f, 10f * u), r.height), bar);
    }

    /// <summary>이름과 풀이가 있는 카드 한 장. 눌리면 참.</summary>
    public static bool Card2 (Rect r, string title, string sub, Color bar, float u, bool canTap = true)
    {
        Styles(u);
        bool over = Pressed(r);
        CardFace(r, bar, u, over && canTap);
        float px = r.x + (bar.a > 0.001f ? 34f : 26f) * u, pw = r.width - (px - r.x) - 20 * u;
        if (string.IsNullOrEmpty(sub)) GUI.Label(new Rect(px, r.y, pw, r.height), title, sTitle);
        else
        {
            float th = 52 * u, sh = 36 * u, gap = 4 * u, top = r.y + (r.height - th - sh - gap) * 0.5f;
            GUI.Label(new Rect(px, top, pw, th), title, sTitle);
            GUI.Label(new Rect(px, top + th + gap, pw, sh), sub, sSmall);
        }
        return canTap && GUI.Button(r, GUIContent.none, GUIStyle.none);
    }

    /// <summary>먹색 단추(창마다 하나 — 나가는 길). 눌리면 참.</summary>
    public static bool InkButton (Rect r, string text, float u, bool canTap = true)
    {
        Styles(u);
        bool over = Pressed(r);
        var c = Button; if (over && canTap) c = Color.Lerp(c, Ground, 0.18f);
        Box(r, c);
        GUI.Label(r, text, sButton);
        return canTap && GUI.Button(r, GUIContent.none, GUIStyle.none);
    }

    /// <summary>아래쪽 먹색 단추 자리(가운데 400×96).</summary>
    public static Rect BottomButton (float W, float H, float u)
    {
        float bh = 96 * u;
        return new Rect(W * 0.5f - 200 * u, H - bh - Mathf.Max(60f * u, H * 0.04f), 400 * u, bh);
    }

    /// <summary>켜고 끄는 알약(46×24 비율). on 이면 먹, 아니면 바닥색.</summary>
    public static void ToggleKnob (Rect r, bool on)
    {
        float h = r.height, kd = h * 0.75f, pad = (h - kd) * 0.5f;
        DrawPill(r, on ? Button : Track);
        var kr = new Rect(on ? r.xMax - pad - kd : r.x + pad, r.y + pad, kd, kd);
        DrawDot(kr, Card);
    }

    /// <summary>미끄럼대(t 0~1). 붉은 채움, 종이빛 손잡이에 먹 테.</summary>
    public static void Slider (Rect track, float t, float u)
    {
        float th = Mathf.Max(2f, 6f * u), cy = track.center.y;
        Box(new Rect(track.x, cy - th * 0.5f, track.width, th), Track);
        Box(new Rect(track.x, cy - th * 0.5f, track.width * Mathf.Clamp01(t), th), Accent);
        float kd = 34f * u, kx = track.x + track.width * Mathf.Clamp01(t) - kd * 0.5f;
        DrawDot(new Rect(kx - 3f * u, cy - kd * 0.5f - 3f * u, kd + 6f * u, kd + 6f * u), Ink);
        DrawDot(new Rect(kx, cy - kd * 0.5f, kd, kd), Card);
    }

    // ---- 둥근 조각(작은 원 텍스처 한 장) ----
    static Texture2D dot;
    static Texture2D Dot
    {
        get
        {
            if (dot != null) return dot;
            int n = 64; dot = new Texture2D(n, n, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            float c = (n - 1) * 0.5f, rad = n * 0.5f - 1f;
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                dot.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(rad - d + 0.5f)));
            }
            dot.Apply();
            return dot;
        }
    }
    public static void DrawDot (Rect r, Color c) { var o = GUI.color; GUI.color = new Color(c.r, c.g, c.b, c.a * o.a); GUI.DrawTexture(r, Dot); GUI.color = o; }
    public static void DrawPill (Rect r, Color c)
    {
        float h = r.height;
        DrawDot(new Rect(r.x, r.y, h, h), c);
        DrawDot(new Rect(r.xMax - h, r.y, h, h), c);
        Box(new Rect(r.x + h * 0.5f, r.y, r.width - h, h), c);
    }

    // ---- 끌어 올리는 목록 ----
    /// <summary>손가락·마우스로 끌고, 휠로 굴리는 세로 목록 하나의 상태.</summary>
    public class Scroller
    {
        public float y, v; float downY = -1f, lastY; public bool dragging;
        /// <summary>Repaint/ScrollWheel 에서 부른다. 끌고 있는 동안은 카드가 눌리지 않게 dragging 을 본다.</summary>
        public void Update (Rect view, float max, float u)
        {
            Vector2 pg; bool down = MolakGui.PointerGui(out pg);
            var ev = Event.current;
            if (ev.type == EventType.Repaint)
            {
                if (down && view.Contains(pg))
                {
                    if (downY < 0f) { downY = pg.y; lastY = pg.y; dragging = false; }
                    if (Mathf.Abs(pg.y - downY) > 14f * u) dragging = true;
                    if (dragging) { float d = lastY - pg.y; y = Mathf.Clamp(y + d, 0f, max); v = d / Mathf.Max(0.001f, Time.unscaledDeltaTime); }
                    lastY = pg.y;
                }
                else if (!down)
                {
                    downY = -1f; dragging = false;
                    if (Mathf.Abs(v) > 1f) { y = Mathf.Clamp(y + v * Time.unscaledDeltaTime, 0f, max); v *= Mathf.Pow(0.04f, Time.unscaledDeltaTime); }
                }
            }
            if (ev.type == EventType.ScrollWheel && view.Contains(ev.mousePosition)) { y = Mathf.Clamp(y + ev.delta.y * 40f * u, 0f, max); ev.Use(); }
            y = Mathf.Clamp(y, 0f, max);
        }
        public void Reset () { y = 0f; v = 0f; downY = -1f; dragging = false; }
    }
}
