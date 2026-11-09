using System.Collections.Generic;
using Naninovel;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// OnGUI로 그린 자체 UI(HUD·☰·말로 카드 등) 위를 누를 때 Naninovel의 '계속' 입력이 같이 먹히지 않도록
/// 포인터가 보호 영역 안에 있는 동안 Continue/Skip 입력을 음소거한다. 영역은 각 UI가 매 프레임 등록한다.
/// </summary>
[DefaultExecutionOrder(-500)]
public class MolakInputGuard : MonoBehaviour
{
    static MolakInputGuard inst;
    static readonly List<Rect> rects = new List<Rect>();   // GUI 좌표(좌상단 원점)
    static int rectFrame = -1;
    bool muted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot ()
    {
        if (inst) return;
        var go = new GameObject("MolakInputGuard"); DontDestroyOnLoad(go);
        inst = go.AddComponent<MolakInputGuard>();
    }

    /// <summary>GUI 좌표계 Rect(좌상단 원점) 등록 — OnGUI 안에서 매 프레임 호출.</summary>
    public static void Guard (Rect guiRect)
    {
        if (rectFrame != Time.frameCount) { rects.Clear(); rectFrame = Time.frameCount; }
        rects.Add(guiRect);
    }

    static bool PointerDown ()
    {
        var ts = Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.isPressed) return true;
        var ms = Mouse.current;
        return ms != null && ms.leftButton.isPressed;
    }

    static Vector2 PointerGui ()
    {
        Vector2 p = new Vector2(-1f, -1f);
        var ts = Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.isPressed) p = ts.primaryTouch.position.ReadValue();
        else if (Mouse.current != null) p = Mouse.current.position.ReadValue();
        else if (Pointer.current != null) p = Pointer.current.position.ReadValue();
        return new Vector2(p.x, Screen.height - p.y);
    }

    bool wasDownInside;
    bool wasDown;            // 직전 프레임 포인터 눌림
    bool pressBlocked;       // 이번 누름은 '계속'으로 처리하지 않음(보호 영역에서 시작 / 드래그)
    int releaseFrame = -1;   // 합성 Continue 해제 예약

    void Update ()
    {
        if (!Engine.Initialized) return;
        var im = Engine.GetService<IInputManager>();
        if (im == null) return;
        var cont = im.GetInput("Continue");
        var skip = im.GetInput("Skip");
        bool inside = false;
        var gp = PointerGui();
        if (rectFrame >= Time.frameCount - 2)
            foreach (var r in rects) if (r.Contains(gp)) { inside = true; break; }
        bool down = PointerDown();

        // 합성 탭 해제(한 프레임 뒤)
        if (releaseFrame >= 0 && Time.frameCount >= releaseFrame) { if (cont != null) cont.Activate(Vector2.zero); releaseFrame = -1; }

        if (down && !wasDown) { pressBlocked = inside; wasDownInside = inside; }
        if (down && inside) wasDownInside = true;
        if (down && MolakScroll.DraggedThisPress) pressBlocked = true;

        // 포인터를 누르고 있는 동안은 항상 음소거 — '계속'은 뗄 때(드래그가 아니었을 때만) 합성으로 보낸다.
        bool want = down || inside || wasDownInside;
        if (want != muted)
        {
            muted = want;
            if (cont != null) cont.Muted = muted;
            if (skip != null) skip.Muted = muted;
        }

        if (!down && wasDown)
        {
            bool tap = !pressBlocked && !MolakScroll.DraggedThisPress && !MolakMenu.IsOpen;
            wasDownInside = false; pressBlocked = false;
            if (tap && cont != null)
            {
                // 음소거를 먼저 풀고 같은 프레임에 누름 → 다음 프레임에 뗌
                muted = false; cont.Muted = false; if (skip != null) skip.Muted = false;
                cont.Activate(Vector2.one);
                releaseFrame = Time.frameCount + 1;
            }
        }
        wasDown = down;
    }
}
