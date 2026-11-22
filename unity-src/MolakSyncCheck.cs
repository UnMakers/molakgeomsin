using System.Collections;
using System.Text;
using Naninovel;
using UnityEngine;

/// <summary>
/// 0.4.95 그림·글 맞춤 점검(언메이커: 빨리 모드에서 하진무와 대화 중인데 그림이 먼저 넘어갔다 — 빨리·보통 모두 전부 확인).
///  기준: 화면에 떠 있는 쪽의 글을 읽는 동안에는 그림(장면·인물)이 바뀌지 않는다. 그림은 새 글이 찍히는 순간(MolakFlow) 또는 선택지·정지에서만 바뀐다.
///  MolakArt.DoCommit 이 그림을 바꿀 때마다 OnCommit 이 불린다. 글과 함께가 아닌데(InPrint 아님) 대본이 돌고 있으면 '이른 그림'으로 적는다.
///    · 쪽을 붙잡는 중(MolakFlow.Gating) · 누름 대기 중(AwaitingInput) · 글 없이 1.5초가 지나 저절로 올린 것(fallback)으로 나눠 적는다.
///    · 넘기기 중, 먹 화면·마디 덮개가 글을 가린 동안, 대본이 멈춘(선택지·정지) 때는 세지 않는다.
///  편집기 시험: MolakSyncCheck.Run(fast, policy, lives, minutes) — 넘기기 없이 자동 읽기로 봇(MolakEndTester)을 돌리고 Build/sync_{fast|normal}.txt 에 적는다.
/// </summary>
public class MolakSyncCheck : MonoBehaviour
{
    public static bool InPrint;
    public static int Early, Ok, Stopped;
    static readonly StringBuilder log = new StringBuilder();
    static readonly System.Collections.Generic.HashSet<string> seen = new System.Collections.Generic.HashSet<string>();
    public static string Status = "idle";
    static MolakSyncCheck inst;

    public static void OnCommit (string newScene, string newChar, string oldScene, string oldChar)
    {
        if (InPrint) { Ok++; return; }
        if (!Engine.Initialized) return;
        var sp = Engine.GetService<IScriptPlayer>(); var tr = sp != null ? sp.MainTrack : null;
        if (tr == null || !tr.Playing) { Stopped++; return; }
        if (MolakPlayMode.SkipAll || sp.Skipping) return;
        if (MolakBlackout.Busy || MolakVfx.ChapterCover > 0.5f) return;
        if (!MolakFlow.PageHasText) return;   // 빈 쪽(글이 찍히기 전)이면 옛 글을 읽는 중이 아니다
        string kind = MolakFlow.Gating ? "쪽 붙잡는 중" : tr.AwaitingInput ? "누름 대기" : "글 없이 오래(8초)";
        string page = "";
        foreach (var t in Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (t.gameObject.name == "DialogueText") { page = System.Text.RegularExpressions.Regex.Replace(t.text ?? "", "<[^>]+>", "").Replace("\n", " ").Trim(); break; }
        if (page.Length > 40) page = "…" + page.Substring(page.Length - 40);
        if (page.Replace("​", "").Length == 0) return;   // 빈 쪽이면 옛 글을 읽는 중이 아니다
        string spot = tr.PlaybackSpot.ToString();
        string key = spot + "|" + newScene + "|" + newChar;
        Early++;
        if (seen.Add(key))
            log.AppendLine(kind + " · " + spot + " · " + (oldScene ?? "") + "/" + (oldChar ?? "") + " → " + (newScene ?? "") + "/" + (newChar ?? "") + " · 쪽 끝: " + page);
    }

    public static void Run (bool fast, int policy, int lives, float minutes)
    {
        if (inst == null) { var go = new GameObject("MolakSyncCheck"); DontDestroyOnLoad(go); inst = go.AddComponent<MolakSyncCheck>(); }
        inst.StopAllCoroutines(); inst.StartCoroutine(inst.Go(fast, policy, lives, minutes));
    }

    IEnumerator Go (bool fast, int policy, int lives, float minutes)
    {
        Early = 0; Ok = 0; Stopped = 0; log.Length = 0; seen.Clear();
        string path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), "Build", "sync_" + (fast ? "fast" : "normal") + "_p" + policy + ".txt");
        MolakEndTester.Slow = true;
        MolakPlayMode.SetSkipAll(false); MolakPlayMode.SetFast(fast); MolakPlayMode.SetAuto(true);
        MolakSoakRunner.Matrix(new[] { policy }, lives, "sync" + (fast ? "F" : "N"));
        float until = Time.realtimeSinceStartup + minutes * 60f;
        while (Time.realtimeSinceStartup < until && !MolakSoakRunner.Status.StartsWith("done"))
        {
            Status = (fast ? "빨리" : "보통") + " 이른 그림 " + Early + " · 글과 함께 " + Ok + " · 날 " + MolakStats.Get("day") + " · " + MolakEndTester.Status;
            MolakPlayMode.SetAuto(true); if (MolakPlayMode.Fast != fast) MolakPlayMode.SetFast(fast);
            if (MolakName.Open) MolakName.AutoFill("연호");
            if (MolakVfx.Holding) MolakVfx.ReleaseStamp();
            try { System.IO.File.WriteAllText(path, "# " + Status + "\n" + log, new UTF8Encoding(false)); } catch { }
            yield return new WaitForSecondsRealtime(2f);
        }
        Status = "done " + Status;
        try { System.IO.File.WriteAllText(path, "# " + Status + "\n" + log, new UTF8Encoding(false)); } catch { }
        MolakEndTester.Slow = false;
    }
}
