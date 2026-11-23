using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

// 0.5.0 자체 실행기 — 변수·저장·설정.
//   판 저장(이어하기·빠른 저장): 대본 자리 + 돌아갈 자리들 + 판 변수 + 화면의 글·선택지. 글 파일 하나(persistentDataPath/MolakSave/*.sav).
//   전역 저장: g_ 변수 + 읽은 줄 기록. 예전(나니노벨) 전역 저장이 있으면 첫 실행 때 옮겨 온다.
namespace MolakVn
{
    public enum CustomVariableValueType { String, Numeric, Boolean }
    public struct CustomVariableValue
    {
        public CustomVariableValueType Type; public string String; public float Number;
        public CustomVariableValue (string s) { Type = CustomVariableValueType.String; String = s ?? ""; Number = 0f; }
        public CustomVariableValue (float n) { Type = CustomVariableValueType.Numeric; String = null; Number = n; }
    }
    public struct CustomVariable { public string Name; public CustomVariableValue Value; }

    public interface ICustomVariableManager
    {
        IReadOnlyCollection<CustomVariable> Variables { get; }
        bool VariableExists (string name);
        CustomVariableValue GetVariableValue (string name);
        void SetVariableValue (string name, CustomVariableValue value);
    }
    /// <summary>변수 저장소(MolakVars)를 옛 이름으로 내어 준다.</summary>
    public class CustomVariableManager : ICustomVariableManager
    {
        public IReadOnlyCollection<CustomVariable> Variables
        {
            get
            {
                var l = new List<CustomVariable>(MolakVars.Count);
                foreach (var kv in MolakVars.All) l.Add(new CustomVariable { Name = kv.Key, Value = kv.Value.IsStr ? new CustomVariableValue(kv.Value.Str) : new CustomVariableValue(kv.Value.Num) });
                return l;
            }
        }
        public bool VariableExists (string name) { return MolakVars.Has(name); }
        public CustomVariableValue GetVariableValue (string name)
        {
            MolakVars.Val v;
            if (!MolakVars.TryGet(name, out v)) return new CustomVariableValue(0f);
            return v.IsStr ? new CustomVariableValue(v.Str) : new CustomVariableValue(v.Num);
        }
        public void SetVariableValue (string name, CustomVariableValue value)
        {
            if (value.Type == CustomVariableValueType.String) MolakVars.Set(name, value.String ?? ""); else MolakVars.Set(name, value.Number);
        }
    }

    public class GameSaveLoadArgs { public string SlotId; public bool Quick; }

    public interface IStateManager
    {
        event Action OnResetStarted;
        event Action OnResetFinished;
        event Action<GameSaveLoadArgs> OnGameLoadStarted;
        event Action<GameSaveLoadArgs> OnGameLoadFinished;
        bool QuickLoadAvailable { get; }
        bool AutoLoadAvailable { get; }
        bool AnyGameSaveExists { get; }
        Awaitable ResetState (string[] exclude, params Func<Awaitable>[] tasks);
        Awaitable ResetState (params Func<Awaitable>[] tasks);
        Awaitable SaveGlobal ();
        Awaitable SaveSettings ();
        Awaitable QuickSave ();
        Awaitable QuickLoad ();
        Awaitable AutoSave ();
        Awaitable AutoLoad ();
    }

    public class StateManager : IStateManager
    {
        public event Action OnResetStarted, OnResetFinished;
        public event Action<GameSaveLoadArgs> OnGameLoadStarted, OnGameLoadFinished;

#if UNITY_EDITOR
        // 편집기 저장은 프로젝트 안에 둔다 — 같은 PC 에 깐 빌드의 저장(persistentDataPath)을 점검 도구가 덮지 않게
        static string Dir { get { return Path.Combine(Path.GetDirectoryName(Application.dataPath), "Library", "MolakSave"); } }
#else
        static string Dir { get { return Path.Combine(Application.persistentDataPath, "MolakSave"); } }
#endif
        static string GlobalPath { get { return Path.Combine(Dir, "global.sav"); } }
        static string QuickPath { get { return Path.Combine(Dir, "quick.sav"); } }
        static string AutoPath { get { return Path.Combine(Dir, "auto.sav"); } }
        public bool QuickLoadAvailable { get { return File.Exists(QuickPath); } }
        public bool AutoLoadAvailable { get { return File.Exists(AutoPath); } }
        public bool AnyGameSaveExists { get { return false; } }   // 칸 저장은 없다 — 이어하기(자동)와 빠른 저장 둘뿐

        static void Fire (Action a) { if (a != null) try { a(); } catch (Exception e) { Debug.LogException(e); } }
        static void Fire (Action<GameSaveLoadArgs> a, GameSaveLoadArgs args)
        {
            if (a == null) return;
            foreach (var d in a.GetInvocationList()) try { ((Action<GameSaveLoadArgs>)d)(args); } catch (Exception e) { Debug.LogException(e); }
        }

        // ---- 초기화: 판을 비운다(전역은 남는다) ----
        public async Awaitable ResetState (string[] exclude, params Func<Awaitable>[] tasks)
        {
            if (OnResetStarted != null) foreach (var d in OnResetStarted.GetInvocationList()) Fire((Action)d);
            Engine.Player.Track.Halt(true); Engine.Player.ResetModes();
            var pr = Engine.Printers.Default; pr.ClearMessages(); pr.Visible = false;
            Engine.Choices.Default.Clear();
            MolakVars.ClearLocals();
            if (tasks != null) foreach (var t in tasks) if (t != null) await t();
            if (OnResetFinished != null) foreach (var d in OnResetFinished.GetInvocationList()) Fire((Action)d);
        }
        public Awaitable ResetState (params Func<Awaitable>[] tasks) { return ResetState(null, tasks); }

        // ---- 글 파일 쓰기(먼저 옆에 쓰고 바꿔치기 — 쓰다 꺼져도 옛 저장이 남는다) ----
        static void WriteSafe (string path, string text)
        {
            Directory.CreateDirectory(Dir);
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, text, new UTF8Encoding(false));
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }
        static string Esc (string s) { return (s ?? "").Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\t", "\\t").Replace("\r", ""); }
        static string Unesc (string s)
        {
            if (s.IndexOf('\\') < 0) return s;
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '\\' && i + 1 < s.Length) { char c = s[++i]; sb.Append(c == 'n' ? '\n' : c == 't' ? '\t' : c); }
                else sb.Append(s[i]);
            }
            return sb.ToString();
        }

        // ---- 전역 ----
#pragma warning disable CS1998
        public async Awaitable SaveGlobal ()
        {
            try
            {
                var sb = new StringBuilder(65536);
                sb.Append("MOLAKGLOBAL\t1\n");
                Engine.Player.Played.Dump(sb);
                sb.Append("---\n").Append(MolakVars.Dump(true));
                WriteSafe(GlobalPath, sb.ToString());
                Engine.Player.Played.Dirty = false;
            }
            catch (Exception e) { Debug.LogWarning("[Molak] 전역 저장 실패: " + e.Message); }
        }

        public async Awaitable SaveSettings ()
        {
            var am = Engine.GetService<IAudioManager>(); var pm = Engine.Printers;
            PlayerPrefs.SetFloat("molak_vol_master", am.MasterVolume); PlayerPrefs.SetFloat("molak_vol_bgm", am.BgmVolume); PlayerPrefs.SetFloat("molak_vol_sfx", am.SfxVolume);
            PlayerPrefs.SetFloat("molak_reveal", pm.BaseRevealSpeed); PlayerPrefs.SetFloat("molak_autodelay", pm.BaseAutoDelay);
            PlayerPrefs.Save();
        }
#pragma warning restore CS1998

        internal void LoadGlobalAtBoot ()
        {
            try
            {
                if (File.Exists(GlobalPath))
                {
                    var text = File.ReadAllText(GlobalPath, Encoding.UTF8);
                    int cut = text.IndexOf("\n---\n", StringComparison.Ordinal);
                    string head = cut >= 0 ? text.Substring(0, cut) : text, vars = cut >= 0 ? text.Substring(cut + 5) : "";
                    foreach (var row in head.Split('\n')) { var p = row.Split('\t'); if (p[0] == "played") Engine.Player.Played.LoadRow(p); }
                    MolakVars.Load(vars, true, true);
                    Engine.Player.Played.Dirty = false;
                    return;
                }
                if (MolakVnMigrate.FromNaninovel()) { _ = SaveGlobal(); _ = SaveSettings(); }
            }
            catch (Exception e) { Debug.LogWarning("[Molak] 전역 불러오기 실패: " + e.Message); }
        }

        // ---- 판 ----
        string Serialize ()
        {
            var sb = new StringBuilder(32768);
            sb.Append("MOLAKSAVE\t1\n");
            Engine.Player.Track.Save(sb);
            var pr = Engine.Printers.Default;
            sb.Append("printer\t").Append(pr.Visible ? 1 : 0).Append('\n');
            var msgs = new List<PrintedMessage>(); pr.CollectMessages(msgs);
            foreach (var m in msgs) sb.Append("msg\t").Append(Esc(m.Text.ToString())).Append('\n');
            var cs = new List<Choice>(); Engine.Choices.Default.CollectChoices(cs);
            foreach (var c in cs) sb.Append("choice\t").Append(c.ScriptPath ?? "").Append('\t').Append(c.GotoPath ?? "").Append('\t').Append(c.Locked ? 1 : 0).Append('\t').Append(Esc(c.Summary)).Append('\n');
            sb.Append("---\n").Append(MolakVars.Dump(false));
            return sb.ToString();
        }

        void Deserialize (string text, GameSaveLoadArgs args)
        {
            Fire(OnGameLoadStarted, args);
            var track = Engine.Player.Track;
            track.LoadBegin(); Engine.Player.ResetModes();
            var pr = Engine.Printers.Default; pr.ClearMessages();
            Engine.Choices.Default.Clear();
            int cut = text.IndexOf("\n---\n", StringComparison.Ordinal);
            string head = cut >= 0 ? text.Substring(0, cut) : text, vars = cut >= 0 ? text.Substring(cut + 5) : "";
            MolakVars.Load(vars, false, true);
            var msgs = new List<PrintedMessage>(); bool vis = false;
            foreach (var row in head.Split('\n'))
            {
                var p = row.Split('\t');
                switch (p[0])
                {
                    case "track": case "ret": track.LoadRow(p); break;
                    case "printer": vis = p.Length > 1 && p[1] == "1"; break;
                    case "msg": if (p.Length > 1) msgs.Add(new PrintedMessage(new LocalizableText(Unesc(p[1])))); break;
                    case "choice": if (p.Length >= 5) Engine.Choices.Default.AddChoice(new Choice(Guid.NewGuid().ToString("N"), Unesc(p[4]), p[1], p[2].Length > 0 ? p[2] : null, p[3] == "1")); break;
                }
            }
            pr.SetMessages(msgs); pr.RevealProgress = 1f; pr.Visible = vis;
            track.LoadPlaced();
            Fire(OnGameLoadFinished, args);
            track.LoadEnd();
        }

#pragma warning disable CS1998
        async Awaitable Save (string path) { try { WriteSafe(path, Serialize()); } catch (Exception e) { Debug.LogWarning("[Molak] 저장 실패: " + e.Message); } }
        async Awaitable Load (string path, bool quick)
        {
            try { if (File.Exists(path)) Deserialize(File.ReadAllText(path, Encoding.UTF8), new GameSaveLoadArgs { SlotId = Path.GetFileNameWithoutExtension(path), Quick = quick }); }
            catch (Exception e) { Debug.LogError("[Molak] 불러오기 실패: " + e); }
        }
#pragma warning restore CS1998
        public Awaitable QuickSave () { return Save(QuickPath); }
        public Awaitable QuickLoad () { return Load(QuickPath, true); }
        public Awaitable AutoSave () { return Save(AutoPath); }
        public Awaitable AutoLoad () { return Load(AutoPath, false); }
        /// <summary>점검용: 지금 판을 글로 적었다가 그대로 되살린다.</summary>
        public string Snapshot () { return Serialize(); }
        public void Restore (string snap) { Deserialize(snap, new GameSaveLoadArgs { SlotId = "snapshot" }); }
    }

    // ---------------------------------------------------------------- 소리 크기(설정)
    public interface IAudioManager { float MasterVolume { get; set; } float BgmVolume { get; set; } float SfxVolume { get; set; } }
    public class AudioManager : IAudioManager
    {
        public float MasterVolume { get; set; }
        public float BgmVolume { get; set; }
        public float SfxVolume { get; set; }
        public AudioManager ()
        {
            MasterVolume = PlayerPrefs.GetFloat("molak_vol_master", 1f);
            BgmVolume = PlayerPrefs.GetFloat("molak_vol_bgm", 0.5f);
            SfxVolume = PlayerPrefs.GetFloat("molak_vol_sfx", 1f);
        }
    }

    // ---------------------------------------------------------------- 옛 화면(저장 칸·기록·설정 창)은 없다
    public interface IUIManager { T GetUI<T> () where T : class; }
    public class UIManager : IUIManager { public T GetUI<T> () where T : class { return null; } }
}

namespace MolakVn.UI
{
    public interface IManagedUI { bool Visible { get; } void ChangeVisibility (bool visible, float duration); }
    public interface ISaveLoadUI : IManagedUI { }
    public interface ISettingsUI : IManagedUI { }
    public interface IConfirmationUI : IManagedUI { }
    public interface ITitleUI : IManagedUI { }
    public interface IBacklogUI : IManagedUI { }
}

/// <summary>예전(나니노벨) 저장에서 전역 변수·읽은 줄·설정을 옮겨 온다. 판 저장(이어하기)은 꼴이 달라 옮기지 못한다.</summary>
public static class MolakVnMigrate
{
    [Serializable] class NMap { public NObj objectJsonMap; }
    [Serializable] class NObj { public List<string> keys; public List<string> values; }
    [Serializable] class NVars { public List<NVar> GlobalVariables; }
    [Serializable] class NVar { public string name; public NVal value; }
    [Serializable] class NVal { public int type; public string stringValue; public float numericValue; }
    [Serializable] class NPlayedRoot { public NPlayed Played; }
    [Serializable] class NPlayed { public List<NPs> playedScripts; }
    [Serializable] class NPs { public string ScriptPath; public List<NRange> PlayedIndexes; }
    [Serializable] class NRange { public int startIndex; public int endIndex; }

    static string OldDir ()
    {
#if UNITY_EDITOR
        string b = Path.Combine(Application.dataPath, "NaninovelData/.nani/Transient/Saves");
        if (File.Exists(Path.Combine(b, "GlobalSave.nson"))) return b;
        return null;
#else
        string a = Path.Combine(Application.persistentDataPath, "Saves");
        return File.Exists(Path.Combine(a, "GlobalSave.nson")) ? a : null;
#endif
    }

    public static bool FromNaninovel ()
    {
        string dir = OldDir(); if (dir == null) return false;
        int nv = 0, np = 0;
        try
        {
            string json;
            using (var fs = File.OpenRead(Path.Combine(dir, "GlobalSave.nson")))
            using (var ds = new System.IO.Compression.DeflateStream(fs, System.IO.Compression.CompressionMode.Decompress))
            using (var rd = new StreamReader(ds, Encoding.UTF8)) json = rd.ReadToEnd();
            if (json.Length > 0 && json[0] == '\uFEFF') json = json.Substring(1);
            var map = JsonUtility.FromJson<NMap>(json);
            if (map == null || map.objectJsonMap == null || map.objectJsonMap.keys == null) return false;
            for (int i = 0; i < map.objectJsonMap.keys.Count && i < map.objectJsonMap.values.Count; i++)
            {
                string k = map.objectJsonMap.keys[i], v = map.objectJsonMap.values[i];
                if (k.StartsWith("Naninovel.CustomVariableManager+GlobalState"))
                {
                    var gv = JsonUtility.FromJson<NVars>(v);
                    if (gv != null && gv.GlobalVariables != null)
                        foreach (var x in gv.GlobalVariables)
                        {
                            if (x == null || string.IsNullOrEmpty(x.name) || x.value == null) continue;
                            if (x.value.type == 0) MolakVars.Set(x.name, x.value.stringValue ?? ""); else MolakVars.Set(x.name, x.value.numericValue);
                            nv++;
                        }
                }
                else if (k.StartsWith("Naninovel.ScriptPlayer+GlobalState"))
                {
                    var pr = JsonUtility.FromJson<NPlayedRoot>(v);
                    if (pr != null && pr.Played != null && pr.Played.playedScripts != null)
                        foreach (var s in pr.Played.playedScripts)
                        {
                            if (s == null || s.PlayedIndexes == null || string.IsNullOrEmpty(s.ScriptPath)) continue;
                            foreach (var r in s.PlayedIndexes) { MolakVn.Engine.Player.Played.RegisterRange(s.ScriptPath, r.startIndex, r.endIndex); np++; }
                        }
                }
            }
        }
        catch (Exception e) { Debug.LogWarning("[Molak] 예전 전역 저장을 옮기지 못했다: " + e.Message); return nv > 0; }
        try
        {   // 설정(소리 크기·글 빠르기)
            string sp = Path.Combine(dir, "Settings.json");
            if (File.Exists(sp))
            {
                string t = File.ReadAllText(sp);
                Pref(t, "MasterVolume", "molak_vol_master"); Pref(t, "BgmVolume", "molak_vol_bgm"); Pref(t, "SfxVolume", "molak_vol_sfx");
                Pref(t, "BaseRevealSpeed", "molak_reveal"); Pref(t, "BaseAutoDelay", "molak_autodelay");
                var am = MolakVn.Engine.GetService<MolakVn.IAudioManager>();
                if (am != null) { am.MasterVolume = PlayerPrefs.GetFloat("molak_vol_master", 1f); am.BgmVolume = PlayerPrefs.GetFloat("molak_vol_bgm", 0.5f); am.SfxVolume = PlayerPrefs.GetFloat("molak_vol_sfx", 1f); }
                MolakVn.Engine.Printers.BaseRevealSpeed = PlayerPrefs.GetFloat("molak_reveal", 0.5f); MolakVn.Engine.Printers.BaseAutoDelay = PlayerPrefs.GetFloat("molak_autodelay", 0.5f);
            }
        }
        catch { }
        Debug.Log("[Molak] 예전 저장에서 옮겼다 — 전역 변수 " + nv + " · 읽은 줄 묶음 " + np);
        return nv > 0;
    }

    static void Pref (string json, string key, string pref)
    {
        var m = System.Text.RegularExpressions.Regex.Match(json, "\\\\?\"" + key + "\\\\?\":\\s*([0-9.eE+-]+)");
        float f;
        if (m.Success && float.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out f)) PlayerPrefs.SetFloat(pref, Mathf.Clamp01(f));
    }
}
