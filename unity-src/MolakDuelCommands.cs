using Naninovel;
using UnityEngine;

/// <summary>비무 시스템 — 상대 생성/판정. 서사는 전부 .nani가 담당하고 여기는 수치만 계산한다.</summary>
public static class MolakDuel
{
    public struct Foe { public string Name; public int Rank; public float P; public string Boss; }
    public static Foe Pending;

    static readonly string[] Names = { "유랑검객", "파문제자", "표국 무사", "낭인", "산채 부두목", "무관 사범", "비도객", "도객", "권사", "창수" };

    public static string TierName (float r)
    {
        if (r <= 0) return "무명";
        if (r <= 1) return "9경 멸도";
        if (r <= 10) return "8경 천묵";
        if (r <= 100) return "7경 허실";
        if (r <= 1000) return "6경 발경";
        if (r <= 10000) return "5경 화경";
        if (r <= 100000) return "4경 관맥";
        if (r <= 1000000) return "3경 운위";
        if (r <= 3000000) return "2경 취위";
        return "1경 식위";
    }

    public static Foe MakeFoe (string kind)
    {
        var rank = MolakStats.Get("rank");
        float baseRank = rank > 0 ? rank : 8000000f;
        switch (kind)
        {
            case "street": return new Foe { Name = "떠돌이 무사", Rank = 7903211, P = 6 + Random.Range(0, 3) };
            case "hochi": return new Foe { Name = "독아단 아장 호치", Rank = 184210, P = 31 + Random.Range(0, 4), Boss = "hochi" };
            case "kyona": return new Foe { Name = "독아단주 견아", Rank = 51000, P = 45 + Random.Range(0, 4), Boss = "kyona" };
            case "jinbaek": return new Foe { Name = "혈랑대주 진백", Rank = 300, P = 55 + Random.Range(0, 4), Boss = "jinbaek" };
            case "chainmob": return new Foe { Name = "혈쇄문 사슬잡이들", Rank = 30000, P = 39 + Random.Range(0, 4) };
            case "hunt1": return new Foe { Name = "혈랑대 척후조", Rank = 9000, P = 45 + Random.Range(0, 4) };
            case "hunt2": return new Foe { Name = "혈랑대 정예", Rank = 2500, P = 56 + Random.Range(0, 4) };
            case "hunt3": return new Foe { Name = "집법전주", Rank = 40, P = 66 + Random.Range(0, 5), Boss = "hunt3" };
            case "cheolsan": return new Foe { Name = "철산도", Rank = 5213, P = 40 + Random.Range(0, 4) };
            case "wolf": return new Foe { Name = "혈쇄문주", Rank = 77, P = 53 + Random.Range(0, 5), Boss = "wolf" };
            case "lord": return new Foe { Name = "혈마교주 혈천마", Rank = 1, P = 81 + Random.Range(0, 5), Boss = "lord" };
            case "rival":
            {
                int t = Mathf.Max(FloorRank(), Mathf.FloorToInt(baseRank * 0.75f));
                return new Foe { Name = "곽산", Rank = t, P = Mathf.Round(5 + Mathf.Log10(8000001f / t) * 11f + Random.Range(1, 4)) };
            }
            default:
            {
                int t = rank > 0 ? Mathf.Max(FloorRank(), Mathf.FloorToInt(baseRank * (0.32f + Random.value * 0.26f))) : Random.Range(6500000, 7900001);
                return new Foe { Name = Names[Random.Range(0, Names.Length)], Rank = t, P = Mathf.Round(4 + Mathf.Log10(8000001f / t) * 11f + Random.Range(0, 4)) };
            }
        }
    }

    /// <summary>비무판에서 만날 수 있는 최고 서열: 백위 안은 교의 사람들(혈쇄문주를 꺾기 전엔 101위까지), 그 뒤엔 십위권(교주 직속) 바로 밑까지.</summary>
    public static int FloorRank () { return MolakStats.Get("f_wolfdown") > 0 ? 10 : 100; }

    public static float MyPower ()
    {
        float hp = MolakStats.Get("hp"), hpm = Mathf.Max(1, MolakStats.Get("hpmax"));
        return MolakStats.Get("sw") * 1.1f + MolakStats.Get("ng") * 1.3f + (hp / hpm) * 3f + (MolakStats.Get("sl") >= 5 ? 2f : 0f);
    }

    public static float Odds (Foe foe, float bonus)
    {
        return Mathf.Clamp(50f + (MyPower() + bonus - foe.P) * 5f, 5f, 92f);
    }
}

/// <summary>@duelinfo kind:any — 상대를 뽑아 전시용 변수 설정 (foename, foerank, foetier, odds10)</summary>
[Alias("duelinfo")]
public class DuelInfoCommand : Command
{
    public StringParameter Kind;

#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        var kind = Assigned(Kind) ? Kind.Value : "any";
        var foe = MolakDuel.MakeFoe(kind);
        MolakDuel.Pending = foe;
        var vars = Engine.GetServiceOrErr<ICustomVariableManager>();
        vars.SetVariableValue("duelkind", new CustomVariableValue(kind));
        vars.SetVariableValue("foename", new CustomVariableValue(foe.Name));
        vars.SetVariableValue("foerank", new CustomVariableValue(string.Format("{0:N0}", foe.Rank)));
        vars.SetVariableValue("foetier", new CustomVariableValue(MolakDuel.TierName(foe.Rank)));
        MolakStats.Set("odds10", Mathf.Clamp(Mathf.Round(MolakDuel.Odds(foe, 0) / 10f), 1f, 9f));
        MolakStats.Set("cansl", MolakStats.Get("sl") >= 3 ? 1f : 0f);
        MolakStats.Set("cancurse", MolakStats.Get("curse") >= 10 ? 1f : 0f);
        string art = kind == "hochi" ? "hochi" : kind == "kyona" ? "kyona" : kind == "jinbaek" ? "jinbaek" : kind == "lord" ? "lord" : kind == "rival" ? "gwaksan" : kind == "hunt3" ? "lawlord" : null;
        if (art != null) MolakArt.Char(art);
    }
}

/// <summary>@duel style:front|killer|guard|curse — 판정. 결과 변수: duelwin, oldrank, newrank(표시용 문자열), tierup, tiername, dmg, reward</summary>
[Alias("duel")]
public class DuelCommand : Command
{
    public StringParameter Style;

#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        var vars = Engine.GetServiceOrErr<ICustomVariableManager>();
        var foe = MolakDuel.Pending;
        string kind = vars.VariableExists("duelkind") ? vars.GetVariableValue("duelkind").String : "any";
        string style = Assigned(Style) ? Style.Value : "front";
        float bonus = 0f, hpRisk = 3f;
        float sl = MolakStats.Get("sl"), curse = MolakStats.Get("curse");
        if (style == "killer") { bonus = 2f + sl * 0.3f; hpRisk = 5f; }
        if (style == "guard") { bonus = -1f; hpRisk = 2f; }
        if (style == "curse") { bonus = 3f + curse * 0.06f; hpRisk = 2f; MolakStats.Add("curse", 5f); }
        if (kind == "lord" && MolakStats.Get("f_janginfo") > 0) bonus += 1.5f;
        if (kind == "lord" && MolakStats.Get("f_lordedge") > 0) bonus += 1f;
        if (kind == "wolf" && MolakStats.Get("f_chaininfo") > 0) bonus += 1.5f;
        if (kind == "wolf" && MolakStats.Get("f_wolfedge") > 0) bonus += 1.5f;
        if (kind == "wolf" && MolakStats.Get("f_wolfseal") > 0) bonus += 1f;
        if (kind == "wolf" && MolakStats.Get("f_wolfally") > 0) bonus += 2f;
        if (kind == "lord" && MolakStats.Get("f_northstep") > 0) bonus += 1.5f;
        if (kind == "lord" && MolakStats.Get("f_lordseal") > 0) bonus += 0.5f;
        if (kind == "lord" && MolakStats.Get("f_cheonggang") > 0) bonus += 0.5f;
        if (kind == "hochi" && MolakStats.Get("f_dokinfo") > 0) bonus += 1.5f;
        float odds = MolakDuel.Odds(foe, bonus);
        bool win = Random.Range(1, 101) <= odds;
        float oldRank = MolakStats.Get("rank");
        if (win)
        {
            float newRank = oldRank > 0 ? Mathf.Min(oldRank, foe.Rank) : foe.Rank;
            bool tierUp = MolakDuel.TierName(oldRank) != MolakDuel.TierName(newRank);
            float dmg = Random.Range(0, Mathf.Max(1, (int)hpRisk - 1));
            float reward = kind == "unju" ? 3f : kind == "hochi" ? 8f : kind == "cheolsan" ? 100f : 1f;
            MolakStats.Set("rank", newRank);
            MolakStats.Add("hp", -dmg);
            MolakStats.Add("sw", 1f);
            MolakStats.Add("mn", reward);
            if (style == "killer" || style == "curse") MolakStats.Add("sl", 1f);
            MolakStats.Set("duelwin", 1f); MolakArt.EndDuel();
            MolakStats.Set("tierup", tierUp ? 1f : 0f);
            vars.SetVariableValue("oldrank", new CustomVariableValue(oldRank > 0 ? string.Format("{0:N0}", oldRank) : "무명"));
            vars.SetVariableValue("newrank", new CustomVariableValue(string.Format("{0:N0}", newRank)));
            vars.SetVariableValue("tiername", new CustomVariableValue(MolakDuel.TierName(newRank)));
            MolakVfx.Gold();
            MolakHud.RankShift(oldRank, newRank, tierUp, MolakDuel.TierName(newRank));
            MolakDuelCard.Show(true, foe.Name, "서열 " + (oldRank > 0 ? string.Format("{0:N0}", oldRank) : "무명") + " → " + string.Format("{0:N0}", newRank) + "위", tierUp ? "─ " + MolakDuel.TierName(newRank) + " 진입 ─" : "");
            if (foe.Boss == "wolf") MolakStats.Set("f_wolfdown", 1f);
            if (foe.Boss == "hochi") MolakStats.Set("f_hmsaved", 1f);
        }
        else
        {
            float dmg = Random.Range((int)hpRisk, (int)hpRisk + 4) + (string.IsNullOrEmpty(foe.Boss) ? 0f : 3f) + Mathf.Floor(foe.P / 9f);
            MolakStats.Add("hp", -dmg);
            MolakStats.Set("duelwin", 0f); MolakArt.EndDuel();
            MolakVfx.Blood();
            MolakStats.Set("tierup", 0f);
            MolakDuelCard.Show(false, foe.Name, "서열 변동 없음 · 체력 −" + Mathf.RoundToInt(dmg), "");
        }
        MolakStats.GrowBody();
        MolakStats.ClampAll();
    }
}
