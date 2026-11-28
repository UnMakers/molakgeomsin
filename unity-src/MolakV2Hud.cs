using System.Collections.Generic;
using MolakVn;
using UnityEngine;

/// <summary>
/// 새 이야기(V2) 수치 줄. 대본이 v2_on=1 로 켜면 극장판 위 띠에 몸·기억·동전, 오른쪽에 흑호방 빚과 그믐까지 남은 날을 띄운다.
/// 규칙 하나: 회귀자는 기억만 가지고 돌아온다 — 죽으면 몸·돈·정은 처음으로, 기억(v2_gae)만 남는다. 식구 정은 대본 안 갈래에만 쓴다.
/// 값이 바뀌면 그림 창 왼쪽 위에 「깨침 +5」 같은 쪽지가 떠올랐다 사라진다.
/// 변수: v2_hp v2_hpmax v2_gae(기억) v2_coin(닢) v2_debt(냥) v2_dday(그믐까지, -1이면 숨김). 정(v2_aff_*)·혈기(v2_hyeol)는 화면에 띄우지 않는다.
/// </summary>
public class MolakV2Hud : MonoBehaviour
{
    static MolakV2Hud inst;
    public static bool On { get { return Engine.Initialized && MolakStats.Get("v2_on") >= 1f; } }

    /// <summary>깨침이 이 값에 닿으면 그 식을 깨친다.</summary>
    public static readonly float[] FormAt = { 5f, 30f, 60f };
    public static readonly string[] FormName = { "물러섬", "받아넘김", "이어받음" };
    public static string Form (float gae)
    {
        string f = "";
        for (int i = 0; i < FormAt.Length; i++) if (gae >= FormAt[i]) f = FormName[i];
        return f;
    }

    static readonly string[] watch = { "v2_hp", "v2_hpmax", "v2_gae", "v2_coin", "v2_debt" };
    static readonly string[] watchName = { "몸", "몸 한도", "기억", "동전", "빚" };
    readonly Dictionary<string, float> last = new Dictionary<string, float>();
    bool primed;
    struct Pop { public string text; public Color col; public float t; }
    readonly List<Pop> pops = new List<Pop>();

    GUIStyle sLab, sNum, sPop; float builtU = -1f;
    static Texture2D px;
    static readonly Color Gold = new Color(0.83f, 0.71f, 0.42f, 1f);
    static readonly Color Paper = new Color(0.93f, 0.89f, 0.83f, 1f);
    static readonly Color Dim = new Color(0.66f, 0.60f, 0.53f, 1f);
    static readonly Color Blood = new Color(0.86f, 0.30f, 0.26f, 1f);
    static readonly Color Hyeol = new Color(0.78f, 0.36f, 0.58f, 1f);
    static readonly Color Jade = new Color(0.55f, 0.78f, 0.72f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap ()
    {
        if (inst != null) return;
        var go = new GameObject("MolakV2Hud");
        DontDestroyOnLoad(go);
        inst = go.AddComponent<MolakV2Hud>();
    }

    void Update ()
    {
        if (!On) { primed = false; pops.Clear(); return; }
        {   // 몸은 0 과 한도 사이, 정은 0~5, 혈기·동전은 0 아래로 안 간다
            float hm = Mathf.Max(1f, MolakStats.Get("v2_hpmax")), h = MolakStats.Get("v2_hp");
            if (h < 0f || h > hm) MolakStats.Set("v2_hp", Mathf.Clamp(h, 0f, hm));
            foreach (var k in new[] { "v2_coin", "v2_debt" }) if (MolakStats.Get(k) < 0f) MolakStats.Set(k, 0f);
            foreach (var k in new[] { "v2_aff_jinu", "v2_aff_gwak", "v2_aff_yeoni", "v2_aff_seok" }) { float a = MolakStats.Get(k); if (a < 0f || a > 5f) MolakStats.Set(k, Mathf.Clamp(a, 0f, 5f)); }
        }
        for (int i = 0; i < watch.Length; i++)
        {
            float v = MolakStats.Get(watch[i]);
            float o;
            if (primed && last.TryGetValue(watch[i], out o) && !Mathf.Approximately(o, v)) Popup(i, o, v);
            last[watch[i]] = v;
        }
        primed = true;
        for (int i = pops.Count - 1; i >= 0; i--)
        {
            var p = pops[i]; p.t += Time.unscaledDeltaTime; pops[i] = p;
            if (p.t > 2.4f) pops.RemoveAt(i);
        }
    }

    void Popup (int i, float o, float v)
    {
        string k = watch[i];
        if (k == "v2_hpmax" && Mathf.Approximately(MolakStats.Get("v2_hp"), v) && v > o) { Add("몸이 한 칸 두터워졌다", Jade); return; }
        float d = v - o;
        string sign = d > 0 ? "+" : "−";
        Color c = k == "v2_hp" ? (d < 0 ? Blood : Jade) : k == "v2_gae" ? Gold : k == "v2_debt" ? (d > 0 ? Blood : Jade) : Paper;
        Add(watchName[i] + " " + sign + Mathf.Abs(d).ToString("0"), c);
        if (k == "v2_gae")
            for (int f = 0; f < FormAt.Length; f++)
                if (o < FormAt[f] && v >= FormAt[f]) Add("청운검법 「" + FormName[f] + "」을 몸이 기억한다", Gold);
    }

    void Add (string text, Color col)
    {
        pops.Add(new Pop { text = text, col = col, t = -0.15f * pops.Count });
        if (pops.Count > 5) pops.RemoveAt(0);
    }

    GUIStyle sPin, sChip, sSeal; 
    void Styles (float u)
    {
        if (sLab != null && Mathf.Approximately(builtU, u)) return;
        builtU = u;
        var f = MolakGui.Gowun;
        sLab = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(25 * u), alignment = TextAnchor.MiddleLeft, richText = true, wordWrap = false, clipping = TextClipping.Overflow };
        if (f != null) sLab.font = f;
        sNum = new GUIStyle(sLab) { fontSize = Mathf.RoundToInt(34 * u) };
        sPop = new GUIStyle(sLab) { fontSize = Mathf.RoundToInt(27 * u) };
        sPin = new GUIStyle(sLab) { fontSize = Mathf.RoundToInt(29 * u) }; sPin.font = MolakTypography.Heading(f);
        sChip = new GUIStyle(sLab) { fontSize = Mathf.RoundToInt(22 * u), alignment = TextAnchor.MiddleCenter };
        sSeal = new GUIStyle(sChip) { fontSize = Mathf.RoundToInt(23 * u) };
        MolakGui.Ink(sLab, Dim); MolakGui.Ink(sNum, Paper); MolakGui.Ink(sPop, Paper); MolakGui.Ink(sPin, Gold); MolakGui.Ink(sChip, Gold); MolakGui.Ink(sSeal, Paper);
    }

    static void Box (Rect r, Color c)
    {
        if (px == null) { px = new Texture2D(1, 1); px.SetPixel(0, 0, Color.white); px.Apply(); }
        var g = GUI.color; GUI.color = c; GUI.DrawTexture(r, px); GUI.color = g;
    }
    static void Frame (Rect r, Color c, float t)
    {
        Box(new Rect(r.x, r.y, r.width, t), c); Box(new Rect(r.x, r.yMax - t, r.width, t), c);
        Box(new Rect(r.x, r.y, t, r.height), c); Box(new Rect(r.xMax - t, r.y, t, r.height), c);
    }

    static string Hex (Color c) { return "#" + ColorUtility.ToHtmlStringRGB(c); }
    static Texture2D grad;
    /// <summary>위 띠 바탕: 위는 옻칠 먹색, 아래로 갈수록 아주 옅게 붉은 기운이 도는 세로 그라데이션.</summary>
    static Texture2D Grad
    {
        get
        {
            if (grad != null) return grad;
            grad = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < 64; y++) { float t = y / 63f; grad.SetPixel(0, y, Color.Lerp(new Color(0.11f, 0.07f, 0.06f, 1f), new Color(0.05f, 0.045f, 0.04f, 1f), t)); }
            grad.Apply(); return grad;
        }
    }

    void OnGUI ()
    {
        if (!On || MolakMenu.IsOpen || MolakBlackout.Busy) return;
        GUI.depth = 9;
        float u = MolakGui.U, w = Screen.width, s = MolakLayout.CanvasPx;
        Styles(u);
        float W = Mathf.Min(w, 1080 * u), X0 = (w - W) / 2f, x = X0 + 44 * u;
        float top = MolakLayout.ThInsetTop * s;
        bool thr = MolakTheme.TheaterNow;
        float bandB = thr ? MolakLayout.ThTopBand * s : top + 136 * s;
        float bandBottom = thr ? MolakLayout.ThArtTop * s : bandB;
        // 바탕 · 금줄 두 겹(옛 극장판 결)
        GUI.DrawTexture(new Rect(0, 0, w, bandBottom), Grad);
        var goldA = new Color(Gold.r, Gold.g, Gold.b, 0.75f);
        Box(new Rect(0, bandB, w, Mathf.Max(1f, 2f * s)), goldA);
        Box(new Rect(0, bandB + 5f * s, w, Mathf.Max(1f, 1f * s)), new Color(Gold.r, Gold.g, Gold.b, 0.28f));

        float hp = MolakStats.Get("v2_hp"), hpm = Mathf.Max(1f, MolakStats.Get("v2_hpmax"));
        float gae = MolakStats.Get("v2_gae"), coin = MolakStats.Get("v2_coin"), debt = MolakStats.Get("v2_debt"), dday = MolakStats.Get("v2_dday");
        Color cHp = Hex2(MolakHud.StatCol.Hp[1]), cMem = Hex2(MolakHud.StatCol.Sw[1]), cCoin = Hex2(MolakHud.StatCol.Realm[1]);

        // 윗줄: 마디 이름(금) · 오른쪽에 빚 도장
        float l1 = top + (bandB - top) * 0.30f, l2 = top + (bandB - top) * 0.70f, rowH = 48 * u;
        string pin = MolakVfx.PinText;
        if (!string.IsNullOrEmpty(pin)) GUI.Label(new Rect(x, l1 - rowH * 0.5f, W * 0.55f, rowH), pin, sPin);
        if (debt > 0f)
        {
            string a1 = "흑호방 빚 " + debt.ToString("0") + "냥";
            string a2 = dday > 0f ? "그믐 D-" + dday.ToString("0") : Mathf.Approximately(dday, 0f) ? "오늘 그믐" : "";
            float rx = X0 + W - 40 * u;
            if (a2.Length > 0)
            {
                float w2 = sSeal.CalcSize(new GUIContent(a2)).x + 24 * u; var r2 = new Rect(rx - w2, l1 - 20 * u, w2, 40 * u);
                bool urgent = dday <= 2f;
                float pul = urgent ? 0.75f + 0.25f * Mathf.Sin(Time.time * 3f) : 1f;
                Box(r2, new Color(0.55f, 0.12f, 0.10f, (urgent ? 0.95f : 0.7f) * pul));
                Frame(r2, new Color(0.85f, 0.35f, 0.28f, 0.9f), Mathf.Max(1f, 1.5f * u));
                GUI.Label(r2, a2, sSeal); rx = r2.x - 12 * u;
            }
            float w1 = sChip.CalcSize(new GUIContent(a1)).x + 22 * u; var r1 = new Rect(rx - w1, l1 - 20 * u, w1, 40 * u);
            Frame(r1, new Color(0.85f, 0.35f, 0.28f, 0.7f), Mathf.Max(1f, 1.5f * u));
            MolakGui.Ink(sChip, new Color(0.93f, 0.62f, 0.55f, 1f)); GUI.Label(r1, a1, sChip); MolakGui.Ink(sChip, Gold);
        }

        // 아랫줄: 몸(피통) · 기억(식) · 동전
        float cx = x, ry = l2 - rowH * 0.5f, gap = 46 * u;
        bool low = hp / hpm <= 0.34f;
        string form = Form(gae);
        // 몸
        cx += Item(cx, ry, rowH, "몸", cHp, "<color=" + Hex(low ? Blood : Paper) + ">" + hp.ToString("0") + "</color><size=" + Mathf.RoundToInt(22 * u) + "><color=" + Hex(Dim) + ">/" + hpm.ToString("0") + "</color></size>", out float hpX0, out float hpX1);
        {
            float bw = Mathf.Max(hpX1 - hpX0, 120 * u), by = l2 + rowH * 0.5f - 6 * u, bh = 12 * u;
            var g = GUI.color; GUI.color = new Color(Paper.r, Paper.g, Paper.b, 0.10f); GUI.DrawTexture(new Rect(hpX0, by, bw, bh), MolakHud.HpGlow);
            float pul = low ? 0.65f + 0.35f * Mathf.Sin(Time.time * 4f) : 1f;
            GUI.color = low ? new Color(1f, 0.55f, 0.5f, pul) : Color.white;
            float r = Mathf.Clamp01(hp / hpm); if (r > 0f) GUI.DrawTexture(new Rect(hpX0, by, bw * r, bh), MolakHud.HpGlow);
            GUI.color = g;
        }
        cx += gap;
        // 기억
        string memN = "<color=" + Hex(cMem) + "><b>" + gae.ToString("0") + "</b></color>";
        cx += Item(cx, ry, rowH, "기억", cMem, memN, out float m0, out float m1);
        if (form.Length > 0)
        {
            string ft = "「" + form + "」"; float fw = sChip.CalcSize(new GUIContent(ft)).x + 14 * u; var fr = new Rect(cx + 8 * u, ry + 6 * u, fw, rowH - 12 * u);
            Frame(fr, new Color(cMem.r, cMem.g, cMem.b, 0.55f), Mathf.Max(1f, 1f * u));
            MolakGui.Ink(sChip, cMem); GUI.Label(fr, ft, sChip); MolakGui.Ink(sChip, Gold); cx = fr.xMax;
        }
        cx += gap;
        // 동전
        Item(cx, ry, rowH, "동전", cCoin, "<color=" + Hex(Paper) + ">" + coin.ToString("0") + "</color><size=" + Mathf.RoundToInt(22 * u) + "><color=" + Hex(Dim) + "> 닢</color></size>", out float c0, out float c1);

        // 쪽지: 그림 창 왼쪽 위에서 떠올라 흐려진다
        float py = bandB + 24 * u;
        for (int i = 0; i < pops.Count; i++)
        {
            var p = pops[i]; if (p.t < 0f) continue;
            float a = p.t < 0.25f ? p.t / 0.25f : p.t > 1.8f ? Mathf.Clamp01(1f - (p.t - 1.8f) / 0.6f) : 1f;
            float rise = Mathf.Min(p.t, 0.4f) * 30f * u;
            var sz = sPop.CalcSize(new GUIContent(p.text));
            var r = new Rect(x - 12 * u, py + i * 54 * u - rise + 12 * u, sz.x + 28 * u, 48 * u);
            Box(r, new Color(0.067f, 0.059f, 0.055f, 0.82f * a));
            Box(new Rect(r.x, r.y, Mathf.Max(2f, 3f * u), r.height), new Color(p.col.r, p.col.g, p.col.b, 0.9f * a));
            Box(new Rect(r.x, r.yMax - Mathf.Max(1f, u), r.width, Mathf.Max(1f, u)), new Color(Gold.r, Gold.g, Gold.b, 0.35f * a));
            MolakGui.Ink(sPop, new Color(p.col.r, p.col.g, p.col.b, a));
            GUI.Label(new Rect(r.x + 14 * u, r.y, sz.x + 4 * u, r.height), p.text, sPop);
        }
    }


    /// <summary>이름(작게, 제 빛) + 숫자(크게). 그린 폭을 돌려준다.</summary>
    float Item (float x, float y, float h, string label, Color c, string num, out float x0, out float x1)
    {
        float u = MolakGui.U;
        MolakGui.Ink(sLab, c);
        float lw = sLab.CalcSize(new GUIContent(label)).x;
        GUI.Label(new Rect(x, y + 3 * u, lw + 4 * u, h), label, sLab);
        MolakGui.Ink(sLab, Dim);
        float nx = x + lw + 10 * u, nw = sNum.CalcSize(new GUIContent(num)).x;
        GUI.Label(new Rect(nx, y, nw + 4 * u, h), num, sNum);
        x0 = x; x1 = nx + nw;
        return lw + 10 * u + nw;
    }
    static Color Hex2 (string h) { Color c; return ColorUtility.TryParseHtmlString(h, out c) ? c : Color.white; }
}
