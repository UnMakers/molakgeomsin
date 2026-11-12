using System.Collections;
using System.Collections.Generic;
using Naninovel;
using UnityEngine;

/// <summary>
/// 시험용: 이야기 마디를 하나씩 열어 끝까지 걸어 본다. 선택지는 갈래마다 돌려 가며 고른다(pick 번째).
/// MolakBeatWalker.Walk(new[]{"Molak07#SoaN9", ...}, pick) — 결과는 Build/beat_walk.txt (마디마다 OK/STALL과 지나간 줄 수).
/// 마디가 Molak02 로 돌아오거나(AfterChapter 등) 말로·회귀에 닿으면 끝난 것으로 본다.
/// </summary>
public class MolakBeatWalker : MonoBehaviour
{
    public static string Status = "idle";
    static MolakBeatWalker inst;

    public static void Walk (string[] refs, int pick)
    {
        if (inst == null) { var go = new GameObject("MolakBeatWalker"); DontDestroyOnLoad(go); inst = go.AddComponent<MolakBeatWalker>(); }
        inst.StopAllCoroutines(); inst.StartCoroutine(inst.Run(refs, pick));
    }

    static string OutPath { get { return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), "Build", "beat_walk.txt"); } }

    IEnumerator Run (string[] refs, int pick)
    {
        var sb = new System.Text.StringBuilder();
        var pl = Engine.GetService<IScriptPlayer>();
        var tr = pl.MainTrack;
        var setWait = tr.GetType().GetMethod("SetAwaitInput");
        foreach (var r in refs)
        {
            Status = r;
            var parts = r.Split('#');
            MolakStats.Set("riskforce", (pick % 2) == 0 ? 1f : 2f);   // 승부수: 짝수 회는 통과, 홀수 회는 실패 갈래
            Naninovel.ScriptTrackExtensions.LoadAndPlayAtLabel(tr, parts[0], parts[1]);
            yield return new WaitForSecondsRealtime(0.5f);
            int steps = 0, lastIdx = -1, idle = 0, choices = 0;
            string end = "STALL";
            while (steps < 400)
            {
                steps++;
                var path = tr.PlayedScript != null ? tr.PlayedScript.Path : "";
                if (path == "Molak02" || path == "MolakOver" || path == "Molak01" && parts[0] != "Molak01") { end = "OK→" + path + "#" + tr.PlayedIndex; break; }
                if (tr.AwaitingInput) setWait.Invoke(tr, new object[] { false });
                var bl = GameObject.Find(MolakUi.ChoiceListPath);
                var btns = new List<UnityEngine.UI.Button>();
                if (bl != null) foreach (var b in bl.GetComponentsInChildren<Naninovel.UI.ChoiceHandlerButton>()) if (b.gameObject.activeInHierarchy) btns.Add(b.GetComponent<UnityEngine.UI.Button>());
                var cg = bl != null ? bl.GetComponent<CanvasGroup>() : null;
                if (btns.Count > 0 && cg != null && cg.alpha > 0.5f)
                {
                    yield return new WaitForSecondsRealtime(MolakLayout.ChoiceLock + 0.1f);
                    btns[(pick + choices) % btns.Count].onClick.Invoke(); choices++;
                    yield return new WaitForSecondsRealtime(0.3f);
                }
                if (tr.PlayedIndex == lastIdx) idle++; else { idle = 0; lastIdx = tr.PlayedIndex; }
                if (idle > 60) { end = "STALL@" + path + "#" + tr.PlayedIndex; break; }
                yield return new WaitForSecondsRealtime(0.05f);
            }
            sb.AppendLine(r + " pick" + pick + " → " + end + " (choices " + choices + ")");
        }
        MolakStats.Set("riskforce", 0f);
        try { System.IO.File.AppendAllText(OutPath, sb.ToString(), new System.Text.UTF8Encoding(false)); } catch { }
        Status = "done";
    }
}
