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

    // 드래그 상태 (MolakInputGuard가 참조)
    public static bool Dragging { get; private set; }
    public static bool DraggedThisPress { get; private set; }
    public static bool ScrolledUp => inst != null && inst.userScroll > 1f;
    bool pressing; Vector2 pressPos; float pressScroll; bool pressInBottomZone;

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

    static float CanvasScale () => Mathf.Min(Screen.width / 900f, Screen.height / 1600f);

    bool Ensure ()
    {
        if (viewport != null && printer != null) return true;
        var panel = GameObject.Find("Naninovel<Runtime>/UI/TextPrinter/Dialogue/Content/PrinterPanel");
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
            vpT.offsetMin = new Vector2(70f, 150f); vpT.offsetMax = new Vector2(-70f, -215f);
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
        Layout();
    }

    void HandleDrag ()
    {
        bool down = false; Vector2 pos = Vector2.zero;
        var ts = Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.isPressed) { down = true; pos = ts.primaryTouch.position.ReadValue(); }
        else if (Mouse.current != null && Mouse.current.leftButton.isPressed) { down = true; pos = Mouse.current.position.ReadValue(); }
        if (down && !pressing)
        {
            pressing = true; pressPos = pos; pressScroll = userScroll; DraggedThisPress = false; Dragging = false;
            // 화면 맨 아래 띠(선택지 아래 150캔버스단위)에서 시작한 누름 → 위로 쓸면 행낭
            float zone = (MolakTheme.ChoiceCount > 0 ? 150f : 280f) * CanvasScale();
            pressInBottomZone = pos.y < zone && MolakStats.Get("day") >= 1f && !MolakMenu.IsOpen && !MolakBag.IsOpen;
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
            if (!Dragging && Mathf.Abs(dyPx) > DragThresholdPx && Mathf.Abs(dyPx) > Mathf.Abs(pos.x - pressPos.x)) { Dragging = true; DraggedThisPress = true; }
            if (Dragging)
            {
                float dyCanvas = dyPx / Mathf.Max(0.01f, CanvasScale());
                // 손가락을 위로 올리면 본문이 위로 가므로(최신 쪽으로) userScroll 감소, 아래로 내리면 과거로 → 증가
                userScroll = Mathf.Clamp(pressScroll - dyCanvas, 0f, Mathf.Max(0f, contentOffset));
            }
        }
        else if (!down && pressing)
        {
            pressing = false; Dragging = false;
        }
    }

    void Layout ()
    {
        float canvasW = 900f, canvasH = 1600f;
        var root = viewport.GetComponentInParent<Canvas>();
        if (root != null && root.rootCanvas != null) { var r = (root.rootCanvas.transform as RectTransform).rect; canvasW = r.width; canvasH = r.height; }
        float side = Mathf.Max(70f, (canvasW - 900f) / 2f + 70f);
        float topOff = MolakArt.BandActive ? -(MolakArt.BandTop + MolakArt.BandH + 24f) : -215f;
        topCur = Mathf.Lerp(topCur, topOff, Time.deltaTime * 6f);
        if (Mathf.Abs(topCur - topOff) < 0.5f) topCur = topOff;

        // 아래 한계: 선택지가 떠 있으면 선택지 위, 아니면 화면 하단 여백
        float bottom = 150f;   // 하단 행낭 띠(150u) 위까지
        int choices = MolakTheme.ChoiceCount;
        if (choices > 0)
        {
            var bl = GameObject.Find("Naninovel<Runtime>/UI/ChoiceHandler/ButtonList");
            var cp = bl != null ? bl.transform.Find("ContentPanel") as RectTransform : null;
            float cardH = choices >= 4 ? 122f : 148f;
            float h = cp != null && cp.rect.height > 10f ? cp.rect.height : choices * (cardH + 26f);
            bottom = 150f + h + 30f;
        }
        bottom = Mathf.Min(bottom, canvasH - 300f);
        var wantMin = new Vector2(side, bottom); var wantMax = new Vector2(-side, topCur);
        if ((viewport.offsetMin - wantMin).sqrMagnitude > 0.01f) viewport.offsetMin = wantMin;
        if ((viewport.offsetMax - wantMax).sqrMagnitude > 0.01f) viewport.offsetMax = wantMax;

        float vpH = Mathf.Max(1f, viewport.rect.height);
        float prH = printer.rect.height;
        contentOffset = Mathf.Max(0f, prH - vpH);
        userScroll = Mathf.Clamp(userScroll, 0f, contentOffset);
        float y = contentOffset - userScroll;     // 양수 = 위로 밀어 올림 (맨 아래 정렬)
        if (Mathf.Abs(printer.anchoredPosition.y - y) > 0.01f || Mathf.Abs(printer.offsetMin.x) > 0.01f || Mathf.Abs(printer.offsetMax.x) > 0.01f)
        {
            printer.offsetMin = new Vector2(0f, printer.offsetMin.y);
            printer.offsetMax = new Vector2(0f, y);
        }
    }

    // 위로 올려 본 상태 표시 — 뷰포트 아래쪽에 '최신으로' 힌트
    static Texture2D px; GUIStyle st; float builtU = -1f;
    void OnGUI ()
    {
        if (!ScrolledUp || viewport == null || MolakMenu.IsOpen) return;
        float u = MolakTheme.U;
        if (st == null || !Mathf.Approximately(builtU, u))
        {
            builtU = u;
            st = new GUIStyle(GUI.skin.label){ fontSize = Mathf.RoundToInt(24 * u), alignment = TextAnchor.MiddleCenter };
            var gowun = Resources.Load<Font>("GowunBatang"); if (gowun != null) st.font = gowun;
            st.normal.textColor = MolakTheme.Ink3;
        }
        if (px == null) { px = new Texture2D(1,1); px.SetPixel(0,0,Color.white); px.Apply(); }
        // 뷰포트 하단 중앙(스크린 좌표)
        var corners = new Vector3[4]; viewport.GetWorldCorners(corners);
        var cam = viewport.GetComponentInParent<Canvas>().rootCanvas.worldCamera;
        var b0 = RectTransformUtility.WorldToScreenPoint(cam, corners[0]); var b3 = RectTransformUtility.WorldToScreenPoint(cam, corners[3]);
        float cx = (b0.x + b3.x) / 2f, gy = Screen.height - b0.y;
        float w = 220 * u, h = 44 * u;
        var r = new Rect(cx - w/2f, gy - h - 6*u, w, h);
        var g = GUI.color; GUI.color = new Color(0.06f, 0.05f, 0.045f, 0.78f); GUI.DrawTexture(r, px); GUI.color = g;
        var tc = st.normal.textColor; st.normal.textColor = new Color(0.93f, 0.9f, 0.85f, 1f);
        GUI.Label(r, "▽ 최신 대사로", st);
        st.normal.textColor = tc;
        MolakInputGuard.Guard(r);
        if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition)) { userScroll = 0f; Event.current.Use(); }
    }
}
