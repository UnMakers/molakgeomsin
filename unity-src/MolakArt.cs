using System.Collections.Generic;
using Naninovel;
using UnityEngine;

/// <summary>
/// 삽화 시스템 — 종이 카메라(레이어 30) 위에 장면 삽화와 인물 입상을 먹(곱셈 블렌드)으로 얹는다.
/// 상단 띠(캔버스 900×1600 기준 y 184~484). 인물이 나오면 장면을 옅게 깔고 그 위에 인물(0.4.9). 장면이 하루의 기본 풍경이면 인물이 있을 법한 자리로 바꾼다.
/// 마지막에 부른 쪽이 이긴다 — 새 장면(@art scene:)은 서 있던 인물을 내리고, 새 인물(@art char:)은 장면을 가린다(장면은 기억해 둔다).
/// 하루가 시작될 때(DayLoop) 인물은 내린다.
/// </summary>
public class MolakArt : MonoBehaviour
{
    public static bool Collapsed;
    public static bool BandActive { get { return inst != null && !Collapsed && (inst.sceneCur != null || inst.charCur != null); } }
    /// <summary>0.4.47: 띠에 그림이 있나(선택지 때문에 접혀 있어도). 본문 한 쪽의 높이를 어림할 때 쓴다.</summary>
    /// <summary>0.4.49 비무 판인가(장면이 duel*).</summary>
    public static bool InDuel { get { return inst != null && inst.sceneId != null && inst.sceneId.StartsWith("duel"); } }
    /// <summary>지금 서 있는 인물 그림(비무의 잔상용).</summary>
    /// <summary>0.4.82 지금 서 있는 인물의 id(이름 인장용). 없으면 빈 문자열.</summary>
    public static string CharId { get { return inst != null && inst.charCur != null ? inst.charId : ""; } }
    public static Texture2D CharTexture { get { return inst != null && inst.charCur != null ? inst.charCur.texture : null; } }
    static int pose; float poseK;
    /// <summary>0.4.49 비무 기세: 1 덤벼든다(한 걸음 다가옴) 2 물러서 받는다 3 흔든다 0 제자리.</summary>
    public static void Pose (int p) { pose = p; }
    // 0.4.85 몸짓(언메이커: 상황에 따라 표정이나 몸짓도 드라마틱하게). 서술 한 줄의 낱말로 고르거나(AutoGesture) 대본에서 @gesture kind: 로 부른다.
    //  lunge 다가서기 · recoil 움찔 · tremble 떨림 · sink 무너짐(다음 인물까지 유지) · bounce 웃음 · nod 끄덕 · turn 돌아섬
    static string gest = ""; static float gestT = 9f; static bool sunk;
    /// <summary>0.4.85 몸짓을 끈다(언메이커: 그림을 밀고 당기기보다 상황에 맞는 그림을 새로 뽑자). 코드는 남겨 둔다.</summary>
    public static readonly bool GestureOn = false;
    public static void Gesture (string kind)
    {
        if (!GestureOn) return;
        if (inst == null || inst.charR == null || inst.charR.sprite == null) return;
        gest = kind ?? ""; gestT = 0f;
        if (gest == "sink") sunk = true; else if (gest == "lunge" || gest == "bounce" || gest == "nod") sunk = false;
    }
    static readonly string[][] GestureWords = {
        new[]{ "lunge", "버럭", "소리친다", "소리를 지른다", "고함", "멱살", "노려본다", "쏘아본다", "다가선다", "바짝 다가", "칼을 뽑는다", "검을 뽑는다", "탁자를 친다", "내리친다" },
        new[]{ "recoil", "흠칫", "움찔", "멈칫", "뒷걸음", "숨을 삼킨다", "얼굴이 하얗", "눈이 커진다", "굳는다" },
        new[]{ "tremble", "어깨가 떨", "손이 떨", "흐느", "울음을", "소리 내어 운다", "눈물", "떨리는 목소리", "목이 멘" },
        new[]{ "sink", "고개를 떨군", "고개를 숙인", "무릎을 꿇", "주저앉", "털썩", "무너진다", "어깨가 처" },
        new[]{ "bounce", "웃음을 터", "껄껄", "낄낄", "깔깔", "소리 내어 웃", "크게 웃" },
        new[]{ "nod", "고개를 끄덕" },
        new[]{ "turn", "등을 돌린다", "돌아선다", "고개를 돌린다" },
    };
    /// <summary>서술 한 줄(대사·독백 아님)에 몸짓 낱말이 있으면 서 있는 인물이 그렇게 움직인다. 주인공(당신)이 주어인 줄은 건너뛴다.</summary>
    public static void AutoGesture (string plain)
    {
        if (!GestureOn || inst == null || inst.charR == null || inst.charR.sprite == null || string.IsNullOrEmpty(plain)) return;
        string t = plain.Trim();
        if (t.StartsWith("'") || t.StartsWith("\"") || t.StartsWith("당신")) return;
        foreach (var row in GestureWords)
            for (int i = 1; i < row.Length; i++)
            {
                int at = t.IndexOf(row[i]);
                if (at < 0) continue;
                int me = t.IndexOf("당신이"); if (me < 0) me = t.IndexOf("당신은");
                if (me >= 0 && me < at) return;   // 당신이 움찔한다 — 인물이 아니라 나
                Gesture(row[0]); return;
            }
    }
    public static bool HasArt { get { return inst != null && (inst.sceneCur != null || inst.charCur != null); } }
    public const float BandTop = 184f, BandH = 300f;   // 캔버스 단위(900×1600)

    static MolakArt inst;
    static Material mat, matAlpha, matScene, matCine;
    static readonly Dictionary<string, string> SceneAlias = new Dictionary<string, string>
    {
        {"day","morning"},{"morning","morning"},{"night","night"},{"shrine","shrine"},{"blood","blood"},
        {"valley","valley"},{"rain","rain"},{"market","market"},{"crossroad","crossroad"},{"roofmoon","roofmoon"},{"roof","roofmoon"},{"duel","duel"},
    };

    SpriteRenderer sceneR, charR, shadeR; Sprite shadeV, shadeH;
    SpriteMask mask;
    Sprite sceneCur, charCur;
    float sceneA, charA, sceneT = 1f, charT = 1f, colT = 1f, sceneVis = 1f;
    string sceneId = "", charId = "";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot ()
    {
        if (inst) return;
        var go = new GameObject("MolakArt");
        DontDestroyOnLoad(go);
        inst = go.AddComponent<MolakArt>();
    }

    // 0.4.84 마디 대표 그림: @chapter 가 뜨는 동안 그 줄기(sub)의 그림을 그림 창 맨 위에 깔았다가, 마디 이름이 걷히면 함께 스러진다.
    static readonly Dictionary<string, string> KeyArt = new Dictionary<string, string>
    {
        { "회귀", "regress" }, { "소아", "soa" }, { "복수", "revenge" }, { "청묘", "cheongmyo" }, { "노인", "oldman" },
        { "그물", "net" }, { "참새", "sparrow" }, { "종장", "finale" }, { "종장 · 닷새", "finale" }, { "암향", "amhyang" }, { "암향루", "amhyang" },
        { "사당", "shrine" }, { "객잔", "inn" }, { "운락객잔", "inn" }, { "쥐눈이 장", "jang" }
    };
    // 좁은 화면에서 잘릴 때 어디를 가운데 둘지(그림 너비 비율)
    static readonly Dictionary<string, float> KeyFocus = new Dictionary<string, float>
    {
        { "regress", 0.62f }, { "soa", 0.70f }, { "revenge", 0.50f }, { "cheongmyo", 0.50f }, { "oldman", 0.40f }, { "net", 0.50f },
        { "sparrow", 0.55f }, { "finale", 0.48f }, { "amhyang", 0.42f }, { "shrine", 0.62f }, { "inn", 0.50f }, { "jang", 0.55f }
    };
    // 위아래로 잘릴 때 어디를 가운데 둘지(그림 높이 비율, 위가 0)
    static readonly Dictionary<string, float> KeyFocusY = new Dictionary<string, float>
    {
        { "regress", 0.45f }, { "soa", 0.55f }, { "revenge", 0.55f }, { "cheongmyo", 0.62f }, { "oldman", 0.62f }, { "net", 0.55f },
        { "sparrow", 0.62f }, { "finale", 0.62f }, { "amhyang", 0.58f }, { "shrine", 0.55f }, { "inn", 0.58f }, { "jang", 0.62f }
    };
    // 화면이 그림보다 훨씬 넓을 때(가로 PC) 양옆을 메울 종이빛(그림 왼쪽·오른쪽 끝의 빛)
    static readonly Dictionary<string, Color32[]> KeyEdge = new Dictionary<string, Color32[]>
    {
        { "regress", new Color32[]{ new Color32(251,247,228,255), new Color32(149,136,116,255) } }, { "soa", new Color32[]{ new Color32(249,243,226,255), new Color32(248,242,226,255) } },
        { "revenge", new Color32[]{ new Color32(236,229,209,255), new Color32(234,228,208,255) } }, { "cheongmyo", new Color32[]{ new Color32(248,243,221,255), new Color32(249,241,215,255) } },
        { "oldman", new Color32[]{ new Color32(254,252,232,255), new Color32(254,252,233,255) } }, { "net", new Color32[]{ new Color32(250,245,225,255), new Color32(251,247,228,255) } },
        { "sparrow", new Color32[]{ new Color32(238,224,201,255), new Color32(246,242,225,255) } }, { "finale", new Color32[]{ new Color32(252,250,230,255), new Color32(252,250,229,255) } },
        { "amhyang", new Color32[]{ new Color32(251,248,234,255), new Color32(250,248,232,255) } }, { "shrine", new Color32[]{ new Color32(253,251,228,255), new Color32(253,251,229,255) } },
        { "inn", new Color32[]{ new Color32(253,251,219,255), new Color32(252,243,212,255) } }, { "jang", new Color32[]{ new Color32(253,249,214,255), new Color32(253,250,216,255) } }
    };
    SpriteRenderer keyR, keyBgR; Sprite keyCur; float keyT = 1f, keyIn, keyFx = 0.5f, keyFy = 0.5f; Texture2D keyBgTex;
    public static void Key (string sub)
    {
        if (inst == null) return;
        string id; Sprite sp = null;
        if (!string.IsNullOrEmpty(sub) && KeyArt.TryGetValue(sub.Trim(), out id)) sp = Load("MolakArt/key_" + id);
        inst.keyCur = sp; inst.keyT = 0f; inst.keyIn = 0f;
        string kid = KeyFor(sub); float fx, fy;
        inst.keyFx = sp != null && KeyFocus.TryGetValue(kid, out fx) ? fx : 0.5f;
        inst.keyFy = sp != null && KeyFocusY.TryGetValue(kid, out fy) ? fy : 0.5f;
        Color32[] edge;
        if (sp != null && inst.keyBgTex != null && KeyEdge.TryGetValue(kid, out edge)) { inst.keyBgTex.SetPixels32(new Color32[]{ edge[0], edge[1] }); inst.keyBgTex.Apply(); }
    }
    // 0.4.85 장면 전환 암전(언메이커: 대화로 장면이 끝나며 바로 다음 장면이라 갑작스럽다): @page 로 장면이 바뀌면 그림 창이 먹으로 잠깐 덮였다가 새 그림이 떠오른다.
    SpriteRenderer dipR; float dipT = 9f;
    public static void Dip () { if (inst != null) inst.dipT = 0f; }
    public static bool Dipping { get { return inst != null && inst.dipT < 0.5f; } }
    public static string KeyFor (string sub) { string id; return !string.IsNullOrEmpty(sub) && KeyArt.TryGetValue(sub.Trim(), out id) ? id : ""; }

    public static void Scene (string id)
    {
        if (inst == null) return;
        string key;
        if (string.IsNullOrEmpty(id) || id == "none" || id == "off") { inst.SetScene(null, ""); return; }
        if (!SceneAlias.TryGetValue(id, out key)) key = id;
        if (key == "night") IsNight = true; else if (key == "morning") IsNight = false;
        // 0.4.9: 날씨는 장면을 따라간다(비·눈·불티·핏빛 비). 비무 장면은 그대로 두고, 다른 장면으로 넘어가면 갠다.
        if (!key.StartsWith("duel"))
        {
            string w;
            MolakVfx.Weather(SceneWeather.TryGetValue(key, out w) ? w : "");
        }
        if (inst.sceneId == key) { if (inst.charCur != null) inst.SetChar(null, ""); return; }   // 같은 장면을 다시 부르면 인물을 내리고 장면을 보인다
        bool isDuel = key.StartsWith("duel");   // duel, duelbridge … 비무 판의 변형
        if (isDuel) { if (!inst.sceneId.StartsWith("duel")) { prevScene = inst.sceneId; MolakSfx.Draw(); MolakSfx.Crowd(); } } else prevScene = null;
        if (!isDuel) MolakFlow.Break();
        var sp = Load("MolakArt/scene_" + key);
        inst.SetScene(sp, key);
        MolakBgm.Mood(MoodFor(key));
        MolakAmbience.Scene(key);
    }

    // 0.4.70 장면에 맞는 배경음 묶음
    static readonly Dictionary<string, string> SceneMood = new Dictionary<string, string>
    {
        {"raid","fire"},{"bloodroof","fire"},{"raidnight","fire"},{"manorfire","fire"},{"herbfire","fire"},
        {"rain","rain"},{"flood","rain"},{"soanight","rain"},
        {"night","night"},{"roofmoon","night"},{"nightmarket","night"},{"stairs","night"},{"well","night"},
        {"lordhall","boss"},
        {"camp","tension"},{"pass","tension"},{"valley","tension"},{"cellar","tension"},{"blood","tension"},{"ruins","tension"},{"storehouse","tension"},
        {"mourners","sorrow"},{"shrineash","sorrow"},{"stonebox","sorrow"},
        {"yard","prologue"},{"gate","prologue"},
        {"truemorning","true"},{"hapbyeok2","true"},
    };
    static string MoodFor (string key)
    {
        if (string.IsNullOrEmpty(key)) return IsNight ? "night" : "day";
        if (key.StartsWith("duel")) return BossDuel ? "boss" : "duel";
        string m;
        if (SceneMood.TryGetValue(key, out m))
        {
            if (m == "prologue" && MolakStats.Get("day") >= 1f) return "day";   // 청운산장 앞마당은 서장에서만
            return m;
        }
        return IsNight ? "night" : "day";
    }
    /// <summary>교주전·결전 비무면 boss 묶음(MolakDuel 이 켠다).</summary>
    public static bool BossDuel;
    public static string CurrentMood ()
    {
        if (inst == null) return "day";
        string c = inst.charId ?? "";
        if (c.StartsWith("soa")) return "soa";
        if (c.StartsWith("amhyang") || c.StartsWith("cheongmyo") || c.StartsWith("chamsae")) return "heroine";
        return MoodFor(inst.sceneId);
    }

    // 0.4.9: 인물이 나오면 그 사람이 있을 법한 자리를 뒤에 깐다(지금 장면이 하루의 기본 풍경일 때만 — 대본이 고른 장면은 그대로 둔다)
    static readonly HashSet<string> Generic = new HashSet<string> { "", "morning", "night", "rain" };
    static readonly Dictionary<string, string> HomeDay = new Dictionary<string, string>
    {
        {"granny","herbshop"},{"chamsae","innhall"},{"chamsae_serious","rain"},{"hajinmu","innhall"},{"gwaksan","market"},{"yonggae","crossroad"},{"amhyang","teahouse"},
        {"soa","innhall"},{"soa_smile","market"},{"soa_grief","rain"},{"soa_fierce","alley"},{"hochi","alley"},{"kyona","valley"},
        {"jinbaek","gate"},{"lawlord","gate"},{"oldman","night"},{"ochi","valley"},{"cheongmyo","temple"},
        {"cheongmyo_robe","temple"},{"amhyang_pain","teahouse"},{"amhyang_soft","ferry"},{"jang","alley"},
        {"hajinmu_grave","innhall"},{"granny_grave","herbshop"},{"jinbaek_sorrow","gate"},{"mugo","smithy"},{"jeomsoi","innhall"},{"panju","alley"},{"storyteller","market"},{"porter","ferry"},{"yeomga","yard"},{"taxman","market"},{"abbot","temple"},{"beggarmonk","crossroad"},{"ferryman","ferry"},{"inspector","gate"},{"cultist","market"},{"odam","village"},{"wangsam","pawn"},{"machung","yard"},{"baekri","teahouse"},{"scout","alley"},{"hyeok","yard"},{"gyeol","alley"},
        {"hyeok_grim","yard"},{"gyeol_angry","alley"},{"gwaksan_grief","market"},{"lord_wrath","lordhall"},
    };
    static readonly Dictionary<string, string> HomeNight = new Dictionary<string, string>
    {
        {"granny","herbshop"},{"chamsae","roofmoon"},{"chamsae_serious","rain"},{"hajinmu","innhall"},{"gwaksan","alley"},{"yonggae","alley"},{"amhyang","teahouse"},
        {"soa","roofmoon"},{"soa_smile","roofmoon"},{"soa_grief","rain"},{"soa_fierce","alley"},{"hochi","alley"},
        {"jinbaek","alley"},{"lawlord","gate"},{"oldman","night"},{"jang","alley"},
        {"amhyang_pain","teahouse"},{"amhyang_soft","ferry"},
        {"hajinmu_grave","innhall"},{"granny_grave","herbshop"},{"jinbaek_sorrow","alley"},{"mugo","smithy"},{"jeomsoi","innhall"},{"panju","nightmarket"},{"storyteller","nightmarket"},{"porter","ferry"},{"yeomga","yard"},{"taxman","alley"},{"abbot","temple"},{"beggarmonk","alley"},{"ferryman","ferry"},{"inspector","gate"},{"cultist","night"},{"odam","village"},{"wangsam","pawn"},{"machung","yard"},{"baekri","roofmoon"},{"scout","night"},{"hyeok","alley"},{"gyeol","roofmoon"},
        {"hyeok_grim","alley"},{"gyeol_angry","roofmoon"},{"gwaksan_grief","alley"},{"lord_wrath","lordhall"},
    };
    public static bool IsNight;
    static readonly Dictionary<string, string> SceneWeather = new Dictionary<string, string>
    {
        {"rain","rain"},{"flood","rain"},{"snow","snow"},{"manorfire","inferno"},{"herbfire","embers"},{"bloodroof","bloodrain"},{"raid","bloodrain"},{"raidnight","embers"},{"soanight","rain"},
    };

    public static void Char (string id)
    {
        if (inst == null) return;
        if (string.IsNullOrEmpty(id) || id == "none" || id == "off") { inst.SetChar(null, ""); if (!inst.sceneId.StartsWith("duel")) MolakBgm.Mood(CurrentMood()); return; }
        if (inst.charId == id) return;
        if (Generic.Contains(inst.sceneId) && !inst.sceneId.StartsWith("duel"))
        {
            string home;
            var homes = IsNight ? HomeNight : HomeDay;
            if ((homes.TryGetValue(id, out home) || homes.TryGetValue(Person(id), out home)) && home != inst.sceneId)
            {
                var hs = Load("MolakArt/scene_" + home);
                if (hs != null) { inst.sceneCur = hs; inst.sceneId = home; inst.sceneT = 0f; ChangedAt = Time.unscaledTime; }
            }
            else if (inst.sceneCur == null)
            {
                var fs = Load("MolakArt/scene_" + (IsNight ? "night" : "morning"));
                if (fs != null) { inst.sceneCur = fs; inst.sceneId = IsNight ? "night" : "morning"; inst.sceneT = 0f; ChangedAt = Time.unscaledTime; }
            }
        }
        var sp = Load("MolakArt/char_" + id);
        inst.SetChar(sp, id);
        if (!inst.sceneId.StartsWith("duel")) MolakBgm.Mood(CurrentMood());   // 0.4.70 소아·히로인이 나오면 그 사람의 곡
    }

    static string prevScene;
    /// <summary>결투 장면 종료 후 직전 장면으로 복귀</summary>
    public static void EndDuel ()
    {
        if (inst == null || !inst.sceneId.StartsWith("duel")) return;
        MolakDuelStage.Clear();
        if (string.IsNullOrEmpty(prevScene)) inst.SetScene(null, ""); else { var sp = Load("MolakArt/scene_" + prevScene); inst.SetScene(sp, prevScene); }
        BossDuel = false;
        MolakBgm.Mood(CurrentMood());
        MolakAmbience.Scene(inst.sceneId);
        prevScene = null;
    }

    /// <summary>0.4.41: 그림이 바뀌는 중인가(새 장면·인물이 아직 다 떠오르지 않았다). @art 가 이게 끝나길 잠깐 기다린 뒤 글로 넘어간다.</summary>
    public static bool Settling { get { return inst != null && (inst.sceneT < 0.7f || inst.charT < 0.7f); } }
    // 0.4.45: 옅게(35%)에서 0.22초 → 60%에서 0.15초. 새 그림이 바뀌는 순간부터 거의 다 보인다.
    const float FadeIn = 0.15f, FadeFrom = 0.6f;

    /// <summary>0.4.42: 장면 없이 시작하는 사건(우연 사건의 절반 남짓)은 앞 사건 그림이 그대로 남아 있었다 — 하루의 바탕(낮/밤) 그림으로 되돌린다.</summary>
    /// <summary>0.4.44: 하루의 바탕 그림(낮/밤)으로. 이미 바탕이고 인물도 없으면 아무것도 안 한다(깜박임 방지). 비무 중엔 건드리지 않는다.</summary>
    public static void ResetToBase ()
    {
        if (inst == null || (inst.sceneId != null && inst.sceneId.StartsWith("duel"))) return;
        string b = IsNight ? "night" : "morning";
        if (inst.sceneId == b && inst.charCur == null) return;
        Scene(b);
    }

    public static void ResetForEvent (string eventId)
    {
        if (inst == null || string.IsNullOrEmpty(eventId) || eventId == "none") return;
        string sc;
        if (MolakBridgeData.EventScene.TryGetValue(eventId, out sc) && !string.IsNullOrEmpty(sc)) return;   // 사건이 제 장면을 깐다
        Scene(IsNight ? "night" : "morning");
    }

    public static void Clear () { if (inst == null) return; inst.SetScene(null, ""); inst.SetChar(null, ""); }

    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    // ---- 0.4.45 미리 읽기 ----
    // 그림을 처음 부르는 순간 Resources.Load 로 그 자리에서 읽으면, 기기에서는 그 프레임이 길게 멈춘 뒤에야 그림이 뜬다
    // (장면이 바뀔 때 그림이 한 박자 늦게 뜨던 것). 게임이 켜지면 MolakArt/_index(빌드 때 MolakBuild 가 만든다)에 적힌
    // 그림을 한 장씩 비동기로 읽어 둔다. 하루의 바탕(낮·밤)부터. 목록에 없는 그림은 예전처럼 부를 때 읽는다.
    bool preloadStarted;
    System.Collections.IEnumerator Preload ()
    {
        var idx = Resources.Load<TextAsset>("MolakArt/_index");
        if (idx == null) yield break;
        var names = new List<string> { "scene_morning", "scene_night" };
        foreach (var raw in idx.text.Split('\n')) { var n = raw.Trim(); if (n.Length > 0 && !names.Contains(n)) names.Add(n); }
        foreach (var n in names)
        {
            string path = "MolakArt/" + n;
            Sprite have;
            if ((cache.TryGetValue(path, out have) && have != null) || missing.Contains(path)) continue;
            var rq = Resources.LoadAsync<Texture2D>(path);
            yield return rq;
            var tex = rq.asset as Texture2D;
            if (tex == null) continue;
            if (cache.TryGetValue(path, out have) && have != null) continue;   // 기다리는 사이 대본이 먼저 불렀다
            cache[path] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
    static readonly HashSet<string> missing = new HashSet<string>();
    static Sprite Load (string path)
    {
        Sprite s;
        if (cache.TryGetValue(path, out s) && s != null) return s;   // 에디터에서 플레이를 다시 켜면 만들어 둔 스프라이트가 파괴돼 있다 — 다시 만든다
        if (missing.Contains(path)) return null;                       // 그림이 아직 없는 인물·장면: 한 번만 알리고 조용히 넘어간다
        var tex = Resources.Load<Texture2D>(path);
        if (tex == null) { Debug.LogWarning("[MolakArt] missing " + path); missing.Add(path); return null; }
        s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        cache[path] = s;
        return s;
    }

    // 0.4.85 장면이 바뀌면 앞 인물은 그 자리에서 내린다(언메이커: 불탄 장면 뒤 눈을 뜰 때 노인이 살짝 비쳤다 사라지는 잔상) — 옛 인물이 새 장면 위로 스러지지 않게
    void SetScene (Sprite s, string id)
    {
        sceneCur = s; sceneId = id; sceneT = 0f; ChangedAt = Time.unscaledTime;
        if (s != null && (charCur != null || (charR != null && charR.sprite != null))) { SetChar(null, ""); if (charR != null) charR.sprite = null; charT = 1f; }
    }
    /// <summary>0.4.84 새 사람이 들어오면 옆에서 미끄러져 들어온다(언메이커: 처음 등장할 때 옆에서 나오는 게 좋다). 같은 사람의 표정만 바뀌면(soa → soa_smile) 제자리에서 바뀐다.</summary>
    static string Person (string id) { if (string.IsNullOrEmpty(id)) return ""; int u = id.IndexOf('_'); return u > 0 ? id.Substring(0, u) : id; }
    void SetChar (Sprite s, string id)
    {
        bool newPerson = s != null && Person(id) != Person(charCur != null ? charId : null);
        if (newPerson) { pushT = 0f; slideT = 0f; gest = ""; gestT = 9f; sunk = false; }
        charCur = s; charId = id; charT = 0f; ChangedAt = Time.unscaledTime;
        // 0.4.85 같은 사람의 표정이 바뀌면 몸도 같이 움직인다
        if (s != null && !newPerson && !string.IsNullOrEmpty(id))
        {
            string e = id.Substring(Person(id).Length);
            string g = e.Contains("wrath") || e.Contains("angry") || e.Contains("fierce") || e.Contains("scold") ? "lunge"
                     : e.Contains("tears") ? "tremble" : e.Contains("grief") || e.Contains("sorrow") || e.Contains("grave") || e.Contains("pain") ? "sink"
                     : e.Contains("laugh") || e.Contains("smile") || e.Contains("kind") ? "bounce" : e.Contains("uneasy") || e.Contains("fear") ? "recoil" : "";
            if (!GestureOn) g = "";
            if (g.Length > 0) { gest = g; gestT = 0f; sunk = g == "sink"; }
            else if (e.Length == 0) sunk = false;   // 평소 얼굴로 돌아오면 다시 선다
        }
    }
    float slideT = 1f;
    /// <summary>0.4.65 그림이 마지막으로 바뀐 때(빨리에서 글을 그림 뒤로 반 박자 늦추는 데 쓴다).</summary>
    public static float ChangedAt = -9f;
    /// <summary>0.4.65 그림이 아직 다 떠오르지 않았다(장면·인물 페이드, 인물 뒤 풍경 눌림까지).</summary>
    public static bool Busy { get { return inst != null && (inst.sceneT < 1f || inst.charT < 1f); } }
    float pushT = 1f, hurtT = -1f, quietK;
    /// <summary>인물이 맞았다: 반 박자 옆으로 흔들린다(장면만 있으면 장면이).</summary>
    public static void Hurt () { if (inst != null) inst.hurtT = 0f; }

    void Ensure ()
    {
        if (sceneR != null) return;
        if (mat == null)
        {
            var resMat = Resources.Load<Material>("MolakArt/SpriteMultiply");
            var sh = Shader.Find("Molak/SpriteMultiply");
            mat = resMat != null ? new Material(resMat) : (sh != null ? new Material(sh) : new Material(Shader.Find("Sprites/Default")));
            var nightMat = Resources.Load<Material>("MolakArt/SpriteInkNight");
            var nsh = Shader.Find("Molak/SpriteInkNight");
            matAlpha = nightMat != null ? new Material(nightMat) : (nsh != null ? new Material(nsh) : new Material(Shader.Find("Sprites/Default")));
            matScene = new Material(Shader.Find("Sprites/Default"));
            var cineMat = Resources.Load<Material>("MolakArt/SpriteCine");   // 0.4.82 그림 화면 인물(옅은 네모 자국 지움)
            matCine = cineMat != null ? new Material(cineMat) : matScene;
            if (matCine != matScene) matCine.SetFloat("_BlurPx", 0f);   // 흐림은 뺐다(언메이커: 어색하다) — 어둡게만
        }
        var band = new GameObject("MolakArtBand"); band.transform.SetParent(transform, false); band.layer = 30;
        var mg = new GameObject("Mask"); mg.transform.SetParent(band.transform, false); mg.layer = 30;
        mask = mg.AddComponent<SpriteMask>();
        mask.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        mask.isCustomRangeActive = false;

        var sg = new GameObject("Scene"); sg.transform.SetParent(band.transform, false); sg.layer = 30;
        sceneR = sg.AddComponent<SpriteRenderer>(); sceneR.sharedMaterial = matScene; sceneR.sortingOrder = 10;
        sceneR.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

        var cg = new GameObject("Char"); cg.transform.SetParent(band.transform, false); cg.layer = 30;
        charR = cg.AddComponent<SpriteRenderer>(); charR.sharedMaterial = mat; charR.sortingOrder = 11;
        charR.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

        // 0.4.82 그림 화면: 위아래로 먹이 스미는 막(위는 수치 줄, 아래는 선택지·글자리 자리)
        var shg = new GameObject("Shade"); shg.transform.SetParent(band.transform, false); shg.layer = 30;
        shadeR = shg.AddComponent<SpriteRenderer>(); shadeR.sharedMaterial = matScene; shadeR.sortingOrder = 12;
        shadeV = ShadeSprite(); shadeH = SideShadeSprite();
        shadeR.sprite = shadeV; shadeR.enabled = false;
        var kg = new GameObject("Key"); kg.transform.SetParent(band.transform, false); kg.layer = 30;
        keyR = kg.AddComponent<SpriteRenderer>(); keyR.sharedMaterial = matScene; keyR.sortingOrder = 13;
        keyR.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        var dg = new GameObject("Dip"); dg.transform.SetParent(band.transform, false); dg.layer = 30;
        dipR = dg.AddComponent<SpriteRenderer>(); dipR.sharedMaterial = matScene; dipR.sortingOrder = 15;
        dipR.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        dipR.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f); dipR.enabled = false;
        var kbg = new GameObject("KeyBg"); kbg.transform.SetParent(band.transform, false); kbg.layer = 30;
        keyBgR = kbg.AddComponent<SpriteRenderer>(); keyBgR.sharedMaterial = matScene; keyBgR.sortingOrder = 13;
        keyBgR.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        keyBgTex = new Texture2D(2, 1, TextureFormat.RGBA32, false); keyBgTex.filterMode = FilterMode.Bilinear; keyBgTex.wrapMode = TextureWrapMode.Clamp;
        keyBgTex.SetPixels32(new Color32[]{ new Color32(250,246,228,255), new Color32(250,246,228,255) }); keyBgTex.Apply();
        keyBgR.sprite = Sprite.Create(keyBgTex, new Rect(0, 0, 2, 1), new Vector2(0.5f, 0.5f), 100f); keyBgR.enabled = false;
        keyR.sortingOrder = 14;
    }

    /// <summary>0.4.82 극장판 그림 창의 좌우 먹(시안 DE4): 왼쪽 55% → 30%에서 0 … 70%에서 0 → 오른쪽 40%.</summary>
    static Sprite SideShadeSprite ()
    {
        int W = 256; var tx = new Texture2D(W, 2, TextureFormat.RGBA32, false); tx.filterMode = FilterMode.Bilinear; tx.wrapMode = TextureWrapMode.Clamp;
        var ink = new Color(17f/255f, 15f/255f, 14f/255f);
        for (int x = 0; x < W; x++)
        {
            float f = x / (float)(W - 1);
            float a = f < 0.30f ? Mathf.Lerp(0.55f, 0f, Mathf.SmoothStep(0f, 1f, f / 0.30f)) : f > 0.70f ? Mathf.Lerp(0f, 0.40f, Mathf.SmoothStep(0f, 1f, (f - 0.70f) / 0.30f)) : 0f;
            for (int y = 0; y < 2; y++) tx.SetPixel(x, y, new Color(ink.r, ink.g, ink.b, a));
        }
        tx.Apply();
        return Sprite.Create(tx, new Rect(0, 0, W, 2), new Vector2(0.5f, 0.5f), 100f);
    }

    /// <summary>세로 먹 그러데이션(시안 F1): 0%·92 → 9%·55 → 20%·0 … 46%·0 → 62%·50 → 80%·96 → 끝까지 96.</summary>
    static Sprite ShadeSprite ()
    {
        int H = 256; var tx = new Texture2D(2, H, TextureFormat.RGBA32, false); tx.filterMode = FilterMode.Bilinear; tx.wrapMode = TextureWrapMode.Clamp;
        float[] at = { 0f, 0.09f, 0.20f, 0.46f, 0.62f, 0.80f, 1f };
        float[] av = { 0.92f, 0.55f, 0f, 0f, 0.50f, 0.96f, 0.96f };
        var ink = new Color(17f/255f, 15f/255f, 14f/255f);
        for (int y = 0; y < H; y++)
        {
            float f = 1f - y / (float)(H - 1);   // 텍스처 y 는 아래가 0 — 화면 위에서부터 잰 비율로
            float a = av[av.Length - 1];
            for (int i = 1; i < at.Length; i++) if (f <= at[i]) { a = Mathf.Lerp(av[i - 1], av[i], Mathf.SmoothStep(0f, 1f, (f - at[i - 1]) / (at[i] - at[i - 1]))); break; }
            for (int x = 0; x < 2; x++) tx.SetPixel(x, y, new Color(ink.r, ink.g, ink.b, a));
        }
        tx.Apply();
        return Sprite.Create(tx, new Rect(0, 0, 2, H), new Vector2(0.5f, 0.5f), 100f);
    }

    static Camera PaperCam ()
    {
        foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            if (c.cullingMask == (1 << 30)) return c;
        return null;
    }

    bool hooked;
    void HookReset ()
    {
        if (hooked || !Engine.Initialized) return;
        var st = Engine.GetService<IStateManager>();
        if (st == null) return;
        st.OnResetStarted += () => Clear();
        hooked = true;
    }

    void LateUpdate ()
    {
        Ensure();
        HookReset();
        if (!preloadStarted) { preloadStarted = true; StartCoroutine(Preload()); }
        var cam = PaperCam();
        if (cam == null) return;

        // 페이드 — 0.4.42: 새 그림은 그 자리에서 바로 바꾸고(옛 그림이 꺼졌다 다시 비치다 반쯤에서 바뀌던 0.6초짜리 교차를 없앴다),
        //         옅게(35%)에서 0.22초 만에 또렷해진다. 그림을 불러오느라 한 프레임이 길어져도 단계를 건너뛰지 않게 dt 를 묶는다.
        float fdt = Mathf.Min(Time.unscaledDeltaTime, 0.05f) / FadeIn;
        sceneT = Mathf.Min(1f, sceneT + fdt);
        charT = Mathf.Min(1f, charT + fdt);
        if (sceneCur != null && sceneR.sprite != sceneCur) sceneR.sprite = sceneCur;
        if (charCur != null && charR.sprite != charCur) charR.sprite = charCur;
        float sA = sceneR.sprite == null ? 0f : (sceneCur == null ? 1f - sceneT : Mathf.Lerp(FadeFrom, 1f, sceneT));
        // 그림은 한 번에 하나: 인물이 서 있으면 장면은 가린다(인물이 빠지면 다시 떠오른다)
        // 0.4.9: 인물이 서면 장면을 지우지 않고 옅게 깔아 둔다 — 인물 뒤에 그 자리의 풍경이 보인다
        bool cine = MolakTheme.CinemaNow, thr = MolakTheme.TheaterNow;
        sceneVis = Mathf.MoveTowards(sceneVis, charCur != null && !cine ? 0.7f : 1f, Time.deltaTime * 2.4f);   // 그림 화면은 인물 뒤 풍경도 온전히   // 인물이 불투명해졌으니 뒤 풍경은 덜 지운다
        sA *= sceneVis;
        float cA = charR.sprite == null ? 0f : (charCur == null ? 1f - charT : Mathf.Lerp(FadeFrom, 1f, charT));
        if (sceneCur == null && sceneT >= 1f) sceneR.sprite = null;
        if (charCur == null && charT >= 1f) charR.sprite = null;
        // 0.4.28: 인물은 곱하기가 아니라 보통 알파로 — 곱하기면 얼굴·흰 옷 같은 밝은 데로 뒤 배경이 비쳤다(인물 그림도 안쪽을 채워 다시 오렸다)
        var wantMat = cine || thr ? matCine : MolakTheme.Night ? matAlpha : matScene;   // 그림 화면: 인물은 제 빛 그대로(종이 오린 듯)
        if (charR.sharedMaterial != wantMat) charR.sharedMaterial = wantMat;
        bool hide = MolakMenu.IsOpen;
        sceneR.enabled = !hide; charR.enabled = !hide;
        colT = Mathf.MoveTowards(colT, Collapsed && !cine && !thr ? 0f : 1f, Time.deltaTime*4f);
        // 야간(먹색) 모드: 종이색 장면 패널이 하얗게 떠 보이지 않도록 어둡게 눌러 그린다
        float sg = MolakTheme.Night ? 0.62f : 1f;
        sceneR.color = thr ? new Color(0.80f, 0.80f, 0.80f, sA*colT) : cine ? new Color(0.72f, 0.72f, 0.72f, sA*colT) : new Color(sg, sg, sg, sA*(MolakTheme.Night ? 0.80f : 0.88f)*colT);
        float gDim = 1f;
        if (gest == "turn" || gest == "recoil") { float ge = gestT < 0.12f ? gestT / 0.12f : gestT < 0.4f ? 1f : Mathf.Clamp01(1f - (gestT - 0.4f) / 0.45f); gDim = 1f - (gest == "turn" ? 0.35f : 0.18f) * ge; }
        if (sunk) gDim *= 0.85f;
        charR.color = cine || thr ? new Color(0.9f * gDim, 0.9f * gDim, 0.9f * gDim, cA*colT) : new Color(gDim, gDim, gDim, cA*colT);
        // 0.4.82 말하지 않는 동안 인물은 흐리고 어둡게 물러나 있다가, 입을 열면 0.25초에 또렷해진다
        quietK = Mathf.MoveTowards(quietK, (cine || thr) && !MolakScroll.CharSpeaking ? 1f : 0f, Mathf.Min(Time.unscaledDeltaTime, 0.05f) / 0.25f);   // 0.4.85 1: 밝기 약 45%(인물 0.9 × 셰이더 0.5), 빛 절반 뺌
        if (matCine != null && matCine != matScene) matCine.SetFloat("_Quiet", quietK);

        // 배치: 카메라 뷰 기준 캔버스(900×1600) 좌표 → 월드
        float camH = cam.orthographic ? cam.orthographicSize*2f : 20f;
        float camW = camH*cam.aspect;
        float unit = Mathf.Min(camW/900f, camH/1600f);   // 캔버스 1단위의 월드 크기 (CanvasScaler Expand와 동일)
        float cx = cam.transform.position.x, top = cam.transform.position.y + camH*0.5f;
        float z = cam.transform.position.z + 3.5f;
        float bandW = Mathf.Min(camW, 900f*unit);
        float bandH = BandH*unit;
        float bandCy = top - (BandTop + BandH*0.5f)*unit;
        if (cine) { bandW = camW; bandH = camH; bandCy = cam.transform.position.y; }   // 0.4.82 온 화면
        if (thr) { bandW = camW; bandH = (MolakLayout.ThArtBottom - MolakLayout.ThArtTop) * unit; bandCy = top - (MolakLayout.ThArtTop + (MolakLayout.ThArtBottom - MolakLayout.ThArtTop) * 0.5f) * unit; }   // 0.4.82 극장판: 금줄 사이 창
        shadeR.enabled = (cine || thr) && !hide && (sceneR.sprite != null || charR.sprite != null);
        if (shadeR.enabled)
        {
            if (thr)
            {
                if (shadeR.sprite != shadeH) shadeR.sprite = shadeH;
                shadeR.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                shadeR.transform.position = new Vector3(cx, bandCy, z - 0.1f);
                shadeR.transform.localScale = new Vector3(bandW / 2.56f, bandH / 0.02f, 1f);
            }
            else
            {
                if (shadeR.sprite != shadeV) shadeR.sprite = shadeV;
                shadeR.maskInteraction = SpriteMaskInteraction.None;
                shadeR.transform.position = new Vector3(cx, cam.transform.position.y, z - 0.1f);
                shadeR.transform.localScale = new Vector3(camW / 0.02f, camH / 2.56f, 1f);
            }
        }

        mask.transform.position = new Vector3(cx, bandCy, z);
        mask.transform.localScale = new Vector3(bandW/0.04f, bandH/0.04f, 1f);

        // 맞은 순간의 흔들림(0.32초)
        float jit = 0f;
        if (hurtT >= 0f) { hurtT += Time.deltaTime; float hk = 1f - hurtT / 0.32f; if (hk <= 0f) hurtT = -1f; else jit = Mathf.Sin(hurtT * 70f) * hk * 14f * unit; }
        float sceneW = bandW, sceneCx = cx;
        if (sceneR.sprite != null)
        {
            var sp = sceneR.sprite; float sw = sp.texture.width/100f, sh = sp.texture.height/100f;
            float k = Mathf.Max(sceneW/sw, bandH/sh);
            // 그림이 멈춰 있지 않도록: 아주 느리게 다가갔다 물러나며 옆으로 흐른다(한 바퀴 약 1분 반). 띠 밖으로는 넘치지 않는다.
            float t = Time.time;
            float zoom = 1.06f + 0.03f * Mathf.Sin(t * 0.07f);
            k *= zoom;
            float spareX = Mathf.Max(0f, sw * k - sceneW) * 0.5f, spareY = Mathf.Max(0f, sh * k - bandH) * 0.5f;
            float px = Mathf.Sin(t * 0.043f + 1.3f) * spareX * 0.8f, py = Mathf.Cos(t * 0.031f) * spareY * 0.6f;
            sceneR.transform.localScale = new Vector3(k, k, 1f);
            sceneR.transform.position = new Vector3(sceneCx + px + (charR.sprite == null ? jit : 0f), bandCy + py, z + 0.1f);
        }
        if (charR.sprite != null)
        {
            var sp = charR.sprite; float sw = sp.texture.width/100f, sh = sp.texture.height/100f;
            float k = cine ? Mathf.Min(camH*0.62f/sh, camW*1.05f/sw) : thr ? Mathf.Min(bandH*1.27f/sh, camW*1.05f/sw) : (bandH*1.75f)/sh;   // 그림 화면: 화면 높이의 62%
            // 인물은 등장할 때 한 걸음 다가오고(0.7초), 그 뒤로는 숨을 쉰다
            pushT = Mathf.Min(1f, pushT + Time.deltaTime / 0.7f);
            float ease = 1f - Mathf.Pow(1f - pushT, 3f);
            k *= Mathf.Lerp(0.93f, 1f, ease) * (1f + 0.006f * Mathf.Sin(Time.time * 1.15f));
            float want = pose == 1 ? 1.1f : pose == 2 ? 0.94f : 1f;
            poseK = Mathf.Lerp(poseK <= 0f ? 1f : poseK, want, 1f - Mathf.Pow(0.002f, Time.deltaTime));
            k *= poseK;
            if (pose == 3) jit += Mathf.Sin(Time.time * 9f) * 10f * unit;
            // 0.4.83 잘림 없게(언메이커: 움직임까지 고려해 잘리는 데가 없어야): 극장판은 정수리를 창 위 28단위 아래에 묶고(커지고 숨 쉬어도 머리가 창 밖으로 안 나간다),
            //   어떤 움직임에도 그림 아랫변(허벅지에서 끊긴 자리)이 창 안으로 올라오지 않게 키를 받친다. 좌우는 화면 밖으로 밀려나지 않을 만큼만 비켜 선다.
            if (thr) k = Mathf.Max(k, (bandH - 28f * unit + 30f * unit) / sh);
            // 몸짓
            gestT += Time.unscaledDeltaTime;
            float gx = 0f, gy = 0f, gk = 1f;
            float env = gestT < 0.12f ? gestT / 0.12f : gestT < 0.4f ? 1f : Mathf.Clamp01(1f - (gestT - 0.4f) / 0.45f);
            env = Mathf.SmoothStep(0f, 1f, env);
            switch (gest)
            {
                case "lunge": gk = 1f + 0.18f * env; gx -= camW * 0.04f * env; break;   // 크게 한 걸음 다가선다(정수리는 창 위에 묶여 있어 아래로 커진다)
                case "recoil": gx += camW * 0.11f * env; gy -= 14f * unit * env; break;   // 뒤로 젖히며 물러선다
                case "tremble": if (gestT < 1.8f) gx += Mathf.Sin(gestT * 52f) * 9f * unit * (1f - gestT / 1.8f); break;
                case "bounce": if (gestT < 0.8f) gy -= Mathf.Abs(Mathf.Sin(gestT * 13.5f)) * 16f * unit * (1f - gestT / 0.8f); break;
                case "nod": if (gestT < 0.55f) gy -= Mathf.Sin(Mathf.PI * gestT / 0.55f) * 26f * unit; break;
                case "turn": gx += camW * 0.16f * env; break;
            }
            if (sunk) gy -= bandH * 0.13f * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(gest == "sink" ? gestT / 0.7f : 1f));
            k *= gk;
            charR.transform.localScale = new Vector3(k, k, 1f);
            float x = cx + jit + gx;   // 홀로 서니 띠 가운데
            // 새 사람: 오른쪽 화면 밖에서 0.45초에 미끄러져 들어온다(끝에서 부드럽게 선다)
            slideT = Mathf.Min(1f, slideT + Time.unscaledDeltaTime / 0.45f);
            if (slideT < 1f) x += (1f - (1f - Mathf.Pow(1f - slideT, 3f))) * camW * 0.55f;
            float y = cine ? top - camH*0.105f - sh*k*0.5f : thr ? bandCy + bandH*0.5f - 28f*unit - sh*k*0.5f : bandCy + bandH*0.5f - sh*k*0.5f + 16f*unit;
            if (thr) x += Mathf.Min(sw * k * 0.12f, Mathf.Max(0f, (camW - sw * k) * 0.5f));   // 시안처럼 조금 오른쪽 — 넓은 그림은 덜
            charR.transform.position = new Vector3(x, y + gy, z);
        }
        // 장면 전환 암전: 바로 먹으로 덮고 0.2초 머문 뒤 0.6초에 걷히며 새 장면이 떠오른다
        dipT += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        float dipA = dipT < 0.2f ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (dipT - 0.2f) / 0.6f);
        dipR.enabled = !hide && dipT < 0.8f && (sceneR.sprite != null || charR.sprite != null);
        if (dipR.enabled)
        {
            dipR.color = new Color(17f/255f, 15f/255f, 14f/255f, Mathf.Clamp01(dipA) * 0.92f * colT);
            dipR.transform.localScale = new Vector3(bandW / 0.04f, bandH / 0.04f, 1f);
            dipR.transform.position = new Vector3(cx, bandCy, z - 0.25f);
        }
        // 마디 대표 그림: 마디 이름이 덮는 만큼 짙게, 0.5초에 떠오르고 아주 느리게 다가간다
        float cover = MolakVfx.ChapterCover;
        if (keyCur != null && cover <= 0f && keyIn > 0.5f) keyCur = null;
        if (keyCur != null && keyR.sprite != keyCur) keyR.sprite = keyCur;
        if (keyCur == null) { keyR.sprite = null; keyR.enabled = false; if (keyBgR != null) keyBgR.enabled = false; }
        else
        {
            keyIn = Mathf.Min(1f, keyIn + Mathf.Min(Time.unscaledDeltaTime, 0.05f) / 0.5f);
            keyT += Time.unscaledDeltaTime;
            keyR.enabled = !hide;
            float ka = Mathf.SmoothStep(0f, 1f, keyIn) * cover;
            float kg2 = MolakTheme.Night ? 0.78f : 1f;
            keyR.color = new Color(kg2, kg2, kg2, ka * colT);
            var sp = keyR.sprite; float sw = sp.texture.width/100f, sh = sp.texture.height/100f;
            // 그림 창이 그림보다 1.1배 넘게 넓으면(폴드 안쪽·가로 PC) 위아래를 자르지 않고 높이에 맞추고, 양옆은 그림 끝의 종이빛으로 메운다
            bool wide = bandW / bandH > (sw / sh) * 1.1f;
            float creep = 1f + 0.03f * Mathf.Clamp01(keyT / 8f);
            float k = (wide ? bandH / sh : Mathf.Max(bandW/sw, bandH/sh)) * creep;
            keyR.transform.localScale = new Vector3(k, k, 1f);
            float spare = Mathf.Max(0f, sw * k - bandW) * 0.5f, spareY = Mathf.Max(0f, sh * k - bandH) * 0.5f;
            float kx = Mathf.Clamp((0.5f - keyFx) * sw * k, -spare, spare);
            float ky = Mathf.Clamp((keyFy - 0.5f) * sh * k, -spareY, spareY);   // 그림 아래쪽을 보려면 위로 올린다
            keyR.transform.position = new Vector3(cx + kx, bandCy + ky, z - 0.2f);
            keyBgR.enabled = keyR.enabled && wide;
            if (keyBgR.enabled)
            {
                keyBgR.color = new Color(kg2, kg2, kg2, ka * colT);
                keyBgR.transform.localScale = new Vector3(bandW / 0.02f, bandH / 0.01f, 1f);
                keyBgR.transform.position = new Vector3(cx, bandCy, z - 0.15f);
            }
        }
    }
}
