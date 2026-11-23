using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MolakVn;
using UnityEngine;

/// <summary>
/// 0.4.94 인연별 이야기 모드 — 소설처럼 읽기(언메이커: 선택하는 게 중요한 게 아니라 소설 보듯이 한 번 보려는 것).
///  · 이야기 모드를 「소아 편·암향 편·청묘 편·참새 편」으로 시작하면(g_storyroute 1~4) 선택지를 직접 고르지 않는다.
///  · 고르는 기준은 기준표(Resources/MolakData/storypicks, tools/story_picks.py 가 대본을 따라가 매긴 점수)다.
///    그 편 히로인의 정, 진엔딩에 쓰이는 사실·앎, 사람이 죽는지, 사술을 본다. 이미 가진 앎(g_)을 또 얻는 몫은 뺀다.
///  · 선택지 단추는 보이지 않는다. 고른 말은 본문 끝에 한 줄로 남는다 — 대사면 주인공의 말(오른쪽), 행동이면 흐린 서술.
///    그 줄을 읽고 한 번 누르면 이야기가 이어진다(자동 읽기를 켜 두면 잠시 뒤 저절로).
///  · 비무는 MolakDuel#StoryCut 이 한 장면으로 줄이고, 승부수(@risk)는 통과한다(MolakRisk).
/// </summary>
public class MolakStoryAuto : MonoBehaviour
{
    struct Row { public int Route; public string Script; public float Score; public Regex Re; public string Text; public (string flag, float w)[] Gains; }
    static List<Row> rows;
    static MolakStoryAuto inst;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot ()
    {
        var go = new GameObject("MolakStoryAuto"); DontDestroyOnLoad(go); inst = go.AddComponent<MolakStoryAuto>();
    }

    static void Load ()
    {
        rows = new List<Row>();
        var ta = Resources.Load<TextAsset>("MolakData/storypicks");
        if (ta == null) return;
        foreach (var raw in ta.text.Split('\n'))
        {
            var l = raw.TrimEnd('\r');
            if (l.Length == 0 || l[0] == '#') continue;
            var c = l.Split('\t'); if (c.Length < 4) continue;
            int r; float sc;
            if (!int.TryParse(c[0], out r) || !float.TryParse(c[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out sc)) continue;
            var gains = new List<(string, float)>();
            if (c.Length >= 5 && c[4].Length > 0)
                foreach (var g in c[4].Split(','))
                { var kv = g.Split(':'); float w; if (kv.Length == 2 && float.TryParse(kv[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out w)) gains.Add((kv[0], w)); }
            rows.Add(new Row { Route = r, Script = c[1], Score = sc, Text = c[3], Re = Pattern(c[3]), Gains = gains.ToArray() });
        }
    }

    /// <summary>대본의 글귀 → 화면 글귀와 맞출 틀. {변수}는 아무 글자, 꺾쇠 태그·승부수 표시는 걷는다.</summary>
    static Regex Pattern (string t)
    {
        t = Regex.Replace(t, "<[^>]+>", "");
        t = MolakChoiceMeta.StripRisk(t);   // 승부수 표시 〔검술+2〕 는 화면 글귀에서 빠져 주석으로 간다(MolakChoiceMeta.StripRisk) — 대본 글귀에서도 뺀다
        var parts = Regex.Split(t, @"\{[^}]*\}");
        return new Regex("^" + string.Join(".*?", parts.Select(p => Regex.Escape(p.Trim())).ToArray()), RegexOptions.CultureInvariant);
    }

    IScriptPlayer player;
    float seenAt = -1f, echoAt = -1f;
    MolakVn.UI.ChoiceHandlerButton picked;
    readonly List<CanvasGroup> hidden = new List<CanvasGroup>();
    public static string LastPick = "";
    /// <summary>편마다 맺는 끝(MolakEnd 의 id): 소아 합벽 · 암향 면향 · 청묘 운해 · 참새 명가.</summary>
    public static readonly string[] RouteEnd = { "", "hapbyeok", "myeonhyang", "unhae", "myeongga" };
    bool closing;
    /// <summary>같은 날 같은 글귀를 다시 고를 때마다 빼는 값.</summary>
    public const float RepeatCost = 12f;
    readonly Dictionary<string, int> picks = new Dictionary<string, int>();
    float pickDay = -1f;

    void Update ()
    {
        if (!Engine.Initialized) return;
        if (player == null) player = Engine.GetService<IScriptPlayer>();
        if (!MolakStory.Auto || MolakPractice.Active) { Unhide(); picked = null; seenAt = -1f; rows = null; return; }   // 다음에 켜질 때 기준표를 다시 읽는다(편집기에서 재생을 다시 해도 정적 값이 남는다)
        var bs = Buttons();
        if (bs.Length == 0) { picked = null; seenAt = -1f; echoAt = -1f; return; }
        Hide(bs);
        if (picked != null)
        {
            if (MolakPageBack.Viewing) { echoAt = Time.unscaledTime; return; }   // 0.4.95 지난 쪽을 보는 동안은 이어 가지 않는다
            // 고른 말을 읽고 누르면(또는 자동 읽기·넘기기면 잠시 뒤) 이어 간다
            bool skipping = MolakPlayMode.SkipAll || (player != null && player.Skipping);
            bool go = skipping || MolakGui.TapDown() && Time.unscaledTime - echoAt > 0.25f || MolakPlayMode.Auto && Time.unscaledTime - echoAt > 1.6f;
            if (!go) return;
            var ub = picked.GetComponent<UnityEngine.UI.Button>();
            picked = null; seenAt = -1f;
            if (ub != null) ub.onClick.Invoke();
            return;
        }
        if (player != null && player.MainTrack != null && player.MainTrack.Playing) { seenAt = -1f; return; }
        if (seenAt < 0f) { seenAt = Time.unscaledTime; return; }
        if (Time.unscaledTime - seenAt < 0.2f) return;   // 단추가 다 깔릴 때까지
        var open = bs.Where(b => !b.Choice.Locked).OrderBy(b => b.transform.GetSiblingIndex()).ToArray();
        if (open.Length == 0) return;
        string script = "";
        try { var ps = player.MainTrack.PlayedScript; if (ps != null) script = ps.Path ?? ""; } catch { }
        int slash = script.LastIndexOf('/'); if (slash >= 0) script = script.Substring(slash + 1);
        int route = MolakStory.Route;
        // 그 편의 진짜 끝을 보고 나면 이야기는 거기서 맺는다 — 다음 생으로 넘기지 않고 제목 화면으로
        if (script == "MolakOver" && MolakStats.Str("g_lastend", "") == RouteEnd[Mathf.Clamp(route, 0, 4)])
        {
            if (!closing) { closing = true; Unhide(); var st = Engine.GetService<IStateManager>(); if (st != null) st.ResetState(); }
            return;
        }
        closing = false;
        float day = MolakStats.Get("day");
        if (day != pickDay) { pickDay = day; picks.Clear(); }
        MolakVn.UI.ChoiceHandlerButton best = null; float bestSc = float.MinValue; string bestLabel = "";
        foreach (var b in open)
        {
            string lab = Label(b);
            float sc = Score(route, script, lab);
            // 같은 날 같은 갈림길을 또 고르면 값이 깎인다 — 장터·객잔 같은 둘러보기 갈림길을 맴돌지 않고 나간다
            int cnt; if (picks.TryGetValue(lab, out cnt)) sc -= RepeatCost * cnt;
            if (sc > bestSc + 0.01f) { bestSc = sc; best = b; bestLabel = lab; }
        }
        if (best == null) return;
        { int c0; picks.TryGetValue(bestLabel, out c0); picks[bestLabel] = c0 + 1; }
        LastPick = script + " · " + bestLabel + " (" + bestSc.ToString("0.#") + ")";
        if (script != "MolakOver") Echo(bestLabel);   // 생과 생 사이의 고르기(다시 그날로 · 비탄)는 본문에 남기지 않는다
        picked = best; echoAt = Time.unscaledTime;
    }

    public static float Score (int route, string script, string label)
    {
        if (rows == null) Load();
        if (label.Contains("처음 화면")) return -100f;
        Row? hit = null;
        foreach (var r in rows) if (r.Route == route && r.Script == script && r.Re.IsMatch(label)) { hit = r; break; }
        if (hit == null) foreach (var r in rows) if (r.Route == route && r.Re.IsMatch(label)) { hit = r; break; }
        if (hit == null) return 0f;
        float sc = hit.Value.Score;
        foreach (var g in hit.Value.Gains) if (MolakStats.Get(g.flag) > 0f) sc -= g.w;   // 이미 가진 앎은 또 얻을 값이 없다
        return sc;
    }

    // ---------- 고른 말 한 줄 ----------
    static readonly Regex Cost = new Regex(@"\s*(\(은자[^)]*\)|〔[^〕]*〕|·\s*[^·—]*[+−-]\s*\d+[^·—]*|\b(저주|사술|내공|검술|체력|살기|은자)\s*[+−-]\s*\d+)\s*", RegexOptions.CultureInvariant);
    static void Echo (string label)
    {
        string head = label, tail = "";
        int dash = label.IndexOf(" — ", System.StringComparison.Ordinal);
        if (dash >= 0) { head = label.Substring(0, dash); tail = label.Substring(dash + 3); }
        head = Cost.Replace(head, " ").Trim(); tail = Cost.Replace(tail, " ").Trim();
        if (Regex.IsMatch(head, @"^(\S )+\S$")) head = head.Replace(" ", "");   // 「다 시 그 날 로」처럼 글자를 띄워 쓴 글귀
        string line;
        bool talk = head.StartsWith("\"") || head.StartsWith("“") || head.StartsWith("「");
        if (talk)
        {
            line = "<align=right>" + head + "</align>";
            if (tail.Length > 0 && !Regex.IsMatch(tail, @"\d")) line += "\n<color=#8A7A66>" + Stop(tail) + "</color>";
        }
        else line = "<color=#8A7A66>" + Stop(head) + "</color>";
        var pr = Printer(); if (pr == null) return;
        var buf = new List<PrintedMessage>(); pr.CollectMessages(buf);
        pr.AppendText(LocalizableText.FromPlainText((buf.Count > 0 ? "\n\n" : "") + line));
        try { pr.RevealProgress = 1f; } catch { }
    }
    static string Stop (string s) { s = s.Trim(); if (s.Length == 0) return s; char c = s[s.Length - 1]; return (c == '.' || c == '?' || c == '!' || c == '"' || c == '」' || c == '…') ? s : s + "."; }

    static ITextPrinterActor Printer ()
    {
        var pm = Engine.GetService<ITextPrinterManager>();
        if (pm == null) return null;
        var id = pm.DefaultPrinterId;
        if (string.IsNullOrEmpty(id) || !pm.ActorExists(id)) return null;
        return pm.GetActor(id);
    }

    // ---------- 단추 ----------
    static MolakVn.UI.ChoiceHandlerButton[] Buttons ()
    {
        return FindObjectsByType<MolakVn.UI.ChoiceHandlerButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(b => b.isActiveAndEnabled).ToArray();
    }
    static string Label (MolakVn.UI.ChoiceHandlerButton b)
    {
        foreach (var t in b.GetComponentsInChildren<TMPro.TMP_Text>(true))
        {
            if (t.gameObject.name == "Num") continue;
            string s = t.text ?? ""; int h = s.IndexOf('\n'); if (h >= 0) s = s.Substring(0, h);
            return Regex.Replace(s, "<[^>]+>", "").Trim();
        }
        return "";
    }
    void Hide (MolakVn.UI.ChoiceHandlerButton[] bs)
    {
        foreach (var b in bs)
        {
            var cg = b.GetComponent<CanvasGroup>(); if (cg == null) cg = b.gameObject.AddComponent<CanvasGroup>();
            if (cg.alpha > 0f || cg.blocksRaycasts) { cg.alpha = 0f; cg.blocksRaycasts = false; cg.interactable = false; if (!hidden.Contains(cg)) hidden.Add(cg); }
        }
    }
    void Unhide ()
    {
        if (hidden.Count == 0) return;
        foreach (var cg in hidden) if (cg != null) { cg.alpha = 1f; cg.blocksRaycasts = true; cg.interactable = true; }
        hidden.Clear();
    }
}
