using Naninovel;
using UnityEngine;

/// <summary>
/// 0.4.46 메뉴의 '자동'·'빨리'를 켜 둔 동안 유지한다.
/// 나니노벨은 다른 대본으로 넘어갈 때마다(사건 하나 들어갈 때마다) 자동·넘기기를 끄고,
/// 기본 넘기기 방식이 '읽은 글만'이라 처음 보는 줄에서도 넘기기를 끈다.
/// 그래서 '빨리'를 켜면 그 쪽만 한 번에 나오고 다음 쪽부터 다시 한 글자씩 나왔다.
///  · 빨리: 처음 보는 글까지 넘긴다(선택지에서는 멈추고, 고르면 다시 넘긴다). 화면을 누르면 멈춘다. 메뉴에서 끌 수 있다.
///  · 자동: 메뉴에서 끌 때까지 유지.
///  · '읽은 글 ▶▶'는 예전처럼 처음 보는 글에서 멈춘다.
///  · 새 생·불러오기(상태 초기화) 때는 둘 다 끈다.
/// 0.4.64 빨리 = 한 쪽씩(언메이커: 다 넘어가는 게 아니라 한 페이지만). 넘기기를 쓰지 않는다.
///  · 한 쪽 안의 글은 타자 없이 한꺼번에 나오고(PrintText 속도 ×100), 쪽 안의 단락마다 기다리지 않는다.
///  · 쪽 끝(다음 글이 쪽을 넘기거나 선택지·장면 전환이 오면)에서는 기다린다 — 누르면 다음 쪽이 한꺼번에.
///  · 판단은 MolakFlow(쪽 나눔을 하는 곳)가 한다. 검증 도구가 쓰던 '끝까지 넘기기'는 SetSkipAll 로 남겼다.
/// </summary>
public class MolakPlayMode : MonoBehaviour
{
    static MolakPlayMode inst;
    public static bool Fast { get; private set; }
    /// <summary>검증 도구용 '끝까지 넘기기'(예전의 빨리). 선택지에서만 멈춘다.</summary>
    public static bool SkipAll { get; private set; }
    public static bool Auto { get; private set; }

    bool hooked;
    int stopFrame = -100;   // 사람이 화면을 누른 프레임(시간으로 재면 프레임이 밀릴 때 놓친다)
    float uiOpenAt = -10f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot ()
    {
        if (inst) return;
        var go = new GameObject("MolakPlayMode"); DontDestroyOnLoad(go);
        inst = go.AddComponent<MolakPlayMode>();
    }

    static IScriptPlayer Player { get { return Engine.Initialized ? Engine.GetService<IScriptPlayer>() : null; } }

    /// <summary>메뉴의 '빨리' — 한 쪽씩 한꺼번에(넘기기 없이).</summary>
    public static void SetFast (bool on)
    {
        Fast = on;
        if (on && SkipAll) SetSkipAll(false);
    }

    /// <summary>검증 도구용: 처음 보는 글까지 끝까지 넘긴다(선택지에서 멈춤).</summary>
    public static void SetSkipAll (bool on)
    {
        var sp = Player;
        SkipAll = on;
        if (inst != null) inst.stopFrame = -100;
        if (sp == null) return;
        sp.SkipMode = on ? PlayerSkipMode.Everything : PlayerSkipMode.ReadOnly;
        sp.SetSkip(on);
    }

    public static void SetAuto (bool on)
    {
        var sp = Player;
        Auto = on;
        if (sp != null) sp.SetAutoPlay(on);
    }

    /// <summary>'읽은 글 ▶▶' — 처음 보는 글에서 멈추는 한 번짜리 넘기기(유지하지 않는다).</summary>
    public static void SkipRead ()
    {
        var sp = Player;
        SkipAll = false;
        if (sp == null) return;
        sp.SkipMode = PlayerSkipMode.ReadOnly;
        sp.SetSkip(true);
    }

    void Hook ()
    {
        if (hooked || !Engine.Initialized) return;
        var im = Engine.GetService<IInputManager>();
        var st = Engine.GetService<IStateManager>();
        if (im == null || st == null) return;
        var cont = im.GetInput("Continue");
        if (cont != null) cont.OnStart += OnContinue;
        st.OnResetStarted += () => { Fast = false; SkipAll = false; Auto = false; };
        hooked = true;
    }

    void OnContinue ()
    {
        // 메뉴·설정을 닫는 탭이 '계속'으로 새어 들어온 것은 사람이 멈춘 게 아니다
        if (!SkipAll || Time.unscaledTime - uiOpenAt < 0.4f || MolakMenu.IsOpen || MolakSettings.IsOpen) return;
        stopFrame = Time.frameCount;
    }

    void Update ()
    {
        Hook();
        if (MolakMenu.IsOpen || MolakSettings.IsOpen) uiOpenAt = Time.unscaledTime;
        var sp = Player;
        if (sp == null) return;

        if (SkipAll && !sp.Skipping)
        {
            if (Time.frameCount - stopFrame <= 3) { SkipAll = false; sp.SkipMode = PlayerSkipMode.ReadOnly; stopFrame = -100; }
            else { if (sp.SkipMode != PlayerSkipMode.Everything) sp.SkipMode = PlayerSkipMode.Everything; sp.SetSkip(true); }
        }
        if (Auto && !sp.AutoPlaying) sp.SetAutoPlay(true);
    }
}
