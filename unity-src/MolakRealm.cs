using System.Collections.Generic;
using MolakVn;
using UnityEngine;

/// <summary>
/// 0.4.72 경지(境地) — 서열에서 떼어 낸 몸과 검의 깊이(언메이커: 전투만 하면 그냥 오른다, 달성감이 없다, 과정이 없다).
///  · 경지 realm 0~8: 삼류 · 이류 · 일류 · 절정 · 초절정 · 화경 · 현경 · 생사경 · 신화경. 이번 생의 값(생마다 삼류에서).
///  · 다음 경지 앞에는 벽이 있다. 벽 앞에 앉으려면 셋: 검술 · 내공이 그 경지의 문턱, 깨달음(insight)이 그 경지의 수.
///  · 깨달음은 지는 판, 넘기 힘든 판을 넘은 날, 검·기가 한꺼번에 자란 날, 사람에게 배운 한 수에서 하나씩 온다(하루 한 번까지 저절로).
///  · 벽 앞에 서면 밤에 폐관할 수 있다(Molak18#Wall). 승산은 동전 던지기가 아니다 — 남는 검·기·깨달음, 협, 호법을 서 줄 사람과 등을 맡긴 사람,
///    품은 물건, 지난 생에 넘어 본 벽, 실패에서 본 벽의 모양이 다 보태고, 저주와 지친 몸이 깎는다. 넘지 못해도 죽지 않는다.
///  · 서열은 경지를 앞서지 못한다: 경지 하나 위 띠까지만 오른다(삼류는 이류 띠 끝, 1,000,001위까지).
/// </summary>
public static class MolakRealm
{
    public static readonly string[] Names = { "삼류", "이류", "일류", "절정", "초절정", "화경", "현경", "생사경", "신화경" };
    // 그 경지로 오르는 벽의 문턱(인덱스 = 오를 경지)
    static readonly int[] ReqSw  = { 0, 6, 9, 12, 15, 18, 21, 24, 27 };
    static readonly int[] ReqNg  = { 0, 5, 8, 11, 14, 17, 20, 23, 26 };
    static readonly int[] ReqIns = { 0, 1, 2, 3, 5, 6, 8, 10, 12 };
    // 경지가 허락하는 가장 높은 서열(숫자가 작을수록 높다): 경지 하나 위 띠의 끝까지
    static readonly float[] Cap = { 1000001f, 100001f, 10001f, 1001f, 101f, 11f, 2f, 1f, 1f };

    public static int Realm => Mathf.Clamp(Mathf.RoundToInt(MolakStats.Get("realm")), 0, 8);
    public static string Name (int r) => Names[Mathf.Clamp(r, 0, 8)];
    public static string Current => Name(Realm);
    public static float CapRank => Cap[Realm];
    public static int Next => Mathf.Min(8, Realm + 1);
    public static int NeedSw => ReqSw[Next];
    public static int NeedNg => ReqNg[Next];
    public static int NeedIns => ReqIns[Next];
    public static int Insight => Mathf.RoundToInt(MolakStats.Get("insight"));
    public static bool Top => Realm >= 8;

    public static bool Ready ()
    {
        if (Top) return false;
        return MolakStats.Get("sw") >= NeedSw && MolakStats.Get("ng") >= NeedNg && Insight >= NeedIns;
    }

    /// <summary>서열이 경지를 앞서려 하면 벽에서 멎는다. 멎었으면 true.</summary>
    public static bool ClampRank (ref float newRank)
    {
        if (newRank > 0f && newRank < CapRank) { newRank = CapRank; return true; }
        return false;
    }

    /// <summary>깨달음 하나. auto=true 는 하루 한 번까지(검·기가 한꺼번에 자란 날 같은 저절로 오는 것).</summary>
    public static void Gain (string why, bool auto = false)
    {
        if (MolakPractice.Active) return;
        float day = MolakStats.Get("day");
        if (auto)
        {
            if (MolakStats.Get("insightday") >= day && day > 0) return;
            MolakStats.Set("insightday", day);
        }
        MolakStatLog.Why("insight", why);
        MolakStats.Add("insight", 1f);
        var log = MolakStats.Str("insightlog", "");
        if (log == "none") log = "";
        var parts = new List<string>(log.Length > 0 ? log.Split('|') : new string[0]);
        parts.Add(why);
        while (parts.Count > 12) parts.RemoveAt(0);
        MolakStats.SetStr("insightlog", string.Join("|", parts.ToArray()));
    }

    /// <summary>0.4.94 깨달음은 배운 날에만 온다(언메이커: 좋은 아이템 먹으면 경지가 막 오른다).
    /// 기준: 검·기가 한꺼번에 자란 그 장면에 가르칠 수 있는 사람(인물 표의 주연, 또는 Teachers)이 서 있어야 한다 — 영약·기연은 몸만 키운다.
    /// 깨달음의 다른 길(진 판, 넘기 힘든 판, 넘지 못한 벽, 대본의 @realm do:gain)은 그대로다.</summary>
    public static readonly string[] Teachers = { "gyeol", "hyeok", "ungeom", "yeomga", "abbot", "beggarmonk" };
    public static void Taught (string why)
    {
        string id = MolakArt.CharId ?? "";
        if (id.Length == 0) return;
        string baseId = id.Split('_')[0];
        bool teacher = MolakCast.MainNames.ContainsKey(id) || MolakCast.MainNames.ContainsKey(baseId) || System.Array.IndexOf(Teachers, baseId) >= 0;
        if (!teacher) return;
        string who = MolakCast.MainNames.ContainsKey(id) ? MolakCast.MainNames[id] : MolakCast.OtherNames.ContainsKey(id) ? MolakCast.OtherNames[id] : "";
        Gain(who.Length > 0 ? who + "에게서 — " + why : why, true);
    }

    public struct Part { public string Label; public float V; }

    /// <summary>0.5.0 그 벽 앞에 맨몸으로 앉았을 때의 승산.</summary>
    public static float BaseOdds (int next) { return 40f - 4f * Mathf.Max(0, next - 1); }
    /// <summary>0.5.0 벽을 넘은 몸이 자리 잡는 날수 — 그동안은 다음 벽 앞에 앉지 못한다(오른 경지가 높을수록 길다: 이류 6일 … 생사경 12일).</summary>
    public static float SettleDays (int realm) { return 5f + realm; }
    /// <summary>자리 잡기까지 남은 날(없으면 0). 넘지 못한 밤의 이틀도 여기에 든다.</summary>
    public static int WaitLeft { get { return Mathf.Max(0, Mathf.CeilToInt(MolakStats.Get("btwait") - MolakStats.Get("day") + 1f)); } }

    /// <summary>폐관의 승산과 그 셈(무엇이 얼마를 보탰는지).</summary>
    public static float Odds (List<Part> parts)
    {
        float G (string k) => MolakStats.Get(k);
        void P (string l, float v) { if (!string.IsNullOrEmpty(l) && Mathf.Abs(v) >= 0.5f) parts?.Add(new Part { Label = l, V = Mathf.Round(v) }); }
        // 0.4.94 기준: 경지는 「먹은 것」보다 「배운 것·넘어진 것·곁의 사람」으로 넘는다. 넘친 검·기는 몫을 줄이고(각 8), 품은 물건은 가르침만(10),
        //   등을 맡긴 사람(18)과 실패에서 본 벽(24)을 크게 친다.
        // 0.5.0 기준(언메이커: 경지 오르는 게 너무 쉽고 빠르다): 높은 벽일수록 맨몸의 몫이 준다 — 이류 40, 그 뒤로 벽마다 4씩(신화경 12).
        //   높은 벽은 곁의 사람·배운 것·넘어져 본 밤이 받쳐야 넘는다.
        float baseOdds = BaseOdds(Next);
        float sum = baseOdds;
        P("벽 앞에 앉는 몸", baseOdds);
        P("문턱을 넘친 검", Mathf.Min(8f, (G("sw") - NeedSw) * 2f));
        P("문턱을 넘친 기", Mathf.Min(8f, (G("ng") - NeedNg) * 2f));
        P("남는 깨달음", Mathf.Min(18f, (Insight - NeedIns) * 6f));
        float hy = G("f_hyeop");
        P("협(俠) — 남을 위해 칼을 든 날들", hy >= 7 ? 12f : hy >= 4 ? 8f : hy >= 2 ? 4f : 0f);
        string guard = MolakStats.Str("btguard", "none");
        P(guard == "soa" ? "호법 — 담 위의 소아" : guard == "hajin" ? "호법 — 문 앞의 하진무" : guard == "town" ? "호법 — 골목 사람들의 등불" : "",
          guard == "soa" ? 12f : guard == "hajin" ? 9f : guard == "town" ? 9f : 0f);
        float people = 0f;
        if (G("f_gyeolally") > 0 && G("f_gyeoldead") < 1) people += 9f;      // 다섯째 사형이 막힌 자리를 짚는다
        if ((G("f_hyeokally") > 0 || G("f_sparfriend") > 0) && G("f_hyeokdead") < 1) people += 6f;   // 염혁과 맞춘 합
        if (G("f_jinally") > 0 && G("f_jinbaekdead") < 1) people += 8f;      // 대사형의 북두
        if (G("f_soabond") > 0 && G("f_soadead") < 1) people += 6f;          // 소아와 맞춘 합벽의 걸음
        if (G("f_gyeolfight") > 0) people += 3f;
        P("등을 맡긴 사람들의 수", Mathf.Min(18f, people));
        float items = 0f;   // 가르침이 적힌 것만 — 약·향·부적은 몸을 돕지 벽을 넘겨 주지 않는다
        if (G("f_biloklearn") > 0) items += 7f; else if (G("f_bilokhalf") > 0) items += 3f;
        if (G("f_cmform") > 0) items += 4f;
        if (G("f_sutrashard") > 0) items += 2f;
        P("품은 가르침 — 비록·검보·경판", Mathf.Min(10f, items));
        P("지난 생에 넘어 본 벽", G("g_realmbest") >= Next ? 10f : 0f);
        P("넘지 못한 밤에 본 벽의 모양", Mathf.Min(24f, G("btfail") * 8f));
        P("가슴의 문양 — 운기가 열을 깨운다", -Mathf.Min(20f, G("curse") / 4f));
        float hpm = Mathf.Max(1f, G("hpmax"));
        P("지친 몸", G("hp") / hpm < 0.5f ? -10f : 0f);
        P("다친 데", G("injury") > 0 ? -8f : 0f);
        P("운기를 끊고 칼을 들었다", -G("btpen"));
        if (parts != null) { sum = 0f; foreach (var p in parts) sum += p.V; }
        else
        {
            var tmp = new List<Part>(); return Odds(tmp);
        }
        if (MolakStory.On) sum = Mathf.Max(sum, 95f);
        return Mathf.Clamp(Mathf.Round(sum), 10f, 95f);
    }

    public static string OddsText (List<Part> parts)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var p in parts)
        {
            string c = p.V >= 0 ? "#2E6E37" : "#A33226";
            if (p.Label == "벽 앞에 앉는 몸") continue;
            sb.Append("<color=").Append(c).Append(">").Append(p.V >= 0 ? "+" : "−").Append(Mathf.Abs(p.V).ToString("0")).Append("</color> ").Append(p.Label).Append('\n');
        }
        // 0.4.80 더하고 뺄 것이 하나도 없으면 빈 줄이 되어 나니노벨이 오류를 냈다(「{btoddstext}」 줄) — 한 줄을 둔다.
        if (sb.Length == 0) return "<color=#8A7A66>더할 것도 뺄 것도 없다.</color>";
        return sb.ToString().TrimEnd('\n');
    }

    /// <summary>HUD 서랍의 벽 한 줄.</summary>
    public static string WallLine ()
    {
        if (Top) return "<color=#C9A227>더 넘을 벽이 없다</color>";
        if (Ready() && WaitLeft > 0) return "<color=#C9A227>" + Name(Next) + "의 벽 앞</color> — 몸이 자리 잡는 중(" + WaitLeft + "일 뒤)";
        if (Ready()) return "<color=#C9A227>" + Name(Next) + "의 벽 앞 — 밤에 폐관할 수 있다</color>";
        string Mark (float have, int need) => have >= need ? "<color=" + (MolakTheme.Night ? "#7FD08A" : "#2E6E37") + ">" + have.ToString("0") + "/" + need + "</color>" : have.ToString("0") + "/" + need;
        return MolakHud.Tag(Name(Next), MolakHud.StatCol.Realm) + "의 벽 — " + MolakHud.SwordTag("검") + " " + Mark(MolakStats.Get("sw"), NeedSw) + " · " + MolakHud.Tag("기", MolakHud.StatCol.Ng) + " " + Mark(MolakStats.Get("ng"), NeedNg) + " · " + MolakHud.Tag("깨달음", MolakHud.StatCol.Ins) + " " + Mark(Insight, NeedIns);
    }

    /// <summary>이야기 모드: 날짜에 맞춰 경지를 채운다(이야기가 서열의 벽에 막히지 않게).</summary>
    public static void StoryFill (float day)
    {
        int want = day >= 56 ? 7 : day >= 50 ? 6 : day >= 31 ? 5 : day >= 26 ? 4 : day >= 20 ? 3 : day >= 14 ? 2 : day >= 6 ? 1 : 0;
        // 0.4.75 날짜보다 둘 이상 처지면 그 아래까지는 조용히 채운다(밀린 벽이 한꺼번에 몰리지 않게)
        if (Realm < want - 1) { MolakStats.Set("realm", want - 1); MolakStats.SetStr("tiername", Name(want - 1)); }
        // 마지막 한 벽은 건너뛰지 않는다: 문턱만 채워 두면 그날 밤 벽 앞에 앉는다(벽마다 한 사람의 깨달음이 있다)
        if (Realm < want)
        {
            if (MolakStats.Get("sw") < NeedSw) MolakStats.Set("sw", NeedSw);
            if (MolakStats.Get("ng") < NeedNg) MolakStats.Set("ng", NeedNg);
            if (Insight < NeedIns) MolakStats.Set("insight", NeedIns);
        }
    }
}

/// <summary>@realm do:check|odds|roll|up|fail — 경지의 밤에서 쓴다.
///  check: btready(벽 앞인가) · odds: btodds, btoddstext, btnext · roll: btpass · up: 경지 하나 · fail: 넘지 못한 밤.</summary>
[Alias("realm")]
public class RealmCommand : Command
{
    public StringParameter Do;
    public StringParameter Why;
#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        string d = Assigned(Do) ? Do.Value : "check";
        switch (d)
        {
            case "check":
                MolakStats.Set("btready", MolakRealm.Ready() ? 1f : 0f);
                MolakStats.SetStr("btnext", MolakRealm.Name(MolakRealm.Next));
                break;
            case "odds":
            {
                var parts = new List<MolakRealm.Part>();
                float o = MolakRealm.Odds(parts);
                MolakStats.Set("btodds", o);
                MolakStats.SetStr("btoddstext", MolakRealm.OddsText(parts));
                MolakStats.SetStr("btnext", MolakRealm.Name(MolakRealm.Next));
                break;
            }
            case "roll":
            {
                float o = MolakRealm.Odds(null);
                MolakStats.Set("btodds", o);
                MolakStats.Set("btpass", Random.Range(0f, 100f) < o ? 1f : 0f);
                break;
            }
            case "up":
            {
                int r = Mathf.Min(8, MolakRealm.Realm + 1);
                MolakStats.Set("realm", r);
                MolakStats.SetStr("tiername", MolakRealm.Name(r));
                if (MolakStats.Get("g_realmbest") < r) MolakStats.Set("g_realmbest", r);
                MolakStats.Set("btfail", 0f);
                MolakStats.Set("btpen", 0f);
                // 0.5.0 넘은 벽이 몸에 자리 잡을 때까지 다음 벽은 열리지 않는다(이야기 모드는 날짜에 맞춰 채우므로 뺀다)
                if (!MolakStory.On) MolakStats.Set("btwait", MolakStats.Get("day") + MolakRealm.SettleDays(r));
                MolakStatLog.Why("*", "경지의 밤 — " + MolakRealm.Name(r) + "의 벽을 넘었다");
                break;
            }
            case "gain":
                MolakRealm.Gain(Assigned(Why) ? Why.Value : "한 수를 깨쳤다");
                break;
            case "fail":
                MolakStats.Add("btfail", 1f);
                MolakStats.Set("btpen", 0f);
                MolakStats.Set("btwait", MolakStats.Get("day") + 2f);
                MolakRealm.Gain("넘지 못한 벽의 모양을 몸이 안다");
                break;
        }
    }
}
