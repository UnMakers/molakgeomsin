using System.Collections.Generic;
using Naninovel;
using Naninovel.Commands;
using UnityEngine;

/// <summary>
/// 대사 흐름 — 같은 맥락(같은 스크립트·인접 행)의 문장은 지우지 않고 아래로 쌓고,
/// 맥락이 끊길 때(스크립트/라벨 점프, 장면 전환, 선택 직후, 분량 초과)만 비운다.
/// </summary>
public class MolakFlow : MonoBehaviour
{
    public static int MaxMessages = 40;    // 쌓이는 최대 문장 수(성능상 상한). 넘친 본문은 MolakScroll 이 위로 밀어 두어 스크롤로 다시 본다
    public static int MaxChars = 900;      // 쌓이는 최대 글자 수(초과 시 다음 문장에서 비움)
    public static int MaxLineGap = 7;      // 이 이상 행이 건너뛰면 맥락 전환으로 간주

    static MolakFlow inst;
    public static string LastDecision = "";
    /// <summary>다음 문장부터 새 단락(화면 비움) — 장면/날짜 전환 시 호출</summary>
    public static void Break () { if (inst != null && !inst.holdPage) inst.breakNext = true; }
    /// <summary>0.4.41 이음 문장: 다음 본문(이음 문장)을 새 쪽 첫 줄로, 입력 없이 찍고, 그 뒤 사건 첫 글은 같은 쪽에 잇는다.</summary>
    /// <summary>이음 문장(@bridge)이 그림을 직접 맞췄으니 맥락 전환 그림 되돌리기는 하지 않는다.</summary>
    public static void CancelArtReset () { if (inst != null) inst.artResetPending = false; }
    public static void BridgeNext () { if (inst != null) { inst.breakNext = true; inst.bridgePending = true; } }
    bool bridgePending, holdPage;
    // 0.4.44 그림도 글처럼: 맥락이 바뀌어(다른 대본으로·하루 흐름 라벨로·날 넘김) 글이 새 쪽이 될 때, 대본이 그림을 따로 정하지 않았으면
    //        하루의 바탕 그림으로 되돌린다. 선택지 직후·쪽이 꽉 차서 넘길 때·같은 대본 안의 처리 루틴(gosub)은 같은 자리라 그대로 둔다.
    bool artResetPending;
    string lastPrintScript = "";
    bool returning;
    IScriptPlayer player;
    string lastScript = "";
    int lastLine = -9999;
    bool breakNext = true;
    static bool afterChoice;
    bool lastWasChoice;
    bool lastPrintWaited = true;   // 직전 본문이 입력 대기였는가 → 다음 본문은 '새 단락'
    int runParas, runChars;         // 0.4.37: 빈 줄 없이 이어 붙은 단락 수·글자 수
    public static int RunBreakChars = 80;
    bool fastBlock;                // 단락 출력 중 탭 → 남은 줄 즉시 출력
    IInputHandle continueHandle;
    static readonly List<Choice> cbuf = new List<Choice>();
    static void ClearStaleChoices ()
    {
        var chm = Engine.GetService<IChoiceHandlerManager>();
        if (chm == null || string.IsNullOrEmpty(chm.DefaultHandlerId) || !chm.ActorExists(chm.DefaultHandlerId)) return;
        var act = chm.GetActor(chm.DefaultHandlerId);
        cbuf.Clear(); act.CollectChoices(cbuf);
        foreach (var c in cbuf) act.RemoveChoice(c.Id);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot ()
    {
        if (inst) return;
        var go = new GameObject("MolakFlow");
        DontDestroyOnLoad(go);
        inst = go.AddComponent<MolakFlow>();
    }

    void Update ()
    {
        if (player != null) return;
        if (!Engine.Initialized) return;
        player = Engine.GetService<IScriptPlayer>();
        if (player == null) return;
        player.OnExecute += OnExecute;
        var im = Engine.GetService<IInputManager>();
        continueHandle = im != null ? im.GetInput("Continue") : null;
        // 나니노벨 기본 배경 액터(MainBackground)는 쓰지 않는다 — 리소스 없는 placeholder 사각형이 화면 중앙에 그려지는 것을 막는다
        var bm = Engine.GetService<IBackgroundManager>() as IActorManager;
        if (bm != null)
        {
            if (bm.ActorExists("MainBackground")) bm.GetActor("MainBackground").Visible = false;
            bm.OnActorAdded += id => { if (bm.ActorExists(id)) bm.GetActor(id).Visible = false; };
        }
    }

    void OnDestroy ()
    {
        if (player != null) player.OnExecute -= OnExecute;

    }

    void OnExecute (IScriptTrack track)
    {
        var cmd = track != null ? track.PlayedCommand : null;
        if (cmd == null) return;
        var spot = cmd.PlaybackSpot;
        var tn = cmd.GetType().Name;
        // 다른 대본으로 넘어간 첫 명령 = 맥락이 바뀌었다(그 대본이 곧 @art 를 부르면 거기서 취소된다)
        //  (gosub 에서 돌아온 것은 원래 자리로 돌아온 것 — 비무를 마치고 사건으로 돌아오면 그 사건 그림이 맞다)
        string cs = spot.ScriptPath ?? "";
        if (lastPrintScript.Length > 0 && cs != lastPrintScript && !returning) artResetPending = true;
        returning = cmd is Return;   // 바로 다음 명령이 돌아간 자리다
        lastPrintScript = cs;

        // 맥락을 끊는 명령: 장면 전환·선택지 제시·정지·말로 카드
        if (tn.Contains("Ending")) breakNext = true;
        // 맥락 전환 = 본문 초기화: 장면 명령, 사건/서브루틴 진입·복귀, 다른 스크립트로 이동, 하루 허브 라벨로 이동
        if (tn == "SceneCommand" || tn == "DayTickCommand" || tn == "InitRunCommand" || tn == "PageCommand") breakNext = true;
        if (tn == "DayTickCommand" || tn == "InitRunCommand" || tn == "PageCommand") artResetPending = true;
        if (tn == "SceneCommand") artResetPending = false;                 // @scene 이 그림을 정한다
        if (cmd is ArtCommand)
        {
            var ac = (ArtCommand)cmd;
            if (Assigned(ac.SceneId)) { breakNext = true; artResetPending = false; }            // 대본이 장면을 정했다
            else if (artResetPending && Assigned(ac.CharId)) { artResetPending = false; MolakArt.ResetToBase(); }   // 인물만 정하면 바탕부터 깐 뒤 인물(그 사람 자리 그림이 깔린다)
        }
        if (cmd is Gosub || cmd is Return) breakNext = true;
        if (cmd is Goto)
        {
            var g = (Goto)cmd;
            string pv = Assigned(g.Path) ? (g.Path.Value ?? "") : "";
            int hi = pv.IndexOf('#');
            string gs = hi >= 0 ? pv.Substring(0, hi) : pv;
            string gl = hi >= 0 ? pv.Substring(hi + 1) : null;
            string curPath = track.PlayedScript != null ? track.PlayedScript.Path : "";
            if (!string.IsNullOrEmpty(gs) && gs != curPath) breakNext = true;
            else if (!string.IsNullOrEmpty(gl) && IsHubLabel(gl)) { breakNext = true; artResetPending = true; }
        }

        // 새 선택지 묶음이 시작되면 처리되지 않은 옛 선택지를 정리 (뭉탱이 방지)
        if (cmd is AddChoice)
        {
            if (!lastWasChoice) { ClearStaleChoices(); MolakChoiceMeta.Reset(); }
            string cexp = cmd.ConditionalExpression.HasValue ? cmd.ConditionalExpression.Value : "";
            bool shown = true;
            if (!string.IsNullOrEmpty(cexp)) { try { shown = Naninovel.ExpressionEvaluator.Evaluate<bool>(cexp); } catch { shown = true; } }
            if (shown) MolakChoiceMeta.Push(cexp, ChoiceText(cmd));   // 조건이 거짓이라 안 뜨는 선택지는 카드 순서에서 뺀다
            lastWasChoice = true;
            afterChoice = true;   // 선택지 뒤의 첫 본문은 빈 줄로 단락을 나눈다
        }
        else if (!(cmd is Stop)) lastWasChoice = false;

        var pt = cmd as PrintText;
        if (pt == null) return;
        // 0.4.9: {dejapre} 한 줄(지난 생 장면). 비어 있으면 아무것도 찍지 않고 흐름에도 끼지 않는다.
        if (pt.Text != null && pt.Text.ToString().Trim() == "{dejapre}")
        {
            string dv = MolakStats.Str("dejapre");
            if (string.IsNullOrEmpty(dv) || dv == "none" || dv == "\u200B") { pt.ResetPrinter = false; pt.WaitForInput = false; return; }
        }
        EnsureParagraphBreaks();
        // (컴파일러가 generic 라인에 reset을 미리 박아 두므로 항상 덮어쓴다)

        Overflow();   // 성능상 하드 캡
        // 0.4.47 쪽 단위: 글이 내려가며 움직이면 읽기 흐트러진다(언메이커). 이 글까지 찍으면 한 쪽을 넘치겠으면 글만 비우고 새 쪽 맨 위부터(그림은 그대로).
        bool willReset = breakNext || afterChoice;
        bool ovf = !willReset && !(holdPage && !bridgePending) && PageWouldOverflow(TextOf(pt));
        // 다른 대본에서 넘어와 찍는 첫 글이면 맥락이 바뀐 것
        if (artResetPending) { artResetPending = false; if (!holdPage) MolakArt.ResetToBase(); }
        bool isBridge = bridgePending; bridgePending = false;
        if (holdPage && !isBridge) { breakNext = false; }      // 이음 문장 바로 뒤: 사건의 장면 전환·gosub 이 쪽을 넘기지 않는다
        bool held = holdPage && !isBridge; holdPage = false;
        bool reset = breakNext || ovf || afterChoice;   // 선택지를 고른 뒤의 첫 본문은 새 화면
        LastDecision = spot.ScriptPath + ":" + spot.LineNumber + " reset=" + reset + " break=" + breakNext + " ovf=" + ovf + " hasValue=" + pt.ResetPrinter.HasValue;


        if (afterChoice) MolakInputGuard.MuteFor(0.22f);   // 0.4.43: 0.45→0.22초(언메이커: 너무 안 넘어간다). 0.4.39: 고르는 탭의 여파가 첫 문단의 타자 효과를 건너뛰지 않게
        pt.ResetPrinter = reset;
        if (reset) { var prp = Printer(); if (prp != null) { buf.Clear(); prp.CollectMessages(buf); if (buf.Count > 0) MolakPageTurn.Play(); } }   // 글이 있던 쪽을 넘긴다
        MolakScroll.ResetToBottom();
        // 새 단락 시작(직전 본문이 대기였음) + 화면을 비우지 않을 때 → 빈 줄 하나로 단락 구분
        // 0.4.9: 대사 줄은 앞뒤로 한 줄 띄운다(서술과 대사가 붙어 있으면 읽기 어렵다)
        bool curDlg = IsDialogue(pt);
        bool beat = lastPrintWaited || afterChoice || curDlg || prevDlg || held;
        // 0.4.37: 한 번에 쭉 나오는 서술이 길면 숨 쉴 자리를 둔다 — 빈 줄 없이 이어진 단락이 둘 이상이고 글자가 RunBreakChars 를 넘었으면
        //         다음 단락 앞에 빈 줄 하나(예: 첫 거리 장면 "…사람들이 길을 비킨다." 다음).
        int len = PlainLen(pt);
        if (!reset && !beat && runParas >= 2 && runChars >= RunBreakChars) beat = true;
        if (reset || beat) { runParas = 0; runChars = 0; }
        runParas++; runChars += len;
        if (!reset) InsertBreak(beat ? "\n\n" : "\n");
        prevDlg = curDlg;
        afterChoice = false;
        // 같은 단락(연속된 서사 문장)은 한 번의 탭으로 쭉 출력: 다음 명령이 또 본문이면 입력 대기 없이 이어간다.
        bool wait = !NextIsText(track);
        // 0.4.47: 탭 한 번에 이어 나올 다음 문단이 쪽을 넘기게 되면, 쪽을 넘기기 전에 한 번 기다린다(읽던 글이 바로 지워지지 않게)
        if (!wait && !isBridge)
        {
            var nx = NextText(track);
            if (nx != null)
            {
                var dt = DialogueText();
                string cur = TextOf(pt), page = reset || dt == null ? cur : (dt.text ?? "") + "\n\n" + cur;
                if (PageWouldOverflow(TextOf(nx), page)) wait = true;
            }
        }
        if (isBridge) { wait = false; holdPage = true; }   // 이음 문장은 기다리지 않고 바로 사건으로
        pt.WaitForInput = wait;
        lastPrintWaited = wait;
        breakNext = false;
        Mark(spot);
    }

    static readonly System.Collections.Generic.HashSet<string> HubLabels = new System.Collections.Generic.HashSet<string> {
        "DayLoop", "Daytime", "Night", "NightChoice", "NightEnd", "Evening", "AfterChapter", "NightTown", "Town", "TownMenu", "Entry", "Rebirth", "Prologue"
    };
    static bool IsHubLabel (string l) { return HubLabels.Contains(l); }
    static bool Assigned<T> (T p) where T : Naninovel.ICommandParameter { return p != null && p.HasValue; }

    /// <summary>재생 목록에서 다음 '의미 있는' 명령이 본문 출력인지 (변수 설정·능력치·인물 삽화 명령은 건너뜀)</summary>
    static bool NextIsText (IScriptTrack track)
    {
        var pl = track.Playlist; if (pl == null) return false;
        int idx = track.PlayedIndex;
        for (int k = idx + 1; k < pl.Count; k++)
        {
            var c = pl[k];
            if (c is PrintText) return true;
            if (c is AddChoice) return false;           // 0.4.8: 마지막 글을 다 읽고 한 번 더 눌러야 선택지가 뜬다(선택지가 글을 가리지 않게)
            var n = c.GetType().Name;
            // 흐름을 끊는 명령 = 단락 경계
            if (c is Gosub || c is Return || c is Stop || c is Wait) return false;
            if (c is Goto) { var g = (Goto)c; if (!g.ConditionalExpression.HasValue && !g.InvertedConditionalExpression.HasValue) return false; continue; }
            if (n == "SceneCommand" || n.Contains("Ending") || n == "DayTickCommand" || n == "InitRunCommand" || n == "PageCommand") return false;
            if (n == "ArtCommand")
            {
                var f = c.GetType().GetField("SceneId"); var v = f != null ? f.GetValue(c) as StringParameter : null;
                var off = c.GetType().GetField("Off"); var ov = off != null ? off.GetValue(c) as BooleanParameter : null;
                if ((v != null && v.HasValue) || (ov != null && ov.HasValue && ov.Value)) return false;
                continue;
            }
            continue; // 변수·능력치·연출·기타 명령은 통과
        }
        return false;
    }

    static bool prevDlg;
    /// <summary>선택지 글귀(번역 키를 풀어 낸 실제 글). 승부수 표시 〔검술+2〕 를 읽는 데 쓴다.</summary>
    static string ChoiceText (Command cmd)
    {
        try
        {
            foreach (var f in cmd.GetType().GetFields())
            {
                if (!(f.GetValue(cmd) is LocalizableTextParameter p) || !p.HasValue) continue;
                var loc = Engine.GetService<ITextLocalizer>();
                return loc != null ? loc.Resolve(p.Value) : p.Value.ToString();
            }
        }
        catch { }
        return null;
    }

    static int PlainLen (PrintText pt)
    {
        try
        {
            if (pt.Text == null || !pt.Text.HasValue) return 0;
            var loc = Engine.GetService<ITextLocalizer>();
            var t = loc != null ? loc.Resolve(pt.Text.Value) : pt.Text.Value.ToString();
            return string.IsNullOrEmpty(t) ? 0 : System.Text.RegularExpressions.Regex.Replace(t, "<[^>]+>", "").Trim().Length;
        }
        catch { return 0; }
    }

    static bool IsDialogue (PrintText pt)
    {
        try
        {
            if (pt.Text == null || !pt.Text.HasValue) return false;
            var loc = Engine.GetService<ITextLocalizer>();
            var t = loc != null ? loc.Resolve(pt.Text.Value) : pt.Text.Value.ToString();
            if (string.IsNullOrEmpty(t)) return false;
            t = System.Text.RegularExpressions.Regex.Replace(t, "<[^>]+>", "").TrimStart();
            return t.StartsWith("\"") || t.StartsWith("“") || t.StartsWith("「");
        }
        catch { return false; }
    }

    void Mark (PlaybackSpot spot) { lastScript = spot.ScriptPath; lastLine = spot.LineIndex; }

    static ITextPrinterActor Printer ()
    {
        var pm = Engine.GetService<ITextPrinterManager>();
        if (pm == null) return null;
        var id = pm.DefaultPrinterId;
        if (string.IsNullOrEmpty(id) || !pm.ActorExists(id)) return null;
        return pm.GetActor(id);
    }

    // 문단 사이 빈 줄: RevealableTextPrinterPanel.autoLineBreaks(직렬화 전용)를 런타임에 설정
    static bool breaksSet;
    static void EnsureParagraphBreaks ()
    {
        if (breaksSet) return;
        foreach (var panel in Object.FindObjectsByType<Naninovel.UI.RevealableTextPrinterPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var f = typeof(Naninovel.UI.RevealableTextPrinterPanel).GetField("autoLineBreaks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f != null) { f.SetValue(panel, 0); breaksSet = true; }
        }
    }

    static readonly List<PrintedMessage> buf = new List<PrintedMessage>();
    static void InsertBreak (string br)
    {
        var pr = Printer(); if (pr == null) return;
        buf.Clear(); pr.CollectMessages(buf);
        if (buf.Count == 0) return;
        pr.AppendText(LocalizableText.FromPlainText(br));
    }

    /// <summary>주어진 높이(캔버스 단위)에 맞게 오래된 문단부터 제거 — 선택지가 떴을 때 호출</summary>
    public static void TrimToHeight (float maxH)
    {
        var pr = Printer(); if (pr == null) return;
        buf.Clear(); pr.CollectMessages(buf); if (buf.Count <= 1) return;
        TMPro.TMP_Text tx = null;
        foreach (var panel in Object.FindObjectsByType<Naninovel.UI.RevealableTextPrinterPanel>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            foreach (var t0 in panel.GetComponentsInChildren<TMPro.TMP_Text>(false))
                if (t0.gameObject.name == "DialogueText") tx = t0;
        if (tx == null) return;
        int guard = 0;
        while (buf.Count > 1 && tx.preferredHeight > maxH && guard++ < 20)
        {
            buf.RemoveAt(0);
            pr.SetMessages(new List<PrintedMessage>(buf));
            tx.ForceMeshUpdate();
        }
    }

    /// <summary>본문이 화면 영역을 넘치면 가장 오래된 문단부터 밀어낸다(서울2033식 흐름 유지). 넘치지 않으면 false.</summary>
    /// <summary>번역 키·변수를 풀어 낸 본문(어림용).</summary>
    static string TextOf (PrintText pt)
    {
        try
        {
            if (pt == null || pt.Text == null || !pt.Text.HasValue) return "";
            // 0.4.52: 아직 실행 전인 줄의 {변수} 를 미리 풀면 나니노벨이 'Unexpected parameter value part' 오류를 낸다 → 어림에는 날 글을 쓴다
            if (pt.Text.DynamicValue) return pt.Text.ToString() ?? "";
            var loc = Engine.GetService<ITextLocalizer>();
            return (loc != null ? loc.Resolve(pt.Text.Value) : pt.Text.Value.ToString()) ?? "";
        }
        catch { return ""; }
    }

    /// <summary>다음 '의미 있는' 본문 출력 명령(NextIsText 와 같은 규칙). 없으면 null.</summary>
    static PrintText NextText (IScriptTrack track)
    {
        if (!NextIsText(track)) return null;
        var pl = track.Playlist;
        for (int k = track.PlayedIndex + 1; k < pl.Count; k++) if (pl[k] is PrintText) return (PrintText)pl[k];
        return null;
    }

    static TMPro.TMP_Text DialogueText ()
    {
        foreach (var panel in Object.FindObjectsByType<Naninovel.UI.RevealableTextPrinterPanel>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            foreach (var t0 in panel.GetComponentsInChildren<TMPro.TMP_Text>(false))
                if (t0.gameObject.name == "DialogueText") return t0;
        return null;
    }

    /// <summary>지금 쪽의 글(existing, null 이면 화면의 글) 뒤에 addition 을 붙이면 한 쪽을 넘치나. 쪽이 비어 있으면 넘치지 않는다고 본다(긴 한 문단은 스크롤로).</summary>
    static bool PageWouldOverflow (string addition, string existing = null)
    {
        if (string.IsNullOrEmpty(addition)) return false;
        var tx = DialogueText();
        if (tx == null) return false;
        float vpH = MolakScroll.TargetBodyHeight();
        if (vpH <= 0f) return false;
        string cur = existing ?? tx.text ?? "";
        if (cur.Trim().Length == 0) return false;
        float w = tx.rectTransform.rect.width;
        if (w <= 1f) return false;
        var m = Measurer(tx);
        if (m == null) return false;
        float h = m.GetPreferredValues(cur + "\n\n" + addition, w, 0f).y;
        return h > vpH * 0.97f;
    }

    // 본문 칸(DialogueText)으로 GetPreferredValues 를 부르면 그 칸의 글자 정보가 바뀌어 글자 드러내기가 깨진다 → 같은 설정의 보이지 않는 칸으로 잰다
    static TMPro.TextMeshProUGUI measurer;
    static TMPro.TextMeshProUGUI Measurer (TMPro.TMP_Text src)
    {
        if (measurer == null)
        {
            var go = new GameObject("MolakMeasure", typeof(RectTransform));
            go.transform.SetParent(src.transform.parent, false);
            measurer = go.AddComponent<TMPro.TextMeshProUGUI>();
            measurer.raycastTarget = false;
            measurer.color = new Color(0f, 0f, 0f, 0f);
            measurer.text = "";
        }
        else if (measurer.transform.parent != src.transform.parent) measurer.transform.SetParent(src.transform.parent, false);
        measurer.font = src.font; measurer.fontSharedMaterial = src.fontSharedMaterial;
        measurer.fontSize = src.fontSize; measurer.fontStyle = src.fontStyle;
        measurer.lineSpacing = src.lineSpacing; measurer.paragraphSpacing = src.paragraphSpacing;
        measurer.characterSpacing = src.characterSpacing; measurer.wordSpacing = src.wordSpacing;
        measurer.margin = src.margin; measurer.richText = src.richText;
        measurer.textWrappingMode = src.textWrappingMode; measurer.textPreprocessor = src.textPreprocessor;
        measurer.alignment = src.alignment;
        return measurer;
    }

    bool Overflow ()
    {
        var pr = Printer();
        if (pr == null) return false;
        buf.Clear(); pr.CollectMessages(buf);
        if (buf.Count == 0) return false;
        TMPro.TMP_Text tx = null;
        foreach (var panel in Object.FindObjectsByType<Naninovel.UI.RevealableTextPrinterPanel>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            foreach (var t0 in panel.GetComponentsInChildren<TMPro.TMP_Text>(false))
                if (t0.gameObject.name == "DialogueText") tx = t0;
        if (tx == null) return buf.Count >= MaxMessages;
        // 넘친 본문은 지우지 않고 MolakScroll 이 뷰포트 밖으로 밀어 둔다(스크롤로 다시 볼 수 있음). 성능상 하드 캡만 유지.
        int guard = 0;
        while (buf.Count > MaxMessages && guard++ < 20)
        {
            buf.RemoveAt(0);
            pr.SetMessages(new List<PrintedMessage>(buf));
            tx.ForceMeshUpdate();
        }
        return false;
    }
}
