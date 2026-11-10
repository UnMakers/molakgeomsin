using System.Collections.Generic;
using Naninovel;
using UnityEngine;

/// <summary>
/// 삽화 시스템 — 종이 카메라(레이어 30) 위에 장면 삽화와 인물 입상을 먹(곱셈 블렌드)으로 얹는다.
/// 상단 띠(캔버스 900×1600 기준 y 110~430)에 장면, 그 오른쪽에 인물 반신.
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
        {"valley","valley"},{"rain","rain"},{"market","market"},{"crossroad","crossroad"},{"roofmoon","roofmoon"},{"duel","duel"},
    };

    SpriteRenderer sceneR, charR;
    SpriteMask mask;
    Sprite sceneCur, charCur;
    float sceneA, charA, sceneT = 1f, charT = 1f, colT = 1f;
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
        if (inst.sceneId == key) return;
        bool isDuel = key.StartsWith("duel");   // duel, duelbridge … 비무 판의 변형
        if (isDuel) { if (!inst.sceneId.StartsWith("duel")) { prevScene = inst.sceneId; MolakSfx.Draw(); } } else prevScene = null;
        if (!isDuel) MolakFlow.Break();
        var sp = Load("MolakArt/scene_" + key);
        inst.SetScene(sp, key);
        MolakBgm.Mood(isDuel ? "duel" : (key == "raid" || key == "bloodroof") ? "raid" : "day");
    }

    public static void Char (string id)
    {
        if (inst == null) return;
        if (string.IsNullOrEmpty(id) || id == "none" || id == "off") { inst.SetChar(null, ""); return; }
        if (inst.charId == id) return;
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

    public static void Clear () { if (inst == null) return; inst.SetScene(null, ""); inst.SetChar(null, ""); }

    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
    static Sprite Load (string path)
    {
        Sprite s;
        if (cache.TryGetValue(path, out s)) return s;
        var tex = Resources.Load<Texture2D>(path);
        if (tex == null) { Debug.LogWarning("[MolakArt] missing " + path); cache[path] = null; return null; }
        s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        cache[path] = s;
        return s;
    }

    void SetScene (Sprite s, string id) { sceneCur = s; sceneId = id; sceneT = 0f; }
    void SetChar (Sprite s, string id) { charCur = s; charId = id; charT = 0f; }

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
        float cA = charR.sprite == null ? 0f : (charCur == null ? 1f - charT : Mathf.Clamp01(charT));
        if (sceneCur == null && sceneT >= 1f) sceneR.sprite = null;
        if (charCur == null && charT >= 1f) charR.sprite = null;
        var wantMat = MolakTheme.Night ? matAlpha : mat;
        if (charR.sharedMaterial != wantMat) charR.sharedMaterial = wantMat;
        bool hide = MolakMenu.IsOpen;
        sceneR.enabled = !hide; charR.enabled = !hide;
        colT = Mathf.MoveTowards(colT, Collapsed ? 0f : 1f, Time.deltaTime*4f);
        // 야간(먹색) 모드: 종이색 장면 패널이 하얗게 떠 보이지 않도록 어둡게 눌러 그린다
        float sg = MolakTheme.Night ? 0.62f : 1f;
        sceneR.color = new Color(sg, sg, sg, sA*(MolakTheme.Night ? 0.80f : 0.88f)*colT);
        charR.color = new Color(1f, 1f, 1f, cA*0.95f*colT);

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

        bool both = sceneR.sprite != null && charR.sprite != null;
        float sceneW = both ? bandW*0.62f : bandW;
        float sceneCx = both ? cx - bandW*0.5f + sceneW*0.5f : cx;
        if (sceneR.sprite != null)
        {
            var sp = sceneR.sprite; float sw = sp.texture.width/100f, sh = sp.texture.height/100f;
            float k = Mathf.Max(sceneW/sw, bandH/sh);
            sceneR.transform.localScale = new Vector3(k, k, 1f);
            sceneR.transform.position = new Vector3(sceneCx, bandCy, z + 0.1f);
            // 장면 마스크 폭에 맞춰 잘라내기: 마스크는 띠 전체이므로 좌측 정렬된 장면이 우측 인물 영역을 침범하지 않도록 스케일 보정
            if (both) { float kk = Mathf.Max(sceneW/sw, bandH/sh); sceneR.transform.localScale = new Vector3(kk, kk, 1f); }
        }
        if (charR.sprite != null)
        {
            var sp = charR.sprite; float sw = sp.texture.width/100f, sh = sp.texture.height/100f;
            float k = (bandH*1.75f)/sh;
            charR.transform.localScale = new Vector3(k, k, 1f);
            float x = cx + bandW*0.5f - sw*k*0.5f + 30f*unit;
            float y = bandCy + bandH*0.5f - sh*k*0.5f + 16f*unit;
            charR.transform.position = new Vector3(x, y, z);
        }
    }
}
