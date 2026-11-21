using System.Collections.Generic;
using System.Linq;
using Naninovel;
using UnityEngine;

/// <summary>@initrun — 회차 시작: 일반 변수 전부 리셋 후 몸에 남은 것(MolakMemory)을 반영해 시작치를 정한다.</summary>
[Alias("initrun")]
public class InitRunCommand : Command
{
    /// <summary>코드에서만 쓰는 전역 수치(스크립트에는 나오지 않아 자동 목록에 없는 것).</summary>
    static readonly string[] CodeGlobals = {
        "g_pts", "g_runs", "g_clears", "g_wolfwon", "g_lastday", "g_bestday", "g_bestrank", "g_hapbo", "g_samecount", "g_lasttheme",
        "g_next_bond", "g_next_item", "g_realmbest", "g_redo", "g_lk_nameless", "g_ls_nameless",
        "g_bestsw", "g_bestng", "g_vow_rev", "g_vow_bond", "g_vow_live", "g_cursedeaths", "g_memv", "g_membuy", "g_up_mem", "g_wk_sw", "g_wk_sl", "g_wk_curse",
        "g_up_sword", "g_up_body", "g_up_core", "g_up_coin", "g_up_seal", "g_up_tea", "g_up_lodge", "g_up_vigor",
    };

    /// <summary>전역(g_) 기본값 보장 — 회차 시작과 옛 저장 불러오기 때 모두 쓴다.
    /// 되감기지 않는 앎(지난 생들에서 보고 듣고 죽으며 알게 된 것)은 조건식에서 쓰기 전에 반드시 있어야 한다.
    /// 목록을 손으로 적어 두면 새 변수를 빠뜨리기 쉬워, 스크립트에서 뽑은 목록(MolakVarList.Globals)과 표에서 끌어낸 이름을 쓴다.</summary>
    public static void EnsureGlobals ()
    {
        foreach (var n in MolakVarList.Globals) if (!n.StartsWith("g_pname")) MolakStats.EnsureNum(n);   // 0.4.75 이름은 글자다
        foreach (var n in CodeGlobals) MolakStats.EnsureNum(n);
        foreach (var f in MolakMemory.Forms) MolakStats.EnsureNum("g_form_" + f.Key);                    // 청운검결의 조각
        foreach (var sg in MolakMemory.Sigs) MolakStats.EnsureNum("g_sig_" + sg.Kind);                    // 받아 본 필살 초식
        MolakStats.EnsureStr("g_scars");                                                                   // 겪어 본 죽음
        foreach (var k in DuelCommand.BossKinds) { MolakStats.EnsureNum("g_know_" + k); MolakStats.EnsureNum("g_died_" + k); } // 진 상대의 검로·죽은 기억
        foreach (var e in MolakEnd.All) MolakStats.EnsureNum("g_end_" + e.Id);                            // 본 말로
        foreach (var le in MolakLedger.All)
            foreach (var pre in new[] { "g_lk_", "g_ld_", "g_ls_" }) MolakStats.EnsureNum(pre + le.Id);      // 강호록
        MolakStats.EnsureStr("g_lastend");
        MolakStats.EnsureStr("g_lastcause");
        MolakStats.EnsureStr("g_nxform");   // 0.4.62 다음 생 첫 아침에 몸에 들일 검결
    }

#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        var vars = Engine.GetServiceOrErr<ICustomVariableManager>();
        EnsureGlobals();
        MolakBridge.Reset();   // 0.4.41: 이음 문장의 '앞 사건' 기준점을 새 생에서 비운다
        // 전역(g_)이 아닌 변수 전부 제거 수준으로 0 초기화
        var snapshot = vars.Variables.Where(v => !v.Name.StartsWith("g_")).Select(v => (v.Name, v.Value.Type)).ToList();
        foreach (var (n, ty) in snapshot)
            vars.SetVariableValue(n, ty == CustomVariableValueType.String ? new CustomVariableValue("none") : new CustomVariableValue(0f));
        MolakMemory.Migrate();
        MolakMemory.ApplyStart();   // 손이 기억한 검·겪어 본 죽음·터져 본 문양·뜻·앎
        MolakStats.Set("sl", 0);
        MolakStats.Set("mn", MolakRules.BaseCoin);
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
            // 0.4.85 야습 날짜를 주사위로 흔들지 않는다(언메이커: 앎이 배신하면 납득이 안 된다).
            // 이번 생에 한 일(척후를 벰·수배·거처를 옮김)로만 움직인다 — Molak02 #DayLoop 머리.
            MolakStats.Set("raidshift", 0f);
            MolakStats.Set("thday", MolakRules.ThemeHintDay);
        }
        MolakStats.Set("rank", 0);
        if (MolakStats.Get("g_hapbo") > 0) MolakStats.Set("f_hapbyeok", 1f);   // 0.4.9: 한 번 읽은 합벽 검보는 되감기지 않는다
        MolakKnow.Snapshot();   // 이번 생이 시작될 때 이미 아는 것 — 죽을 때 "되감기지 않는 것"을 가려낸다
        MolakStats.Set("runs", MolakStats.Get("g_runs"));
        MolakMemory.ApplyWake();   // 0.4.56 눈을 뜬 아침의 선택
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
            float root = MolakRules.CurseRoot(day) * MolakStats.CurseFactor() * MolakRules.BodyCurseFactor(day, MolakStats.Get("ng"), MolakStats.Get("sl")) * (MolakStory.On ? 0.4f : 1f);   // 0.4.94 내공이 버티고 살기가 먹인다
            if (MolakStats.Get("f_tea") > 0) root = Mathf.Max(0f, root - MolakRules.TeaCurseCut); // 청심차 비방: 매일 반 김
            if (root > 0f) MolakStatLog.Why("curse", MolakStats.Get("f_tea") > 0 ? "하룻밤 새 문양이 자란다(청심차로 반은 눌렀다)" : "하룻밤 새 문양이 뿌리를 뻗는다");
            MolakStats.Add("curse", root);
            if (MolakStats.Get("f_ringclosed") > 0) MolakStats.Add("f_ringclosed", -1f); // 비무판 폐쇄 일수
            if (MolakStats.Get("injury") > 0) MolakStats.Add("injury", -1f);                 // 부상: 사흘이면 아문다
            if (MolakStory.On) MolakStats.Set("taint", 0f);
            if (MolakStats.Get("taint") > 0) MolakStatLog.Why("hp", MolakStats.Get("taintkind") == 1f ? "몸에 든 독이 하루치 살을 깎는다" : "곪은 상처가 하루치 살을 깎는다");
            if (MolakStats.Get("taint") > 0)                                                 // 독·곪은 상처: 날마다 몸을 깎는다
                MolakStats.Add("hp", MolakStats.Get("taint") >= MolakRules.TaintHeavy ? -MolakRules.TaintHpHeavy : -MolakRules.TaintHpLight);
            float lodg = MolakRules.LodgeOn(day);
            if (MolakStats.Get("f_hajinfree") > 0) lodg = 0f;                          // 하진무가 방값을 안 받기로 함
            if (MolakStats.Get("f_hajincold") > 0) lodg += MolakRules.LodgeColdExtra;  // 징세조 앞에서 외면한 뒤로 방값 패가 바뀌었다
            if (MolakStats.Get("f_hajindead") > 0) lodg = MolakRules.LodgeNoHajin;     // 하진무가 없는 객잔: 새 주인은 빈칸을 모른다
            if (MolakStory.On) lodg = 0f;
            if (lodg <= 0f) { }   // 이야기 모드: 방값 없음
            else if (MolakStats.Get("mn") >= lodg) { MolakStatLog.Why("mn", "객잔 방값 " + lodg.ToString("0") + "냥" + (n > 1 ? " × " + n + "밤" : "") + (MolakStats.Get("f_hajindead") > 0 ? "(새 주인)" : MolakStats.Get("f_hajincold") > 0 ? "(하진무가 셈을 바꿨다)" : "")); MolakStats.Add("mn", -lodg); }
            else if (MolakStats.Get("f_gaerice") > 0 && day - MolakStats.Get("gaefedday") >= MolakRules.GaeRiceGap)
            {   // 다리 밑의 솥: 굶는 날 거지들이 한 그릇 덜어 준다(나흘에 한 번). 아침에 그 장면이 나온다.
                MolakStatLog.Why("mn", "방값에 남은 은자를 털었다");
                MolakStats.Set("mn", 0f); MolakStats.Set("gaefed", 1f); MolakStats.Set("gaefedday", day);
            }
            else { MolakStatLog.Why("mn", "방값에 남은 은자를 털었다"); MolakStatLog.Why("hp", "방값을 치르고 나니 밥값이 없다. 굶었다"); MolakStats.Set("mn", 0f); MolakStats.Add("hp", -(n >= 3 ? MolakRules.StarveHpLong : MolakRules.StarveHpShort)); MolakStats.Set("starved", 1f); }
        }
        // 독·곪은 상처는 마감마다 깊어진다(며칠 한 바퀴면 두 칸 — 손쓸 기회는 한두 바퀴)
        MolakStory.Boost(MolakStats.Get("day"));
        if (MolakStats.Get("taint") > 0) MolakStats.Add("taint", n >= 3 ? MolakRules.TaintGrowLong : MolakRules.TaintGrowShort);
        MolakStats.ClampAll();
#if UNITY_EDITOR
        // 에디터 전용: 은자 흐름 기록(소크 분석용)
        try { System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), "Build", "soak_money.csv"), MolakStats.Get("g_runs").ToString("0") + "," + MolakStats.Get("day").ToString("0") + "," + MolakStats.Get("mn").ToString("0") + "," + MolakStats.Get("rank").ToString("0") + "\n"); } catch { }
#endif
    }
}

/// <summary>@regress — 사망 정산: 몸에 남는 것을 셈하고(MolakMemory.OnLifeEnd) 회귀 횟수를 센다.</summary>
[Alias("regress")]
public class RegressCommand : Command
{
    public StringParameter Cause;

    public override async Awaitable Execute (ExecutionContext ctx)
    {
        if (MolakStats.Get("practice") > 0f) return;
        MolakMemory.OnLifeEnd();
        // 0.4.85 그 밤까지 산 생이면 그날을 기억한다 — 다음 생에 날이 달라지면 까닭과 함께 말한다
        if (MolakStats.Get("raidday") > 0f && MolakStats.Get("day") >= MolakStats.Get("raidday")) MolakStats.Set("g_raidlast", MolakStats.Get("raidday"));
        MolakStats.Add("g_runs", 1f);
        MolakLedger.Accumulate();
        MolakAutoSave.LogLife(MolakStats.Get("rm_c"));
        await Engine.GetServiceOrErr<IStateManager>().SaveGlobal();
    }
}

/// <summary>@clearrun — 교주 격파 정산.</summary>
[Alias("clearrun")]
public class ClearRunCommand : Command
{
    public override async Awaitable Execute (ExecutionContext ctx)
    {
        MolakMemory.OnLifeEnd();
        MolakStats.Add("g_clears", 1f);
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
    // 0.4.58 생마다 더 달라지게(언메이커): 한 번도 못 본 사건 ×3, 바로 앞 생에 본 사건은 한 번 더 깎는다(×0.45).
    public static float RunMul (string id)
    {
        float runs = MolakStats.Get("g_runs");
        if (runs < 1f) return 1f;
        float k = MolakStats.Get("g_ev_" + id);
        if (k <= 0f) return 3f;
        float m = Mathf.Max(0.3f, 1f / (1f + 0.5f * k));
        if (MolakStats.Get("g_evl_" + id) >= runs) m *= 0.45f;   // g_evl_ = 그 사건을 마지막으로 본 생의 번호(1부터). 지금 생 = runs+1
        return m;
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
            if (MolakStory.On && !MolakStory.EventAllowed(d.Id)) continue;   // 0.4.73 이야기 모드는 줄기 사건만
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
        MolakStats.Set("g_evl_" + pick.Id, MolakStats.Get("g_runs") + 1f);
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

/// <summary>@lifenote — 0.4.8 말로 직후 한 장: 이번 생의 기록(버틴 날·서열), 지난 생과 견줌,
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
        float day = MolakStats.Get("day"), rank = MolakStats.Get("rank");
        string cause = MolakStats.Str("deathcause");
        // 이번 생의 뜻(회귀 아침에 고른 것): 이루면 그 뜻이 몸에 남는다(MolakMemory.OnLifeEnd 가 정산)
        int vow = Mathf.RoundToInt(MolakStats.Get("vow"));
        string vowline = "";
        if (vow > 0)
        {
            bool ok = MolakMemory.VowOk();
            string name = vow == 1 ? "복수" : vow == 2 ? "인연" : "생존";
            if (ok)
                vowline = "이번 생의 뜻(" + name + ")을 이루었다 — " + (vow == 1 ? "칼에 실은 뜻이 손에 남는다" : vow == 2 ? "곁을 내준 사람의 숨이 단전에 남는다" : "버틴 날들이 몸에 남는다") + ".";
            else vowline = "이번 생의 뜻(" + name + ")은 이루지 못했다.";
        }
        MolakStats.SetStr("vowline", vowline);
        MolakStats.Set("vowc", vowline.Length > 0 ? 1f : 0f);
        // 이번 생의 기록
        string sum = "이번 생 — " + Days(day) + " · " + (rank > 0 && rank <= MolakDuel.ListEdge ? "서열 " + MolakDuel.RankText(rank) : "서열록에 이름 없음");
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
            else if (hap && (rank <= 0f || rank > MolakRules.TrueEndRank)) hint = "'검보를 품었거든 서열은 서른 안이면 족하다.'";
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
            // 0.4.80: 바로 뒤에서 회귀한 생 갈래(…K / …Re, g_ev_* · g_bt_*)로 넘어가면 이 한 줄은 찍지 않는다.
            // 갈래가 제 첫 줄로 '안다'를 보여 주는데, 앞에서 같은 앎을 먼저 말해 버리면 터질 대목이 미리 새고 두 번 말하게 된다.
            if (seen && tr != null && GoesToRegressionBranch(tr)) seen = false;
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

    /// <summary>{dejapre} \uC904 \uB4A4 \uBA87 \uBA85\uB839 \uC548\uC5D0 \uD68C\uADC0\uD55C \uC0DD \uAC08\uB798\uB85C \uAC00\uB294 \uC870\uAC74 @goto \uAC00 \uC788\uACE0, \uADF8 \uC870\uAC74\uC774 \uC9C0\uAE08 \uCC38\uC778\uAC00</summary>
    static bool GoesToRegressionBranch (IScriptTrack tr)
    {
        try
        {
            var pl = tr.Playlist; if (pl == null) return false;
            for (int k = tr.PlayedIndex + 2; k < pl.Count && k <= tr.PlayedIndex + 4; k++)
            {
                var c = pl[k];
                if (!(c is Naninovel.Commands.Goto)) { if (c is Naninovel.Commands.PrintText || c is Naninovel.Commands.AddChoice) return false; continue; }
                if (c.ConditionalExpression == null || !c.ConditionalExpression.HasValue) return false;
                string ex = c.ConditionalExpression.Value;
                if (ex == null || (ex.IndexOf("g_ev_", System.StringComparison.Ordinal) < 0 && ex.IndexOf("g_bt_", System.StringComparison.Ordinal) < 0)) continue;
                return Naninovel.ExpressionEvaluator.Evaluate<bool>(ex);
            }
        }
        catch { }
        return false;
    }
}
