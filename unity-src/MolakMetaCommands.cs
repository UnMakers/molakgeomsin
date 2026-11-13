using System.Collections.Generic;
using System.Linq;
using Naninovel;
using UnityEngine;

/// <summary>@initrun — 회차 시작: 일반 변수 전부 리셋 후 깨달음(전역 g_up_*) 반영 시작치 설정.</summary>
[Alias("initrun")]
public class InitRunCommand : Command
{
    /// <summary>코드에서만 쓰는 전역 수치(스크립트에는 나오지 않아 자동 목록에 없는 것).</summary>
    static readonly string[] CodeGlobals = {
        "g_pts", "g_runs", "g_clears", "g_lastday", "g_bestday", "g_bestrank", "g_hapbo", "g_samecount", "g_lasttheme",
        "g_next_bond", "g_next_item", "g_redo", "g_lk_nameless", "g_ls_nameless",
    };

    /// <summary>전역(g_) 기본값 보장 — 회차 시작과 옛 저장 불러오기 때 모두 쓴다.
    /// 되감기지 않는 앎(지난 생들에서 보고 듣고 죽으며 알게 된 것)은 조건식에서 쓰기 전에 반드시 있어야 한다.
    /// 목록을 손으로 적어 두면 새 변수를 빠뜨리기 쉬워, 스크립트에서 뽑은 목록(MolakVarList.Globals)과 표에서 끌어낸 이름을 쓴다.</summary>
    public static void EnsureGlobals ()
    {
        foreach (var n in MolakVarList.Globals) MolakStats.EnsureNum(n);
        foreach (var n in CodeGlobals) MolakStats.EnsureNum(n);
        foreach (var u in ShopInfoCommand.Ups) MolakStats.EnsureNum("g_up_" + u.key);                  // 깨달음 강화 단계
        foreach (var k in DuelCommand.BossKinds) { MolakStats.EnsureNum("g_know_" + k); MolakStats.EnsureNum("g_died_" + k); } // 진 상대의 검로·죽은 기억
        foreach (var e in MolakEnd.All) MolakStats.EnsureNum("g_end_" + e.Id);                            // 본 말로
        foreach (var le in MolakLedger.All)
            foreach (var pre in new[] { "g_lk_", "g_ld_", "g_ls_" }) MolakStats.EnsureNum(pre + le.Id);      // 강호록
        MolakStats.EnsureStr("g_lastend");
        MolakStats.EnsureStr("g_lastcause");
    }

#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        var vars = Engine.GetServiceOrErr<ICustomVariableManager>();
        EnsureGlobals();
        // 전역(g_)이 아닌 변수 전부 제거 수준으로 0 초기화
        var snapshot = vars.Variables.Where(v => !v.Name.StartsWith("g_")).Select(v => (v.Name, v.Value.Type)).ToList();
        foreach (var (n, ty) in snapshot)
            vars.SetVariableValue(n, ty == CustomVariableValueType.String ? new CustomVariableValue("none") : new CustomVariableValue(0f));
        float up (string k) => MolakStats.Get("g_up_" + k);
        float hp0 = MolakRules.BaseHp + MolakRules.HpPerBody * up("body") + MolakRules.HpPerVigor * up("vigor");
        MolakStats.Set("hpmax", hp0);
        MolakStats.Set("hp", hp0);
        if (up("tea") > 0) MolakStats.Set("f_tea", 1f);      // 회귀 상점: 청심차 비방
        if (up("lodge") > 0) MolakStats.Set("f_hajinfree", 1f); // 회귀 상점: 빈칸 방값
        MolakStats.Set("ng", up("core") * MolakRules.CorePerLv);
        MolakStats.Set("sw", MolakRules.BaseSword + up("sword") * MolakRules.SwordPerLv);
        MolakStats.Set("sl", 0);
        MolakStats.Set("mn", MolakRules.BaseCoin + up("coin") * MolakRules.CoinPerLv);
        MolakStats.Set("curse", MolakRules.StartCurse);
        MolakStats.Set("day", 1);
        // 이번 생의 바람(회차 테마): 첫 생은 맑음. 그 뒤로는 다섯에 하나 맑음, 나머지 넷 중 하나 — 지난 생과 겹치지 않게.
        {
            int th = MolakRules.ThemeClear;
            if (MolakStats.Get("g_runs") >= 1 && Random.Range(0, MolakRules.ThemeClearOdds) > 0)
            {
                int last = Mathf.RoundToInt(MolakStats.Get("g_lasttheme"));
                do { th = Random.Range(MolakRules.ThemeMonsoon, MolakRules.ThemeEarlyRaid + 1); } while (th == last);
            }
            MolakStats.Set("theme", th);
            MolakStats.Set("g_lasttheme", th);
            MolakStats.Set("raidday", th == MolakRules.ThemeEarlyRaid ? MolakRules.RaidDayEarly : MolakRules.RaidDay);
            MolakStats.Set("thday", MolakRules.ThemeHintDay);
        }
        // 이번 생의 채비: 상점에서 골라 둔 인연·물건을 첫 아침에 쥐여 주고 비운다
        {
            int b = Mathf.RoundToInt(MolakStats.Get("g_next_bond")), it = Mathf.RoundToInt(MolakStats.Get("g_next_item"));
            if (b == 1) { MolakStats.Set("f_herbfriend", 1f); MolakStats.Set("f_herbpulse", 1f); }
            if (b == 2) { MolakStats.Set("f_alms", 1f); MolakStats.Set("f_gaekid", 1f); MolakStats.Set("rep_gae", 1f); }
            if (b == 3) { MolakStats.Set("f_jangfriend", 1f); }
            if (it == 1) MolakStats.Set("f_hat", 1f);
            if (it == 2) MolakStats.Set("f_salve", 1f);
            if (it == 3) { MolakStats.Set("f_incense", 1f); MolakStats.Add("curse", -MolakRules.IncenseCurseCut); }
            MolakStats.Set("g_next_bond", 0f); MolakStats.Set("g_next_item", 0f);
        }
        MolakStats.Set("rank", 0);
        if (MolakStats.Get("g_hapbo") > 0) MolakStats.Set("f_hapbyeok", 1f);   // 0.4.9: 한 번 읽은 합벽 검보는 되감기지 않는다
        MolakStats.Set("seallv", up("seal"));
        MolakKnow.Snapshot();   // 이번 생이 시작될 때 이미 아는 것 — 죽을 때 "되감기지 않는 것"을 가려낸다
        MolakStats.Set("runs", MolakStats.Get("g_runs"));
    }
}

/// <summary>@daytick n:3 — 며칠 마감: 날마다 저주 뿌리 성장(날이 갈수록 가속)·숙식, 독·상처는 마감마다 두 칸. starved 변수 설정.</summary>
[Alias("daytick")]
public class DayTickCommand : Command
{
    public IntegerParameter N;
#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        int n = Assigned(N) ? Mathf.Max(1, N.Value) : 1;
        MolakArt.Scene("morning"); MolakArt.Char(null); MolakVfx.Scene("day"); MolakFlow.Break();
        MolakStats.Set("starved", 0f);
        for (int i = 0; i < n; i++)
        {
            MolakStats.Add("day", 1f);
            float day = MolakStats.Get("day");
            float root = MolakRules.CurseRoot(day) * MolakStats.CurseFactor();
            if (MolakStats.Get("f_tea") > 0) root = Mathf.Max(0f, root - MolakRules.TeaCurseCut); // 청심차 비방: 매일 반 김
            MolakStats.Add("curse", root);
            if (MolakStats.Get("f_ringclosed") > 0) MolakStats.Add("f_ringclosed", -1f); // 비무판 폐쇄 일수
            if (MolakStats.Get("injury") > 0) MolakStats.Add("injury", -1f);                 // 부상: 사흘이면 아문다
            if (MolakStats.Get("taint") > 0)                                                 // 독·곪은 상처: 날마다 몸을 깎는다
                MolakStats.Add("hp", MolakStats.Get("taint") >= 4f ? -2f : -1f);
            float lodg = day >= MolakRules.LodgeLateDay ? MolakRules.LodgeLate : MolakRules.LodgeBase;
            if (MolakStats.Get("f_hajinfree") > 0) lodg = 0f;                          // 하진무가 방값을 안 받기로 함
            if (MolakStats.Get("f_hajincold") > 0) lodg += MolakRules.LodgeColdExtra;  // 징세조 앞에서 외면한 뒤로 방값 패가 바뀌었다
            if (MolakStats.Get("f_hajindead") > 0) lodg = MolakRules.LodgeNoHajin;     // 하진무가 없는 객잔: 새 주인은 빈칸을 모른다
            if (MolakStats.Get("mn") >= lodg) MolakStats.Add("mn", -lodg);
            else if (MolakStats.Get("f_gaerice") > 0 && day - MolakStats.Get("gaefedday") >= MolakRules.GaeRiceGap)
            {   // 다리 밑의 솥: 굶는 날 거지들이 한 그릇 덜어 준다(나흘에 한 번). 아침에 그 장면이 나온다.
                MolakStats.Set("mn", 0f); MolakStats.Set("gaefed", 1f); MolakStats.Set("gaefedday", day);
            }
            else { MolakStats.Set("mn", 0f); MolakStats.Add("hp", -(n >= 3 ? MolakRules.StarveHpLong : MolakRules.StarveHpShort)); MolakStats.Set("starved", 1f); }
        }
        // 독·곪은 상처는 마감마다 깊어진다(며칠 한 바퀴면 두 칸 — 손쓸 기회는 한두 바퀴)
        if (MolakStats.Get("taint") > 0) MolakStats.Add("taint", n >= 3 ? 2f : 1f);
        MolakStats.ClampAll();
#if UNITY_EDITOR
        // 에디터 전용: 은자 흐름 기록(소크 분석용)
        try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), "Build", "soak_money.csv"), MolakStats.Get("g_runs").ToString("0") + "," + MolakStats.Get("day").ToString("0") + "," + MolakStats.Get("mn").ToString("0") + "," + MolakStats.Get("rank").ToString("0") + "\n"); } catch { }
#endif
    }
}

/// <summary>@regress cause:\"사술\" — 사망 정산: 깨달음 계산, 전역 누적, 표시 변수 설정.</summary>
[Alias("regress")]
public class RegressCommand : Command
{
    public StringParameter Cause;

    public override async Awaitable Execute (ExecutionContext ctx)
    {
        float day = MolakStats.Get("day"), sw = MolakStats.Get("sw"), ng = MolakStats.Get("ng");
        float gained = MolakRules.Enlightenment(day, sw, ng, MolakStats.Get("rank"));
        MolakStats.Add("g_pts", gained);
        MolakStats.Add("g_runs", 1f);
        MolakStats.Set("gained", gained);
        MolakLedger.Accumulate();
        MolakAutoSave.LogLife(gained);
        await Engine.GetServiceOrErr<IStateManager>().SaveGlobal();
    }
}

/// <summary>@clearrun — 교주 격파 정산.</summary>
[Alias("clearrun")]
public class ClearRunCommand : Command
{
    public override async Awaitable Execute (ExecutionContext ctx)
    {
        float gained = 30f + Mathf.Floor(MolakStats.Get("day") / 2f);
        MolakStats.Add("g_pts", gained);
        MolakStats.Add("g_clears", 1f);
        MolakStats.Set("gained", gained);
        await Engine.GetServiceOrErr<IStateManager>().SaveGlobal();
    }
}

/// <summary>@shopinfo — 깨달음 상점 표시 변수(cost_*, lv_*) 갱신.</summary>
[Alias("shopinfo")]
public class ShopInfoCommand : Command
{
    public static readonly (string key, int baseCost, int step, int max)[] Ups = {
        ("sword", 3, 2, 5), ("body", 3, 2, 5), ("core", 4, 2, 5), ("coin", 2, 1, 4), ("seal", 5, 3, 4), ("mem", 6, 4, 3),
        ("tea", 10, 0, 1), ("lodge", 8, 0, 1), ("vigor", 6, 2, 10)
    };

#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        foreach (var u in Ups)
        {
            float lv = MolakStats.Get("g_up_" + u.key);
            MolakStats.Set("lv_" + u.key, lv);
            MolakStats.Set("cost_" + u.key, u.baseCost + lv * u.step);
            MolakStats.Set("max_" + u.key, lv >= u.max ? 1f : 0f);
        }
        var bonds = MolakRules.PrepBondNames; var items = MolakRules.PrepItemNames;
        MolakStats.SetStr("prepbond", bonds[Mathf.Clamp(Mathf.RoundToInt(MolakStats.Get("g_next_bond")), 0, bonds.Length - 1)]);
        MolakStats.SetStr("prepitem", items[Mathf.Clamp(Mathf.RoundToInt(MolakStats.Get("g_next_item")), 0, items.Length - 1)]);
    }
}

/// <summary>@buyup kind:sword — 깨달음 소모 강화. buyok 변수로 결과 보고.</summary>
[Alias("buyup")]
public class BuyUpCommand : Command
{
    [RequiredParameter] public StringParameter Kind;

    public override async Awaitable Execute (ExecutionContext ctx)
    {
        // 이번 생의 채비(한 생에 하나씩) · 되묻기(한 번)
        string k = Kind.Value;
        if (k.StartsWith("bond") || k.StartsWith("item") || k == "redo")
        {
            int pcost = k.StartsWith("bond") ? MolakRules.PrepBondCost : k.StartsWith("item") ? MolakRules.PrepItemCost : MolakRules.RedoCost;
            string slot = k.StartsWith("bond") ? "g_next_bond" : k.StartsWith("item") ? "g_next_item" : "g_redo";
            if (MolakStats.Get(slot) > 0 || MolakStats.Get("g_pts") < pcost) { MolakStats.Set("buyok", 0f); return; }
            MolakStats.Add("g_pts", -pcost);
            MolakStats.Set(slot, k == "redo" ? 1f : float.Parse(k.Substring(4)));
            MolakStats.Set("buyok", 1f);
            await Engine.GetServiceOrErr<IStateManager>().SaveGlobal();
            return;
        }
        var u = ShopInfoCommand.Ups.FirstOrDefault(x => x.key == Kind.Value);
        if (u.key == null) { MolakStats.Set("buyok", 0f); return; }
        float lv = MolakStats.Get("g_up_" + u.key);
        float cost = u.baseCost + lv * u.step;
        if (lv >= u.max || MolakStats.Get("g_pts") < cost) { MolakStats.Set("buyok", 0f); return; }
        MolakStats.Add("g_pts", -cost);
        MolakStats.Set("g_up_" + u.key, lv + 1);
        MolakStats.Set("buyok", 1f);
        await Engine.GetServiceOrErr<IStateManager>().SaveGlobal();
    }
}

/// <summary>
/// 사건 조건식 평가 — 정의표(MolakEventDefs)의 Cond 문자열.
/// 토큰을 '&'로 연결: name(>0) / !name(<=0) / name>=N / name<=N / name>N / name<N / name==N
/// </summary>
public static class MolakCond
{
    public static bool Eval (string cond)
    {
        if (string.IsNullOrEmpty(cond)) return true;
        foreach (var raw in cond.Split('&'))
        {
            var tok = raw.Trim();
            if (tok.Length == 0) continue;
            bool neg = tok[0] == '!';
            if (neg) tok = tok.Substring(1);
            string[] ops = { ">=", "<=", "==", ">", "<" };
            string op = null; int oi = -1;
            foreach (var o in ops) { oi = tok.IndexOf(o); if (oi > 0) { op = o; break; } }
            bool ok;
            if (op == null) ok = MolakStats.Get(tok) > 0f;
            else
            {
                var name = tok.Substring(0, oi);
                float n; float.TryParse(tok.Substring(oi + op.Length), out n);
                float v = MolakStats.Get(name);
                ok = op == ">=" ? v >= n : op == "<=" ? v <= n : op == "==" ? Mathf.Approximately(v, n) : op == ">" ? v > n : v < n;
            }
            if (neg) ok = !ok;
            if (!ok) return false;
        }
        return true;
    }
}

/// <summary>
/// @pickevent chance:55 slot:day — 사건 추첨 v2. nextevent 변수에 라벨명 또는 none.
/// 규칙: 한 회차에 같은 사건은 한 번만(seen_*). 일차 창(MinDay~MaxDay), 낮/밤 슬롯, 조건식, 선행 사건+경과일(After/Gap), 가중치.
/// 후보가 있는데 추첨에 실패하면 evdry 누적 → 다음 추첨 확률 +20/회 (빈 날 연속 방지).
/// </summary>
[Alias("pickevent")]
public class PickEventCommand : Command
{
    public IntegerParameter Chance;
    public StringParameter Slot;

    const int DryBoost = 20;

    /// <summary>가중치 × 이번 생의 바람(theme). 정수로 10배해서 쓴다.</summary>
    static int W (MolakEventDefs.Def d) { return Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(1, d.Weight) * 10f * ThemeMul(d.Id) * RunMul(d.Id))); }
    // 0.4.21 생을 넘는 사건 기억: 앞선 생들에서 자주 본 사건은 덜, 아직 못 본 사건은 더 나온다(첫 생에는 그대로).
    // g_ev_<id>는 나니노벨 전역 변수(g_ 접두)라 회귀해도 남는다.
    public static float RunMul (string id)
    {
        if (MolakStats.Get("g_runs") < 1f) return 1f;
        float k = MolakStats.Get("g_ev_" + id);
        if (k <= 0f) return 1.8f;
        return Mathf.Max(0.35f, 1f / (1f + 0.5f * k));
    }
    public static float ThemeMul (string id) { return MolakRules.ThemeMul(Mathf.RoundToInt(MolakStats.Get("theme")), id); }

#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        void SetNext (string v) => MolakStats.SetStr("nextevent", v);
        float day = MolakStats.Get("day");
        char slot = Assigned(Slot) && !string.IsNullOrEmpty(Slot.Value) ? char.ToLowerInvariant(Slot.Value[0]) : 'a';

        var cands = new List<MolakEventDefs.Def>();
        foreach (var d in MolakEventDefs.Table)
        {
            if (MolakStats.Get("seen_" + d.Id) > 0f) continue;
            if (day < d.MinDay || (d.MaxDay > 0 && day > d.MaxDay)) continue;
            if (d.Slot != 'a' && slot != 'a' && d.Slot != slot) continue;
            if (!MolakCond.Eval(d.Cond)) continue;
            if (!string.IsNullOrEmpty(d.After))
            {
                if (MolakStats.Get("seen_" + d.After) <= 0f) continue;
                if (day - MolakStats.Get("seenday_" + d.After) < d.Gap) continue;
            }
            cands.Add(d);
        }
        if (cands.Count == 0) { SetNext("none"); return; }

        float dry = MolakStats.Get("evdry");
        int chance = Assigned(Chance) ? Chance.Value : 100;
        int eff = Mathf.Min(100, chance + DryBoost * Mathf.RoundToInt(dry));
        if (Random.Range(1, 101) > eff) { MolakStats.Set("evdry", dry + 1f); SetNext("none"); return; }

        int total = cands.Sum(c => W(c));
        int r = Random.Range(0, total);
        var pick = cands[cands.Count - 1];
        foreach (var c in cands) { r -= W(c); if (r < 0) { pick = c; break; } }

        MolakStats.Set("seen_" + pick.Id, 1f);
        MolakStats.Set("seenday_" + pick.Id, day);
        MolakStats.Add("g_ev_" + pick.Id, 1f);
        MolakStats.Set("evdry", 0f);
        MolakStats.Add("evcount", 1f);
        SetNext(pick.Id);
    }
}

/// <summary>
/// @pickflavor pool:morning — 아침/밤 풍경 문장 추첨. 회차 내 미출현 우선, 일차·조건 필터.
/// morning → mrand, night → nrand (1부터 시작하는 표 인덱스).
/// </summary>
[Alias("pickflavor")]
public class PickFlavorCommand : Command
{
    [RequiredParameter] public StringParameter Pool;

#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        bool morning = Pool.Value == "morning";
        var table = morning ? MolakEventDefs.Morning : MolakEventDefs.Night;
        string outVar = morning ? "mrand" : "nrand";
        string seenPrefix = morning ? "fl_m_" : "fl_n_";
        float day = MolakStats.Get("day");
        var elig = new List<int>(); var unseen = new List<int>();
        for (int i = 0; i < table.Length; i++)
        {
            var f = table[i];
            if (day < f.MinDay || (f.MaxDay > 0 && day > f.MaxDay)) continue;
            if (!MolakCond.Eval(f.Cond)) continue;
            elig.Add(i);
            if (MolakStats.Get(seenPrefix + i) <= 0f) unseen.Add(i);
        }
        var pool = unseen.Count > 0 ? unseen : elig;
        int idx = pool.Count > 0 ? pool[Random.Range(0, pool.Count)] : 0;
        MolakStats.Set(seenPrefix + idx, 1f);
        MolakStats.Set(outVar, idx + 1);
    }
}

/// <summary>@lifenote — 0.4.8 말로 직후 한 장: 이번 생의 기록(버틴 날·서열·깨달음), 지난 생과 견줌,
/// 같은 사인으로 두 번 죽었을 때 노인의 한 마디, 몇 생째 진말로에 닿지 못할 때 길의 다음 조각.
/// 표시 변수: lifesum · lifecmp/lifecmpc · deathtip/deathtipc · truehint/truehintc</summary>
[Alias("lifenote")]
public class LifeNoteCommand : Command
{
    /// <summary>같은 사인으로 두 번 죽었을 때 노인의 한 마디 — Resources/MolakData/death_tips.txt</summary>
    static IReadOnlyDictionary<string, string> Tips => MolakData.Map("death_tips");
    static readonly HashSet<string> NotDeaths = new HashSet<string> { "등극", "합벽", "성사", "순검", "원행", "재건", "도주" };

    static string Days (float d)
    {
        if (d <= 3f) return "사흘을 넘기지 못했다";
        if (d < 10f) return "열흘을 채우지 못했다";
        if (d < 20f) return "보름 남짓 버텼다";
        if (d < 30f) return "스무 날 남짓 버텼다";
        if (d < 40f) return "한 달 남짓 버텼다";
        if (d < 50f) return "마흔 날 남짓 버텼다";
        if (d < MolakRules.RunDays) return "쉰 날을 넘겼다";
        return "예순 날을 다 채웠다";
    }

    public override async Awaitable Execute (ExecutionContext ctx)
    {
        float day = MolakStats.Get("day"), rank = MolakStats.Get("rank"), gained = MolakStats.Get("gained");
        string cause = MolakStats.Str("deathcause");
        // 이번 생의 뜻(회귀 아침에 고른 것): 이루면 깨달음 +4
        int vow = Mathf.RoundToInt(MolakStats.Get("vow"));
        string vowline = "";
        if (vow > 0)
        {
            bool ok = vow == 1 ? (rank > 0f && rank <= MolakRules.VowRevengeRank)
                    : vow == 2 ? (MolakStats.Get("aff") >= MolakRules.VowBondAff || System.Array.Exists(MolakRules.VowBondFlags, f => MolakStats.Get(f) > 0f))
                    : day >= MolakRules.VowSurviveDay;
            string name = vow == 1 ? "복수" : vow == 2 ? "인연" : "생존";
            if (ok)
            {
                MolakStats.Add("g_pts", MolakRules.VowReward); gained += MolakRules.VowReward; MolakStats.Set("gained", gained);
                vowline = "이번 생의 뜻(" + name + ")을 이루었다 — 깨달음 +" + MolakRules.VowReward.ToString("0");
            }
            else vowline = "이번 생의 뜻(" + name + ")은 이루지 못했다.";
        }
        MolakStats.SetStr("vowline", vowline);
        MolakStats.Set("vowc", vowline.Length > 0 ? 1f : 0f);
        // 이번 생의 기록
        string sum = "이번 생 — " + Days(day) + " · " + (rank > 0 && rank <= MolakDuel.ListEdge ? "서열 " + MolakDuel.RankText(rank) : "서열록에 이름 없음") + " · 깨달음 +" + gained.ToString("0");
        MolakStats.SetStr("lifesum", sum);
        // 지난 생과 견줌(두 번째 생부터)
        float lastDay = MolakStats.Get("g_lastday"), bestDay = MolakStats.Get("g_bestday"), bestRank = MolakStats.Get("g_bestrank");
        var cmp = new List<string>();
        if (MolakStats.Get("g_runs") >= 2f && lastDay > 0f)
        {
            if (day > bestDay) cmp.Add("가장 오래 버틴 생이다");
            else if (day > lastDay + 2f) cmp.Add("지난 생보다 오래 버텼다");
            if (rank > 0f && (bestRank <= 0f || rank < bestRank)) cmp.Add("가장 높이 오른 생이다");
        }
        MolakStats.SetStr("lifecmp", string.Join(". ", cmp) + (cmp.Count > 0 ? "." : ""));
        MolakStats.Set("lifecmpc", cmp.Count);
        MolakStats.Set("g_lastday", day);
        if (day > bestDay) MolakStats.Set("g_bestday", day);
        if (rank > 0f && (bestRank <= 0f || rank < bestRank)) MolakStats.Set("g_bestrank", rank);
        // 같은 자리에서 두 번 — 노인의 한 마디
        string lastCause = MolakStats.Str("g_lastcause");
        string tip = "";
        if (!string.IsNullOrEmpty(cause) && cause == lastCause && !NotDeaths.Contains(cause)) Tips.TryGetValue(cause, out tip);
        tip = tip ?? "";
        MolakStats.SetStr("deathtip", tip);
        MolakStats.Set("deathtipc", tip.Length > 0 ? 1f : 0f);
        MolakStats.Set("g_samecount", (!string.IsNullOrEmpty(cause) && cause == lastCause) ? MolakStats.Get("g_samecount") + 1f : 1f);
        MolakStats.SetStr("g_lastcause", string.IsNullOrEmpty(cause) ? "none" : cause);
        // 진말로 길의 다음 조각: 세 생째부터, 노인의 한 마디가 없을 때만, 이번 생에서 막힌 자리 하나
        string hint = "";
        if (tip.Length == 0 && MolakStats.Get("g_runs") >= 3f && MolakStats.Get("g_hapdone") < 1f)
        {
            bool hap = MolakStats.Get("f_hapbyeok") > 0f, bond = MolakStats.Get("f_soabond") > 0f || MolakStats.Get("aff") >= 5f;
            if (MolakStats.Get("g_redverse") < 1f) hint = "'적운의 아이 곁에서 혈랑대의 밤을 맞거라. 지붕 위에 남는 것이 있느니라.'";
            else if (hap && !bond) hint = "'검보는 두 사람의 검이다. 그 아이가 등을 맡길 만큼 곁에 두거라.'";
            else if (hap && MolakStats.Get("f_wolfdown") < 1f) hint = "'교주의 산문은 혈쇄문의 사슬을 끊은 자에게만 열리느니라.'";
            else if (hap && (rank <= 0f || rank > 30f)) hint = "'검보를 품었거든 서열은 서른 안이면 족하다.'";
        }
        MolakStats.SetStr("truehint", hint);
        MolakStats.Set("truehintc", hint.Length > 0 ? 1f : 0f);
        try { await Engine.GetServiceOrErr<IStateManager>().SaveGlobal(); } catch { }
    }
}

/// <summary>@deja [text:"…"] [need:g_var] — 0.4.9: 지난 생에 이미 지나온 장면이면 다음 본문 줄 앞에 붙일 한 줄({dejapre})을 만든다.
/// 바로 다음 명령(그 본문 줄)이 예전에 재생된 적이 있으면 '지난 생에 겪은 장면'으로 본다. need 가 있으면 그 전역 앎도 있어야 한다.
/// text 가 없으면 되풀이의 절박함을 담은 공용 문장 가운데 하나(같은 생에 겹치지 않게)를 고른다.</summary>
[Alias("deja")]
public class DejaCommand : Command
{
    public StringParameter Text;
    public StringParameter Need;
    /// <summary>되풀이의 절박함을 담은 공용 문장 — Resources/MolakData/deja_lines.txt</summary>
    static IReadOnlyList<string> Pool => MolakData.List("deja_lines");
    static readonly List<int> used = new List<int>();
    static int usedRun = -1;

#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        string pre = "";
        try
        {
            var sp = Engine.GetService<IScriptPlayer>();
            var tr = sp != null ? sp.MainTrack : null;
            bool seen = false;
            if (tr != null && tr.PlayedScript != null) seen = sp.HasPlayed(tr.PlayedScript.Path, tr.PlayedIndex + 1);
            bool need = !Assigned(Need) || MolakStats.Get(Need.Value) > 0f;
            if (seen && need && MolakStats.Get("g_runs") >= 1f)
            {
                string line;
                if (Assigned(Text) && !string.IsNullOrEmpty(Text.Value)) line = Text.Value;
                else
                {
                    int run = Mathf.RoundToInt(MolakStats.Get("g_runs"));
                    if (run != usedRun) { used.Clear(); usedRun = run; }
                    var pool = Pool;
                    if (pool.Count == 0) line = "";
                    else
                    {
                        if (used.Count >= pool.Count) used.Clear();
                        int k; do { k = Random.Range(0, pool.Count); } while (used.Contains(k));
                        used.Add(k); line = pool[k];
                    }
                }
                if (line.Length > 0) pre = "<color=" + MolakTheme.CurseHex + ">" + line + "</color>";
            }
        }
        catch { pre = ""; }
        // 빈 문자열을 찍으면 나니노벨이 글자 드러내기를 끝내지 못하고 멈춘다 — 보이지 않는 한 글자(폭 없는 공백)로 둔다
        if (string.IsNullOrEmpty(pre)) pre = "\u200B";
        MolakStats.SetStr("dejapre", pre);
    }
}
