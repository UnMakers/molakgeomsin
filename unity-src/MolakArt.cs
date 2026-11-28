using System.Collections.Generic;
using MolakVn;
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>0.4.85 화면 점검용: 지금 실제로 그려지는 그림과 투명도.</summary>
    public static string DbgScene { get { return inst != null && inst.sceneR != null && inst.sceneR.sprite != null && inst.sceneR.enabled ? inst.sceneR.sprite.name : ""; } }
    public static float DbgSceneA { get { return inst != null && inst.sceneR != null && inst.sceneR.enabled && inst.sceneR.sprite != null ? inst.sceneR.color.a : 0f; } }
    public static string DbgChar { get { return inst != null && inst.charR != null && inst.charR.sprite != null && inst.charR.enabled ? inst.charR.sprite.name : ""; } }
    public static float DbgCharA { get { return inst != null && inst.charR != null && inst.charR.enabled && inst.charR.sprite != null ? inst.charR.color.a : 0f; } }
    public static float DbgDipA { get { return inst != null && inst.dipR != null && inst.dipR.enabled ? inst.dipR.color.a : 0f; } }
    public static float DbgKeyA { get { return inst != null && inst.keyR != null && inst.keyR.enabled && inst.keyR.sprite != null ? inst.keyR.color.a : 0f; } }
#endif
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
    // 0.4.88 두 사람 세우기: 말하는 사람(charR)과 곁에서 듣는 사람(partR). 그림 화면·극장판에서만 둘을 좌우에 세운다.
    //   말하는 쪽은 밝게 앞으로, 듣는 쪽은 어둡게 조금 뒤로. 사람마다 선 자리(왼/오)는 바뀌지 않고, 말하는 사람이 바뀌면 밝기만 바뀐다.
    SpriteRenderer partR; Sprite partCur, showPart; string partId = "", showPartId = ""; float partT = 1f, partSlideT = 1f; static Material matPart;
    string leftPerson = "";
    SpriteMask mask;
    Sprite sceneCur, charCur;          // 대본이 정한 그림(곧바로 바뀐다)
    // 0.4.85 기준: 그림은 글과 함께 바뀐다. 대본이 그림을 몇 번 바꾸든(@art scene → @page 의 바탕 되돌리기 → @art char → 인물 자리 그림 …)
    //   화면은 다음 글이 찍히기 직전(또는 선택지·정지)에 '마지막으로 보이던 그림 → 지금 정해진 그림'으로 딱 한 번 넘어간다(Commit).
    //   그 사이의 그림은 화면에 나오지 않는다. 그래서 번짐이 두 번·세 번 도는 일이 구조적으로 생기지 않는다.
    Sprite showScene, showChar; string showCharId = ""; bool dirty, noInkOnce; float dirtyAt;
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
    // 0.4.85 장면 전환 「먹이 번진다」(언메이커 시안 2): 옛 그림(장면·인물)을 아래 깔아 두고, 새 그림이 가운데서 먹처럼 번져 나온다.
    //   새 장면·인물은 번짐 가림판(SpriteMask, alphaCutoff 를 1→0)에만 비치고, 번지는 끝에 먹 테(Molak/InkEdge)를 두른다.
    SpriteRenderer oldSceneR, oldCharR, inkR; SpriteMask revealMask, oldMask; Material inkMat, oldMat; Sprite revealSp; float inkT = 9f; int inkWarm = 4;
    const float InkDur = 1.4f;   // 시안 2 그대로
    public static int InkStarts; public static float InkMaxDt, InkSumDt; public static int InkN; int inkFrames;   // 점검용: 마지막 번짐 동안 가장 긴 프레임(초)
#if UNITY_EDITOR
    public static bool CapArm; static float capUntil; static int capN;   // 점검용: 번짐 프레임을 Build/cap 에 찍는다
#endif
    public static bool InkOn = true;
    public static void Dip () { if (inst != null) inst.dipAt = Time.frameCount; }   // 0.4.85 바로 뒤에 장면이 바뀌면(번짐) 암전은 하지 않는다 — 몇 프레임 기다려 본다
    int dipAt = -1;
    /// <summary>0.4.86 쪽 넘김 먹 덮개가 아직 글을 가리는가 — 시간(0.5초)이 아니라 덮개 농도로 본다(0.5초에도 덮개가 50% 남아 글이 덮인 채 찍혔다).
    /// 덮개를 기다리는 몇 프레임(Dip 요청 뒤 번짐이 오는지 보는 동안)도 덮개로 친다.</summary>
    public static bool Dipping { get { return inst != null && (inst.dipAt >= 0 || DipAlpha > 0.35f || inst.inkT < 0.25f); } }
    static float DipAlpha { get { if (inst == null || inst.dipT >= 0.8f) return 0f; float t = inst.dipT; return t < 0.2f ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (t - 0.2f) / 0.6f); } }
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
        inst.soundPending = true;   // 0.4.91 배경음·장면 소리는 그림이 화면에 오를 때(Commit) 같이 바뀐다
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
        // 0.4.91 기준: 불길이 이어지는 동안(inferno·불티 날씨 — @vfx fireon)은 장면이 바뀌어도 곡도 불이다.
        //   서장에서 불타는 산장을 등지고 뒷산으로 달아나면(숲, 불길은 이어짐) 낮 곡이 흐르던 것(언메이커: 도망친다를 고르면 이상한 곡).
        var wk = MolakVfx.WeatherKind;
        if (wk == "inferno" || wk == "embers") return "fire";
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
    static Dictionary<string, string> HomeDay => MolakCast.HomeDay;   // 0.4.88 cast.txt
    static Dictionary<string, string> HomeNight => MolakCast.HomeNight;
    public static bool IsNight;
    static readonly Dictionary<string, string> SceneWeather = new Dictionary<string, string>
    {
        {"rain","rain"},{"flood","rain"},{"snow","snow"},{"manorfire","inferno"},{"herbfire","embers"},{"bloodroof","bloodrain"},{"raid","bloodrain"},{"raidnight","embers"},{"soanight","rain"},
    };

    public static void Char (string id)
    {
        if (inst == null) return;
        if (string.IsNullOrEmpty(id) || id == "none" || id == "off") { inst.SetChar(null, ""); inst.SetPart(null, ""); inst.soundPending = true; return; }
        if (inst.charId == id) return;
        // 0.4.88 곁에 선 사람이 입을 열면 두 사람의 역할만 바꾼다(그 자리에 그대로 선 채 밝기만 바뀐다)
        if (inst.partCur != null && Person(id) == Person(inst.partId))
        {
            var oldSp = inst.charCur; var oldId = inst.charId;
            var nsp = Load("MolakArt/char_" + id);
            if (nsp != null) { inst.SetChar(nsp, id); inst.SetPart(oldSp, oldId); return; }
        }
        else if (inst.partCur != null && Person(id) != Person(inst.charId)) inst.SetPart(null, "");   // 셋째 사람이 오면 혼자 선다
        if (Generic.Contains(inst.sceneId) && !inst.sceneId.StartsWith("duel"))
        {
            string home;
            var homes = IsNight ? HomeNight : HomeDay;
            if ((homes.TryGetValue(id, out home) || homes.TryGetValue(Person(id), out home)) && home != inst.sceneId)
            {
                var hs = Load("MolakArt/scene_" + home);
                if (hs != null) inst.SetScene(hs, home);   // 인물의 자리 그림으로 바뀔 때도 먹이 번진다
            }
            else if (inst.sceneCur == null)
            {
                var fs = Load("MolakArt/scene_" + (IsNight ? "night" : "morning"));
                if (fs != null) inst.SetScene(fs, IsNight ? "night" : "morning");
            }
        }
        var sp = Load("MolakArt/char_" + id);
        inst.SetChar(sp, id);
        inst.soundPending = true;   // 0.4.70 소아·히로인이 나오면 그 사람의 곡 — 0.4.91 그 사람이 화면에 설 때(Commit)
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

    public static void Clear () { if (inst == null) return; inst.SetScene(null, ""); inst.SetChar(null, ""); inst.SetPart(null, ""); inst.noInkOnce = true; inst.DoCommit(); }

    /// <summary>0.4.88 @art char:말하는사람 with:곁에선사람 — 두 사람을 함께 세운다. with:none 이면 혼자.</summary>
    public static void With (string id)
    {
        if (inst == null) return;
        if (string.IsNullOrEmpty(id) || id == "none" || id == "off") { inst.SetPart(null, ""); return; }
        if (inst.charCur == null || Person(id) == Person(inst.charId)) return;
        if (Person(id) == Person(inst.partId) && inst.partId == id) return;
        var sp = Load("MolakArt/char_" + id);
        if (sp == null) return;
        if (inst.partCur == null) inst.leftPerson = Person(id);   // 나중에 온 사람(곁에 선 사람)이 왼쪽
        inst.SetPart(sp, id);
    }
    void SetPart (Sprite s, string id)
    {
        partCur = s; partId = s != null ? (id ?? "") : "";
        if (s == null) leftPerson = "";
        if (!restoring) MolakStats.SetStr("artwith", partId);
        MarkDirty();
    }
    // 0.4.85 언메이커: 불러오기 하면 그림이 안 뜬다 — 그림 상태는 나니노벨 저장에 들지 않아서, 다음 @art 까지 빈 창이었다.
    //   장면·인물 이름을 이번 생 변수(artscene/artchar)에 적어 두고, 불러오기가 끝나면 그대로 다시 세운다.
    static bool restoring;
    public static void RestoreAfterLoad ()
    {
        if (inst == null) return;
        string sc = MolakStats.Str("artscene", ""), ch = MolakStats.Str("artchar", "");
        // 그림 이름이 저장에 없으면(이 기능 전의 기록) 대본을 거슬러 올라가 마지막으로 깐 장면·인물을 찾는다
        if (string.IsNullOrEmpty(sc) && string.IsNullOrEmpty(ch)) ScanBack(out sc, out ch);
        inst.sceneId = "\u0001"; inst.charId = "\u0001"; inst.charCur = null;
        if (string.IsNullOrEmpty(sc)) Scene(IsNight ? "night" : "morning"); else Scene(sc);
        if (!string.IsNullOrEmpty(ch)) Char(ch);
        string wi = MolakStats.Str("artwith", ""); inst.partCur = null; inst.partId = "";
        if (!string.IsNullOrEmpty(wi) && wi != "none") With(wi);
        MolakStats.SetStr("artscene", inst.sceneId ?? ""); MolakStats.SetStr("artchar", inst.charId ?? "");
        inst.noInkOnce = true; inst.DoCommit();   // 불러온 그림은 번지지 않고 바로
    }

    /// <summary>지금 멈춘 줄에서 위로 올라가며 가장 가까운 @art/@scene 을 찾는다. 장면을 만나면 멈추고, 그보다 뒤(아래)에서 세운 인물은 살린다.</summary>
    static void ScanBack (out string sc, out string ch)
    {
        sc = ""; ch = "";
        try
        {
            var sp = Engine.GetService<IScriptPlayer>();
            var tr = sp != null ? sp.MainTrack : null;
            var pl = tr != null ? tr.Playlist : null;
            if (pl == null) return;
            for (int i = Mathf.Min(tr.PlayedIndex, pl.Count - 1); i >= 0; i--)
            {
                var ac = pl[i] as ArtCommand;
                if (ac != null)
                {
                    if (ac.Off != null && ac.Off.HasValue && ac.Off.Value) return;
                    if (ch.Length == 0 && ac.CharId != null && ac.CharId.HasValue) ch = ac.CharId.Value ?? "";
                    if (ac.SceneId != null && ac.SceneId.HasValue) { sc = ac.SceneId.Value ?? ""; return; }
                    continue;
                }
                var sc2 = pl[i] as SceneCommand;
                if (sc2 != null && sc2.Id != null && sc2.Id.HasValue) { sc = sc2.Id.Value ?? ""; return; }
            }
        }
        catch (System.Exception e) { Debug.LogWarning("[MolakArt] ScanBack " + e.Message); }
    }

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
        sceneCur = s; sceneId = id;
        if (!restoring) MolakStats.SetStr("artscene", id ?? "");   // 불러오기 때 그림을 되살리려고 저장 상태에 적어 둔다
        if (s != null && charCur != null) SetChar(null, "");       // 새 장면은 서 있던 인물을 내린다
        if (s != null && partCur != null) SetPart(null, "");
        MarkDirty();
    }
    /// <summary>전신으로 뽑은 그림(소아 등장·공격)은 초상 비율보다 몸이 길어 띠 안에서 작게 보인다(언메이커: 더 확대해야). 정수리는 띠 위에 묶여 있으니 아래가 잘리며 커진다.</summary>
    static float CharZoom (string id)
    {
        switch (id) { case "soa_enter": return 1.55f; case "soa_attack": return 1.55f; }
        return 1f;
    }
    void MarkDirty () { if (!dirty) dirtyAt = Time.unscaledTime; dirty = true; }

    /// <summary>0.4.85 지금 정해진 그림을 화면에 올린다 — 마지막으로 보이던 그림에서 한 번만 넘어간다. 다음 글 직전·선택지·정지에서 부른다.</summary>
    public static void Commit () { if (inst != null) inst.DoCommit(); }
    /// <summary>0.4.91 기준: 소리는 그림을 따른다(언메이커: 글이 끝나고 다음 장으로 넘어가기 전에 배경음이 먼저 바뀐다).
    /// 예전엔 @art 명령이 실행되는 순간 곡을 바꿔, 쪽 넘김 덮개가 내려오는 동안(옛 글이 아직 보이는 동안) 새 곡이 먼저 흘렀다.
    /// 이제 장면·인물 명령은 '바꿀 곡'만 적어 두고, 그 그림이 화면에 오르는 순간(DoCommit) 배경음 묶음과 장면 소리를 함께 바꾼다.</summary>
    bool soundPending;
    void ApplySound ()
    {
        if (!soundPending) return;
        soundPending = false;
        string sid = sceneId ?? "";
        MolakBgm.Mood(sid.StartsWith("duel") ? MoodFor(sid) : CurrentMood());
        MolakAmbience.Scene(sid);
    }
    // 0.4.95 지난 쪽 보기(MolakPageBack): 그 쪽이 찍힐 때 떠 있던 그림을 잠시 비춘다. 지금 그림은 따로 두었다가 돌아오면 되돌린다.
    bool previewing; Sprite pvScene, pvChar, pvPart; string pvSceneId = "", pvCharId = "", pvPartId = "";
    public static string ShownScene { get { return inst != null ? (inst.previewing ? inst.pvSceneId : inst.showSceneId) ?? "" : ""; } }
    public static string ShownChar { get { return inst != null ? (inst.previewing ? inst.pvCharId : inst.showCharId) ?? "" : ""; } }
    public static void Preview (string scene, string ch)
    {
        if (inst == null) return;
        if (!inst.previewing) { inst.previewing = true; inst.pvScene = inst.showScene; inst.pvChar = inst.showChar; inst.pvPart = inst.showPart; inst.pvSceneId = inst.showSceneId; inst.pvCharId = inst.showCharId; inst.pvPartId = inst.showPartId; }
        inst.showScene = string.IsNullOrEmpty(scene) ? null : Load("MolakArt/scene_" + scene); inst.showSceneId = scene ?? "";
        inst.showChar = string.IsNullOrEmpty(ch) ? null : Load("MolakArt/char_" + ch); inst.showCharId = ch ?? "";
        inst.showPart = null; inst.showPartId = "";
        inst.sceneT = 1f; inst.charT = 1f;
    }
    public static void EndPreview () { if (inst != null) inst.StopPreview(); }
    void StopPreview ()
    {
        if (!previewing) return;
        previewing = false;
        showScene = pvScene; showChar = pvChar; showPart = pvPart; showSceneId = pvSceneId; showCharId = pvCharId; showPartId = pvPartId;
    }

    void DoCommit ()
    {
        if (sceneR == null) return;
        StopPreview();   // 지난 쪽을 보던 중이면 지금 그림으로 돌아온 뒤 올린다
        ApplySound();
        if (!dirty) return;
        dirty = false;
        bool sceneChanged = showScene != sceneCur, charChanged = showChar != charCur, partChanged = showPart != partCur;
        // 0.4.88 두 사람이 역할만 바꿨다(말하던 사람 ↔ 곁의 사람): 미끄러져 들어오거나 다시 떠오르지 않는다
        bool swapped = charCur != null && partCur != null && showChar != null && showPart != null && Person(charId) == Person(showPartId) && Person(partId) == Person(showCharId);
        if (partChanged && !swapped) { partT = partCur != null && showPart == null ? 0f : 1f; if (partCur != null && showPart == null) partSlideT = 0f; }
        showPart = partCur; showPartId = partId ?? "";
        bool noInk = noInkOnce; noInkOnce = false;
        bool underBlack = MolakBlackout.Covering;   // 0.5.0 먹 화면 밑: 번지지도 떠오르지도 않고 그 자리에서 바뀐다(걷히면 이미 서 있게)
        if (!sceneChanged && !charChanged) return;
        MolakSyncCheck.OnCommit(sceneId, charId, showSceneId, showCharId);   // 0.4.95 옛 쪽을 읽는 중에 그림이 바뀌면 적는다
        if (swapped && !sceneChanged) { showChar = charCur; showCharId = charId ?? ""; charR.sprite = charCur; return; }
        ChangedAt = Time.unscaledTime;
        string sid = sceneId ?? "";
        bool ink = !noInk && InkOn && sceneChanged && sceneCur != null && oldSceneR != null && (sceneR.sprite != null || charR.sprite != null)
                   && !sid.StartsWith("duel") && !(showSceneId ?? "").StartsWith("duel") && !MolakPlayMode.SkipAll && !(Engine.GetService<IScriptPlayer>()?.Skipping ?? false);
        bool newPerson = charCur != null && Person(charId) != Person(showChar != null ? showCharId : null);
        if (charChanged && newPerson) { pushT = 0f; slideT = 0f; gest = ""; gestT = 9f; sunk = false; }
        if (underBlack)
        {
            inkT = InkDur; sceneR.sprite = sceneCur; charR.sprite = charCur; sceneT = 1f; charT = 1f;
            if (charChanged && newPerson) { pushT = 1f; slideT = 1f; }
        }
        else if (ink)
        {
            bool inking = inkT < InkDur && oldSceneR.sprite != null;
            // 번지는 중에 또 넘어가면: 반도 안 번졌으면 옛 그림은 그대로 두고 새 목표로 다시, 반 넘게 번졌으면 지금 그림을 옛 그림 삼아
            if (!(inking && inkT < InkDur * 0.5f))
            {
#if UNITY_EDITOR
                if (CapArm) { capUntil = Time.unscaledTime + 1.5f; capN = 0; }
#endif
                oldSceneR.sprite = sceneR.sprite; oldSceneR.color = sceneR.color; oldSceneR.transform.position = sceneR.transform.position + new Vector3(0f, 0f, 0.05f); oldSceneR.transform.localScale = sceneR.transform.localScale;
                oldCharR.sprite = charR.sprite; oldCharR.color = charR.color; oldCharR.transform.position = charR.transform.position + new Vector3(0f, 0f, 0.05f); oldCharR.transform.localScale = charR.transform.localScale;
            }
            InkStarts++; dipT = 9f; dipAt = -1;   // 번짐이 시작되면 쪽 넘김 암전은 하지 않는다
            inkT = 0f; InkMaxDt = 0f; InkSumDt = 0f; InkN = 0; inkFrames = 0;
            sceneR.sprite = sceneCur; charR.sprite = charCur;
            sceneT = 1f; charT = 1f;
        }
        else
        {
            if (sceneChanged) { sceneT = 0f; if (sceneCur != null) sceneR.sprite = sceneCur; }
            if (charChanged) { charT = 0f; if (charCur != null) charR.sprite = charCur; }
        }
        showScene = sceneCur; showSceneId = sid; showChar = charCur; showCharId = charId ?? "";
    }
    string showSceneId = "";

    /// <summary>0.4.84 새 사람이 들어오면 옆에서 미끄러져 들어온다(언메이커: 처음 등장할 때 옆에서 나오는 게 좋다). 같은 사람의 표정만 바뀌면(soa → soa_smile) 제자리에서 바뀐다.</summary>
    static string Person (string id) { if (string.IsNullOrEmpty(id)) return ""; int u = id.IndexOf('_'); return u > 0 ? id.Substring(0, u) : id; }
    void SetChar (Sprite s, string id)
    {
        bool newPerson = s != null && Person(id) != Person(charCur != null ? charId : null);
        charCur = s; charId = id;
        if (!restoring) MolakStats.SetStr("artchar", id ?? "");
        MarkDirty();
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
    const float SlideDur = 0.5f, SlideDist = 0.30f;
    static float SlideStep () { return Mathf.Min(Time.unscaledDeltaTime, 1f / 40f) / SlideDur; }
    static float SlideOff (float t) { return Mathf.Pow(1f - Mathf.Clamp01(t), 2.2f) * SlideDist; }
    /// <summary>0.4.65 그림이 마지막으로 바뀐 때(빨리에서 글을 그림 뒤로 반 박자 늦추는 데 쓴다).</summary>
    public static float ChangedAt = -9f;
    /// <summary>0.4.65 그림이 아직 다 떠오르지 않았다(장면·인물 페이드, 인물 뒤 풍경 눌림까지).</summary>
    public static bool Busy { get { return inst != null && (inst.sceneT < 1f || inst.charT < 1f); } }
    /// <summary>0.5.0 번짐이 끝나기까지 남은 초(번지는 중이 아니면 0).</summary>
    public static float InkLeft { get { return inst == null ? 0f : Mathf.Max(0f, InkDur - inst.inkT); } }
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
        mask.isCustomRangeActive = false;   // 띠 가림판: 창 안의 모든 그림
        var og = new GameObject("MaskOld"); og.transform.SetParent(band.transform, false); og.layer = 30;
        oldMask = og.AddComponent<SpriteMask>(); oldMask.sprite = mask.sprite; oldMask.isCustomRangeActive = true; oldMask.backSortingOrder = 4; oldMask.frontSortingOrder = 9; oldMask.enabled = false;   // 쓰지 않는다(옛 그림은 셰이더가 걷는다)
        var rg = new GameObject("MaskReveal"); rg.transform.SetParent(band.transform, false); rg.layer = 30;
        revealMask = rg.AddComponent<SpriteMask>(); revealSp = RevealSprite(); revealMask.sprite = revealSp; revealMask.isCustomRangeActive = true; revealMask.backSortingOrder = 9; revealMask.frontSortingOrder = 11;   // 새 장면·인물(10·11)
        revealMask.alphaCutoff = 0.001f; revealMask.enabled = false;
        var osg = new GameObject("OldScene"); osg.transform.SetParent(band.transform, false); osg.layer = 30;
        oldSceneR = osg.AddComponent<SpriteRenderer>(); oldSceneR.sortingOrder = 12; oldSceneR.maskInteraction = SpriteMaskInteraction.VisibleInsideMask; oldSceneR.enabled = false;
        var ocg = new GameObject("OldChar"); ocg.transform.SetParent(band.transform, false); ocg.layer = 30;
        oldCharR = ocg.AddComponent<SpriteRenderer>(); oldCharR.sortingOrder = 13; oldCharR.maskInteraction = SpriteMaskInteraction.VisibleInsideMask; oldCharR.enabled = false;
        var ig = new GameObject("InkEdge"); ig.transform.SetParent(band.transform, false); ig.layer = 30;
        inkR = ig.AddComponent<SpriteRenderer>(); inkR.sortingOrder = 14; inkR.sprite = revealSp; inkR.enabled = false;
        var inkRes = Resources.Load<Material>("MolakArt/InkEdge"); var ish = Shader.Find("Molak/InkEdge");
        inkMat = inkRes != null ? new Material(inkRes) : ish != null ? new Material(ish) : null;
        var oldRes = Resources.Load<Material>("MolakArt/InkOld"); var osh = Shader.Find("Molak/InkOld");
        oldMat = oldRes != null ? new Material(oldRes) : osh != null ? new Material(osh) : null;
        if (oldMat != null) { oldMat.SetTexture("_Reveal", revealSp.texture); oldSceneR.sharedMaterial = oldMat; oldCharR.sharedMaterial = oldMat; }
        if (inkMat != null && oldMat != null) inkR.sharedMaterial = inkMat; else InkOn = false;

        var sg = new GameObject("Scene"); sg.transform.SetParent(band.transform, false); sg.layer = 30;
        sceneR = sg.AddComponent<SpriteRenderer>(); sceneR.sharedMaterial = matScene; sceneR.sortingOrder = 10;
        sceneR.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

        var pg = new GameObject("Partner"); pg.transform.SetParent(band.transform, false); pg.layer = 30;
        partR = pg.AddComponent<SpriteRenderer>(); partR.sortingOrder = 11; partR.maskInteraction = SpriteMaskInteraction.VisibleInsideMask; partR.enabled = false;
        if (matPart == null && matCine != null && matCine != matScene) { matPart = new Material(matCine); matPart.SetFloat("_Quiet", 1f); }
        var cg = new GameObject("Char"); cg.transform.SetParent(band.transform, false); cg.layer = 30;
        charR = cg.AddComponent<SpriteRenderer>(); charR.sharedMaterial = mat; charR.sortingOrder = 11;
        charR.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

        // 0.4.82 그림 화면: 위아래로 먹이 스미는 막(위는 수치 줄, 아래는 선택지·글자리 자리)
        var shg = new GameObject("Shade"); shg.transform.SetParent(band.transform, false); shg.layer = 30;
        shadeR = shg.AddComponent<SpriteRenderer>(); shadeR.sharedMaterial = matScene; shadeR.sortingOrder = 16;
        shadeV = ShadeSprite(); shadeH = SideShadeSprite();
        shadeR.sprite = shadeV; shadeR.enabled = false;
        var kg = new GameObject("Key"); kg.transform.SetParent(band.transform, false); kg.layer = 30;
        keyR = kg.AddComponent<SpriteRenderer>(); keyR.sharedMaterial = matScene; keyR.sortingOrder = 18;
        keyR.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        var dg = new GameObject("Dip"); dg.transform.SetParent(band.transform, false); dg.layer = 30;
        dipR = dg.AddComponent<SpriteRenderer>(); dipR.sharedMaterial = matScene; dipR.sortingOrder = 19;
        dipR.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        dipR.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f); dipR.enabled = false;
        var kbg = new GameObject("KeyBg"); kbg.transform.SetParent(band.transform, false); kbg.layer = 30;
        keyBgR = kbg.AddComponent<SpriteRenderer>(); keyBgR.sharedMaterial = matScene; keyBgR.sortingOrder = 17;
        keyBgR.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        keyBgTex = new Texture2D(2, 1, TextureFormat.RGBA32, false); keyBgTex.filterMode = FilterMode.Bilinear; keyBgTex.wrapMode = TextureWrapMode.Clamp;
        keyBgTex.SetPixels32(new Color32[]{ new Color32(250,246,228,255), new Color32(250,246,228,255) }); keyBgTex.Apply();
        keyBgR.sprite = Sprite.Create(keyBgTex, new Rect(0, 0, 2, 1), new Vector2(0.5f, 0.5f), 100f); keyBgR.enabled = false;
        keyR.sortingOrder = 18;
    }

    /// <summary>0.4.85 번짐 가림판: 알파 = 1 - (가운데서 잰 거리 62% + 먹 결 38%). alphaCutoff 를 1→0 으로 내리면 가운데서 번져 나간다.</summary>
    static Sprite RevealSprite ()
    {
        // 시안 2 그대로: m = (가운데(0.55W, 0.5H)에서 잰 거리, 세로 1.3배)/R × 0.62 + 잡음 × 0.38. 잡음은 가로 8·16·32칸(0.5·0.3·0.2), 세로는 H/W×1.7.
        const int W = 384, H = 220; var tx = new Texture2D(W, H, TextureFormat.RGBA32, false); tx.filterMode = FilterMode.Bilinear; tx.wrapMode = TextureWrapMode.Clamp;
        var px = new Color32[W * H]; float cxp = W * 0.55f, cyp = H * 0.5f, R = Mathf.Sqrt(W * W + H * H) * 0.55f;
        var n = new float[W * H]; float mn = 9f, mx = -9f;
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
        {
            float v = 0f; float[] f = { 8f, 16f, 32f }, w = { 0.5f, 0.3f, 0.2f };
            for (int o = 0; o < 3; o++) v += w[o] * Mathf.PerlinNoise(x / (float)W * f[o] + 13.7f * o, y / (float)H * f[o] * H / W * 1.7f + 5.3f * o);
            n[y * W + x] = v; if (v < mn) mn = v; if (v > mx) mx = v;
        }
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
        {
            float d = Mathf.Sqrt((x - cxp) * (x - cxp) + ((y - cyp) * 1.3f) * ((y - cyp) * 1.3f)) / R;
            float m = d * 0.62f + (n[y * W + x] - mn) / (mx - mn) * 0.38f;
            byte a = (byte)Mathf.Clamp(Mathf.RoundToInt((1f - m) * 255f), 0, 255);
            px[y * W + x] = new Color32(255, 255, 255, a);
        }
        tx.SetPixels32(px); tx.Apply();
        return Sprite.Create(tx, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
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
        st.OnGameLoadFinished += _ => RestoreAfterLoad();   // 0.4.85 이어하기·불러오기 뒤 그림 되살리기(예전엔 MolakHud 의 정적 깃발에 걸려 있어 다시 걸리지 않는 때가 있었다)
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
        // 그림이 정해졌는데 아직 화면에 안 올렸다: 대본이 멈췄으면(선택지·정지) 지금, 글 없이 오래 머물면(1.5초, 탭 대기 제외) 그때 올린다
        if (dirty || soundPending)
        {
            var spc = Engine.Initialized ? Engine.GetService<IScriptPlayer>() : null; var trc = spc != null ? spc.MainTrack : null;
            bool stopped = trc == null || !trc.Playing;
            // 0.4.95 쪽을 붙잡는 동안(MolakFlow.Gating, 옛 쪽 글이 아직 읽히는 중)은 기다린다 — 문이 열릴 때 MolakFlow 가 올린다
            // 0.4.95 기준: 그림은 글과 함께. 글이 떠 있는 쪽에서는 글 없이 오래 머물러도(효과·기다림) 그림만 먼저 바꾸지 않는다 — 다음 글·선택지·정지에서 올린다.
            //   글이 없는 빈 쪽이면 1.5초 뒤에, 글이 있어도 8초를 넘기면(멈춘 듯 보이지 않게) 올린다.
            float idle = Time.unscaledTime - dirtyAt;
            bool awaiting = trc != null && trc.AwaitingInput;
            //   먹 화면·마디 덮개가 글을 가린 동안은 바로 올린다(걷히면 새 그림이 이미 서 있게).
            bool covered = MolakBlackout.Busy || MolakBlackout.Covering || MolakVfx.ChapterCover > 0.5f || MolakTurnCard.Showing;
            bool late = !awaiting && (covered || idle > 8f || idle > 1.5f && !MolakFlow.PageHasText);
            if (MolakPlayMode.SkipAll || !MolakFlow.Gating && (stopped || late)) DoCommit();
        }
        if (showScene != null && sceneR.sprite != showScene) sceneR.sprite = showScene;
        if (showChar != null && charR.sprite != showChar) charR.sprite = showChar;
        float sA = sceneR.sprite == null ? 0f : (showScene == null ? 1f - sceneT : Mathf.Lerp(FadeFrom, 1f, sceneT));
        // 그림은 한 번에 하나: 인물이 서 있으면 장면은 가린다(인물이 빠지면 다시 떠오른다)
        // 0.4.9: 인물이 서면 장면을 지우지 않고 옅게 깔아 둔다 — 인물 뒤에 그 자리의 풍경이 보인다
        bool cine = MolakTheme.CinemaNow, thr = MolakTheme.TheaterNow;
        sceneVis = Mathf.MoveTowards(sceneVis, showChar != null && !cine ? 0.7f : 1f, Time.deltaTime * 2.4f);   // 그림 화면은 인물 뒤 풍경도 온전히   // 인물이 불투명해졌으니 뒤 풍경은 덜 지운다
        sA *= sceneVis;
        float cA = charR.sprite == null ? 0f : (showChar == null ? 1f - charT : Mathf.Lerp(FadeFrom, 1f, charT));
        if (showScene == null && sceneT >= 1f) sceneR.sprite = null;
        if (showChar == null && charT >= 1f) charR.sprite = null;
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
        oldMask.transform.position = mask.transform.position; oldMask.transform.localScale = mask.transform.localScale;
        // 번짐: 1초 동안 alphaCutoff 1 → 0(가운데서 바깥으로). 끝나면 가림판은 띠 전체를 비춘다.
        inkT += Mathf.Min(Time.unscaledDeltaTime, 0.1f);   // 프레임이 늦어도 번짐이 느려지지 않게(0.05 로 묶었더니 20프레임 아래에선 늘어졌다)
        float ip = Mathf.Clamp01(inkT / InkDur);
        bool inking = inkT < InkDur && !hide;
        // 시안 2: 앞선 하나가 1.4초 동안 고르게 퍼진다(th −0.1 → 1.05). 앞선 안쪽은 새 그림, 끝에 먹, 바깥은 옛 그림.
        if (inking) { inkFrames++; if (inkFrames > 3) { InkMaxDt = Mathf.Max(InkMaxDt, Time.unscaledDeltaTime); InkSumDt += Time.unscaledDeltaTime; InkN++; } }
        float cut = inking ? 1f - Mathf.Lerp(0.02f, 1.05f, ip) : -1f;   // 시안은 −0.1 에서 시작해 처음 0.1초 남짓 아무것도 안 바뀌었다 — 누르자마자 번지기 시작하게
        if (oldMat != null) { oldMat.SetFloat("_Cut2", cut); oldMat.SetVector("_Band", new Vector4(cx - bandW * 0.5f, bandCy - bandH * 0.5f, bandW, bandH)); }
#if UNITY_EDITOR
        if (CapArm && capUntil > 0f && inkT < InkDur + 0.1f && capN < 80) { System.IO.Directory.CreateDirectory("Build/cap"); ScreenCapture.CaptureScreenshot("Build/cap/f" + (capN++).ToString("000") + "_" + inkT.ToString("0.00") + ".png"); }
#endif
        float rsx = bandW / (revealSp.texture.width / 100f), rsy = bandH / (revealSp.texture.height / 100f);
        oldSceneR.enabled = inking && oldSceneR.sprite != null; oldCharR.enabled = inking && oldCharR.sprite != null;
        if (!inking) { oldSceneR.sprite = null; oldCharR.sprite = null; }
        inkR.enabled = inking && inkMat != null;
        if (inkR.enabled)
        {
            inkMat.SetFloat("_Cut", cut); inkMat.SetFloat("_W", 0.035f); inkMat.SetFloat("_Halo", 0.05f);
            inkR.color = new Color(1f, 1f, 1f, colT);
            inkR.transform.position = new Vector3(cx, bandCy, z - 0.05f); inkR.transform.localScale = new Vector3(rsx, rsy, 1f);
        }

        // 처음 몇 프레임은 번짐 셰이더를 보이지 않게 한 번 그려 둔다(첫 전환 때 셰이더를 준비하느라 한 박자 멈추던 것)
        if (inkWarm > 0 && !inking)
        {
            inkWarm--;
            inkR.enabled = inkMat != null; inkR.color = new Color(1f, 1f, 1f, 0f); inkR.transform.position = new Vector3(cx, bandCy, z - 0.05f);
            if (oldMat != null) { oldSceneR.sprite = revealSp; oldSceneR.enabled = true; oldSceneR.color = new Color(1f, 1f, 1f, 0f); oldSceneR.transform.position = new Vector3(cx, bandCy, z); }
            if (inkWarm == 0) { inkR.enabled = false; oldSceneR.enabled = false; oldSceneR.sprite = null; }
        }
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
            k *= CharZoom(showCharId);   // 전신 그림은 띠 안에서 너무 작다 — 상반신 쪽으로 당겨 본다
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
            // 새 사람: 오른쪽에서 미끄러져 들어온다(끝에서 부드럽게 선다).
            // 0.5.5 (언메이커: 들어올 때 프레임이 끊겨 보인다) 거리 0.55→0.30 화면 폭, 0.45→0.5초, 첫 출발을 덜 급하게(세제곱 → 2.2제곱),
            //   그림을 처음 올리는 프레임이 늦어도 한 번에 건너뛰지 않게 한 프레임에 1/40초까지만 나아간다.
            slideT = Mathf.Min(1f, slideT + SlideStep());
            if (slideT < 1f) x += SlideOff(slideT) * camW;
            float y = cine ? top - camH*0.105f - sh*k*0.5f : thr ? bandCy + bandH*0.5f - 28f*unit - sh*k*0.5f : bandCy + bandH*0.5f - sh*k*0.5f + 16f*unit;
            bool duo = (cine || thr) && partR != null && showPart != null;
            if (duo) x += (Person(showCharId) == leftPerson ? -1f : 1f) * camW * 0.21f;   // 0.4.88 두 사람: 제 자리(왼/오)에 선다
            else if (thr) x += Mathf.Min(sw * k * 0.12f, Mathf.Max(0f, (camW - sw * k) * 0.5f));   // 시안처럼 조금 오른쪽 — 넓은 그림은 덜
            charR.transform.position = new Vector3(x, y + gy, z);
        }
        // 0.4.88 곁에서 듣는 사람: 말하는 사람보다 조금 작고 낮게, 뒤에, 어둡게. 처음 설 때는 제 쪽 화면 밖에서 미끄러져 들어온다.
        if (partR != null)
        {
            bool duo2 = (cine || thr) && showPart != null && charR.sprite != null && !hide;
            partR.enabled = duo2;
            if (duo2)
            {
                if (partR.sprite != showPart) partR.sprite = showPart;
                var pm = matPart != null ? matPart : wantMat; if (partR.sharedMaterial != pm) partR.sharedMaterial = pm;
                partT = Mathf.Min(1f, partT + fdt);
                partSlideT = Mathf.Min(1f, partSlideT + SlideStep());
                var psp = partR.sprite; float psw = psp.texture.width / 100f, psh = psp.texture.height / 100f;
                float pk = cine ? Mathf.Min(camH * 0.62f / psh, camW * 1.05f / psw) : Mathf.Min(bandH * 1.27f / psh, camW * 1.05f / psw);
                pk *= 0.92f * (1f + 0.006f * Mathf.Sin(Time.time * 1.15f + 1.7f));
                if (thr) pk = Mathf.Max(pk, (bandH - 20f * unit) / psh);   // 아랫변이 창 안으로 올라오지 않게
                partR.transform.localScale = new Vector3(pk, pk, 1f);
                float side = Person(showPartId) == leftPerson ? -1f : 1f;
                float ppx = cx + side * camW * 0.21f;
                if (partSlideT < 1f) ppx += side * SlideOff(partSlideT) * camW;
                float ppy = cine ? top - camH * 0.105f - psh * pk * 0.5f - 30f * unit : bandCy + bandH * 0.5f - 58f * unit - psh * pk * 0.5f;
                partR.transform.position = new Vector3(ppx, ppy, z + 0.02f);
                float pa = Mathf.Lerp(FadeFrom, 1f, partT) * colT;
                partR.color = new Color(0.82f, 0.82f, 0.82f, pa);
            }
        }
        // 장면 전환 암전: 바로 먹으로 덮고 0.2초 머문 뒤 0.6초에 걷히며 새 장면이 떠오른다
        if (dipAt >= 0 && Time.frameCount - dipAt >= 6) { dipAt = -1; if (inkT >= InkDur && !dirty) dipT = 0f; }
        dipT += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
        float dipA = dipT < 0.2f ? 1f : 1f - Mathf.SmoothStep(0f, 1f, (dipT - 0.2f) / 0.6f);
        dipR.enabled = !hide && dipT < 0.8f && inkT >= InkDur && (sceneR.sprite != null || charR.sprite != null);
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
