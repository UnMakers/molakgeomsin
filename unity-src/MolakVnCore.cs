using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;

// 0.5.0 자체 실행기 — 나니노벨을 걷어 내고 그 자리에 놓는 우리 엔진.
//   몰락검신의 화면·흐름 코드(MolakFlow·MolakScroll·MolakTheme …)는 「엔진·대본 재생기·인쇄기·선택지」에 기대어 있다.
//   그 기대는 면(쓰는 이름과 동작)만 우리 손으로 다시 지었다 — 대본(.nani)도, 화면·흐름 코드도 그대로 돈다.
//   이 파일: 엔진 뼈대 · 명령과 인자 · 대본을 명령 목록으로 엮기 · 기본 명령(글·선택지·이동·정지·기다림·변수).
namespace MolakVn
{
    /// <summary>엔진: 서비스(재생기·저장·변수·입력·소리·인쇄기·선택지)를 한곳에서 꺼낸다.</summary>
    public static class Engine
    {
        public static bool Initialized { get; private set; }
        /// <summary>점검 봇용: 글 한 줄마다 한 프레임 쉬는 것까지 건너뛴다(화면 없이 빨리 돌릴 때).</summary>
        public static bool Turbo;
        static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();
        public static T GetService<T> () where T : class { object o; return services.TryGetValue(typeof(T), out o) ? (T)o : null; }
        public static T GetServiceOrErr<T> () where T : class { var s = GetService<T>(); if (s == null) throw new Exception("[Molak] 엔진 서비스가 없다: " + typeof(T).Name); return s; }
        static void Reg<T> (T s) where T : class { services[typeof(T)] = s; }

        public static ScriptPlayer Player;
        public static StateManager State;
        public static TextPrinterManager Printers;
        public static ChoiceHandlerManager Choices;
        public static InputManager Input;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot ()
        {
            // 편집기는 재생할 때 도메인을 다시 올리지 않는다 — 지난 재생의 정적 상태를 여기서 다 비운다
            Initialized = false; Turbo = false; services.Clear();
            MolakScript.ClearCache(); Script.ClearCache(); MolakVars.ClearAll();
            MolakCore.On = true;
            var stage = Stage.Build();
            Player = new ScriptPlayer();
            State = new StateManager();
            Printers = new TextPrinterManager(stage.PrinterPanel);
            Choices = new ChoiceHandlerManager(stage.ChoicePanel);
            Input = stage.gameObject.AddComponent<InputManager>();
            Reg<IScriptPlayer>(Player); Reg<IStateManager>(State);
            Reg<ICustomVariableManager>(new CustomVariableManager());
            Reg<IInputManager>(Input); Reg<IAudioManager>(new AudioManager());
            Reg<ITextPrinterManager>(Printers); Reg<IChoiceHandlerManager>(Choices);
            Reg<IUIManager>(new UIManager()); Reg<ITextLocalizer>(new TextLocalizer());
            State.LoadGlobalAtBoot();
            Initialized = true;
        }
    }

    // ---------------------------------------------------------------- 인자
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field)]
    public class AliasAttribute : Attribute { public readonly string Alias; public AliasAttribute (string alias) { Alias = alias; } }
    [AttributeUsage(AttributeTargets.Field)]
    public class RequiredParameterAttribute : Attribute { }

    public interface ICommandParameter { bool HasValue { get; } bool DynamicValue { get; } void SetRaw (string raw); }

    /// <summary>명령 인자 하나. 대본에 적힌 날 글을 들고 있다가, 값을 물을 때 {변수}를 풀어 읽는다(줄을 실행하는 그 순간의 값).</summary>
    public abstract class Parameter<T> : ICommandParameter
    {
        string raw; bool has, direct; T val;
        public bool HasValue { get { return has; } }
        public bool DynamicValue { get { return !direct && raw != null && raw.IndexOf('{') >= 0; } }
        public T Value
        {
            get { if (!has) return default(T); return direct ? val : Parse(MolakExpr.Inject(raw)); }
            set { val = value; direct = true; has = true; }
        }
        public void SetRaw (string r) { raw = r; has = r != null; direct = false; }
        public void Clear () { raw = null; has = false; direct = false; val = default(T); }
        protected abstract T Parse (string s);
        public override string ToString () { return !has ? "" : direct ? Convert.ToString(val, CultureInfo.InvariantCulture) : raw; }
    }
    public class StringParameter : Parameter<string>
    {
        protected override string Parse (string s) { return s; }
        public static implicit operator StringParameter (string v) { var p = new StringParameter(); if (v != null) p.Value = v; return p; }
        public static implicit operator string (StringParameter p) { return p == null ? null : p.Value; }
    }
    public class BooleanParameter : Parameter<bool>
    {
        protected override bool Parse (string s) { s = (s ?? "").Trim(); return s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1" || s == "+"; }
        public static implicit operator BooleanParameter (bool v) { return new BooleanParameter { Value = v }; }
        public static implicit operator bool (BooleanParameter p) { return p != null && p.HasValue && p.Value; }
    }
    public class IntegerParameter : Parameter<int>
    {
        protected override int Parse (string s) { float f; return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out f) ? Mathf.RoundToInt(f) : 0; }
        public static implicit operator IntegerParameter (int v) { return new IntegerParameter { Value = v }; }
        public static implicit operator int (IntegerParameter p) { return p == null ? 0 : p.Value; }
    }
    public class DecimalParameter : Parameter<float>
    {
        protected override float Parse (string s) { float f; return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out f) ? f : 0f; }
        public static implicit operator DecimalParameter (float v) { return new DecimalParameter { Value = v }; }
        public static implicit operator float (DecimalParameter p) { return p == null ? 0f : p.Value; }
    }

    /// <summary>화면에 찍는 글. (번역 층은 없다 — 원문이 곧 글이다.)</summary>
    public struct LocalizableText
    {
        readonly string text;
        public LocalizableText (string t) { text = t; }
        public bool IsEmpty { get { return string.IsNullOrEmpty(text); } }
        public static LocalizableText FromPlainText (string t) { return new LocalizableText(t); }
        public static LocalizableText operator + (LocalizableText a, LocalizableText b) { return new LocalizableText((a.text ?? "") + (b.text ?? "")); }
        public static implicit operator string (LocalizableText t) { return t.text ?? ""; }
        public static implicit operator LocalizableText (string t) { return new LocalizableText(t); }
        public override string ToString () { return text ?? ""; }
    }
    public class LocalizableTextParameter : Parameter<LocalizableText>
    {
        protected override LocalizableText Parse (string s) { return new LocalizableText(s); }
        public static implicit operator LocalizableTextParameter (string v) { var p = new LocalizableTextParameter(); if (v != null) p.Value = new LocalizableText(v); return p; }
    }
    public interface ITextLocalizer { string Resolve (LocalizableText text); }
    public class TextLocalizer : ITextLocalizer { public string Resolve (LocalizableText text) { return text.ToString(); } }

    public struct PrintedMessage
    {
        public LocalizableText Text;
        public PrintedMessage (LocalizableText text) { Text = text; }
    }

    /// <summary>대본의 자리: 어느 대본 몇째 줄.</summary>
    public struct PlaybackSpot
    {
        public string ScriptPath; public int LineIndex; public int InlineIndex;
        public int LineNumber { get { return LineIndex + 1; } }
        public PlaybackSpot (string script, int lineIndex) { ScriptPath = script; LineIndex = lineIndex; InlineIndex = 0; }
        public override string ToString () { return (ScriptPath ?? "-") + ":" + LineNumber; }
    }

    public struct ExecutionContext { public IScriptTrack Track; }

    public static class ExpressionEvaluator
    {
        public static T Evaluate<T> (string expr)
        {
            var v = MolakExpr.Eval(expr);
            object o;
            if (typeof(T) == typeof(bool)) o = MolakExpr.True(expr);
            else if (typeof(T) == typeof(float)) o = MolakExpr.Number(expr);
            else if (typeof(T) == typeof(int)) o = Mathf.RoundToInt(MolakExpr.Number(expr));
            else o = v.ToString();
            return (T)o;
        }
    }

    // ---------------------------------------------------------------- 명령
    public abstract class Command
    {
        public PlaybackSpot PlaybackSpot;
        public StringParameter ConditionalExpression = new StringParameter();
        public StringParameter InvertedConditionalExpression = new StringParameter();
        /// <summary>if: 조건이 지금 참인가(없으면 참). 거짓이면 재생기가 이 줄을 건너뛴다.</summary>
        public virtual bool ShouldExecute
        {
            get
            {
                if (ConditionalExpression != null && ConditionalExpression.HasValue && !MolakExpr.True(ConditionalExpression.Value)) return false;
                if (InvertedConditionalExpression != null && InvertedConditionalExpression.HasValue && MolakExpr.True(InvertedConditionalExpression.Value)) return false;
                return true;
            }
        }
        public abstract Awaitable Execute (ExecutionContext ctx);
        public static bool Assigned (ICommandParameter p) { return p != null && p.HasValue; }

        // ---- 명령 표: [Alias("이름")] 이 붙은 Command 자손을 한 번 훑어 둔다 ----
        static Dictionary<string, Type> types;
        static readonly Dictionary<Type, Dictionary<string, FieldInfo>> fields = new Dictionary<Type, Dictionary<string, FieldInfo>>();
        static void Scan ()
        {
            types = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var an = asm.GetName().Name;
                if (an.StartsWith("Unity") || an.StartsWith("System") || an.StartsWith("mscorlib") || an.StartsWith("Mono") || an.StartsWith("netstandard")) continue;
                Type[] ts; try { ts = asm.GetTypes(); } catch (ReflectionTypeLoadException e) { ts = e.Types; }
                foreach (var t in ts)
                {
                    if (t == null || t.IsAbstract || !typeof(Command).IsAssignableFrom(t)) continue;
                    var al = t.GetCustomAttribute<AliasAttribute>();
                    if (al != null) types[al.Alias] = t;
                }
            }
        }
        static Dictionary<string, FieldInfo> FieldsOf (Type t)
        {
            Dictionary<string, FieldInfo> d;
            if (fields.TryGetValue(t, out d)) return d;
            d = new Dictionary<string, FieldInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!typeof(ICommandParameter).IsAssignableFrom(f.FieldType)) continue;
                var al = f.GetCustomAttribute<AliasAttribute>();
                d[al != null ? al.Alias : f.Name] = f;
            }
            fields[t] = d; return d;
        }

        /// <summary>대본 한 줄(@이름 인자…)로 명령을 짓는다. 모르는 명령이면 null.</summary>
        public static Command Create (MolakLine ln, string script)
        {
            if (types == null) Scan();
            Type t;
            if (!types.TryGetValue(ln.Name, out t)) { Debug.LogError("[Molak] 모르는 명령 @" + ln.Name + " — " + script + ":" + ln.LineNo); return null; }
            var cmd = (Command)Activator.CreateInstance(t);
            var fs = FieldsOf(t);
            foreach (var kv in fs) if (kv.Value.GetValue(cmd) == null) kv.Value.SetValue(cmd, Activator.CreateInstance(kv.Value.FieldType));   // 인자는 늘 있다(값만 없을 뿐)
            if (ln.Args != null)
                foreach (var a in ln.Args)
                {
                    FieldInfo f;
                    if (fs.TryGetValue(a.Key, out f)) ((ICommandParameter)f.GetValue(cmd)).SetRaw(a.Value);
                    else Debug.LogWarning("[Molak] @" + ln.Name + " 이 모르는 인자 " + a.Key + " — " + script + ":" + ln.LineNo);
                }
            if (ln.Pos != null) Debug.LogWarning("[Molak] @" + ln.Name + " 은 이름 없는 값을 받지 않는다 — " + script + ":" + ln.LineNo);
            return cmd;
        }
    }

    // ---------------------------------------------------------------- 대본 = 명령 목록
    public class ScriptPlaylist : List<Command>
    {
        public bool IsIndexValid (int i) { return i >= 0 && i < Count; }
        /// <summary>그 줄(0부터) 또는 그 뒤의 첫 명령 자리. 없으면 -1.</summary>
        public int IndexAtOrAfterLine (int lineIndex) { for (int i = 0; i < Count; i++) if (this[i].PlaybackSpot.LineIndex >= lineIndex) return i; return -1; }
    }

    public class Script
    {
        public string Path;
        public readonly ScriptPlaylist Playlist = new ScriptPlaylist();
        readonly List<KeyValuePair<string, int>> labelLines = new List<KeyValuePair<string, int>>();   // 라벨 → 줄(0부터), 적힌 순서대로
        readonly Dictionary<string, int> labelIndex = new Dictionary<string, int>();                  // 라벨 → 그 뒤 첫 명령의 자리
        public static implicit operator bool (Script s) { return s != null; }

        static readonly Dictionary<string, Script> cache = new Dictionary<string, Script>();
        public static void ClearCache () { cache.Clear(); }
        public static Script Get (string path)
        {
            Script s;
            if (cache.TryGetValue(path, out s)) return s;
            var src = MolakScript.Get(path);
            if (src == null) return null;
            s = new Script { Path = path };
            foreach (var ln in src.Lines)
            {
                if (ln.Kind == MolakLineKind.Label)
                {
                    s.labelLines.Add(new KeyValuePair<string, int>(ln.Name, ln.LineNo - 1));
                    s.labelIndex[ln.Name] = s.Playlist.Count;
                    continue;
                }
                Command c;
                if (ln.Kind == MolakLineKind.Text) c = new Commands.PrintText { Text = NewText(ln.Pos) };
                else if (ln.Kind == MolakLineKind.Choice)
                {
                    var ac = new Commands.AddChoice { ChoiceSummary = NewText(ln.Pos) };
                    ac.GotoPath.SetRaw(ln.Arg("goto")); ac.Lock.SetRaw(ln.Arg("lock"));
                    c = ac;
                }
                else switch (ln.Name)
                {
                    case "print": c = new Commands.PrintText { Text = NewText(ln.Pos) }; break;
                    case "set": { var sc = new Commands.SetCustomVariable(); sc.Expression.SetRaw(ln.Pos); c = sc; break; }
                    case "goto": { var g = new Commands.Goto(); g.Path.SetRaw(ln.Pos); c = g; break; }
                    case "gosub": { var g = new Commands.Gosub(); g.Path.SetRaw(ln.Pos); c = g; break; }
                    case "return": c = new Commands.Return(); break;
                    case "stop": c = new Commands.Stop(); break;
                    case "wait": { var w = new Commands.Wait(); w.WaitMode.SetRaw(ln.Pos); c = w; break; }
                    case "showUI": case "hideUI": c = new Commands.Noop(); break;
                    default: c = Command.Create(ln, path); break;
                }
                if (c == null) c = new Commands.Noop();
                c.PlaybackSpot = new PlaybackSpot(path, ln.LineNo - 1);
                if (ln.Cond != null) c.ConditionalExpression.SetRaw(ln.Cond);
                s.Playlist.Add(c);
            }
            cache[path] = s; return s;
        }
        static LocalizableTextParameter NewText (string raw) { var p = new LocalizableTextParameter(); p.SetRaw(raw ?? ""); return p; }

        public bool LabelExists (string label) { return labelIndex.ContainsKey(label); }
        /// <summary>라벨 뒤 첫 명령의 자리(재생 목록). 없으면 -1.</summary>
        public int IndexOfLabel (string label) { int i; return labelIndex.TryGetValue(label, out i) ? i : -1; }
        public int GetLineIndexForLabel (string label) { foreach (var kv in labelLines) if (kv.Key == label) return kv.Value; return -1; }
        /// <summary>그 줄이 든 라벨(그 줄 위에서 가장 가까운 라벨). 없으면 null.</summary>
        public string GetLabelForLine (int lineIndex)
        {
            string best = null;
            foreach (var kv in labelLines) { if (kv.Value > lineIndex) break; best = kv.Key; }
            return best;
        }
    }
}

namespace MolakVn.Commands
{
    /// <summary>아무 일도 하지 않는 줄(옛 화면 명령 등).</summary>
    public class Noop : Command
    {
#pragma warning disable CS1998
        public override async Awaitable Execute (ExecutionContext ctx) { }
#pragma warning restore CS1998
    }

    /// <summary>본문 한 줄을 찍는다. 쪽을 비울지(ResetPrinter)·기다릴지(WaitForInput)·빠르기(RevealSpeed)는 MolakFlow 가 찍기 직전에 정해 준다.</summary>
    public class PrintText : Command
    {
        public LocalizableTextParameter Text = new LocalizableTextParameter();
        public BooleanParameter ResetPrinter = new BooleanParameter();
        public BooleanParameter WaitForInput = new BooleanParameter();
        public DecimalParameter RevealSpeed = new DecimalParameter();

        public override async Awaitable Execute (ExecutionContext ctx)
        {
            var track = Engine.Player.Track; int my = track.Session;
            var pr = Engine.Printers.Default;
            if (!Assigned(ResetPrinter) || ResetPrinter.Value) { pr.ClearMessages(); pr.RevealProgress = 0f; }
            pr.Visible = true;
            string text = Text.Value.ToString();
            if (string.IsNullOrEmpty(text)) return;
            pr.AddMessage(new PrintedMessage(new LocalizableText(text)));
            MolakBacklog.Line(text);
            float speed = Assigned(RevealSpeed) ? RevealSpeed.Value : 1f;
            float delay = Engine.Player.Skipping ? 0f : Mathf.Lerp(TextPrinterManager.MaxRevealDelay, 0f, Mathf.Clamp01(Engine.Printers.BaseRevealSpeed * speed));
            await pr.Reveal(delay);
            if (track.Session != my) return;
            if (!Engine.Turbo) { await Awaitable.NextFrameAsync(); if (track.Session != my) return; }
            bool wait = !Assigned(WaitForInput) || WaitForInput.Value;
            if (!wait || Engine.Player.Skipping) return;
            if (Engine.Player.AutoPlaying)
            {   // 자동: 글 길이만큼 읽을 틈을 준 뒤 넘긴다
                int letters = 0; foreach (char ch in text) if (char.IsLetterOrDigit(ch)) letters++;
                float until = Time.time + Mathf.Lerp(0f, TextPrinterManager.MaxAutoWaitDelay, Mathf.Clamp01(Engine.Printers.BaseAutoDelay * speed)) * letters;
                while (Time.time < until && Engine.Player.AutoPlaying && !Engine.Player.Skipping && track.Session == my) await Awaitable.NextFrameAsync();
                if (track.Session != my) return;
            }
            track.SetAwaitInput(true);
        }
    }

    /// <summary>선택지 하나를 올린다. lock: 식이 참이면 잠긴 채로 보인다.</summary>
    public class AddChoice : Command
    {
        public LocalizableTextParameter ChoiceSummary = new LocalizableTextParameter();
        public StringParameter GotoPath = new StringParameter();
        public StringParameter Lock = new StringParameter();
#pragma warning disable CS1998
        public override async Awaitable Execute (ExecutionContext ctx)
        {
            bool locked = Assigned(Lock) && MolakExpr.True(Lock.Value);
            Engine.Choices.Default.AddChoice(new Choice(Guid.NewGuid().ToString("N"), ChoiceSummary.Value.ToString(), PlaybackSpot.ScriptPath, Assigned(GotoPath) ? GotoPath.Value : null, locked));
        }
#pragma warning restore CS1998
    }

    public class SetCustomVariable : Command
    {
        public StringParameter Expression = new StringParameter();
#pragma warning disable CS1998
        public override async Awaitable Execute (ExecutionContext ctx) { if (Expression.HasValue) MolakExpr.Assign(Expression.ToString()); }
#pragma warning restore CS1998
    }

    public class Goto : Command
    {
        public StringParameter Path = new StringParameter();
#pragma warning disable CS1998
        public override async Awaitable Execute (ExecutionContext ctx) { Engine.Player.Track.Jump(Path.Value, PlaybackSpot.ScriptPath); }
#pragma warning restore CS1998
    }

    public class Gosub : Command
    {
        public StringParameter Path = new StringParameter();
#pragma warning disable CS1998
        public override async Awaitable Execute (ExecutionContext ctx) { var t = Engine.Player.Track; t.PushReturn(); t.Jump(Path.Value, PlaybackSpot.ScriptPath); }
#pragma warning restore CS1998
    }

    public class Return : Command
    {
#pragma warning disable CS1998
        public override async Awaitable Execute (ExecutionContext ctx) { Engine.Player.Track.PopReturn(); }
#pragma warning restore CS1998
    }

    public class Stop : Command
    {
#pragma warning disable CS1998
        public override async Awaitable Execute (ExecutionContext ctx) { Engine.Player.Track.RequestStop(); }
#pragma warning restore CS1998
    }

    /// <summary>@wait 초 — 그만큼 쉰다(넘기는 중이면 쉬지 않는다). @wait i 는 누를 때까지.</summary>
    public class Wait : Command
    {
        public StringParameter WaitMode = new StringParameter();
        public override async Awaitable Execute (ExecutionContext ctx)
        {
            var track = Engine.Player.Track; int my = track.Session;
            string m = WaitMode.HasValue ? WaitMode.Value : "";
            float sec;
            if (float.TryParse(m, NumberStyles.Float, CultureInfo.InvariantCulture, out sec))
            {
                float until = Time.time + sec;
                while (Time.time < until && !Engine.Player.Skipping && track.Session == my) await Awaitable.NextFrameAsync();
            }
            else if (!Engine.Player.Skipping) track.SetAwaitInput(true);
        }
    }
}
