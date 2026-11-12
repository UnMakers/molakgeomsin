using Naninovel;
using UnityEngine;

/// <summary>
/// 0.4.30 비무의 합(合): 마지막 한 칼 앞에 몇 합을 주고받는다.
/// 상대의 기세 — 1 찌르러 온다(공세) 2 굳게 받는다(수비) 3 허초로 흔든다 — 를 읽고 응수한다.
///   공세엔 받아치기, 수비엔 흔들기, 허초엔 정면으로. 맞히면 기세를 잡고, 틀리면 칼을 맞는다.
/// 기세는 맞게 보일 때가 많지만 늘 맞지는 않는다(검술이 높을수록 잘 읽힌다). 지난 생에 진 상대의 수는 확실히 보인다(보라).
/// 쌓인 기세(dx_mom)는 마지막 판정(@duel)의 승산에 더해진다. 합 수: 보통 판 1, 원수·빚·뒷골목 2, 이야기 결전 3.
/// </summary>
public static class MolakDuelX
{
    public const float MomBonus = 2.5f;   // 기세 한 칸 = 승산 약 12%

    static readonly string[][] Tells = {
        new string[0],
        new[] { "상대의 어깨가 먼저 들린다. 발끝이 앞으로 쏠린다.", "칼끝이 낮게 가라앉는다. 찌르러 오는 자세다.", "숨 들이마시는 소리가 짧다. 곧 온다.", "앞발 뒤꿈치가 땅에서 뜬다." },
        new[] { "상대가 칼을 몸 앞에 세우고 반 보 물러선다.", "발이 땅에 박힌다. 먼저 오기를 기다리는 자세다.", "칼끝이 가슴 높이에서 움직이지 않는다.", "상대의 눈이 칼이 아니라 당신 발을 본다." },
        new[] { "칼끝이 자꾸 왼쪽으로 흔들린다. 눈은 오른쪽을 본다.", "어깨는 들리는데 발이 따라오지 않는다.", "칼이 크게 움직인다. 너무 크다.", "상대가 일부러 빈틈을 보인다. 너무 쉽게 열린 문이다." },
    };
    static readonly string[] WinT = { "",
        "들어오는 칼을 받아 흘리고, 손잡이로 상대의 손목을 친다. 상대가 한 걸음 밀린다.",
        "왼쪽으로 흔들고 오른쪽으로 든다. 굳게 선 칼이 반 박자 늦게 돌아온다.",
        "흔들리는 칼끝을 보지 않고 곧장 든다. 허초는 허초일 뿐이다. 상대 소매가 갈라진다." };
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

    public static void Init ()
    {
        string kind = MolakStats.Str("duelkind", "any");
        MolakStats.Set("dx_n", Rounds(kind));
        MolakStats.Set("dx_i", 0f);
        MolakStats.Set("dx_mom", 0f);
        MolakStats.Set("dx_true", 0f);
    }

    public static void Tell ()
    {
        int t = Random.Range(1, 4);
        string kind = MolakStats.Str("duelkind", "any");
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
        var pool = Tells[shown];
        string line = pool[Random.Range(0, pool.Length)];
        int i = Mathf.RoundToInt(MolakStats.Get("dx_i")) + 1, n = Mathf.RoundToInt(MolakStats.Get("dx_n"));
        string head = n > 1 ? "<color=#8A7A66>" + Num(i) + " 합.</color> " : "";
        if (sure) line = "<color=#9A70C4>지난 생에 본 수다.</color> " + line;
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
        string txt;
        if (res > 0) { txt = WinT[t]; MolakVfx.Shake(4f); MolakSfx.Draw(); }
        else if (res < 0)
        {
            txt = LoseT[t];
            float hp = MolakStats.Get("hp");
            if (hp > 2f) MolakStats.Add("hp", -1f);          // 합에서 맞는 칼은 얕다. 목숨은 마지막 한 칼이 정한다
            MolakVfx.Blood(); MolakSfx.Hit(); MolakArt.Hurt();
        }
        else { txt = EvenT[Random.Range(0, EvenT.Length)]; MolakVfx.Shake(2f); }
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
