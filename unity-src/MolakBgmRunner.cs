#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Net;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 0.4.85 배경음 뽑기 대기열(에디터 전용). 컴파일·플레이 모드로 도메인이 다시 올라와도 저절로 이어진다.
///  · 작업: Build/bgm2jobs/g_&lt;묶음&gt;_c&lt;N&gt;.json(ComfyUI /prompt 몸통). c0 묶음 전부 → c1 → c2 순.
///  · ComfyUI 대기열이 비고 90초 쉰 뒤 다음 작업을 넣는다. 결과(output/molak/bgm2/*.mp3)는 Build/bgm2 로 복사.
///  · Build/bgm2/PAUSE 파일이 있으면 새 작업을 넣지 않는다(그림을 뽑을 때).
///  · 기록 Build/bgm2/log.txt
/// </summary>
[InitializeOnLoad]
public static class MolakBgmRunner
{
    const string Comfy = "http://127.0.0.1:8188";
    const string ComfyOut = @"C:\Users\sjf1t\AppData\Local\Comfy-Desktop\ComfyUI-Shared\output\molak\bgm2";
    const float Rest = 90f;
    static double nextCheck, idleSince = -1;
    static string Root { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Build")); } }

    static MolakBgmRunner () { EditorApplication.update += Tick; }

    static void Log (string s)
    {
        try { File.AppendAllText(Path.Combine(Root, "bgm2", "log.txt"), DateTime.Now.ToString() + " " + s + "\n"); } catch { }
    }

    static bool Produced (string name)
    {
        try { return Directory.Exists(ComfyOut) && Directory.GetFiles(ComfyOut, name + "_*.mp3").Length > 0; } catch { return false; }
    }

    static void Tick ()
    {
        if (EditorApplication.timeSinceStartup < nextCheck) return;
        nextCheck = EditorApplication.timeSinceStartup + 5.0;
        try
        {
            string jobs = Path.Combine(Root, "bgm2jobs"), outDir = Path.Combine(Root, "bgm2");
            if (!Directory.Exists(jobs)) return;
            Directory.CreateDirectory(outDir);
            // 나온 곡 복사
            if (Directory.Exists(ComfyOut))
                foreach (var f in Directory.GetFiles(ComfyOut, "*.mp3"))
                {
                    var dst = Path.Combine(outDir, Path.GetFileName(f));
                    if (!File.Exists(dst)) { File.Copy(f, dst); Log("DONE " + Path.GetFileNameWithoutExtension(f)); }
                }
            var all = Directory.GetFiles(jobs, "g_*_c*.json").Select(p => Path.GetFileNameWithoutExtension(p).Substring(2))
                               .OrderBy(n => n.Substring(n.LastIndexOf("_c") + 2)).ThenBy(n => n).ToList();
            var left = all.Where(n => !Produced(n)).ToList();
            if (left.Count == 0) return;
            if (File.Exists(Path.Combine(outDir, "PAUSE"))) { idleSince = -1; return; }
            string q;
            using (var wc = new WebClient()) { wc.Encoding = System.Text.Encoding.UTF8; q = wc.DownloadString(Comfy + "/queue"); }
            bool busy = q.Contains("\"queue_running\": [[") || q.Contains("\"queue_running\":[[") || q.Contains("\"queue_pending\": [[") || q.Contains("\"queue_pending\":[[");
            if (busy) { idleSince = -1; return; }
            if (idleSince < 0) { idleSince = EditorApplication.timeSinceStartup; return; }
            if (EditorApplication.timeSinceStartup - idleSince < Rest) return;
            var name = left[0];
            var body = File.ReadAllText(Path.Combine(jobs, "g_" + name + ".json"));
            string res;
            using (var wc = new WebClient()) { wc.Encoding = System.Text.Encoding.UTF8; wc.Headers[HttpRequestHeader.ContentType] = "application/json"; res = wc.UploadString(Comfy + "/prompt", body); }
            idleSince = -1;
            Log("START " + name + " left=" + left.Count + " " + res.Replace("\n", " "));
        }
        catch (Exception e) { nextCheck = EditorApplication.timeSinceStartup + 30.0; Debug.LogWarning("[MolakBgmRunner] " + e.Message); }
    }
}
#endif
