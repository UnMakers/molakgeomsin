using System.Collections.Generic;
using Naninovel;
using UnityEngine;

/// <summary>
/// 0.4.58 밤의 갈고리 — 하루를 닫을 때 '내일'을 한 줄 귀띔한다(@teaser → nightteaser).
///  · 다음 날 아침에 터질 마디(이야기 비트·빚·원한·혈랑대·교의 수레…)가 있으면 그것을, 같은 귀띔은 한 생에 한 번만.
///  · 터질 것이 없으면 절반쯤은 운락의 밤 풍경 한 줄, 나머지는 아무 말 없이 넘어간다.
///  · 회귀를 겪은 몸은 더 많이 알아챈다: 혈랑대가 내려오는 날을 센다.
/// </summary>
public static class MolakTeaser
{
    struct T { public string Id; public System.Func<float, bool> When; public System.Func<float, string> Text; }
    static float G (string k) { return MolakStats.Get(k); }

    static readonly T[] Table = {
        new T { Id = "capture", When = d => d >= 57 && d < 60 && G("b_capture") < 1, Text = d => "북쪽 관도로 교의 수레가 내려온다는 말이 돈다. 운락까지 " + Num(60 - d) + "." },
        new T { Id = "debt",    When = d => G("debt") > 0 && d >= G("debtday"), Text = d => "징세조가 내일 아침 장부를 들고 온다. 장부의 빚은 " + G("debt").ToString("0") + "냥." },
        new T { Id = "grudge",  When = d => G("grudge") > 0 && d >= G("grudgeday"), Text = d => "누가 객잔 앞을 두 번 지나갔다. 걸음이 낯익다. 기다리는 걸음이다." },
        new T { Id = "hunt",    When = d => d >= 4 && G("b_hunt") < 1, Text = d => G("g_hunt_won") > 0 || G("g_know_scout") > 0
                                        ? "아래층에서 말발굽이 멎는다. 북쪽 말투 둘. <color=#C9A227>그 밤이다.</color>"
                                        : "아래층에서 말발굽이 멎는다. 북쪽 말투 둘이 빈방이 몇이냐고 묻는다." },
        new T { Id = "raid",    When = d => G("raidday") > 0 && G("raidday") - d >= 0 && G("raidday") - d <= 3, Text = RaidLine },
        new T { Id = "soa",     When = d => d >= 3 && G("f_soamet") < 1 && G("b_soa") < 1, Text = d => "저자 쪽에서 쇳소리가 짧게 두 번 났다. 한 호흡에 두 번." },
        new T { Id = "hajin",   When = d => d >= 5 && G("b_hajin") < 1, Text = d => "하진무가 장부를 덮었다가 다시 편다. 내일은 무슨 말을 꺼낼 얼굴이다." },
        new T { Id = "plate",   When = d => d >= 7 && G("b_plate") < 1, Text = d => "서열각 앞에 새 쇠패를 다는 망치 소리가 늦게까지 난다." },
        new T { Id = "wanted",  When = d => d >= 8 && G("b_wanted") < 1, Text = d => "관아 담벼락 밑에 풀 솥이 놓였다. 아침이면 무언가 붙는다." },
        new T { Id = "jang",    When = d => d >= 11 && G("b_jang") < 1, Text = d => "뒷문 쪽에서 누가 오래 서 있다 간다. 신발 끄는 소리가 가볍다." },
        new T { Id = "unju",    When = d => d >= 15 && G("f_unjudone") < 1, Text = d => "운주 들판 쪽으로 칼 찬 사람들이 밤새 지나간다." },
        new T { Id = "fame",    When = d => d >= 20 && G("rank") > 0 && G("rank") <= 500000 && G("b_fame") < 1, Text = d => "아래층 술꾼들이 서열각 이야기를 하다 당신을 보고 입을 다문다." },
        new T { Id = "shr",     When = d => d >= 22 && G("f_shrseen") < 1, Text = d => "사당 쪽 숲에서 향 냄새가 바람을 거슬러 넘어온다." },
        new T { Id = "hm",      When = d => d >= 26 && G("b_hm") < 1, Text = d => "회목촌에서 오던 장작 수레가 오늘은 한 대도 오지 않았다." },
        new T { Id = "amh",     When = d => d >= 30 && G("b_amhyang") < 1, Text = d => "암향루 이층의 등이 오늘 밤 유난히 오래 켜져 있다." },
        new T { Id = "old",     When = d => d >= 36 && G("b_old") < 1, Text = d => "잠결에 누가 당신 얼굴로 웃는다. 주름이 더 깊은 얼굴이다." },
        new T { Id = "ripe",    When = d => G("curse") >= 80 && G("b_ripe") < 1, Text = d => "문양이 목덜미를 타고 오른다. 손끝이 제 것 같지 않다." },
    };

    static readonly string[] Ambient = {
        "강 쪽에서 안개가 올라온다. 내일 아침 저잣거리는 늦게 열린다.",
        "객잔 지붕 위를 누가 가볍게 건너간다. 고양이보다는 무겁다.",
        "아래층에서 하진무가 주판을 튕기는 소리가 늦게까지 난다.",
        "멀리 관아 쪽에서 북이 두 번 울린다. 통금 북보다 이르다.",
        "창틈으로 바람이 세다. 내일은 비가 올지도 모른다.",
        "백초당 굴뚝에서 늦도록 약 달이는 연기가 오른다.",
        "가슴의 문양이 잠깐 따끔하다. 잠들기 전까지 그 자리를 손으로 누르고 있었다.",
        "비무판 쪽에서 누가 늦게까지 목검을 휘두른다. 바람 가르는 소리가 고르지 않다.",
    };

    static string Num (float n) { int k = Mathf.Max(0, Mathf.RoundToInt(n)); string[] w = { "오늘", "하루", "이틀", "사흘", "나흘", "닷새" }; return k < w.Length ? w[k] : k + "일"; }

    static string RaidLine (float d)
    {
        int left = Mathf.RoundToInt(G("raidday") - d);
        bool knows = G("g_runs") >= 1f;   // 한 번이라도 죽어 본 몸은 날을 센다
        if (!knows) return "북쪽 산마루에 횃불이 셋. 사냥꾼은 저렇게 많이 다니지 않는다.";
        string when = left <= 0 ? "오늘 밤이다" : left == 1 ? "내일 밤이다" : Num(left) + " 남았다";
        string shift = G("raidshift") != 0f ? " <color=#8A7A66>(지난 생과 날이 다르다)</color>" : "";
        return "북쪽 산마루에 횃불이 셋. 혈랑대가 내려오기까지 — <color=#C03434>" + when + "</color>." + shift;
    }

    public static string Pick ()
    {
        float d = G("day");
        var seen = new HashSet<string>((MolakStats.Str("tzseen") ?? "").Split('|'));
        foreach (var t in Table)
        {
            bool on; try { on = t.When(d); } catch { on = false; }
            if (!on) continue;
            string key = t.Id == "raid" ? "raid" + Mathf.RoundToInt(G("raidday") - d) : t.Id;   // 혈랑대는 날마다 센다
            if (seen.Contains(key)) continue;
            seen.Add(key);
            MolakStats.SetStr("tzseen", string.Join("|", new List<string>(seen).ToArray()));
            return "<color=#8A7A66>잠들기 전 —</color> " + t.Text(d);
        }
        if (Random.value < 0.45f) return "<color=#8A7A66>잠들기 전 —</color> " + Ambient[Random.Range(0, Ambient.Length)];
        return "none";
    }
}

/// <summary>@teaser — 내일을 한 줄(nightteaser, 없으면 "none").</summary>
[Alias("teaser")]
public class TeaserCommand : Command
{
#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        string s = "none";
        try { s = MolakTeaser.Pick(); } catch (System.Exception e) { Debug.LogWarning("[teaser] " + e.Message); }
        MolakStats.SetStr("nightteaser", s);
    }
}

/// <summary>
/// 0.4.58 판이 바뀌는 날 — 열흘 남짓마다 세상의 셈이 달라진다(문양이 자라는 빠르기, 방값…). 그날 밤 한 장으로 알린다.
/// @turn title:"열흘째 밤" lines:"a|b" — 화면을 누르면 닫힌다.
/// </summary>
public class MolakTurnCard : MonoBehaviour
{
    static MolakTurnCard inst;
    string title = "", kicker = ""; string[] lines = new string[0];
    float t = -1f;
    public static bool Showing => inst != null && inst.t >= 0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot ()
    {
        if (inst) return;
        var go = new GameObject("MolakTurnCard"); DontDestroyOnLoad(go);
        inst = go.AddComponent<MolakTurnCard>();
    }

    public static void Show (string kick, string ttl, string[] ls)
    {
        if (inst == null) return;
        inst.kicker = kick ?? ""; inst.title = ttl ?? ""; inst.lines = ls ?? new string[0]; inst.t = 0f;
        MolakSfx.Stamp(); MolakVfx.Shake(10f);
    }

    void Update () { if (t >= 0f) t += Time.unscaledDeltaTime; }

    GUIStyle sKick, sTitle, sLine, sHint; float builtU = -1f;
    void OnGUI ()
    {
        if (t < 0f) return;
        float u = MolakGui.U, w = Screen.width, h = Screen.height;
        if (sTitle == null || !Mathf.Approximately(builtU, u))
        {
            builtU = u;
            sKick = MolakGui.Label(26, u, TextAnchor.MiddleCenter, false);
            sTitle = MolakGui.Label(64, u, TextAnchor.MiddleCenter, true); sTitle.font = MolakTypography.Heading(sTitle.font); sTitle.richText = true;
            sLine = MolakGui.Label(30, u, TextAnchor.MiddleCenter, true); sLine.richText = true;
            sHint = MolakGui.Label(22, u, TextAnchor.MiddleCenter, false);
        }
        GUI.depth = MolakGui.DepthBlackout + 8;
        MolakInputGuard.Guard(new Rect(0, 0, w, h));
        var g0 = GUI.color;
        float a = Mathf.Clamp01(t / 0.35f);
        GUI.color = new Color(0.02f, 0.016f, 0.014f, 0.86f * a); GUI.DrawTexture(new Rect(0, 0, w, h), MolakGui.Px);
        // 가운데 먹띠
        float cw = Mathf.Min(w * 0.9f, 940f * u), cx = (w - cw) / 2f;
        float lh = 0f; foreach (var l in lines) lh += sLine.CalcHeight(new GUIContent("· " + l), cw - 80f * u) + 12f * u;
        float bandH = 230f * u + lh, by = (h - bandH) / 2f;
        GUI.color = new Color(0.07f, 0.055f, 0.045f, 0.97f * a); GUI.DrawTexture(new Rect(0, by, w, bandH), MolakGui.Px);
        GUI.color = new Color(0.62f, 0.08f, 0.05f, 0.9f * a);
        float lineW = Mathf.Lerp(0f, cw, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.15f) / 0.5f)));
        GUI.DrawTexture(new Rect((w - lineW) / 2f, by + 4f * u, lineW, 3f * u), MolakGui.Px);
        GUI.DrawTexture(new Rect((w - lineW) / 2f, by + bandH - 7f * u, lineW, 3f * u), MolakGui.Px);
        GUI.color = Color.white;
        MolakGui.Ink(sKick, new Color(0.85f, 0.68f, 0.3f, a));
        GUI.Label(new Rect(cx, by + 26f * u, cw, 36f * u), kicker, sKick);
        float ta = Mathf.Clamp01((t - 0.2f) / 0.4f);
        var jit = t < 0.6f ? Random.insideUnitCircle * 3f * u : Vector2.zero;
        MolakGui.Ink(sTitle, new Color(0.55f, 0.05f, 0.03f, 0.6f * ta));
        GUI.Label(new Rect(cx + 4f * u + jit.x, by + 70f * u + 3f * u + jit.y, cw, 90f * u), title, sTitle);
        MolakGui.Ink(sTitle, new Color(0.95f, 0.9f, 0.82f, ta));
        GUI.Label(new Rect(cx + jit.x, by + 70f * u + jit.y, cw, 90f * u), title, sTitle);
        float y = by + 180f * u;
        for (int i = 0; i < lines.Length; i++)
        {
            float la = Mathf.Clamp01((t - 0.55f - 0.25f * i) / 0.35f);
            float hh = sLine.CalcHeight(new GUIContent("· " + lines[i]), cw - 80f * u);
            MolakGui.Ink(sLine, new Color(0.88f, 0.83f, 0.74f, la));
            GUI.Label(new Rect(cx + 40f * u, y, cw - 80f * u, hh), "· " + lines[i], sLine);
            y += hh + 12f * u;
        }
        if (t > 1.2f)
        {
            MolakGui.Ink(sHint, new Color(0.6f, 0.56f, 0.5f, Mathf.Clamp01((t - 1.2f) / 0.4f)));
            GUI.Label(new Rect(0, by + bandH + 18f * u, w, 34f * u), "눌러서 넘긴다", sHint);
        }
        GUI.color = g0;
        if (GUI.Button(new Rect(0, 0, w, h), GUIContent.none, GUIStyle.none) && t > 0.9f) { t = -1f; MolakInputGuard.MuteFor(0.25f); }
    }
}

/// <summary>@turn kicker:"판이 바뀌었다" title:"열흘째 밤" lines:"a|b" — 닫힐 때까지 기다린다. '빨리' 중에는 띄우지 않는다.</summary>
[Alias("turn")]
public class TurnCommand : Command
{
    public StringParameter Kicker;
    [RequiredParameter] public StringParameter Title;
    public StringParameter Lines;

    public override async Awaitable Execute (ExecutionContext ctx)
    {
        var sp = Engine.GetService<IScriptPlayer>();
        if (sp != null && sp.Skipping) return;
        MolakTurnCard.Show(Assigned(Kicker) ? Kicker.Value : "판이 바뀌었다", Title.Value, Assigned(Lines) ? Lines.Value.Split('|') : new string[0]);
        while (MolakTurnCard.Showing) await Awaitable.NextFrameAsync();
    }
}
