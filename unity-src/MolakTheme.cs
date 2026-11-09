using System.Collections;
using Naninovel;
using UnityEngine;
using UnityEngine.UI;

/// <summary>먹(墨) 테마 — 카메라 배경·프린터·선택지를 수묵 톤으로 틴트.</summary>
public class MolakTheme : MonoBehaviour
{
    // ---- 표시 설정 (영구) ----
    public static bool Night { get { return PlayerPrefs.GetInt("molak_night", 0) == 1; } set { PlayerPrefs.SetInt("molak_night", value ? 1 : 0); } }
    // 글자 크기: -2(80%) ~ +4(140%), 10% 단위. 예전 3단계 설정(molak_font 0/1/2)은 -1/0/+1 로 이관.
    public const int FontMin = -2, FontMax = 4;
    public static int FontStep
    {
        get
        {
            if (!PlayerPrefs.HasKey("molak_font2")) { int old = PlayerPrefs.GetInt("molak_font", 1); PlayerPrefs.SetInt("molak_font2", old - 1); }
            return Mathf.Clamp(PlayerPrefs.GetInt("molak_font2", 0), FontMin, FontMax);
        }
        set { PlayerPrefs.SetInt("molak_font2", Mathf.Clamp(value, FontMin, FontMax)); PlayerPrefs.Save(); }
    }
    public static float FontScale { get { return 1f + 0.1f * FontStep; } }
    public static Color InkText { get { return Night ? new Color(0.90f, 0.86f, 0.78f, 1f) : new Color(0.16f, 0.13f, 0.10f, 1f); } }
    public static Color InkMid { get { return Night ? new Color(0.66f, 0.62f, 0.55f, 1f) : new Color(0.44f, 0.38f, 0.32f, 1f); } }
    public static Color CardFace { get { return Night ? new Color(0.17f, 0.15f, 0.13f, 1f) : new Color(0.995f, 0.98f, 0.95f, 1f); } }
    static readonly System.Collections.Generic.List<Naninovel.Choice> choiceBuf = new System.Collections.Generic.List<Naninovel.Choice>();
    static readonly System.Collections.Generic.HashSet<int> sfxHooked = new System.Collections.Generic.HashSet<int>();

    /// <summary>화면 비율 무관 UI 스케일: 1080×1920 기준 폭·높이 중 작은 쪽</summary>
    public static int ChoiceCount;
    public static float U { get { return Mathf.Min(Screen.width/1080f, Screen.height/1920f); } }
    public static readonly Color Bg = Hex("#100D0C");
    public static readonly Color Paper = Hex("#191411");
    public static readonly Color Raise = Hex("#221B17");
    public static readonly Color Line = Hex("#453831");
    public static readonly Color Ink = Hex("#F2E8D4");
    public static readonly Color Ink3 = Hex("#8A7D67");
    public static readonly Color Blood = Hex("#C03434");
    public static readonly Color Gold = Hex("#C9A227");
    public static readonly Color Curse = Hex("#9A70C4");

    public static Color Hex (string h){Color c;ColorUtility.TryParseHtmlString(h,out c);return c;}

    static MolakTheme instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot ()
    {
        if (instance != null) return;
        var go = new GameObject("MolakTheme");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<MolakTheme>();
    }

    IEnumerator Start ()
    {
        while (!Engine.Initialized) yield return null;
        var cam = PaperCam();
        if (cam != null) { cam.backgroundColor = Bg; cam.clearFlags = CameraClearFlags.SolidColor; }
    }

    Transform backdrop;

    void EnsureBackdrop ()
    {
        // 기본 플레이스홀더(보라 타일) 숨김
        var ph = GameObject.Find("MainBackgroundPlaceholder");
        if (ph != null && ph.activeSelf) ph.SetActive(false);
        if (backdrop != null) return;
        var tex = Resources.Load<Texture2D>("MolakInkBg");
        if (tex == null) return;
        var cam = PaperCam();
        if (cam == null) return;
        var go = new GameObject("MolakBackdrop");
        DontDestroyOnLoad(go);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.material = new Material(Shader.Find("Sprites/Default"));
        sr.sprite = Sprite.Create(tex, new Rect(0,0,tex.width,tex.height), new Vector2(0.5f,0.5f), 100f);
        sr.color = new Color(0.97f,0.95f,0.92f,1f); // 종이 원색
        sr.sortingOrder = -1000;
        float camH = cam.orthographic ? cam.orthographicSize*2f : 20f;
        float camW = camH*cam.aspect;
        go.transform.position = new Vector3(0f, 0f, cam.transform.position.z+4f);
        backdrop = go.transform;
        // 백지 베일 — 산수를 수채처럼 가라앉혀 글 가독성 확보
        var veil = new GameObject("MolakPaperVeil");
        DontDestroyOnLoad(veil);
        var vsr = veil.AddComponent<SpriteRenderer>();
        vsr.material = new Material(Shader.Find("Sprites/Default"));
        var vt = new Texture2D(1,1); vt.SetPixel(0,0,Color.white); vt.Apply();
        vsr.sprite = Sprite.Create(vt, new Rect(0,0,1,1), new Vector2(0.5f,0.5f), 1f);
        vsr.color = Night ? new Color(0.07f, 0.065f, 0.06f, 0.97f) : new Color(0.975f, 0.955f, 0.92f, 0.52f);
        vsr.sortingOrder = -999;
        veil.layer = 30;
        veil.transform.SetParent(go.transform, false);
        veil.transform.localPosition = new Vector3(0f, 0f, -0.5f);
        veil.transform.localScale = new Vector3(2000f, 2000f, 1f);
    }

    Camera PaperCam ()
    {
        foreach (var c in FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (c.CompareTag("MainCamera") && c.depth < 0) return c;
        return Camera.main;
    }

    void FitBackdrop ()
    {
        if (backdrop == null) return;
        var cam = PaperCam();
        var sr = backdrop.GetComponent<SpriteRenderer>();
        if (cam == null || sr == null || sr.sprite == null) return;
        // 종이 전용 카메라 구성: 레이어 30만, 가장 먼저 렌더
        if (!cam.enabled) cam.enabled = true;
        if (cam.cullingMask != (1 << 30)) cam.cullingMask = 1 << 30;
        if (cam.clearFlags != CameraClearFlags.SolidColor) cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Bg;
        if (backdrop.gameObject.layer != 30) backdrop.gameObject.layer = 30;
        var veilT = backdrop.Find("MolakPaperVeil");
        if (veilT != null) { var vr = veilT.GetComponent<SpriteRenderer>(); var vc = Night ? new Color(0.07f, 0.065f, 0.06f, 0.97f) : new Color(0.975f, 0.955f, 0.92f, 0.52f); if (vr != null && vr.color != vc) vr.color = vc; }
        // 노벨 카메라에선 종이 레이어 제외 (이중상 방지)
        foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            if (c != cam && (c.cullingMask & (1 << 30)) != 0) c.cullingMask &= ~(1 << 30);
        float camH = cam.orthographic ? cam.orthographicSize*2f : 20f;
        float camW = camH*cam.aspect;
        float sw = sr.sprite.texture.width/100f, sh = sr.sprite.texture.height/100f;
        var want = new Vector3(camW/sw, camH/sh, 1f);
        if ((backdrop.localScale - want).sqrMagnitude > 0.0001f) backdrop.localScale = want;
        var wantPos = new Vector3(cam.transform.position.x, cam.transform.position.y, cam.transform.position.z+4f);
        if ((backdrop.position - wantPos).sqrMagnitude > 0.0001f) backdrop.position = wantPos;
    }

    static Sprite cardSprite;
    static TMPro.TMP_FontAsset gowunTMP;

    static TMPro.TMP_FontAsset GowunTMP ()
    {
        if (gowunTMP != null) return gowunTMP;
        gowunTMP = TMPro.TMP_Settings.defaultFontAsset;
        if (gowunTMP == null || !gowunTMP.name.Contains("Gowun"))
        {
            var fb = TMPro.TMP_Settings.fallbackFontAssets;
            if (fb != null) foreach (var f in fb) if (f != null && f.name.Contains("Gowun")) { gowunTMP = f; break; }
        }
        return gowunTMP;
    }

    static void EnsureCardSprite ()
    {
        if (cardSprite != null) return;
        int S = 48, B = 4; // 테두리 두께 4px
        var borderCol = new Color(0.40f, 0.33f, 0.26f, 1f);
        var tx = new Texture2D(S, S, TextureFormat.RGBA32, false);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
                tx.SetPixel(x, y, (x < B || y < B || x >= S-B || y >= S-B) ? borderCol : Color.white);
        tx.Apply();
        cardSprite = Sprite.Create(tx, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(8, 8, 8, 8));
    }

    void LateUpdate ()
    {
        if (!Engine.Initialized) return;
        // 모든 TMP 텍스트를 고운바탕(주 폰트)으로 — Inter 폴백 경로의 투명 렌더 버그 회피
        var gf = GowunTMP();
        if (gf != null)
            foreach (var tx in FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (tx.font != gf) tx.font = gf;
        EnsureBackdrop();
        FitBackdrop();
        // 본문 상자: 화면 가운데, 크게
        // 본문 창(Printer)의 위치·마스크·스크롤은 MolakScroll 이 담당
        // 선택지: 하단 고정, 폭 확대
        var bl = GameObject.Find("Naninovel<Runtime>/UI/ChoiceHandler/ButtonList");
        if (bl != null)
        {
            var cp = bl.transform.Find("ContentPanel") as RectTransform;
            if (cp != null && (cp.anchorMax.y != 0f || cp.pivot.y != 0f))
            {
                cp.anchorMin = new Vector2(0.5f, 0f);
                cp.anchorMax = new Vector2(0.5f, 0f);
                cp.pivot = new Vector2(0.5f, 0f);
                cp.anchoredPosition = new Vector2(0f, 150f);
                cp.sizeDelta = new Vector2(850f, cp.sizeDelta.y);
            }
        }
        // 모바일 세로 기준 UI 스케일 (폭 기준 매칭 — 폰에서 글자·버튼이 작아지지 않게)
        foreach (var cs in FindObjectsByType<UnityEngine.UI.CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (cs.uiScaleMode != UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize) continue;
            var want = new Vector2(900f, 1600f);
            if (cs.referenceResolution != want || cs.screenMatchMode != UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand)
            {
                cs.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand;
                cs.referenceResolution = want;
                cs.matchWidthOrHeight = 0f;
            }
        }
        // 선택지: 개별 종이 카드(테두리) + 눌림 효과
        EnsureCardSprite();
        int choiceCount = 0;
        {
            var chm = Naninovel.Engine.GetService<Naninovel.IChoiceHandlerManager>();
            if (chm != null && !string.IsNullOrEmpty(chm.DefaultHandlerId) && chm.ActorExists(chm.DefaultHandlerId))
            {
                var act = chm.GetActor(chm.DefaultHandlerId);
                choiceBuf.Clear(); act.CollectChoices(choiceBuf);
                choiceCount = act.Visible ? choiceBuf.Count : 0;
            }
        }
        MolakArt.Collapsed = choiceCount >= 3;
        ChoiceCount = choiceCount;   // 선택지가 떠 있으면 MolakScroll 이 본문 창 아래 한계를 선택지 위로 올린다
        foreach (var btn in FindObjectsByType<Naninovel.UI.ChoiceHandlerButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            var rootImg = btn.GetComponent<Image>();
            if (rootImg != null)
            {
                if (rootImg.sprite != cardSprite) { rootImg.sprite = cardSprite; rootImg.type = Image.Type.Sliced; }
                var want = CardFace;
                if (rootImg.color != want) rootImg.color = want;
            }
            foreach (var img in btn.GetComponentsInChildren<Image>(true))
                if (img != rootImg && img.color.a > 0.001f) img.color = new Color(0,0,0,0);
            var ub = btn.GetComponent<UnityEngine.UI.Button>();
            if (ub != null && (ub.transition != UnityEngine.UI.Selectable.Transition.ColorTint || ub.targetGraphic != rootImg))
            {
                ub.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
                ub.targetGraphic = rootImg;
                var cb = ub.colors;
                cb.normalColor = Color.white;
                cb.highlightedColor = Night ? new Color(1.25f, 1.22f, 1.18f, 1f) : new Color(0.93f, 0.90f, 0.84f, 1f);
                cb.pressedColor = Night ? new Color(1.7f, 1.6f, 1.45f, 1f) : new Color(0.76f, 0.71f, 0.63f, 1f); // 또렷한 눌림
                cb.selectedColor = Color.white;
                cb.colorMultiplier = 1f;
                cb.fadeDuration = 0.06f;
                ub.colors = cb;
            }
            var le = btn.GetComponent<UnityEngine.UI.LayoutElement>();
            if (le == null) le = btn.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            float wantH = choiceCount >= 4 ? 122f : 148f;
            if (Mathf.Abs(le.minHeight - wantH) > 0.5f) { le.minHeight = wantH; le.minWidth = 860f; }
            var inkDark = InkText;
            if (ub != null && !sfxHooked.Contains(ub.GetInstanceID())) { sfxHooked.Add(ub.GetInstanceID()); ub.onClick.AddListener(() => { MolakSfx.Tap(); }); }
            foreach (var tx in btn.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                if (tx.color != inkDark) tx.color = inkDark;
                float cfs = Mathf.Round(40f*FontScale);
                if (Mathf.Abs(tx.fontSize - cfs) > 0.5f) tx.fontSize = cfs;
                if (!tx.enableWordWrapping) tx.enableWordWrapping = true;
                if (tx.overflowMode != TMPro.TextOverflowModes.Overflow) tx.overflowMode = TMPro.TextOverflowModes.Overflow;
                float need = tx.preferredHeight + 44f;
                if (need > le.minHeight + 1f) le.minHeight = need;
            }
        }
        // 선택지 리스트: 흰 패널 배경 제거(뭉탱이 원인) + 카드 간격
        var blGo = GameObject.Find("Naninovel<Runtime>/UI/ChoiceHandler/ButtonList");
        if (blGo != null)
        {
            foreach (var img in blGo.GetComponentsInChildren<Image>(true))
            {
                if (img.GetComponent<Naninovel.UI.ChoiceHandlerButton>() != null) continue;
                if (img.GetComponentInParent<Naninovel.UI.ChoiceHandlerButton>() != null) continue;
                var mk = img.GetComponent<UnityEngine.UI.Mask>();
                if (mk != null) { if (mk.showMaskGraphic) mk.showMaskGraphic = false; }
                else if (img.color.a > 0.001f) img.color = new Color(0,0,0,0);
            }
            foreach (var vlg in blGo.GetComponentsInChildren<UnityEngine.UI.VerticalLayoutGroup>(true))
                if (vlg.spacing < 28f) vlg.spacing = 26f;
        }
        // 프린터 하단 컨트롤 바 숨김 (모바일)
        foreach (var panel in FindObjectsByType<Naninovel.UI.UITextPrinterPanel>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            var ctrl = panel.transform.Find("Content/ControlPanel") ?? panel.transform.Find("ControlPanel");
            if (ctrl == null)
                foreach (Transform tr in panel.GetComponentsInChildren<Transform>(true))
                    { if (tr.name == "ControlPanel") { ctrl = tr; break; } }
            if (ctrl != null && ctrl.gameObject.activeSelf) ctrl.gameObject.SetActive(false);
        }
        // 프린터 패널 틴트 (배경 이미지)
        foreach (var panel in FindObjectsByType<Naninovel.UI.UITextPrinterPanel>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            foreach (var img in panel.GetComponentsInChildren<Image>(true))
                if (img.color.a > 0.001f) img.color = new Color(0f,0f,0f,0f); // 상자 없음 — 종이에 바로 쓴다
            var inkTxt = InkText;
            float bfs = Mathf.Round(36f*FontScale);
            foreach (var tx in panel.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                if (tx.color != inkTxt) tx.color = inkTxt;
                if (tx.gameObject.name == "DialogueText" && Mathf.Abs(tx.fontSize - bfs) > 0.5f) tx.fontSize = bfs;
            }
        }
    }
}
