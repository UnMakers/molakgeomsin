using UnityEditor;
using UnityEngine;
/// <summary>PC(Windows) 빌드 — Build/pc_build_request 파일이 있으면 다음 편집기 틱에 한 번 빌드하고, 끝나면 안드로이드로 되돌린다.</summary>
[InitializeOnLoad]
public static class MolakPcBuild
{
    const string Req = "C:/Projects/RuinedSword/Build/pc_build_request";
    const string Log = "C:/Projects/RuinedSword/Build/pc_build_log.txt";
    static MolakPcBuild () { EditorApplication.update += Tick; }
    static void Tick ()
    {
        if (!System.IO.File.Exists(Req) || EditorApplication.isCompiling || EditorApplication.isPlaying || BuildPipeline.isBuildingPlayer) return;
        System.IO.File.Delete(Req);
        Build();
    }
    [MenuItem("Molak/PC 빌드")]
    public static void Build ()
    {
        System.IO.File.AppendAllText(Log, "start " + System.DateTime.Now + "\n");
        try
        {
            var opt = new BuildPlayerOptions();
            opt.scenes = new[] { "Assets/Scenes/SampleScene.unity" };
            opt.locationPathName = "C:/Projects/RuinedSword/Build/PC/몰락검신.exe";
            opt.target = BuildTarget.StandaloneWindows64;
            opt.targetGroup = BuildTargetGroup.Standalone;
            opt.options = BuildOptions.None;
            var s = BuildPipeline.BuildPlayer(opt).summary;
            System.IO.File.AppendAllText(Log, "result=" + s.result + " size=" + s.totalSize + " time=" + s.totalTime + " errors=" + s.totalErrors + " at " + System.DateTime.Now + "\n");
        }
        catch (System.Exception e) { System.IO.File.AppendAllText(Log, "EXC " + e + "\n"); }
        bool ok = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        System.IO.File.AppendAllText(Log, "switchback=" + ok + " active=" + EditorUserBuildSettings.activeBuildTarget + " at " + System.DateTime.Now + "\n");
    }
}
