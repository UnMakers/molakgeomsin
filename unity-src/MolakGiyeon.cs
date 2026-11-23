using System.Collections.Generic;
using MolakVn;
using UnityEngine;

/// <summary>
/// 0.4.66 기연(奇緣) — 크게 한 번 몸이 뛰는 순간(언메이커: "도파민 팍 도는 게 없다").
/// @giyeon name:"적운환(赤雲丸)" hanja:"丹" line:"소아가 기름종이째 던져 준 붉은 환약" ng:3 hpmax:3 sw:2 heal:true why:"소아의 적운환"
///  · 능력치를 이 명령이 직접 올리고(능력치 기록에 why 로 남는다), 한 장의 금빛 판으로 "전 → 후"를 숫자가 차오르듯 보여 준다.
///  · 금빛 살(光)이 돌고 불티가 튀고, 가운데에 한 글자 인장이 찍힌다. 종 두 번.
///  · '빨리'·넘기기 중에도 뜬다(보상은 놓치면 안 된다) — 그때는 2.6초 보이고 저절로 닫힌다. '자동'이면 4.2초.
/// </summary>
[Alias("giyeon")]
public class GiyeonCommand : Command, IMolakMoment
{
    public bool CoversText => true;   // 0.4.90 글자리를 덮는다(IMolakMoment)
    [RequiredParameter] public StringParameter Name;
    public StringParameter Hanja;
    public StringParameter Kicker;
    public StringParameter Line;
    public StringParameter Why;
    public IntegerParameter Hp, Hpmax, Ng, Sw, Sl, Mn, Curse;
    public BooleanParameter Heal;

    public override async Awaitable Execute (ExecutionContext ctx)
    {
        string why = Assigned(Why) ? Why.Value : "기연 — " + Name.Value;
        MolakStatLog.Why("*", why);
        var rows = new List<MolakGiyeonCard.Row>();
        void Bump (string key, string label, IntegerParameter p)
        {
            if (!Assigned(p) || p.Value == 0) return;
            float a = MolakStats.Get(key);
            MolakStats.Add(key, p.Value);
            MolakStats.ClampAll();
            rows.Add(new MolakGiyeonCard.Row { Label = label, From = a, To = MolakStats.Get(key), Bad = key == "curse" ? p.Value > 0 : p.Value < 0 });
        }
        if (Assigned(Hpmax) && Hpmax.Value != 0)
        {
            float a = MolakStats.Get("hpmax");
            MolakStats.Set("hpmax", a + Hpmax.Value);
            rows.Add(new MolakGiyeonCard.Row { Label = "최대 체력", From = a, To = a + Hpmax.Value, Bad = Hpmax.Value < 0 });
        }
        Bump("sw", "검술", Sw);
        Bump("ng", "내공", Ng);
        Bump("hp", "체력", Hp);
        Bump("mn", "은자", Mn);
        Bump("sl", "살기", Sl);
        Bump("curse", "저주", Curse);
        if (Assigned(Heal) && Heal.Value)
        {
            float a = MolakStats.Get("hp"), m = MolakStats.Get("hpmax");
            if (a < m) { MolakStats.Set("hp", m); rows.Add(new MolakGiyeonCard.Row { Label = "체력", From = a, To = m, Note = "다 차오른다" }); }
        }
        if (Assigned(Sw) || Assigned(Ng)) MolakStats.GrowBody();
        MolakStats.ClampAll();
        var sp = Engine.GetService<IScriptPlayer>();
        MolakGiyeonCard.AutoClose = sp != null && sp.Skipping ? 2.6f : (MolakPlayMode.Auto ? 4.2f : -1f);   // 넘기는 중이면 잠깐 보이고 저절로 닫힌다
        MolakGiyeonCard.Show(Assigned(Kicker) ? Kicker.Value : "기연(奇緣)", Name.Value, Assigned(Hanja) ? Hanja.Value : "緣", Assigned(Line) ? Line.Value : "", rows);
        while (MolakGiyeonCard.Showing) await Awaitable.NextFrameAsync();
    }
}

/// <summary>
/// 0.4.68 기연 판 — 종이 위의 먹 한 획(언메이커: "촌스럽다, 글씨가 잘 안 보인다"). 금빛 살·불티·흔들림을 걷어 내고,
/// 화면을 담백한 종이로 덮은 뒤 붓으로 그은 원(MolakEnso) 안에 한 글자를 먹으로 쓰고, 붉은 인장 하나를 찍는다.
/// 이름은 짙은 먹, 수치는 "전 → 후"를 한 줄씩 또렷하게(대비 높은 먹색), 차오르는 숫자는 짧게.
/// </summary>
public class MolakGiyeonCard : MonoBehaviour
{
    public struct Row { public string Label; public float From, To; public bool Bad; public string Note; }
    static MolakGiyeonCard inst;
    string kicker = "", title = "", hanja = "", line = ""; List<Row> rows = new List<Row>();
    float t = -1f, outAt = -1f; bool sealed_;
    public static bool Showing => inst != null && inst.t >= 0f;
    public static float AutoClose = -1f;
    static Texture2D enso;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot ()
    {
        if (inst) return;
        var go = new GameObject("MolakGiyeonCard"); DontDestroyOnLoad(go);
        inst = go.AddComponent<MolakGiyeonCard>();
    }

    public static void Show (string kick, string ttl, string hj, string ln, List<Row> rs)
    {
        if (inst == null) return;
        if (enso == null) enso = Resources.Load<Texture2D>("MolakEnso");
        inst.kicker = kick ?? ""; inst.title = ttl ?? ""; inst.hanja = hj ?? "緣"; inst.line = ln ?? ""; inst.rows = rs ?? new List<Row>();
        inst.t = 0f; inst.outAt = -1f; inst.sealed_ = false;
        MolakSfx.TrueBell(); MolakSfx.Vibrate();
    }

    void Update ()
    {
        if (t < 0f) return;
        t += Time.unscaledDeltaTime;
        if (!sealed_ && t > 0.95f) { sealed_ = true; MolakSfx.Stamp(); }
        if (AutoClose > 0f && t > AutoClose && outAt < 0f) outAt = t;
        if (outAt >= 0f && t - outAt > 0.4f) { t = -1f; outAt = -1f; MolakInputGuard.MuteFor(0.3f); }
    }

    GUIStyle sKick, sTitle, sHanja, sLine, sLab, sVal, sHint, sSeal; float builtU = -1f;
    void OnGUI ()
    {
        if (t < 0f) return;
        float u = MolakGui.U, w = Screen.width, h = Screen.height;
        if (sTitle == null || !Mathf.Approximately(builtU, u))
        {
            builtU = u;
            sKick = MolakGui.Label(26, u, TextAnchor.MiddleCenter, false);
            sTitle = MolakGui.Label(54, u, TextAnchor.MiddleCenter, true); sTitle.font = MolakTypography.Heading(sTitle.font); sTitle.richText = true;
            sHanja = MolakGui.Label(190, u, TextAnchor.MiddleCenter, false); if (MolakGui.Hanja != null) sHanja.font = MolakGui.Hanja;
            sSeal = MolakGui.Label(40, u, TextAnchor.MiddleCenter, false); if (MolakGui.Hanja != null) sSeal.font = MolakGui.Hanja;
            sLine = MolakGui.Label(27, u, TextAnchor.UpperCenter, true); sLine.richText = true;
            sLab = MolakGui.Label(32, u, TextAnchor.MiddleLeft, false);
            sVal = MolakGui.Label(32, u, TextAnchor.MiddleRight, false); sVal.richText = true;
            sHint = MolakGui.Label(22, u, TextAnchor.MiddleCenter, false);
        }
        GUI.depth = MolakGui.DepthBlackout + 7;
        MolakInputGuard.Guard(new Rect(0, 0, w, h));
        var g0 = GUI.color; var m0 = GUI.matrix;
        float a = Mathf.Clamp01(t / 0.35f);
        if (outAt >= 0f) a *= Mathf.Clamp01(1f - (t - outAt) / 0.4f);
        var ink = new Color(0.13f, 0.11f, 0.09f, 1f);
        var grey = new Color(0.42f, 0.37f, 0.31f, 1f);
        var red = new Color(0.66f, 0.13f, 0.09f, 1f);

        // 종이
        GUI.color = new Color(0.955f, 0.94f, 0.905f, a); GUI.DrawTexture(new Rect(0, 0, w, h), MolakGui.Px);
        GUI.color = new Color(0.62f, 0.5f, 0.32f, 0.12f * a); GUI.DrawTexture(new Rect(0, 0, w, 6f * u), MolakGui.Px); GUI.DrawTexture(new Rect(0, h - 6f * u, w, 6f * u), MolakGui.Px); GUI.color = Color.white;

        float cw = Mathf.Min(w * 0.86f, 820f * u), cx = (w - cw) / 2f;
        float d = Mathf.Min(w * 0.62f, 520f * u);
        var c = new Vector2(w / 2f, h * 0.31f);
        // 위: 奇緣 · 기연
        MolakGui.Ink(sKick, new Color(grey.r, grey.g, grey.b, a));
        GUI.Label(new Rect(cx, c.y - d * 0.5f - 70f * u, cw, 40f * u), kicker.Length > 0 && kicker != "기연(奇緣)" ? kicker : "奇 緣   ·   기 연", sKick);

        // 붓 원: 살짝 크게 번졌다가 자리 잡는다
        float ea = Mathf.Clamp01((t - 0.1f) / 0.55f);
        if (enso != null && ea > 0f)
        {
            float sc = Mathf.Lerp(1.06f, 1f, Mathf.SmoothStep(0f, 1f, ea));
            GUIUtility.ScaleAroundPivot(new Vector2(sc, sc), c);
            GUI.color = new Color(1f, 1f, 1f, ea * a);
            GUI.DrawTexture(new Rect(c.x - d / 2f, c.y - d / 2f, d, d), enso, ScaleMode.ScaleToFit, true);
            GUI.matrix = m0; GUI.color = Color.white;
        }
        // 한 글자
        float ha = Mathf.Clamp01((t - 0.4f) / 0.4f);
        if (ha > 0f)
        {
            MolakGui.Ink(sHanja, new Color(ink.r, ink.g, ink.b, ha * a));
            GUI.Label(new Rect(c.x - d / 2f, c.y - d / 2f, d, d), hanja, sHanja);
        }
        // 붉은 인장
        if (t > 0.95f)
        {
            float sa = Mathf.Clamp01((t - 0.95f) / 0.12f);
            float ss = Mathf.Lerp(1.35f, 1f, sa);
            var sp = new Vector2(c.x + d * 0.36f, c.y + d * 0.34f);
            GUIUtility.ScaleAroundPivot(new Vector2(ss, ss), sp);
            var sr = new Rect(sp.x - 34f * u, sp.y - 34f * u, 68f * u, 68f * u);
            GUI.color = new Color(red.r, red.g, red.b, 0.9f * sa * a); GUI.DrawTexture(sr, MolakGui.Px); GUI.color = Color.white;
            MolakGui.Ink(sSeal, new Color(0.98f, 0.94f, 0.86f, sa * a)); GUI.Label(sr, "緣", sSeal);
            GUI.matrix = m0;
        }

        float y = c.y + d * 0.5f + 34f * u;
        float ta = Mathf.Clamp01((t - 0.65f) / 0.35f);
        MolakGui.Ink(sTitle, new Color(ink.r, ink.g, ink.b, ta * a));
        float th = sTitle.CalcHeight(new GUIContent(title), cw);
        GUI.Label(new Rect(cx, y, cw, th), title, sTitle); y += th + 10f * u;
        if (line.Length > 0)
        {
            float lh = sLine.CalcHeight(new GUIContent(line), cw - 40f * u);
            MolakGui.Ink(sLine, new Color(grey.r, grey.g, grey.b, ta * a));
            GUI.Label(new Rect(cx + 20f * u, y, cw - 40f * u, lh), line, sLine); y += lh + 18f * u;
        }
        // 가는 줄
        GUI.color = new Color(0.5f, 0.4f, 0.28f, 0.35f * ta * a); GUI.DrawTexture(new Rect(cx + cw * 0.18f, y, cw * 0.64f, Mathf.Max(1f, 1.5f * u)), MolakGui.Px); GUI.color = Color.white; y += 22f * u;
        // 수치: 한 줄씩
        float colW = cw * 0.64f, colX = cx + cw * 0.18f;
        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            float k0 = 1.05f + 0.16f * i;
            float ra = Mathf.Clamp01((t - k0) / 0.25f);
            if (ra <= 0f) { y += 54f * u; continue; }
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - k0) / 0.5f));
            float cur = Mathf.Round(Mathf.Lerp(r.From, r.To, p));
            float dv = r.To - r.From;
            string acc = r.Bad ? "#8A2A1E" : "#2F6B3A";
            MolakGui.Ink(sLab, new Color(ink.r, ink.g, ink.b, ra * a));
            GUI.Label(new Rect(colX, y, colW, 50f * u), r.Label, sLab);
            string val = "<color=#7A6E60>" + r.From.ToString("0") + "</color>  →  <b><color=" + acc + ">" + cur.ToString("0") + "</color></b>"
                       + (string.IsNullOrEmpty(r.Note) ? "  <color=" + acc + ">(" + (dv > 0 ? "+" : "−") + Mathf.Abs(dv).ToString("0") + ")</color>" : "  <color=#7A6E60>" + r.Note + "</color>");
            MolakGui.Ink(sVal, new Color(1f, 1f, 1f, ra * a));
            GUI.Label(new Rect(colX, y, colW, 50f * u), val, sVal);
            y += 54f * u;
        }
        float hold = 1.3f + 0.16f * rows.Count;
        if (t > hold && outAt < 0f)
        {
            MolakGui.Ink(sHint, new Color(grey.r, grey.g, grey.b, Mathf.Clamp01((t - hold) / 0.4f) * a));
            GUI.Label(new Rect(0, Mathf.Min(h - 70f * u, y + 20f * u), w, 34f * u), "눌러서 넘긴다", sHint);
        }
        GUI.color = g0; GUI.matrix = m0;
        if (GUI.Button(new Rect(0, 0, w, h), GUIContent.none, GUIStyle.none) && t > hold * 0.6f && outAt < 0f) outAt = t;
    }
}
