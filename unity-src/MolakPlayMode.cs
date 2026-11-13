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
/// </summary>
public class MolakPlayMode : MonoBehaviour
{
    static MolakPlayMode inst;
    public static bool Fast { get; private set; }
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

    public static void SetFast (bool on)
    {
        var sp = Player;
        Fast = on;
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
        Fast = false;
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
        st.OnResetStarted += () => { Fast = false; Auto = false; };
        hooked = true;
    }

    void OnContinue ()
    {
        // 메뉴·설정을 닫는 탭이 '계속'으로 새어 들어온 것은 사람이 멈춘 게 아니다
        if (!Fast || Time.unscaledTime - uiOpenAt < 0.4f || MolakMenu.IsOpen || MolakSettings.IsOpen) return;
        stopFrame = Time.frameCount;
    }

    void Update ()
    {
        Hook();
        if (MolakMenu.IsOpen || MolakSettings.IsOpen) uiOpenAt = Time.unscaledTime;
        var sp = Player;
        if (sp == null) return;

        if (Fast && !sp.Skipping)
        {
            if (Time.frameCount - stopFrame <= 3) { Fast = false; sp.SkipMode = PlayerSkipMode.ReadOnly; stopFrame = -100; }
            else { if (sp.SkipMode != PlayerSkipMode.Everything) sp.SkipMode = PlayerSkipMode.Everything; sp.SetSkip(true); }
        }
        if (Auto && !sp.AutoPlaying) sp.SetAutoPlay(true);
    }
}
