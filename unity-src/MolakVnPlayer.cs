using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// 0.5.0 자체 실행기 — 대본 재생기. 명령을 차례로 실행하고, 글이 기다리라면 누를 때까지 기다린다.
//   한 줄의 차례: (조건이 거짓이면 건너뜀) → OnExecute(MolakFlow 가 쪽 나눔을 정한다) → 실행 전 작업(쪽 붙잡기·그림 맞춤) → 실행 → 기다림 → 다음 줄.
namespace MolakVn
{
    public enum PlayerSkipMode { ReadOnly, Everything }

    public interface IScriptTrack
    {
        Script PlayedScript { get; }
        ScriptPlaylist Playlist { get; }
        int PlayedIndex { get; }
        Command PlayedCommand { get; }
        PlaybackSpot PlaybackSpot { get; }
        bool Playing { get; }
        bool Executing { get; }
        bool AwaitingInput { get; }
    }

    public interface IScriptPlayer
    {
        IScriptTrack MainTrack { get; }
        bool Playing { get; }
        bool Skipping { get; }
        bool AutoPlaying { get; }
        PlayerSkipMode SkipMode { get; set; }
        event Action<IScriptTrack> OnExecute;
        event Action<IScriptTrack> OnPlay;
        event Action<IScriptTrack> OnAwaitInput;
        void AddPreExecutionTask (Func<IScriptTrack, Awaitable> task);
        void SetSkip (bool enabled);
        void SetAutoPlay (bool enabled);
        bool HasPlayed (string scriptPath, int playlistIndex);
    }

    public static class ScriptTrackExtensions
    {
#pragma warning disable CS1998
        public static async Awaitable LoadAndPlay (this IScriptTrack track, string scriptPath) { ((ScriptTrack)track).Play(scriptPath, 0); }
        public static async Awaitable LoadAndPlayAtLabel (this IScriptTrack track, string scriptPath, string label) { ((ScriptTrack)track).PlayAtLabel(scriptPath, label); }
#pragma warning restore CS1998
    }

    public class ScriptTrack : IScriptTrack
    {
        public Script PlayedScript { get; private set; }
        public ScriptPlaylist Playlist { get { return PlayedScript != null ? PlayedScript.Playlist : null; } }
        public int PlayedIndex { get; private set; }
        public Command PlayedCommand { get { var pl = Playlist; return pl != null && pl.IsIndexValid(PlayedIndex) ? pl[PlayedIndex] : null; } }
        public PlaybackSpot PlaybackSpot { get { var c = PlayedCommand; return c != null ? c.PlaybackSpot : default(PlaybackSpot); } }
        public bool Playing { get; private set; }
        public bool Executing { get; private set; }
        public bool AwaitingInput { get; private set; }
        /// <summary>재생이 끊길 때마다(멈춤·다른 자리에서 다시 재생·초기화) 늘어난다. 실행 중이던 명령은 이 값이 바뀌면 손을 뗀다.</summary>
        public int Session { get; private set; }

        readonly ScriptPlayer player;
        readonly List<KeyValuePair<string, int>> returns = new List<KeyValuePair<string, int>>();   // gosub 가 돌아올 자리(대본·명령 자리)
        Script jumpScript; int jumpIndex = -1; bool stopAsked;
        public ScriptTrack (ScriptPlayer p) { player = p; PlayedIndex = -1; }

        // ---- 이동 ----
        static void Split (string path, string cur, out string script, out string label)
        {
            path = path ?? "";
            int h = path.IndexOf('#');
            script = h >= 0 ? path.Substring(0, h) : path; label = h >= 0 ? path.Substring(h + 1) : null;
            if (script.Length == 0) script = cur;
        }
        bool Resolve (string path, string cur, out Script s, out int index)
        {
            string sn, lb; Split(path, cur, out sn, out lb);
            s = Script.Get(sn); index = -1;
            if (s == null) { Debug.LogError("[Molak] 갈 대본이 없다: " + path + " ← " + PlaybackSpot); return false; }
            if (string.IsNullOrEmpty(lb)) { index = 0; return true; }
            index = s.IndexOfLabel(lb);
            if (index < 0) { Debug.LogError("[Molak] 갈 라벨이 없다: " + path + " ← " + PlaybackSpot); return false; }
            return true;
        }
        /// <summary>실행 중인 명령(@goto·@gosub)이 다음 자리를 정한다.</summary>
        public void Jump (string path, string cur) { Script s; int i; if (Resolve(path, cur, out s, out i)) { jumpScript = s; jumpIndex = i; } }
        public void PushReturn () { if (PlayedScript != null) returns.Add(new KeyValuePair<string, int>(PlayedScript.Path, PlayedIndex + 1)); }
        public void PopReturn ()
        {
            if (returns.Count == 0) { Debug.LogWarning("[Molak] @return — 돌아갈 자리가 없다 " + PlaybackSpot); return; }
            var r = returns[returns.Count - 1]; returns.RemoveAt(returns.Count - 1);
            var s = Script.Get(r.Key); if (s == null) return;
            jumpScript = s; jumpIndex = r.Value;
        }
        public void RequestStop () { stopAsked = true; }

        // ---- 재생 ----
        public void Play (string scriptPath, int index)
        {
            var s = Script.Get(scriptPath);
            if (s == null) { Debug.LogError("[Molak] 틀 대본이 없다: " + scriptPath); return; }
            StartAt(s, index);
        }
        public void PlayAtLabel (string scriptPath, string label)
        {
            var s = Script.Get(scriptPath);
            if (s == null) { Debug.LogError("[Molak] 틀 대본이 없다: " + scriptPath); return; }
            int i = string.IsNullOrEmpty(label) ? 0 : s.IndexOfLabel(label);
            if (i < 0) { Debug.LogError("[Molak] 틀 라벨이 없다: " + scriptPath + "#" + label); return; }
            StartAt(s, i);
        }
        /// <summary>선택지를 골랐다: 그 선택지가 가리키는 자리부터 다시 재생. 가리키는 자리가 없으면 멈춘 줄 다음부터.</summary>
        public void PlayFromChoice (Choice c)
        {
            if (string.IsNullOrEmpty(c.GotoPath)) { if (PlayedScript != null) StartAt(PlayedScript, PlayedIndex + 1); return; }
            Script s; int i;
            if (Resolve(c.GotoPath, c.ScriptPath, out s, out i)) StartAt(s, i);
        }
        void StartAt (Script s, int index)
        {
            Session++; AwaitingInput = false; Executing = false; jumpScript = null; stopAsked = false;
            PlayedScript = s; PlayedIndex = index;
            _ = Run(Session, false);
        }
        /// <summary>멈춘다(초기화·불러오기 앞). 실행 중이던 명령은 손을 뗀다.</summary>
        public void Halt (bool forget)
        {
            Session++; Playing = false; Executing = false; AwaitingInput = false; jumpScript = null; stopAsked = false;
            if (forget) { returns.Clear(); PlayedScript = null; PlayedIndex = -1; }
        }

        public void SetAwaitInput (bool on)
        {
            if (on && player.Skipping) return;
            if (AwaitingInput == on) return;
            AwaitingInput = on;
            player.RaiseAwaitInput(this);
        }

        async Awaitable Run (int my, bool resumeAfterExecuted)
        {
            Playing = true;
            player.RaisePlay(this);
            int burst = 0, burstFrame = Time.frameCount;
            try
            {
                while (my == Session)
                {
                    var pl = Playlist;
                    if (pl == null || !pl.IsIndexValid(PlayedIndex)) break;   // 대본 끝
                    var cmd = pl[PlayedIndex];
                    if (resumeAfterExecuted) resumeAfterExecuted = false;     // 불러온 자리: 이 줄은 이미 실행했다 — 기다림부터 잇는다
                    else if (cmd.ShouldExecute)
                    {
                        player.RaiseExecute(this);
                        player.Played.Register(PlayedScript.Path, PlayedIndex);
                        for (int i = player.PreTasks.Count - 1; i >= 0; i--)
                        {
                            await player.PreTasks[i](this);
                            if (my != Session) return;
                        }
                        Executing = true;
                        try { await cmd.Execute(new ExecutionContext { Track = this }); }
                        catch (Exception e) { Debug.LogException(e); }
                        if (my != Session) return;
                        Executing = false;
                    }
                    if (AwaitingInput)
                    {
                        float autoAt = Time.unscaledTime + ScriptPlayer.MinAutoPlayDelay;
                        while (AwaitingInput && my == Session)
                        {
                            if (player.Skipping) break;
                            if (player.AutoPlaying && Time.unscaledTime >= autoAt) break;
                            if (!player.AutoPlaying) autoAt = Time.unscaledTime + ScriptPlayer.MinAutoPlayDelay;   // 자동을 기다리는 중에 켜면 그때부터 센다
                            await Awaitable.NextFrameAsync();
                        }
                        if (my != Session) return;
                        SetAwaitInput(false);
                    }
                    if (stopAsked) { stopAsked = false; break; }
                    if (jumpScript != null) { PlayedScript = jumpScript; PlayedIndex = jumpIndex; jumpScript = null; }
                    else PlayedIndex++;
                    if (player.Skipping && player.SkipMode == PlayerSkipMode.ReadOnly && PlayedScript != null && !player.Played.Has(PlayedScript.Path, PlayedIndex)) player.SetSkip(false);
                    // 한 프레임에 너무 많은 줄을 돌지 않는다(대본이 맴돌아도 화면이 얼지 않게)
                    if (Time.frameCount != burstFrame) { burstFrame = Time.frameCount; burst = 0; }
                    else if (++burst > 4000) { burst = 0; await Awaitable.NextFrameAsync(); if (my != Session) return; burstFrame = Time.frameCount; }
                }
            }
            finally { if (my == Session) { Playing = false; Executing = false; } }
        }

        // ---- 저장·불러오기 ----
        public void Save (StringBuilder sb)
        {
            var spot = PlaybackSpot;
            sb.Append("track\t").Append(PlayedScript != null ? PlayedScript.Path : "").Append('\t').Append(PlayedIndex).Append('\t').Append(spot.LineIndex)
              .Append('\t').Append(Playing ? 1 : 0).Append('\t').Append(AwaitingInput ? 1 : 0).Append('\n');
            foreach (var r in returns)
            {
                var s = Script.Get(r.Key); int line = s != null && s.Playlist.IsIndexValid(r.Value) ? s.Playlist[r.Value].PlaybackSpot.LineIndex : -1;
                sb.Append("ret\t").Append(r.Key).Append('\t').Append(r.Value).Append('\t').Append(line).Append('\n');
            }
        }
        string loadScript; int loadIndex; bool loadPlaying, loadAwait;
        public void LoadBegin () { Halt(true); loadScript = null; loadPlaying = false; loadAwait = false; }
        public void LoadRow (string[] p)
        {
            if (p[0] == "track" && p.Length >= 6)
            {
                loadScript = p[1]; loadIndex = FixIndex(p[1], int.Parse(p[2]), int.Parse(p[3]));
                loadPlaying = p[4] == "1"; loadAwait = p[5] == "1";
            }
            else if (p[0] == "ret" && p.Length >= 4) returns.Add(new KeyValuePair<string, int>(p[1], FixIndex(p[1], int.Parse(p[2]), int.Parse(p[3]))));
        }
        /// <summary>저장한 뒤 대본이 바뀌었으면(줄 번호가 안 맞으면) 그 줄이나 그 뒤의 첫 명령으로.</summary>
        static int FixIndex (string script, int index, int line)
        {
            var s = Script.Get(script); if (s == null) return index;
            if (s.Playlist.IsIndexValid(index) && (line < 0 || s.Playlist[index].PlaybackSpot.LineIndex == line)) return index;
            if (line < 0) return index;
            int i = s.Playlist.IndexAtOrAfterLine(line);
            return i >= 0 ? i : index;
        }
        /// <summary>대본 자리만 먼저 놓는다(그림 되살리기가 이 자리에서 위로 훑는다).</summary>
        public void LoadPlaced ()
        {
            if (string.IsNullOrEmpty(loadScript)) return;
            var s = Script.Get(loadScript); if (s == null) { loadScript = null; return; }
            PlayedScript = s; PlayedIndex = loadIndex;
        }
        /// <summary>다른 것(변수·글·선택지·그림)을 다 되살린 뒤에 부른다 — 멈춘 자리에서 재생을 잇는다.</summary>
        public void LoadEnd ()
        {
            if (string.IsNullOrEmpty(loadScript) || PlayedScript == null) return;
            if (!loadPlaying) return;   // 선택지·@stop 에서 멈춰 있던 저장
            Session++;
            AwaitingInput = loadAwait;
            _ = Run(Session, true);
        }
    }

    /// <summary>읽은 줄 기록(대본마다 읽은 명령 자리). 「읽은 글 넘기기」와 지난 생 줄(@deja)이 쓴다. 전역 저장에 들어간다.</summary>
    public class PlayedSet
    {
        readonly Dictionary<string, List<bool>> map = new Dictionary<string, List<bool>>();
        public bool Dirty;
        public void Register (string script, int index)
        {
            List<bool> l;
            if (!map.TryGetValue(script, out l)) { l = new List<bool>(); map[script] = l; }
            while (l.Count <= index) l.Add(false);
            if (!l[index]) { l[index] = true; Dirty = true; }
        }
        public bool Has (string script, int index) { List<bool> l; return map.TryGetValue(script, out l) && index >= 0 && index < l.Count && l[index]; }
        public void RegisterRange (string script, int from, int to) { for (int i = from; i <= to; i++) Register(script, i); }
        public void Clear () { map.Clear(); }
        /// <summary>한 줄에 대본 하나: played&lt;TAB&gt;대본&lt;TAB&gt;시작-끝,시작-끝…</summary>
        public void Dump (StringBuilder sb)
        {
            foreach (var kv in map)
            {
                sb.Append("played\t").Append(kv.Key).Append('\t');
                var l = kv.Value; int i = 0; bool first = true;
                while (i < l.Count)
                {
                    if (!l[i]) { i++; continue; }
                    int j = i; while (j + 1 < l.Count && l[j + 1]) j++;
                    if (!first) sb.Append(','); first = false;
                    sb.Append(i).Append('-').Append(j);
                    i = j + 1;
                }
                sb.Append('\n');
            }
        }
        public void LoadRow (string[] p)
        {
            if (p.Length < 3 || p[2].Length == 0) return;
            foreach (var seg in p[2].Split(','))
            {
                int d = seg.IndexOf('-'); int a, b;
                if (d > 0 && int.TryParse(seg.Substring(0, d), out a) && int.TryParse(seg.Substring(d + 1), out b)) RegisterRange(p[1], a, b);
            }
        }
    }

    public class ScriptPlayer : IScriptPlayer
    {
        /// <summary>자동 읽기: 글이 다 나온 뒤 적어도 이만큼(초) 둔다.</summary>
        public const float MinAutoPlayDelay = 1f;
        public readonly ScriptTrack Track;
        public readonly PlayedSet Played = new PlayedSet();
        public readonly List<Func<IScriptTrack, Awaitable>> PreTasks = new List<Func<IScriptTrack, Awaitable>>();
        public IScriptTrack MainTrack { get { return Track; } }
        public bool Playing { get { return Track.Playing; } }
        public bool Skipping { get; private set; }
        public bool AutoPlaying { get; private set; }
        public PlayerSkipMode SkipMode { get; set; }
        public event Action<IScriptTrack> OnExecute, OnPlay, OnAwaitInput;

        public ScriptPlayer () { Track = new ScriptTrack(this); }
        public void AddPreExecutionTask (Func<IScriptTrack, Awaitable> task) { PreTasks.Add(task); }
        internal void RaiseExecute (IScriptTrack t) { var h = OnExecute; if (h != null) try { h(t); } catch (Exception e) { Debug.LogException(e); } }
        internal void RaisePlay (IScriptTrack t) { var h = OnPlay; if (h != null) try { h(t); } catch (Exception e) { Debug.LogException(e); } }
        internal void RaiseAwaitInput (IScriptTrack t) { var h = OnAwaitInput; if (h != null) try { h(t); } catch (Exception e) { Debug.LogException(e); } }

        public void SetSkip (bool on)
        {
            if (Skipping == on) return;
            if (on && SkipMode == PlayerSkipMode.ReadOnly && Track.PlayedScript != null && !Played.Has(Track.PlayedScript.Path, Track.PlayedIndex + 1)) return;   // 처음 보는 글 앞에서는 켜지지 않는다
            Skipping = on;
            if (on) { if (AutoPlaying) AutoPlaying = false; Engine.Printers.Default.CompleteReveal(); if (Track.AwaitingInput) Track.SetAwaitInput(false); }
        }
        public void SetAutoPlay (bool on)
        {
            if (AutoPlaying == on) return;
            AutoPlaying = on;
            if (on && Track.AwaitingInput) Track.SetAwaitInput(false);   // 기다리던 중에 자동을 켜면 바로 잇는다
        }
        public bool HasPlayed (string scriptPath, int playlistIndex) { return Played.Has(scriptPath, playlistIndex); }

        /// <summary>'계속' 입력: 글이 나오는 중이면 한꺼번에 다 보이고, 기다리는 중이면 다음으로.</summary>
        /// <summary>글이 찍히는 중에 누르면 다음 몇 줄을 한꺼번에 낸다(탭을 기다리는 자리에서 0으로 돌아간다).</summary>
        public int InstantLines;
        internal void HandleContinue ()
        {
            var pr = Engine.Printers.Default;
            // 0.5.5 (언메이커: 누르면 조금만 넘어간다 → 지금 줄 + 다음 줄까지) 찍히던 줄을 마치고, 같은 탭에 이어 나올 다음 한 줄도 바로 낸다
            if (pr.Revealing) { pr.CompleteReveal(); InstantLines = 1; return; }
            if (Track.AwaitingInput) Track.SetAwaitInput(false);
        }
        internal void ResetModes () { Skipping = false; AutoPlaying = false; }
    }
}
