using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 몰락검신 안드로이드 빌드 — 나니노벨 BuildProcessor.Building 플래그를 켠 상태로 전처리/빌드/정리.
/// (플래그 없이 PreprocessBuild만 부르면 임시 복사본 스크립트 경로가 TEMP_NANINOVEL/... 로 깨져 기기에서 타이틀 복귀 현상)
/// </summary>
public static class MolakBuild
{
    /// <summary>빌드 산출물은 프로젝트 옆 Build 폴더에 둔다(프로젝트를 어디로 옮겨도 따라간다).</summary>
    static string BuildDir { get { return Path.Combine(Path.GetDirectoryName(Application.dataPath), "Build"); } }
    public static string Apk { get { return Path.Combine(BuildDir, "Android", "molakgeomsin.apk"); } }
    public static string ResultFile { get { return Path.Combine(BuildDir, "android_build_result.txt"); } }
    /// <summary>빌드가 끝나면 APK·PC zip 을 버전명 붙여 복사해 두는 공유 폴더(Google Drive 데스크톱 동기화 폴더).
    /// 기기마다 다를 수 있어 EditorPrefs "Molak.DriveDir" 로 바꿀 수 있다. 폴더가 없으면 복사를 건너뛴다.</summary>
    const string DefaultDriveDir = @"G:\내 드라이브\작업중(Ings)\시현\전생검신\Build_Apk";
    public static string DriveDir { get { return EditorPrefs.GetString("Molak.DriveDir", DefaultDriveDir); } }

    public static string CopyToDrive (string version)
    {
        try
        {
            if (!Directory.Exists(DriveDir)) return " | drive: 폴더 없음";
            var dst = Path.Combine(DriveDir, "몰락검신_v" + version + ".apk");
            File.Copy(Apk, dst, true);
            return " | drive: " + Path.GetFileName(dst);
        }
        catch (System.Exception ex) { return " | driveEXC " + ex.Message; }
    }

    public static string Android (string version, int code)
    {
        if (File.Exists(ResultFile)) File.Delete(ResultFile);
        // 보조: .nani 전부 재임포트(커맨드 컴파일 이후 상태 보장)
        foreach (var f in Directory.GetFiles(Path.Combine(Application.dataPath, "Scenario"), "*.nani"))
            AssetDatabase.ImportAsset("Assets/Scenario/" + Path.GetFileName(f), ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        PlayerSettings.bundleVersion = version;
        PlayerSettings.Android.bundleVersionCode = code;
        Directory.CreateDirectory(Path.GetDirectoryName(Apk));
        var opts = new BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = Apk, target = BuildTarget.Android, options = BuildOptions.None
        };
        var building = typeof(Naninovel.BuildProcessor).GetProperty("Building");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        string res = "";
        try
        {
            building.SetValue(null, true, null);
            Naninovel.BuildProcessor.PreprocessBuild(opts);
            // 임시 복사본 스크립트 경로 검증
            var bad = Directory.GetFiles(Path.Combine(Application.dataPath, "TEMP_NANINOVEL/Resources/Naninovel/Scripts"), "*.nani")
                .Select(f => AssetDatabase.LoadAssetAtPath<Naninovel.Script>("Assets/TEMP_NANINOVEL/Resources/Naninovel/Scripts/" + Path.GetFileName(f)))
                .Where(sc => sc == null || sc.Path.Contains("/")).ToList();
            if (bad.Count > 0) throw new System.Exception("TEMP script path broken: " + string.Join(",", bad.Select(b => b ? b.Path : "null")));
            var report = BuildPipeline.BuildPlayer(opts);
            res = "result=" + report.summary.result + " size=" + (report.summary.totalSize / (1024 * 1024)) + "MB errors=" + report.summary.totalErrors + " time=" + (int)sw.Elapsed.TotalSeconds + "s v=" + version;
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded) res += CopyToDrive(version);
        }
        catch (System.Exception ex) { res = "EXC " + ex.GetType().Name + ": " + ex.Message; }
        finally
        {
            try { Naninovel.BuildProcessor.PostprocessBuild(); } catch (System.Exception ex2) { res += " | cleanupEXC " + ex2.Message; }
            building.SetValue(null, false, null);
        }
        File.WriteAllText(ResultFile, res + "\n");
        return res;
    }

    public static string WinDir { get { return Path.Combine(BuildDir, "Win"); } }
    public static string WinResultFile { get { return Path.Combine(BuildDir, "win_build_result.txt"); } }

    /// <summary>PC(Windows 64비트) 빌드 → Build\Win\v{버전}\몰락검신.exe, 폴더째 zip 으로 묶어 드라이브 Build_Apk 에 함께 둔다.</summary>
    public static string Windows (string version)
    {
        if (File.Exists(WinResultFile)) File.Delete(WinResultFile);
        foreach (var f in Directory.GetFiles(Path.Combine(Application.dataPath, "Scenario"), "*.nani"))
            AssetDatabase.ImportAsset("Assets/Scenario/" + Path.GetFileName(f), ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        PlayerSettings.bundleVersion = version;
        string outDir = Path.Combine(WinDir, "v" + version);
        if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
        Directory.CreateDirectory(outDir);
        var opts = new BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = Path.Combine(outDir, "몰락검신.exe"), target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
        };
        var building = typeof(Naninovel.BuildProcessor).GetProperty("Building");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        string res = "";
        try
        {
            building.SetValue(null, true, null);
            Naninovel.BuildProcessor.PreprocessBuild(opts);
            var report = BuildPipeline.BuildPlayer(opts);
            res = "result=" + report.summary.result + " size=" + (report.summary.totalSize / (1024 * 1024)) + "MB errors=" + report.summary.totalErrors + " time=" + (int)sw.Elapsed.TotalSeconds + "s v=" + version;
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                // 디버그 심볼 폴더는 빼고 묶는다
                foreach (var d in Directory.GetDirectories(outDir, "*DoNotShip*")) Directory.Delete(d, true);
                string zip = Path.Combine(WinDir, "몰락검신_v" + version + "_PC.zip");
                if (File.Exists(zip)) File.Delete(zip);
                System.IO.Compression.ZipFile.CreateFromDirectory(outDir, zip);
                res += " | zip=" + (new FileInfo(zip).Length / (1024 * 1024)) + "MB";
                try { if (Directory.Exists(DriveDir)) { File.Copy(zip, Path.Combine(DriveDir, Path.GetFileName(zip)), true); res += " | drive: " + Path.GetFileName(zip); } }
                catch (System.Exception ex) { res += " | driveEXC " + ex.Message; }
            }
        }
        catch (System.Exception ex) { res = "EXC " + ex.GetType().Name + ": " + ex.Message; }
        finally
        {
            try { Naninovel.BuildProcessor.PostprocessBuild(); } catch (System.Exception ex2) { res += " | cleanupEXC " + ex2.Message; }
            building.SetValue(null, false, null);
        }
        File.WriteAllText(WinResultFile, res + "\n");
        return res;
    }
}
