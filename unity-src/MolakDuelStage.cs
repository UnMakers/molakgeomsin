using Naninovel;
using UnityEngine;

/// <summary>
/// 0.4.49 비무의 그림. 합(合)마다 글만 있던 것을 그림 띠 위에서 보이게 한다.
///  · 상대의 기세: 攻(찌르러 온다) — 인물이 한 걸음 덤벼들고 붉은 속도선이 모인다.
///                守(굳게 받는다) — 인물이 반 걸음 물러서고 앞에 칼날 한 줄이 선다.
///                虛(허초로 흔든다) — 인물의 잔상 둘이 좌우로 흔들린다.
///  · 내 수: 정면(斬) — 대각선 한 획, 받아치기(受) — 둥근 획과 불꽃, 흔들기(變) — 꺾인 세 획. 이기면 흰 먹, 지면 붉은 먹.
///  · 첫 비무 때 「비무의 법」 한 장(기세 셋과 맞수, 기세가 승산에 더해지는 것). 이 기기에서 한 번 보면 다시 뜨지 않는다.
/// </summary>
public class MolakDuelStage : MonoBehaviour
{
    static MolakDuelStage inst;
    static int tell; static float tellT = -1f;
    static int act, res; static float strikeT = -1f;
    static bool guideOpen; static float guideT;
    const string GuidePref = "molak.duelguide2_kp";   // 0.4.55 필살·검결 줄이 붙어서 한 번 더 보여 준다

    public static bool GuideOpen { get { return guideOpen; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot ()
    {
        if (inst) return;
        var go = new GameObject("MolakDuelStage"); DontDestroyOnLoad(go);
        inst = go.AddComponent<MolakDuelStage>();
    }

    /// <summary>shown 0 = 읽히지 않는 수(처음 보는 필살 초식).</summary>
    public static void Tell (int shown) { tell = Mathf.Clamp(shown, 0, 3); tellT = 0f; strikeT = -1f; MolakArt.Pose(tell); }
    public static void Strike (int a, int r) { act = a; res = r; strikeT = 0f; tellT = -1f; MolakArt.Pose(0); }
    public static void Clear () { tellT = -1f; strikeT = -1f; MolakArt.Pose(0); }

    /// <summary>처음이면 안내를 연다. 열었으면 true.</summary>
    public static bool ShowGuideIfFirst ()
    {
        bool seen = false;
        try { seen = PlayerPrefs.GetInt(GuidePref, 0) > 0; } catch { }
        if (seen) return false;
        OpenGuide();
        return true;
    }
    public static void OpenGuide () { guideOpen = true; guideT = 0f; }

    static readonly string[] TellHanja = { "絶", "攻", "守", "虛" };
    static readonly string[] TellName = { "읽히지 않는다", "찌르러 온다", "굳게 받는다", "허초로 흔든다" };

    GUIStyle sSeal, sSealSub, sHead, sRow, sRowH, sHint, sBtn; float builtU = -1f;
    void Styles (float u)
    {
        if (sSeal != null && Mathf.Approximately(builtU, u)) return;
        builtU = u;
        sSeal = MolakGui.Label(84, u, TextAnchor.MiddleCenter, false); if (MolakGui.Hanja != null) sSeal.font = MolakGui.Hanja;
        sSealSub = MolakGui.Label(26, u, TextAnchor.MiddleCenter, false);
        sHead = MolakGui.Label(44, u, TextAnchor.MiddleCenter, false); sHead.font = MolakTypography.Heading(sHead.font);
        sRow = MolakGui.Label(30, u, TextAnchor.MiddleLeft, true);
        sRowH = MolakGui.Label(56, u, TextAnchor.MiddleCenter, false); if (MolakGui.Hanja != null) sRowH.font = MolakGui.Hanja;
        sHint = MolakGui.Label(27, u, TextAnchor.UpperLeft, true);
        sBtn = MolakGui.Label(32, u, TextAnchor.MiddleCenter, false);
    }

    /// <summary>그림 띠의 화면(GUI) 좌표. 캔버스 900×1600 기준 띠(184~484)를 화면에 맞춘다(MolakArt 와 같은 계산).</summary>
    static Rect BandRect ()
    {
        float unit = Mathf.Min(Screen.width / 900f, Screen.height / 1600f);
        float top = (Screen.height - 1600f * unit) * 0.5f;   // 세로가 남으면 위아래로 나눠 남는다(Expand)
        if (top < 0f) top = 0f;
        float w = Mathf.Min(Screen.width, 900f * unit);
        return new Rect((Screen.width - w) * 0.5f, top + MolakArt.BandTop * unit, w, MolakArt.BandH * unit);
    }

    static void Line (Vector2 a, Vector2 b, float thick, Color c)
    {
        var d = b - a; float len = d.magnitude; if (len < 1f) return;
        float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        var m = GUI.matrix; var g = GUI.color;
        GUIUtility.RotateAroundPivot(ang, a);
        GUI.color = c * g;
        GUI.DrawTexture(new Rect(a.x, a.y - thick * 0.5f, len, thick), MolakGui.Px);
        GUI.matrix = m; GUI.color = g;
    }

    void OnGUI ()
    {
        if (!Engine.Initialized) return;
        float u = MolakGui.U; Styles(u);
        if (guideOpen) { DrawGuide(u); return; }
        if (!MolakArt.InDuel || MolakMenu.IsOpen) return;
        GUI.depth = MolakGui.DepthVfx - 5;
        var band = BandRect();
        float dt = Time.unscaledDeltaTime;
        // 띠 밖(HUD 위)으로 넘치지 않게 띠 안에서만 그린다
        GUI.BeginGroup(band);
        var local = new Rect(0f, 0f, band.width, band.height);
        if (tellT >= 0f) { tellT += dt; DrawTell(local, u); }
        if (strikeT >= 0f) { strikeT += dt; DrawStrike(local, u); if (strikeT > 0.9f) strikeT = -1f; }
        GUI.EndGroup();
        // 0.4.75 간파 판은 그림 띠 아래 칸에(상대 그림을 가리지 않게)
        if (MolakLayout.DuelStrip > 0f)
        {
            float unit = Mathf.Min(Screen.width / 900f, Screen.height / 1600f);
            var strip = new Rect(band.x, band.yMax + 6f * unit, band.width, (MolakLayout.DuelStripH - 6f) * unit);
            GUI.BeginGroup(strip);
            DrawBoard(new Rect(0f, 0f, strip.width, strip.height), u);
            GUI.EndGroup();
        }
    }

    // ---- 0.4.67 간파 판: 그림 띠 아래쪽에 상대의 세 수(위 줄)와 내 수(아래 줄) ----
    static readonly string[] MvH = { "？", "攻", "守", "虛" };
    static readonly string[] MvN = { "엎어진 수", "찌른다", "버틴다", "허초" };
    static readonly Color[] MvC = { new Color(0.35f, 0.32f, 0.3f, 1f), new Color(0.62f, 0.12f, 0.08f, 1f), new Color(0.2f, 0.36f, 0.55f, 1f), new Color(0.42f, 0.26f, 0.56f, 1f) };
    static readonly string[] AcH = { "", "斬", "受", "變", "觀", "返", "步" };
    static readonly string[] AcN = { "", "정면", "받아친다", "흔든다", "관", "운반", "적운보" };
    GUIStyle sCard, sCardSub, sMark; float cardU = -1f;
    void DrawBoard (Rect band, float u)
    {
        if (sCard == null || !Mathf.Approximately(cardU, u))
        {
            cardU = u;
            sCard = MolakGui.Label(46, u, TextAnchor.MiddleCenter, false); if (MolakGui.Hanja != null) sCard.font = MolakGui.Hanja;
            sCardSub = MolakGui.Label(22, u, TextAnchor.MiddleCenter, false);
            sMark = MolakGui.Label(54, u, TextAnchor.MiddleCenter, false);
        }
        var g0 = GUI.color;
        int k = Mathf.RoundToInt(MolakStats.Get("kp_k")) + 1;
        int resolved = Mathf.RoundToInt(MolakStats.Get("dx_i"));
        bool resolving = MolakStats.Get("kp_k") >= 3f;
        int sigi = Mathf.RoundToInt(MolakStats.Get("dx_sigi"));
        float ch = band.height * 0.44f, cw = Mathf.Min(band.width * 0.2f, ch * 1.6f), gap = cw * 0.22f;
        float total = cw * 3f + gap * 2f, x0 = band.x + (band.width - total) * 0.5f + cw * 0.2f;
        float yTop = band.y + band.height * 0.04f;
        var panel = new Rect(band.x, band.y, band.width, band.height);
        GUI.color = new Color(0.05f, 0.04f, 0.03f, 0.55f); GUI.DrawTexture(panel, MolakGui.Px);
        GUI.color = Color.white;
        MolakGui.Ink(sCardSub, new Color(0.85f, 0.78f, 0.66f, 0.9f));
        GUI.Label(new Rect(x0 - cw * 0.8f, yTop, cw * 0.65f, ch), "상대", sCardSub);
        GUI.Label(new Rect(x0 - cw * 0.8f, yTop + ch + band.height * 0.04f, cw * 0.65f, ch), "나", sCardSub);
        for (int i = 1; i <= 3; i++)
        {
            float x = x0 + (i - 1) * (cw + gap);
            int t = Mathf.RoundToInt(MolakStats.Get("kp_t" + i));
            bool vis = MolakStats.Get("kp_v" + i) > 0 || (resolving && i <= resolved);
            bool dep = MolakStats.Get("kp_d" + i) > 0;
            bool cur = !resolving && i == k;
            // 위: 상대
            var r1 = new Rect(x, yTop, cw, ch);
            string hj = dep ? "察" : i == sigi && !vis ? "絶" : vis ? MvH[Mathf.Clamp(t, 0, 3)] : "？";
            string nm = dep ? "앞 수를 본다" : i == sigi ? (vis ? "필살 · " + MvN[Mathf.Clamp(t, 0, 3)] : "필살") : vis ? MvN[Mathf.Clamp(t, 0, 3)] : "엎어진 수";
            Color bg = dep ? new Color(0.3f, 0.28f, 0.24f, 1f) : i == sigi ? new Color(0.5f, 0.06f, 0.05f, 1f) : vis ? MvC[Mathf.Clamp(t, 0, 3)] : MvC[0];
            GUI.color = new Color(bg.r, bg.g, bg.b, 0.92f); GUI.DrawTexture(r1, MolakGui.Px);
            if (cur) { GUI.color = new Color(0.85f, 0.68f, 0.3f, 0.9f + 0.1f * Mathf.Sin(Time.unscaledTime * 6f)); DrawFrame(r1, Mathf.Max(2f, 3f * u)); }
            GUI.color = Color.white;
            MolakGui.Ink(sCard, new Color(0.96f, 0.93f, 0.86f, 1f)); GUI.Label(new Rect(r1.x, r1.y, r1.width, r1.height * 0.74f), hj, sCard);
            MolakGui.Ink(sCardSub, new Color(0.9f, 0.86f, 0.78f, 0.9f)); GUI.Label(new Rect(r1.x, r1.y + r1.height * 0.66f, r1.width, r1.height * 0.32f), nm, sCardSub);
            // 아래: 나
            var r2 = new Rect(x, yTop + ch + band.height * 0.04f, cw, ch);
            int a = Mathf.RoundToInt(MolakStats.Get("kp_a" + i));
            GUI.color = a > 0 ? new Color(0.93f, 0.89f, 0.8f, 0.95f) : new Color(1f, 1f, 1f, 0.1f); GUI.DrawTexture(r2, MolakGui.Px);
            if (a > 0)
            {
                MolakGui.Ink(sCard, new Color(0.12f, 0.1f, 0.08f, 1f)); GUI.Label(new Rect(r2.x, r2.y, r2.width, r2.height * 0.74f), AcH[Mathf.Clamp(a, 0, 6)], sCard);
                MolakGui.Ink(sCardSub, new Color(0.25f, 0.2f, 0.16f, 1f)); GUI.Label(new Rect(r2.x, r2.y + r2.height * 0.66f, r2.width, r2.height * 0.32f), AcN[Mathf.Clamp(a, 0, 6)], sCardSub);
            }
            else { MolakGui.Ink(sCardSub, new Color(0.9f, 0.86f, 0.78f, 0.6f)); GUI.Label(r2, (i == 3 ? "결(決)" : "") + (cur ? " ▼" : ""), sCardSub); }
            // 결과 표식
            if (resolving && i <= resolved)
            {
                float rr = MolakStats.Get("kp_r" + i);
                string mk = rr > 0 ? "○" : rr < 0 ? "×" : "=";
                Color mc = rr > 0 ? new Color(0.95f, 0.8f, 0.35f, 1f) : rr < 0 ? new Color(0.9f, 0.3f, 0.25f, 1f) : new Color(0.85f, 0.82f, 0.76f, 1f);
                MolakGui.Ink(sMark, mc); GUI.Label(new Rect(x, yTop + ch * 0.5f, cw, ch), mk, sMark);
            }
        }
        GUI.color = g0;
    }

    static void DrawFrame (Rect r, float th)
    {
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, th), MolakGui.Px); GUI.DrawTexture(new Rect(r.x, r.yMax - th, r.width, th), MolakGui.Px);
        GUI.DrawTexture(new Rect(r.x, r.y, th, r.height), MolakGui.Px); GUI.DrawTexture(new Rect(r.xMax - th, r.y, th, r.height), MolakGui.Px);
    }

    void DrawTell (Rect band, float u)
    {
        float t = tellT, a = Mathf.Clamp01(t / 0.18f);
        var g0 = GUI.color;
        var c = band.center;
        if (tell == 0)
        {   // 絶: 처음 보는 필살 — 띠 전체가 어두워지고, 칼빛 세 줄이 어디서 올지 모르게 번쩍인다
            GUI.color = new Color(0.03f, 0.02f, 0.02f, 0.45f * a); GUI.DrawTexture(band, MolakGui.Px); GUI.color = g0;
            for (int i = 0; i < 3; i++)
            {
                float ph = (t * 1.3f + i * 0.33f) % 1f;
                float fl = ph < 0.18f ? 1f - ph / 0.18f : 0f;
                if (fl <= 0f) continue;
                float y0 = band.y + band.height * (0.2f + 0.3f * ((i * 0.37f + Mathf.Floor(t * 1.3f + i * 0.33f) * 0.61f) % 1f));
                Line(new Vector2(band.x + band.width * 0.08f, y0), new Vector2(band.xMax - band.width * 0.08f, y0 + band.height * 0.35f), Mathf.Max(2f, 5f * u), new Color(0.95f, 0.9f, 0.85f, 0.75f * fl * a));
            }
        }
        else if (tell == 1)
        {   // 攻: 띠 가장자리에서 가운데로 모이는 붉은 속도선
            float ph = (t * 1.6f) % 1f;
            for (int i = 0; i < 14; i++)
            {
                float ang = i * Mathf.PI * 2f / 14f + 0.2f;
                var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * 0.42f);
                float r0 = band.width * (0.62f - 0.18f * ((ph + i * 0.07f) % 1f)), r1 = r0 - band.width * 0.16f;
                Line(c + dir * r0, c + dir * r1, Mathf.Max(2f, 5f * u), new Color(0.62f, 0.08f, 0.05f, 0.55f * a));
            }
        }
        else if (tell == 2)
        {   // 守: 인물 앞을 가로막는 칼날 한 줄
            float h = band.height * 0.86f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.25f));
            var top = new Vector2(c.x + band.width * 0.02f, c.y - h * 0.5f);
            Line(top, top + new Vector2(0f, h), Mathf.Max(3f, 10f * u), new Color(0.12f, 0.1f, 0.09f, 0.75f * a));
            Line(top + new Vector2(-6f * u, 0f), top + new Vector2(-6f * u, h), Mathf.Max(1f, 3f * u), new Color(0.95f, 0.92f, 0.86f, 0.6f * a));
        }
        else if (tell == 3)
        {   // 虛: 인물 잔상 둘이 좌우로 흔들린다
            var tex = MolakArt.CharTexture;
            if (tex != null)
            {
                float hh = band.height * 1.75f, ww = hh * tex.width / tex.height;
                for (int s = -1; s <= 1; s += 2)
                {
                    float off = s * band.width * (0.12f + 0.05f * Mathf.Sin(t * 7f + s));
                    GUI.color = new Color(1f, 1f, 1f, 0.28f * a);
                    GUI.DrawTexture(new Rect(band.x + band.width * 0.5f - ww * 0.5f + off, band.y + band.height - hh + 16f * u, ww, hh), tex);
                }
                GUI.color = g0;
            }
        }
        // 기세의 인장(왼쪽 위)
        var sr = new Rect(band.x + 34f * u, band.y + 16f * u, 110f * u, 110f * u);
        GUI.color = new Color(1f, 1f, 1f, a);
        var box = tell == 0 ? new Color(0.08f, 0.06f, 0.05f, 0.92f) : new Color(0.55f, 0.1f, 0.07f, 0.88f);
        var gc = GUI.color; GUI.color = box * gc; GUI.DrawTexture(sr, MolakGui.Px); GUI.color = gc;
        MolakGui.Ink(sSeal, new Color(0.98f, 0.94f, 0.86f, 1f));
        GUI.Label(sr, TellHanja[tell], sSeal);
        var lr = new Rect(sr.x - 20f * u, sr.yMax + 4f * u, sr.width + 40f * u, 34f * u);
        GUI.color = new Color(0.05f, 0.04f, 0.035f, 0.62f * a); GUI.DrawTexture(lr, MolakGui.Px);
        GUI.color = new Color(1f, 1f, 1f, a);
        MolakGui.Ink(sSealSub, new Color(0.96f, 0.92f, 0.84f, 1f));
        GUI.Label(lr, TellName[tell], sSealSub);
        GUI.color = g0;
    }

    void DrawStrike (Rect band, float u)
    {
        if (act == 0) return;   // 「관」: 칼을 섞지 않았다
        float t = strikeT;
        float draw = Mathf.Clamp01(t / 0.16f), fade = t < 0.45f ? 1f : Mathf.Clamp01(1f - (t - 0.45f) / 0.45f);
        Color ink = res > 0 ? new Color(0.98f, 0.96f, 0.9f, 0.95f * fade) : res < 0 ? new Color(0.62f, 0.07f, 0.05f, 0.9f * fade) : new Color(0.2f, 0.17f, 0.15f, 0.8f * fade);
        Color edge = new Color(0.08f, 0.06f, 0.05f, 0.7f * fade);
        float th = Mathf.Max(4f, 16f * u);
        var c = band.center;
        if (act == 1)
        {   // 정면: 오른쪽 위에서 왼쪽 아래로 한 획
            var a = new Vector2(band.xMax - band.width * 0.18f, band.y + band.height * 0.08f);
            var b = new Vector2(band.x + band.width * 0.2f, band.yMax - band.height * 0.06f);
            var e = Vector2.Lerp(a, b, draw);
            Line(a, e, th * 1.4f, edge); Line(a, e, th, ink);
        }
        else if (act == 2)
        {   // 받아치기: 아래에서 위로 휘는 획 + 맞닿는 자리의 불꽃
            int seg = 10; Vector2 prev = Vector2.zero;
            for (int i = 0; i <= seg; i++)
            {
                float k = i / (float)seg; if (k > draw) break;
                float ang = Mathf.Lerp(200f, 340f, k) * Mathf.Deg2Rad;
                var p = c + new Vector2(Mathf.Cos(ang) * band.width * 0.3f, -Mathf.Sin(ang) * band.height * 0.42f + band.height * 0.1f);
                if (i > 0) { Line(prev, p, th * 1.3f, edge); Line(prev, p, th * 0.9f, ink); }
                prev = p;
            }
            if (draw >= 1f && t < 0.5f)
                for (int i = 0; i < 9; i++)
                {
                    float ang = i * 40f * Mathf.Deg2Rad + t * 3f; float r = band.height * (0.08f + 0.5f * t);
                    var p0 = c + new Vector2(0f, -band.height * 0.1f);
                    Line(p0 + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r * 0.5f, p0 + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r, Mathf.Max(2f, 4f * u), new Color(1f, 0.92f, 0.7f, 0.9f * fade));
                }
        }
        else
        {   // 흔들기: 꺾인 세 획
            var pts = new[] {
                new Vector2(band.x + band.width * 0.22f, band.y + band.height * 0.25f), new Vector2(band.x + band.width * 0.45f, band.y + band.height * 0.7f),
                new Vector2(band.x + band.width * 0.6f, band.y + band.height * 0.3f), new Vector2(band.x + band.width * 0.8f, band.y + band.height * 0.78f) };
            float total = draw * 3f;
            for (int i = 0; i < 3; i++)
            {
                float k = Mathf.Clamp01(total - i); if (k <= 0f) break;
                var e = Vector2.Lerp(pts[i], pts[i + 1], k);
                Line(pts[i], e, th * 1.2f, edge); Line(pts[i], e, th * 0.8f, ink);
            }
        }
    }

    Vector2 guideScroll;
    void DrawGuide (float u)
    {
        GUI.depth = MolakGui.DepthBlackout + 10;
        guideT += Time.unscaledDeltaTime;
        float w = Screen.width, h = Screen.height, a = Mathf.Clamp01(guideT / 0.2f);
        var g0 = GUI.color; GUI.color = new Color(1f, 1f, 1f, a);
        MolakInputGuard.Guard(new Rect(0, 0, w, h));
        GUI.color = new Color(0.04f, 0.035f, 0.03f, 0.95f * a); GUI.DrawTexture(new Rect(0, 0, w, h), MolakGui.Px); GUI.color = new Color(1f, 1f, 1f, a);
        float cw = Mathf.Min(w * 0.9f, 900f * u), cx = (w - cw) / 2f, y = Mathf.Max(60f * u, h * 0.08f);
        var ink = new Color(0.93f, 0.89f, 0.81f, 1f); var gold = new Color(0.85f, 0.68f, 0.3f, 1f); var red = new Color(0.8f, 0.22f, 0.16f, 1f);
        MolakGui.Ink(sHead, gold); GUI.Label(new Rect(cx, y, cw, 70f * u), "비무의 법", sHead); y += 90f * u;
        MolakGui.Ink(sHint, ink);
        GUI.Label(new Rect(cx, y, cw, 120f * u), "<b>간파(看破)</b> — 판이 열리면 상대가 낼 세 수가 엎어진 패로 놓인다. 몇 장은 앞면이 보인다.\n내 응수 세 장을 순서대로 깔면 한 합씩 맞부딪친다. 셋째 합은 <b>결(決)</b>, 두 배로 친다.", sHint);
        y += 130f * u;
        string[,] rows = { { "攻", "찌르러 온다", "受", "받아친다" }, { "守", "굳게 받는다", "變", "칼끝을 흔든다" }, { "虛", "허초로 흔든다", "斬", "정면으로 벤다" } };
        float rh = 104f * u;
        for (int i = 0; i < 3; i++)
        {
            var row = new Rect(cx, y, cw, rh - 12f * u);
            GUI.color = new Color(1f, 1f, 1f, 0.07f * a); GUI.DrawTexture(row, MolakGui.Px); GUI.color = new Color(1f, 1f, 1f, a);
            float sq = rh - 36f * u;
            var s1 = new Rect(row.x + 18f * u, row.y + 12f * u, sq, sq);
            GUI.color = new Color(0.55f, 0.1f, 0.07f, 0.9f * a); GUI.DrawTexture(s1, MolakGui.Px); GUI.color = new Color(1f, 1f, 1f, a);
            MolakGui.Ink(sRowH, ink); GUI.Label(s1, rows[i, 0], sRowH);
            MolakGui.Ink(sRow, ink); GUI.Label(new Rect(s1.xMax + 16f * u, row.y, cw * 0.32f, row.height), "상대가\n" + rows[i, 1], sRow);
            MolakGui.Ink(sRow, gold); GUI.Label(new Rect(cx + cw * 0.47f, row.y, 60f * u, row.height), "→", sRow);
            var s2 = new Rect(cx + cw * 0.56f, row.y + 12f * u, sq, sq);
            GUI.color = new Color(0.16f, 0.13f, 0.11f, 1f * a); GUI.DrawTexture(s2, MolakGui.Px); GUI.color = new Color(1f, 1f, 1f, a);
            MolakGui.Ink(sRowH, gold); GUI.Label(s2, rows[i, 2], sRowH);
            MolakGui.Ink(sRow, ink); GUI.Label(new Rect(s2.xMax + 16f * u, row.y, cw - (s2.xMax - cx) - 20f * u, row.height), rows[i, 3], sRow);
            y += rh;
        }
        y += 14f * u;
        MolakGui.Ink(sHint, ink);
        GUI.Label(new Rect(cx, y, cw, 200f * u),
            "맞수를 깔면 <color=#D9B55A>기세 +1</color>, 상대가 노리던 수를 깔면 <color=#CC5544>기세 −1 · 체력 −1</color>.\n" +
            "기세가 <color=#D9B55A>+2 이상</color>이면 마지막 한 칼은 반드시 이기고, <color=#CC5544>−2 이하</color>면 반드시 진다. 그 사이일 때만 승산을 굴린다.\n" +
            "검술이 높을수록 앞면이 많이 보이고, 지난 생에 맞붙어 본 상대는 세 수가 다 보인다. 察(살핌) 표시는 내가 앞 칸에 깐 수를 보고 정하는 수다.", sHint);
        y += 206f * u;
        GUI.Label(new Rect(cx, y, cw, 200f * u),
            "<color=#CC5544>필살 초식(絶)</color> — 이름난 칼잡이는 한 판에 한 번 제 초식을 낸다. 처음 받을 땐 읽히지 않고, 얻고 잃는 것이 두 배다. 한 번 받아 보면 다음 생부터 보인다.\n" +
            "<color=#B08CD8>청운검결</color> — 죽음을 겪으며 몸에서 돌아오는 사형들의 수. 운보·관·연·낙, 넷이다.", sHint);
        y += 206f * u;
        var br = new Rect(cx + cw * 0.25f, Mathf.Min(y, h - 140f * u), cw * 0.5f, 96f * u);
        GUI.color = new Color(0.45f, 0.13f, 0.1f, a); GUI.DrawTexture(br, MolakGui.Px); GUI.color = new Color(1f, 1f, 1f, a);
        MolakGui.Ink(sBtn, ink); GUI.Label(br, "칼을 뽑는다", sBtn);
        bool close = GUI.Button(br, GUIContent.none, GUIStyle.none);
        GUI.Button(new Rect(0, 0, w, h), GUIContent.none, GUIStyle.none);   // 뒤로 새지 않게
        GUI.color = g0;
        if (close && guideT > 0.4f)
        {
            guideOpen = false; MolakSfx.Draw();
            try { PlayerPrefs.SetInt(GuidePref, 1); PlayerPrefs.Save(); } catch { }
            MolakInputGuard.MuteFor(0.25f);
        }
    }
}
