using System.Collections.Generic;
using MolakVn;
using UnityEngine;

/// <summary>
/// 0.4.67 동행(同行) — 상황에 따라 히로인이 곁에 붙는다(언메이커: "상황에 따라 동행하는 것도 좋지 않아?").
///  · 낮·밤 사건의 이음 문장 끝에 한 줄: 만난 히로인 가운데 하나가 따라나선다(그 사건의 주인공이 바로 그 히로인이면 붙지 않는다).
///  · 비무판(@companion at:duel): 소아·암향은 상대의 버릇을 짚어 승산 한 칸(compbonus), 청묘·참새는 구경꾼 자리에서 한마디.
///  · 저잣거리(@companion at:town): 참새는 흥정으로 은자 한 냥을 깎아 돌려준다. 나머지는 한마디.
///  · 함께 다닌 날은 그 히로인의 정이 하루 한 번 오른다(compday_*).
///  · 정이 깊을수록 자주 붙는다. 바로 앞에 붙은 사람은 덜 붙는다.
/// </summary>
public static class MolakCompanion
{
    struct H { public string Id, Name, Aff; public System.Func<bool> Ok; }
    static float G (string k) { return MolakStats.Get(k); }

    static readonly H[] All = {
        new H { Id = "soa",  Name = "소아", Aff = "aff",     Ok = () => G("f_soamet") > 0 && G("f_soadead") < 1 && G("f_leftwithsoa") < 1 },
        new H { Id = "amh",  Name = "암향", Aff = "amhaff",  Ok = () => G("f_amhyangknown") > 0 && G("f_amhdead") < 1 && G("f_amhgone") < 1 },
        new H { Id = "cm",   Name = "청묘", Aff = "cmaff",   Ok = () => G("f_jbsister") > 0 },
        new H { Id = "cham", Name = "참새", Aff = "chamaff", Ok = () => G("b_ch1") > 0 && G("f_chamgone") < 1 },
    };

    // 0.4.68 따라나선 까닭이 보이게: 어디서 만나 왜 같이 가는지를 한 줄 안에 담는다(언메이커: "소아가 어떻게 따라나섰다는 거임?")
    static readonly Dictionary<string, string[]> Day = new Dictionary<string, string[]> {
        { "soa", new[] {
            "객잔 문을 나서는데 소아가 문설주에 기대 서 있다. \"저자로 가는 길이면 같이 가자. 마침 숫돌을 하나 사야 하거든.\" 둘이 나란히 걷는다.",
            "계단을 내려가자 소아가 따라 일어선다. \"혼자 나갔다가 또 칼 맞고 들어올까 봐 그런다. 다른 뜻은 없으니 오해는 하지 마라.\"",
            "골목 어귀에서 소아와 마주친다. \"마침 잘 만났다. 나도 그쪽에 볼일이 있으니 같이 가지.\"",
            "소아가 반 걸음 뒤에서 따라온다. 왜 따라오느냐고 묻자 \"네가 어디서 무엇을 하고 다니는지 보는 것도 내 일이다\"라고만 한다." } },
        { "amh", new[] {
            "골목 모퉁이에서 침향 냄새가 난다. 암향이 양산을 접으며 다가온다. \"오늘은 루에서 저를 찾는 손님이 없어서요. 공자 가시는 길에 잠깐 같이 걸어도 될까요?\"",
            "암향루 앞을 지나는데 뒷문에서 암향이 나온다. \"심부름 가는 길이에요. 방향이 같으니 같이 가요.\"",
            "암향이 찻집 이층 난간에서 손을 흔든다. 내려와서 걸음을 맞춘다. \"혼자 마시는 차는 맛이 없어서요. 오늘은 공자를 따라다녀 볼래요.\"" } },
        { "cm", new[] {
            "저잣거리에서 공양미를 사러 내려온 청묘와 마주친다. \"막내, 짐 좀 들어 줄래요? 그 대신 오늘은 막내 볼일에 따라가 줄게요.\"",
            "객잔 앞에서 청묘가 기다리고 있다. \"사형들 기일이 가까워서 향을 사러 내려왔어요. 가는 길이 같으면 같이 가요.\"",
            "산사 아래 길목에서 청묘가 바구니를 들고 서 있다. \"오늘은 스님들이 하루 쉬라고 하셨어요. 막내 따라 저자 구경이나 할래요.\"" } },
        { "cham", new[] {
            "처마 위에서 참새가 뛰어내린다. \"형씨, 오늘 어디 가요? 따라가면 소식거리 하나는 생기겠죠. 따라가도 되죠?\"",
            "객잔 일층에서 국밥을 먹던 참새가 숟가락을 놓고 따라 나온다. \"오늘은 소식 팔 데가 없어서 한가해요. 형씨 가는 데 구경이나 할래요.\"",
            "골목을 도는데 참새가 담장 위에서 손짓한다. \"그쪽 길은 오늘 관아 순라가 돌아요. 제가 돌아가는 길을 알려 줄게요, 같이 가요.\"" } },
    };
    static readonly Dictionary<string, string[]> Night = new Dictionary<string, string[]> {
        { "soa",  new[] { "어두워져 나서는데 소아가 등불 하나를 들고 따라 나온다. \"밤길에 외눈 하나라도 보태 주지.\"", "밤길. 뒤에서 발소리가 따라온다. 소아다. \"잠이 안 와서 나왔다. 가는 데까지만 같이 가 주마.\"" } },
        { "amh",  new[] { "밤길 모퉁이에서 암향이 향 한 대를 피워 들고 기다린다. \"밤에는 이 골목이 험해요. 루 사람과 같이 걸으면 아무도 안 건드려요.\"", "암향이 등불 그림자 속에서 걸어 나온다. \"루가 파하고 돌아가는 길이에요. 잠깐 같이 걸어요.\"" } },
        { "cm",   new[] { "저녁 예불을 마치고 내려왔다는 청묘가 객잔 앞에 서 있다. \"막내 얼굴 보고 올라가려고요. 가는 데까지 같이 가요.\"", "청묘가 산사로 올라가는 길이라며 따라 걷는다. 밤이슬에 젖지 말라고 제 삿갓을 당신 머리에 얹는다." } },
        { "cham", new[] { "지붕 위에서 참새가 나란히 따라온다. \"밤에는 지붕이 길이에요. 형씨는 아래로, 저는 위로 같이 가요.\"", "참새가 등불 아래서 오늘 번 동전을 세다가 따라붙는다. \"오늘 밤은 할 일이 없어요. 같이 다녀요.\"" } },
    };
    static readonly Dictionary<string, string[]> Duel = new Dictionary<string, string[]> {
        { "soa",  new[] { "판 둘레 맨 앞줄에 소아가 팔짱을 끼고 선다. 입 모양만으로 말한다. '왼발.' 상대가 먼저 내딛는 발이다.", "소아가 판 기둥에 기대 외눈으로 상대 어깨를 본다. 당신과 눈이 마주치자 턱을 한 번 든다. 오른쪽이 빈다는 뜻이다." } },
        { "amh",  new[] { "판 건너편 찻집 이층 난간에 암향이 기대 있다. 손가락 둘을 접어 보인다. 상대가 어제 술을 두 되 마셨다는 뜻이다.", "구경꾼 틈에서 침향 냄새가 난다. 암향이 상대 쪽을 보고 코를 찡그린다. 손목에 감은 천 밑이 곪았다는 표시다." } },
        { "cm",   new[] { "구경꾼 사이에 승복 하나. 청묘가 합장한 손을 풀지 못하고 서 있다.", "청묘가 맨 뒷줄에서 입술로 청운 구결을 왼다. 셋째 구결, 받는 수다." } },
        { "cham", new[] { "참새가 구경꾼들 사이를 돌며 판돈을 받는다. 당신 쪽에 건 사람이 제일 적다고 귓속말을 하고 간다.", "참새가 판 기둥 꼭대기에 올라앉아 소리친다. \"우리 형씨한테 걸어요! 지금이 제일 싸요!\"" } },
    };
    static readonly Dictionary<string, string[]> Town = new Dictionary<string, string[]> {
        { "soa",  new[] { "소아가 따라나와 대장간 앞에서 멈춘다. 칼날 하나를 들어 등불에 비춰 보고 \"쓰레기\"라고 한다. 대장장이가 웃는다." } },
        { "amh",  new[] { "암향이 등불 그림자에서 나와 곁에 선다. 약재 노점 앞에서 당신 소매를 당긴다. 저 집 당귀는 물에 불린 것이라고." } },
        { "cm",   new[] { "청묘가 산사 공양미를 사러 내려왔다며 곁에 선다. 쌀가게 주인이 승복을 보고 됫박을 꾹꾹 눌러 담는다." } },
        { "cham", new[] { "참새가 소매를 잡아끈다. \"흥정은 제가 해요.\" 상인이 은자 한 냥을 깎아 준다. 깎은 한 냥은 당신 전낭으로 돌아온다." } },
    };

    static string last = "";
    static readonly HashSet<string> usedLines = new HashSet<string>();

    static string PickLine (string[] pool)
    {
        if (pool == null || pool.Length == 0) return "";
        var fresh = new List<string>(); foreach (var s in pool) if (!usedLines.Contains(s)) fresh.Add(s);
        if (fresh.Count == 0) { foreach (var s in pool) usedLines.Remove(s); fresh.AddRange(pool); }
        var p = fresh[Random.Range(0, fresh.Count)]; usedLines.Add(p); return p;
    }

    /// <summary>지금 붙을 수 있는 히로인 하나(없으면 null). exclude = 이 사건의 주인공.</summary>
    static H? Choose (string exclude, float minAff)
    {
        var c = new List<H>(); var w = new List<float>();
        foreach (var h in All)
        {
            bool ok; try { ok = h.Ok(); } catch { ok = false; }
            if (!ok || h.Id == exclude) continue;
            if (G(h.Aff) < minAff) continue;   // 0.4.68 정이 어느 만큼은 쌓여야 따라나선다(갑자기 붙지 않게)
            float wt = 1f + Mathf.Clamp(G(h.Aff), 0f, 8f) * 0.5f;
            if (h.Id == last) wt *= 0.35f;
            c.Add(h); w.Add(wt);
        }
        if (c.Count == 0) return null;
        float tot = 0f; foreach (var x in w) tot += x;
        float r = Random.value * tot;
        for (int i = 0; i < c.Count; i++) { r -= w[i]; if (r <= 0f) return c[i]; }
        return c[c.Count - 1];
    }

    static void Bond (H h)
    {
        last = h.Id;
        MolakStats.SetStr("comp", h.Id);
        float day = G("day");
        if (G("compday_" + h.Id) < day) { MolakStats.Set("compday_" + h.Id, day); MolakStats.Add(h.Aff, 1f); }
    }

    static string HeroOf (string eventId)
    {
        if (string.IsNullOrEmpty(eventId)) return "";
        string ch; if (!MolakBridgeData.EventChar.TryGetValue(eventId, out ch) || ch == null) return "";
        if (ch.StartsWith("soa")) return "soa"; if (ch.StartsWith("amhyang")) return "amh"; if (ch.StartsWith("cheongmyo")) return "cm"; if (ch.StartsWith("chamsae")) return "cham";
        return "";
    }

    /// <summary>이음 문장 끝에 붙일 한 줄(낮·밤 사건). 없으면 "".</summary>
    public static string Join (string kind, string nextEvent)
    {
        MolakStats.SetStr("comp", "none");
        if (kind != "day" && kind != "night") return "";
        // 0.4.85 하루 한 번만 따라붙는다(언메이커 지적: 지붕에서 술 마시던 사람이 같은 날 사건마다 서 있으면 어색하다)
        if (G("compjoinday") >= G("day")) return "";
        if (Random.value > (kind == "day" ? 0.5f : 0.35f)) return "";
        var h = Choose(HeroOf(nextEvent), 3f);
        if (h == null) return "";
        MolakStats.Set("compjoinday", G("day"));
        Bond(h.Value);
        return PickLine((kind == "day" ? Day : Night)[h.Value.Id]);
    }

    /// <summary>비무판·저잣거리에서. compline 에 한 줄(없으면 "none").</summary>
    public static void At (string at)
    {
        MolakStats.SetStr("compline", "none"); MolakStats.Set("compbonus", 0f);
        if (MolakStats.Get("practice") > 0f) return;
        if (Random.value > (at == "duel" ? 0.7f : 0.6f)) return;
        var h = Choose("", 2f);
        if (h == null) return;
        Bond(h.Value);
        var pool = at == "duel" ? Duel : Town;
        MolakStats.SetStr("compline", "<color=#8A7A66>" + PickLine(pool[h.Value.Id]) + "</color>");
        if (at == "duel" && (h.Value.Id == "soa" || h.Value.Id == "amh")) MolakStats.Set("compbonus", 1f);
        if (at == "town" && h.Value.Id == "cham") { MolakStatLog.Why("mn", "참새가 흥정해 한 냥을 깎아 주었다"); MolakStats.Add("mn", 1f); }
    }
}

/// <summary>@companion at:duel|town — 그 자리에 붙는 동행 한 줄을 compline 에.</summary>
[Alias("companion")]
public class CompanionCommand : Command
{
    public StringParameter At;
#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        try { MolakCompanion.At(Assigned(At) ? At.Value : "town"); }
        catch (System.Exception e) { Debug.LogWarning("[companion] " + e.Message); MolakStats.SetStr("compline", "none"); }
    }
}
