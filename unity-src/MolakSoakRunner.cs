using System.Collections;
using Naninovel;
using UnityEngine;

/// <summary>정책 행렬 소크: MolakSoakRunner.Matrix(new[]{0,1,2,3,4,5}, 6, "base") — 정책마다 전역 진척을 초기화하고 새 게임부터 N회차.
/// 결과는 MolakEndTester.ResultPath(Build/soak_results.csv)에 누적. 진행 상황: MolakSoakRunner.Status</summary>
public class MolakSoakRunner : MonoBehaviour
{
    public static string Status = "idle";
    static MolakSoakRunner inst;

    public static void Matrix (int[] policies, int lives, string tag)
    {
        if (inst == null) { var go = new GameObject("MolakSoakRunner"); DontDestroyOnLoad(go); inst = go.AddComponent<MolakSoakRunner>(); }
        inst.StopAllCoroutines(); inst.StartCoroutine(inst.Run(policies, lives, tag));
    }

    IEnumerator Run (int[] policies, int lives, string tag)
    {
        var sm = Engine.GetServiceOrErr<IStateManager>();
        var sp = Engine.GetServiceOrErr<IScriptPlayer>();
        var vars = Engine.GetServiceOrErr<ICustomVariableManager>();
        for (int pi = 0; pi < policies.Length; pi++)
        {
            Status = tag + " policy " + policies[pi] + " (" + (pi + 1) + "/" + policies.Length + ")";
            var names = new System.Collections.Generic.List<string>();
            foreach (var v in vars.Variables) if (v.Name.StartsWith("g_")) names.Add(v.Name);
            foreach (var n in names) { if (n == "g_lastend" || n == "g_lastcause") MolakStats.SetStr(n, "none"); else MolakStats.Set(n, 0f); }
            for (int attempt = 0; attempt < 3; attempt++)
            {
                sm.ResetState(new string[0], () => ScriptTrackExtensions.LoadAndPlay(sp.MainTrack, "Entry"));
                yield return new WaitForSecondsRealtime(2f);
                var ps = sp.MainTrack.PlayedScript;
                if (ps != null && ps.Path != "Title") break;
            }
            MolakEndTester.Batch(policies[pi], lives, tag + "_p" + policies[pi]);
            while (!MolakEndTester.I.done) yield return new WaitForSecondsRealtime(1f);
            if (MolakEndTester.Status == "stall") { Status = "STALL in " + tag + " policy " + policies[pi]; }
            yield return new WaitForSecondsRealtime(0.5f);
        }
        Status = "done " + tag;
    }
}
