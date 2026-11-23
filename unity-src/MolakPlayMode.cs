using MolakVn;
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
        var spx = Engine.GetService<IScriptPlayer>();
        if (spx != null) { spx.AddPreExecutionTask(FastArtHold); spx.AddPreExecutionTask(MomentHold); }
        var cont = im.GetInput("Continue");
        if (cont != null) cont.OnStart += OnContinue;
        st.OnResetStarted += () => { Fast = false; SkipAll = false; Auto = false; };
        hooked = true;
    }

    /// <summary>0.4.65 빨리일 때 그림이 바뀌면 글을 반 박자 늦춘다(언메이커: 빨리 모드에서 그림이 너무 늦게 나오는 느낌).
    /// 한 쪽이 한꺼번에 찍히니 그림이 글 뒤에 떠오르는 것처럼 보였다 — 그림이 다 떠오를 때까지(최대 0.25초) 기다린 뒤 0.08초 더 두고 글을 찍는다.
    /// 나니노벨 순서: OnExecute(MolakFlow 가 바탕 그림을 되돌림) → 실행 전 작업(여기) → 실행. 그래서 같은 줄에서 바뀐 그림도 잡힌다.</summary>
    static async Awaitable FastArtHold (IScriptTrack track)
    {
        var pt = track != null ? track.PlayedCommand as MolakVn.Commands.PrintText : null;
        if (!Fast || pt == null) return;
        // 비어 있는 {dejapre} 줄은 아무것도 찍지 않는다 — 여기서 기다리면 정작 글 앞에서는 안 기다린다
        if (pt.Text != null && pt.Text.ToString().Trim() == "{dejapre}") { var dv = MolakStats.Str("dejapre"); if (string.IsNullOrEmpty(dv) || dv == "none" || dv == "\u200B") return; }
        var sp = Player; if (sp != null && sp.Skipping) return;
        bool changed = Time.unscaledTime - MolakArt.ChangedAt < 0.35f;
        if (!changed && !MolakArt.Busy) return;
        float until = Time.realtimeSinceStartup + 0.25f;
        while (MolakArt.Busy && Time.realtimeSinceStartup < until) await Awaitable.NextFrameAsync();
        float hold = Time.realtimeSinceStartup + 0.08f;
        while (Time.realtimeSinceStartup < hold) await Awaitable.NextFrameAsync();
    }

    /// <summary>0.4.68 인장·비무 결과판이 떠 있는 동안 다음 글·선택지를 붙잡는다(빈 글자리에 먼저 보이고, 누르면 걷힌 뒤 대사가 나온다).</summary>
    static async Awaitable MomentHold (IScriptTrack track)
    {
        var cmd = track != null ? track.PlayedCommand : null;
        if (cmd == null) return;
        // 인장·결과판이 떠 있으면 다음 명령은 무엇이든 기다린다(그 사이 다른 인장이 겹쳐 뜨지 않게)
        float until = Time.realtimeSinceStartup + 30f;
        while ((MolakVfx.Holding || MolakDuelCard.Visible) && Time.realtimeSinceStartup < until) await Awaitable.NextFrameAsync();
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
