using System.Collections.Generic;
using Naninovel;
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

    static readonly Dictionary<string, string[]> Day = new Dictionary<string, string[]> {
        { "soa", new[] {
            "소아가 반 걸음 뒤에서 따라온다. 쌍검 자루에 손을 얹은 채다.",
            "어느새 소아가 옆에 있다. 묻기도 전에 \"심심해서\"라고 한다.",
            "소아가 처마 그늘만 골라 걷는다. 외눈이 지나가는 사람들 손목부터 본다.",
            "소아가 노점에서 곶감 하나를 집어 반으로 쪼개 내민다. 값은 당신이 낸다.",
            "\"오늘은 어디서 칼 맞을 거냐.\" 소아가 따라나서며 묻는다." } },
        { "amh", new[] {
            "골목 모퉁이에서 침향 냄새가 난다. 암향이 소매로 입을 가리고 걸음을 맞춘다.",
            "암향이 양산 그늘 아래서 따라온다. \"오늘은 루에 볼일이 없어서요.\"",
            "암향이 지나가는 사람의 냄새를 하나씩 귓속말로 일러 준다. 술, 쇠, 피, 또 술.",
            "암향이 노점의 비녀를 하나 들었다 내려놓는다. 값을 묻지 않는다." } },
        { "cm", new[] {
            "청묘가 장을 보러 내려왔다며 바구니를 들고 따라온다. 승복 자락이 먼지를 쓴다.",
            "청묘가 걸음을 맞춘다. 사형들 걸음과 같은 박자라 당신 발이 저절로 따라간다.",
            "청묘가 길가의 아이에게 대추를 하나 쥐여 주고 다시 따라온다.",
            "\"막내, 허리 펴요.\" 청묘가 등을 손바닥으로 툭 친다." } },
        { "cham", new[] {
            "참새가 지붕에서 뛰어내려 옆에 붙는다. \"오늘 따라다니면 소식이 공짜예요.\"",
            "참새가 앞서 걸으며 지나가는 집마다 소식을 하나씩 붙인다.",
            "참새가 남의 처마 밑 곶감을 하나 슬쩍하더니 반을 당신 입에 넣는다.",
            "참새가 오늘 들은 소식을 손가락으로 세다가 아홉에서 틀린다." } },
    };
    static readonly Dictionary<string, string[]> Night = new Dictionary<string, string[]> {
        { "soa",  new[] { "소아가 등불 하나를 들고 앞서 걷는다. 외눈 쪽으로 불을 든다.", "밤길. 소아의 발소리가 당신 발소리에 맞춰 반 박자 늦게 따라온다." } },
        { "amh",  new[] { "암향이 밤길에 향 한 대를 피워 든다. 연기가 길잡이처럼 앞서간다.", "암향이 등불 그림자 쪽으로만 걷는다. 루 사람의 버릇이다." } },
        { "cm",   new[] { "청묘가 저녁 예불을 마치고 내려왔다며 염주를 굴리며 따라온다.", "청묘가 밤이슬에 젖지 말라고 제 삿갓을 당신 머리에 얹는다." } },
        { "cham", new[] { "참새가 지붕 위로 나란히 따라온다. 기와 밟는 소리가 고양이보다 가볍다.", "참새가 등불 아래서 오늘 번 동전을 세다가 하나를 당신 손에 쥐여 준다. \"수수료.\"" } },
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
    static H? Choose (string exclude)
    {
        var c = new List<H>(); var w = new List<float>();
        foreach (var h in All)
        {
            bool ok; try { ok = h.Ok(); } catch { ok = false; }
            if (!ok || h.Id == exclude) continue;
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
        if (Random.value > (kind == "day" ? 0.55f : 0.4f)) return "";
        var h = Choose(HeroOf(nextEvent));
        if (h == null) return "";
        Bond(h.Value);
        return PickLine((kind == "day" ? Day : Night)[h.Value.Id]);
    }

    /// <summary>비무판·저잣거리에서. compline 에 한 줄(없으면 "none").</summary>
    public static void At (string at)
    {
        MolakStats.SetStr("compline", "none"); MolakStats.Set("compbonus", 0f);
        if (MolakStats.Get("practice") > 0f) return;
        if (Random.value > (at == "duel" ? 0.7f : 0.6f)) return;
        var h = Choose("");
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
