using UnityEngine;

/// <summary>
/// 한 생의 규칙 수치(균형). 예전에는 InitRun·DayTick·Regress·Hud 곳곳에 10·42·0.03·46 같은 숫자가 그대로 박혀 있어,
/// 균형을 손볼 때 같은 값을 여러 군데서 찾아 고쳐야 했다. 여기만 고치면 된다.
/// (나니 스크립트 안의 이야기 분기 숫자는 그 장면의 일부라 스크립트에 둔다.)
/// </summary>
public static class MolakRules
{
    // ---- 한 생의 길이 ----
    /// <summary>예순 날이 차면 관아 그물에 걸린다(포박).</summary>
    public const float RunDays = 60f;
    /// <summary>하단 띠의 그물 경고: 조여 온다 / 닫히기 직전.</summary>
    public const float NetWarnDay = 46f, NetCloseDay = 55f;
    /// <summary>혈랑대 야습 날 — 보통 / '혈랑대 이른 생'.</summary>
    public const float RaidDay = 42f, RaidDayEarly = 38f;
    /// <summary>0.4.85 지난 생에 본 수로 연 승부수의 최저 승산(언메이커: 알고도 지면 납득이 안 된다).</summary>
    public const int RiskKnowFloor = 85;
    /// <summary>이번 생의 바람을 처음 귀띔하는 날.</summary>
    public const float ThemeHintDay = 3f;
    /// <summary>귀띔하지 않음(바람이 생기기 전 저장을 불러왔을 때).</summary>
    public const float ThemeHintNever = 999f;

    // ---- 시작 능력치 (기본값. *PerLv 는 옛 깨달음 강화를 몸의 기억으로 옮길 때만 쓴다) ----
    public const float BaseHp = 10f, HpPerBody = 2f, HpPerVigor = 1f;
    public const float CorePerLv = 3f;
    public const float BaseSword = 2f, SwordPerLv = 2f;
    public const float BaseCoin = 3f, CoinPerLv = 5f;
    public const float StartCurse = 5f;
    /// <summary>채비로 고른 안식향이 처음부터 덜어 주는 저주.</summary>
    public const float IncenseCurseCut = 2f;

    // ---- 몸 ----
    /// <summary>체력 상한 = 기본 + floor((검술+내공) / 이 값).</summary>
    public const float BodyGrowthDivisor = 9f;

    // ---- 저주 ----
    public const float CurseMax = 100f;
    /// <summary>날마다 자라는 저주 뿌리 = (Base + PerDay × 날) × 가속 × 봉인 보정.
    /// 0.4.93 기준(출시 점검): 가만히 있어도 익는 속도 — 뿌리만으로는 쉰다섯째 날 무렵 익고(88), 예순 날 그물과 함께 끝난다(100 근처).
    /// 그보다 이른 사술 폭주는 고른 것(운기·사술·살기)에서 온다. 0.4.92까지는 뿌리만으로 쉰둘째 날에 100을 넘어,
    /// 아무것도 안 해도 종장(떠나기 전 밤 · 북행) 전에 죽었다(소크 r0492: 13생 중 5생이 사술 폭주, 46~61일).</summary>
    public const float CurseRootBase = 0.55f, CurseRootPerDay = 0.025f;
    /// <summary>날이 깊어지면 가속: 서른다섯째 날부터 ×1.3, 쉰째 날부터 또 ×1.5.</summary>
    public const float CurseAccelDay1 = 35f, CurseAccel1 = 1.3f, CurseAccelDay2 = 50f, CurseAccel2 = 1.5f;
    /// <summary>청심차 비방이 날마다 덜어 주는 양.</summary>
    public const float TeaCurseCut = 0.15f;
    /// <summary>봉인 문양 한 단계당 저주 성장 감소율, 소아의 봉인 배율.</summary>
    public const float SealCutPerLv = 0.12f, SoaSealMul = 0.85f;

    public static float CurseRoot (float day)
    {
        float accel = (day >= CurseAccelDay1 ? CurseAccel1 : 1f) * (day >= CurseAccelDay2 ? CurseAccel2 : 1f);
        return (CurseRootBase + CurseRootPerDay * day) * accel;
    }

    // ---- 숙식 ----
    /// <summary>객잔 방값: 처음엔 하루 1냥, 스물다섯째 날부터 2냥. 하진무가 차갑게 돌아서면 +1, 하진무가 없으면 3냥.</summary>
    public const float LodgeBase = 1f, LodgeLate = 2f, LodgeLateDay = 25f, LodgeColdExtra = 1f, LodgeNoHajin = 3f;
    /// <summary>그날의 기본 방값(사람 사정 — 하진무가 안 받음·차가워짐·없음 — 은 빼고).</summary>
    public static float LodgeOn (float day) { return day >= LodgeLateDay ? LodgeLate : LodgeBase; }
    /// <summary>0.4.91 비무 상금 기준(언메이커: 돈벌기가 빡세다 — 이긴 비무가 1냥이라 수련하는 밤마다 은자가 빠지기만 했다):
    /// 이긴 보통 비무 한 판 = 그날 방값 사흘치(밤 한 번이 사흘). 뒷골목·운주처럼 판이 큰 곳은 그 두 배. 이름난 판(호치·철산도)은 따로.</summary>
    public const float DuelPrizeNights = 3f, DuelPrizeBigMul = 2f;
    public static float DuelPrize (float day, bool big) { return LodgeOn(day) * DuelPrizeNights * (big ? DuelPrizeBigMul : 1f); }
    /// <summary>다리 밑의 솥(개방의 인연): 굶는 날 한 그릇을 얻는 간격(날).</summary>
    public const float GaeRiceGap = 4f;
    /// <summary>방값을 못 내면 굶는다 — 하루짜리 마감은 체력 -2, 며칠 마감은 -1(날마다).</summary>
    public const float StarveHpShort = 2f, StarveHpLong = 1f;

    // ---- 깨달음(회귀 정산) — 0.4.55부터 쓰지 않는다. 회귀의 보상은 MolakMemory(몸에 남는 것) ----
    /// <summary>깨달음 = floor(날/3) + floor((검술+내공)/6) + (서열에 올랐으면 2), 최소 1.</summary>
    public const float PtsPerDays = 3f, PtsPerStats = 6f, PtsRanked = 2f;
    public static float Enlightenment (float day, float sw, float ng, float rank)
    {
        return Mathf.Max(1f, Mathf.Floor(day / PtsPerDays) + Mathf.Floor((sw + ng) / PtsPerStats) + (rank > 0f ? PtsRanked : 0f));
    }

    // ---- 교주를 벤 생의 정산 ----
    /// <summary>등극 정산 깨달음 = ClearPtsBase + floor(날 / ClearPtsPerDays).</summary>
    public const float ClearPtsBase = 30f, ClearPtsPerDays = 2f;
    public static float ClearPoints (float day) { return ClearPtsBase + Mathf.Floor(day / ClearPtsPerDays); }

    // ---- 독·곪은 상처 ----
    /// <summary>날마다 깎는 체력: 곪음이 TaintHeavy 이상이면 TaintHpHeavy, 아니면 TaintHpLight. 마감마다 곪음이 하루치면 +1, 며칠치면 +2.</summary>
    public const float TaintHeavy = 4f, TaintHpHeavy = 2f, TaintHpLight = 1f, TaintGrowShort = 1f, TaintGrowLong = 2f;

    // ---- 진엔딩(합벽) ----
    /// <summary>검보를 품은 생이 교주전에 드는 서열. 대본(Molak02 첫 줄의 rank&lt;=30)과 같은 수여야 한다 — 노인의 귀띔 문장도 이 수를 쓴다.</summary>
    public const float TrueEndRank = 30f;

    // ---- 이번 생의 뜻(회귀 아침에 고르는 맹세) ----
    public const float VowReward = 4f;
    /// <summary>복수: 서열 이 안에 들면 이룬다.</summary>
    public const float VowRevengeRank = 1170f;   // 0.4.35: 보이는 서열 300위(= 안쪽 1,170)
    /// <summary>인연: 이 가운데 하나라도 맺었거나 소아 호감이 VowBondAff 이상이면 이룬다.</summary>
    public static readonly string[] VowBondFlags = { "f_soabond", "f_hajinbond", "f_herbbond", "f_gwakally", "f_yongally", "f_amhally", "f_jinally", "f_chamally" };
    public const float VowBondAff = 3f;
    /// <summary>생존: 그물이 조여 오는 날까지 버티면 이룬다.</summary>
    public const float VowSurviveDay = NetWarnDay;

    // ---- 강호의 격(格): 날이 갈수록 강호의 칼이 무거워진다 ----
    /// <summary>그날 개봉 바닥에서 '보통'으로 통하는 수준. 처음 사는 생의 평범한 성장(소크 중앙값)에 맞췄다.
    /// 판정 문턱 '[검술+2]'는 이 격보다 둘 높다는 뜻 — 이레째의 사냥꾼과 쉰째 날의 사냥꾼은 같은 칼이 아니다.</summary>
    public static float Bar (string stat, float day)
    {
        switch (stat)
        {
            case "sw": return Mathf.Round(5f + 0.45f * day);
            case "ng": return Mathf.Round(4f + 0.30f * day);
            case "sl": return Mathf.Round(2f + 0.20f * day);
        }
        return 0f;
    }

    // ---- 0.4.94 몸이 사술을 버틴다(언메이커: 내공이 큰 의미가 있어? · 사용처가 너무 언밸런스) ----
    /// <summary>기준: 내공은 몸 안에서 문양을 밀어내는 힘, 살기는 문양에 먹이를 주는 힘.
    /// 그날의 격(Bar)보다 내공이 한 칸 높을 때마다 하룻밤 뿌리가 NgCurseStep 만큼 덜 자라고, 낮으면 더 자란다(±NgCurseCap 에서 자름).
    /// 살기가 격보다 높으면 한 칸마다 SlCurseStep 만큼 더 자란다(SlCurseCap 까지). 격에 맞춰 자란 몸이면 1배 — 사술 시계(쉰다섯째 날 익음)는 그대로다.</summary>
    public const float NgCurseStep = 0.03f, NgCurseCap = 0.30f, SlCurseStep = 0.02f, SlCurseCap = 0.20f;
    public static float BodyCurseFactor (float day, float ng, float sl)
    {
        float f = 1f - Mathf.Clamp((ng - Bar("ng", day)) * NgCurseStep, -NgCurseCap, NgCurseCap);
        f += Mathf.Clamp((sl - Bar("sl", day)) * SlCurseStep, 0f, SlCurseCap);
        return Mathf.Max(0.5f, f);
    }

    /// <summary>0.4.94 그날의 격에 맞춰 자란 몸의 비무 힘(MolakDuel.MyPower 와 같은 셈). 내 힘에 맞춰 오던 상대(업보의 칼·자객·소아·남궁현·단목결·야습조)는
    /// 이것과 내 힘의 가운데(FoeSelfShare)에 선다 — 수련한 만큼의 절반은 승산으로 남는다(예전엔 내 힘을 그대로 따라와 검술·내공이 승산에 남지 않았다).</summary>
    public const float FoeSelfShare = 0.5f;
    public static float BarPower (float day)
    {
        return Bar("sw", day) * 1.1f + Bar("ng", day) * 1.3f + 3f + (Bar("sl", day) >= 5f ? 2f : 0f);
    }

    /// <summary>승부수의 문턱. spec 이 "+2"/"-1"/"0" 꼴이면 그날의 격에 더하고, "12" 꼴이면 그 수 그대로.</summary>
    public static float RiskNeed (string stat, string spec, float day)
    {
        spec = (spec ?? "").Trim();
        if (spec.Length == 0) return Bar(stat, day);
        bool rel = spec[0] == '+' || spec[0] == '-' || spec == "0";
        float n; float.TryParse(spec, out n);
        return rel ? Mathf.Max(1f, Bar(stat, day) + n) : n;
    }

    /// <summary>승산(勝算), 할 단위(10~90%). 확실한 판은 없다 — 아홉 할을 넘지 않고, 한 할 밑으로 내려가지도 않는다.</summary>
    public static int RiskOdds (string stat, float have, float need)
    {
        float k = stat == "sl" ? 8f : 6f;
        float p = 55f + (have - need) * k;
        return Mathf.Clamp(Mathf.RoundToInt(p / 10f) * 10, 10, 90);
    }

    public static string StatName (string stat)
    {
        switch (stat) { case "sw": return "검술"; case "ng": return "내공"; case "sl": return "살기"; }
        return stat;
    }
    public static string StatKey (string korean)
    {
        switch (korean) { case "검술": return "sw"; case "내공": return "ng"; case "살기": return "sl"; }
        return null;
    }
    public static string OddsText (int p) { return "열에 " + MolakDuel.Ten(Mathf.Clamp(Mathf.RoundToInt(p / 10f), 0, 10)); }   // 0.4.68 "9할" → "열에 아홉"

    // ---- 회귀 상점: 이번 생의 채비 ----
    public const int PrepBondCost = 3, PrepItemCost = 2, RedoCost = 12;
    public static readonly string[] PrepBondNames = { "없음", "백초 할멈의 단골", "거지 아이", "쥐눈이 장" };
    public static readonly string[] PrepItemNames = { "없음", "죽립", "금창약", "안식향" };

    // ---- 이번 생의 바람(회차 테마) ----
    public const int ThemeClear = 0, ThemeMonsoon = 1, ThemePlague = 2, ThemeTax = 3, ThemeEarlyRaid = 4;
    /// <summary>두 번째 생부터 맑은 생이 나올 확률은 1/ClearOdds.</summary>
    public const int ThemeClearOdds = 5;

    /// <summary>바람마다 잦아지거나 드물어지는 사건. 사건 Id 끝의 '*'는 그 이름으로 시작하는 사건 전부.
    /// 한 사건에 여러 줄이 맞으면 위에 있는 줄이 이긴다.</summary>
    public struct ThemeBias { public int Theme; public string[] Ids; public float Mul; }
    public static readonly ThemeBias[] ThemeBiases = {
        new ThemeBias { Theme = ThemeMonsoon,   Mul = 3f,   Ids = new[] { "E_Flood" } },
        new ThemeBias { Theme = ThemeMonsoon,   Mul = 2.5f, Ids = new[] { "E_Rain*" } },
        new ThemeBias { Theme = ThemeMonsoon,   Mul = 1.6f, Ids = new[] { "E_Ferry", "E_Ferry2", "E_Ferry3" } },
        new ThemeBias { Theme = ThemePlague,    Mul = 1.8f, Ids = new[] { "E_Herb*" } },
        new ThemeBias { Theme = ThemePlague,    Mul = 2.5f, Ids = new[] { "E_Funeral", "E_Emptytown", "E_Lastletter", "E_Vigil" } },
        new ThemeBias { Theme = ThemePlague,    Mul = 1.6f, Ids = new[] { "E_Monk*", "E_Beggar*", "E_Gaebang*" } },
        new ThemeBias { Theme = ThemePlague,    Mul = 0.4f, Ids = new[] { "E_Gamble*", "E_Nightmarket*", "E_Caravan", "E_Peddler*" } },
        new ThemeBias { Theme = ThemeTax,       Mul = 2.5f, Ids = new[] { "E_Bloodtax*", "E_Hostage", "E_HerbTax", "E_JangSell" } },
        new ThemeBias { Theme = ThemeTax,       Mul = 1.6f, Ids = new[] { "E_Guard*", "E_HajinNight", "E_HerbBurn" } },
        new ThemeBias { Theme = ThemeEarlyRaid, Mul = 2.5f, Ids = new[] { "E_Hunter*", "E_Search", "E_SoaPoster" } },
        new ThemeBias { Theme = ThemeEarlyRaid, Mul = 1.6f, Ids = new[] { "E_Spy*", "E_Cultists*", "E_Cup" } },
    };

    public static float ThemeMul (int theme, string eventId)
    {
        foreach (var b in ThemeBiases)
        {
            if (b.Theme != theme) continue;
            foreach (var pat in b.Ids)
                if (pat.EndsWith("*") ? eventId.StartsWith(pat.Substring(0, pat.Length - 1)) : eventId == pat) return b.Mul;
        }
        return 1f;
    }
}
