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
    bool hSwipe; Vector2 lastPos;   // 0.4.57 가로로 쓸기: 왼쪽→오른쪽이면 지난 쪽(MolakPageBack)

    const float DragThresholdPx = 14f;

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
        float top = MolakArt.HasArt ? (MolakArt.BandTop + MolakArt.BandH + MolakLayout.BelowBand) : MolakLayout.BodyTop;
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
        if (!Dragging && over != 0f)
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            over = Mathf.Lerp(over, 0f, 1f - Mathf.Pow(0.0004f, dt));   // 약 0.3초에 제자리로
            if (Mathf.Abs(over) < 0.5f) over = 0f;
        }
        Layout();
    }

    void HandleDrag ()
    {
        Vector2 pos; bool down = MolakGui.PointerScreen(out pos);
        if (down && !pressing)
        {
            pressing = true; pressPos = pos; lastPos = pos; pressScroll = userScroll; DraggedThisPress = false; Dragging = false; hSwipe = false;
            // 화면 맨 아래 띠(선택지 아래 150캔버스단위)에서 시작한 누름 → 위로 쓸면 행낭
            float zone = (MolakTheme.ChoiceCount > 0 ? MolakLayout.BagSwipeZoneWithChoices : MolakLayout.BagSwipeZone) * CanvasScale();
            pressInBottomZone = pos.y < zone && MolakStats.Get("day") >= 1f && !MolakMenu.IsOpen && !MolakBag.IsOpen && !MolakBlackout.Busy;
        }
        else if (down && pressing)
        {
            float dyPx = pos.y - pressPos.y;   // 위로 쓸면 +
            if (MolakBag.IsOpen)
            {
                // 서랍이 열려 있으면 본문 스크롤 대신: 아래로 쓸면 닫기
                // 아래로 쓸어 닫기: 목록이 맨 위에 있거나, 서랍 머리(손잡이·제목) 부근에서 시작한 누름이면
                float headerY = Screen.height - MolakBag.PanelTopPx;   // 패널 윗변(스크린 좌표)
                bool fromHeader = pressPos.y >= headerY - 130f * CanvasScale();
                if (dyPx < -36f && (MolakBag.ListAtTop || fromHeader)) { MolakBag.Close(); DraggedThisPress = true; }
                if (Mathf.Abs(dyPx) > DragThresholdPx) DraggedThisPress = true;
                return;
            }
            if (pressInBottomZone)
            {
                if (dyPx > 22f) { MolakBag.Open(); DraggedThisPress = true; pressInBottomZone = false; }
                return;
            }
            lastPos = pos;
            float dxPx = pos.x - pressPos.x;
            if (!Dragging && !hSwipe && Mathf.Abs(dxPx) > 34f * CanvasScale() && Mathf.Abs(dxPx) > Mathf.Abs(dyPx) * 1.4f
                && MolakStats.Get("day") >= 1f && !MolakMenu.IsOpen && !MolakGuide.IsOpen && !MolakBlackout.Busy && !MolakArt.InDuel)
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
        var root = viewport.GetComponentInParent<Canvas>();
        if (root != null && root.rootCanvas != null) { var r = (root.rootCanvas.transform as RectTransform).rect; canvasW = r.width; canvasH = r.height; }
        float side = Mathf.Max(MolakLayout.BodySide, (canvasW - MolakLayout.Canvas.x) / 2f + MolakLayout.BodySide);
        float topOff = MolakArt.BandActive ? -(MolakArt.BandTop + MolakArt.BandH + MolakLayout.BelowBand) : -MolakLayout.BodyTop;
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

        float vpH = Mathf.Max(1f, viewport.rect.height);
        float prH = printer.rect.height;
        contentOffset = Mathf.Max(0f, prH - vpH);
        userScroll = Mathf.Clamp(userScroll, 0f, contentOffset);
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
