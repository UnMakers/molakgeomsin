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
    public const float BandTop = 184f, BandH = 300f;   // 캔버스 단위(900×1600)

    static MolakArt inst;
    static Material mat, matAlpha, matScene;
    static readonly Dictionary<string, string> SceneAlias = new Dictionary<string, string>
    {
        {"day","morning"},{"morning","morning"},{"night","night"},{"shrine","shrine"},{"blood","blood"},
        {"valley","valley"},{"rain","rain"},{"market","market"},{"crossroad","crossroad"},{"roofmoon","roofmoon"},{"roof","roofmoon"},{"duel","duel"},
    };

    SpriteRenderer sceneR, charR;
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
        if (isDuel) { if (!inst.sceneId.StartsWith("duel")) { prevScene = inst.sceneId; MolakSfx.Draw(); } } else prevScene = null;
        if (!isDuel) MolakFlow.Break();
        var sp = Load("MolakArt/scene_" + key);
        inst.SetScene(sp, key);
        MolakBgm.Mood(isDuel ? "duel" : (key == "raid" || key == "bloodroof") ? "raid" : "day");
    }

    // 0.4.9: 인물이 나오면 그 사람이 있을 법한 자리를 뒤에 깐다(지금 장면이 하루의 기본 풍경일 때만 — 대본이 고른 장면은 그대로 둔다)
    static readonly HashSet<string> Generic = new HashSet<string> { "", "morning", "night", "rain" };
    static readonly Dictionary<string, string> HomeDay = new Dictionary<string, string>
    {
        {"granny","herbshop"},{"hajinmu","innhall"},{"gwaksan","market"},{"yonggae","crossroad"},{"amhyang","teahouse"},
        {"soa","innhall"},{"soa_smile","market"},{"soa_grief","rain"},{"soa_fierce","alley"},{"hochi","alley"},{"kyona","valley"},
        {"jinbaek","gate"},{"lawlord","gate"},{"oldman","night"},{"ochi","valley"},{"cheongmyo","temple"},
        {"cheongmyo_robe","temple"},{"amhyang_pain","teahouse"},{"amhyang_soft","ferry"},{"jang","alley"},
    };
    static readonly Dictionary<string, string> HomeNight = new Dictionary<string, string>
    {
        {"granny","herbshop"},{"hajinmu","innhall"},{"gwaksan","alley"},{"yonggae","alley"},{"amhyang","teahouse"},
        {"soa","roofmoon"},{"soa_smile","roofmoon"},{"soa_grief","rain"},{"soa_fierce","alley"},{"hochi","alley"},
        {"jinbaek","alley"},{"lawlord","gate"},{"oldman","night"},{"jang","alley"},
        {"amhyang_pain","teahouse"},{"amhyang_soft","ferry"},
    };
    public static bool IsNight;
    static readonly Dictionary<string, string> SceneWeather = new Dictionary<string, string>
    {
        {"rain","rain"},{"flood","rain"},{"snow","snow"},{"manorfire","inferno"},{"herbfire","embers"},{"bloodroof","bloodrain"},{"raid","bloodrain"},
    };

    public static void Char (string id)
    {
        if (inst == null) return;
        if (string.IsNullOrEmpty(id) || id == "none" || id == "off") { inst.SetChar(null, ""); return; }
        if (inst.charId == id) return;
        if (Generic.Contains(inst.sceneId) && !inst.sceneId.StartsWith("duel"))
        {
            string home;
            if ((IsNight ? HomeNight : HomeDay).TryGetValue(id, out home) && home != inst.sceneId)
            {
                var hs = Load("MolakArt/scene_" + home);
                if (hs != null) { inst.sceneCur = hs; inst.sceneId = home; inst.sceneT = 0f; }
            }
            else if (inst.sceneCur == null)
            {
                var fs = Load("MolakArt/scene_" + (IsNight ? "night" : "morning"));
                if (fs != null) { inst.sceneCur = fs; inst.sceneId = IsNight ? "night" : "morning"; inst.sceneT = 0f; }
            }
        }
        var sp = Load("MolakArt/char_" + id);
        inst.SetChar(sp, id);
    }

    static string prevScene;
    /// <summary>결투 장면 종료 후 직전 장면으로 복귀</summary>
    public static void EndDuel ()
    {
        if (inst == null || !inst.sceneId.StartsWith("duel")) return;
        if (string.IsNullOrEmpty(prevScene)) inst.SetScene(null, ""); else { var sp = Load("MolakArt/scene_" + prevScene); inst.SetScene(sp, prevScene); }
        MolakBgm.Mood(prevScene == "raid" || prevScene == "bloodroof" ? "raid" : "day");
        prevScene = null;
    }

    /// <summary>0.4.41: 그림이 바뀌는 중인가(새 장면·인물이 아직 다 떠오르지 않았다). @art 가 이게 끝나길 잠깐 기다린 뒤 글로 넘어간다.</summary>
    public static bool Settling { get { return inst != null && (inst.sceneT < 0.8f || inst.charT < 0.8f); } }

    public static void Clear () { if (inst == null) return; inst.SetScene(null, ""); inst.SetChar(null, ""); }

    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
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

    void SetScene (Sprite s, string id) { sceneCur = s; sceneId = id; sceneT = 0f; if (s != null && charCur != null) SetChar(null, ""); }
    void SetChar (Sprite s, string id) { if (id != charId) pushT = 0f; charCur = s; charId = id; charT = 0f; }
    float pushT = 1f, hurtT = -1f;
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
        var cam = PaperCam();
        if (cam == null) return;

        // 페이드
        sceneT = Mathf.Min(1f, sceneT + Time.deltaTime*1.6f);
        charT = Mathf.Min(1f, charT + Time.deltaTime*1.6f);
        if (sceneR.sprite != sceneCur && (sceneR.sprite == null || sceneT >= 0.5f || sceneCur == null)) sceneR.sprite = sceneCur;
        if (charR.sprite != charCur && (charR.sprite == null || charT >= 0.5f || charCur == null)) charR.sprite = charCur;
        float sA = sceneR.sprite == null ? 0f : (sceneCur == null ? 1f - sceneT : Mathf.Clamp01(sceneT));
        // 그림은 한 번에 하나: 인물이 서 있으면 장면은 가린다(인물이 빠지면 다시 떠오른다)
        // 0.4.9: 인물이 서면 장면을 지우지 않고 옅게 깔아 둔다 — 인물 뒤에 그 자리의 풍경이 보인다
        sceneVis = Mathf.MoveTowards(sceneVis, charCur != null ? 0.7f : 1f, Time.deltaTime * 2.4f);   // 인물이 불투명해졌으니 뒤 풍경은 덜 지운다
        sA *= sceneVis;
        float cA = charR.sprite == null ? 0f : (charCur == null ? 1f - charT : Mathf.Clamp01(charT));
        if (sceneCur == null && sceneT >= 1f) sceneR.sprite = null;
        if (charCur == null && charT >= 1f) charR.sprite = null;
        // 0.4.28: 인물은 곱하기가 아니라 보통 알파로 — 곱하기면 얼굴·흰 옷 같은 밝은 데로 뒤 배경이 비쳤다(인물 그림도 안쪽을 채워 다시 오렸다)
        var wantMat = MolakTheme.Night ? matAlpha : matScene;
        if (charR.sharedMaterial != wantMat) charR.sharedMaterial = wantMat;
        bool hide = MolakMenu.IsOpen;
        sceneR.enabled = !hide; charR.enabled = !hide;
        colT = Mathf.MoveTowards(colT, Collapsed ? 0f : 1f, Time.deltaTime*4f);
        // 야간(먹색) 모드: 종이색 장면 패널이 하얗게 떠 보이지 않도록 어둡게 눌러 그린다
        float sg = MolakTheme.Night ? 0.62f : 1f;
        sceneR.color = new Color(sg, sg, sg, sA*(MolakTheme.Night ? 0.80f : 0.88f)*colT);
        charR.color = new Color(1f, 1f, 1f, cA*colT);

        // 배치: 카메라 뷰 기준 캔버스(900×1600) 좌표 → 월드
        float camH = cam.orthographic ? cam.orthographicSize*2f : 20f;
        float camW = camH*cam.aspect;
        float unit = Mathf.Min(camW/900f, camH/1600f);   // 캔버스 1단위의 월드 크기 (CanvasScaler Expand와 동일)
        float cx = cam.transform.position.x, top = cam.transform.position.y + camH*0.5f;
        float z = cam.transform.position.z + 3.5f;
        float bandW = Mathf.Min(camW, 900f*unit);
        float bandH = BandH*unit;
        float bandCy = top - (BandTop + BandH*0.5f)*unit;

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
            float k = (bandH*1.75f)/sh;
            // 인물은 등장할 때 한 걸음 다가오고(0.7초), 그 뒤로는 숨을 쉰다
            pushT = Mathf.Min(1f, pushT + Time.deltaTime / 0.7f);
            float ease = 1f - Mathf.Pow(1f - pushT, 3f);
            k *= Mathf.Lerp(0.93f, 1f, ease) * (1f + 0.006f * Mathf.Sin(Time.time * 1.15f));
            charR.transform.localScale = new Vector3(k, k, 1f);
            float x = cx + jit;   // 홀로 서니 띠 가운데
            float y = bandCy + bandH*0.5f - sh*k*0.5f + 16f*unit;
            charR.transform.position = new Vector3(x, y, z);
        }
    }
}
