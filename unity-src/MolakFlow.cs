using System.Collections.Generic;
using MolakVn;
using MolakVn.Commands;
using UnityEngine;

/// <summary>0.4.90 글자리를 덮거나 화면을 가리는 연출 명령(인장·기연 판·먹 화면·판이 바뀌는 날 카드·광란·이름 화면·비무 판).
/// 기준: 이런 명령 바로 앞의 글은 읽을 틈을 준 뒤(한 번 누른 뒤)에 연출이 뜬다 — 대본 흐름(MolakFlow.NextIsText·NextBreaksPage)이 단락 끝으로 센다.
/// 예전엔 연출 명령을 지나쳐 보고 그 뒤에 또 글이 있으면 기다리지 않아, 「단전에서 열이 터진다.」 같은 한 줄이 찍히자마자 기연 판·인장에 덮였다
/// (언메이커: 선택지 고른 직후 한 줄짜리가 읽기 전에 사라진다). CoversText 가 거짓이면(그때그때 덮지 않는 종류) 지나쳐 본다.</summary>
public interface IMolakMoment { bool CoversText { get; } }

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
    /// <summary>0.4.98 시험용: true 면 옛 쪽 넘김 어림(날 {변수}·이음 뒤 안 잼)으로 — 넘침 점검 비교에만 쓴다.</summary>
    public static bool OldFit;
    /// <summary>다음 문장부터 새 단락(화면 비움) — 장면/날짜 전환 시 호출</summary>
    public static void Break () { if (inst != null && !inst.holdPage) inst.breakNext = true; }
    /// <summary>0.4.41 이음 문장: 다음 본문(이음 문장)을 새 쪽 첫 줄로, 입력 없이 찍고, 그 뒤 사건 첫 글은 같은 쪽에 잇는다.</summary>
    /// <summary>이음 문장(@bridge)이 그림을 직접 맞췄으니 맥락 전환 그림 되돌리기는 하지 않는다.</summary>
    public static void CancelArtReset () { if (inst != null) inst.artResetPending = false; }
    public static void BridgeNext () { if (inst != null) { inst.breakNext = true; inst.bridgePending = true; } }
    bool bridgePending, holdPage;
    // 0.4.68 쪽 넘김 문(언메이커: 빨리 모드에서 '팍팍팍' 넘어간다 · 넘기면 다음 대사까지 넘어간다).
    //  · 읽을 틈을 준 적 없는 쪽(직전 글이 기다리지 않았고, 그 뒤로 누른 적도 없다)은 지우지 않는다 — 한 번 누를 때까지 붙잡는다.
    //  · 빨리 모드에서는 쪽이 한꺼번에 뜨므로 새 쪽이 뜬 직후 잠깐(0.35초) '계속'을 받지 않는다(한 번의 탭이 두 쪽을 넘기지 않게).
    bool lastFinalWait = true; float lastPrintAt = -10f, lastPressAt = -10f; bool prevDown;
    bool gatePending, gateTurn; float gateStart = -1f;
    public static bool Gating { get { return inst != null && inst.gateStart >= 0f; } }
    public static System.Text.StringBuilder Trace;
    /// <summary>인장·결과판·기연판처럼 글 위에 뜬 것을 사람이 눌러 걷었다 = 읽을 틈을 줬다.</summary>
    /// <summary>0.4.95 지금 쪽에 읽을 글이 떠 있는가(인쇄기에 찍힌 글이 있고 보이는 글자가 있다). 글이 떠 있는 동안 그림은 다음 글과 함께 바뀐다(MolakArt).</summary>
    public static bool PageHasText
    {
        get
        {
            var pr = Printer(); if (pr == null || !pr.Visible) return false;
            buf.Clear(); pr.CollectMessages(buf); if (buf.Count == 0) return false;
            var dt = DialogueText(); if (dt == null || !dt.isActiveAndEnabled) return false;
            return System.Text.RegularExpressions.Regex.Replace(dt.text ?? "", "<[^>]+>", "").Replace("\u200B", "").Trim().Length > 0;
        }
    }
    /// <summary>0.4.95 효과(@vfx)가 터질 때: 읽던 글이 없는 쪽이면 기다리던 그림을 지금 세운다. 글이 떠 있으면 다음 글과 함께.</summary>
    public static void CommitForBeat () { if (!PageHasText) CommitArt(); }
    /// <summary>0.4.96 지금 쪽을 바로 비우고 다음 글은 새 쪽 맨 위부터(마디 이름이 뜰 때). 비우는 쪽의 글은 지난 쪽 보기에 남긴다.</summary>
    public static void ClearPageNow ()
    {
        if (inst == null) return;
        var pr = Printer(); if (pr == null) return;
        buf.Clear(); pr.CollectMessages(buf); if (buf.Count == 0) { inst.breakNext = true; return; }
        var dt = DialogueText(); if (dt != null) MolakPageBack.Push(dt.text);
        pr.SetMessages(new List<PrintedMessage>());
        inst.breakNext = true;
    }
    public static void Ack () { if (inst != null) inst.lastPressAt = Time.unscaledTime; }
    // 0.4.44 그림도 글처럼: 맥락이 바뀌어(다른 대본으로·하루 흐름 라벨로·날 넘김) 글이 새 쪽이 될 때, 대본이 그림을 따로 정하지 않았으면
    //        하루의 바탕 그림으로 되돌린다. 선택지 직후·쪽이 꽉 차서 넘길 때·같은 대본 안의 처리 루틴(gosub)은 같은 자리라 그대로 둔다.
    bool artResetPending, artSetSincePrint;
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
    static readonly System.Collections.Generic.Dictionary<Command, string> origCond = new System.Collections.Generic.Dictionary<Command, string>();
    object preparedList;
    /// <summary>0.4.63 대본(재생 목록)이 바뀔 때 한 번: '갖추면 열리는' 조건만 붙은 @choice 의 if: 를 lock:!(조건) 으로 옮긴다.
    /// 나니노벨은 if: 가 거짓인 명령은 OnExecute 도 부르지 않고 건너뛰므로, 실행 때 바꾸면 늦다.</summary>
    void PrepareLocks (IScriptTrack track)
    {
        var pl = track != null ? track.Playlist : null;
        if (pl == null || ReferenceEquals(pl, preparedList)) return;
        preparedList = pl;
        for (int i = 0; i < pl.Count; i++)
        {
            var ac = pl[i] as AddChoice;
            if (ac == null || origCond.ContainsKey(ac)) continue;
            if (!ac.ConditionalExpression.HasValue) continue;
            string c = ac.ConditionalExpression.Value;
            if (!MolakChoiceMeta.Lockable(c) || (ac.Lock != null && ac.Lock.HasValue)) continue;
            origCond[ac] = c;
            ac.Lock = "!(" + c + ")";
            ac.ConditionalExpression = new StringParameter();
        }
    }

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
        Vector2 pp; bool dn = MolakGui.PointerScreen(out pp);
        if (dn && !prevDown) lastPressAt = Time.unscaledTime;
        prevDown = dn;
        if (player != null) return;
        if (!Engine.Initialized) return;
        player = Engine.GetService<IScriptPlayer>();
        if (player == null) return;
        player.OnExecute += OnExecute;
        player.AddPreExecutionTask(PrePrint);
        player.OnPlay += PrepareLocks;   // 새 대본을 틀 때 첫 명령보다 먼저
        var im = Engine.GetService<IInputManager>();
        continueHandle = im != null ? im.GetInput("Continue") : null;
        if (continueHandle != null) continueHandle.OnStart += () => { lastPressAt = Time.unscaledTime; };
    }

    void OnDestroy ()
    {
        if (player != null) { player.OnExecute -= OnExecute; player.OnPlay -= PrepareLocks; }

    }

    void OnExecute (IScriptTrack track)
    {
        PrepareLocks(track);
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
        if (tn == "DayTickCommand" || tn == "InitRunCommand" || (tn == "PageCommand" && !artSetSincePrint)) artResetPending = true;   // 0.4.85 @art scene → @page 순이면 방금 정한 그림을 바탕으로 되돌리지 않는다(번짐이 세 번 돌던 것: 골목 → 바탕 → 다시 골목)
        if (tn == "SceneCommand") artResetPending = false;                 // @scene 이 그림을 정한다
        if (cmd is ArtCommand)
        {
            var ac = (ArtCommand)cmd;
            if (Assigned(ac.SceneId)) { breakNext = true; artResetPending = false; artSetSincePrint = true; }            // 대본이 장면을 정했다
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
            CommitArt();   // 선택지가 뜨기 전에 그림부터
            if (!lastWasChoice) { ClearStaleChoices(); MolakChoiceMeta.Reset(); }
            // 0.4.63 잠긴 선택지: 대본을 틀 때 '갖추면 열리는' if: 를 lock:!(조건) 으로 옮겨 두었다(PrepareLocks). 여기서는 원래 조건으로 주석을 단다.
            string cexp; bool lockIt = false;
            if (origCond.TryGetValue(cmd, out cexp))
            { try { lockIt = !MolakVn.ExpressionEvaluator.Evaluate<bool>(cexp); } catch { lockIt = false; } }
            else cexp = cmd.ConditionalExpression.HasValue ? cmd.ConditionalExpression.Value : "";
            bool shown = true;
            if (!lockIt && !string.IsNullOrEmpty(cexp)) { try { shown = MolakVn.ExpressionEvaluator.Evaluate<bool>(cexp); } catch { shown = true; } }
            if (shown || lockIt) MolakChoiceMeta.Push(cexp, ChoiceText(cmd), lockIt);   // 조건이 거짓이라 안 뜨는 선택지는 카드 순서에서 뺀다
            lastWasChoice = true;
            afterChoice = true;   // 선택지 뒤의 첫 본문은 빈 줄로 단락을 나눈다
        }
        else if (!(cmd is Stop)) lastWasChoice = false;

        var pt = cmd as PrintText;
        if (pt == null) return;
        artSetSincePrint = false;
        // 0.4.83 말하는 사람(@spk id:soa) — 바로 다음 한 줄에만 붙는다. 없으면 글로 어림한다(MolakScroll.CharSpeaking).
        CurrentSpeaker = PendingSpeaker; PendingSpeaker = null;
        // 0.4.9: {dejapre} 한 줄(지난 생 장면). 비어 있으면 아무것도 찍지 않고 흐름에도 끼지 않는다.
        if (pt.Text != null && pt.Text.ToString().Trim() == "{dejapre}")
        {
            string dv = MolakStats.Str("dejapre");
            if (string.IsNullOrEmpty(dv) || dv == "none" || dv == "\u200B") { pt.ResetPrinter = false; pt.WaitForInput = false; return; }
        }
        MolakStatLog.Line(TextOf(pt));   // 0.4.66 능력치 기록: 변화 앞의 마지막 서술
        EnsureParagraphBreaks();
        // (컴파일러가 generic 라인에 reset을 미리 박아 두므로 항상 덮어쓴다)

        Overflow();   // 성능상 하드 캡
        // 0.4.47 쪽 단위: 글이 내려가며 움직이면 읽기 흐트러진다(언메이커). 이 글까지 찍으면 한 쪽을 넘치겠으면 글만 비우고 새 쪽 맨 위부터(그림은 그대로).
        bool willReset = breakNext || afterChoice;
        // 0.4.98 이음 문장 바로 뒤의 글도 넘치는지 잰다 — 넘치면 이음 문장을 앞 쪽 끝에 두고 새 쪽(쪽이 꽉 찬 채 글이 위로 밀려 올라가던 것)
        bool ovf = !willReset && !(OldFit && holdPage && !bridgePending) && PageWouldOverflow(TextOf(pt));
        // 다른 대본에서 넘어와 찍는 첫 글이면 맥락이 바뀐 것
        if (artResetPending) { artResetPending = false; if (!holdPage) MolakArt.ResetToBase(); }
        bool isBridge = bridgePending; bridgePending = false;
        if (holdPage && !isBridge) { breakNext = false; }      // 이음 문장 바로 뒤: 사건의 장면 전환·gosub 이 쪽을 넘기지 않는다
        bool held = holdPage && !isBridge; holdPage = false;
        // 0.4.82 그림 화면: 대사는 한 줄씩 제 쪽에(언메이커: 한 글자리에 소아 말과 다른 말이 섞이니 이름 인장이 오락가락한다).
        //   대사가 오거나, 쪽의 마지막 글이 대사였으면 새 쪽. 서술끼리는 그대로 이어 쓴다. 이음 문장 바로 뒤는 넘기지 않는다.
        bool cineSplit = false;
        if (MolakTheme.CinemaNow && !breakNext && !afterChoice && !ovf && !held && !isBridge && (IsDialogue(pt) || prevDlg))
        { var dtc = DialogueText(); cineSplit = dtc != null && (dtc.text ?? "").Replace("\u200B", "").Trim().Length > 0; }
        bool reset = breakNext || ovf || afterChoice || cineSplit;   // 선택지를 고른 뒤의 첫 본문은 새 화면
        LastDecision = spot.ScriptPath + ":" + spot.LineNumber + " reset=" + reset + " break=" + breakNext + " ovf=" + ovf + " hasValue=" + pt.ResetPrinter.HasValue;


        if (afterChoice) MolakInputGuard.MuteFor(0.22f);   // 0.4.43: 0.45→0.22초(언메이커: 너무 안 넘어간다). 0.4.39: 고르는 탭의 여파가 첫 문단의 타자 효과를 건너뛰지 않게
        MolakPageBack.Close();   // 0.4.57 지난 쪽을 보던 중이면 지금 쪽으로
        if (reset)
        {   // 0.4.57 넘어가는 쪽의 글을 적어 둔다(오른쪽으로 쓸어 다시 본다). 선택지를 고른 뒤의 첫 쪽이면 고르기 전 쪽들은 버린다.
            if (afterChoice && !MolakStory.Auto) { MolakPageBack.Clear(); MolakPageBack.MarkPicked(); }
            else if (afterChoice) { var dtq = DialogueText(); if (dtq != null) MolakPageBack.Push(dtq.text); }   // 0.4.95 이야기 모드: 고른 말이 적힌 쪽도 남긴다
            else { var dtp = DialogueText(); if (dtp != null) MolakPageBack.Push(dtp.text); }
        }
        else MolakPageBack.MarkPrinted();
        pt.ResetPrinter = reset;
        bool hadText = false;
        if (reset) { var prp = Printer(); if (prp != null) { buf.Clear(); prp.CollectMessages(buf); hadText = buf.Count > 0; } if (hadText) { var dth = DialogueText(); if (dth != null && (dth.text ?? "").Replace("\u200B", "").Trim().Length == 0) hadText = false; } }   // 0.4.80 보이지 않는 한 글자(빈 지난 생 줄)만 있는 쪽은 빈 쪽
        // 0.4.68 읽을 틈 없이 지워지는 쪽은 붙잡는다(FlipGate). 선택지 뒤·이음 문장·검증 넘기기는 제외.
        bool unread = reset && hadText && !afterChoice && !isBridge && !lastFinalWait && lastPressAt <= lastPrintAt + 0.05f;
        var spk = player; bool skipping = MolakPlayMode.SkipAll || (spk != null && spk.Skipping);
        if (unread && !skipping) { gatePending = true; gateTurn = true; }
        else if (reset && hadText) { MolakPageTurn.Play(); if (!skipping) MolakScroll.NewPage(); }   // 글이 있던 쪽을 넘긴다(0.4.91 새 쪽은 잠깐 떠오른다)
        // 0.4.85 기준: 그림은 글과 함께 — 지난 글 뒤로 바뀐 그림을 이 글 직전에 한 번에 올린다.
        // 0.4.95 읽을 틈 없던 쪽을 붙잡는 동안(FlipGate)은 올리지 않는다 — 빨리로 읽을 때 옛 쪽 글(하진무의 대추)이 남아 있는데 그림이 먼저 장터로 넘어갔다. 문이 열리면 올린다.
        if (!gatePending) CommitArt();
        if (reset && MolakPlayMode.Fast && !skipping && !unread) MolakInputGuard.MuteFor(0.35f);
        MolakScroll.ResetToBottom();
        // 새 단락 시작(직전 본문이 대기였음) + 화면을 비우지 않을 때 → 빈 줄 하나로 단락 구분
        // 0.4.9: 대사 줄은 앞뒤로 한 줄 띄운다(서술과 대사가 붙어 있으면 읽기 어렵다)
        bool curDlg = IsDialogue(pt);
        if (!curDlg && !isBridge) { var spk2 = player; if (!(MolakPlayMode.SkipAll || (spk2 != null && spk2.Skipping))) MolakArt.AutoGesture(System.Text.RegularExpressions.Regex.Replace(TextOf(pt) ?? "", "<[^>]+>", "")); }   // 0.4.85 몸짓
        // 0.4.88 독백('…')도 대사처럼 앞뒤로 한 줄 띄운다(속말이 서술에 묻히지 않게). 그림 화면의 대기 규칙은 대사만.
        bool curSp = curDlg || IsMonologue(pt);
        bool beat = lastPrintWaited || afterChoice || curSp || prevSp || held;
        // 0.4.37: 한 번에 쭉 나오는 서술이 길면 숨 쉴 자리를 둔다 — 빈 줄 없이 이어진 단락이 둘 이상이고 글자가 RunBreakChars 를 넘었으면
        //         다음 단락 앞에 빈 줄 하나(예: 첫 거리 장면 "…사람들이 길을 비킨다." 다음).
        int len = PlainLen(pt);
        if (!reset && !beat && runParas >= 2 && runChars >= RunBreakChars) beat = true;
        if (reset || beat) { runParas = 0; runChars = 0; }
        runParas++; runChars += len;
        if (!reset) InsertBreak(beat ? "\n\n" : "\n");
        prevDlg = curDlg; prevSp = curSp;
        afterChoice = false;
        // 같은 단락(연속된 서사 문장)은 한 번의 탭으로 쭉 출력: 다음 명령이 또 본문이면 입력 대기 없이 이어간다.
        bool wait = !NextIsText(track);
        // 0.4.82 그림 화면: 다음 글이 대사 쪽으로 갈라지면(이 글이 대사이거나 다음 글이 대사) 여기서 한 번 기다린다 — 읽던 글이 바로 지워지지 않게
        bool cineSplitNext = false;
        if (MolakTheme.CinemaNow && !isBridge && NextIsText(track)) { var nxc = NextText(track); if (nxc != null && (curDlg || IsDialogue(nxc))) cineSplitNext = true; }
        if (cineSplitNext) wait = true;
        // 0.4.47: 탭 한 번에 이어 나올 다음 문단이 쪽을 넘기게 되면, 쪽을 넘기기 전에 한 번 기다린다(읽던 글이 바로 지워지지 않게)
        if (!wait && !isBridge)
        {
            var nx = NextText(track);
            if (nx != null)
            {
                var dt = DialogueText();
                string cur = TextOf(pt), page = reset || dt == null ? cur : (dt.text ?? "").TrimEnd('\n', ' ') + "\n\n" + cur;   // 0.4.64 이미 넣은 단락 빈 줄을 두 번 세지 않게(쪽 끝을 일찍 잡아 쪽 가운데서 멈추던 것)
                if (PageWouldOverflow(TextOf(nx), page)) wait = true;
            }
        }
        if (isBridge) { wait = false; holdPage = true; }   // 이음 문장은 기다리지 않고 바로 사건으로
        lastPrintWaited = wait;   // 단락 나눔(빈 줄)은 보통 때 기준으로 — 빨리여도 쪽 모양이 같게
        // 0.4.64 빨리(한 쪽씩): 쪽 안의 단락은 기다리지 않고, 쪽 끝에서만 기다린다. 글은 타자 없이 한꺼번에.
        if (MolakPlayMode.Fast && !isBridge && wait)
        {
            bool overflowNext = false;
            var nx2 = NextText(track);
            if (nx2 != null && NextIsText(track))
            {
                var dt2 = DialogueText();
                string cur2 = TextOf(pt), page2 = reset || dt2 == null ? cur2 : (dt2.text ?? "").TrimEnd('\n', ' ') + "\n\n" + cur2;
                overflowNext = PageWouldOverflow(TextOf(nx2), page2);
            }
            if (!overflowNext && !cineSplitNext && !NextBreaksPage(track)) wait = false;
        }
        pt.RevealSpeed = MolakPlayMode.Fast ? (DecimalParameter)100f : new DecimalParameter();
        pt.WaitForInput = wait;
        if (Trace != null) Trace.AppendLine(spot.ScriptPath + ":" + (spot.LineNumber) + " reset=" + reset + (ovf ? "(ovf)" : "") + " wait=" + wait + (gatePending ? " GATE" : "") + " | " + Short(TextOf(pt)));
        lastFinalWait = wait; lastPrintAt = Time.unscaledTime;
        breakNext = false;
        Mark(spot);
    }

    /// <summary>0.4.95 글·선택지와 함께 그림을 올린다(MolakSyncCheck 가 이른 그림과 가른다).
    /// 0.5.0 먹 화면이 덮여 있으면 그 밑에서 그림을 바꾼 뒤 먹을 걷는다 — 걷히면 새 그림이 이미 서 있다.</summary>
    static void CommitArt ()
    {
        float before = MolakArt.ChangedAt;
        MolakSyncCheck.InPrint = true; try { MolakArt.Commit(); } finally { MolakSyncCheck.InPrint = false; }
        if (inst != null && MolakArt.ChangedAt != before) inst.artChanged = true;
        MolakBlackout.Release();
    }
    bool artChanged;

    /// <summary>0.5.0 실행 전 작업: 붙잡은 쪽(FlipGate) → 그림이 먼저(ArtHold).</summary>
    static async Awaitable PrePrint (IScriptTrack track)
    {
        await FlipGate(track);
        await ArtHold(track);
    }

    /// <summary>0.5.0 기준: 그림이 바뀌면 그림이 먼저, 글은 그 뒤에(언메이커: 그림이 바뀔 때 글이 먼저 나와 몰입을 해친다).
    /// 이 글과 함께 그림이 바뀌었거나 먹 화면·마디 덮개가 걷히는 중이면 — 새 쪽이면 옛 글을 먼저 걷고 — 번짐·떠오름·걷힘이 거의 끝날 때까지 글을 붙잡는다.
    /// 누르면 바로 글로 넘어간다. 넘기기·점검 봇은 붙잡지 않는다.</summary>
    static async Awaitable ArtHold (IScriptTrack track)
    {
        if (inst == null) return;
        var pt = track != null ? track.PlayedCommand as PrintText : null;
        if (pt == null) return;
        bool changed = inst.artChanged; inst.artChanged = false;
        var sp = inst.player;
        if (MolakPlayMode.SkipAll || (sp != null && sp.Skipping) || MolakVn.Engine.Turbo) return;
        bool covered = MolakBlackout.Lifting || MolakVfx.ChapterCover > 0.02f;
        if (!changed && !covered) return;
        bool reset = pt.ResetPrinter.HasValue && pt.ResetPrinter.Value;
        if (reset) { var pr = MolakVn.Engine.Printers.Default; pr.ClearMessages(); }   // 옛 쪽 글이 새 그림 위에 남지 않게
        float start = Time.unscaledTime, cap = start + (MolakPlayMode.Fast ? 0.7f : 1.8f);
        while (Time.unscaledTime < cap)
        {
            bool busy = MolakBlackout.Lifting || MolakVfx.ChapterCover > 0.02f || MolakArt.InkLeft > HoldInkTail || MolakArt.Busy;
            if (!busy) break;
            if (inst.lastPressAt > start + 0.05f && !MolakMenu.IsOpen && !MolakSettings.IsOpen) { MolakInputGuard.BlockCurrentPress(); break; }
            await Awaitable.NextFrameAsync();
            if (track.PlayedCommand != pt) return;
        }
        float beat = Time.unscaledTime + (MolakPlayMode.Fast ? 0.05f : 0.15f);   // 그림이 선 뒤 한 박자
        while (Time.unscaledTime < beat) await Awaitable.NextFrameAsync();
        if (Trace != null) Trace.AppendLine("  (그림 먼저 " + (Time.unscaledTime - start).ToString("0.00") + "초)");
    }
    /// <summary>번짐이 이만큼(초) 남았으면 글을 내보낸다 — 새 그림은 이미 거의 다 드러났다.</summary>
    const float HoldInkTail = 0.25f;

    static string Short (string t) { t = System.Text.RegularExpressions.Regex.Replace(t ?? "", "<[^>]+>", "").Replace("\n", " "); return t.Length > 28 ? t.Substring(0, 28) + "…" : t; }

    /// <summary>0.4.68 읽을 틈이 없던 쪽: 누를 때까지(자동이면 읽을 만큼) 다음 쪽을 찍지 않는다.</summary>
    static async Awaitable FlipGate (IScriptTrack track)
    {
        if (inst == null || !inst.gatePending) return;
        if (!(track != null && track.PlayedCommand is PrintText)) return;
        inst.gatePending = false;
        float start = Time.unscaledTime; inst.gateStart = start;
        int chars = 0; var dt = DialogueText(); if (dt != null) chars = (dt.text ?? "").Length;
        float autoAt = start + 1.6f + chars * 0.03f;
        while (true)
        {
            var sp = inst.player;
            if (MolakPlayMode.SkipAll || (sp != null && sp.Skipping)) break;
            if (inst.lastPressAt > start + 0.05f && !MolakMenu.IsOpen && !MolakSettings.IsOpen) { MolakInputGuard.BlockCurrentPress(); break; }
            if (MolakPlayMode.Auto && Time.unscaledTime >= autoAt) break;
            if (Time.unscaledTime - start > 600f) break;
            await Awaitable.NextFrameAsync();
        }
        inst.gateStart = -1f;
        CommitArt();   // 0.4.95 붙잡은 쪽을 넘기는 순간에 새 그림을 올린다
        if (inst.Trace0()) Trace.AppendLine("  (문 열림 " + (Time.unscaledTime - start).ToString("0.00") + "초)");
        MolakInputGuard.MuteFor(0.3f);
        if (inst.gateTurn) { inst.gateTurn = false; MolakPageTurn.Play(); MolakScroll.NewPage(); }
    }
    bool Trace0 () { return Trace != null; }

    GUIStyle sGate; float gateU = -1f;
    void OnGUI ()
    {
        if (gateStart < 0f || MolakMenu.IsOpen) return;
        float u = MolakGui.U;
        if (sGate == null || !Mathf.Approximately(gateU, u)) { gateU = u; sGate = MolakGui.Label(30, u, TextAnchor.LowerRight, false); }
        var ta = MolakVfx.TextArea();
        float a = 0.35f + 0.45f * (0.5f + 0.5f * Mathf.Sin((Time.unscaledTime - gateStart) * 4f));
        MolakGui.Ink(sGate, new Color(0.75f, 0.6f, 0.32f, a));
        GUI.Label(new Rect(ta.x, ta.yMax - 40f * u, ta.width - 10f * u, 36f * u), "▼", sGate);
    }

    static readonly System.Collections.Generic.HashSet<string> HubLabels = new System.Collections.Generic.HashSet<string> {
        "DayLoop", "Daytime", "Night", "NightChoice", "NightEnd", "Evening", "AfterChapter", "NightTown", "Town", "TownMenu", "Entry", "Rebirth", "Prologue"
    };
    static bool IsHubLabel (string l) { return HubLabels.Contains(l); }
    static bool Assigned<T> (T p) where T : MolakVn.ICommandParameter { return p != null && p.HasValue; }

    /// <summary>재생 목록에서 다음 '의미 있는' 명령이 본문 출력인지 (변수 설정·능력치·인물 삽화 명령은 건너뜀)</summary>
    static bool NextIsText (IScriptTrack track)
    {
        var pl = track.Playlist; if (pl == null) return false;
        int idx = track.PlayedIndex;
        for (int k = idx + 1; k < pl.Count; k++)
        {
            var c = pl[k];
            if (Skipped(c)) continue;   // 0.4.80 조건이 거짓이라 건너뛸 명령(@print … if:)은 다음 글로 치지 않는다
            if (c is PrintText) return true;
            if (c is AddChoice) return false;           // 0.4.8: 마지막 글을 다 읽고 한 번 더 눌러야 선택지가 뜬다(선택지가 글을 가리지 않게)
            if (c is IMolakMoment mm && mm.CoversText) return false;   // 0.4.90 인장·기연 판·먹 화면 앞에서는 글을 읽게 한다
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

    /// <summary>0.4.64 다음에 오는 것이 쪽을 넘기는가(빨리 — 한 쪽씩). 쪽을 넘기지 않는 @wait·같은 대본 안의 @goto 는 지나쳐 본다.</summary>
    static bool NextBreaksPage (IScriptTrack track)
    {
        var pl = track.Playlist; if (pl == null) return true;
        string curPath = track.PlayedScript != null ? track.PlayedScript.Path : "";
        for (int k = track.PlayedIndex + 1; k < pl.Count; k++)
        {
            var c = pl[k];
            if (c.ConditionalExpression != null && c.ConditionalExpression.HasValue)
            { bool ok = true; try { ok = MolakVn.ExpressionEvaluator.Evaluate<bool>(c.ConditionalExpression.Value); } catch { } if (!ok) continue; }
            if (c is PrintText) return false;
            if (c is AddChoice || c is Gosub || c is Return || c is Stop) return true;
            if (c is IMolakMoment mm && mm.CoversText) return true;    // 0.4.90 덮는 연출 앞은 쪽 끝(빨리여도 거기서 기다린다)
            if (c is Wait) continue;
            var n = c.GetType().Name;
            if (n == "SceneCommand" || n.Contains("Ending") || n == "DayTickCommand" || n == "InitRunCommand" || n == "PageCommand") return true;
            if (c is Goto)
            {
                var g = (Goto)c;
                string pv = Assigned(g.Path) ? (g.Path.Value ?? "") : "";
                int hi = pv.IndexOf('#');
                string gs = hi >= 0 ? pv.Substring(0, hi) : pv, gl = hi >= 0 ? pv.Substring(hi + 1) : null;
                if (!string.IsNullOrEmpty(gs) && gs != curPath) return true;
                if (!string.IsNullOrEmpty(gl) && IsHubLabel(gl)) return true;
                return false;   // 같은 대본 안에서 이어진다(그 뒤 글이 넘치면 그 글에서 다시 판단)
            }
            if (n == "ArtCommand")
            {
                var f = c.GetType().GetField("SceneId"); var v = f != null ? f.GetValue(c) as StringParameter : null;
                var off = c.GetType().GetField("Off"); var ov = off != null ? off.GetValue(c) as BooleanParameter : null;
                if ((v != null && v.HasValue) || (ov != null && ov.HasValue && ov.Value)) return true;
            }
        }
        return true;
    }

    static bool prevDlg, prevSp;   // prevSp: 0.4.88 대사 또는 독백
    /// <summary>0.4.83 @spk 로 적은 말하는 사람. Pending 은 다음 글에 붙을 것, Current 는 지금 찍힌(찍히는) 글의 것(없으면 null).</summary>
    public static string PendingSpeaker, CurrentSpeaker;
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

    /// <summary>0.4.88 독백 줄 — 꺾쇠 태그를 걷고 작은따옴표로 시작하면.</summary>
    static bool IsMonologue (PrintText pt)
    {
        try
        {
            if (pt.Text == null || !pt.Text.HasValue) return false;
            var loc = Engine.GetService<ITextLocalizer>();
            var t = loc != null ? loc.Resolve(pt.Text.Value) : pt.Text.Value.ToString();
            if (string.IsNullOrEmpty(t)) return false;
            t = System.Text.RegularExpressions.Regex.Replace(t, "<[^>]+>", "").TrimStart();
            return t.StartsWith("'") || t.StartsWith("‘");
        }
        catch { return false; }
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
        foreach (var panel in Object.FindObjectsByType<MolakVn.UI.RevealableTextPrinterPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var f = typeof(MolakVn.UI.RevealableTextPrinterPanel).GetField("autoLineBreaks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
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
        foreach (var panel in Object.FindObjectsByType<MolakVn.UI.RevealableTextPrinterPanel>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
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
            // 0.4.98 그러나 날 글({dejapre} 같은 한 단어)로 재면 실제로 찍힐 한 단락이 빠져 쪽이 넘치고 글이 위로 밀렸다 → 우리 변수로 직접 풀어 잰다(나니노벨은 거치지 않는다)
            if (pt.Text.DynamicValue) return OldFit ? pt.Text.ToString() ?? "" : ResolveVars(pt.Text.ToString() ?? "");
            var loc = Engine.GetService<ITextLocalizer>();
            return (loc != null ? loc.Resolve(pt.Text.Value) : pt.Text.Value.ToString()) ?? "";
        }
        catch { return ""; }
    }

    /// <summary>0.4.98 어림용 {변수} 풀기 — 문자열 변수는 값 그대로("none"·보이지 않는 한 글자는 빈 글), 숫자는 정수로. 모르는 이름은 그대로 둔다.</summary>
    static string ResolveVars (string raw)
    {
        if (raw.IndexOf('{') < 0) return raw;
        return System.Text.RegularExpressions.Regex.Replace(raw, @"\{(\w+)\}", m =>
        {
            string n = m.Groups[1].Value;
            if (!MolakStats.Has(n)) return m.Value;
            string sv = MolakStats.Str(n, null);
            if (sv != null) return sv == "none" || sv == "\u200B" ? "" : sv;
            return Mathf.RoundToInt(MolakStats.Get(n)).ToString();
        });
    }

    /// <summary>다음 '의미 있는' 본문 출력 명령(NextIsText 와 같은 규칙). 없으면 null.</summary>
    static PrintText NextText (IScriptTrack track)
    {
        if (!NextIsText(track)) return null;
        var pl = track.Playlist;
        for (int k = track.PlayedIndex + 1; k < pl.Count; k++) if (pl[k] is PrintText && !Skipped(pl[k])) return (PrintText)pl[k];
        return null;
    }

    /// <summary>0.4.80 조건(if:)이 지금 거짓이라 나니노벨이 건너뛸 명령인가</summary>
    static bool Skipped (Command c)
    {
        if (c == null) return false;
        bool hasCond = (c.ConditionalExpression != null && c.ConditionalExpression.HasValue) || (c.InvertedConditionalExpression != null && c.InvertedConditionalExpression.HasValue);
        if (!hasCond) return false;
        try { return !c.ShouldExecute; } catch { return false; }
    }

    static TMPro.TMP_Text DialogueText ()
    {
        foreach (var panel in Object.FindObjectsByType<MolakVn.UI.RevealableTextPrinterPanel>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
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
        // 0.4.98 비무 띠가 올라와 글 칸이 줄어 있으면(626 → 416) 줄어든 칸으로 잰다 — 비무 중 꽉 찬 쪽이 33px 넘쳤다
        float vpNow = MolakScroll.ViewportHeight();
        if (!OldFit && vpNow > 120f && vpNow < vpH) vpH = vpNow;
        string cur = existing ?? tx.text ?? "";
        if (cur.Trim().Length == 0) return false;
        float w = tx.rectTransform.rect.width;
        if (w <= 1f) return false;
        var m = Measurer(tx);
        if (m == null) return false;
        float h = m.GetPreferredValues(cur + "\n\n" + addition, w, 0f).y;
        if (OldFit) return h > vpH * 0.9f;
        // 0.4.98 본문 칸 둘레의 여백(글 칸 위 15·아래 60, 인쇄기 위 10 — 레이아웃 그룹에서 바로 읽는다)을 빼고 잰다.
        //   옛 어림(칸의 90%)은 이 여백 85px 와 아래 MolakMeasure 가 레이아웃에 끼어 먹던 한 줄(61px)을 다 못 덮어 꽉 찬 쪽이 6~16px 넘쳤고, 글이 그만큼 위로 밀렸다.
        var panel = tx.transform.parent;
        float room = vpH - PadV(panel) - PadV(panel != null ? panel.parent : null) - FitSlack;
        return h > room;
    }

    /// <summary>0.4.98 쪽 끝 여유(어림과 실제 배치의 반올림 차) — 한 줄(56px)보다 훨씬 작게.</summary>
    const float FitSlack = 12f;
    static float PadV (Transform t) { var lg = t != null ? t.GetComponent<UnityEngine.UI.LayoutGroup>() : null; return lg != null ? lg.padding.top + lg.padding.bottom : 0f; }

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
            go.AddComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;   // 0.4.98 글 칸의 세로 레이아웃에 끼면 빈 한 줄 높이(61px)를 차지해 쪽이 넘쳤다
            measurer.color = new Color(0f, 0f, 0f, 0f);
            measurer.text = "";
        }
        else if (measurer.transform.parent != src.transform.parent) measurer.transform.SetParent(src.transform.parent, false);
        { var le = measurer.GetComponent<UnityEngine.UI.LayoutElement>(); if (le == null) le = measurer.gameObject.AddComponent<UnityEngine.UI.LayoutElement>(); le.ignoreLayout = !OldFit; }
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
        foreach (var panel in Object.FindObjectsByType<MolakVn.UI.RevealableTextPrinterPanel>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
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
