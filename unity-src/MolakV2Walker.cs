using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MolakVn;
using UnityEngine;

/// 새 이야기(V2) 시험 걷기: 새 게임부터 끝까지 넘기며, 선택지마다 정해 둔 낱말이 든 단추를 고르고 화면을 찍는다.
public class MolakV2Walker : MonoBehaviour
{
    public static string Status = "idle";
    public static readonly StringBuilder Log = new StringBuilder();
    static MolakV2Walker inst;
    List<string> picks; string runTag; int shot; bool shootAll;

    public static void Run (string pickList, string tagName, bool shootEveryChoice)
    {
        if (inst == null) { var go = new GameObject("MolakV2Walker"); DontDestroyOnLoad(go); inst = go.AddComponent<MolakV2Walker>(); }
        inst.StopAllCoroutines();
        inst.picks = (pickList ?? "").Split('|').Where(s => s.Length > 0).ToList();
        inst.runTag = tagName; inst.shot = 0; inst.shootAll = shootEveryChoice;
        Log.Length = 0; Status = "run";
        inst.StartCoroutine(inst.Go());
    }

    void Shot (string why)
    {
        System.IO.Directory.CreateDirectory(@"C:\Projects\_train\shots");
        string p = @"C:\Projects\_train\shots\" + runTag + "_" + (shot++).ToString("00") + ".png";
        ScreenCapture.CaptureScreenshot(p);
        Log.AppendLine("  shot " + System.IO.Path.GetFileName(p) + " " + why);
    }

    IEnumerator Go ()
    {
        var state = Engine.GetService<IStateManager>(); var sp = Engine.GetService<IScriptPlayer>();
        bool started = false;
        state.ResetState(new string[0], () => ScriptTrackExtensions.LoadAndPlay(sp.MainTrack, "Entry"));
        float t0 = Time.realtimeSinceStartup;
        while ((sp.MainTrack.PlaybackSpot.ScriptPath ?? "") == "" && Time.realtimeSinceStartup - t0 < 10f) yield return null;
        started = true;
        var tr = sp.MainTrack; float last = Time.realtimeSinceStartup; string lastPath = "";
        while (true)
        {
            yield return null;
            var path = tr.PlaybackSpot.ScriptPath ?? "";
            if (path != lastPath) { Log.AppendLine("== " + path); lastPath = path; }
            if (path == "Title" && Time.realtimeSinceStartup - t0 > 5f) { Status = "done"; Log.AppendLine("END at Title"); yield break; }
            sp.SetAutoPlay(true); sp.SkipMode = PlayerSkipMode.Everything; sp.SetSkip(true);
            if (tr.Playing) { if (Time.realtimeSinceStartup - last > 20f) { Status = "stall-play " + tr.PlaybackSpot; yield break; } continue; }
            var btns = FindObjectsByType<MolakVn.UI.ChoiceHandlerButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(b => b.isActiveAndEnabled && !b.Choice.Locked && b.GetComponent<UnityEngine.UI.Button>() != null && b.GetComponent<UnityEngine.UI.Button>().interactable).ToArray();
            if (btns.Length == 0)
            {
                if (Time.realtimeSinceStartup - last > 8f) { Status = "stall " + path + ":" + tr.PlaybackSpot.LineIndex; Log.AppendLine(Status); Shot("stall"); yield break; }
                continue;
            }
            var labels = btns.Select(b => b.GetComponentInChildren<TMPro.TMP_Text>(true)?.text ?? "?").ToArray();
            int k = 0;
            for (int pi = 0; pi < picks.Count; pi++)
            {
                int hit = System.Array.FindIndex(labels, l => l.Contains(picks[pi]));
                if (hit >= 0) { k = hit; picks.RemoveAt(pi); break; }
            }
            sp.SetSkip(false); sp.SetAutoPlay(false);
            if (shootAll) { yield return new WaitForSecondsRealtime(0.6f); Shot(path + ":" + tr.PlaybackSpot.LineIndex); yield return new WaitForSecondsRealtime(0.3f); }
            Log.AppendLine("[" + path + ":" + tr.PlaybackSpot.LineIndex + "] " + string.Join(" / ", labels) + "  => " + labels[k]);
            float c0 = Time.realtimeSinceStartup; int tries = 0;
            while (!tr.Playing && Time.realtimeSinceStartup - c0 < 3f)
            {
                if (btns[k] == null || !btns[k].isActiveAndEnabled) break;
                if (tries == 0 || Time.realtimeSinceStartup - c0 > 0.5f * tries) { btns[k].GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); tries++; }
                yield return null;
            }
            last = Time.realtimeSinceStartup;
            if (Time.realtimeSinceStartup - t0 > 900f) { Status = "timeout"; yield break; }
        }
    }
}
