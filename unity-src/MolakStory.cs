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
    /// <summary>0.4.94 인연별 이야기 모드: 1 소아 편 · 2 암향 편 · 3 청묘 편 · 4 참새 편(g_storyroute, 장 고르기에서 고른다). 0 이면 예전처럼 직접 고른다.</summary>
    public static int Route => On ? Mathf.Clamp(Mathf.RoundToInt(MolakStats.Get("g_storyroute")), 0, 4) : 0;
    /// <summary>선택지·비무·승부수를 알아서 넘기는가(MolakStoryAuto).</summary>
    public static bool Auto => Route > 0;

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
        // 0.4.94 편마다 다른 히로인의 마디는 닫는다(Molak02 BeatCheck 의 nx_*). 대본의 storyauto 는 비무를 한 장면으로 줄인다(MolakDuel#StoryCut).
        int r = Route;
        Flag("storyauto", r > 0);
        Flag("nx_amh", r > 0 && r != 2);
        Flag("nx_cm", r > 0 && r != 3);
        Flag("nx_cham", r > 0 && r != 4);
    }
    static void Flag (string k, bool on) { float v = on ? 1f : 0f; if (MolakStats.Get(k) != v) MolakStats.Set(k, v); }

    /// <summary>0.4.73 이야기 모드에서 뽑는 사건 — 소아와 두 사내(염혁·단목결), 하진무, 쥐눈이 장, 표국, 맹의 줄기만. 나머지 저잣거리 일은 건너뛴다.</summary>
    static readonly string[] Keep = { "E_Soa", "E_Spar", "E_Hyeok", "E_Gyeol", "E_Hajin", "E_Hostage", "E_Jang", "E_Hao", "E_Guard", "E_Maeng", "E_Rain2", "E_OldDream" };   // 0.4.94 노인의 꿈(사태의 함 뚜껑 — 종장의 노인 대목이 묻는다)
    /// <summary>0.4.94 편마다 더 읽는 줄기 — 그 히로인의 사건과, 그 히로인의 진짜 끝을 여는 사람들의 사건(암향 편은 백초 할멈의 재우는 약).</summary>
    static readonly string[][] RouteKeep = {
        new string[0],
        new string[0],
        new[] { "E_Amh", "E_SoaAmh", "E_JangAmh", "E_YongAmh", "E_HajinAmh", "E_Herb" },
        new[] { "E_Cm", "E_SoaCm", "E_HerbCm", "E_JinSister", "E_TempleBell" },
        new[] { "E_Cham" },
    };
    /// <summary>편에 속하지 않는 히로인의 사건(두 히로인이 함께 나오는 것 포함)은 뺀다.</summary>
    static readonly string[][] Heroine = { new[] { "E_Amh", "E_SoaAmh", "E_JangAmh", "E_YongAmh", "E_HajinAmh" }, new[] { "E_Cm", "E_SoaCm", "E_HerbCm", "E_JinSister", "E_TempleBell" }, new[] { "E_Cham" } };
    public static bool EventAllowed (string id)
    {
        int r = Route;
        if (r > 0)
        {
            for (int h = 0; h < 3; h++) if (h + 2 != r) foreach (var k in Heroine[h]) if (id.StartsWith(k)) return false;
            foreach (var k in RouteKeep[r]) if (id.StartsWith(k)) return true;
        }
        foreach (var k in Keep) if (id.StartsWith(k)) return true;
        return false;
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
        // 0.4.94 인연별 이야기 모드: 문양은 적어도 뿌리만으로 자란 만큼은 익는다(68에서 멎는다) — 사술이 익어야 오는 대목(할멈의 금침·재우는 약, 문양의 속삭임)이
        //   보호하는 선택에 밀려 오지 않던 것(암향 편 시험). 사술은 이 이야기의 시계다.
        if (Auto)
        {
            float c = 0f; for (int d = 1; d <= Mathf.RoundToInt(day); d++) c += MolakRules.CurseRoot(d);
            Max("curse", Mathf.Min(68f, c));
        }
        MolakRealm.StoryFill(day);   // 0.4.72 경지도 날짜에 맞춰(서열이 벽에 막혀 이야기가 멈추지 않게)
        MolakStats.GrowBody();
        MolakStats.Set("hp", MolakStats.Get("hpmax"));
        MolakBridge.Resnap();   // 여운 문장("방금 익힌 것…")이 이 채움을 사건의 결과로 착각하지 않게
    }
}
