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

    /// <summary>지금 떠 있는(아직 고르지 않은) 선택지 수. MolakScroll·MolakArt 가 본문 창과 삽화 띠를 맞춘다.</summary>
    public static int ChoiceCount;
    static float clickT = -1f; static string clickSig = "";
    public static int StuckCount;
    static string ChoiceSig () { var sb = new System.Text.StringBuilder(); foreach (var c in choiceBuf) sb.Append(c.Id).Append('|'); return sb.ToString(); }
    static float choiceShownAt = -9f; static int lastChoiceCount;
    /// <summary>화면 비율 무관 UI 스케일: 1080×1920 기준 폭·높이 중 작은 쪽</summary>
    public static float U { get { return Mathf.Min(Screen.width/1080f, Screen.height/1920f); } }
    public static readonly Color Bg = Hex("#100D0C");
    public static readonly Color Paper = Hex("#191411");
    public static readonly Color Raise = Hex("#221B17");
    public static readonly Color Line = Hex("#453831");
    public static readonly Color Ink = Hex("#F2E8D4");
    public static readonly Color Ink3 = Hex("#8A7D67");
    public static readonly Color Blood = Hex("#C03434");
    public static readonly Color Gold = Hex("#C9A227");
    /// <summary>저주·사술의 보랏빛. 되풀이(@deja) 문장도 이 빛으로 적는다.</summary>
    public const string CurseHex = "#9A70C4";
    public static readonly Color Curse = Hex(CurseHex);

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
        // 0.4.9: 9-슬라이스 — 네 모서리의 回 문양은 늘어나지 않고(정사각형 유지), 테두리 선과 가운데만 늘어난다
        sr.sprite = Sprite.Create(tex, new Rect(0,0,tex.width,tex.height), new Vector2(0.5f,0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(96,96,96,96));
        sr.drawMode = SpriteDrawMode.Sliced;
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
        // 가로·세로를 같은 배율로 키우고(모서리 정사각형), 모자란 쪽은 슬라이스 크기로 채운다
        if (sr.drawMode != SpriteDrawMode.Sliced) sr.drawMode = SpriteDrawMode.Sliced;
        float k = Mathf.Min(camW/sw, camH/sh);
        var want = new Vector3(k, k, 1f);
        if ((backdrop.localScale - want).sqrMagnitude > 0.0001f) backdrop.localScale = want;
        var wantSize = new Vector2(camW/k, camH/k);
        if ((sr.size - wantSize).sqrMagnitude > 0.0001f) sr.size = wantSize;
        var veilT2 = backdrop.Find("MolakPaperVeil"); if (veilT2 != null) veilT2.localScale = new Vector3(2000f/k, 2000f/k, 1f);
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
    /// <summary>0.4.8 선택지 카드: 먹 붓 자국. 왼쪽 붓머리는 짙고 둥글게, 오른쪽으로 갈수록 옅어지며 붓털 결대로 갈라져 끝난다.
    /// 텍스처 알파는 '눌렀을 때' 농도로 만들고, 평소에는 버튼 색(알파 절반)으로 옅게 보인다.</summary>
    static Sprite MakeBrush (Color ink, int seed)
    {
        int W = 512, H = 128;
        var tx = new Texture2D(W, H, TextureFormat.RGBA32, false); tx.filterMode = FilterMode.Bilinear; tx.wrapMode = TextureWrapMode.Clamp;
        var rnd = new System.Random(seed);
        // 붓털: 줄마다 결 농도와 끝나는 지점(갈라짐)
        float[] fib = new float[H], len = new float[H];
        for (int y = 0; y < H; y++) { fib[y] = (float)rnd.NextDouble(); len[y] = 0.6f + 0.4f * (float)rnd.NextDouble(); }
        for (int y = 1; y < H - 1; y++) fib[y] = (fib[y - 1] + 2f * fib[y] + fib[y + 1]) * 0.25f;
        for (int y = 1; y < H - 1; y++) len[y] = Mathf.Lerp(len[y], (len[y - 1] + len[y + 1]) * 0.5f, 0.35f);
        float ph1 = (float)rnd.NextDouble() * 6.28f, ph2 = (float)rnd.NextDouble() * 6.28f;
        var px = new Color[W * H];
        for (int x = 0; x < W; x++)
        {
            float u = x / (float)(W - 1);
            float jt = ((float)rnd.NextDouble() - 0.5f) * 0.012f, jb = ((float)rnd.NextDouble() - 0.5f) * 0.012f;
            // 위·아래 가장자리 흔들림(저주파 + 잔떨림), 오른쪽으로 조금씩 가늘어진다
            float top = 0.12f + 0.025f * Mathf.Sin(u * 7.1f + ph1) + 0.012f * Mathf.Sin(u * 23f + ph2) + jt + u * 0.06f;
            float bot = 0.12f + 0.025f * Mathf.Sin(u * 5.3f + ph2) + 0.012f * Mathf.Sin(u * 19f + ph1) + jb + u * 0.07f;
            const float hc = 0.07f;
            if (u < hc) { float k = (hc - u) / hc; float cap = 0.5f * (1f - Mathf.Sqrt(Mathf.Max(0f, 1f - k * k))); top = Mathf.Max(top, 0.12f + cap * 0.76f); bot = Mathf.Max(bot, 0.12f + cap * 0.76f); }   // 둥근 붓머리
            float press = 1f + 0.55f * Mathf.Exp(-u / 0.10f);                         // 붓을 처음 누른 자리가 짙다
            float fade = Mathf.Lerp(1f, 0.35f, Mathf.Pow(u, 0.85f));                  // 왼쪽 짙고 오른쪽 옅게
            for (int y = 0; y < H; y++)
            {
                float v = y / (float)(H - 1);
                float a = 0f;
                if (v > bot && v < 1f - top)
                {
                    float edge = Mathf.Min(v - bot, (1f - top) - v);
                    float soft = Mathf.Clamp01(edge / 0.016f);
                    float rim = 1f + 0.3f * Mathf.Clamp01(1f - edge / 0.05f);              // 마른 붓 테두리가 조금 더 짙다
                    float grain = 0.5f + 0.5f * fib[y];
                    float dry = u > 0.35f ? Mathf.Clamp01((fib[y] + 0.15f - (u - 0.35f) * 1.1f) / 0.25f) : 1f;   // 오른쪽으로 갈수록 결 사이가 빈다
                    float tail = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - len[y]) / 0.08f));               // 붓털마다 다른 자리에서 끊긴다
                    a = soft * rim * grain * dry * tail * fade * press;
                }
                px[y * W + x] = new Color(ink.r, ink.g, ink.b, Mathf.Clamp01(a) * ink.a);
            }
        }
        tx.SetPixels(px); tx.Apply();
        return Sprite.Create(tx, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
    }
    static void EnsureCardSprite ()
    {
        if (cardSpriteDay != null) return;
        cardSpriteDay = MakeBrush(new Color(0.17f, 0.13f, 0.10f, 0.64f), 11);
        cardSpriteNight = MakeBrush(new Color(0.88f, 0.82f, 0.72f, 0.44f), 11);
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
        float cw = MolakLayout.Canvas.x;
        var cv = c.GetComponentInParent<Canvas>(); if (cv != null && cv.rootCanvas != null) cw = ((RectTransform)cv.rootCanvas.transform).rect.width;
        return Mathf.Min(MolakLayout.CardMaxW, cw * MolakLayout.CardWidthRatio);
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
        float canvasH = MolakLayout.Canvas.y;
        var rc = dlgText.canvas != null ? dlgText.canvas.rootCanvas : null; if (rc != null) canvasH = ((RectTransform)rc.transform).rect.height;
        float top = MolakArt.BandTop + MolakArt.BandH + MolakLayout.BelowBand;
        float bottom = MolakLayout.BodyBottom(choices);
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
        var bl = MolakUi.ChoiceList;
        if (bl != null)
        {
            var cp = bl.transform.Find("ContentPanel") as RectTransform;
            if (cp != null && (cp.anchorMax.y != 0f || cp.pivot.y != 0f))
            {
                cp.anchorMin = new Vector2(0.5f, 0f);
                cp.anchorMax = new Vector2(0.5f, 0f);
                cp.pivot = new Vector2(0.5f, 0f);
                cp.anchoredPosition = new Vector2(0f, MolakLayout.BagStripH);
                cp.sizeDelta = new Vector2(MolakLayout.ChoicePanelW, cp.sizeDelta.y);
            }
        }
        // 모바일 세로 기준 UI 스케일 (폭 기준 매칭 — 폰에서 글자·버튼이 작아지지 않게)
        foreach (var cs in FindObjectsByType<UnityEngine.UI.CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (cs.uiScaleMode != UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize) continue;
            var want = MolakLayout.Canvas;
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
        // 선택지 멈춤 감시: 고른 뒤 2.5초가 지나도 같은 선택지(같은 Id 묶음)가 그대로면 기록한다(시험자 보고 '골랐는데 안 사라짐' 추적)
        if (clickT >= 0f && Time.unscaledTime - clickT > 2.5f)
        {
            if (choiceCount > 0 && ChoiceSig() == clickSig)
            {
                string where = "";
                try { var plo = Naninovel.Engine.GetService<Naninovel.IScriptPlayer>(); var tr = plo.GetType().GetProperty("MainTrack").GetValue(plo, null) as Naninovel.IScriptTrack; where = tr.PlaybackSpot + " cmd=" + (tr.PlayedCommand != null ? tr.PlayedCommand.GetType().Name : "-") + " exec=" + tr.Executing + " await=" + tr.AwaitingInput; } catch { }
                Debug.LogWarning("[MolakChoiceStuck] " + choiceCount + "개 그대로 · " + where + " · " + clickSig);
                StuckCount++;
                try { System.IO.File.AppendAllText(System.IO.Path.Combine(Application.persistentDataPath, "molak_debug.txt"), System.DateTime.Now.ToString("MM-dd HH:mm:ss") + " 선택지 멈춤 " + where + "\n"); } catch { }
            }
            clickT = -1f;
        }
        ChoiceCount = choiceCount;   // 선택지가 떠 있으면 MolakScroll 이 본문 창 아래 한계를 선택지 위로 올린다
        // 선택지 잠금: 선택지가 뜬 뒤 ChoiceLock 초 동안은 카드가 흐리게 떠오르며 눌리지 않는다(그 사이 시작한 터치는 무시).
        // 주의: ButtonList 자체의 CanvasGroup 은 나니노벨이 선택지 창을 보이고 감추는 데 쓴다(고르면 alpha 0).
        //       예전에는 여기서 그 값을 매 프레임 1 로 되돌려 놓아, 고른 선택지가 다음 묶음이 올 때까지 남아 있었다.
        //       그래서 잠금·떠오름은 안쪽 ContentPanel 에 따로 단 CanvasGroup 으로만 한다(두 값은 곱해진다).
        if (choiceCount > 0 && choiceCount != lastChoiceCount) choiceShownAt = Time.unscaledTime;
        lastChoiceCount = choiceCount;
        {
            var list = MolakUi.ChoiceList;
            var content = list != null ? list.transform.Find("ContentPanel") : null;
            if (content != null)
            {
                var cg = content.GetComponent<CanvasGroup>(); if (cg == null) cg = content.gameObject.AddComponent<CanvasGroup>();
                float since = Time.unscaledTime - choiceShownAt;
                bool locked = choiceCount > 0 && since < MolakLayout.ChoiceLock;
                float cgA = choiceCount > 0 ? Mathf.Clamp01(since / MolakLayout.ChoiceLock) : 1f;
                if (Mathf.Abs(cg.alpha - cgA) > 0.004f) cg.alpha = cgA;
                if (cg.interactable == locked) cg.interactable = !locked;
                if (cg.blocksRaycasts == locked) cg.blocksRaycasts = !locked;
            }
        }
        var tier = MolakLayout.For(choiceCount);
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
                if (rootImg.sprite != spr) { rootImg.sprite = spr; rootImg.type = Image.Type.Simple; rootImg.preserveAspect = false; }
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
                srt.sizeDelta = new Vector2(44f, 44f); srt.anchoredPosition = new Vector2(48f, 0f);
                sg.GetComponent<Image>().raycastTarget = false;
                var ng = new GameObject("Num", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
                ng.transform.SetParent(sg.transform, false);
                var nrt = (RectTransform)ng.transform; nrt.anchorMin = Vector2.zero; nrt.anchorMax = Vector2.one; nrt.offsetMin = Vector2.zero; nrt.offsetMax = new Vector2(0f, 2f);
                var nt = ng.GetComponent<TMPro.TextMeshProUGUI>(); nt.alignment = TMPro.TextAlignmentOptions.Center; nt.fontSize = 26f; nt.raycastTarget = false; nt.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
                seal = sg.transform;
            }
            var sealImg = seal.GetComponent<Image>();
            { var srt0 = (RectTransform)seal; if (Mathf.Abs(srt0.sizeDelta.x - tier.Seal) > 0.5f) { srt0.sizeDelta = new Vector2(tier.Seal, tier.Seal); srt0.anchoredPosition = new Vector2(tier.SealX, 0f); } }
            // 조건부 선택지: 원래 글귀(주석 붙기 전)를 기억해 두고 종류·주석을 만든다
            var lblTx = btn.GetComponentInChildren<TMPro.TMP_Text>(true);
            if (lblTx != null && lblTx.gameObject.name == "Num") lblTx = null;
            foreach (var tx0 in btn.GetComponentsInChildren<TMPro.TMP_Text>(true)) if (tx0.gameObject.name != "Num") { lblTx = tx0; break; }
            string rawLabel = lblTx != null ? lblTx.text : "";
            int hintAt = rawLabel.IndexOf("\n<size=62%>");
            if (hintAt >= 0) rawLabel = rawLabel.Substring(0, hintAt);
            rawLabel = MolakChoiceMeta.StripRisk(rawLabel);   // 승부수 표시 〔검술+2〕 는 주석으로 옮겨 적는다
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
            string want = MolakLayout.Numeral(cidx);
            if (num.text != want) num.text = want;
            { float nfs = want.Length > 1 ? tier.Numeral2 : tier.Numeral; if (Mathf.Abs(num.fontSize - nfs) > 0.5f) num.fontSize = nfs; }
            var numCol = new Color(0.98f, 0.95f, 0.89f, 1f);
            if (num.color != numCol) num.color = numCol;
            var vlg0 = btn.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            if (vlg0 != null && (vlg0.padding.left != MolakLayout.CardPadLeft || vlg0.padding.right != MolakLayout.CardPadRight || vlg0.childAlignment != TextAnchor.MiddleLeft))
            { vlg0.padding.left = MolakLayout.CardPadLeft; vlg0.padding.right = MolakLayout.CardPadRight; vlg0.childAlignment = TextAnchor.MiddleLeft; }
            var ub = btn.GetComponent<UnityEngine.UI.Button>();
            // 붓 자국: 평소엔 옅게(알파 절반), 누르면 먹이 배어든 듯 짙게(텍스처 본래 농도)
            if (ub != null && (ub.targetGraphic != rootImg || ub.colors.normalColor != new Color(1f, 1f, 1f, 0.5f) || ub.colors.pressedColor != Color.white))
            {
                ub.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
                ub.targetGraphic = rootImg;
                var cb = ub.colors;
                cb.normalColor = new Color(1f, 1f, 1f, 0.5f);
                cb.highlightedColor = new Color(1f, 1f, 1f, 0.58f);
                cb.pressedColor = Color.white;
                cb.selectedColor = new Color(1f, 1f, 1f, 0.5f);
                cb.disabledColor = new Color(1f, 1f, 1f, 0.25f);
                cb.colorMultiplier = 1f;
                cb.fadeDuration = 0.06f;
                ub.colors = cb;
            }
            var le = btn.GetComponent<UnityEngine.UI.LayoutElement>();
            if (le == null) le = btn.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            float wantH = tier.CardH;
            float cardW = CardWidth(btn);
            if (Mathf.Abs(le.minHeight - wantH) > 0.5f || Mathf.Abs(le.minWidth - cardW) > 0.5f || le.flexibleWidth != 0f) { le.minHeight = wantH; le.minWidth = cardW; le.preferredWidth = cardW; le.flexibleWidth = 0f; }
            var inkDark = InkText;
            if (ub != null && !sfxHooked.Contains(ub.GetInstanceID())) { sfxHooked.Add(ub.GetInstanceID()); ub.onClick.AddListener(() => { MolakSfx.Tap(); clickT = Time.unscaledTime; clickSig = ChoiceSig(); }); }
            foreach (var tx in btn.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                if (tx.gameObject.name == "Num") continue;
                if (tx.color != inkDark) tx.color = inkDark;
                float cfs = Mathf.Round(tier.Font * FontScale);
                if (Mathf.Abs(tx.fontSize - cfs) > 0.5f) tx.fontSize = cfs;
                if (tx.textWrappingMode != TMPro.TextWrappingModes.Normal) tx.textWrappingMode = TMPro.TextWrappingModes.Normal;
                if (tx.gameObject.name != "Num" && tx.alignment != TMPro.TextAlignmentOptions.MidlineLeft) tx.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
                if (tx.overflowMode != TMPro.TextOverflowModes.Overflow) tx.overflowMode = TMPro.TextOverflowModes.Overflow;
                float need = tx.preferredHeight + tier.TextPad;
                if (need > le.minHeight + 1f) le.minHeight = need;
            }
        }
        // 선택지 리스트: 흰 패널 배경 제거(뭉탱이 원인) + 카드 간격
        var blGo = MolakUi.ChoiceList;
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
                { float gp = tier.Gap; if (Mathf.Abs(vlg.spacing - gp) > 0.5f) vlg.spacing = gp; }
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
            float bfs = Mathf.Round(MolakLayout.BodyFont * FontScale);
            foreach (var tx in panel.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                if (tx.color != inkTxt) tx.color = inkTxt;
                if (tx.gameObject.name == "DialogueText" && Mathf.Abs(tx.fontSize - bfs) > 0.5f) tx.fontSize = bfs;
            }
        }
    }
}
