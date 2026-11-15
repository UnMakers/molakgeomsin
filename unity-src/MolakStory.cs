using UnityEngine;

/// <summary>
/// 0.4.66 이야기 모드 — 이야기만 따라 읽는 판(언메이커: "스토리 보기 위해 스토리 모드만 나오게").
///  · 제목 화면 「이야기 모드」로 시작한다(g_storypick=1 → Molak01 이 storymode=1). 죽어서 다시 깨어나도 이어진다. 「처음부터」로 시작하면 꺼진다.
///  · 죽지 않는다: 체력은 1 밑으로 떨어지지 않고, 저주는 70에서 멎는다(문양이 익는 끝·저주로 죽는 끝을 막는다).
///  · 밤의 수련·날품 고르기를 건너뛴다(Molak02#Night 첫머리). 방값·굶주림·독도 없다.
///  · 비무는 거의 이긴다(승산 97).
///  · 날이 갈수록 이야기 마디의 문이 열리도록 몸·서열·정을 날짜에 맞춰 채워 준다(아래 Boost). 히로인 마디가 끝나기 전에 마지막 결전이 먼저 열리지 않게 서열은 쉰셋째 날까지 백 위 밖에 둔다.
/// </summary>
public class MolakStory : MonoBehaviour
{
    public static bool On => MolakStats.Get("storymode") > 0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot ()
    {
        var go = new GameObject("MolakStory"); DontDestroyOnLoad(go); go.AddComponent<MolakStory>();
    }

    void Update ()
    {
        if (!Naninovel.Engine.Initialized || !On) return;
        if (MolakStats.Get("day") >= 1f && MolakStats.Get("hp") < 1f) MolakStats.Set("hp", 1f);
        if (MolakStats.Get("curse") > 70f) MolakStats.Set("curse", 70f);
    }

    static void Max (string k, float v) { if (MolakStats.Get(k) < v) MolakStats.Set(k, v); }
    static void RankAtMost (float v) { float r = MolakStats.Get("rank"); if (r <= 0f || r > v) MolakStats.Set("rank", v); }

    /// <summary>하루 마감(daytick)마다 — 그 날짜의 이야기 마디가 요구하는 만큼 채운다.</summary>
    public static void Boost (float day)
    {
        if (!On) return;
        MolakStatLog.Why("*", "이야기 모드 — 이 날짜의 이야기를 볼 수 있게 몸과 서열을 채웠다");
        Max("sw", 4f + day * 0.8f); Max("ng", 2f + day * 0.6f);
        Max("mn", 30f);
        if (day >= 6) RankAtMost(900000f);
        if (day >= 10) Max("arenawin", 3f);
        if (day >= 12) Max("f_janginfo", 1f);
        if (day >= 14) RankAtMost(400000f);
        if (day >= 20) RankAtMost(120000f);
        if (day >= 26) RankAtMost(1800f);
        if (day >= 31) RankAtMost(900f);
        if (day >= 53) RankAtMost(90f);
        if (day >= 56) RankAtMost(9f);
        Max("aff", Mathf.Min(8f, Mathf.Floor(day / 5f)));
        if (day > 30) Max("amhaff", Mathf.Min(6f, Mathf.Floor((day - 30f) / 2f)));
        if (day > 24) Max("cmaff", Mathf.Min(6f, Mathf.Floor((day - 24f) / 3f)));
        if (day > 12) Max("chamaff", Mathf.Min(6f, Mathf.Floor((day - 12f) / 4f)));
        MolakStats.GrowBody();
        MolakStats.Set("hp", MolakStats.Get("hpmax"));
        MolakBridge.Resnap();   // 여운 문장("방금 익힌 것…")이 이 채움을 사건의 결과로 착각하지 않게
    }
}
