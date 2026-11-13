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
    /// <summary>플레이 스토어 업로드용 AAB(0.4.22). 업로드 키스토어가 지정되어 있어야 스토어가 받는다.</summary>
    public static string Aab { get { return Path.Combine(BuildDir, "Android", "molakgeomsin.aab"); } }
    public static string ResultFile { get { return Path.Combine(BuildDir, "android_build_result.txt"); } }
    /// <summary>빌드가 끝나면 APK·PC zip 을 버전명 붙여 복사해 두는 공유 폴더(Google Drive 데스크톱 동기화 폴더).
    /// 기기마다 다를 수 있어 EditorPrefs "Molak.DriveDir" 로 바꿀 수 있다. 폴더가 없으면 복사를 건너뛴다.</summary>
    const string DefaultDriveDir = @"G:\내 드라이브\작업중(Ings)\시현\전생검신\Build_Apk";
    public static string DriveDir { get { return EditorPrefs.GetString("Molak.DriveDir", DefaultDriveDir); } }

    public static string CopyToDrive (string version, bool aab = false)
    {
        try
        {
            if (!Directory.Exists(DriveDir)) return " | drive: 폴더 없음";
            var dst = Path.Combine(DriveDir, "몰락검신_v" + version + (aab ? ".aab" : ".apk"));
            File.Copy(aab ? Aab : Apk, dst, true);
            return " | drive: " + Path.GetFileName(dst);
        }
        catch (System.Exception ex) { return " | driveEXC " + ex.Message; }
    }

    public static string Android (string version, int code) { return Build(version, code, false); }

    /// <summary>0.4.45: 기기에서 그림을 미리 읽어 두도록 Resources/MolakArt 의 그림 이름을 _index.txt 에 적는다(빌드 때마다 다시).</summary>
    public static void WriteArtIndex ()
    {
        var dir = Path.Combine(Application.dataPath, "Resources", "MolakArt");
        if (!Directory.Exists(dir)) return;
        var names = Directory.GetFiles(dir).Where(f => { var e = Path.GetExtension(f).ToLowerInvariant(); return e == ".png" || e == ".jpg" || e == ".jpeg"; })
            .Select(f => Path.GetFileNameWithoutExtension(f)).OrderBy(n => n).ToArray();
        var file = Path.Combine(dir, "_index.txt");
        var text = string.Join("\n", names);
        if (!File.Exists(file) || File.ReadAllText(file) != text) { File.WriteAllText(file, text, new System.Text.UTF8Encoding(false)); AssetDatabase.ImportAsset("Assets/Resources/MolakArt/_index.txt"); }
    }

    /// <summary>스토어용 AAB. 타깃 API는 36으로 고정한다(2026-08-31부터 새 앱·업데이트 요구 수준).
    /// 업로드 키스토어(Publishing Settings)가 없으면 디버그 서명이라 스토어가 거절한다 — 결과 줄에 표시한다.</summary>
    public static string Bundle (string version, int code) { return Build(version, code, true); }

    static string Build (string version, int code, bool aab)
    {
        if (File.Exists(ResultFile)) File.Delete(ResultFile);
        WriteArtIndex();
        // 보조: .nani 전부 재임포트(커맨드 컴파일 이후 상태 보장)
        foreach (var f in Directory.GetFiles(Path.Combine(Application.dataPath, "Scenario"), "*.nani"))
            AssetDatabase.ImportAsset("Assets/Scenario/" + Path.GetFileName(f), ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        PlayerSettings.bundleVersion = version;
        PlayerSettings.Android.bundleVersionCode = code;
        Directory.CreateDirectory(Path.GetDirectoryName(Apk));
        bool prevBundle = EditorUserBuildSettings.buildAppBundle;
        var prevTarget = PlayerSettings.Android.targetSdkVersion;
        EditorUserBuildSettings.buildAppBundle = aab;
        if (aab) PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
        var opts = new BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = aab ? Aab : Apk, target = BuildTarget.Android, options = BuildOptions.None
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
            if (aab) res += " | aab target=" + (int)PlayerSettings.Android.targetSdkVersion + (PlayerSettings.Android.useCustomKeystore ? " signed=upload" : " signed=DEBUG(스토어 거절)");
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded) res += CopyToDrive(version, aab);
        }
        catch (System.Exception ex) { res = "EXC " + ex.GetType().Name + ": " + ex.Message; }
        finally
        {
            try { Naninovel.BuildProcessor.PostprocessBuild(); } catch (System.Exception ex2) { res += " | cleanupEXC " + ex2.Message; }
            building.SetValue(null, false, null);
            EditorUserBuildSettings.buildAppBundle = prevBundle;
            if (aab) PlayerSettings.Android.targetSdkVersion = prevTarget;
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

    // ---------------- iOS (0.4.41) ----------------
    // 윈도우에서는 Xcode 프로젝트까지만 만든다(애플 정책상 앱으로 묶고 서명하는 마지막 단계는 macOS + Xcode 필요).
    // 맥이 없으면: 이 프로젝트 zip 을 macOS 클라우드(깃허브 Actions 등)에서 서명 없이 .ipa 로 묶고,
    // 윈도우의 Sideloadly 같은 도구로 무료 애플 ID 서명을 해 아이폰에 설치한다(7일마다 재서명). tools/ios/README 참고.
    public const string IosBundleId = "com.unmakers.molakgeomsin";
    public const string IosMinVersion = "15.0";
    public static string IosDir { get { return Path.Combine(BuildDir, "iOS"); } }
    public static string IosResultFile { get { return Path.Combine(BuildDir, "ios_build_result.txt"); } }

    /// <summary>iOS 플레이어 설정(빌드 전에 매번 맞춘다 — 손으로 바뀐 값도 되돌린다).</summary>
    public static void ApplyIosSettings (string version, int code)
    {
        var ios = UnityEditor.Build.NamedBuildTarget.iOS;
        PlayerSettings.SetApplicationIdentifier(ios, IosBundleId);
        PlayerSettings.bundleVersion = version;
        PlayerSettings.iOS.buildNumber = code.ToString();
        PlayerSettings.iOS.targetOSVersionString = IosMinVersion;
        PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
        PlayerSettings.iOS.appleEnableAutomaticSigning = true;   // 팀 ID는 Xcode/클라우드에서 넣는다(여기선 비워 둠)
        PlayerSettings.SetScriptingBackend(ios, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetArchitecture(ios, 1);                  // ARM64
        PlayerSettings.iOS.hideHomeButton = false;
        PlayerSettings.iOS.requiresPersistentWiFi = false;
        PlayerSettings.iOS.appInBackgroundBehavior = iOSAppInBackgroundBehavior.Suspend;
        PlayerSettings.iOS.statusBarStyle = iOSStatusBarStyle.Default;
        PlayerSettings.statusBarHidden = true;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
    }

    /// <summary>Xcode 프로젝트 출력 → Build\iOS\v{버전}\ , 그리고 zip. 결과 줄은 ios_build_result.txt.</summary>
    public static string IOS (string version, int code)
    {
        if (File.Exists(IosResultFile)) File.Delete(IosResultFile);
        WriteArtIndex();
        string res = "";
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
        {
            res = "EXC iOS Build Support 모듈이 설치돼 있지 않다(Unity Hub → 설치 → 모듈 추가)";
            File.WriteAllText(IosResultFile, res + "\n"); return res;
        }
        foreach (var f in Directory.GetFiles(Path.Combine(Application.dataPath, "Scenario"), "*.nani"))
            AssetDatabase.ImportAsset("Assets/Scenario/" + Path.GetFileName(f), ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        ApplyIosSettings(version, code);
        string outDir = Path.Combine(IosDir, "v" + version);
        if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
        Directory.CreateDirectory(outDir);
        var opts = new BuildPlayerOptions {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = outDir, target = BuildTarget.iOS, options = BuildOptions.None
        };
        var building = typeof(Naninovel.BuildProcessor).GetProperty("Building");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            building.SetValue(null, true, null);
            Naninovel.BuildProcessor.PreprocessBuild(opts);
            var report = BuildPipeline.BuildPlayer(opts);
            res = "result=" + report.summary.result + " errors=" + report.summary.totalErrors + " warnings=" + report.summary.totalWarnings + " time=" + (int)sw.Elapsed.TotalSeconds + "s v=" + version + " build=" + code;
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                PatchInfoPlist(Path.Combine(outDir, "Info.plist"));
                string zip = Path.Combine(IosDir, "몰락검신_v" + version + "_iOS_Xcode.zip");
                if (File.Exists(zip)) File.Delete(zip);
                System.IO.Compression.ZipFile.CreateFromDirectory(outDir, zip, System.IO.Compression.CompressionLevel.Optimal, false);
                res += " | zip=" + (new FileInfo(zip).Length / (1024 * 1024)) + "MB";
            }
        }
        catch (System.Exception ex) { res = "EXC " + ex.GetType().Name + ": " + ex.Message; }
        finally
        {
            try { Naninovel.BuildProcessor.PostprocessBuild(); } catch (System.Exception ex2) { res += " | cleanupEXC " + ex2.Message; }
            building.SetValue(null, false, null);
        }
        File.WriteAllText(IosResultFile, res + "\n");
        return res;
    }

    /// <summary>앱스토어·테스트플라이트 제출 때 매번 묻는 '암호화 사용' 질문을 미리 답해 둔다(표준 HTTPS도 안 쓰므로 면제),
    /// 표시 이름을 한글로. iOS 모듈 없이도 컴파일되게 plist 는 글자로 고친다.</summary>
    static void PatchInfoPlist (string plist)
    {
        if (!File.Exists(plist)) return;
        string s = File.ReadAllText(plist);
        int end = s.LastIndexOf("</dict>");
        if (end < 0) return;
        var add = new System.Text.StringBuilder();
        if (!s.Contains("ITSAppUsesNonExemptEncryption")) add.Append("\t<key>ITSAppUsesNonExemptEncryption</key>\n\t<false/>\n");
        if (!s.Contains("CFBundleDisplayName")) add.Append("\t<key>CFBundleDisplayName</key>\n\t<string>몰락검신</string>\n");
        if (add.Length > 0) File.WriteAllText(plist, s.Substring(0, end) + add + s.Substring(end), new System.Text.UTF8Encoding(false));
    }
}
