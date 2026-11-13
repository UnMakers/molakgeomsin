using System.Collections.Generic;
using Naninovel;
using UnityEngine;

/// <summary>
/// 0.4.55 비무 연습 — 타이틀의 「비무 연습」. 목숨도 서열도 걸리지 않는 판에서 합만 겨뤄 본다.
///  · 들어갈 때 전역(g_) 변수를 통째로 적어 두고, 나올 때(연습을 마치거나 '처음 화면으로') 그대로 되돌린다.
///    연습에서 받은 필살·진 상대의 기억이 본편에 새어 들지 않는다.
///  · 연습 중에는 자동 기록·빠른 저장을 하지 않는다(이어하기 기록을 덮지 않게).
///  · 청운검결 넷은 모두 쓸 수 있다(practice 변수 — MolakMemory.HasForm).
/// 대본: MolakDuel.nani #Practice.
/// </summary>
public static class MolakPractice
{
    public static bool Active { get; private set; }
    static readonly List<KeyValuePair<string, CustomVariableValue>> snap = new List<KeyValuePair<string, CustomVariableValue>>();
    static bool hooked;

    public static void Start ()
    {
        if (!Engine.Initialized) return;
        var vars = Engine.GetService<ICustomVariableManager>();
        var sp = Engine.GetService<IScriptPlayer>();
        var st = Engine.GetService<IStateManager>();
        if (vars == null || sp == null || st == null) return;
        InitRunCommand.EnsureGlobals();
        snap.Clear();
        foreach (var v in vars.Variables) if (v.Name.StartsWith("g_")) snap.Add(new KeyValuePair<string, CustomVariableValue>(v.Name, v.Value));
        if (!hooked) { st.OnResetStarted += OnReset; hooked = true; }
        Active = true;
        st.ResetState(new string[0], () =>
        {
            Active = true;
            return ScriptTrackExtensions.LoadAndPlayAtLabel(sp.MainTrack, "MolakDuel", "Practice");
        });
    }

    static bool restoring;
    static void OnReset ()
    {
        if (!Active || restoring) return;
        // 연습 판을 시작하는 그 초기화(Start 안)는 건너뛴다 — Active 를 켜기 전에 연결하므로, 연습 중의 초기화만 여기로 온다
        if (MolakStats.Get("practice") < 1f) return;
        Restore();
    }

    static void Restore ()
    {
        restoring = true;
        try
        {
            var vars = Engine.GetService<ICustomVariableManager>();
            if (vars != null)
            {
                var keep = new HashSet<string>();
                foreach (var kv in snap) { vars.SetVariableValue(kv.Key, kv.Value); keep.Add(kv.Key); }
                // 연습 중에 새로 생긴 전역은 비운다
                var extra = new List<(string, CustomVariableValueType)>();
                foreach (var v in vars.Variables) if (v.Name.StartsWith("g_") && !keep.Contains(v.Name)) extra.Add((v.Name, v.Value.Type));
                foreach (var (n, ty) in extra) vars.SetVariableValue(n, ty == CustomVariableValueType.String ? new CustomVariableValue("none") : new CustomVariableValue(0f));
            }
        }
        catch (System.Exception e) { Debug.LogWarning("[MolakPractice] " + e.Message); }
        Active = false;
        restoring = false;
    }

    /// <summary>연습을 마치고 처음 화면으로.</summary>
    public static void End ()
    {
        Restore();
        var st = Engine.GetService<IStateManager>();
        if (st != null) st.ResetState();
    }

    /// <summary>몸 고르기: 1 갓 깨어난 몸 · 2 한 달을 버틴 몸 · 3 교주 앞에 선 몸.</summary>
    public static void Body (int b)
    {
        float sw = b == 1 ? 8 : b == 2 ? 20 : 34, ng = b == 1 ? 3 : b == 2 ? 10 : 22, hp = b == 1 ? 12 : b == 2 ? 16 : 20;
        MolakStats.Set("pbody", b);
        MolakStats.Set("sw", sw); MolakStats.Set("ng", ng);
        MolakStats.Set("hpmax", hp); MolakStats.Set("hp", hp);
        MolakStats.Set("sl", b == 1 ? 1 : b == 2 ? 3 : 5);
        MolakStats.Set("curse", b == 1 ? 15 : b == 2 ? 35 : 55);
        MolakStats.Set("day", b == 1 ? 5 : b == 2 ? 26 : 48);
        MolakStats.Set("rank", b == 1 ? 0 : b == 2 ? 30000 : 900);
        MolakStats.Set("mn", 30);
        MolakStats.Set("injury", 0); MolakStats.Set("taint", 0);
        MolakStats.Set("memwarn", 0);
    }
}

/// <summary>@practice do:begin|heal|guide|end  /  @practice body:1~3</summary>
[Alias("practice")]
public class PracticeCommand : Command
{
    public StringParameter Do;
    public IntegerParameter Body;

    public override async Awaitable Execute (ExecutionContext ctx)
    {
        if (Assigned(Body)) { MolakPractice.Body(Body.Value); return; }
        string d = Assigned(Do) ? Do.Value : "";
        if (d == "begin")
        {
            InitRunCommand.EnsureGlobals();
            foreach (var n in MolakVarList.Nums) MolakStats.EnsureNum(n);       // 본편은 서장 첫머리에서 회차 변수를 모두 만든다 — 연습 판은 여기서
            foreach (var n in MolakVarList.Strings) MolakStats.EnsureStr(n);
            MolakStats.Set("practice", 1f);
            MolakStats.SetStr("deathcause", "none");
            MolakPractice.Body(2);
        }
        else if (d == "heal")
        {
            int b = Mathf.RoundToInt(MolakStats.Get("pbody"));
            MolakPractice.Body(b < 1 ? 2 : b);
        }
        else if (d == "guide")
        {
            MolakDuelStage.OpenGuide();
            while (MolakDuelStage.GuideOpen) await Awaitable.NextFrameAsync();
        }
        else if (d == "end") MolakPractice.End();
    }
}
