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
            // 업보의 칼: 내 힘에 맞춰 오는 상대들 — 질 수도, 이길 수도 있는 판
            case "grudge": return new Foe { Name = GrudgeNames[Random.Range(0, GrudgeNames.Length)], Rank = rank > 0 ? Mathf.FloorToInt(baseRank * 1.2f) : 7800000, P = Mathf.Round(MyPower() + Random.Range(-4, 2)) };
            case "debt": return new Foe { Name = "고리대 왕삼의 수하들", Rank = 6200000, P = Mathf.Round(16 + MolakStats.Get("day") * 0.5f + Random.Range(0, 4)) };
            case "assassin": return new Foe { Name = "교의 자객", Rank = rank > 0 ? Mathf.Max(FloorRank(), Mathf.FloorToInt(baseRank * 0.5f)) : 900000, P = Mathf.Round(MyPower() + Random.Range(-1, 3)) };
            case "soa": return new Foe { Name = "적운검파 소아", Rank = rank > 0 ? Mathf.Max(FloorRank(), Mathf.FloorToInt(baseRank * 0.8f)) : 600000, P = Mathf.Round(MyPower() + Random.Range(0, 4)), Boss = "soa" };
            case "raid": return new Foe { Name = "혈랑대 야습조", Rank = 2500, P = Mathf.Round(MyPower() + Random.Range(-2, 3)) };
            case "tax": return new Foe { Name = "징세조 칼잡이들", Rank = 400000, P = 27 + Random.Range(0, 5) };
            case "back": // 뒷골목 사설 비무판: 더 깊은 서열의 상대, 더 세고, 판돈 셋
            {
                int t = rank > 0 ? Mathf.Max(FloorRank(), Mathf.FloorToInt(baseRank * (0.22f + Random.value * 0.18f))) : Random.Range(5500000, 7000001);
                return new Foe { Name = BackNames[Random.Range(0, BackNames.Length)], Rank = t, P = Mathf.Round(7 + Mathf.Log10(8000001f / t) * 11f + Random.Range(0, 4)) };
            }
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

    static readonly string[] GrudgeNames = { "죽은 자의 의형제", "죽은 자의 사부", "상복 입은 쌍도객", "원수를 갚으러 온 사내" };

    /// <summary>지면 목숨을 잃는 판: 이야기의 결전, 업보의 칼, 생사결. 그 밖의 비무는 지더라도 목은 붙어 있다.</summary>
    public static bool Lethal (string kind)
    {
        switch (kind)
        {
            case "hochi": case "kyona": case "jinbaek": case "chainmob": case "hunt1": case "hunt2": case "hunt3":
            case "wolf": case "lord": case "cheolsan": case "grudge": case "debt": case "assassin": case "tax": case "soa": case "raid": return true;
        }
        return MolakStats.Get("dsit") == 7;
    }

    static readonly string[] BackNames = { "얼굴 없는 도객", "복면의 창잡이", "외팔이 낭인", "이 빠진 칼의 사내", "말 없는 쌍부(雙斧)" };

    public static float MyPower ()
    {
        float hp = MolakStats.Get("hp"), hpm = Mathf.Max(1, MolakStats.Get("hpmax"));
        return MolakStats.Get("sw") * 1.1f + MolakStats.Get("ng") * 1.3f + (hp / hpm) * 3f + (MolakStats.Get("sl") >= 5 ? 2f : 0f)
            - (MolakStats.Get("injury") > 0 ? 4f : 0f) - (MolakStats.Get("taint") > 0 ? 2f : 0f);
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
        // 판의 형편: 0 없음 1 빗속 2 외나무다리 3 구경꾼 4 독 바른 칼 5 판돈 6 제자를 데려온 상대 7 생사결(뒷골목만)
        int sit = 0;
        if (kind == "any" || kind == "street" || kind == "back")
        {
            int r = Random.Range(0, 100);
            if (kind == "back" && r < 18) sit = 7;
            else if (r < 50) sit = 0;
            else sit = 1 + Random.Range(0, 6);
            if (sit == 1 && MolakStats.Get("day") < 3) sit = 0;
        }
        MolakStats.Set("dsit", sit);
        var foe = MolakDuel.MakeFoe(kind);
        if (sit == 4) foe.P -= 3f;           // 독에 기대는 손은 검이 무르다
        if (sit == 7) foe.P += 2f;
        MolakDuel.Pending = foe;
        var vars = Engine.GetServiceOrErr<ICustomVariableManager>();
        vars.SetVariableValue("duelkind", new CustomVariableValue(kind));
        // 전생의 기억(상점 강화) + 이 상대에게 죽은 적이 있으면 경고
        bool bossKind = kind == "soa" || kind == "wolf" || kind == "lord" || kind == "jinbaek" || kind == "hochi" || kind == "kyona" || kind == "hunt3" || kind == "cheolsan" || kind == "rival";
        MolakStats.Set("memwarn", bossKind && MolakStats.Get("g_up_mem") > 0 && vars.VariableExists("g_died_" + kind) && vars.GetVariableValue("g_died_" + kind).Number > 0 ? 1f : 0f);
        vars.SetVariableValue("foename", new CustomVariableValue(foe.Name));
        vars.SetVariableValue("foerank", new CustomVariableValue(string.Format("{0:N0}", foe.Rank)));
        vars.SetVariableValue("foetier", new CustomVariableValue(MolakDuel.TierName(foe.Rank)));
        MolakStats.Set("odds10", Mathf.Clamp(Mathf.Round(MolakDuel.Odds(foe, 0) / 10f), 1f, 9f));
        MolakStats.Set("cansl", MolakStats.Get("sl") >= 3 ? 1f : 0f);
        MolakStats.Set("cancurse", MolakStats.Get("curse") >= 10 ? 1f : 0f);
        MolakStats.Set("dlethal", MolakDuel.Lethal(kind) ? 1f : 0f);
        MolakStats.Set("canflee", sit == 2 || sit == 7 || kind == "grudge" || kind == "assassin" ? 0f : 1f);
        MolakStats.Set("cansense", sit == 4 && (MolakStats.Get("f_dokinfo") > 0 || MolakStats.Get("sw") >= 18 || MolakStats.Get("f_herbpulse") > 0) ? 1f : 0f);
        MolakStats.Set("dinj", 0f); MolakStats.Set("dtaint", 0f); MolakStats.Set("dloot", 0f);
        string art = kind == "soa" ? "soa" : kind == "hochi" ? "hochi" : kind == "kyona" ? "kyona" : kind == "jinbaek" ? "jinbaek" : kind == "lord" ? "lord" : kind == "rival" ? "gwaksan" : kind == "hunt3" ? "lawlord" : null;
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
        if (style == "curse2") { bonus = 5f + curse * 0.08f; hpRisk = 2f; MolakStats.Add("curse", 8f); } // 75 이상: 사술이 먼저 나선다
        if (style == "sense") { bonus = 2f; hpRisk = 2f; }   // 독 바른 칼을 먼저 읽는다
        if (MolakStats.Get("memwarn") > 0) bonus += 1f; // 전생의 기억: 상대의 첫 수를 안다
        float sit = MolakStats.Get("dsit");
        if (sit == 1) bonus += style == "guard" ? 2f : (style == "front" || style == "killer" ? -1.5f : 0f); // 빗속: 디딤이 미끄럽다
        if (sit == 2) bonus += style == "killer" ? 1f : (style == "guard" ? -1f : 0f);                    // 외나무다리: 물러설 데가 없다
        if (sit == 3 && style == "killer" && MolakStats.Get("sl") >= 5) bonus += 1f;                        // 구경꾼: 살기가 판을 누른다
        if (sit == 4 && style == "guard") bonus -= 1f;                                                       // 독: 스치기만 해도 진다
        if (sit == 7) hpRisk += 3f;
        if (kind == "grudge" && MolakStats.Get("grudgewarn") > 0) bonus += 3f;                               // 미리 귀띔을 받았다
        if (kind == "back") hpRisk += 2f;
        if (kind == "lord" && MolakStats.Get("f_janginfo") > 0) bonus += 1.5f;
        if (kind == "lord" && MolakStats.Get("f_lordedge") > 0) bonus += 1f;
        if (kind == "wolf" && MolakStats.Get("f_chaininfo") > 0) bonus += 1.5f;
        if (kind == "wolf" && MolakStats.Get("f_wolfedge") > 0) bonus += 1.5f;
        if (kind == "wolf" && MolakStats.Get("f_wolfseal") > 0) bonus += 1f;
        if (kind == "wolf" && MolakStats.Get("f_wolfally") > 0) bonus += 2f;
        if (kind == "lord" && MolakStats.Get("f_northstep") > 0) bonus += 1.5f;
        if (kind == "lord" && MolakStats.Get("f_lordseal") > 0) bonus += 0.5f;
        if (kind == "lord" && MolakStats.Get("f_cheonggang") > 0) bonus += 0.5f;
        if (kind == "lord" && MolakStats.Get("f_halfhap") > 0) bonus += 2f;
        if (kind == "tax" && MolakStats.Get("f_taxknow") > 0) bonus += 3f;       // 지난 생에 본 우두머리의 버릇
        if (kind == "raid" && MolakStats.Get("f_raidprep") > 0) bonus += 3f;     // 그 밤이 올 줄 알고 계단에 기름을 부었다
        if (kind == "raid" && MolakStats.Get("f_amhally") > 0) bonus += 1.5f;    // 암향이 미리 기별했다
        if (kind == "lord" && MolakStats.Get("f_amhally") > 0) bonus += 1f;      // 반쪽의 합벽: 그녀의 검이 빈자리를 막는다
        if (kind == "hochi" && MolakStats.Get("f_dokinfo") > 0) bonus += 1.5f;
        float odds = MolakDuel.Odds(foe, bonus);
        bool win = Random.Range(1, 101) <= odds;
        float oldRank = MolakStats.Get("rank");
        if (win)
        {
            float newRank = oldRank > 0 ? Mathf.Min(oldRank, foe.Rank) : foe.Rank;
            bool tierUp = MolakDuel.TierName(oldRank) != MolakDuel.TierName(newRank);
            float dmg = Random.Range(0, Mathf.Max(1, (int)hpRisk - 1));
            float reward = kind == "unju" ? 3f : kind == "hochi" ? 8f : kind == "cheolsan" ? 100f : kind == "back" ? 3f : 1f;
            MolakStats.Set("rank", newRank);
            MolakStats.Add("hp", -dmg);
            MolakStats.Add("sw", 1f);
            MolakStats.Add("mn", reward);
            if (style == "killer" || style == "curse" || style == "curse2") MolakStats.Add("sl", 1f);
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
            if (sit == 3) { MolakStats.Add("mn", 3f); MolakStats.Add("sl", 1f); }
            if (sit == 7) MolakStats.Add("mn", 10f);
        }
        else
        {
            float dmg = Random.Range((int)hpRisk, (int)hpRisk + 4) + (string.IsNullOrEmpty(foe.Boss) ? 0f : 3f) + Mathf.Floor(foe.P / 9f);
            if (!MolakDuel.Lethal(kind))
            {
                // 목은 붙어 있다. 대신 몸과 전낭과 체면이 값을 치른다.
                float hp0 = MolakStats.Get("hp");
                float floor = Mathf.Max(2f, Mathf.Ceil(MolakStats.Get("hpmax") * 0.2f));   // 목숨을 남겨 두는 판: 바닥까지는 베지 않는다
                if (hp0 - dmg < floor) dmg = Mathf.Max(0f, hp0 - floor);
                bool wasHurt = MolakStats.Get("injury") > 0;
                MolakStats.Set("injury", Mathf.Max(MolakStats.Get("injury"), 3f));
                MolakStats.Set("dinj", 1f);
                if (wasHurt && MolakStats.Get("taint") <= 0) { MolakStats.Set("taint", 1f); MolakStats.Set("taintkind", 2f); MolakStats.Set("dtaint", 1f); }
                if (sit == 4 && MolakStats.Get("taint") <= 0) { MolakStats.Set("taint", 1f); MolakStats.Set("taintkind", 1f); MolakStats.Set("dtaint", 1f); }
                if (sit == 2) { float lost = Mathf.Ceil(MolakStats.Get("mn") / 2f); MolakStats.Add("mn", -lost); MolakStats.Set("dloot", lost); }
                if (sit == 3) MolakStats.Add("sl", -1f);
            }
            MolakStats.Add("hp", -dmg);
            MolakStats.Set("duelwin", 0f); MolakArt.EndDuel();
            MolakVfx.Blood();
            MolakStats.Set("tierup", 0f);
            MolakDuelCard.Show(false, foe.Name, "서열 변동 없음 · 체력 −" + Mathf.RoundToInt(dmg), MolakStats.Get("dtaint") > 0 ? (MolakStats.Get("taintkind") == 1 ? "독이 들었다" : "상처가 덧났다") : (MolakStats.Get("dinj") > 0 ? "부상 — 사흘" : ""));
        }
        MolakStats.GrowBody();
        MolakStats.ClampAll();
    }
}
