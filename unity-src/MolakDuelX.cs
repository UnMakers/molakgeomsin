using System.Collections.Generic;
using Naninovel;
using UnityEngine;

/// <summary>
/// 0.4.30 비무의 합(合): 마지막 한 칼 앞에 몇 합을 주고받는다.
/// 상대의 기세 — 1 찌르러 온다(공세) 2 굳게 받는다(수비) 3 허초로 흔든다 — 를 읽고 응수한다.
///   공세엔 받아치기, 수비엔 흔들기, 허초엔 정면으로. 맞히면 기세를 잡고, 틀리면 칼을 맞는다.
/// 기세는 맞게 보일 때가 많지만 늘 맞지는 않는다(검술이 높을수록 잘 읽힌다). 지난 생에 진 상대의 수는 확실히 보인다(보라).
/// 쌓인 기세(dx_mom)는 마지막 판정(@duel)의 승산에 더해진다. 합 수: 보통 판 1, 원수·빚·뒷골목 2, 이야기 결전 3.
/// 0.4.31 버릇: 상대마다 즐겨 내는 수가 다르고, 몇몇은 수를 고르는 방식이 있다
///   (교주는 당신이 방금 낸 수를 노리고, 곽산은 따라 하고, 남궁현은 정석대로 차례를 지키고, 진백은 같은 수를 두 번 내지 않고, 호치는 밀리면 웅크린다).
///   지난 생에 맞붙어 본 상대이거나 눈이 밝으면(검술) 첫 합에 그 버릇이 한 줄로 보인다.
/// </summary>
public static class MolakDuelX
{
    public const float MomBonus = 2.5f;   // 기세 한 칸 = 승산 약 12%

    // 수 고르는 방식
    const int ModeW = 0, ModeCounter = 1, ModeMirror = 2, ModeCycle = 3, ModeCower = 4, ModeNoRepeat = 5;

    class Habit
    {
        public int[] W; public int Mode; public float ModeP; public string Label; public bool Always;
        public string[][] Lines;   // 그 상대만의 기세 문장(없으면 공용)
        public Habit (int a, int d, int f, string label, int mode = ModeW, float modeP = 0f, bool always = false, string[][] lines = null)
        { W = new[] { a, d, f }; Label = label; Mode = mode; ModeP = modeP; Always = always; Lines = lines; }
    }

    static readonly Dictionary<string, Habit> Habits = new Dictionary<string, Habit> {
        { "hochi", new Habit(5, 1, 2, "찌르기를 즐긴다. 밀리면 웅크린다.", ModeCower, 0.7f, false, new[] {
            new[] { "호치가 혀로 입술을 축인다. 독 묻은 칼끝이 앞으로 기운다.", "호치의 무릎이 굽는다. 덤벼들 자세다." },
            new[] { "호치가 칼을 가슴께로 끌어당긴다. 웬일로 기다린다." },
            new[] { "호치가 히죽 웃으며 칼을 크게 돌린다. 웃음이 너무 길다." } }) },
        { "jinbaek", new Habit(2, 1, 5, "허초가 많고, 같은 수를 두 번 잇지 않는다.", ModeNoRepeat, 1f, false, new[] {
            new[] { "진백의 칼이 소리 없이 내려간다. 늑대가 몸을 낮출 때처럼." },
            new[] { "진백이 한 발을 뒤로 뺀다. 눈은 당신 손목에 가 있다." },
            new[] { "진백의 칼끝이 둘로 보인다. 하나는 거짓이다.", "진백이 왼어깨를 열어 보인다. 일부러 연 자리다." } }) },
        { "lord", new Habit(3, 3, 3, "앞 합에서 당신이 낸 수를 노린다.", ModeCounter, 0.6f, false, new[] {
            new[] { "교주의 소매가 바람도 없이 부푼다. 온다." },
            new[] { "교주가 칼을 거꾸로 쥔 채 서 있다. 움직이지 않는다." },
            new[] { "교주가 웃는다. 칼은 보이지 않고 그림자만 길다." } }) },
        { "rival", new Habit(3, 3, 3, "앞 합에서 당신이 낸 수를 따라 한다.", ModeMirror, 0.6f, false, new[] {
            new[] { "곽산이 이를 악문다. 당신 발놀림을 흉내 낸 걸음이다." },
            new[] { "곽산이 당신과 똑같은 높이로 칼을 세운다." },
            new[] { "곽산의 칼이 당신이 쓰던 허초를 그린다." } }) },
        { "namgung", new Habit(3, 3, 3, "정석대로 공세, 수비, 허초를 차례로 낸다.", ModeCycle, 0.8f, false, new[] {
            new[] { "남궁현의 칼이 교본대로 곧게 뻗어 온다." },
            new[] { "남궁현이 칼을 세우고 숨을 고른다. 세가에서 배운 받음이다." },
            new[] { "남궁현의 칼끝이 꼭 세 번 흔들린다. 배운 대로다." } }) },
        { "soa", new Habit(2, 3, 3, "받아 흘리는 검이다. 서두르지 않는다.", ModeW, 0f, false, new[] {
            new[] { "소아의 붉은 끈이 뒤로 날린다. 발이 먼저 나온다." },
            new[] { "소아가 검을 눕혀 든다. 적운검파의 받는 자세다." },
            new[] { "소아의 검끝이 바람 맞은 꽃가지처럼 흔들린다. 어디로 떨어질지 모른다." } }) },
        { "kyona", new Habit(4, 1, 3, "물어뜯듯 몰아친다.") },
        { "wolf", new Habit(1, 4, 2, "사슬로 받아 놓고 기다린다.") },
        { "hunt3", new Habit(2, 4, 2, "법도대로 받으며 앞 합에서 당신이 낸 수의 틈을 본다.", ModeCounter, 0.35f) },
        { "cheolsan", new Habit(2, 5, 1, "무거운 칼로 버틴다.") },
        { "chainmob", new Habit(3, 3, 1, "여럿이 사슬을 던지고 당긴다.") },
        { "hunt1", new Habit(3, 1, 3, "척후답게 찔러 보고 빠진다.") },
        { "hunt2", new Habit(4, 2, 2, "혈랑대의 칼은 먼저 든다.") },
        { "raid", new Habit(5, 1, 2, "어둠 속에서 먼저 찌른다.", ModeW, 0f, true) },
        { "assassin", new Habit(2, 1, 5, "보이는 칼은 미끼다.", ModeW, 0f, true) },
        { "tax", new Habit(4, 2, 1, "머릿수로 밀어붙인다.", ModeW, 0f, true) },
        { "debt", new Habit(4, 2, 1, "주먹패라 앞뒤 없이 덤빈다.", ModeW, 0f, true) },
        { "grudge", new Habit(5, 1, 2, "원한이 칼을 앞세운다.", ModeW, 0f, true) },
    };
    // 이름 없는 상대의 성미 — 판마다 하나를 뽑는다
    static readonly Habit[] Tempers = {
        new Habit(3, 3, 3, ""),
        new Habit(5, 2, 2, "성미가 급해 보인다."),
        new Habit(2, 5, 2, "먼저 오지 않는 자다."),
        new Habit(2, 2, 5, "손속이 교활하다."),
    };

    static readonly string[][] Tells = {
        new string[0],
        new[] { "상대의 어깨가 먼저 들린다. 발끝이 앞으로 쏠린다.", "칼끝이 낮게 가라앉는다. 찌르러 오는 자세다.", "숨 들이마시는 소리가 짧다. 곧 온다.", "앞발 뒤꿈치가 땅에서 뜬다." },
        new[] { "상대가 칼을 몸 앞에 세우고 반 보 물러선다.", "발이 땅에 박힌다. 먼저 오기를 기다리는 자세다.", "칼끝이 가슴 높이에서 움직이지 않는다.", "상대의 눈이 칼이 아니라 당신 발을 본다." },
        new[] { "칼끝이 자꾸 왼쪽으로 흔들린다. 눈은 오른쪽을 본다.", "어깨는 들리는데 발이 따라오지 않는다.", "칼이 크게 움직인다. 너무 크다.", "상대가 일부러 빈틈을 보인다. 너무 쉽게 열린 문이다." },
    };
    static readonly string[] WinT = { "",
        "들어오는 칼을 받아 흘리고, 손잡이로 상대의 손목을 친다. 상대가 한 걸음 밀린다.",
        "왼쪽으로 흔들고 오른쪽으로 든다. 굳게 선 칼이 반 박자 늦게 돌아온다.",
        "흔들리는 칼끝을 보지 않고 곧장 든다. 상대 소매가 갈라진다." };
    static readonly string[] LoseT = { "",
        "흔드는 사이에 상대 칼이 먼저 들어온다. 팔뚝에 붉은 줄이 그어진다.",
        "정면으로 든 칼이 막히고, 되돌아온 칼에 어깨를 베인다.",
        "막으려던 자리에 칼이 없다. 옆구리가 서늘하다." };
    static readonly string[] EvenT = { "칼과 칼이 맞물렸다 떨어진다. 아무도 얻지 못했다.", "서로 한 걸음씩 물러선다. 숨만 거칠어졌다.", "쇳소리 한 번. 둘 다 제자리다." };

    public static int Rounds (string kind)
    {
        if (DuelCommand.IsBossKind(kind) || kind == "raid" || kind == "assassin" || kind == "tax") return 3;
        if (kind == "grudge" || kind == "debt" || kind == "back" || MolakStats.Get("dsit") == 7) return 2;
        return 1;
    }

    static Habit Cur (string kind)
    {
        Habit h;
        if (Habits.TryGetValue(kind, out h)) return h;
        int i = Mathf.Clamp(Mathf.RoundToInt(MolakStats.Get("dx_temper")), 0, Tempers.Length - 1);
        return Tempers[i];
    }

    public static void Init ()
    {
        string kind = MolakStats.Str("duelkind", "any");
        MolakStats.Set("dx_n", Rounds(kind));
        MolakStats.Set("dx_i", 0f);
        MolakStats.Set("dx_mom", 0f);
        MolakStats.Set("dx_true", 0f);
        MolakStats.Set("dx_last", 0f);      // 내가 방금 낸 수
        MolakStats.Set("dx_lastres", 0f);   // 방금 합의 결과(+1 내가 이김)
        MolakStats.Set("dx_cyc", Random.Range(1, 4));
        MolakStats.Set("dx_temper", Habits.ContainsKey(kind) ? 0f : Random.Range(0, Tempers.Length));
        // 버릇이 보이는가: 지난 생에 맞붙어 본 이름 있는 상대, 판이 무거워 숨길 수 없는 버릇, 혹은 검술로 읽어 내는 눈
        var h = Cur(kind);
        bool named = Habits.ContainsKey(kind) && !h.Always;
        bool seen = MolakStats.Get("g_seen_" + kind) > 0 || MolakStats.Get("g_know_" + kind) > 0;
        float sw = MolakStats.Get("sw");
        bool show = !string.IsNullOrEmpty(h.Label) && (h.Always || seen || (named ? sw >= 16 : sw >= 10));
        MolakStats.Set("dx_showhab", show ? 1f : 0f);
        if (named) MolakStats.Set("g_seen_" + kind, 1f);
    }

    static int Pick (Habit h)
    {
        int last = Mathf.RoundToInt(MolakStats.Get("dx_last"));
        int lastT = Mathf.RoundToInt(MolakStats.Get("dx_true"));
        int i = Mathf.RoundToInt(MolakStats.Get("dx_i"));
        if (i > 0)
        {
            switch (h.Mode)
            {
                case ModeCounter:   // 당신이 방금 낸 수를 이기는 기세로
                    if (last > 0 && Random.value < h.ModeP) return last == 1 ? 2 : last == 2 ? 3 : 1;
                    break;
                case ModeMirror:    // 당신이 방금 낸 수를 그대로(정면→공세, 받아치기→수비, 흔들기→허초)
                    if (last > 0 && Random.value < h.ModeP) return last;
                    break;
                case ModeCycle:
                    if (Random.value < h.ModeP) { int c = Mathf.RoundToInt(MolakStats.Get("dx_cyc")) % 3 + 1; MolakStats.Set("dx_cyc", c); return c; }
                    break;
                case ModeCower:     // 방금 밀렸으면 웅크린다
                    if (MolakStats.Get("dx_lastres") > 0 && Random.value < h.ModeP) return 2;
                    break;
                case ModeNoRepeat:
                    if (lastT > 0) { var w2 = (int[])h.W.Clone(); w2[lastT - 1] = 0; return Weighted(w2); }
                    break;
            }
        }
        else if (h.Mode == ModeCycle) { int c = Mathf.RoundToInt(MolakStats.Get("dx_cyc")); return Mathf.Clamp(c, 1, 3); }
        return Weighted(h.W);
    }

    static int Weighted (int[] w)
    {
        int sum = w[0] + w[1] + w[2];
        if (sum <= 0) return Random.Range(1, 4);
        int r = Random.Range(0, sum);
        return r < w[0] ? 1 : r < w[0] + w[1] ? 2 : 3;
    }

    public static void Tell ()
    {
        string kind = MolakStats.Str("duelkind", "any");
        var h = Cur(kind);
        int t = Pick(h);
        float sit = MolakStats.Get("dsit");
        bool sure = MolakStats.Get("memwarn") > 0;
        float p = Mathf.Clamp(0.58f + MolakStats.Get("sw") * 0.008f + (MolakStats.Get("g_up_mem") > 0 ? 0.05f : 0f), 0.58f, 0.86f);
        if (sit == 11) p = 0.34f;                     // 취한 걸음은 읽히지 않는다
        if (sit == 1) p -= 0.08f;                     // 빗속: 흐리다
        if (MolakStats.Get("injury") > 0) p -= 0.06f; // 아픈 몸은 눈도 늦다
        if (sure) p = 1f;
        int shown = t;
        if (Random.value > p) { shown = Random.Range(1, 3); if (shown >= t) shown++; }
        MolakStats.Set("dx_true", t);
        string line;
        if (h.Lines != null && h.Lines[shown - 1].Length > 0 && Random.value < 0.6f) { var pl = h.Lines[shown - 1]; line = pl[Random.Range(0, pl.Length)]; }
        else { var pool = Tells[shown]; line = pool[Random.Range(0, pool.Length)]; }
        int i = Mathf.RoundToInt(MolakStats.Get("dx_i")) + 1, n = Mathf.RoundToInt(MolakStats.Get("dx_n"));
        string head = "";
        if (n > 1)
        {
            head = "<color=#8A7A66>" + Num(i) + " 합.</color> ";
            if (i > 1)
            {
                int mom = Mathf.RoundToInt(MolakStats.Get("dx_mom"));
                string mc = mom > 0 ? "#C9A227" : mom < 0 ? "#B0443A" : "#8A7A66";
                head = "<color=#8A7A66>" + Num(i) + " 합 · 기세</color> <color=" + mc + ">" + (mom > 0 ? "+" + mom : mom < 0 ? "−" + (-mom) : "0") + "</color> ";
            }
        }
        if (sure) line = "<color=#9A70C4>지난 생에 본 수다.</color> " + line;
        if (i == 1 && MolakStats.Get("dx_showhab") > 0) line += "<br><color=#8A7A66>버릇 — " + h.Label + "</color>";
        MolakStats.SetStr("dx_telltext", head + line);
    }

    public static void Act (int a)
    {
        int t = Mathf.RoundToInt(MolakStats.Get("dx_true"));
        int best = t == 1 ? 2 : t == 2 ? 3 : 1;     // 공세→받아치기, 수비→흔들기, 허초→정면
        int worst = t == 1 ? 3 : t == 2 ? 1 : 2;
        int res = a == best ? 1 : a == worst ? -1 : 0;
        MolakStats.Add("dx_mom", res);
        MolakStats.Add("dx_i", 1f);
        MolakStats.Set("dx_last", a);
        MolakStats.Set("dx_lastres", res);
        string txt;
        if (res > 0)
        {
            txt = WinT[t];
            MolakVfx.Clash(true);                       // 흰 섬광, 크게 흔들림
            MolakArt.Hurt();                            // 상대가 밀려 흔들린다
            MolakSfx.Play("hit_drum", 0.55f, 1.3f);
            MolakVfx.Stamp("破", "기세 +1", false);
        }
        else if (res < 0)
        {
            txt = LoseT[t];
            float hp = MolakStats.Get("hp");
            bool cut = hp > 2f;
            if (cut) MolakStats.Add("hp", -1f);          // 합에서 맞는 칼은 얕다. 목숨은 마지막 한 칼이 정한다
            MolakVfx.Blood(); MolakSfx.Hit();
            MolakVfx.Stamp("傷", cut ? "기세 −1 · 체력 −1" : "기세 −1", false);
        }
        else { txt = EvenT[Random.Range(0, EvenT.Length)]; MolakVfx.Clash(false); }
        MolakStats.SetStr("dx_restext", txt);
        int i = Mathf.RoundToInt(MolakStats.Get("dx_i")), n = Mathf.RoundToInt(MolakStats.Get("dx_n"));
        if (i >= n)
        {
            float mom = MolakStats.Get("dx_mom");
            float mem = MolakStats.Get("memwarn") > 0 ? 1f + (MolakStats.Get("g_up_mem") > 0 ? 1f : 0f) : 0f;
            float odds = MolakDuel.Odds(MolakDuel.Pending, mom * MomBonus + mem);
            int o10 = Mathf.Clamp(Mathf.RoundToInt(odds / 10f), 1, 9);
            MolakStats.Set("odds10", o10);
            string m = mom >= 2 ? "기세가 완전히 넘어왔다." : mom >= 1 ? "기세를 잡았다." : mom <= -2 ? "기세를 다 내줬다. 몸이 반 박자씩 늦다." : mom <= -1 ? "기세가 상대 쪽으로 기운다." : "기세는 팽팽하다.";
            MolakStats.SetStr("dx_momtext", m + " 마지막 한 칼, 승산은 <color=#C9A227>열에 " + o10 + "</color>.");
        }
    }

    static string Num (int i) { return i == 1 ? "첫" : i == 2 ? "둘째" : i == 3 ? "셋째" : i + "째"; }
}

/// <summary>@duelx do:init|tell  /  @duelx act:1(정면)|2(받아치기)|3(흔들기)</summary>
[Alias("duelx")]
public class DuelXCommand : Command
{
    public StringParameter Do;
    public IntegerParameter Act;

#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        if (Assigned(Act)) { MolakDuelX.Act(Act.Value); return; }
        var d = Assigned(Do) ? Do.Value : "tell";
        if (d == "init") MolakDuelX.Init(); else MolakDuelX.Tell();
    }
}
