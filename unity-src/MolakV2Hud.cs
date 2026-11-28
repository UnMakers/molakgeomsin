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

    void Styles (float u)
    {
        if (sLab != null && Mathf.Approximately(builtU, u)) return;
        builtU = u;
        var f = MolakGui.Gowun;
        sLab = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(26 * u), alignment = TextAnchor.MiddleLeft, richText = true, wordWrap = false, clipping = TextClipping.Overflow };
        if (f != null) sLab.font = f;
        sNum = new GUIStyle(sLab) { fontSize = Mathf.RoundToInt(32 * u) };
        sPop = new GUIStyle(sLab) { fontSize = Mathf.RoundToInt(28 * u) };
        MolakGui.Ink(sLab, Dim); MolakGui.Ink(sNum, Paper); MolakGui.Ink(sPop, Paper);
    }

    static void Box (Rect r, Color c)
    {
        if (px == null) { px = new Texture2D(1, 1); px.SetPixel(0, 0, Color.white); px.Apply(); }
        var g = GUI.color; GUI.color = c; GUI.DrawTexture(r, px); GUI.color = g;
    }

    static string Hex (Color c) { return "#" + ColorUtility.ToHtmlStringRGB(c); }

    void OnGUI ()
    {
        if (!On || MolakMenu.IsOpen || MolakBlackout.Busy) return;
        GUI.depth = 9;
        float u = MolakGui.U, w = Screen.width, s = MolakLayout.CanvasPx;
        Styles(u);
        float W = Mathf.Min(w, 1080 * u), X0 = (w - W) / 2f, x = X0 + 40 * u;
        float top = MolakLayout.ThInsetTop * s;
        float bandB = MolakTheme.TheaterNow ? MolakLayout.ThTopBand * s : top + 136 * s;
        if (MolakTheme.TheaterNow)
        {
            Box(new Rect(0, 0, w, MolakLayout.ThArtTop * s), new Color(0.067f, 0.059f, 0.055f, 1f));
            Box(new Rect(0, bandB, w, Mathf.Max(1f, 2f * s)), new Color(Gold.r, Gold.g, Gold.b, 0.7f));
        }
        else Box(new Rect(0, 0, w, bandB), new Color(0.067f, 0.059f, 0.055f, 0.88f));

        float hp = MolakStats.Get("v2_hp"), hpm = Mathf.Max(1f, MolakStats.Get("v2_hpmax"));
        float gae = MolakStats.Get("v2_gae"), coin = MolakStats.Get("v2_coin"), debt = MolakStats.Get("v2_debt"), dday = MolakStats.Get("v2_dday");

        // 한 줄: 몸 · 기억 · 동전 (위 띠 가운데)
        float rowH = 50 * u, l2 = top + (bandB - top) * 0.5f - 4 * u;
        float rx = x, ry = l2 - rowH * 0.5f, gap = 40 * u;
        bool low = hp / hpm <= 0.34f;
        string form = Form(gae);
        string[] labs = { "몸", "기억", "동전" };
        string[] nums = {
            "<color=" + Hex(low ? Blood : Paper) + ">" + hp.ToString("0") + "</color><size=" + Mathf.RoundToInt(22 * u) + "><color=" + Hex(Dim) + ">/" + hpm.ToString("0") + "</color></size>",
            "<color=" + Hex(Gold) + ">" + gae.ToString("0") + "</color>" + (form.Length > 0 ? "<size=" + Mathf.RoundToInt(22 * u) + "><color=" + Hex(Gold) + "> 「" + form + "」</color></size>" : ""),
            "<color=" + Hex(Paper) + ">" + coin.ToString("0") + "</color><size=" + Mathf.RoundToInt(22 * u) + "><color=" + Hex(Dim) + "> 닢</color></size>" };
        float hpEnd = rx;
        for (int i = 0; i < labs.Length; i++)
        {
            float lw = sLab.CalcSize(new GUIContent(labs[i])).x;
            GUI.Label(new Rect(rx, ry, lw + 4 * u, rowH), labs[i], sLab); rx += lw + 10 * u;
            float nw = sNum.CalcSize(new GUIContent(nums[i])).x;
            GUI.Label(new Rect(rx, ry + 1 * u, nw + 4 * u, rowH), nums[i], sNum); rx += nw + gap;
            if (i == 0) hpEnd = rx - gap;
        }
        // 몸 막대: 몸 글자 아래
        float bw = hpEnd - x, by = l2 + rowH * 0.5f - 2 * u, bh = 6 * u;
        Box(new Rect(x, by, bw, bh), new Color(Paper.r, Paper.g, Paper.b, 0.12f));
        float pul = low ? 0.65f + 0.35f * Mathf.Sin(Time.time * 4f) : 1f;
        Box(new Rect(x, by, bw * Mathf.Clamp01(hp / hpm), bh), new Color(Blood.r, Blood.g, Blood.b, 0.9f * pul));
        // 빚 · 그믐: 줄 오른쪽 끝
        if (debt > 0f)
        {
            string dd = dday > 0f ? "  ·  그믐까지 " + dday.ToString("0") + "일" : Mathf.Approximately(dday, 0f) ? "  ·  오늘 그믐" : "";
            string ds = "<color=" + Hex(Dim) + ">흑호방 빚 </color><color=" + Hex(Blood) + ">" + debt.ToString("0") + "냥</color><color=" + Hex(dday >= 0f && dday <= 2f ? Blood : Dim) + ">" + dd + "</color>";
            float dw = sLab.CalcSize(new GUIContent(ds)).x;
            GUI.Label(new Rect(X0 + W - 40 * u - dw, ry, dw + 4 * u, rowH), ds, sLab);
        }

        // 쪽지: 그림 창 왼쪽 위에서 떠올라 흐려진다
        float py = bandB + 24 * u;
        for (int i = 0; i < pops.Count; i++)
        {
            var p = pops[i]; if (p.t < 0f) continue;
            float a = p.t < 0.25f ? p.t / 0.25f : p.t > 1.8f ? Mathf.Clamp01(1f - (p.t - 1.8f) / 0.6f) : 1f;
            float rise = Mathf.Min(p.t, 0.4f) * 30f * u;
            var sz = sPop.CalcSize(new GUIContent(p.text));
            var r = new Rect(x - 12 * u, py + i * 54 * u - rise + 12 * u, sz.x + 28 * u, 48 * u);
            Box(r, new Color(0.067f, 0.059f, 0.055f, 0.78f * a));
            MolakGui.Ink(sPop, new Color(p.col.r, p.col.g, p.col.b, a));
            GUI.Label(new Rect(r.x + 14 * u, r.y, sz.x + 4 * u, r.height), p.text, sPop);
        }
    }

}
