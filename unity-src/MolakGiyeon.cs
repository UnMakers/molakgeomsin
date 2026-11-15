using System.Collections.Generic;
using Naninovel;
using UnityEngine;

/// <summary>
/// 0.4.66 기연(奇緣) — 크게 한 번 몸이 뛰는 순간(언메이커: "도파민 팍 도는 게 없다").
/// @giyeon name:"적운환(赤雲丸)" hanja:"丹" line:"소아가 기름종이째 던져 준 붉은 환약" ng:3 hpmax:3 sw:2 heal:true why:"소아의 적운환"
///  · 능력치를 이 명령이 직접 올리고(능력치 기록에 why 로 남는다), 한 장의 금빛 판으로 "전 → 후"를 숫자가 차오르듯 보여 준다.
///  · 금빛 살(光)이 돌고 불티가 튀고, 가운데에 한 글자 인장이 찍힌다. 종 두 번.
///  · '빨리'·넘기기 중에도 뜬다(보상은 놓치면 안 된다) — 그때는 2.6초 보이고 저절로 닫힌다. '자동'이면 4.2초.
/// </summary>
[Alias("giyeon")]
public class GiyeonCommand : Command
{
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
            rows.Add(new MolakGiyeonCard.Row { Label = "체력의 끝", From = a, To = a + Hpmax.Value, Bad = Hpmax.Value < 0 });
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

public class MolakGiyeonCard : MonoBehaviour
{
    public struct Row { public string Label; public float From, To; public bool Bad; public string Note; }
    static MolakGiyeonCard inst;
    string kicker = "", title = "", hanja = "", line = ""; List<Row> rows = new List<Row>();
    float t = -1f; bool bell2;
    struct Spark { public Vector2 p, v; public float life, size; }
    readonly List<Spark> sparks = new List<Spark>();
    public static bool Showing => inst != null && inst.t >= 0f;
    public static float AutoClose = -1f;

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
        inst.kicker = kick ?? ""; inst.title = ttl ?? ""; inst.hanja = hj ?? "緣"; inst.line = ln ?? ""; inst.rows = rs ?? new List<Row>();
        inst.t = 0f; inst.bell2 = false; inst.sparks.Clear();
        MolakVfx.Flash(new Color(1f, 0.85f, 0.4f, 0.65f), 0.9f); MolakVfx.Shake(16f);
        MolakSfx.TrueBell(); MolakSfx.Vibrate();
    }

    void Update ()
    {
        if (t < 0f) return;
        float dt = Time.unscaledDeltaTime; t += dt;
        if (!bell2 && t > 0.75f) { bell2 = true; MolakSfx.Bell(); MolakSfx.Stamp(); MolakVfx.Shake(8f); Burst(60); }
        if (t < 2.4f && Random.value < 0.55f) Burst(2);
        for (int i = sparks.Count - 1; i >= 0; i--)
        {
            var s = sparks[i]; s.life -= dt; s.p += s.v * dt; s.v *= 1f - 1.4f * dt; s.v.y += 30f * dt;
            if (s.life <= 0f) sparks.RemoveAt(i); else sparks[i] = s;
        }
        if (AutoClose > 0f && t > AutoClose) Close();
    }

    void Burst (int n)
    {
        float u = MolakGui.U;
        for (int i = 0; i < n; i++)
        {
            var d = Random.insideUnitCircle.normalized;
            sparks.Add(new Spark { p = Vector2.zero, v = d * Random.Range(180f, 720f) * u, life = Random.Range(0.6f, 1.6f), size = Random.Range(3f, 8f) * u });
        }
    }

    void Close () { t = -1f; sparks.Clear(); MolakInputGuard.MuteFor(0.3f); }

    GUIStyle sKick, sTitle, sHanja, sLine, sRow, sHint; float builtU = -1f;
    void OnGUI ()
    {
        if (t < 0f) return;
        float u = MolakGui.U, w = Screen.width, h = Screen.height;
        if (sTitle == null || !Mathf.Approximately(builtU, u))
        {
            builtU = u;
            sKick = MolakGui.Label(30, u, TextAnchor.MiddleCenter, false); sKick.font = MolakTypography.Heading(sKick.font);
            sTitle = MolakGui.Label(68, u, TextAnchor.MiddleCenter, true); sTitle.font = MolakTypography.Heading(sTitle.font); sTitle.richText = true;
            sHanja = MolakGui.Label(200, u, TextAnchor.MiddleCenter, false); if (MolakGui.Hanja != null) sHanja.font = MolakGui.Hanja;
            sLine = MolakGui.Label(28, u, TextAnchor.MiddleCenter, true); sLine.richText = true;
            sRow = MolakGui.Label(38, u, TextAnchor.MiddleCenter, false); sRow.richText = true;
            sHint = MolakGui.Label(22, u, TextAnchor.MiddleCenter, false);
        }
        GUI.depth = MolakGui.DepthBlackout + 7;
        MolakInputGuard.Guard(new Rect(0, 0, w, h));
        var g0 = GUI.color; var m0 = GUI.matrix;
        float a = Mathf.Clamp01(t / 0.3f);
        GUI.color = new Color(0.03f, 0.02f, 0.01f, 0.96f * a); GUI.DrawTexture(new Rect(0, 0, w, h), MolakGui.Px);

        // 금빛 살: 가운데에서 천천히 도는 빛줄기 열여섯
        var c = new Vector2(w / 2f, h * 0.36f);
        float rayA = Mathf.Clamp01((t - 0.1f) / 0.6f) * 0.06f;
        float len = Mathf.Max(w, h) * Mathf.Lerp(0.2f, 0.9f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / 0.9f)));
        for (int i = 0; i < 16; i++)
        {
            GUI.matrix = m0;
            GUIUtility.RotateAroundPivot(i * 22.5f + t * 9f, c);
            GUI.color = new Color(1f, 0.82f, 0.38f, rayA * (i % 2 == 0 ? 1f : 0.55f));
            GUI.DrawTexture(new Rect(c.x - (i % 2 == 0 ? 6f : 3f) * u, c.y - len, (i % 2 == 0 ? 12f : 6f) * u, len), MolakGui.Px);
        }
        GUI.matrix = m0;
        // 불티
        foreach (var s in sparks)
        {
            GUI.color = new Color(1f, Random.Range(0.7f, 0.9f), 0.35f, Mathf.Clamp01(s.life) * a);
            GUI.DrawTexture(new Rect(c.x + s.p.x - s.size / 2f, c.y + s.p.y - s.size / 2f, s.size, s.size), MolakGui.Px);
        }

        // 한 글자 인장: 크게 떨어져 박힌다
        float st = Mathf.Clamp01((t - 0.55f) / 0.22f);
        if (st > 0f)
        {
            float sc = Mathf.Lerp(2.4f, 1f, Mathf.SmoothStep(0f, 1f, st));
            var hr = new Rect(c.x - 160f * u, c.y - 150f * u, 320f * u, 300f * u);
            GUI.matrix = m0; GUIUtility.ScaleAroundPivot(new Vector2(sc, sc), c);
            MolakGui.Ink(sHanja, new Color(0.45f, 0.06f, 0.03f, 0.75f * st)); GUI.Label(new Rect(hr.x + 6f * u, hr.y + 6f * u, hr.width, hr.height), hanja, sHanja);
            MolakGui.Ink(sHanja, new Color(1f, 0.86f, 0.45f, st)); GUI.Label(hr, hanja, sHanja);
            GUI.matrix = m0;
        }

        float cw = Mathf.Min(w * 0.9f, 940f * u), cx = (w - cw) / 2f;
        float y = c.y + 170f * u;
        MolakGui.Ink(sKick, new Color(0.95f, 0.72f, 0.3f, a));
        GUI.Label(new Rect(cx, c.y - 250f * u, cw, 44f * u), "─  " + kicker + "  ─", sKick);
        float ta = Mathf.Clamp01((t - 0.8f) / 0.35f);
        MolakGui.Ink(sTitle, new Color(0.5f, 0.06f, 0.03f, 0.6f * ta)); GUI.Label(new Rect(cx + 4f * u, y + 4f * u, cw, 90f * u), title, sTitle);
        MolakGui.Ink(sTitle, new Color(1f, 0.93f, 0.78f, ta)); GUI.Label(new Rect(cx, y, cw, 90f * u), title, sTitle);
        y += 92f * u;
        if (line.Length > 0)
        {
            float lh = sLine.CalcHeight(new GUIContent(line), cw - 60f * u);
            MolakGui.Ink(sLine, new Color(0.82f, 0.76f, 0.66f, ta)); GUI.Label(new Rect(cx + 30f * u, y, cw - 60f * u, lh), line, sLine);
            y += lh + 18f * u;
        }
        // 능력치: 전 → 후, 숫자가 차오른다
        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            float k0 = 1.15f + 0.22f * i;
            float ra = Mathf.Clamp01((t - k0) / 0.25f);
            if (ra <= 0f) continue;
            float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - k0) / 0.7f));
            float cur = Mathf.Round(Mathf.Lerp(r.From, r.To, p));
            float d = r.To - r.From;
            string col = r.Bad ? "#FF7A61" : "#FFD86A";
            string txt = "<color=#CFC4B0>" + r.Label + "</color>   " + r.From.ToString("0") + "  →  <b><color=" + col + ">" + cur.ToString("0") + "</color></b>"
                       + (string.IsNullOrEmpty(r.Note) ? "   <size=" + Mathf.RoundToInt(28 * u) + "><color=" + col + ">(" + (d > 0 ? "+" : "−") + Mathf.Abs(d).ToString("0") + ")</color></size>" : "   <size=" + Mathf.RoundToInt(26 * u) + "><color=#CFC4B0>" + r.Note + "</color></size>");
            float sc = p < 1f && ra < 1f ? Mathf.Lerp(1.3f, 1f, ra) : 1f;
            var rr = new Rect(cx, y, cw, 56f * u);
            GUI.matrix = m0; GUIUtility.ScaleAroundPivot(new Vector2(sc, sc), rr.center);
            GUI.color = new Color(1f, 0.8f, 0.35f, 0.08f * ra); GUI.DrawTexture(new Rect(cx + cw * 0.12f, y + 4f * u, cw * 0.76f, 48f * u), MolakGui.Px);
            MolakGui.Ink(sRow, new Color(1f, 1f, 1f, ra)); GUI.Label(rr, txt, sRow);
            GUI.matrix = m0;
            y += 60f * u;
        }
        float hold = 1.6f + 0.22f * rows.Count;
        if (t > hold)
        {
            MolakGui.Ink(sHint, new Color(0.7f, 0.62f, 0.5f, Mathf.Clamp01((t - hold) / 0.4f)));
            GUI.Label(new Rect(0, Mathf.Min(h - 70f * u, y + 24f * u), w, 34f * u), "눌러서 넘긴다", sHint);
        }
        GUI.color = g0; GUI.matrix = m0;
        if (GUI.Button(new Rect(0, 0, w, h), GUIContent.none, GUIStyle.none) && t > hold * 0.7f) Close();
    }
}
