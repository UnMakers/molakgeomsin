using UnityEngine;

/// <summary>
/// IMGUI 공용 도구. 0.4.40 전까지 HUD·행낭·메뉴·설정·말로·인장·먹 연출·타이틀이 각자
/// 1픽셀 흰 텍스처·고운바탕 글꼴·화면 배율·'이번 프레임에 눌렀나'를 따로 만들어 쓰고 있었다(14벌·13벌·4벌).
/// 이제 여기 한 벌만 쓴다.
/// </summary>
public static class MolakGui
{
    // ---- 겹 순서(GUI.depth: 작을수록 위) — 예전엔 파일마다 숫자가 흩어져 있었다 ----
    /// <summary>날씨·화면 물들임(HUD·행낭 띠 위, 설정 아래)</summary>
    public const int DepthVfx = -50;
    /// <summary>환경 설정(타이틀·메뉴·본편 위)</summary>
    public const int DepthSettings = -200;
    /// <summary>갈림길 인장(@mark)</summary>
    public const int DepthMark = -2500;
    /// <summary>먹 연출(@black, 타이틀의 "눈을 뜬다.") — 맨 위</summary>
    public const int DepthBlackout = -3000;

    // ---- 색 ----
    /// <summary>먹이 화면을 덮는 색(타이틀 페이드·먹 연출 공용)</summary>
    public static readonly Color InkBlack = new Color(0.02f, 0.018f, 0.016f, 1f);
    /// <summary>핏빛 먹</summary>
    public static readonly Color InkBlood = new Color(0.10f, 0.015f, 0.012f, 1f);
    /// <summary>먹 위에 뜨는 글자(옅은 종이빛)</summary>
    public static readonly Color InkOnBlack = new Color(0.88f, 0.82f, 0.72f, 1f);

    /// <summary>화면 비율 무관 UI 배율: 1080×1920 기준 폭·높이 중 작은 쪽.</summary>
    public static float U { get { return Mathf.Min(Screen.width / 1080f, Screen.height / 1920f); } }

    static Texture2D px;
    /// <summary>1×1 흰 텍스처 — 색 상자·선을 그릴 때.</summary>
    public static Texture2D Px { get { if (px == null) { px = new Texture2D(1, 1); px.SetPixel(0, 0, Color.white); px.Apply(); px.hideFlags = HideFlags.DontSave; } return px; } }

    static Font gowun, hanja;
    /// <summary>본문·UI 글꼴(고운바탕).</summary>
    public static Font Gowun { get { if (gowun == null) gowun = Resources.Load<Font>("GowunBatang"); return gowun; } }
    /// <summary>제목·인장 한자 글꼴.</summary>
    public static Font Hanja { get { if (hanja == null) hanja = Resources.Load<Font>("MolakHanja"); return hanja; } }

    /// <summary>손가락(또는 마우스 왼쪽)이 눌려 있나 + 그 위치(화면 좌표, 아래가 0). 행낭·본문 끌기·입력 보호가 같이 쓴다.</summary>
    public static bool PointerScreen (out Vector2 pos)
    {
        var ts = UnityEngine.InputSystem.Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.isPressed) { pos = ts.primaryTouch.position.ReadValue(); return true; }
        var ms = UnityEngine.InputSystem.Mouse.current;
        if (ms != null) { pos = ms.position.ReadValue(); return ms.leftButton.isPressed; }
        var pt = UnityEngine.InputSystem.Pointer.current;
        pos = pt != null ? pt.position.ReadValue() : new Vector2(-1f, -1f);
        return false;
    }

    /// <summary>같은 것을 GUI 좌표(위가 0)로.</summary>
    public static bool PointerGui (out Vector2 gui)
    {
        Vector2 p; bool down = PointerScreen(out p);
        gui = new Vector2(p.x, Screen.height - p.y);
        return down;
    }

    /// <summary>이번 프레임에 화면(터치·마우스 왼쪽)이나 Space/Enter 를 눌렀나.</summary>
    public static bool TapDown ()
    {
        var ts = UnityEngine.InputSystem.Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.wasPressedThisFrame) return true;
        var ms = UnityEngine.InputSystem.Mouse.current;
        if (ms != null && ms.leftButton.wasPressedThisFrame) return true;
        var kb = UnityEngine.InputSystem.Keyboard.current;
        return kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame);
    }

    /// <summary>
    /// 0.4.28 글자 색 도우미. GUIStyle 은 상태(평소·마우스 올림·누름·초점)마다 글자 색을 따로 가진다.
    /// 평소(normal)에만 색을 주면, PC에서 마우스를 올리거나 누르는 순간 기본 피부의 하얀 글자로 바뀐다(행낭 띠의 "행낭"이 하얘지던 버그).
    /// 색은 언제나 이 함수로 준다.
    /// </summary>
    public static void Ink (GUIStyle s, Color c)
    {
        if (s == null) return;
        s.normal.textColor = c; s.hover.textColor = c; s.active.textColor = c; s.focused.textColor = c;
        s.onNormal.textColor = c; s.onHover.textColor = c; s.onActive.textColor = c; s.onFocused.textColor = c;
    }

    /// <summary>고운바탕 라벨 스타일 하나 만들기(크기는 1080 기준 픽셀 × u).</summary>
    public static GUIStyle Label (float size, float u, TextAnchor anchor, bool wrap = true)
    {
        var s = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(size * u), alignment = anchor, richText = true, wordWrap = wrap, clipping = TextClipping.Overflow };
        if (Gowun != null) s.font = Gowun;
        return s;
    }
}
