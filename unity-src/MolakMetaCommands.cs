using System.Collections.Generic;
using System.Linq;
using Naninovel;
using UnityEngine;

/// <summary>@initrun — 회차 시작: 일반 변수 전부 리셋 후 깨달음(전역 g_up_*) 반영 시작치 설정.</summary>
[Alias("initrun")]
public class InitRunCommand : Command
{
    /// <summary>전역(g_) 기본값 보장 — 회차 시작과 옛 저장 불러오기 때 모두 쓴다.</summary>
    public static void EnsureGlobals (ICustomVariableManager vars)
    {
        // 전역 기본값 보장
        foreach (var gk in new[]{"g_pts","g_runs","g_clears","g_up_sword","g_up_body","g_up_core","g_up_coin","g_up_seal","g_up_mem","g_up_tea","g_up_lodge","g_up_vigor","g_know_tea","g_know_lodge","g_lastday","g_bestday","g_bestrank","g_hapbo","g_samecount"})
            if (!vars.VariableExists(gk)) vars.SetVariableValue(gk, new CustomVariableValue(0f));
        if (!vars.VariableExists("g_lastend")) vars.SetVariableValue("g_lastend", new CustomVariableValue("none"));
        if (!vars.VariableExists("g_lastcause")) vars.SetVariableValue("g_lastcause", new CustomVariableValue("none"));
        // 되감기지 않는 앎: 지난 생들에서 보고 듣고 죽으며 알게 된 것 (조건식에서 쓰기 전에 반드시 존재해야 한다)
        foreach (var gk in new[]{"g_redverse","g_soadied","g_soalast","g_soaname","g_know_cup","g_know_amhkey","g_know_amhsister","g_amhkilled","g_amhfreed",
                                 "g_know_raid","g_know_wangsam","g_know_taxday","g_jbtruth","g_know_hajin","g_hajindied","g_hapdone","g_know_maeng","g_know_gaeknot","g_know_namgung","g_know_hao","g_know_maengsell","g_know_jangsell","g_maenglate","g_know_yong","g_know_gwak","g_gwakwifedied","g_herbdied","g_know_ripe","g_next_bond","g_next_item","g_redo","g_lasttheme","g_fac_gyo","g_fac_gyo2","g_fac_maeng","g_fac_namgung","g_fac_gae","g_fac_gae2","g_fac_hao","g_fac_hao2","g_fac_nok","g_fac_dok","g_fac_swe","g_fac_swe2","g_fac_amh","g_fac_amh2","g_fac_pyo","g_fac_pyo2","g_fac_dang","g_fac_jeok","g_fac_gwan","g_know_soa","g_know_wolf","g_know_lord","g_know_jinbaek","g_know_hochi","g_know_kyona",
                                 "g_know_hunt1","g_know_hunt2","g_know_hunt3","g_know_cheolsan","g_know_rival","g_know_chainmob"})
            if (!vars.VariableExists(gk)) vars.SetVariableValue(gk, new CustomVariableValue(0f));
        foreach (var e in MolakEnd.All)
            if (!vars.VariableExists("g_end_" + e.Id)) vars.SetVariableValue("g_end_" + e.Id, new CustomVariableValue(0f));
        foreach (var le in MolakLedger.All)
            foreach (var pre in new[]{"g_lk_","g_ld_","g_ls_"})
                if (!vars.VariableExists(pre + le.Id)) vars.SetVariableValue(pre + le.Id, new CustomVariableValue(0f));
        foreach (var gk in new[]{"g_lk_nameless","g_ls_nameless"})
            if (!vars.VariableExists(gk)) vars.SetVariableValue(gk, new CustomVariableValue(0f));
    }

#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        var vars = Engine.GetServiceOrErr<ICustomVariableManager>();
        EnsureGlobals(vars);
        // 전역(g_)이 아닌 변수 전부 제거 수준으로 0 초기화
        var snapshot = vars.Variables.Where(v => !v.Name.StartsWith("g_")).Select(v => (v.Name, v.Value.Type)).ToList();
        foreach (var (n, ty) in snapshot)
            vars.SetVariableValue(n, ty == CustomVariableValueType.String ? new CustomVariableValue("none") : new CustomVariableValue(0f));
        float up (string k) => MolakStats.Get("g_up_" + k);
        MolakStats.Set("hpmax", 10 + 2 * up("body") + up("vigor"));
        MolakStats.Set("hp", 10 + 2 * up("body") + up("vigor"));
        if (up("tea") > 0) MolakStats.Set("f_tea", 1f);      // 회귀 상점: 청심차 비방
        if (up("lodge") > 0) MolakStats.Set("f_hajinfree", 1f); // 회귀 상점: 빈칸 방값
        MolakStats.Set("ng", up("core") * 3);
        MolakStats.Set("sw", 2 + up("sword") * 2);
        MolakStats.Set("sl", 0);
        MolakStats.Set("mn", 3 + up("coin") * 5);
        MolakStats.Set("curse", 5);
        MolakStats.Set("day", 1);
        // 이번 생의 바람(회차 테마): 첫 생은 맑음. 그 뒤로는 다섯에 하나 맑음, 나머지 넷 중 하나 — 지난 생과 겹치지 않게.
        {
            int th = 0;
            if (MolakStats.Get("g_runs") >= 1 && Random.Range(0, 5) > 0)
            {
                int last = Mathf.RoundToInt(MolakStats.Get("g_lasttheme"));
                do { th = Random.Range(1, 5); } while (th == last);
            }
            MolakStats.Set("theme", th);
            MolakStats.Set("g_lasttheme", th);
            MolakStats.Set("raidday", th == 4 ? 38f : 42f);
            MolakStats.Set("thday", 3f);
        }
        // 이번 생의 채비: 상점에서 골라 둔 인연·물건을 첫 아침에 쥐여 주고 비운다
        {
            int b = Mathf.RoundToInt(MolakStats.Get("g_next_bond")), it = Mathf.RoundToInt(MolakStats.Get("g_next_item"));
            if (b == 1) { MolakStats.Set("f_herbfriend", 1f); MolakStats.Set("f_herbpulse", 1f); }
            if (b == 2) { MolakStats.Set("f_alms", 1f); MolakStats.Set("f_gaekid", 1f); MolakStats.Set("rep_gae", 1f); }
            if (b == 3) { MolakStats.Set("f_jangfriend", 1f); }
            if (it == 1) MolakStats.Set("f_hat", 1f);
            if (it == 2) MolakStats.Set("f_salve", 1f);
            if (it == 3) { MolakStats.Set("f_incense", 1f); MolakStats.Add("curse", -2f); }
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
            float accel = (day >= 35f ? 1.4f : 1f) * (day >= 50f ? 1.6f : 1f);
            float root = (0.7f + 0.03f * day) * accel * MolakStats.CurseFactor();
            if (MolakStats.Get("f_tea") > 0) root = Mathf.Max(0f, root - 0.15f); // 청심차 비방: 매일 반 김
            MolakStats.Add("curse", root);
            if (MolakStats.Get("f_ringclosed") > 0) MolakStats.Add("f_ringclosed", -1f); // 비무판 폐쇄 일수
            if (MolakStats.Get("injury") > 0) MolakStats.Add("injury", -1f);                 // 부상: 사흘이면 아문다
            if (MolakStats.Get("taint") > 0)                                                 // 독·곪은 상처: 날마다 몸을 깎는다
                MolakStats.Add("hp", MolakStats.Get("taint") >= 4f ? -2f : -1f);
            float lodg = day >= 25f ? 2f : 1f;
            if (MolakStats.Get("f_hajinfree") > 0) lodg = 0f; // 하진무가 방값을 안 받기로 함
            if (MolakStats.Get("f_hajincold") > 0) lodg += 1f;  // 징세조 앞에서 외면한 뒤로 방값 패가 바뀌었다
            if (MolakStats.Get("f_hajindead") > 0) lodg = 3f;   // 하진무가 없는 객잔: 새 주인은 빈칸을 모른다
            if (MolakStats.Get("mn") >= lodg) MolakStats.Add("mn", -lodg);
            else { MolakStats.Set("mn", 0f); MolakStats.Add("hp", n >= 3 ? -1f : -2f); MolakStats.Set("starved", 1f); }
        }
        // 독·곪은 상처는 마감마다 깊어진다(며칠 한 바퀴면 두 칸 — 손쓸 기회는 한두 바퀴)
        if (MolakStats.Get("taint") > 0) MolakStats.Add("taint", n >= 3 ? 2f : 1f);
        MolakStats.ClampAll();
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
        float gained = Mathf.Max(1f, Mathf.Floor(day / 3f) + Mathf.Floor((sw + ng) / 6f) + (MolakStats.Get("rank") > 0 ? 2f : 0f));
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
        var vars = Engine.GetServiceOrErr<ICustomVariableManager>();
        string[] bonds = { "없음", "백초 할멈의 단골", "거지 아이", "쥐눈이 장" };
        string[] items = { "없음", "죽립", "금창약", "안식향" };
        vars.SetVariableValue("prepbond", new CustomVariableValue(bonds[Mathf.Clamp(Mathf.RoundToInt(MolakStats.Get("g_next_bond")), 0, 3)]));
        vars.SetVariableValue("prepitem", new CustomVariableValue(items[Mathf.Clamp(Mathf.RoundToInt(MolakStats.Get("g_next_item")), 0, 3)]));
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
            int pcost = k.StartsWith("bond") ? 3 : k.StartsWith("item") ? 2 : 12;
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
    static int W (MolakEventDefs.Def d) { return Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(1, d.Weight) * 10f * ThemeMul(d.Id))); }
    public static float ThemeMul (string id)
    {
        switch (Mathf.RoundToInt(MolakStats.Get("theme")))
        {
            case 1: // 장마
                if (id == "E_Flood") return 3f;
                if (id.StartsWith("E_Rain")) return 2.5f;
                if (id == "E_Ferry" || id == "E_Ferry2" || id == "E_Ferry3") return 1.6f;
                break;
            case 2: // 역병
                if (id.StartsWith("E_Herb")) return 1.8f;
                if (id == "E_Funeral" || id == "E_Emptytown" || id == "E_Lastletter" || id == "E_Vigil") return 2.5f;
                if (id.StartsWith("E_Monk") || id.StartsWith("E_Beggar") || id.StartsWith("E_Gaebang")) return 1.6f;
                if (id.StartsWith("E_Gamble") || id.StartsWith("E_Nightmarket") || id == "E_Caravan" || id.StartsWith("E_Peddler")) return 0.4f;
                break;
            case 3: // 징세 강화
                if (id.StartsWith("E_Bloodtax") || id == "E_Hostage" || id == "E_HerbTax" || id == "E_JangSell") return 2.5f;
                if (id.StartsWith("E_Guard") || id == "E_HajinNight" || id == "E_HerbBurn") return 1.6f;
                break;
            case 4: // 혈랑대 이른 생
                if (id.StartsWith("E_Hunter") || id == "E_Search" || id == "E_SoaPoster") return 2.5f;
                if (id.StartsWith("E_Spy") || id.StartsWith("E_Cultists") || id == "E_Cup") return 1.6f;
                break;
        }
        return 1f;
    }

#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        var vars = Engine.GetServiceOrErr<ICustomVariableManager>();
        void SetNext (string v) => vars.SetVariableValue("nextevent", new CustomVariableValue(v));
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
    static readonly Dictionary<string, string> Tips = new Dictionary<string, string> {
        { "아사",        "'배가 비면 검도 빈다. 날품이라도 팔거라. 객잔 늙은이의 빈칸도, 약방의 미음도 있느니라.'" },
        { "탈진",        "'몸이 반쯤 꺾였으면 판에 서지 마라. 사흘 쉬는 것도 수련이니라.'" },
        { "포박",        "'그물은 바깥부터 조인다. 예순 날이 차기 전에 끝을 보든지, 길을 떠나든지 하거라.'" },
        { "소아의 검",   "'대답을 기다려 주는 칼은 없느니라. 그 아이 앞에서는 먼저 입을 열거라.'" },
        { "소아의 오인", "'문양이 짙은 채로 그 아이 앞에 서지 마라. 그 눈에는 마인으로 보이느니라.'" },
        { "원수",        "'비무판에서 벤 목이 상복을 부른다. 목을 칠지는 이긴 쪽이 정하느니라.'" },
        { "자객",        "'서열록에 오른 이름은 교의 장부에도 오른다. 높이 오를수록 등 뒤를 보거라.'" },
        { "독",          "'푸른 날에 스쳤거든 미루지 마라. 독은 검을 기다려 주지 않느니라.'" },
        { "곪은 상처",   "'작은 상처를 작게 보면 큰 병이 된다. 약값을 아끼지 마라.'" },
        { "사술 폭주",   "'문양은 사술을 쓸수록 자란다. 청심차와 봉인이 그 열을 누르느니라.'" },
        { "빚",          "'왕삼의 장부는 이자가 빠르다. 외상 판에 칼을 걸지 마라.'" },
        { "협",          "'징세조 앞에서 고개를 드는 날이 마지막 날이 될 수 있느니라.'" },
        { "야습",        "'그 밤은 정해진 밤이다. 계단과 뒷문을 미리 보아 두거라.'" },
        { "미인계",      "'단 술 앞에서는 살기가 혀보다 먼저 독을 안다. 살기를 길러 두거라.'" },
        { "미숙한 초식", "'몸에 배지 않은 초식에 목숨을 걸지 마라. 검이 먼저 익어야 하느니라.'" },
        { "익사",        "'물 위에서는 먼저 나가는 쪽이 먼저 빠지느니라.'" },
        { "입마",        "'부르는 소리에 무릎 꿇지 마라. 그 소리는 네 것이 아니니라.'" },
        { "생사결",      "'금 안에는 물러설 데가 없다. 이길 셈이 서지 않거든 금을 넘지 마라.'" },
        { "비무 패배",   "'판에 서기 전에 상대를 읽거라. 질 판은 걸어 나와도 부끄럽지 않느니라.'" },
    };
    static readonly HashSet<string> NotDeaths = new HashSet<string> { "등극", "합벽", "성사", "순검", "원행", "재건", "도주" };

    static string Days (float d)
    {
        if (d <= 3f) return "사흘을 넘기지 못했다";
        if (d < 10f) return "열흘을 채우지 못했다";
        if (d < 20f) return "보름 남짓 버텼다";
        if (d < 30f) return "스무 날 남짓 버텼다";
        if (d < 40f) return "한 달 남짓 버텼다";
        if (d < 50f) return "마흔 날 남짓 버텼다";
        if (d < 60f) return "쉰 날을 넘겼다";
        return "예순 날을 다 채웠다";
    }

    public override async Awaitable Execute (ExecutionContext ctx)
    {
        var vars = Engine.GetServiceOrErr<ICustomVariableManager>();
        string S (string k) { return vars.VariableExists(k) && vars.GetVariableValue(k).Type == CustomVariableValueType.String ? vars.GetVariableValue(k).String : ""; }
        float day = MolakStats.Get("day"), rank = MolakStats.Get("rank"), gained = MolakStats.Get("gained");
        string cause = S("deathcause");
        // 이번 생의 뜻(회귀 아침에 고른 것): 이루면 깨달음 +4
        int vow = Mathf.RoundToInt(MolakStats.Get("vow"));
        string vowline = "";
        if (vow > 0)
        {
            bool ok = vow == 1 ? (rank > 0f && rank <= 1000f)
                    : vow == 2 ? (MolakStats.Get("f_soabond") > 0f || MolakStats.Get("aff") >= 3f || MolakStats.Get("f_hajinbond") > 0f || MolakStats.Get("f_herbbond") > 0f || MolakStats.Get("f_gwakally") > 0f || MolakStats.Get("f_yongally") > 0f || MolakStats.Get("f_amhally") > 0f || MolakStats.Get("f_jinally") > 0f)
                    : day >= 46f;
            string name = vow == 1 ? "복수" : vow == 2 ? "인연" : "생존";
            if (ok) { MolakStats.Add("g_pts", 4f); gained += 4f; MolakStats.Set("gained", gained); vowline = "이번 생의 뜻(" + name + ")을 이루었다 — 깨달음 +4"; }
            else vowline = "이번 생의 뜻(" + name + ")은 이루지 못했다.";
        }
        vars.SetVariableValue("vowline", new CustomVariableValue(vowline));
        MolakStats.Set("vowc", vowline.Length > 0 ? 1f : 0f);
        // 이번 생의 기록
        string sum = "이번 생 — " + Days(day) + " · " + (rank > 0 ? "서열 " + rank.ToString("N0") + "위" : "서열록에 이름 없음") + " · 깨달음 +" + gained.ToString("0");
        vars.SetVariableValue("lifesum", new CustomVariableValue(sum));
        // 지난 생과 견줌(두 번째 생부터)
        float lastDay = MolakStats.Get("g_lastday"), bestDay = MolakStats.Get("g_bestday"), bestRank = MolakStats.Get("g_bestrank");
        var cmp = new List<string>();
        if (MolakStats.Get("g_runs") >= 2f && lastDay > 0f)
        {
            if (day > bestDay) cmp.Add("가장 오래 버틴 생이다");
            else if (day > lastDay + 2f) cmp.Add("지난 생보다 오래 버텼다");
            if (rank > 0f && (bestRank <= 0f || rank < bestRank)) cmp.Add("가장 높이 오른 생이다");
        }
        vars.SetVariableValue("lifecmp", new CustomVariableValue(string.Join(". ", cmp) + (cmp.Count > 0 ? "." : "")));
        MolakStats.Set("lifecmpc", cmp.Count);
        MolakStats.Set("g_lastday", day);
        if (day > bestDay) MolakStats.Set("g_bestday", day);
        if (rank > 0f && (bestRank <= 0f || rank < bestRank)) MolakStats.Set("g_bestrank", rank);
        // 같은 자리에서 두 번 — 노인의 한 마디
        string lastCause = S("g_lastcause");
        string tip = "";
        if (!string.IsNullOrEmpty(cause) && cause == lastCause && !NotDeaths.Contains(cause) && Tips.ContainsKey(cause)) tip = Tips[cause];
        vars.SetVariableValue("deathtip", new CustomVariableValue(tip));
        MolakStats.Set("deathtipc", tip.Length > 0 ? 1f : 0f);
        MolakStats.Set("g_samecount", (!string.IsNullOrEmpty(cause) && cause == lastCause) ? MolakStats.Get("g_samecount") + 1f : 1f);
        vars.SetVariableValue("g_lastcause", new CustomVariableValue(string.IsNullOrEmpty(cause) ? "none" : cause));
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
        vars.SetVariableValue("truehint", new CustomVariableValue(hint));
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
    static readonly string[] Pool = {
        "이 장면을 안다. 안다고 해서 숨이 편해지지는 않는다.",
        "또 이 자리다. 지난 생에도 여기서 같은 말을 들었고, 같은 숨을 쉬었다.",
        "몸이 먼저 굳는다. 머리보다 몸이 이 순간을 더 잘 기억한다.",
        "몇 번째인지 세다가 그만둔다. 세는 동안에도 날은 같은 쪽으로 흐른다.",
        "같은 바람, 같은 냄새. 달라진 것은 손에 묻은 죽음의 수뿐이다.",
        "이번에는 다르게 하리라. 지난번에도 이 자리에서 그렇게 다짐했다.",
        "기억이 앞질러 간다. 저 사람이 다음에 무슨 말을 할지, 입술보다 먼저 안다.",
        "손끝이 차다. 지난 생의 마지막 온기가 아직 다 빠지지 않았다.",
        "되감긴 것은 날뿐이다. 그날의 두려움은 되감기지 않았다.",
        "이 길을 몇 번이고 지나왔다. 그때마다 어딘가에서 길이 끊겼다.",
        "낯익은 얼굴들이 웃는다. 이 얼굴들이 어떻게 일그러지는지도 안다.",
        "가슴의 문양이 낮게 운다. 너도 기억하느냐고 묻는 것만 같다.",
        "두 번째 보는 장면이 더 아프다. 무엇을 잃을지 알고 보니까.",
        "같은 돌부리에 두 번 넘어지는 자를 강호는 비웃는다. 당신은 몇 번째인지도 모른다.",
        "머릿속에서 누가 숫자를 센다. 남은 날인지, 죽은 횟수인지 모를 숫자를.",
        "모든 것이 그 생 그대로다. 그대로라서 숨이 막힌다.",
        "지난 생의 당신이 서 있던 자리에 이번 생의 당신이 선다. 둘 다 같은 곳을 본다.",
        "식은땀이 등줄기를 탄다. 아무 일도 일어나지 않았는데, 일어날 일을 안다.",
        "웃음소리가 들린다. 지난 생에도 이 웃음 끝에 피 냄새가 났다.",
        "이 순간을 또 살아야 한다. 몇 번을 살아도 쉬워지지 않는다.",
    };
    static readonly List<int> used = new List<int>();
    static int usedRun = -1;

#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        var vars = Engine.GetServiceOrErr<ICustomVariableManager>();
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
                    if (used.Count >= Pool.Length) used.Clear();
                    int k; do { k = Random.Range(0, Pool.Length); } while (used.Contains(k));
                    used.Add(k); line = Pool[k];
                }
                pre = "<color=#9A70C4>" + line + "</color>";
            }
        }
        catch { pre = ""; }
        // 빈 문자열을 찍으면 나니노벨이 글자 드러내기를 끝내지 못하고 멈춘다 — 보이지 않는 한 글자(폭 없는 공백)로 둔다
        if (string.IsNullOrEmpty(pre)) pre = "\u200B";
        vars.SetVariableValue("dejapre", new CustomVariableValue(pre));
    }
}
