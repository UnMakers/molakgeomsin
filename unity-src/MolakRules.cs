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
    /// <summary>이번 생의 바람을 처음 귀띔하는 날.</summary>
    public const float ThemeHintDay = 3f;
    /// <summary>귀띔하지 않음(바람이 생기기 전 저장을 불러왔을 때).</summary>
    public const float ThemeHintNever = 999f;

    // ---- 시작 능력치 (깨달음 강화 g_up_* 한 단계당) ----
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
    /// <summary>날마다 자라는 저주 뿌리 = (Base + PerDay × 날) × 가속 × 봉인 보정.</summary>
    public const float CurseRootBase = 0.7f, CurseRootPerDay = 0.03f;
    /// <summary>날이 깊어지면 가속: 서른다섯째 날부터 ×1.4, 쉰째 날부터 또 ×1.6.</summary>
    public const float CurseAccelDay1 = 35f, CurseAccel1 = 1.4f, CurseAccelDay2 = 50f, CurseAccel2 = 1.6f;
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
    /// <summary>방값을 못 내면 굶는다 — 하루짜리 마감은 체력 -2, 며칠 마감은 -1(날마다).</summary>
    public const float StarveHpShort = 2f, StarveHpLong = 1f;

    // ---- 깨달음(회귀 정산) ----
    /// <summary>깨달음 = floor(날/3) + floor((검술+내공)/6) + (서열에 올랐으면 2), 최소 1.</summary>
    public const float PtsPerDays = 3f, PtsPerStats = 6f, PtsRanked = 2f;
    public static float Enlightenment (float day, float sw, float ng, float rank)
    {
        return Mathf.Max(1f, Mathf.Floor(day / PtsPerDays) + Mathf.Floor((sw + ng) / PtsPerStats) + (rank > 0f ? PtsRanked : 0f));
    }

    // ---- 이번 생의 뜻(회귀 아침에 고르는 맹세) ----
    public const float VowReward = 4f;
    /// <summary>복수: 서열 이 안에 들면 이룬다.</summary>
    public const float VowRevengeRank = 1000f;
    /// <summary>인연: 이 가운데 하나라도 맺었거나 소아 호감이 VowBondAff 이상이면 이룬다.</summary>
    public static readonly string[] VowBondFlags = { "f_soabond", "f_hajinbond", "f_herbbond", "f_gwakally", "f_yongally", "f_amhally", "f_jinally" };
    public const float VowBondAff = 3f;
    /// <summary>생존: 그물이 조여 오는 날까지 버티면 이룬다.</summary>
    public const float VowSurviveDay = NetWarnDay;

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
