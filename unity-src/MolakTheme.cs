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
    /// <summary>선택지가 뜬 뒤 눌리지 않는 시간(초).</summary>
    public const float ChoiceLock = 0.6f;
    static float choiceShownAt = -9f; static int lastChoiceCount;
    /// <summary>선택지 카드 높이·간격 — 선택지가 여섯 개 이상(회귀 상점 등)이면 촘촘하게 접어 화면 위로 넘치지 않게.</summary>
    public static bool Compact (int n) { return n >= 6; }
    public static float CardH (int n) { return n >= 9 ? 64f : n >= 6 ? 84f : n >= 4 ? 122f : 148f; }
    public static float CardGap (int n) { return n >= 9 ? 8f : n >= 6 ? 12f : 26f; }
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

    static Sprite cardSpriteDay, cardSpriteNight, sealSprite;
    static bool builtChoiceNight;
    /// <summary>선택지 카드 9-슬라이스: 바탕 + 바깥 먹선(2px) + 안쪽 가는 선(1px), 모서리 따냄</summary>
    static Sprite MakeCard (Color face, Color line, Color inner)
    {
        int S = 64;
        var tx = new Texture2D(S, S, TextureFormat.RGBA32, false); tx.filterMode = FilterMode.Bilinear; tx.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                int dx = Mathf.Min(x, S - 1 - x), dy = Mathf.Min(y, S - 1 - y), d = Mathf.Min(dx, dy);
                Color c;
                if (dx + dy < 3) c = new Color(0, 0, 0, 0);            // 모서리 따냄
                else if (d < 2) c = line;                               // 바깥 먹선
                else if (d == 5) c = Color.Lerp(face, inner, 0.6f);      // 안쪽 가는 선(옅게)
                else c = face;
                tx.SetPixel(x, y, c);
            }
        tx.Apply();
        return Sprite.Create(tx, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(12, 12, 12, 12));
    }
    static void EnsureCardSprite ()
    {
        if (cardSpriteDay != null) return;
        cardSpriteDay = MakeCard(new Color(0.975f, 0.955f, 0.915f, 0.97f), new Color(0.27f, 0.21f, 0.17f, 1f), new Color(0.55f, 0.47f, 0.39f, 0.55f));
        cardSpriteNight = MakeCard(new Color(0.12f, 0.105f, 0.095f, 0.96f), new Color(0.62f, 0.55f, 0.46f, 1f), new Color(0.45f, 0.40f, 0.34f, 0.6f));
        // 인장: 가장자리가 살짝 거친 사각
        int S = 32; var t2 = new Texture2D(S, S, TextureFormat.RGBA32, false);
        var rnd = new System.Random(7);
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
        {
            int d = Mathf.Min(Mathf.Min(x, S - 1 - x), Mathf.Min(y, S - 1 - y));
            float a = d >= 2 ? 1f : (d == 1 ? (rnd.NextDouble() < 0.85 ? 1f : 0.4f) : (rnd.NextDouble() < 0.55 ? 0.9f : 0f));
            t2.SetPixel(x, y, new Color(1, 1, 1, a));
        }
        t2.Apply();
        sealSprite = Sprite.Create(t2, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
        cardSprite = cardSpriteDay;
    }

    /// <summary>선택지 카드 폭: 배경 종이의 테두리 선(화면 폭의 약 4.5%) 안쪽에 들어오도록 화면 비율에 맞춰 계산</summary>
    static float CardWidth (Component c)
    {
        float cw = 900f;
        var cv = c.GetComponentInParent<Canvas>(); if (cv != null && cv.rootCanvas != null) cw = ((RectTransform)cv.rootCanvas.transform).rect.width;
        return Mathf.Min(840f, cw * 0.84f);
    }

    static TMPro.TMP_Text dlgText;
    static bool BandFits (int choices)
    {
        if (dlgText == null || !dlgText.isActiveAndEnabled)
        {
            dlgText = null;
            foreach (var t0 in FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)) if (t0.gameObject.name == "DialogueText") { dlgText = t0; break; }
            if (dlgText == null) return true;
        }
        float canvasH = 1600f;
        var rc = dlgText.canvas != null ? dlgText.canvas.rootCanvas : null; if (rc != null) canvasH = ((RectTransform)rc.transform).rect.height;
        float top = MolakArt.BandTop + MolakArt.BandH + 24f;
        float bottom = 150f + choices * (CardH(choices) + CardGap(choices)) + 30f;
        return dlgText.preferredHeight + 20f <= canvasH - top - bottom;
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
        // 삽화 띠 접기: 선택지 4개 이상이거나, 띠를 펼치면 본문이 선택지 위 공간에 다 안 들어갈 때만
        MolakArt.Collapsed = choiceCount >= 4 || (choiceCount >= 3 && !BandFits(choiceCount));
        EnsureCardSprite();
        ChoiceCount = choiceCount;   // 선택지가 떠 있으면 MolakScroll 이 본문 창 아래 한계를 선택지 위로 올린다
        // 선택지 잠금: 글을 빠르게 넘기다 막 뜬 선택지를 읽지도 않고 누르는 일을 막는다.
        // 선택지가 뜬 뒤 ChoiceLock 초 동안은 카드가 흐리게 떠오르며 눌리지 않는다(그 사이 시작한 터치는 무시).
        if (choiceCount > 0 && choiceCount != lastChoiceCount) choiceShownAt = Time.unscaledTime;
        lastChoiceCount = choiceCount;
        {
            var blLock = GameObject.Find("Naninovel<Runtime>/UI/ChoiceHandler/ButtonList");
            if (blLock != null)
            {
                var cg = blLock.GetComponent<CanvasGroup>(); if (cg == null) cg = blLock.AddComponent<CanvasGroup>();
                float since = Time.unscaledTime - choiceShownAt;
                bool locked = choiceCount > 0 && since < ChoiceLock;
                float cgA = choiceCount > 0 ? Mathf.Lerp(0.3f, 1f, Mathf.Clamp01(since / ChoiceLock)) : 1f;
                if (Mathf.Abs(cg.alpha - cgA) > 0.004f) cg.alpha = cgA;
                if (cg.interactable == locked) cg.interactable = !locked;
                if (cg.blocksRaycasts == locked) cg.blocksRaycasts = !locked;
            }
        }
        foreach (var btn in FindObjectsByType<Naninovel.UI.ChoiceHandlerButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            // ---- 선택지 카드: 한지 바탕 + 이중 먹선 테두리 + 왼쪽 주홍 인장(一二三四) ----
            int cidx = 0;
            for (int ci = 0; ci < btn.transform.GetSiblingIndex(); ci++)
                if (btn.transform.parent.GetChild(ci).GetComponent<Naninovel.UI.ChoiceHandlerButton>() != null && btn.transform.parent.GetChild(ci).gameObject.activeSelf) cidx++;
            var rootImg = btn.GetComponent<Image>();
            if (rootImg != null)
            {
                var spr = Night ? cardSpriteNight : cardSpriteDay;
                if (rootImg.sprite != spr) { rootImg.sprite = spr; rootImg.type = Image.Type.Sliced; rootImg.pixelsPerUnitMultiplier = 1f; }
                if (rootImg.color != Color.white) rootImg.color = Color.white;
            }
            foreach (var img in btn.GetComponentsInChildren<Image>(true))
                if (img != rootImg && img.gameObject.name != "MolakSeal" && img.color.a > 0.001f) img.color = new Color(0,0,0,0);
            var seal = btn.transform.Find("MolakSeal");
            if (seal == null)
            {
                var sg = new GameObject("MolakSeal", typeof(RectTransform), typeof(Image), typeof(UnityEngine.UI.LayoutElement));
                sg.transform.SetParent(btn.transform, false);
                sg.GetComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;
                var srt = (RectTransform)sg.transform;
                srt.anchorMin = new Vector2(0f, 0.5f); srt.anchorMax = new Vector2(0f, 0.5f); srt.pivot = new Vector2(0.5f, 0.5f);
                srt.sizeDelta = new Vector2(58f, 58f); srt.anchoredPosition = new Vector2(56f, 0f);
                sg.GetComponent<Image>().raycastTarget = false;
                var ng = new GameObject("Num", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
                ng.transform.SetParent(sg.transform, false);
                var nrt = (RectTransform)ng.transform; nrt.anchorMin = Vector2.zero; nrt.anchorMax = Vector2.one; nrt.offsetMin = Vector2.zero; nrt.offsetMax = new Vector2(0f, 2f);
                var nt = ng.GetComponent<TMPro.TextMeshProUGUI>(); nt.alignment = TMPro.TextAlignmentOptions.Center; nt.fontSize = 34f; nt.raycastTarget = false; nt.enableWordWrapping = false;
                seal = sg.transform;
            }
            var sealImg = seal.GetComponent<Image>();
            { var srt0 = (RectTransform)seal; float ss = Compact(choiceCount) ? 46f : 58f; if (Mathf.Abs(srt0.sizeDelta.x - ss) > 0.5f) { srt0.sizeDelta = new Vector2(ss, ss); srt0.anchoredPosition = new Vector2(ss < 50f ? 50f : 56f, 0f); } }
            // 조건부 선택지: 원래 글귀(주석 붙기 전)를 기억해 두고 종류·주석을 만든다
            var lblTx = btn.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (lblTx != null && lblTx.gameObject.name == "Num") lblTx = null;
            foreach (var tx0 in btn.GetComponentsInChildren<TMPro.TMP_Text>(true)) if (tx0.gameObject.name != "Num") { lblTx = tx0; break; }
            string rawLabel = lblTx != null ? lblTx.text : "";
            int hintAt = rawLabel.IndexOf("\n<size=62%>");
            if (hintAt >= 0) rawLabel = rawLabel.Substring(0, hintAt);
            var meta = MolakChoiceMeta.Get(cidx, rawLabel);
            if (lblTx != null)
            {
                string hintCol = Night ? "#9C9286" : "#8C8378";
                string wantText = string.IsNullOrEmpty(meta.Hint) ? rawLabel : rawLabel + "\n<size=62%><color=" + hintCol + ">" + meta.Hint + "</color></size>";
                if (lblTx.text != wantText) lblTx.text = wantText;
            }
            if (meta.Kind != MolakChoiceMeta.Normal && MolakStats.Get("g_sealhelp") < 1f) { MolakStats.Set("g_sealhelp", 1f); MolakMenu.ShowLegend(); }
            var sealCol = MolakChoiceMeta.SealColor(meta.Kind, Night);
            if (sealImg.color != sealCol) sealImg.color = sealCol;
            if (sealImg.sprite != sealSprite) sealImg.sprite = sealSprite;
            var num = seal.Find("Num").GetComponent<TMPro.TextMeshProUGUI>();
            string[] hn = { "一", "二", "三", "四", "五", "六", "七", "八", "九", "十", "十一", "十二", "十三", "十四", "十五" };
            string want = hn[Mathf.Clamp(cidx, 0, hn.Length - 1)];
            if (num.text != want) num.text = want;
            { float nfs = want.Length > 1 ? (Compact(choiceCount) ? 20f : 24f) : (Compact(choiceCount) ? 28f : 34f); if (Mathf.Abs(num.fontSize - nfs) > 0.5f) num.fontSize = nfs; }
            var numCol = new Color(0.98f, 0.95f, 0.89f, 1f);
            if (num.color != numCol) num.color = numCol;
            var vlg0 = btn.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            if (vlg0 != null && (vlg0.padding.left != 108 || vlg0.padding.right != 36 || vlg0.childAlignment != TextAnchor.MiddleLeft))
            { vlg0.padding.left = 108; vlg0.padding.right = 36; vlg0.childAlignment = TextAnchor.MiddleLeft; }
            var ub = btn.GetComponent<UnityEngine.UI.Button>();
            if (ub != null && (ub.targetGraphic != rootImg || ub.colors.normalColor != Color.white || (Night ? ub.colors.highlightedColor.r < 1f : ub.colors.highlightedColor.r > 0.99f)))
            {
                ub.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
                ub.targetGraphic = rootImg;
                var cb = ub.colors;
                cb.normalColor = Color.white;
                cb.highlightedColor = Night ? new Color(1.12f, 1.10f, 1.06f, 1f) : new Color(0.97f, 0.95f, 0.91f, 1f);
                cb.pressedColor = Night ? new Color(1.45f, 1.38f, 1.28f, 1f) : new Color(0.84f, 0.79f, 0.71f, 1f);
                cb.selectedColor = Color.white;
                cb.disabledColor = new Color(1f, 1f, 1f, 0.45f);
                cb.colorMultiplier = 1f;
                cb.fadeDuration = 0.06f;
                ub.colors = cb;
            }
            var le = btn.GetComponent<UnityEngine.UI.LayoutElement>();
            if (le == null) le = btn.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            float wantH = CardH(choiceCount);
            bool compact = Compact(choiceCount);
            float cardW = CardWidth(btn);
            if (Mathf.Abs(le.minHeight - wantH) > 0.5f || Mathf.Abs(le.minWidth - cardW) > 0.5f || le.flexibleWidth != 0f) { le.minHeight = wantH; le.minWidth = cardW; le.preferredWidth = cardW; le.flexibleWidth = 0f; }
            var inkDark = InkText;
            if (ub != null && !sfxHooked.Contains(ub.GetInstanceID())) { sfxHooked.Add(ub.GetInstanceID()); ub.onClick.AddListener(() => { MolakSfx.Tap(); }); }
            foreach (var tx in btn.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                if (tx.gameObject.name == "Num") continue;
                if (tx.color != inkDark) tx.color = inkDark;
                float cfs = Mathf.Round((choiceCount >= 9 ? 31f : compact ? 34f : 40f)*FontScale);
                if (Mathf.Abs(tx.fontSize - cfs) > 0.5f) tx.fontSize = cfs;
                if (!tx.enableWordWrapping) tx.enableWordWrapping = true;
                if (tx.gameObject.name != "Num" && tx.alignment != TMPro.TextAlignmentOptions.MidlineLeft) tx.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
                if (tx.overflowMode != TMPro.TextOverflowModes.Overflow) tx.overflowMode = TMPro.TextOverflowModes.Overflow;
                float need = tx.preferredHeight + (choiceCount >= 9 ? 14f : compact ? 22f : 44f);
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
                if (mk != null)
                {
                    // 둥근 소프트 마스크(UIMask)가 카드 가장자리를 흐리게 깎아 선이 끊겨 보이던 것 → 직사각 RectMask2D로 교체
                    if (mk.enabled) mk.enabled = false;
                    if (img.enabled) img.enabled = false;
                    if (img.GetComponent<UnityEngine.UI.RectMask2D>() == null) img.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
                }
                else if (img.color.a > 0.001f) img.color = new Color(0,0,0,0);
            }
            foreach (var vlg in blGo.GetComponentsInChildren<UnityEngine.UI.VerticalLayoutGroup>(true))
            {
                if (vlg.GetComponent<Naninovel.UI.ChoiceHandlerButton>() != null) continue;
                { float gp = CardGap(choiceCount); if (Mathf.Abs(vlg.spacing - gp) > 0.5f) vlg.spacing = gp; }
                if (vlg.childForceExpandWidth) vlg.childForceExpandWidth = false;   // 카드 폭 820 — 마스크(860) 안쪽으로
                var ca = vlg.childAlignment == TextAnchor.UpperLeft ? TextAnchor.UpperCenter : (vlg.childAlignment == TextAnchor.MiddleLeft ? TextAnchor.MiddleCenter : (vlg.childAlignment == TextAnchor.LowerLeft ? TextAnchor.LowerCenter : vlg.childAlignment));
                if (vlg.childAlignment != ca) vlg.childAlignment = ca;
            }
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
