using Naninovel;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 본문 스크롤 — 같은 맥락(초기화 전)의 이전 대사를 위로 쓸어 올려 다시 볼 수 있게 한다.
/// Printer를 마스크 뷰포트 아래로 옮기고, 넘치는 본문은 지우지 않고 뷰포트 밖으로 밀어 둔다(기본은 항상 맨 아래 정렬).
/// 새 본문이 찍히면 자동으로 맨 아래로 돌아온다. 드래그 중에는 '계속' 입력이 먹지 않도록 MolakInputGuard와 연동.
/// </summary>
[DefaultExecutionOrder(-400)]
public class MolakScroll : MonoBehaviour
{
    static MolakScroll inst;
    RectTransform viewport, printer;
    float userScroll;          // 아래 정렬 기준으로 위로 올린 양(캔버스 단위)
    float contentOffset;       // 본문 높이 - 뷰포트 높이 (>=0)
    float topCur = -215f;
    float over;                // 끝을 넘겨 당긴 양(캔버스 단위, + = 맨 위(옛 글)를 넘어 아래로, − = 맨 아래(새 글)를 넘어 위로). 손을 떼면 0으로 돌아간다.
    const float RubberD = 700f; // 고무줄 세기 — 클수록 덜 뻑뻑하다
    static float Rubber (float x) { return x <= 0f ? 0f : RubberD * (1f - 1f / (x * 0.55f / RubberD + 1f)); }

    // 드래그 상태 (MolakInputGuard가 참조)
    public static bool Dragging { get; private set; }
    public static bool DraggedThisPress { get; private set; }
    public static bool ScrolledUp => inst != null && inst.userScroll > 1f;
    bool pressing; Vector2 pressPos; float pressScroll; bool pressInBottomZone;
    // 0.4.81 행낭 서랍 끌기(손가락을 따라온다) — 빠르기는 손을 뗄 때 열림/닫힘을 정하는 데 쓴다
    bool bagDrag; float bagLastY, bagLastT, bagVel;
    bool hSwipe; Vector2 lastPos;   // 0.4.57 가로로 쓸기: 왼쪽→오른쪽이면 지난 쪽(MolakPageBack)

    const float DragThresholdPx = 14f;

    // ---- 0.4.82 그림 화면(F1)의 글자리: 옻칠 판 + 금 모서리. 판 높이는 글 길이를 따라 자란다 ----
    RectTransform cinePanel; CanvasGroup cineCg, vpCg; float cineH = -1f;
    /// <summary>선택지 묶음의 아랫변(캔버스 단위, 글자리 바로 위). MolakTheme 이 ContentPanel 을 여기에 놓는다.</summary>
    public static float CinemaChoiceY = MolakLayout.BagStripH;
    /// <summary>글자리 판의 화면 자리(GUI 좌표, 위가 0). 이름 인장·마디 이름이 쓴다. 판이 없으면 폭 0.</summary>
    public static Rect CinemaPanelGui;
    /// <summary>글자리가 지금 보이나(마디 이름이 덮고 있는 동안은 아니다).</summary>
    public static bool CinemaPanelShown { get { return inst != null && inst.cinePanel != null && inst.cinePanel.gameObject.activeSelf && (inst.cineCg == null || inst.cineCg.alpha > 0.5f); } }
    /// <summary>0.4.82 그림 속 사람이 지금 말하는 중인가 — 대본에 화자가 없어 글로 어림한다.
    /// 지금 쪽의 마지막 문단이 따옴표 대사이고 오른쪽 정렬(주인공의 말)이 아니면 '말한다'. 서술·속말('…')·주인공 대사면 아니다.</summary>
    public static bool CharSpeaking { get; private set; }
    TMPro.TMP_Text spkTx; string spkSrc;
    void UpdateSpeaking ()
    {
        if (spkTx == null || !spkTx.isActiveAndEnabled)
        {
            spkTx = null;
            if (printer != null) foreach (var t0 in printer.GetComponentsInChildren<TMPro.TMP_Text>(false)) if (t0.gameObject.name == "DialogueText") { spkTx = t0; break; }
            if (spkTx == null) { CharSpeaking = false; return; }
        }
        string src = spkTx.text;
        if (ReferenceEquals(src, spkSrc)) return;
        spkSrc = src;
        CharSpeaking = false;
        // 0.4.83 대본에 말하는 사람이 적혀 있으면 그것을 따른다(@spk) — 그림 속 인물 id 가 그 이름으로 시작하면 '말한다'
        var fs = MolakFlow.CurrentSpeaker;
        if (!string.IsNullOrEmpty(fs)) { var cid = MolakArt.CharId; CharSpeaking = fs != "me" && !string.IsNullOrEmpty(cid) && cid.StartsWith(fs); return; }
        if (string.IsNullOrEmpty(src)) return;
        int end = src.Length - 1;
        while (end >= 0 && char.IsWhiteSpace(src[end])) end--;
        if (end < 0) return;
        int st = src.LastIndexOf('\n', end); st = st < 0 ? 0 : st + 1;
        string para = src.Substring(st, end - st + 1);
        if (para.Contains("align=right")) return;
        var sb = new System.Text.StringBuilder(para.Length); bool tag = false;
        foreach (char c in para) { if (c == '<') tag = true; else if (c == '>') tag = false; else if (!tag) sb.Append(c); }
        string plain = sb.ToString().Trim();
        if (plain.Length == 0) return;
        char f = plain[0];
        CharSpeaking = f == '"' || f == '\u201C' || f == '「';
    }
    static Sprite cineSprite;
    /// <summary>옻칠 판: 짙은 먹빛 바탕(90%), 가는 금테(1), 네 모서리 금 꺾쇠(2×14). 9-슬라이스.</summary>
    static Sprite CineSprite ()
    {
        if (cineSprite != null) return cineSprite;
        const int S = 72, B = 32, Arm = 28, Th = 4, Ln = 2;
        var tx = new Texture2D(S, S, TextureFormat.RGBA32, false); tx.filterMode = FilterMode.Bilinear; tx.wrapMode = TextureWrapMode.Clamp;
        var fill = new Color(17f/255f, 15f/255f, 14f/255f, 0.90f);
        var line = new Color(212f/255f, 180f/255f, 106f/255f, 0.42f);
        var br = new Color(212f/255f, 180f/255f, 106f/255f, 1f);
        var px = new Color[S * S];
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
        {
            int dx = Mathf.Min(x, S - 1 - x), dy = Mathf.Min(y, S - 1 - y);
            Color c;
            if ((dx < Th && dy < Arm) || (dy < Th && dx < Arm)) c = br;
            else if (dx < Ln || dy < Ln) c = line;
            else c = fill;
            px[y * S + x] = c;
        }
        tx.SetPixels(px); tx.Apply();
        cineSprite = Sprite.Create(tx, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(B, B, B, B));
        return cineSprite;
    }
    void EnsureCine ()
    {
        if (cinePanel != null || viewport == null) return;
        var par = viewport.parent;
        var old = par.Find("MolakCinePanel") as RectTransform;
        if (old == null)
        {
            var go = new GameObject("MolakCinePanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            old = go.GetComponent<RectTransform>();
            old.SetParent(par, false);
            old.anchorMin = Vector2.zero; old.anchorMax = Vector2.one; old.pivot = new Vector2(0.5f, 0f);
            var img = go.GetComponent<Image>(); img.sprite = CineSprite(); img.type = Image.Type.Sliced; img.raycastTarget = false; img.color = Color.white;
            go.GetComponent<CanvasGroup>().blocksRaycasts = false;
        }
        old.SetSiblingIndex(0);   // 본문(뷰포트) 뒤에 깔린다
        cinePanel = old; cineCg = old.GetComponent<CanvasGroup>();
        vpCg = viewport.GetComponent<CanvasGroup>(); if (vpCg == null) vpCg = viewport.gameObject.AddComponent<CanvasGroup>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot ()
    {
        if (inst) return;
        var go = new GameObject("MolakScroll"); DontDestroyOnLoad(go);
        inst = go.AddComponent<MolakScroll>();
    }

    /// <summary>새 본문 출력 시작 — 맨 아래로 복귀.</summary>
    public static void ResetToBottom () { if (inst != null) inst.userScroll = 0f; }

    /// <summary>0.4.47: 선택지가 없을 때 본문 한 쪽의 높이(캔버스 단위). 그림 띠가 있으면 그 아래부터. 뷰포트가 아직 없으면 -1.</summary>
    public static float TargetBodyHeight ()
    {
        if (inst == null || inst.viewport == null) return -1f;
        float canvasH = MolakLayout.Canvas.y;
        var root = inst.viewport.GetComponentInParent<Canvas>();
        if (root != null && root.rootCanvas != null) canvasH = (root.rootCanvas.transform as RectTransform).rect.height;
        if (MolakTheme.CinemaNow) return MolakLayout.CineMaxH(canvasH) - MolakLayout.CinePadT - MolakLayout.CinePadB;
        if (MolakTheme.TheaterNow) return canvasH - MolakLayout.ThTextTop - MolakLayout.ThBottomBand - 20f;   // 0.4.82 극장판 글 칸   // 0.4.82 글자리 판을 다 키웠을 때의 글 칸
        float top = MolakArt.HasArt ? (MolakArt.BandTop + MolakArt.BandH + MolakLayout.BelowBand + MolakLayout.DuelStrip) : MolakLayout.BodyTop;
        float bottom = Mathf.Min(MolakLayout.BodyBottom(0, 0f), canvasH - MolakLayout.BodyMinH);
        return canvasH - bottom - top;
    }

    static float CanvasScale () => Mathf.Min(Screen.width / MolakLayout.Canvas.x, Screen.height / MolakLayout.Canvas.y);

    bool Ensure ()
    {
        if (viewport != null && printer != null) return true;
        var panel = MolakUi.PrinterPanel;
        if (panel == null) return false;
        var pr = panel.transform.Find("Printer") as RectTransform;
        var vpT = panel.transform.Find("MolakViewport") as RectTransform;
        if (vpT == null)
        {
            if (pr == null) return false;
            var go = new GameObject("MolakViewport", typeof(RectTransform), typeof(RectMask2D));
            vpT = go.GetComponent<RectTransform>();
            vpT.SetParent(panel.transform, false);
            vpT.anchorMin = new Vector2(0f, 0f); vpT.anchorMax = new Vector2(1f, 1f); vpT.pivot = new Vector2(0.5f, 1f);
            vpT.offsetMin = new Vector2(MolakLayout.BodySide, MolakLayout.BagStripH); vpT.offsetMax = new Vector2(-MolakLayout.BodySide, -MolakLayout.BodyTop);
            pr.SetParent(vpT, false);
            pr.anchorMin = new Vector2(0f, 1f); pr.anchorMax = new Vector2(1f, 1f); pr.pivot = new Vector2(0.5f, 1f);
            pr.offsetMin = new Vector2(0f, pr.offsetMin.y); pr.offsetMax = new Vector2(0f, 0f);
        }
        else if (pr == null) pr = vpT.Find("Printer") as RectTransform;
        viewport = vpT; printer = pr;
        return printer != null;
    }

    void Update ()
    {
        if (!Engine.Initialized || !Ensure()) return;
        HandleDrag();
        UpdateSpeaking();
        if (!Dragging && over != 0f)
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            over = Mathf.Lerp(over, 0f, 1f - Mathf.Pow(0.0004f, dt));   // 약 0.3초에 제자리로
            if (Mathf.Abs(over) < 0.5f) over = 0f;
        }
        Layout();
    }

    void BagTrack (float y)
    {
        float t = Time.unscaledTime, dt = t - bagLastT;
        if (dt <= 0.0001f) return;
        float v = (y - bagLastY) / dt;
        bagVel = Mathf.Lerp(bagVel, v, 0.5f);
        bagLastY = y; bagLastT = t;
    }

    void HandleDrag ()
    {
        Vector2 pos; bool down = MolakGui.PointerScreen(out pos);
        if (down && !pressing)
        {
            pressing = true; pressPos = pos; lastPos = pos; pressScroll = userScroll; DraggedThisPress = false; Dragging = false; hSwipe = false; bagDrag = false; bagVel = 0f;
            // 화면 맨 아래 띠(선택지 아래 150캔버스단위)에서 시작한 누름 → 위로 쓸면 행낭
            float zone = (MolakTheme.ChoiceCount > 0 ? MolakLayout.BagSwipeZoneWithChoices : MolakLayout.BagSwipeZone) * CanvasScale();
            pressInBottomZone = !MolakTheme.CinemaNow && !MolakTheme.TheaterNow && pos.y < zone && MolakStats.Get("day") >= 1f && !MolakMenu.IsOpen && !MolakBag.IsOpen && !MolakBlackout.Busy;
        }
        else if (down && pressing)
        {
            float dyPx = pos.y - pressPos.y;   // 위로 쓸면 +
            if (bagDrag)
            {
                MolakBag.DragSet(dyPx); BagTrack(pos.y); DraggedThisPress = true;
                return;
            }
            if (MolakBag.IsOpen)
            {
                // 서랍이 열려 있으면 본문 스크롤 대신: 아래로 끌면 서랍이 손가락을 따라 내려온다
                // (목록이 맨 위에 있거나, 서랍 머리 — 손잡이·제목 — 부근에서 시작한 누름이면)
                float headerY = Screen.height - MolakBag.PanelTopPx;   // 패널 윗변(스크린 좌표)
                bool fromHeader = pressPos.y >= headerY - 130f * CanvasScale();
                if (dyPx < -10f * Mathf.Max(1f, CanvasScale()) && (MolakBag.ListAtTop || fromHeader))
                { bagDrag = true; MolakBag.DragBegin(); pressPos = pos; bagLastY = pos.y; bagLastT = Time.unscaledTime; DraggedThisPress = true; return; }
                if (Mathf.Abs(dyPx) > DragThresholdPx) DraggedThisPress = true;
                return;
            }
            if (pressInBottomZone)
            {
                if (dyPx > 8f * Mathf.Max(1f, CanvasScale()))
                { bagDrag = true; MolakBag.DragBegin(); pressPos = pos; bagLastY = pos.y; bagLastT = Time.unscaledTime; DraggedThisPress = true; pressInBottomZone = false; }
                return;
            }
            lastPos = pos;
            float dxPx = pos.x - pressPos.x;
            if (!Dragging && !hSwipe && Mathf.Abs(dxPx) > 34f * CanvasScale() && Mathf.Abs(dxPx) > Mathf.Abs(dyPx) * 1.4f
                && MolakStats.Get("day") >= 1f && !MolakMenu.IsOpen && !MolakGuide.IsOpen && !MolakTruthView.IsOpen && !MolakStatLogView.IsOpen && !MolakBlackout.Busy && !MolakArt.InDuel)
            { hSwipe = true; DraggedThisPress = true; }
            if (hSwipe) return;
            if (!Dragging && Mathf.Abs(dyPx) > DragThresholdPx && Mathf.Abs(dyPx) > Mathf.Abs(pos.x - pressPos.x)) { Dragging = true; DraggedThisPress = true; }
            if (Dragging)
            {
                float dyCanvas = dyPx / Mathf.Max(0.01f, CanvasScale());
                // 손가락을 위로 올리면 본문이 위로 가므로(최신 쪽으로) userScroll 감소, 아래로 내리면 과거로 → 증가
                // 끝을 넘겨 당기면 고무줄처럼 늘어난다(당길수록 조금씩 뻑뻑), 손을 떼면 Update 에서 되돌아간다
                float raw = pressScroll - dyCanvas, mx = Mathf.Max(0f, contentOffset);
                if (raw > mx) { userScroll = mx; over = Rubber(raw - mx); }
                else if (raw < 0f) { userScroll = 0f; over = -Rubber(-raw); }
                else { userScroll = raw; over = 0f; }
            }
        }
        else if (!down && pressing)
        {
            if (bagDrag) { MolakBag.DragEnd(Time.unscaledTime - bagLastT > 0.12f ? 0f : bagVel); bagDrag = false; }
            if (hSwipe)
            {
                float dx = lastPos.x - pressPos.x;
                if (Mathf.Abs(dx) > 90f * CanvasScale()) { if (dx > 0f) MolakPageBack.Back(); else MolakPageBack.Forward(); }
                hSwipe = false;
            }
            pressing = false; Dragging = false;
        }
    }

    void Layout ()
    {
        float canvasW = MolakLayout.Canvas.x, canvasH = MolakLayout.Canvas.y;
        // 0.4.68 가로 화면(PC): 본문 창의 부모(PrinterPanel)는 캔버스 전체가 아니라 가운데 폭만 차지한다.
        //   캔버스 전체 폭으로 좌우 여백을 잡으면 여백이 부모 폭보다 커져 본문 칸 폭이 0 이 되어 글이 하나도 안 보였다.
        //   좌우 여백은 본문 창이 실제로 들어 있는 부모의 폭을 기준으로 잡는다.
        var par = viewport.parent as RectTransform;
        if (par != null && par.rect.width > 1f) { canvasW = par.rect.width; canvasH = par.rect.height; }
        else
        {
            var root = viewport.GetComponentInParent<Canvas>();
            if (root != null && root.rootCanvas != null) { var r = (root.rootCanvas.transform as RectTransform).rect; canvasW = r.width; canvasH = r.height; }
        }
        EnsureCine();
        bool cine = MolakTheme.CinemaNow && cinePanel != null;
        if (cinePanel != null && cinePanel.gameObject.activeSelf != cine) cinePanel.gameObject.SetActive(cine);
        {   // 마디 이름이 떠 있는 동안 글자리를 감췄다가, 걷히면 떠오른다
            bool thr = MolakTheme.TheaterNow;
            float a = cine || thr ? 1f - MolakVfx.ChapterCover : 1f;
            // 극장판: 선택지가 뜨면 본문 대신 한두 줄 요약(OnGUI). 고른 뒤에도 새 글이 찍힐 때까지는 옛 쪽을 다시 비추지 않는다
            //   (선택지가 사라지는 동안 옛 본문이 잠깐 떴다가 새 쪽으로 바뀌어 잔상처럼 보이던 것).
            if (thr)
            {
                string curTx = spkTx != null ? spkTx.text : null;
                // 0.4.85 선택지가 뜨는 순간의 글만 붙든다. 예전엔 선택지가 스러지는 동안에도 붙든 글을 계속 새 글로 바꿔 적어,
                //   고른 뒤 첫 문단이 감춰진 채 다 찍혔다가 다음 문단이 올 때 한꺼번에 나타났다(「노점상이 놀라…」).
                int cc = MolakTheme.ChoiceCount;
                if (cc > 0 && thPrevChoices == 0) { thHold = true; thHoldText = curTx; }
                // 0.4.91 붙든 글과 '내용'으로 견준다(예전엔 문자열 참조로 견줘, 선택지가 뜬 직후 같은 글이 다시 대입되기만 해도(빨리 모드에서 드러내기가 끝날 때 등)
                //   요약을 걷고 옛 본문을 선택지 뒤에 다시 비췄다 — 언메이커: 빨리로 볼 때 글이 남아 있거나 안 보이던 글이 갑자기 나온다).
                //   0.4.91 다시: 선택지가 떠 있는 동안에는 글이 어떻게 바뀌든(한 프레임 다른 글이 대입됐다 돌아오는 일이 있었다) 붙든 채로 두고,
                //   선택지가 걷힌 뒤에만 '새 글이 찍혔나(내용이 다르다) / 2초가 지났나'로 놓는다. 빨리 모드 점검에서 선택지가 뜬 지 0.02초 만에 본문이 다시 보이던 것.
                bool same = string.Equals(curTx, thHoldText);
                if (cc > 0 && thHold) thHoldT = Time.unscaledTime;
                if (thHold && cc == 0 && (!same || Time.unscaledTime - thHoldT > 2f)) thHold = false;
                thPrevChoices = cc;
                if (thHold) a = 0f;
            }
            else thHold = false;
            a *= 1f - MolakVfx.StampCover;   // 0.4.91 인장이 뜨면 글을 걷는다(덮개판 대신)
            {   // 0.4.91 기준: 쪽이 바뀌면 새 쪽이 짧게 떠오른다(언메이커: 갑자기 넘어가는 느낌 — 빨리 모드는 한 쪽이 통째로 툭 바뀌었다).
                float pf = (Time.unscaledTime - pageAt) / (MolakPlayMode.Fast ? PageFadeFast : PageFade);
                if (pf < 1f) a *= Mathf.SmoothStep(0.5f, 1f, Mathf.Clamp01(pf));   // 반쯤 비친 데서 시작(가려진 채 글이 찍히지 않게)
            }
            BodyAlpha = a;
            if (cineCg != null && Mathf.Abs(cineCg.alpha - a) > 0.002f) cineCg.alpha = a;
            if (vpCg != null && Mathf.Abs(vpCg.alpha - a) > 0.002f) vpCg.alpha = a;
        }
        if (cine) { LayoutCine(canvasW, canvasH); return; }
        if (MolakTheme.TheaterNow) { LayoutTheater(canvasW, canvasH); return; }
        TheaterTextGui = new Rect(0, 0, 0, 0);
        CinemaPanelGui = new Rect(0, 0, 0, 0);
        float side = Mathf.Max(MolakLayout.BodySide, (canvasW - MolakLayout.Canvas.x) / 2f + MolakLayout.BodySide);
        float topOff = MolakArt.BandActive ? -(MolakArt.BandTop + MolakArt.BandH + MolakLayout.BelowBand + MolakLayout.DuelStrip) : -MolakLayout.BodyTop;
        topCur = Mathf.Lerp(topCur, topOff, Time.deltaTime * 6f);
        if (Mathf.Abs(topCur - topOff) < 0.5f) topCur = topOff;

        // 아래 한계: 선택지가 떠 있으면 선택지 위, 아니면 화면 하단 여백
        int choices = MolakTheme.ChoiceCount;
        float listH = 0f;
        if (choices > 0)
        {
            var bl = MolakUi.ChoiceList;
            var cp = bl != null ? bl.transform.Find("ContentPanel") as RectTransform : null;
            if (cp != null) listH = cp.rect.height;   // 실측이 있으면 실측, 없으면 단계별 어림
        }
        float bottom = Mathf.Min(MolakLayout.BodyBottom(choices, listH), canvasH - MolakLayout.BodyMinH);
        var wantMin = new Vector2(side, bottom); var wantMax = new Vector2(-side, topCur);
        if ((viewport.offsetMin - wantMin).sqrMagnitude > 0.01f) viewport.offsetMin = wantMin;
        if ((viewport.offsetMax - wantMax).sqrMagnitude > 0.01f) viewport.offsetMax = wantMax;
        PlacePrinter();
    }

    void LayoutCine (float canvasW, float canvasH)
    {
        float sideP = Mathf.Max(MolakLayout.CineSide, (canvasW - MolakLayout.Canvas.x) / 2f + MolakLayout.CineSide);
        float bot = MolakLayout.CineBottom, padT = MolakLayout.CinePadT, padB = MolakLayout.CinePadB, padS = MolakLayout.CinePadS;
        float maxH = MolakLayout.CineMaxH(canvasH);
        float want = maxH;   // 0.4.82 글자리 크기는 고정(언메이커: 글 길이 따라 커졌다 줄었다 하니 정신없다)
        int choices = MolakTheme.ChoiceCount;
        if (choices > 0)
        {
            var bl = MolakUi.ChoiceList;
            var cp = bl != null ? bl.transform.Find("ContentPanel") as RectTransform : null;
            float listH = cp != null ? cp.rect.height : MolakLayout.ChoiceListH(choices);
            float room = canvasH - MolakLayout.CineTopReserve - bot - listH - MolakLayout.CineChoiceGap;
            want = Mathf.Min(want, Mathf.Max(220f, room));   // 선택지가 많으면 판을 줄이고 글은 스크롤로
        }
        if (cineH < 0f) cineH = want;
        cineH = Mathf.Lerp(cineH, want, 1f - Mathf.Pow(0.0005f, Mathf.Min(Time.unscaledDeltaTime, 0.1f)));   // 약 0.25초에 맞춰 자라고 준다
        if (Mathf.Abs(cineH - want) < 0.5f) cineH = want;
        float topGap = canvasH - bot - cineH;
        var pMin = new Vector2(sideP, bot); var pMax = new Vector2(-sideP, -topGap);
        if ((cinePanel.offsetMin - pMin).sqrMagnitude > 0.01f) cinePanel.offsetMin = pMin;
        if ((cinePanel.offsetMax - pMax).sqrMagnitude > 0.01f) cinePanel.offsetMax = pMax;
        var wantMin = new Vector2(sideP + padS, bot + padB); var wantMax = new Vector2(-(sideP + padS), -(topGap + padT));
        if ((viewport.offsetMin - wantMin).sqrMagnitude > 0.01f) viewport.offsetMin = wantMin;
        if ((viewport.offsetMax - wantMax).sqrMagnitude > 0.01f) viewport.offsetMax = wantMax;
        topCur = wantMax.y;   // 예전 배치로 돌아갈 때 이 자리에서 미끄러져 간다
        CinemaChoiceY = bot + cineH + MolakLayout.CineChoiceGap;
        // 판의 화면 자리(IMGUI 좌표) — 이름 인장·마디 이름용
        var cs = new Vector3[4]; cinePanel.GetWorldCorners(cs);
        var rc = cinePanel.GetComponentInParent<Canvas>();
        Camera cam = rc != null && rc.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? rc.rootCanvas.worldCamera : null;
        var a = RectTransformUtility.WorldToScreenPoint(cam, cs[0]); var b = RectTransformUtility.WorldToScreenPoint(cam, cs[2]);
        CinemaPanelGui = new Rect(a.x, Screen.height - b.y, b.x - a.x, b.y - a.y);
        PlacePrinter();
    }

    // ================= 0.4.82 극장판(D) =================
    /// <summary>극장판 선택지 묶음의 아랫변(캔버스 단위). 요약 두 줄 바로 아래에서 아래로 늘어서게 잡는다.</summary>
    public static float TheaterChoiceY = MolakLayout.BagStripH;
    /// <summary>극장판 글 칸의 화면 자리(GUI 좌표). 요약·인장·쪽지가 쓴다.</summary>
    public static Rect TheaterTextGui;
    /// <summary>점검용: 본문이 지금 얼마나 보이나(0 이면 가려졌다)</summary>
    public static float BodyAlpha = 1f;
    /// <summary>0.4.91 새 쪽이 떠오르는 시간(초). 보통 0.2 · 빨리 0.28(쪽이 통째로 찍히니 조금 더).</summary>
    public const float PageFade = 0.2f, PageFadeFast = 0.28f;
    static float pageAt = -9f;
    public static void NewPage () { pageAt = Time.unscaledTime; }
    static bool thHold; static int thPrevChoices; static string thHoldText; static float thHoldT; static float thSumTopC = 800f;
    void LayoutTheater (float canvasW, float canvasH)
    {
        // 0.4.87 글 칸 폭: 넓은 화면(폴드·태블릿)에서 900 기준 칸에 묶어 두면 좌우가 휑하다. 그림 창은 화면 폭을 다 쓰니
        //   글도 화면 폭을 따르되, 한 줄이 너무 길어 눈이 길을 잃지 않게 ThTextMaxW 에서 멈춘다(폰 900 폭에선 예전과 같다).
        float side = Mathf.Max(MolakLayout.ThTextSide, (canvasW - MolakLayout.ThTextMaxW) / 2f);
        float top = MolakLayout.ThTextTop + MolakLayout.DuelStrip, bot = MolakLayout.ThBottomBand + 20f;   // 비무의 간파 판이 떠 있으면 글을 그만큼 내린다
        var wantMin = new Vector2(side, bot); var wantMax = new Vector2(-side, -top);
        if ((viewport.offsetMin - wantMin).sqrMagnitude > 0.01f) viewport.offsetMin = wantMin;
        if ((viewport.offsetMax - wantMax).sqrMagnitude > 0.01f) viewport.offsetMax = wantMax;
        topCur = wantMax.y;
        int choices = MolakTheme.ChoiceCount;
        float listH = 0f;
        if (choices > 0) { var bl = MolakUi.ChoiceList; var cp = bl != null ? bl.transform.Find("ContentPanel") as RectTransform : null; listH = cp != null ? cp.rect.height : MolakLayout.ChoiceListH(choices); }
        if (choices > 0)
        {   // 고른 뒤 사라지는 동안엔 자리를 옮기지 않는다
            TheaterChoiceY = Mathf.Max(bot, canvasH - top - MolakLayout.ThSummaryH - MolakLayout.ThChoiceGap - listH);
            // 선택지가 많아 글 칸에 다 안 들어가면: 선택지는 아래 띠 위에 붙이고, 요약은 선택지 바로 위로(그림 창 아래쪽에 걸친다)
            float sumTop = Mathf.Min(top, canvasH - (TheaterChoiceY + listH) - MolakLayout.ThChoiceGap - MolakLayout.ThSummaryH);
            thSumTopC = Mathf.Max(MolakLayout.ThTopBand + 12f, sumTop);
        }
        var cs = new Vector3[4]; viewport.GetWorldCorners(cs);
        var rc = viewport.GetComponentInParent<Canvas>();
        Camera cam = rc != null && rc.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? rc.rootCanvas.worldCamera : null;
        var a = RectTransformUtility.WorldToScreenPoint(cam, cs[0]); var b = RectTransformUtility.WorldToScreenPoint(cam, cs[2]);
        TheaterTextGui = new Rect(a.x, Screen.height - b.y, b.x - a.x, b.y - a.y);
        CinemaPanelGui = new Rect(0, 0, 0, 0);
        UpdateSummary(choices);
        PlacePrinter();
    }

    // 선택지 위 한두 줄: 선택지가 뜨는 순간의 쪽에서 — 마지막 남의 대사(말한 사람 이름) + 그 뒤 서술 한 줄. 남의 대사가 없으면 끝의 두 문단.
    static string sumName = "", sumLine1 = "", sumLine2 = ""; static bool sumDlg; int sumFor = -1;
    void UpdateSummary (int choices)
    {
        if (choices <= 0) { sumFor = -1; return; }
        if (sumFor == choices) return;
        sumFor = choices;
        sumName = ""; sumLine1 = ""; sumLine2 = ""; sumDlg = false;
        if (spkTx == null) return;
        string page = spkTx.text ?? "";
        var paras = new System.Collections.Generic.List<string>(); var right = new System.Collections.Generic.List<bool>();
        foreach (var raw in page.Split('\n'))
        {
            if (raw.Trim().Length == 0) continue;
            string p = System.Text.RegularExpressions.Regex.Replace(raw, "<[^>]+>", "").Replace("\u200B", "").Trim();
            if (p.Length == 0) continue;
            paras.Add(p); right.Add(raw.Contains("align=right"));
        }
        if (paras.Count == 0) return;
        System.Func<int, bool> isQ = i => { char c = paras[i][0]; return c == '"' || c == '\u201C' || c == '「'; };
        int d = -1;
        for (int i = paras.Count - 1; i >= 0 && i >= paras.Count - 5; i--) if (isQ(i) && !right[i]) { d = i; break; }
        if (d >= 0)
        {
            sumDlg = true; sumLine1 = paras[d];
            sumName = MolakBridge.NameOf(MolakArt.CharId) ?? "";
            for (int i = paras.Count - 1; i > d; i--) if (!isQ(i)) { sumLine2 = paras[i]; break; }
        }
        else
        {
            sumLine2 = paras[paras.Count - 1];
            if (paras.Count >= 2) sumLine1 = paras[paras.Count - 2];
        }
    }

    GUIStyle sSumN, sSum1, sSum2; float sumU = -1f;
    /// <summary>글이 rows 줄을 넘으면 끝을 줄여 …를 붙인다.</summary>
    static string Fit (GUIStyle st, string t, float w, int rows)
    {
        if (string.IsNullOrEmpty(t)) return t;
        float lh = st.CalcHeight(new GUIContent("가"), w), max = lh * rows + 1f;
        if (st.CalcHeight(new GUIContent(t), w) <= max) return t;
        int lo = 1, hi = t.Length;
        while (lo < hi) { int m = (lo + hi + 1) / 2; if (st.CalcHeight(new GUIContent(t.Substring(0, m) + "…"), w) <= max) lo = m; else hi = m - 1; }
        return t.Substring(0, lo) + "…";
    }
    void DrawSummary ()
    {
        if (MolakTitle.Showing) { thHold = false; sumLine1 = ""; sumLine2 = ""; return; }   // 0.4.85 처음 화면으로 돌아오면 남아 있던 선택지 위 요약이 제목 위에 떴다
        if (!MolakTheme.TheaterNow || !thHold || TheaterTextGui.width < 2f || MolakMenu.IsOpen) return;
        if (sumLine1.Length == 0 && sumLine2.Length == 0) return;
        float u = MolakTheme.U;
        if (sSum1 == null || !Mathf.Approximately(sumU, u))
        {
            sumU = u;
            sSum1 = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(36 * u), wordWrap = true, richText = false, alignment = TextAnchor.UpperLeft, clipping = TextClipping.Overflow };
            sSum2 = new GUIStyle(sSum1) { fontSize = Mathf.RoundToInt(30 * u) };
            sSumN = new GUIStyle(sSum1) { wordWrap = false };
            var gw = MolakGui.Gowun; if (gw != null) { sSum1.font = gw; sSum2.font = gw; }
            sSumN.font = MolakTypography.Heading(gw);
            MolakGui.Ink(sSum1, new Color(0.93f, 0.89f, 0.83f, 1f));
            MolakGui.Ink(sSum2, new Color(0.75f, 0.70f, 0.62f, 1f));
            MolakGui.Ink(sSumN, new Color(0.88f, 0.48f, 0.53f, 1f));
        }
        var r = TheaterTextGui; float x = r.x, w = r.width;
        float y = Mathf.Min(r.y, thSumTopC * MolakLayout.CanvasPx);
        if (y < r.y - 1f)
        {   // 그림 창에 걸쳤으면 뒤를 먹으로 눌러 읽히게(위로 갈수록 스러진다)
            float bandTop = y - 40 * u, full = Mathf.Min(r.y, y + (MolakLayout.ThSummaryH - 10f) * MolakLayout.CanvasPx) - bandTop;   // 선택지(uGUI)를 덮지 않게 요약 칸까지만
            var g0 = GUI.color;
            for (int i = 0; i < 12; i++) { float f = i / 11f; GUI.color = new Color(0.067f, 0.059f, 0.055f, 0.92f * Mathf.SmoothStep(0f, 1f, f)); GUI.DrawTexture(new Rect(0, bandTop + 40 * u * f, Screen.width, 40 * u / 11f + 1f), MolakGui.Px); }
            GUI.color = new Color(0.067f, 0.059f, 0.055f, 0.92f); GUI.DrawTexture(new Rect(0, bandTop + 40 * u, Screen.width, full - 40 * u), MolakGui.Px);
            GUI.color = g0;
        }
        if (sumLine1.Length > 0)
        {
            float nx = 0f;
            if (sumDlg && sumName.Length > 0) { nx = sSumN.CalcSize(new GUIContent(sumName)).x + 18 * u; GUI.Label(new Rect(x, y, nx, 60 * u), sumName, sSumN); }
            string t1 = Fit(sSum1, sumLine1, w - nx, sumDlg ? 2 : 1);
            float h1 = sSum1.CalcHeight(new GUIContent(t1), w - nx);
            GUI.Label(new Rect(x + nx, y, w - nx, h1), t1, sSum1);
            y += h1 + 6 * u;
        }
        if (sumLine2.Length > 0)
        {
            string t2 = Fit(sSum2, sumLine2, w, 2);
            GUI.Label(new Rect(x, y, w, sSum2.CalcHeight(new GUIContent(t2), w)), t2, sSum2);
        }
    }

    void PlacePrinter ()
    {
        float vpH = Mathf.Max(1f, viewport.rect.height);
        float prH = printer.rect.height;
        contentOffset = Mathf.Max(0f, prH - vpH);
        userScroll = Mathf.Clamp(userScroll, 0f, contentOffset);
        // 0.4.98 점검: 손으로 올리지 않았는데 쪽이 넘쳐 글이 밀려 올라간 자리를 적는다(MolakFlow 의 쪽 넘김 어림이 모자란 곳)
        if (contentOffset > 0.5f && userScroll < 0.5f && !MolakPageBack.Viewing)
        {
            string at = MolakFlow.LastDecision ?? "";
            if (at != lastOvfAt) { lastOvfAt = at; OverflowCount++; if (OverflowLog.Length < 6000) OverflowLog.AppendLine(Mathf.RoundToInt(contentOffset) + "px · 칸 " + Mathf.RoundToInt(vpH) + " / 어림 " + Mathf.RoundToInt(TargetBodyHeight()) + " · " + at); }
        }
        float y = contentOffset - userScroll;     // 양수 = 위로 밀어 올림 (맨 아래 정렬)
        // 맨 위 줄이 반쯤 잘려 보이지 않게: 뷰포트 윗변에 걸친 줄은 통째로 위로 밀어 올린다(아래에 한 줄 미만의 빈자리가 남는다)
        // 0.4.54: 줄 잘림 보정은 본문 높이·뷰포트 높이가 바뀔 때만 다시 잰다. 매 프레임 직전 위치를 기준으로 다시 재면
        // 두 자리 사이를 번갈아 오가며 글이 위아래로 떨렸다(언메이커: 글자 수가 많으면 위아래로 흔들린다).
        if (userScroll < 0.5f && contentOffset > 0.5f)
        {
            int key = Mathf.RoundToInt(prH) * 10007 + Mathf.RoundToInt(vpH);
            if (key != cutKey) { cutKey = key; cutVal = TopLineCut(contentOffset); }
            y += cutVal;
        }
        else cutKey = -1;
        y -= over;   // 고무줄(줄 잘림 계산 뒤에 더해, 당기는 동안 줄 맞춤이 흔들리지 않게)
        if (Mathf.Abs(printer.anchoredPosition.y - y) > 0.01f || Mathf.Abs(printer.offsetMin.x) > 0.01f || Mathf.Abs(printer.offsetMax.x) > 0.01f)
        {
            printer.offsetMin = new Vector2(0f, printer.offsetMin.y);
            printer.offsetMax = new Vector2(0f, y);
        }
    }

    public static int OverflowCount; public static readonly System.Text.StringBuilder OverflowLog = new System.Text.StringBuilder(); string lastOvfAt;
    int cutKey = -1; float cutVal;
    TMPro.TMP_Text dlg;
    /// <summary>현재 배치(printer 가 y 만큼 올라간 상태)를 기준으로, 뷰포트 윗변에 걸친 줄을 완전히 가리려면 더 올려야 할 양(캔버스 단위).</summary>
    float TopLineCut (float y)
    {
        if (dlg == null || !dlg.isActiveAndEnabled)
        {
            dlg = null;
            foreach (var t0 in printer.GetComponentsInChildren<TMPro.TMP_Text>(false)) if (t0.gameObject.name == "DialogueText") { dlg = t0; break; }
            if (dlg == null) return 0f;
        }
        var ti = dlg.textInfo; if (ti == null || ti.lineCount == 0) return 0f;
        var rc = viewport.GetComponentInParent<Canvas>(); if (rc == null) return 0f;
        float scale = rc.rootCanvas.transform.lossyScale.y; if (scale <= 0f) return 0f;
        float vpTop = viewport.TransformPoint(new Vector3(0f, viewport.rect.yMax, 0f)).y;
        // 지금 적용된 위치와 이번에 놓을 위치(y)의 차이만큼 줄 위치를 옮겨서 판단
        float shiftWorld = (y - printer.offsetMax.y) * scale;
        float cut = 0f; int first = -1;
        for (int i = 0; i < ti.lineCount; i++)
        {
            var li = ti.lineInfo[i];
            if (li.characterCount == 0) continue;
            float top = dlg.transform.TransformPoint(new Vector3(0f, li.ascender, 0f)).y + shiftWorld;
            float bot = dlg.transform.TransformPoint(new Vector3(0f, li.descender, 0f)).y + shiftWorld;
            if (top > vpTop + 0.5f && bot < vpTop - 0.5f) { cut = (vpTop - bot) / scale + 2f; first = i + 1; break; }   // 걸친 줄: 아랫변이 뷰포트 윗변을 넘도록
            if (top <= vpTop) { first = i; break; }   // 위에서부터 훑다가 완전히 보이는 줄을 만나면 끝
        }
        if (first <= 0 || first >= ti.lineCount) return cut;
        // 맨 윗줄이 문장 중간(줄바꿈으로 이어진 줄)이면, 두 줄 안에 문단 첫 줄이 있을 때 그 줄부터 보이게 더 올린다 — "…잡아가는 징세조다." 처럼 토막 난 첫 줄 방지
        if (ParaStart(ti, first)) return cut;
        for (int j = first + 1; j <= first + 2 && j < ti.lineCount; j++)
        {
            if (!ParaStart(ti, j)) continue;
            float prevBot = dlg.transform.TransformPoint(new Vector3(0f, ti.lineInfo[j - 1].descender, 0f)).y + shiftWorld;
            return Mathf.Max(cut, (vpTop - prevBot) / scale + 2f);
        }
        return cut;
    }

    static bool ParaStart (TMPro.TMP_TextInfo ti, int line)
    {
        int c = ti.lineInfo[line].firstCharacterIndex;
        if (c <= 0) return true;
        char p = ti.characterInfo[c - 1].character;
        return p == '\n' || p == '\r' || p == '\u2029';
    }

    // 위로 올려 본 상태 표시 — 뷰포트 아래쪽에 '최신으로' 힌트
    GUIStyle st; float builtU = -1f;
    void OnGUI ()
    {
        DrawSummary();
        if (!ScrolledUp || viewport == null || MolakMenu.IsOpen) return;
        float u = MolakTheme.U;
        if (st == null || !Mathf.Approximately(builtU, u))
        {
            builtU = u;
            st = new GUIStyle(GUI.skin.label){ fontSize = Mathf.RoundToInt(24 * u), alignment = TextAnchor.MiddleCenter };
            var gowun = MolakGui.Gowun; if (gowun != null) st.font = gowun;
            MolakGui.Ink(st, MolakTheme.Ink3);
        }
        
        // 뷰포트 하단 중앙(스크린 좌표)
        var corners = new Vector3[4]; viewport.GetWorldCorners(corners);
        var cam = viewport.GetComponentInParent<Canvas>().rootCanvas.worldCamera;
        var b0 = RectTransformUtility.WorldToScreenPoint(cam, corners[0]); var b3 = RectTransformUtility.WorldToScreenPoint(cam, corners[3]);
        float cx = (b0.x + b3.x) / 2f, gy = Screen.height - b0.y;
        float w = 220 * u, h = 44 * u;
        var r = new Rect(cx - w/2f, gy - h - 6*u, w, h);
        var g = GUI.color; GUI.color = new Color(0.06f, 0.05f, 0.045f, 0.78f); GUI.DrawTexture(r, MolakGui.Px); GUI.color = g;
        var tc = st.normal.textColor; MolakGui.Ink(st, new Color(0.93f, 0.9f, 0.85f, 1f));
        GUI.Label(r, "▽ 최신 대사로", st);
        MolakGui.Ink(st, tc);
        MolakInputGuard.Guard(r);
        if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition)) { userScroll = 0f; Event.current.Use(); }
    }
}
