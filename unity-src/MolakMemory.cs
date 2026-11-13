using System.Collections.Generic;
using Naninovel;
using UnityEngine;

/// <summary>
/// 0.4.55 회귀의 보상 — 깨달음 점수와 상점을 걷어 내고 '몸에 남는 것'으로 바꾼다.
/// 회귀물의 보상은 기억이다. 얼마나 멀리 갔느냐보다 무엇을 보고 어떻게 죽었느냐가 다음 생에 남는다.
///  · 손이 기억한 검: 지난 생들에서 가장 높이 오른 검술·내공의 4분의 1을 들고 깨어난다(검술 +10, 내공 +10까지).
///  · 겪어 본 죽음: 처음 겪는 사인(死因)마다 몸이 그 아픔을 기억한다 — 체력 상한 +1(여덟까지).
///  · 터져 본 문양: 사술 폭주로 죽을 때마다 문양이 덜 오른다(셋까지).
///  · 이번 생의 뜻: 이루고 죽으면 그 뜻이 몸에 남는다 — 복수는 검술, 인연은 내공, 생존은 체력(뜻마다 셋까지).
///  · 청운검결의 조각(초식 넷): 특정한 죽음을 겪으면 사형들에게 배운 수가 몸에서 돌아온다. 비무의 합에서 쓴다.
///  · 앎: 비밀·버릇·필살 초식(MolakKnow). 또렷한 기억(g_up_mem)은 아는 것이 늘수록 저절로 깊어진다.
/// 모든 수치는 전역(g_) 변수에 있고, 죽을 때(@regress·@clearrun) 한 번 정산해 rm_text 로 보여 준다.
/// </summary>
public static class MolakMemory
{
    public const float SwordKeep = 0.25f, SwordKeepMax = 10f, CoreKeep = 0.25f, CoreKeepMax = 10f;
    public const int ScarMax = 8, SealMax = 3, VowMax = 3, RmMax = 10;
    public static readonly int[] MemTiers = { 6, 14, 24 };   // 아는 것이 이만큼이면 또렷한 기억 한 단계

    // ---- 청운검결의 조각 ----
    public struct Form
    {
        public string Key, Name, Hanja, Teach, Effect;
        public Form (string k, string n, string h, string t, string e) { Key = k; Name = n; Hanja = h; Teach = t; Effect = e; }
    }
    public static readonly Form[] Forms = {
        new Form("step",  "운보", "雲步", "일곱째 사형이 목검으로 정강이를 치며 하던 말이 돌아온다. 맞을 자리에 서 있지 마라, 막내야. 비켜서는 것도 검이다.",
                 "합에서 지는 수를 한 번 비켜선다 — 기세를 잃지 않는다(판마다 한 번)"),
        new Form("eye",   "관",   "觀",   "사부는 입문한 막내에게 사흘 동안 칼을 쥐여 주지 않았다. 마당에 앉혀 놓고 사형들의 어깨만 보게 했다. 칼은 어깨가 먼저 말한다.",
                 "합 하나를 칼 대신 눈으로 쓴다 — 다음 합의 기세가 확실히 보인다(판마다 한 번)"),
        new Form("chain", "연",   "連",   "대사형은 한 번 들어간 칼을 도로 물리면 칼등으로 손등을 쳤다. 들어갔으면 이어서 들어라. 물러서는 건 그다음이다.",
                 "합을 내리 둘 이기면 기세가 하나 더 붙는다"),
        new Form("fall",  "낙",   "落",   "문양이 터지던 순간, 세상이 느려지고 칼 하나가 위에서 아래로 떨어지는 길이 보였다. 청운의 검보에도, 사람의 검법에도 없는 길이었다.",
                 "마지막 한 칼에 쌓은 기세를 두 배로 싣는다(더해지는 것은 셋까지) — 저주 +4"),
    };

    public static bool HasForm (string key)
    {
        return MolakStats.Get("g_form_" + key) > 0f || MolakStats.Get("practice") > 0f;
    }

    // ---- 이름 있는 칼잡이의 필살 초식: 처음 받을 땐 읽히지 않고, 한 번 받아 보면 다음 생부터 보인다 ----
    public struct Sig
    {
        public string Kind, Name, Line; public int True;
        public Sig (string k, string n, int t, string l) { Kind = k; Name = n; True = t; Line = l; }
    }
    public static readonly Sig[] Sigs = {
        new Sig("hochi",    "독아쇄(毒牙碎)",   1, "호치가 도끼를 머리 위로 들어 올린다. 뼈 장식이 한꺼번에 운다."),
        new Sig("kyona",    "광견난무(狂犬亂舞)", 1, "견아가 네 발 짐승처럼 몸을 낮춘다. 칼이 사방에서 문다."),
        new Sig("cheolsan", "철산고(鐵山靠)",   1, "철산도가 어깨부터 들이민다. 칼보다 몸이 먼저 온다."),
        new Sig("namgung",  "천뢰일섬(天雷一閃)", 1, "남궁현의 칼이 칼집 안에서 운다. 뽑는 순간이 곧 베는 순간이다."),
        new Sig("jinbaek",  "북두낙월(北斗落月)", 3, "진백의 발이 북두칠성을 밟는다. 칼끝이 일곱으로 갈라진다."),
        new Sig("rival",    "회마창(回馬槍)",   3, "곽산이 등을 보이며 물러선다. 창끝이 겨드랑이 밑으로 숨는다."),
        new Sig("soa",      "적운쌍영(赤雲雙影)", 2, "소아가 쌍검을 엇갈려 세운다. 붉은 끈 두 가닥이 바람 속에서 멎는다."),
        new Sig("wolf",     "혈쇄망(血鎖網)",   2, "혈쇄문주가 사슬을 몸 둘레로 돌린다. 쇠고리가 그물처럼 펼쳐진다."),
        new Sig("hunt3",    "집법단죄(執法斷罪)", 2, "집법전주가 칼을 세워 든다. 판결문을 읽기 전의 침묵이다."),
        new Sig("lord",     "멸도벽(滅道壁)",   2, "교주의 그림자가 벽처럼 선다. 칼이 들어갈 자리가 보이지 않는다."),
    };
    public static bool TryGetSig (string kind, out Sig s)
    {
        foreach (var x in Sigs) if (x.Kind == kind) { s = x; return true; }
        s = default(Sig); return false;
    }

    // ---- 시작치 ----
    static float G (string k) { return MolakStats.Get(k); }

    public static float StartSword ()
    {
        float keep = Mathf.Min(SwordKeepMax, Mathf.Floor(Mathf.Max(0f, G("g_bestsw") - MolakRules.BaseSword) * SwordKeep));
        return MolakRules.BaseSword + keep + G("g_vow_rev");
    }
    public static float StartCore ()
    {
        return Mathf.Min(CoreKeepMax, Mathf.Floor(G("g_bestng") * CoreKeep)) + G("g_vow_bond");
    }
    public static int Scars ()
    {
        string s = MolakStats.Str("g_scars", "");
        if (string.IsNullOrEmpty(s) || s == "none") return 0;
        int n = 0; foreach (var p in s.Split('|')) if (p.Length > 0) n++;
        return Mathf.Min(ScarMax, n);
    }
    public static float StartHp () { return MolakRules.BaseHp + Scars() + G("g_vow_live"); }
    public static float SealLv () { return Mathf.Min(SealMax, G("g_cursedeaths")); }
    public static float MemLv ()
    {
        int c = MolakKnow.Count(), lv = 0;
        foreach (var t in MemTiers) if (c >= t) lv++;
        return lv;
    }

    /// <summary>옛 기록 옮기기: 깨달음으로 사 둔 강화를 같은 크기의 '몸의 기억'으로 바꿔 둔다(한 번).</summary>
    public static void Migrate ()
    {
        if (G("g_memv") >= 1f) return;
        MolakStats.Set("g_memv", 1f);
        MolakStats.Set("g_membuy", G("g_up_mem"));   // 사 둔 '또렷한 기억'은 그 단계 밑으로 내려가지 않는다
        float us = G("g_up_sword"), uc = G("g_up_core"), ub = G("g_up_body"), uv = G("g_up_vigor"), useal = G("g_up_seal");
        if (us > 0f) MolakStats.Set("g_bestsw", Mathf.Max(G("g_bestsw"), MolakRules.BaseSword + us * MolakRules.SwordPerLv / SwordKeep));
        if (uc > 0f) MolakStats.Set("g_bestng", Mathf.Max(G("g_bestng"), uc * MolakRules.CorePerLv / CoreKeep));
        float hpUp = ub * MolakRules.HpPerBody + uv * MolakRules.HpPerVigor;
        if (hpUp > 0f) MolakStats.Set("g_vow_live", Mathf.Min(VowMax, Mathf.Max(G("g_vow_live"), Mathf.Ceil(hpUp / 3f))));
        if (useal > 0f) MolakStats.Set("g_cursedeaths", Mathf.Max(G("g_cursedeaths"), Mathf.Min(SealMax, useal)));
        float pts = G("g_pts");   // 쓰지 않고 남은 깨달음은 손에 익은 검으로(넷에 검술 하나)
        if (pts > 0f) { MolakStats.Set("g_bestsw", Mathf.Max(G("g_bestsw"), MolakRules.BaseSword) + pts); MolakStats.Set("g_pts", 0f); }
        if (G("g_up_tea") > 0f) MolakStats.Set("g_know_tea", 1f);
        if (G("g_up_lodge") > 0f) MolakStats.Set("g_know_lodge", 1f);
    }

    /// <summary>회차 시작치(InitRun 이 부른다).</summary>
    public static void ApplyStart ()
    {
        float hp0 = StartHp();
        MolakStats.Set("hpmax", hp0); MolakStats.Set("hp", hp0);
        MolakStats.Set("sw", StartSword());
        MolakStats.Set("ng", StartCore());
        MolakStats.Set("seallv", SealLv());
        MolakStats.Set("g_up_mem", Mathf.Max(MemLv(), G("g_membuy")));
        if (G("g_know_tea") > 0f) MolakStats.Set("f_tea", 1f);          // 청심차 비방을 안다: 아침마다 달여 마신다
        if (G("g_know_lodge") > 0f) MolakStats.Set("f_hajinfree", 1f);  // 빈칸 방값을 안다: 첫날부터 외상으로 단다
    }

    /// <summary>이번 생의 뜻을 이루었는가(@lifenote 도 같은 셈을 쓴다).</summary>
    public static bool VowOk ()
    {
        int vow = Mathf.RoundToInt(G("vow"));
        float rank = G("rank");
        if (vow == 1) return rank > 0f && rank <= MolakRules.VowRevengeRank;
        if (vow == 2) return G("aff") >= MolakRules.VowBondAff || System.Array.Exists(MolakRules.VowBondFlags, f => G(f) > 0f);
        if (vow == 3) return G("day") >= MolakRules.VowSurviveDay;
        return false;
    }

    /// <summary>0.4.55 이번 생에 지키지 못한 사람·당한 배신 — 눈을 뜬 아침의 장면을 고른다(MolakOver #RgGrief).
    /// 1 소아 2 하진무 3 암향 4 백초 할멈 5 곽산의 아내 6 쥐눈이 장의 배신 7 맹의 저울 8 참새가 판 소식.</summary>
    static float GriefKind ()
    {
        if (G("f_soadead") > 0f) return 1f;
        if (G("f_hajindead") > 0f) return 2f;
        if (G("f_amhdead") > 0f) return 3f;
        if (G("f_chamsold") > 0f) return 8f;
        if (NewThisLife("g_know_jangsell")) return 6f;
        if (NewThisLife("g_know_maengsell")) return 7f;
        if (G("f_herbdead") > 0f) return 4f;
        if (G("f_gwakwifedied") > 0f) return 5f;
        return 0f;
    }
    static bool NewThisLife (string key)
    {
        if (G(key) <= 0f) return false;
        string snap = "," + MolakStats.Str("knowsnap") + ",";
        for (int i = 0; i < MolakKnow.All.Length; i++)
            if (MolakKnow.All[i].Keys.Split('|')[0] == key) return !snap.Contains("," + i + ",");
        return false;
    }

    static readonly HashSet<string> NotDeaths = new HashSet<string> { "등극", "합벽", "성사", "순검", "원행", "재건", "도주" };
    static readonly HashSet<string> Blade = new HashSet<string> { "비무 패배", "소아의 검", "야습", "원수", "생사결", "자객", "협", "빚", "소아의 오인" };

    /// <summary>한 생이 끝날 때 한 번: 남는 것을 정산하고 보여 줄 줄을 만든다. 표시 변수 rm_text · rm_c · nx_sw · nx_ng · nx_hp.</summary>
    public static void OnLifeEnd ()
    {
        if (G("rm_done") > 0f) return;
        MolakStats.Set("rm_done", 1f);
        if (G("practice") > 0f) return;
        Migrate();
        float sw0 = StartSword(), ng0 = StartCore(), hp0 = StartHp(), seal0 = SealLv();
        var lines = new List<string>();
        string cause = MolakStats.Str("deathcause", "");
        bool died = !string.IsNullOrEmpty(cause) && cause != "none" && !NotDeaths.Contains(cause);

        // 손이 기억한 검
        if (G("sw") > G("g_bestsw")) MolakStats.Set("g_bestsw", G("sw"));
        if (G("ng") > G("g_bestng")) MolakStats.Set("g_bestng", G("ng"));
        // 겪어 본 죽음
        bool newScar = false;
        if (died)
        {
            string s = MolakStats.Str("g_scars", "");
            if (s == "none") s = "";
            if (!("|" + s + "|").Contains("|" + cause + "|") && Scars() < ScarMax) { MolakStats.SetStr("g_scars", s.Length > 0 ? s + "|" + cause : cause); newScar = true; }
            if (cause == "사술 폭주") MolakStats.Add("g_cursedeaths", 1f);
        }
        // 이번 생의 뜻
        int vow = Mathf.RoundToInt(G("vow"));
        if (vow > 0 && VowOk())
        {
            string vk = vow == 1 ? "g_vow_rev" : vow == 2 ? "g_vow_bond" : "g_vow_live";
            if (G(vk) < VowMax) MolakStats.Add(vk, 1f);
        }
        float sw1 = StartSword(), ng1 = StartCore(), hp1 = StartHp(), seal1 = SealLv();
        MolakStats.Set("griefk", died && cause != "희생" ? GriefKind() : 0f);
        if (sw1 > sw0) lines.Add("<color=#C9A227>손이 기억한 검</color> — 다음 생은 검술 " + sw1.ToString("0") + "로 깨어난다.");
        if (ng1 > ng0) lines.Add("<color=#C9A227>단전에 남은 진기</color> — 다음 생은 내공 " + ng1.ToString("0") + "로 깨어난다.");
        if (newScar) lines.Add("<color=#C9A227>처음 겪은 죽음(" + cause + ")</color> — 몸이 그 아픔을 기억한다. 체력 상한 " + hp1.ToString("0") + ".");
        else if (hp1 > hp0) lines.Add("<color=#C9A227>살아남은 몸</color> — 체력 상한 " + hp1.ToString("0") + ".");
        if (seal1 > seal0) lines.Add("<color=#C9A227>터져 본 문양</color> — 한 번 터진 자리는 다음엔 덜 끓는다. 저주가 덜 오른다.");

        // 청운검결의 조각
        string kind = MolakStats.Str("duelkind", "none");
        float lifewins = G("lifewins");
        foreach (var f in Forms)
        {
            if (G("g_form_" + f.Key) > 0f) continue;
            bool get = false;
            if (f.Key == "step") get = died && Blade.Contains(cause);
            else if (f.Key == "eye") get = (died && (cause == "소아의 검" || (cause == "비무 패배" && DuelCommand.IsBossKind(kind)))) || G("g_runs") >= 4f;
            else if (f.Key == "chain") get = lifewins >= 3f;
            else if (f.Key == "fall") get = cause == "사술 폭주" || (died && G("curse") >= 75f);
            if (!get) continue;
            MolakStats.Set("g_form_" + f.Key, 1f);
            lines.Add("<color=#9A70C4>청운검결 「" + f.Name + "(" + f.Hanja + ")」</color> — " + f.Teach + "<br><color=#8A7A66>　" + f.Effect + ".</color>");
        }
        // 목이 기억한 칼(국밥집에서 한 번 피한다)
        if (MolakStats.Get("g_end_ohin") > 0f && MolakStats.Get("g_redo") < 1f && (cause == "소아의 검" || cause == "소아의 오인"))
        {
            MolakStats.Set("g_redo", 1f);
            lines.Add("<color=#9A70C4>목이 기억한 칼</color> — 국밥집의 그 칼을 다음 생에 한 번 피한다.");
        }
        // 앎(버릇·비밀·필살 초식)
        string snap = MolakStats.Str("knowsnap");
        var had = new HashSet<string>((snap ?? "").Split(','));
        var names = new List<string>();
        for (int i = 0; i < MolakKnow.All.Length; i++)
            if (MolakKnow.Known(MolakKnow.All[i]) && !had.Contains(i.ToString()) && !MolakKnow.All[i].Keys.StartsWith("g_form_")) names.Add("「" + MolakKnow.All[i].Name + "」");   // 검결은 위에서 따로 말했다
        if (names.Count > 0) lines.Add("<color=#9A70C4>앎</color> — " + string.Join(" ", names.ToArray()) + "<br><color=#8A7A66>　오른쪽 위 메뉴(☰)의 「남은 기억」에서 다시 볼 수 있다.</color>");
        float mem0 = G("g_up_mem"), mem1 = Mathf.Max(MemLv(), G("g_membuy"));
        if (mem1 > mem0) { MolakStats.Set("g_up_mem", mem1); lines.Add("<color=#9A70C4>또렷해진 기억</color> — 아는 것이 쌓여 지난 생의 길이 더 잘 보인다. 비무에서 기세도 더 잘 읽힌다."); }

        MolakStats.SetStr("rm_text", lines.Count > 0 ? "· " + string.Join("<br>· ", lines.ToArray()) : "");
        // 대본은 한 줄씩 찍는다(rm_1…rm_10) — 한 덩어리로 찍으면 쪽을 넘겨 위가 잘린다
        for (int k = 0; k < RmMax; k++) MolakStats.SetStr("rm_" + (k + 1), k < lines.Count ? "· " + lines[k] : "none");
        MolakStats.Set("rm_c", lines.Count);
        MolakStats.Set("nx_sw", sw1); MolakStats.Set("nx_ng", ng1); MolakStats.Set("nx_hp", hp1);
        MolakStats.Set("knowc", MolakKnow.Count());
    }
}

/// <summary>@nextbody — 상점 대신: 다음 생에 들고 갈 몸(nx_sw·nx_ng·nx_hp)과 아는 것의 수(knowc)를 채운다. 정산이 없던 길(옛 기록)에서도 쓴다.</summary>
[Alias("nextbody")]
public class NextBodyCommand : Command
{
#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        MolakMemory.Migrate();
        MolakStats.Set("nx_sw", MolakMemory.StartSword());
        MolakStats.Set("nx_ng", MolakMemory.StartCore());
        MolakStats.Set("nx_hp", MolakMemory.StartHp());
        MolakStats.Set("knowc", MolakKnow.Count());
        int forms = 0; foreach (var f in MolakMemory.Forms) if (MolakStats.Get("g_form_" + f.Key) > 0f) forms++;
        MolakStats.Set("formc", forms);
        if (MolakStats.Get("rm_done") < 1f) { MolakStats.SetStr("rm_text", ""); MolakStats.Set("rm_c", 0f); }
        try { await Engine.GetServiceOrErr<IStateManager>().SaveGlobal(); } catch { }
    }
}
