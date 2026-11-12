using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// 선택지 주석: @choice 의 if: 조건을 실행 시점에 읽어 "무엇 때문에 열린 선택지인지"를 만든다.
///  - Hint: 카드 글귀 아래 회색 작은 글씨 (물건 이름·능력치·지난 생의 말로 등)
///  - Kind: 인장 색 (0 일반 주홍 · 1 전생의 앎 보라 · 2 물건·비방·인연 쪽빛 · 3 능력치 먹 · 4 은자 금 · 5 사술 검붉은 자주)
/// MolakFlow 가 AddChoice 실행 때마다 Push, 새 선택지 묶음이 시작되면 Reset. MolakTheme 는 카드 순서(cidx)로 꺼내 쓴다.
/// </summary>
public static class MolakChoiceMeta
{
    public const int Normal = 0, Know = 1, Item = 2, Stat = 3, Coin = 4, Curse = 5;
    public struct Meta { public string Hint; public int Kind; }
    public static readonly List<string> Batch = new List<string>();   // 이번 선택지 묶음의 if: 조건들(실행 순서 = 카드 순서)
    /// <summary>카드마다의 승부수 표시: 글귀 끝의 〔검술+2〕 · 〔내공 12〕 · 〔살기〕 (없으면 null). 나니노벨이 대괄호를 인라인 명령으로 읽지 않도록 거북 괄호를 쓴다.</summary>
    public static readonly List<string> Risks = new List<string>();

    public static void Reset () { Batch.Clear(); Risks.Clear(); }
    public static void Push (string cond, string label = null) { Batch.Add(cond ?? ""); Risks.Add(RiskTag(label)); }

    /// <summary>글귀 안의 승부수 표시.</summary>
    static readonly Regex RiskRx = new Regex(@"\s*〔(검술|내공|살기)\s*([+\-]?\d+)?〕");
    public static string RiskTag (string label)
    {
        if (string.IsNullOrEmpty(label)) return null;
        var m = RiskRx.Match(label);
        return m.Success ? MolakRules.StatKey(m.Groups[1].Value) + "|" + (m.Groups[2].Success ? m.Groups[2].Value : "0") : null;
    }
    public static string StripRisk (string label) { return string.IsNullOrEmpty(label) ? label : RiskRx.Replace(label, ""); }

    /// <summary>승부수 주석: "검술 14 대 18 · 승산 4할". 실제 판정(@risk)과 같은 식으로 셈한다.</summary>
    public static string RiskHint (string tag)
    {
        var parts = tag.Split('|');
        string stat = parts[0]; float day = MolakStats.Get("day");
        float need = MolakRules.RiskNeed(stat, parts.Length > 1 ? parts[1] : "0", day);
        float have = MolakStats.Get(stat);
        return MolakRules.StatName(stat) + " " + have.ToString("0") + " 대 " + need.ToString("0") + " · 승산 " + MolakRules.OddsText(MolakRules.RiskOdds(stat, have, need));
    }

    public static Meta Get (int index, string label)
    {
        var m = Build(index >= 0 && index < Batch.Count ? Batch[index] : "", label);
        string risk = index >= 0 && index < Risks.Count ? Risks[index] : null;
        if (risk != null)
        {
            string rh = RiskHint(risk);
            m.Hint = string.IsNullOrEmpty(m.Hint) ? rh : rh + "  ·  " + m.Hint;
            if (m.Kind == Normal || m.Kind == Coin) m.Kind = Stat;
        }
        return m;
    }

    static readonly Regex Atom = new Regex(@"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*(>=|<=|==|!=|>|<)\s*([0-9.]+)\s*$");

    static int Rank (int kind) { switch (kind) { case Know: return 5; case Curse: return 4; case Item: return 3; case Stat: return 2; case Coin: return 1; } return 0; }

    public static Meta Build (string cond, string label)
    {
        int kind = Normal;
        label = label ?? "";
        if (label.Contains("저주 +") || label.Contains("사술")) kind = Curse;
        var alts = new List<string>();
        if (!string.IsNullOrEmpty(cond))
        {
            foreach (var alt in cond.Split(new[] { "||" }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = new List<string>();
                foreach (var raw in alt.Split(new[] { "&&" }, System.StringSplitOptions.RemoveEmptyEntries))
                {
                    var m = Atom.Match(raw.Trim('(', ')', ' '));
                    if (!m.Success) continue;
                    string v = m.Groups[1].Value, op = m.Groups[2].Value; float n; float.TryParse(m.Groups[3].Value, out n);
                    bool positive = op == ">=" || (op == ">" ) || (op == "==" && n > 0);
                    bool upperBound = op == "<=" || op == "<";
                    int k; string text = Describe(v, op, n, positive, upperBound, label, out k);
                    if (k != Normal && Rank(k) > Rank(kind)) kind = k;
                    if (!string.IsNullOrEmpty(text) && !parts.Contains(text)) parts.Add(text);
                }
                var joined = string.Join(" · ", parts.ToArray());
                if (joined.Length > 0 && !alts.Contains(joined)) alts.Add(joined);
            }
        }
        return new Meta { Hint = string.Join("  또는  ", alts.ToArray()), Kind = kind };
    }

    static string Describe (string v, string op, float n, bool positive, bool upperBound, string label, out int kind)
    {
        kind = Normal;
        // 전생의 앎
        if (v.StartsWith("g_"))
        {
            if (!positive) return null;
            kind = Know;
            if (v.StartsWith("g_end_")) { var e = MolakEnd.Find(v.Substring(6)); return "지난 생의 말로 · " + e.Kor; }
            if (v == "g_redverse") return "지난 생 · 적운의 구결";
            if (v == "g_soaname") return "지난 생 · 그 이름";
            if (v == "g_soadied") return "지난 생 · 그 밤";
            if (v == "g_runs") return "회귀한 자";
            if (v == "g_up_mem") return "지난 생";
            if (v == "g_clears") return "등극을 겪은 자";
            foreach (var ke in MolakKnow.All)
                foreach (var kk in ke.Keys.Split('|')) if (kk == v) return "지난 생 · " + ke.Name;
            return "지난 생";
        }
        // 능력치·자원
        switch (v)
        {
            case "sw": if (!positive) return null; kind = Stat; return "검술 " + n.ToString("0");
            case "ng": if (!positive) return null; kind = Stat; return "내공 " + n.ToString("0");
            case "sl": if (!positive) return null; kind = Stat; return "살기 " + n.ToString("0");
            case "hp": if (!positive) return null; kind = Stat; return "체력 " + n.ToString("0");
            case "curse": if (!positive) return null; kind = Curse; return "저주 " + n.ToString("0") + " 이상";
            case "mn": if (!positive) return null; kind = Coin; return label.Contains("(은자") ? null : "은자 " + n.ToString("0");
            case "aff": if (!positive) return null; kind = Item; return "소아의 마음";
            case "rank": if (!upperBound || n <= 0) return null; kind = Stat; return "서열 " + n.ToString("N0") + "위 이내";
            case "f_wanted": if (!positive) return null; kind = Item; return "수배된 몸";
            case "f_soamet": return null;
            case "rep_maeng": if (!positive) return null; kind = Item; return "무림맹의 신의 " + n.ToString("0");
            case "rep_gae": if (!positive) return null; kind = Item; return "개방의 신의 " + n.ToString("0");
            case "rep_heuk": if (!positive) return null; kind = Item; return "흑도의 신의 " + n.ToString("0");
        }
        // 물건·비방·인연 (행낭 이름 그대로)
        if (v.StartsWith("f_") && positive)
        {
            string nm = BagName(v);
            if (nm != null) { kind = Item; return nm; }
        }
        return null;
    }

    static string BagName (string flag)
    {
        foreach (var e in MolakBag.Items) if (e.Flag == flag) return e.Name;
        foreach (var e in MolakBag.Lore) if (e.Flag == flag) return e.Name;
        foreach (var e in MolakBag.Bonds) if (e.Flag == flag) return e.Name;
        return null;
    }

    public static Color SealColor (int kind, bool night)
    {
        switch (kind)
        {
            case Know: return night ? new Color(0.52f, 0.36f, 0.70f, 1f) : new Color(0.42f, 0.27f, 0.58f, 1f);
            case Item: return night ? new Color(0.28f, 0.44f, 0.60f, 1f) : new Color(0.20f, 0.33f, 0.47f, 1f);
            case Stat: return night ? new Color(0.36f, 0.33f, 0.31f, 1f) : new Color(0.18f, 0.16f, 0.15f, 1f);
            case Coin: return night ? new Color(0.70f, 0.55f, 0.22f, 1f) : new Color(0.62f, 0.47f, 0.16f, 1f);
            case Curse: return night ? new Color(0.48f, 0.18f, 0.40f, 1f) : new Color(0.36f, 0.13f, 0.30f, 1f);
        }
        return night ? new Color(0.60f, 0.18f, 0.14f, 1f) : new Color(0.66f, 0.17f, 0.12f, 1f);
    }
}
