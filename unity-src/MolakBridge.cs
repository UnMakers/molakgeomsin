using System.Collections.Generic;
using Naninovel;
using UnityEngine;

/// <summary>
/// 0.4.41 이음 문장. 우연한 사건(낮·밤 사건)과 밤으로 넘어갈 때 화면이 바뀌면, 새 쪽 맨 위에 한두 마디를 먼저 깐다.
///  · 시간이 흐른 자리(아침나절 → 오후 → 밤)
///  · 방금 일의 여운(다친 데, 전낭 무게, 문양, 사람 목소리)
///  · 여운이 없으면 주인공이 붙들고 있는 일(그물, 빈 전낭, 몸, 이번 생의 뜻)
/// 80 Days 의 길 위 문장, Sunless Sea 의 항해 사이 문장처럼 — 사건이 '뚝' 바뀌지 않고 한 사람의 하루로 이어지게.
/// 사건 첫머리가 장소를 말하므로 이음 문장은 장소 이름을 대지 않는다.
/// </summary>
public static class MolakBridge
{
    static readonly string[] Snap = { "hp", "mn", "curse", "sw", "ng", "sl", "aff", "rank" };
    static float[] snap;
    static float snapDay = -1f;
    static string snapChar;
    static int dayEvents; static float countDay = -1f;
    static readonly HashSet<string> used = new HashSet<string>();
    static float usedRunDay = 999f;

    // ---- 시간 ----
    static readonly string[] TimeDay1 = {
        "아침나절이 지나간다.", "해가 처마 끝을 넘는다.", "객잔을 나서니 거리가 다 깨어 있다.", "볕이 마당을 반쯤 채웠다.",
        "장 서는 소리가 골목 끝까지 밀려온다.", "물지게꾼들이 벌써 두 번째 길을 돈다.",
    };
    static readonly string[] TimeDay2 = {
        "해가 기운다.", "오후 볕이 비스듬하다.", "점심때가 지나 거리가 한산해진다.", "그림자가 한 뼘 길어졌다.",
        "처마 밑 그늘이 길게 눕는다.", "해가 서쪽 담 위에 걸린다.",
    };
    static readonly string[] TimeNight = {
        "등불이 하나둘 켜진다.", "밤이 깊어 간다.", "야경꾼의 딱따기 소리가 멀리서 들린다.", "달이 지붕 위로 올라선다.",
        "개 짖는 소리가 골목을 건너간다.", "문 닫는 소리가 차례로 이어진다.",
    };

    // ---- 여운 ----
    static readonly string[] HurtBad = { "소매 끝에 마른 피가 버석거린다.", "숨을 고를 때마다 상처가 당긴다.", "다리를 조금 끌며 걷는다.", "옷 속으로 감은 천이 축축하다." };
    static readonly string[] HurtLight = { "베인 자리가 욱신거린다.", "손등의 생채기가 따끔하다.", "어깨가 뻐근하다." };
    static readonly string[] CurseUp = { "가슴의 문양이 한동안 미지근하게 들썩인다.", "문양 언저리가 저릿하다.", "옷깃 밑이 화끈거려 자꾸 손이 간다." };
    static readonly string[] CoinUp = { "전낭이 묵직해졌다.", "허리춤에서 은자가 짤랑거린다.", "전낭 끈을 한 번 더 조여 맨다." };
    static readonly string[] CoinDown = { "가벼워진 전낭이 허리에서 덜렁거린다.", "남은 은자를 손끝으로 세어 본다.", "전낭이 홀쭉하다." };
    static readonly string[] Learned = { "방금 익힌 것이 손바닥에 남아 있다.", "단전의 온기가 아직 가시지 않았다.", "걸으면서 손목을 몇 번 돌려 본다." };
    static readonly string[] Killed = { "칼날을 한 번 더 닦는다.", "손끝의 떨림이 늦게 온다." };
    static readonly string[] RankUp = { "서열록 어딘가에서 이름 하나가 자리를 바꿨을 것이다.", "누가 뒤에서 이름을 부르는 것 같아 돌아본다. 아무도 없다." };
    static readonly string[] Person = { "{0}의 말이 귓가에 남아 있다.", "{0}의 얼굴이 자꾸 떠오른다.", "{0}이(가) 마지막에 한 말을 곱씹는다." };

    // ---- 붙들고 있는 일 ----
    static readonly string[] NetClose = { "그물이 조여 온다. 남은 날을 손가락으로 세어 본다.", "관아 앞을 지날 때마다 방(榜)이 하나씩 늘어 있다." };
    static readonly string[] Broke = { "전낭이 비었다. 오늘 밤 방값이 걱정이다.", "빈 전낭이 허리에서 가볍게 흔들린다." };
    static readonly string[] Weak = { "몸이 무겁다. 오늘은 칼을 덜 뽑아야 한다.", "두어 걸음마다 숨이 찬다." };
    static readonly string[] CurseHigh = { "옷깃을 여민다. 문양이 목까지 올라왔다.", "문양이 숨 쉬는 박자가 심장과 어긋난다." };
    static readonly string[] VowRevenge = { "서열록 이름 몇 줄을 머릿속으로 짚어 본다.", "혈마교의 깃발이 걸린 관아 지붕을 한 번 올려다본다." };
    static readonly string[] VowBond = { "오늘은 누구를 만나게 될지 생각한다.", "지난 생에 놓친 얼굴들을 하나씩 떠올린다." };
    static readonly string[] VowSurvive = { "살아남는 쪽으로만 발을 옮긴다.", "그물이 닿지 않는 길을 셈해 본다." };
    static readonly string[] Plain = { "", "", "걸음을 옮긴다.", "생각을 접고 걷는다." };

    static readonly Dictionary<string, string> CharName = new Dictionary<string, string> {
        {"soa","소아"},{"soa_smile","소아"},{"soa_fierce","소아"},{"soa_grief","소아"},{"hajinmu","하진무"},{"granny","백초 할멈"},
        {"yonggae","용개"},{"gwaksan","곽산"},{"amhyang","암향"},{"amhyang_soft","암향"},{"amhyang_pain","암향"},{"cheongmyo","청묘"},
        {"cheongmyo_robe","청묘"},{"jang","쥐눈이 장"},{"ggoji","꼬지"},{"jinbaek","진백"},{"oldman","노인"},
    };

    static float G (string k) { return MolakStats.Get(k); }

    static string Pick (string[] pool)
    {
        if (pool == null || pool.Length == 0) return "";
        var fresh = new List<string>();
        foreach (var s in pool) if (!used.Contains(s)) fresh.Add(s);
        if (fresh.Count == 0) { foreach (var s in pool) used.Remove(s); fresh.AddRange(pool); }
        var p = fresh[Random.Range(0, fresh.Count)];
        if (p.Length > 0) used.Add(p);
        return p;
    }

    static string Josa (string name, string text)
    {
        // "{0}이(가)" 를 받침에 맞춰
        if (!text.Contains("이(가)")) return string.Format(text, name);
        char last = name[name.Length - 1];
        bool batchim = last >= 0xAC00 && last <= 0xD7A3 && (last - 0xAC00) % 28 != 0;
        return string.Format(text.Replace("이(가)", batchim ? "이" : "가"), name);
    }

    /// <summary>kind: "day" | "night" | "dusk"(밤으로 넘어갈 때, 시간 문장 없이 여운만). 할 말이 없으면 null.</summary>
    public static string Compose (string kind, string nextEvent)
    {
        float day = G("day");
        if (day < usedRunDay) used.Clear();   // 새 생
        usedRunDay = day;
        if (day != countDay) { countDay = day; dayEvents = 0; }

        // 1) 여운: 같은 날 앞 사건 이후로 달라진 것
        string after = "";
        if (snap != null && snapDay == day)
        {
            float dHp = G("hp") - snap[0], dMn = G("mn") - snap[1], dCurse = G("curse") - snap[2];
            float dLearn = (G("sw") - snap[3]) + (G("ng") - snap[4]), dSl = G("sl") - snap[5], dAff = G("aff") - snap[6];
            float r0 = snap[7], r1 = G("rank");
            bool rankUp = r1 > 0f && (r0 <= 0f || r1 < r0);
            if (dHp <= -3f) after = Pick(HurtBad);
            else if (dCurse >= 3f) after = Pick(CurseUp);
            else if (dHp < 0f) after = Pick(HurtLight);
            else if (rankUp) after = Pick(RankUp);
            else if (dSl >= 2f) after = Pick(Killed);
            else if (dMn >= 5f) after = Pick(CoinUp);
            else if (dMn <= -3f) after = Pick(CoinDown);
            else if (dLearn >= 2f) after = Pick(Learned);
            else if (!string.IsNullOrEmpty(snapChar) && (dAff > 0f || Random.value < 0.5f)) { string nm; if (CharName.TryGetValue(snapChar, out nm)) after = Josa(nm, Pick(Person)); }
        }

        // 2) 여운이 없으면 붙들고 있는 일(가끔)
        if (after.Length == 0 && kind != "dusk" && Random.value < 0.55f)
        {
            if (day >= MolakRules.NetWarnDay) after = Pick(NetClose);
            else if (G("hp") <= G("hpmax") / 3f) after = Pick(Weak);
            else if (G("mn") <= 1f) after = Pick(Broke);
            else if (G("curse") >= 60f) after = Pick(CurseHigh);
            else
            {
                int vow = Mathf.RoundToInt(G("vow"));
                after = vow == 1 ? Pick(VowRevenge) : vow == 2 ? Pick(VowBond) : vow == 3 ? Pick(VowSurvive) : Pick(Plain);
            }
        }

        // 3) 시간
        string time = "";
        if (kind == "day") time = Pick(dayEvents == 0 ? TimeDay1 : TimeDay2);
        else if (kind == "night") time = Pick(TimeNight);

        // 다음 사건의 기준점
        TakeSnap(day, kind == "dusk" ? null : nextEvent);
        if (kind != "dusk") dayEvents++;

        string line = (time + (time.Length > 0 && after.Length > 0 ? " " : "") + after).Trim();
        if (line.Length == 0) return null;
        return "<color=#8A7A66>" + line + "</color>";
    }

    static void TakeSnap (float day, string nextEvent)
    {
        if (snap == null) snap = new float[Snap.Length];
        for (int i = 0; i < Snap.Length; i++) snap[i] = G(Snap[i]);
        snapDay = day;
        snapChar = null;
        if (!string.IsNullOrEmpty(nextEvent)) MolakBridgeData.EventChar.TryGetValue(nextEvent, out snapChar);
    }

    /// <summary>회귀·새 생 시작 때 비운다.</summary>
    public static void Reset () { snap = null; snapDay = -1f; snapChar = null; used.Clear(); dayEvents = 0; countDay = -1f; }
}

/// <summary>@bridge kind:day|night|dusk — 이음 문장을 bridgetext 에 담는다(없으면 "none").
/// 담았으면 다음 본문은 새 쪽의 첫 줄로 찍히고, 입력을 기다리지 않으며, 바로 뒤 사건의 첫 그림·첫 글은 같은 쪽에 이어진다.</summary>
[Alias("bridge")]
public class BridgeCommand : Command
{
    public StringParameter Kind;
#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        var sp = Engine.GetService<IScriptPlayer>();
        string kind = Assigned(Kind) ? Kind.Value : "day";
        string t = null;
        if (sp == null || !sp.Skipping) t = MolakBridge.Compose(kind, MolakStats.Str("nextevent", "none"));
        MolakStats.SetStr("bridgetext", string.IsNullOrEmpty(t) ? "none" : t);
        if (!string.IsNullOrEmpty(t)) MolakFlow.BridgeNext();
    }
}
