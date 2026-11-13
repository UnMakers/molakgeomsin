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
    const string GuidePref = "molak.duelguide";

    public static bool GuideOpen { get { return guideOpen; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot ()
    {
        if (inst) return;
        var go = new GameObject("MolakDuelStage"); DontDestroyOnLoad(go);
        inst = go.AddComponent<MolakDuelStage>();
    }

    public static void Tell (int shown) { tell = Mathf.Clamp(shown, 1, 3); tellT = 0f; strikeT = -1f; MolakArt.Pose(tell); }
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

    static readonly string[] TellHanja = { "", "攻", "守", "虛" };
    static readonly string[] TellName = { "", "찌르러 온다", "굳게 받는다", "허초로 흔든다" };

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
    }

    void DrawTell (Rect band, float u)
    {
        float t = tellT, a = Mathf.Clamp01(t / 0.18f);
        var g0 = GUI.color;
        var c = band.center;
        if (tell == 1)
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
        var box = new Color(0.55f, 0.1f, 0.07f, 0.88f);
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
        GUI.Label(new Rect(cx, y, cw, 120f * u), "마지막 한 칼 앞에 몇 번 칼을 섞는다. 이것을 <b>합(合)</b>이라 한다.\n합마다 상대의 기세가 보인다. 그 기세를 꺾는 수를 고른다.", sHint);
        y += 130f * u;
        string[,] rows = { { "攻", "찌르러 온다", "受", "받아친다" }, { "守", "굳게 받는다", "變", "칼끝을 흔든다" }, { "虛", "허초로 흔든다", "斬", "정면으로 벤다" } };
        float rh = 120f * u;
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
        GUI.Label(new Rect(cx, y, cw, 230f * u),
            "맞수를 고르면 <color=#D9B55A>기세 +1</color>, 상대가 노리던 수를 고르면 <color=#CC5544>기세 −1 · 체력 −1</color>.\n" +
            "쌓인 기세가 마지막 한 칼의 승산을 올린다(한 칸에 약 열에 하나).\n" +
            "기세는 가끔 거짓으로 보인다. 검술이 높을수록 잘 읽히고, 지난 생에 맞붙어 본 상대의 수는 <color=#B08CD8>보랏빛</color>으로 확실히 보인다.", sHint);
        y += 240f * u;
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
