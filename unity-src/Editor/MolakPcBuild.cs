using UnityEditor;
using UnityEngine;
/// <summary>PC(Windows) 빌드 요청 — Build/pc_build_request 파일이 있으면 다음 편집기 틱에 MolakBuild.Windows(대본 동기화 포함)로 빌드하고, 끝나면 안드로이드로 되돌린다.
/// 주의: BuildPipeline 을 직접 부르면 Resources/MolakNani(빌드용 대본)가 옛 것으로 남아 옛 스토리가 실린다 — 꼭 MolakBuild 를 거친다.</summary>
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
            MolakBuild.WriteArtIndex();
            string res = MolakBuild.Windows(PlayerSettings.bundleVersion);
            System.IO.File.AppendAllText(Log, res + " at " + System.DateTime.Now + "\n");
        }
        catch (System.Exception e) { System.IO.File.AppendAllText(Log, "EXC " + e + "\n"); }
        bool ok = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        System.IO.File.AppendAllText(Log, "switchback=" + ok + " active=" + EditorUserBuildSettings.activeBuildTarget + " at " + System.DateTime.Now + "\n");
    }
}
