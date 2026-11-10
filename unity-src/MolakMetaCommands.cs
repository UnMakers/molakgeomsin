using System.Collections.Generic;
using System.Linq;
using Naninovel;
using UnityEngine;

/// <summary>@initrun — 회차 시작: 일반 변수 전부 리셋 후 깨달음(전역 g_up_*) 반영 시작치 설정.</summary>
[Alias("initrun")]
public class InitRunCommand : Command
{
#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        var vars = Engine.GetServiceOrErr<ICustomVariableManager>();
        // 전역 기본값 보장
        foreach (var gk in new[]{"g_pts","g_runs","g_clears","g_up_sword","g_up_body","g_up_core","g_up_coin","g_up_seal","g_up_mem","g_up_tea","g_up_lodge","g_up_vigor","g_know_tea","g_know_lodge"})
            if (!vars.VariableExists(gk)) vars.SetVariableValue(gk, new CustomVariableValue(0f));
        if (!vars.VariableExists("g_lastend")) vars.SetVariableValue("g_lastend", new CustomVariableValue("none"));
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
        MolakStats.Set("seallv", up("seal"));
        MolakKnow.Snapshot();   // 이번 생이 시작될 때 이미 아는 것 — 죽을 때 "되감기지 않는 것"을 가려낸다
        MolakStats.Set("runs", MolakStats.Get("g_runs"));
    }
}

/// <summary>@daytick — 하루 마감: day+1, 저주 뿌리 성장(날이 갈수록 가속), 숙식. starved 변수 설정.</summary>
[Alias("daytick")]
public class DayTickCommand : Command
{
#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        MolakStats.Add("day", 1f);
        MolakArt.Scene("morning"); MolakArt.Char(null); MolakVfx.Scene("day"); MolakFlow.Break();
        float day = MolakStats.Get("day");
        float accel = (day >= 35f ? 1.4f : 1f) * (day >= 50f ? 1.6f : 1f);
        float root = (0.7f + 0.03f * day) * accel * MolakStats.CurseFactor();
        if (MolakStats.Get("f_tea") > 0) root = Mathf.Max(0f, root - 0.15f); // 청심차 비방: 매일 반 김
        MolakStats.Add("curse", root);
        if (MolakStats.Get("f_ringclosed") > 0) MolakStats.Add("f_ringclosed", -1f); // 비무판 폐쇄 일수
        if (MolakStats.Get("injury") > 0) MolakStats.Add("injury", -1f);                 // 부상: 사흘이면 아문다
        if (MolakStats.Get("taint") > 0)                                                 // 독·곪은 상처: 날마다 깊어진다
        {
            MolakStats.Add("taint", 1f);
            MolakStats.Add("hp", MolakStats.Get("taint") >= 4f ? -2f : -1f);
        }
        float lodg = day >= 25f ? 2f : 1f;
        if (MolakStats.Get("f_hajinfree") > 0) lodg = 0f; // 하진무가 방값을 안 받기로 함
        if (MolakStats.Get("f_hajincold") > 0) lodg += 1f;  // 징세조 앞에서 외면한 뒤로 방값 패가 바뀌었다
        if (MolakStats.Get("f_hajindead") > 0) lodg = 3f;   // 하진무가 없는 객잔: 새 주인은 빈칸을 모른다
        if (MolakStats.Get("mn") >= lodg) { MolakStats.Add("mn", -lodg); MolakStats.Set("starved", 0f); }
        else { MolakStats.Set("mn", 0f); MolakStats.Add("hp", -2f); MolakStats.Set("starved", 1f); }
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
