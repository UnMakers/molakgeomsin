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

    static bool PointerDown () { Vector2 p; return MolakGui.PointerScreen(out p); }
    static Vector2 PointerGui () { Vector2 g; MolakGui.PointerGui(out g); return g; }

    static readonly List<UnityEngine.EventSystems.RaycastResult> hits = new List<UnityEngine.EventSystems.RaycastResult>();
    static bool OverButton (Vector2 gui)
    {
        var es = UnityEngine.EventSystems.EventSystem.current;
        if (es == null) return false;
        var ped = new UnityEngine.EventSystems.PointerEventData(es) { position = new Vector2(gui.x, Screen.height - gui.y) };
        hits.Clear(); es.RaycastAll(ped, hits);
        foreach (var h in hits)
        {
            if (h.gameObject == null) continue;
            var sel = h.gameObject.GetComponentInParent<UnityEngine.UI.Selectable>();
            if (sel != null && sel.isActiveAndEnabled && sel.interactable) return true;
        }
        return false;
    }

    float muteUntil = -1f;  // 막힌 누름을 뗀 뒤 잠깐 '계속' 음소거
    /// <summary>0.4.39: 지금부터 s초 동안 '계속'을 받지 않는다(선택지 직후 첫 문단이 어떤 입력으로도 통째로 찍히지 않게).</summary>
    public static void MuteFor (float s)
    {
        if (inst == null) return;
        inst.muteUntil = Mathf.Max(inst.muteUntil, Time.unscaledTime + s);
        try
        {
            var im = Engine.GetService<IInputManager>(); if (im == null) return;
            var c = im.GetInput("Continue"); if (c != null) c.Muted = true;
            var k = im.GetInput("Skip"); if (k != null) k.Muted = true;
            inst.muted = true;
        }
        catch { }
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

        // 0.4.38: 선택지 카드(uGUI 단추) 위에서 시작한 누름은 '계속'으로 보내지 않는다 — 고르는 탭이 선택지 뒤 첫 문단까지
        //         한꺼번에 다 찍어 버려(한 글자씩 나오지 않고 통째로 뜨던 것) 생기던 일.
        if (down && !wasDown) { bool onBtn = OverButton(gp); pressBlocked = inside || onBtn; wasDownInside = inside; }
        if (down && inside) wasDownInside = true;
        if (down && MolakScroll.DraggedThisPress) pressBlocked = true;

        // 포인터를 누르고 있는 동안은 항상 음소거 — '계속'은 뗄 때(드래그가 아니었을 때만) 합성으로 보낸다.
        // 0.4.39: 막힌 누름(선택지 카드 등)을 뗀 뒤에도 잠깐(0.35초) 더 음소거 — 기기에서는 나니노벨 자체 터치 입력이
        //         손을 떼는 프레임에 '계속'을 읽어, 선택지 뒤 첫 문단이 통째로 찍히던 것.
        if (!down && wasDown && pressBlocked) muteUntil = Time.unscaledTime + 0.35f;
        bool want = down || inside || wasDownInside || Time.unscaledTime < muteUntil;
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
