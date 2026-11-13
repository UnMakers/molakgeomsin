using System.Collections.Generic;
using Naninovel;
using UnityEngine;

/// <summary>
/// 0.4.55 길잡이 — 게임이 제 셈을 말해 주지 않던 것을 처음 그 일이 생길 때 한 장씩 알려 준다.
///  · 처음 그 상태가 되면(저주가 스물다섯을 넘거나, 처음 다치거나, 굶거나, 서열에 오르거나…) 글을 기다리는 틈에 한 장이 뜬다.
///  · 한 번 본 장은 이 기기에서 다시 뜨지 않는다. 메뉴(☰) → 「길잡이」에서 언제든 다시 읽는다.
///  · 빨리 넘기기·비무 중·메뉴가 열린 동안에는 띄우지 않는다(다음 틈에 뜬다).
/// </summary>
public class MolakGuide : MonoBehaviour
{
    public struct Card
    {
        public string Id, Title, Body;
        public Card (string id, string t, string b) { Id = id; Title = t; Body = b; }
    }

    public static readonly Card[] Cards = {
        new Card("body", "몸의 셈",
            "<b>체력</b> — 바닥나면 죽는다. 비무에서 지거나 굶거나 독이 돌면 깎인다. 쉬거나 약을 쓰면 돌아온다. 검술·내공이 오르면 상한도 는다.\n" +
            "<b>검술</b> — 칼 다루는 솜씨. 비무의 승산 대부분이 여기서 나온다. 수련하고 이길수록 오른다. 높을수록 상대의 기세도 잘 읽힌다.\n" +
            "<b>내공</b> — 단전의 진기. 승산에 검술보다 더 크게 보탠다. 쌓기는 더디다.\n" +
            "<b>살기</b> — 사람을 벤 기세. 셋이 넘으면 비무에서 '살기를 실어 선공'할 수 있다. 사람들도 그 눈빛을 알아본다.\n" +
            "<b>은자</b> — 방값·약값·판돈. 방값을 못 내면 굶고, 굶으면 체력이 깎인다.\n" +
            "<b>저주</b> — 화면 맨 위의 보랏빛 줄. 가슴의 문양이 날마다 자란다. 백이 되면 끝이다."),
        new Card("curse", "저주 — 가슴의 문양",
            "문양은 날마다 자란다. 서른다섯째 날부터 더 빨리, 쉰째 날부터 한 번 더 빨리 자란다.\n" +
            "비무에서 사술을 끌어내면 이기기 쉬워지는 대신 저주가 크게 오른다.\n" +
            "<b>예순</b>을 넘기면 사람들이 문양을 알아보기 시작한다. <b>일흔다섯</b>을 넘기면 사술이 제멋대로 나선다. <b>백</b>이면 사술에 먹혀 죽는다.\n" +
            "안식향을 피우거나, 청심차를 달이거나, 봉인을 받으면 덜 오른다. 문양이 터져 죽어 본 몸도 다음 생엔 덜 끓는다."),
        new Card("hurt", "부상 · 독 · 곪은 상처",
            "<b>부상</b> — 비무에서 지면 사흘 동안 몸이 무겁다. 그동안 비무의 승산이 크게 떨어진다.\n" +
            "<b>독</b>과 <b>곪은 상처</b> — 날마다 체력을 깎고, 손쓰지 않으면 점점 깊어진다. 백초당 할멈이 다스린다. 금창약이 있으면 한 번 막는다.\n" +
            "다친 채로 또 지면 상처가 덧난다. 쉬는 것도 수련이다."),
        new Card("hunger", "방값과 굶주림",
            "운락객잔의 방값은 하루 한 냥, 스물다섯째 날부터 두 냥이다. 날이 넘어갈 때 저절로 빠진다.\n" +
            "은자가 모자라면 그날은 굶는다. 굶으면 체력이 깎인다.\n" +
            "은자는 비무판, 품팔이, 밤 저자의 판에서 번다. 하진무와 사이가 좋으면 방값을 외상으로 달아 주기도 한다."),
        new Card("rank", "강호 서열",
            "비무에서 나보다 높은 이름을 꺾으면 그 자리로 오른다. 낮은 이름을 이겨서는 오르지 않는다.\n" +
            "서열에 따라 경지(삼류 · 이류 · 일류 · 절정 · 초절정 · 화경 …)가 나뉘고, 경지가 오르면 만나는 사람과 열리는 길이 달라진다.\n" +
            "이름이 높아지면 교의 장부에도 오른다. 자객이 찾아오는 것도 그때부터다."),
        new Card("debt", "빚",
            "고리대 왕삼의 돈은 기한이 짧고 이자가 붙는다. 기한 안에 갚지 못하면 수하들이 찾아온다.\n" +
            "남은 기한은 화면 맨 윗줄, 저주 표시 왼편에 뜬다."),
        new Card("death", "죽음과 회귀",
            "죽으면 그날 아침으로 되감긴다. 은자·서열·인연은 사라지지만 몸에 남는 것이 있다.\n" +
            "<b>손이 기억한 검</b> — 가장 높이 올랐던 검술·내공의 4분의 1을 들고 깨어난다.\n" +
            "<b>겪어 본 죽음</b> — 처음 겪는 죽음마다 체력 상한이 하나 는다.\n" +
            "<b>이번 생의 뜻</b> — 아침에 고른 뜻을 이루면 그 뜻이 몸에 남는다.\n" +
            "<b>청운검결</b> — 특정한 죽음을 겪으면 사형들에게 배운 초식이 돌아온다. 비무의 합에서 쓴다.\n" +
            "<b>앎</b> — 본 것·들은 것. 다음 생 같은 자리에 서면 보랏빛 선택지로 떠오른다."),
        new Card("duel", "비무",
            "마지막 한 칼 앞에 몇 번 칼을 섞는다(합). 상대의 기세를 읽고 맞수를 고르면 기세가 쌓이고, 쌓인 기세가 마지막 한 칼의 승산을 올린다.\n" +
            "攻(찌르러 온다)엔 받아치기, 守(굳게 받는다)엔 칼끝 흔들기, 虛(허초)엔 정면으로 베기.\n" +
            "이름난 칼잡이는 한 번씩 필살 초식을 낸다. 처음엔 읽히지 않는다. 받아 보면 다음 생부터 보인다.\n" +
            "보통 비무판은 져도 목숨은 붙어 있다. 생사결·원수·이야기의 결전만 목숨을 건다."),
        new Card("bag", "보따리",
            "화면 아래의 띠를 위로 올리면 보따리가 열린다. 지닌 물건, 맺은 인연, 알게 된 것이 담긴다.\n" +
            "어떤 물건은 비무의 마지막 한 칼 앞에 쥐면 기세를 하나 보탠다(물건마다 한 생에 한 번)."),
    };

    static MolakGuide inst;
    const string Pref = "molak.guide.";
    int showing = -1; float showT; bool listOpen; Vector2 scroll; int reading = -1;
    float checkT;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot ()
    {
        if (inst) return;
        var go = new GameObject("MolakGuide"); DontDestroyOnLoad(go);
        inst = go.AddComponent<MolakGuide>();
    }

    public static bool IsOpen => inst != null && (inst.showing >= 0 || inst.listOpen);
    /// <summary>한 장을 읽기로 연다(몸 상태 줄을 눌렀을 때).</summary>
    public static void Read (string id)
    {
        if (inst == null) return;
        int i = Index(id); if (i < 0) return;
        MarkSeen(id); inst.listOpen = true; inst.reading = i; inst.showT = 0f; inst.fromHud = true;
    }
    bool fromHud;
    public static void OpenList () { if (inst != null) { inst.fromHud = false; inst.listOpen = true; inst.reading = -1; inst.scroll = Vector2.zero; } }

    static bool Seen (string id) { try { return PlayerPrefs.GetInt(Pref + id, 0) > 0; } catch { return true; } }
    static void MarkSeen (string id) { try { PlayerPrefs.SetInt(Pref + id, 1); PlayerPrefs.Save(); } catch { } }
    static int Index (string id) { for (int i = 0; i < Cards.Length; i++) if (Cards[i].Id == id) return i; return -1; }

    /// <summary>처음이면 띄운다(대본에서 @guide id:… 로도 부른다).</summary>
    public static void Trigger (string id)
    {
        if (inst == null || inst.showing >= 0 || Seen(id)) return;
        int i = Index(id); if (i < 0) return;
        inst.showing = i; inst.showT = 0f;
    }

    static float G (string k) { try { return MolakStats.Get(k); } catch { return 0f; } }

    void Update ()
    {
        if (!Engine.Initialized || showing >= 0 || listOpen) return;
        checkT += Time.unscaledDeltaTime; if (checkT < 0.5f) return; checkT = 0f;
        if (MolakPractice.Active || MolakMenu.IsOpen || MolakBag.IsOpen || MolakArt.InDuel || MolakDuelStage.GuideOpen || MolakBlackout.Auto) return;
        var sp = Engine.GetService<IScriptPlayer>();
        if (sp == null || sp.Skipping || !sp.Playing || sp.MainTrack == null || !sp.MainTrack.AwaitingInput) return;
        float day = G("day");
        if (day < 1f) return;
        if (!Seen("body")) { Trigger("body"); return; }
        if (G("curse") >= 25f && !Seen("curse")) { Trigger("curse"); return; }
        if ((G("injury") > 0f || G("taint") > 0f) && !Seen("hurt")) { Trigger("hurt"); return; }
        if (G("starved") > 0f && !Seen("hunger")) { Trigger("hunger"); return; }
        if (G("rank") > 0f && !Seen("rank")) { Trigger("rank"); return; }
        if (G("debt") > 0f && !Seen("debt")) { Trigger("debt"); return; }
        if (G("g_runs") >= 1f && !Seen("death")) { Trigger("death"); return; }
        if (day >= 3f && !Seen("bag")) { Trigger("bag"); return; }
    }

    GUIStyle sHead, sBody, sBtn, sItem; float builtU = -1f;
    void Styles (float u)
    {
        if (sHead != null && Mathf.Approximately(builtU, u)) return;
        builtU = u;
        sHead = MolakGui.Label(46, u, TextAnchor.MiddleCenter, false); sHead.font = MolakTypography.Heading(sHead.font); sHead.richText = true;
        sBody = MolakGui.Label(30, u, TextAnchor.UpperLeft, true); sBody.richText = true;
        sBtn = MolakGui.Label(32, u, TextAnchor.MiddleCenter, false);
        sItem = MolakGui.Label(32, u, TextAnchor.MiddleLeft, false); sItem.richText = true;
    }

    void OnGUI ()
    {
        if (!Engine.Initialized) return;
        if (showing < 0 && !listOpen) return;
        float u = MolakGui.U; Styles(u);
        GUI.depth = MolakGui.DepthBlackout + 9;
        float w = Screen.width, h = Screen.height;
        MolakInputGuard.Guard(new Rect(0, 0, w, h));
        var g0 = GUI.color;
        var ink = new Color(0.93f, 0.89f, 0.81f, 1f); var gold = new Color(0.85f, 0.68f, 0.3f, 1f);
        GUI.color = new Color(0.04f, 0.035f, 0.03f, 0.95f); GUI.DrawTexture(new Rect(0, 0, w, h), MolakGui.Px); GUI.color = Color.white;
        float cw = Mathf.Min(w * 0.88f, 900f * u), cx = (w - cw) / 2f;

        if (showing >= 0 || reading >= 0)
        {
            int idx = showing >= 0 ? showing : reading;
            showT += Time.unscaledDeltaTime;
            var c = Cards[idx];
            float y = Mathf.Max(70f * u, h * 0.1f);
            MolakGui.Ink(sHead, gold); GUI.Label(new Rect(cx, y, cw, 70f * u), (showing >= 0 ? "<size=" + Mathf.RoundToInt(24 * u) + ">길잡이</size>\n" : "") + c.Title, sHead);
            y += 110f * u;
            MolakGui.Ink(sBody, ink);
            float bh = sBody.CalcHeight(new GUIContent(c.Body), cw);
            GUI.Label(new Rect(cx, y, cw, bh), c.Body, sBody);
            y += bh + 40f * u;
            var br = new Rect(cx + cw * 0.25f, Mathf.Min(y, h - 150f * u), cw * 0.5f, 96f * u);
            GUI.color = new Color(0.45f, 0.13f, 0.1f, 1f); GUI.DrawTexture(br, MolakGui.Px); GUI.color = Color.white;
            MolakGui.Ink(sBtn, ink); GUI.Label(br, showing >= 0 ? "알겠다" : "돌아간다", sBtn);
            bool close = GUI.Button(br, GUIContent.none, GUIStyle.none);
            GUI.Button(new Rect(0, 0, w, h), GUIContent.none, GUIStyle.none);
            if (close && showT > 0.35f)
            {
                if (showing >= 0) { MarkSeen(c.Id); showing = -1; MolakInputGuard.MuteFor(0.25f); }
                else { reading = -1; if (fromHud) { listOpen = false; fromHud = false; MolakInputGuard.MuteFor(0.25f); } }
                MolakSfx.Confirm();
            }
            GUI.color = g0;
            return;
        }

        // 목록
        {
            float y = Mathf.Max(60f * u, h * 0.07f);
            MolakGui.Ink(sHead, gold); GUI.Label(new Rect(cx, y, cw, 70f * u), "길잡이", sHead);
            y += 100f * u;
            float rowH = 104f * u, gap = 14f * u;
            for (int i = 0; i < Cards.Length; i++)
            {
                var r = new Rect(cx, y + i * (rowH + gap), cw, rowH);
                if (r.yMax > h - 140f * u) break;
                GUI.color = new Color(1f, 1f, 1f, 0.08f); GUI.DrawTexture(r, MolakGui.Px); GUI.color = Color.white;
                MolakGui.Ink(sItem, Seen(Cards[i].Id) ? ink : new Color(0.7f, 0.66f, 0.6f, 1f));
                GUI.Label(new Rect(r.x + 28f * u, r.y, r.width - 40f * u, r.height), Cards[i].Title, sItem);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { reading = i; showT = 0f; MolakSfx.Confirm(); }
            }
            var br = new Rect(cx + cw * 0.25f, h - 130f * u, cw * 0.5f, 96f * u);
            GUI.color = new Color(0.16f, 0.13f, 0.11f, 1f); GUI.DrawTexture(br, MolakGui.Px); GUI.color = Color.white;
            MolakGui.Ink(sBtn, ink); GUI.Label(br, "닫는다", sBtn);
            if (GUI.Button(br, GUIContent.none, GUIStyle.none)) { listOpen = false; MolakInputGuard.MuteFor(0.25f); }
            GUI.Button(new Rect(0, 0, w, h), GUIContent.none, GUIStyle.none);
            GUI.color = g0;
        }
    }
}

/// <summary>@guide id:duel — 처음이면 길잡이 한 장을 띄운다.</summary>
[Alias("guide")]
public class GuideCommand : Command
{
    [RequiredParameter] public StringParameter Id;
#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        MolakGuide.Trigger(Id.Value);
    }
}
