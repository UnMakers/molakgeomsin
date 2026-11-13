using System.Collections.Generic;
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
            case "namgung": return new Foe { Name = "남궁세가 소가주 남궁현", Rank = rank > 0 ? Mathf.Max(FloorRank(), Mathf.Min(2140, Mathf.FloorToInt(baseRank * 0.6f))) : 2140, P = Mathf.Round(MyPower() + Random.Range(-1, 3)), Boss = "namgung" };
            case "back": // 뒷골목 사설 비무판: 더 깊은 서열의 상대, 더 세고, 판돈 셋
            {
                int t = rank > 0 ? Mathf.Max(FloorRank(), Mathf.FloorToInt(baseRank * (0.11f + Random.value * 0.11f))) : Random.Range(4500000, 6500001);
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
                // 며칠에 한 판(0.4.3) — 한 판에 오르는 폭을 키웠다
                int t = rank > 0 ? Mathf.Max(FloorRank(), Mathf.FloorToInt(baseRank * (0.20f + Random.value * 0.18f))) : Random.Range(5800000, 7600001);
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
            case "hochi": case "kyona": case "jinbaek": case "hunt1": case "hunt2": case "hunt3":
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
        //           8 등불 하나의 밤 9 맹인 검객 10 아이를 업은 여검객 11 술 취한 고수 12 교 감찰관 앞
        int sit = 0;
        if (kind == "any" || kind == "street" || kind == "back")
        {
            int r = Random.Range(0, 100);
            if (kind == "back" && r < 18) sit = 7;
            else if (r < 50) sit = 0;
            else { int[] pool = { 1, 2, 3, 4, 5, 6, 8, 9, 10, 11, 12 }; sit = pool[Random.Range(0, pool.Length)]; }
            if (sit == 12 && MolakStats.Get("day") < 8) sit = 3;
            if (sit == 1 && MolakStats.Get("day") < 3) sit = 0;
            if (sit == 0 && Mathf.RoundToInt(MolakStats.Get("theme")) == 1 && MolakStats.Get("day") >= 3 && Random.Range(0, 100) < 35) sit = 1; // 장마
        }
        { float sf = MolakStats.Get("dsitforce"); if (sf > 0) { sit = Mathf.RoundToInt(sf); MolakStats.Set("dsitforce", 0f); } } // 이야기가 정한 판의 형편(독 바른 창 등)
        MolakStats.Set("dsit", sit);
        var foe = MolakDuel.MakeFoe(kind);
        if (sit == 4) foe.P -= 3f;           // 독에 기대는 손은 검이 무르다
        if (sit == 7) foe.P += 2f;
        if (sit == 9) foe.P += 1f;           // 눈 대신 귀로 싸우는 자
        float drunkLie = 0f;
        if (sit == 11) { drunkLie = Random.Range(-35f, 25f); foe.P += Random.Range(-3f, 5f); } // 취한 걸음은 읽히지 않는다
        MolakDuel.Pending = foe;
        MolakStats.SetStr("duelkind", kind);
        // 전생의 기억(상점 강화) + 이 상대에게 죽은 적이 있으면 경고
        bool bossKind = DuelCommand.RemembersFirstMove.Contains(kind);
        // 지난 생에 이 상대에게 진 적이 있으면(살았든 죽었든) 첫 수를 안다. 상점의 '전생의 기억'은 죽은 기억까지 또렷하게 한다.
        bool diedTo = MolakStats.Get("g_died_" + kind) > 0;
        bool lostTo = MolakStats.Get("g_know_" + kind) > 0;
        MolakStats.Set("memwarn", bossKind && (lostTo || (MolakStats.Get("g_up_mem") > 0 && diedTo)) ? 1f : 0f);
        MolakStats.SetStr("foename", foe.Name);
        MolakStats.SetStr("foerank", string.Format("{0:N0}", foe.Rank));
        MolakStats.SetStr("foetier", MolakDuel.TierName(foe.Rank));
        MolakStats.Set("odds10", Mathf.Clamp(Mathf.Round((MolakDuel.Odds(foe, 0) + drunkLie) / 10f), 1f, 9f));
        MolakStats.Set("cansl", MolakStats.Get("sl") >= 3 ? 1f : 0f);
        MolakStats.Set("cancurse", MolakStats.Get("curse") >= 10 ? 1f : 0f);
        MolakStats.Set("dlethal", MolakDuel.Lethal(kind) ? 1f : 0f);
        MolakStats.Set("canflee", sit == 2 || sit == 7 || sit == 8 || kind == "grudge" || kind == "assassin" ? 0f : 1f);
        MolakStats.Set("cansense", sit == 4 && (MolakStats.Get("f_dokinfo") > 0 || MolakStats.Get("sw") >= 18 || MolakStats.Get("f_herbpulse") > 0) ? 1f : 0f);
        MolakStats.Set("dinj", 0f); MolakStats.Set("dtaint", 0f); MolakStats.Set("dloot", 0f);
        if (DuelCommand.FoeArt.TryGetValue(kind, out var art)) MolakArt.Char(art);
    }
}

/// <summary>@duel style:front|killer|guard|curse — 판정. 결과 변수: duelwin, oldrank, newrank(표시용 문자열), tierup, tiername, dmg, reward</summary>
[Alias("duel")]
public class DuelCommand : Command
{
    /// <summary>이야기 속 이름 있는 상대(비무판 잡졸이 아닌 판).</summary>
    public static readonly HashSet<string> BossKinds = new HashSet<string> { "soa", "wolf", "lord", "jinbaek", "hochi", "kyona", "hunt3", "cheolsan", "rival", "chainmob", "hunt1", "hunt2", "namgung" };
    /// <summary>지난 생에 진 적이 있으면 첫 수를 기억해 경고해 주는 상대.</summary>
    public static readonly HashSet<string> RemembersFirstMove = new HashSet<string> { "soa", "wolf", "lord", "jinbaek", "hochi", "kyona", "hunt3", "cheolsan", "rival" };
    /// <summary>판에 오를 때 띄우는 인물 그림.</summary>
    public static readonly Dictionary<string, string> FoeArt = new Dictionary<string, string> {
        { "soa", "soa" }, { "hochi", "hochi" }, { "kyona", "kyona" }, { "jinbaek", "jinbaek" }, { "lord", "lord" }, { "rival", "gwaksan" }, { "hunt3", "lawlord" },
    };
    public static bool IsBossKind (string kind) { return BossKinds.Contains(kind); }
    public StringParameter Style;

#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        var foe = MolakDuel.Pending;
        string kind = MolakStats.Str("duelkind", "any");
        string style = Assigned(Style) ? Style.Value : "front";
        float bonus = 0f, hpRisk = 3f;
        float sl = MolakStats.Get("sl"), curse = MolakStats.Get("curse");
        if (style == "killer") { bonus = 2f + sl * 0.3f; hpRisk = 5f; }
        if (style == "guard") { bonus = -1f; hpRisk = 2f; }
        if (style == "curse") { bonus = 3f + curse * 0.06f; hpRisk = 2f; MolakStats.Add("curse", 5f); }
        if (style == "curse2") { bonus = 5f + curse * 0.08f; hpRisk = 2f; MolakStats.Add("curse", 8f); } // 75 이상: 사술이 먼저 나선다
        if (style == "sense") { bonus = 2f; hpRisk = 2f; }   // 독 바른 칼을 먼저 읽는다
        if (style == "onecut") { bonus = 6f; hpRisk = 4f; } // 0.4.33 한칼에 끝낸다: 격이 다를 때만 열린다(승산 열에 여덟 이상)
        if (MolakStats.Get("memwarn") > 0) bonus += 1f + (MolakStats.Get("g_up_mem") > 0 ? 1f : 0f); // 전생의 앎: 상대의 첫 수를 안다
        bonus += MolakStats.Get("dx_mom") * MolakDuelX.MomBonus; MolakStats.Set("dx_mom", 0f);           // 0.4.30: 앞선 합에서 잡은(내준) 기세
        float sit = MolakStats.Get("dsit");
        if (sit == 1) bonus += style == "guard" ? 2f : (style == "front" || style == "killer" ? -1.5f : 0f); // 빗속: 디딤이 미끄럽다
        if (sit == 2) bonus += style == "killer" ? 1f : (style == "guard" ? -1f : 0f);                    // 외나무다리: 물러설 데가 없다
        if (sit == 3 && style == "killer" && MolakStats.Get("sl") >= 5) bonus += 1f;                        // 구경꾼: 살기가 판을 누른다
        if (sit == 4 && style == "guard") bonus -= 1f;                                                       // 독: 스치기만 해도 진다
        if (sit == 7) hpRisk += 3f;
        if (sit == 8) bonus += style == "killer" ? 1.5f : (style == "guard" ? -1f : 0f);                    // 등불 하나: 먼저 찌르는 쪽이 이긴다
        if (sit == 9) bonus += style == "front" || style == "killer" ? -2f : (style == "guard" ? 2f : 0f); // 맹인: 소리 나는 칼은 읽힌다
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
        if (kind == "raid" && MolakStats.Get("f_hajinbond") > 0 && MolakStats.Get("f_hajindead") < 1) bonus += 2f; // 하진무가 혈랑대에게 계단 셋째 칸을 속였다
        if (kind == "lord" && MolakStats.Get("f_amhally") > 0) bonus += 1f;      // 반쪽의 합벽: 그녀의 검이 빈자리를 막는다
        if (kind == "hochi" && MolakStats.Get("f_dokinfo") > 0) bonus += 1.5f;
        if (kind == "wolf" && MolakStats.Get("f_gwakally") > 0) bonus += 1.5f;    // 곽산이 폐철장 뒷길로 사슬잡이 절반을 끌고 갔다
        if (kind == "hunt2" && MolakStats.Get("f_gwakally") > 0) bonus += 1f;
        if (kind == "hochi" && MolakStats.Get("f_yongally") > 0) bonus += 1.5f;   // 도끼 들기 전에 왼발을 끈다
        if (kind == "hochi" && MolakStats.Get("f_poisonswap") > 0) bonus += 2f;   // 쇠이 끝의 독이 감초다
        if (kind == "kyona" && MolakStats.Get("f_poisonswap") > 0) bonus += 1.5f;
        if (kind == "grudge" && MolakStats.Get("f_mournambush") > 0) bonus += 3f;  // 동쪽 처마 밑에서 기다렸다
        if (sit == 7 && MolakStats.Get("f_sangsaknow") > 0) bonus += 3f;          // 등을 보이는 것은 미끼다
        if (kind == "hunt3" && MolakStats.Get("f_lawdoubt") > 0) bonus += 2f;     // 법이 법 밖의 것을 들었다
        if (kind == "lord" && MolakStats.Get("f_ipmaknow") > 0) bonus += 2f;      // 부름의 거짓을 안다
        if (kind == "raid" && MolakStats.Get("f_maengraid") > 0) bonus += 2f;      // 맹의 칼 셋이 지붕에서 뒤를 쳤다
        if ((kind == "raid" || kind == "hunt1" || kind == "hunt2") && MolakStats.Get("f_rivalfriend") > 0 && MolakStats.Get("f_gwakally") < 1) bonus += 1.5f; // 곽산이 등을 받친다(맹약까지는 아니어도)
        if (kind == "lord" && MolakStats.Get("f_namgungdoor") > 0) bonus += 1f;    // 남궁이 측문을 비웠다
        if (kind == "lord" && MolakStats.Get("f_cmally") > 0) bonus += 1.5f;      // 사저가 가르친 아홉째, 운해
        if ((kind == "lord" || kind == "raid") && MolakStats.Get("f_amhwait") > 0) bonus += 1f;   // 침향 한 대: 살려 보낼 손님
        if (kind == "wolf" && MolakStats.Get("f_soachild") > 0) bonus += 1f;      // 사슬 창고의 길을 이미 걸었다
        if ((kind == "hunt1" || kind == "hunt2") && MolakStats.Get("f_gaewarn") > 0) bonus += 1f;   // 담장의 매듭 셋
        if ((kind == "hunt1" || kind == "hunt2") && MolakStats.Get("f_huntdodge") > 0) bonus += 2f; // 빈방을 부수게 했다
        if ((kind == "hunt1" || kind == "hunt2") && MolakStats.Get("f_jinlure") > 0) bonus += 2f;   // 대사형이 정예 절반을 북쪽으로 끌고 갔다
        if (kind == "hunt3" && MolakStats.Get("f_janglast") > 0) bonus += 1.5f;   // 쥐의 마지막 소식: 집법전주는 오른손으로 법을 쓴다
        if (kind == "lord" && (MolakStats.Get("f_soaeve") > 0 || MolakStats.Get("f_soawait") > 0 || MolakStats.Get("f_amheve") > 0 || MolakStats.Get("f_cmeve") > 0)) bonus += 1f;   // 떠나기 전 밤에 누군가 다녀갔다
        if ((kind == "raid" || kind == "hunt2") && MolakStats.Get("f_yongcrew") > 0) bonus += 1f;   // 나루의 지게꾼들이 골목 어귀를 막는다
        if ((kind == "raid" || kind == "hunt2") && MolakStats.Get("f_yongspear") > 0) bonus += 0.5f;   // 곽산에게 배운 작대기 — 둘을 엇대면 골목이 막힌다
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
            MolakStats.SetStr("oldrank", oldRank > 0 ? string.Format("{0:N0}", oldRank) : "무명");
            MolakStats.SetStr("newrank", string.Format("{0:N0}", newRank));
            MolakStats.SetStr("tiername", MolakDuel.TierName(newRank));
            MolakVfx.Gold(); MolakVfx.Shake(10f); MolakSfx.Draw();
            MolakHud.RankShift(oldRank, newRank, tierUp, MolakDuel.TierName(newRank));
            MolakDuelCard.Show(true, foe.Name, "서열 " + (oldRank > 0 ? string.Format("{0:N0}", oldRank) : "무명") + " → " + string.Format("{0:N0}", newRank) + "위", tierUp ? "─ " + MolakDuel.TierName(newRank) + " 진입 ─" : "");
            if (foe.Boss == "wolf") MolakStats.Set("f_wolfdown", 1f);
            if (foe.Boss == "hochi") MolakStats.Set("f_hmsaved", 1f);
            if (sit == 3) { MolakStats.Add("mn", 3f); MolakStats.Add("sl", 1f); }
            if (sit == 7) MolakStats.Add("mn", 10f);
            if (sit == 12) { MolakStats.Set("f_wanted", 1f); MolakStats.Add("sl", 1f); }
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
                if (sit == 12) { MolakStats.Add("sl", -1f); if (oldRank > 0) MolakStats.Set("rank", Mathf.Floor(oldRank * 1.15f)); }
            }
            MolakStats.Add("hp", -dmg);
            if (IsBossKind(kind)) MolakStats.Set("g_know_" + kind, 1f);   // 진 상대의 검로는 되감겨도 몸에 남는다
            MolakStats.Set("duelwin", 0f); MolakArt.EndDuel();
            MolakVfx.Blood(); MolakSfx.Hit(); MolakArt.Hurt();
            MolakStats.Set("tierup", 0f);
            MolakDuelCard.Show(false, foe.Name, "서열 변동 없음 · 체력 −" + Mathf.RoundToInt(dmg), MolakStats.Get("dtaint") > 0 ? (MolakStats.Get("taintkind") == 1 ? "독이 들었다" : "상처가 덧났다") : (MolakStats.Get("dinj") > 0 ? "부상 — 사흘" : ""));
        }
        MolakStats.GrowBody();
        MolakStats.ClampAll();
    }
}
