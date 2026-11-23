using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MolakVn;
using MolakVn.Commands;
using UnityEngine;

/// <summary>
/// 0.4.70 지난 생을 따라 걷기(언메이커: 죽으면 똑같은 걸 한 번 더 반복하니 지루하다 — 했던 데까지 자동으로, 달라진 데와 중요한 선택만 고르게).
///  · 한 생 동안 고른 선택(갈림길 자리 → 고른 글귀)과 들어선 사건·마디를 적어 둔다(PlayerPrefs, 생을 넘어 남는다).
///  · 회귀하면 첫날 아침부터 지난 생의 선택을 그대로 따라 넘기고, 본문 대신 '하루 한 줄'만 보여 준다.
///  · 멈추고 고르게 하는 자리: 지난 생에 죽음으로 이어진 선택, 이야기 마디(소아·히로인·복수 등)의 선택, 비무, 지난 생엔 없던 선택지가 보일 때.
///    고르고 나면 그날 남은 일은 직접 읽고, 다음 날 아침부터 다시 따라 걷는다.
///  · 끝내는 자리: 지난 생엔 없던 사건·마디·갈림길, 지난 생이 끝난 날, 화면을 누를 때.
/// </summary>
public class MolakAutoLife : MonoBehaviour
{
    static MolakAutoLife inst;
    const string KCur = "molak.life.cur", KCurSeen = "molak.life.curseen", KCurDay = "molak.life.curday";
    const string KPrev = "molak.life.prev", KPrevSeen = "molak.life.prevseen", KPrevDay = "molak.life.prevday";
    public const string KEnabled = "molak.autolife";

    struct Entry { public string Key, Label, Sig; public int Day; public bool Fatal; }

    IScriptPlayer player;
    readonly List<Entry> cur = new List<Entry>();
    readonly HashSet<string> curSeen = new HashSet<string>();
    readonly Dictionary<string, Entry> prev = new Dictionary<string, Entry>();
    readonly HashSet<string> prevSeen = new HashSet<string>();
    int prevDay, curDay;
    bool loaded;

    // 상태
    enum Mode { Off, Armed, Running, Paused, Reading }
    Mode mode = Mode.Off;
    string batchKey = ""; bool lastWasChoice;
    readonly List<string> lines = new List<string>();
    string dayLine = "";
    float shownAt = -1f;
    readonly HashSet<int> hooked = new HashSet<int>();

    public static bool Enabled { get { try { return PlayerPrefs.GetInt(KEnabled, 1) == 1; } catch { return true; } } set { try { PlayerPrefs.SetInt(KEnabled, value ? 1 : 0); PlayerPrefs.Save(); } catch { } } }
    public static bool Following => inst != null && inst.mode == Mode.Running;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot ()
    {
        if (inst) return;
        var go = new GameObject("MolakAutoLife"); DontDestroyOnLoad(go); inst = go.AddComponent<MolakAutoLife>();
    }

    // ---------- 저장 ----------
    static string Esc (string s) { return (s ?? "").Replace("\t", " ").Replace("\n", " "); }
    void Load ()
    {
        if (loaded) return; loaded = true;
        try
        {
            foreach (var e in Parse(PlayerPrefs.GetString(KCur, ""))) cur.Add(e);
            foreach (var s in PlayerPrefs.GetString(KCurSeen, "").Split('\n')) if (s.Length > 0) curSeen.Add(s);
            curDay = PlayerPrefs.GetInt(KCurDay, 0);
            foreach (var e in Parse(PlayerPrefs.GetString(KPrev, ""))) prev[e.Key] = e;
            foreach (var s in PlayerPrefs.GetString(KPrevSeen, "").Split('\n')) if (s.Length > 0) prevSeen.Add(s);
            prevDay = PlayerPrefs.GetInt(KPrevDay, 0);
        }
        catch { }
    }
    static IEnumerable<Entry> Parse (string blob)
    {
        foreach (var ln in (blob ?? "").Split('\n'))
        {
            var p = ln.Split('\t'); if (p.Length < 5) continue;
            int d; int.TryParse(p[3], out d);
            yield return new Entry { Key = p[0], Label = p[1], Sig = p[2], Day = d, Fatal = p[4] == "1" };
        }
    }
    static string Dump (IEnumerable<Entry> es)
    {
        var sb = new StringBuilder();
        foreach (var e in es) sb.Append(Esc(e.Key)).Append('\t').Append(Esc(e.Label)).Append('\t').Append(Esc(e.Sig)).Append('\t').Append(e.Day).Append('\t').Append(e.Fatal ? "1" : "0").Append('\n');
        return sb.ToString();
    }
    void SaveCur ()
    {
        try
        {
            PlayerPrefs.SetString(KCur, Dump(cur));
            PlayerPrefs.SetString(KCurSeen, string.Join("\n", curSeen.ToArray()));
            PlayerPrefs.SetInt(KCurDay, curDay);
            PlayerPrefs.Save();
        }
        catch { }
    }
    /// <summary>「모든 생의 기억 지우기」 때 함께.</summary>
    public static void Reset ()
    {
        try { foreach (var k in new[] { KCur, KCurSeen, KCurDay, KPrev, KPrevSeen, KPrevDay }) PlayerPrefs.DeleteKey(k); PlayerPrefs.Save(); } catch { }
        if (inst != null) { inst.cur.Clear(); inst.curSeen.Clear(); inst.prev.Clear(); inst.prevSeen.Clear(); inst.prevDay = 0; inst.curDay = 0; inst.mode = Mode.Off; }
    }

    // 새 생: 이번 생의 기록을 '지난 생'으로 넘긴다. 마지막으로 고른 것이 죽음으로 이어진 선택이다.
    void Rotate ()
    {
        Load();
        bool first = MolakStats.Get("g_runs") < 1f;
        prev.Clear(); prevSeen.Clear(); prevDay = 0;
        if (!first && cur.Count > 0)
        {
            var last = cur[cur.Count - 1]; last.Fatal = true; cur[cur.Count - 1] = last;
            foreach (var e in cur) prev[e.Key] = e;   // 같은 자리를 두 번 지났으면 나중 것
            foreach (var s in curSeen) prevSeen.Add(s);
            prevDay = curDay;
        }
        cur.Clear(); curSeen.Clear(); curDay = 0;
        try
        {
            PlayerPrefs.SetString(KPrev, Dump(prev.Values)); PlayerPrefs.SetString(KPrevSeen, string.Join("\n", prevSeen.ToArray())); PlayerPrefs.SetInt(KPrevDay, prevDay);
        }
        catch { }
        SaveCur();
        lines.Clear(); dayLine = "";
        mode = (!first && prev.Count > 0 && prevDay > 1 && Enabled && !MolakStory.On && !MolakPractice.Active) ? Mode.Armed : Mode.Off;
        consent = false;   // 0.4.72 따라 걷기는 첫 저녁에 직접 고른다
    }

    // ---------- 흐름 ----------
    void Update ()
    {
        if (!Engine.Initialized) return;
        if (player == null)
        {
            player = Engine.GetService<IScriptPlayer>();
            if (player == null) return;
            player.OnExecute += OnExecute;
            Load();
        }
        HookButtons();
        if (mode == Mode.Running)
        {
            // 사람이 화면을 눌러 넘기기가 꺼졌다 = 여기서 멈춘다
            if (!MolakPlayMode.SkipAll && Time.unscaledTime - shownAt > 0.6f) { Exit("여기서 멈춘다. 이제부터는 직접 걷는다."); return; }
            Decide();
        }
        else if (mode == Mode.Paused)
        {
            // 고르고 나서 대본이 다시 흐르면, 그날 남은 일은 직접 읽는다
            if (ChoiceButtons().Length == 0 && player.MainTrack != null && player.MainTrack.Playing) mode = Mode.Reading;
        }
    }

    void OnDestroy () { if (player != null) player.OnExecute -= OnExecute; }

    void OnExecute (IScriptTrack track)
    {
        var cmd = track != null ? track.PlayedCommand : null;
        if (cmd == null || MolakPractice.Active) return;
        string script = track.PlayedScript != null ? track.PlayedScript.Path : "";
        if (cmd is InitRunCommand) { Rotate(); return; }
        if (cmd is DayTickCommand)
        {
            var dtc = (DayTickCommand)cmd;
            int d = Mathf.RoundToInt(MolakStats.Get("day")) + (dtc.N != null && dtc.N.HasValue ? Mathf.Max(1, dtc.N.Value) : 1);   // 실행 전이라 넘길 날을 더한다
            if (d > curDay) { curDay = d; SaveCur(); }
            if (mode == Mode.Armed && !consent) { mode = Mode.Off; return; }   // 0.4.72 첫 저녁에 고르지 않았으면 이번 생은 직접 걷는다
            if (mode == Mode.Armed || mode == Mode.Reading || mode == Mode.Paused)
            {
                if (prevDay > 0 && d >= prevDay) { mode = Mode.Off; return; }
                BeginFollow(d);
            }
            else if (mode == Mode.Running)
            {
                if (prevDay > 0 && d >= prevDay) { Exit("지난 생은 여기서 끝났다. 여기부터는 처음 걷는 날이다."); return; }
                NewDay(d);
            }
            return;
        }
        if (cmd is Gosub)
        {
            var gp = ((Gosub)cmd).Path;
            string target = Target(gp != null && gp.HasValue ? gp.Value : null, script);
            if (target == null) return;
            if (!curSeen.Contains(target)) { curSeen.Add(target); SaveCur(); }
            if (mode == Mode.Running && !prevSeen.Contains(target) && IsEventLabel(target)) { Read(); return; }
            return;
        }
        if (cmd is ChapterCommand)
        {
            var cc = (ChapterCommand)cmd;
            string t = cc.Text != null && cc.Text.HasValue ? cc.Text.Value : "";
            if (t.Length == 0) return;
            string k = "ch:" + t;
            if (!curSeen.Contains(k)) { curSeen.Add(k); SaveCur(); }
            if (mode == Mode.Running)
            {
                if (!prevSeen.Contains(k)) { Read(); return; }
                Add("「" + t + "」");
            }
            return;
        }
        if (cmd is AddChoice)
        {
            if (!lastWasChoice)
            {   // 갈림길 자리 = 대본#라벨+라벨에서 몇 줄 아래(줄 번호는 대본을 고칠 때마다 밀리므로 라벨 기준)
                int li = cmd.PlaybackSpot.LineIndex; string lab = null; int off = li;
                try { lab = track.PlayedScript.GetLabelForLine(li); if (!string.IsNullOrEmpty(lab)) off = li - track.PlayedScript.GetLineIndexForLabel(lab); } catch { }
                batchKey = script + ":" + (lab ?? "") + "+" + off;
            }
            lastWasChoice = true;
            return;
        }
        if (!(cmd is Stop)) lastWasChoice = false;
    }

    static string Target (string v, string script)
    {
        if (v == null) return null;
        int h = v.IndexOf('#');
        string s = h >= 0 ? v.Substring(0, h) : v, l = h >= 0 ? v.Substring(h + 1) : "";
        if (string.IsNullOrEmpty(s)) s = script;
        return s + "#" + l;
    }
    /// <summary>사건·마디로 들어가는 자리인가(하루 흐름을 도는 도구 라벨은 빼고).</summary>
    static bool IsEventLabel (string target)
    {
        int h = target.IndexOf('#'); string l = h >= 0 ? target.Substring(h + 1) : target;
        if (l.Length == 0) return true;   // 다른 대본 머리
        return l.StartsWith("E_") || l.StartsWith("S_") || l.StartsWith("H_") || l.StartsWith("Soa") || l.StartsWith("Ch") || l.StartsWith("Am") || l.StartsWith("Cm") || l.StartsWith("Vis") || l.StartsWith("Ep");
    }

    /// <summary>멈춰서 직접 고르게 하는 갈림길인가 — 이야기 마디의 선택.</summary>
    static bool Important (string key)
    {
        string script = key.Contains(":") ? key.Substring(0, key.IndexOf(':')) : key;
        if (script == "MolakDuel") return true;   // 비무는 손으로
        if (script == "Molak01" || script == "Molak02" || script == "MolakEvents" || script == "Molak19") return false;   // 첫날 저잣거리·하루 흐름·일상 사건·찾아가기
        return true;   // Molak01(서장·맹세)·03~18 이야기 마디
    }
    bool HeroineEvent ()
    {
        string ev = MolakStats.Str("nextevent") ?? "";
        return ev.StartsWith("E_Soa") || ev.StartsWith("E_Amh") || ev.StartsWith("E_Cm") || ev.StartsWith("E_Cham") || ev.StartsWith("E_Temple");
    }

    bool consent;
    /// <summary>0.4.72 @autolife do:follow — 첫 저녁에 고른다. 그날 밤부터 지난 생을 따라 걷는다.</summary>
    public static void Follow ()
    {
        var me = FindAnyObjectByType<MolakAutoLife>();
        if (me == null || me.mode != Mode.Armed) return;
        me.consent = true; me.startDay = Mathf.RoundToInt(MolakStats.Get("day"));
        me.BeginFollow(Mathf.Max(1, me.startDay));
    }
    public static void Decline ()
    {
        var me = FindAnyObjectByType<MolakAutoLife>();
        if (me != null && me.mode == Mode.Armed) me.mode = Mode.Off;
    }
    int startDay;
    // 다시 걷는 마음 — 날이 넘어갈 때마다 한 줄(같은 줄이 이어서 나오지 않게)
    static readonly string[] Resolve = {
        "같은 칼이 같은 자리에서 온다. 이번엔 반 치 먼저 피한다.",
        "어제 죽은 자리를 오늘 지나간다. 발이 먼저 안다.",
        "같은 국밥, 같은 김. 손등의 흉터만 하나 늘었다.",
        "아는 길이라도 발은 똑같이 아프다.",
        "머리보다 검을 쥔 손이 먼저 기억해 낸다.",
        "몇 번째 같은 하늘인지 세지 않는다. 대신 숨을 고른다.",
        "지난 생의 내가 앞서 걷는다. 그 등을 놓치지 않는다.",
        "이번엔 끝까지 간다. 지난 생에도 똑같이 다짐했다.",
    };
    int resolveIdx = -1;

    void BeginFollow (int d)
    {
        mode = Mode.Running; shownAt = Time.unscaledTime;
        MolakStats.Set("autofollow", 1f); MolakSfx.Follow();   // 회귀 첫날의 「기억대로 흘려보낸다」 갈림을 건너뛴다(따라 걷기가 그 일을 한다)
        if (lines.Count == 0) lines.Add("지난 생의 발자국을 따라 걷는다.");
        NewDay(d);
        MolakPlayMode.SetSkipAll(true);
    }
    void NewDay (int d) { dayLine = DayWord(d); lines.Add(dayLine); Trim(); resolveIdx = (resolveIdx + 1 + Random.Range(0, Resolve.Length - 1)) % Resolve.Length; MolakSfx.Page(); }
    void Add (string s)
    {
        if (lines.Count == 0) lines.Add(dayLine);
        string last = lines[lines.Count - 1];
        lines[lines.Count - 1] = last.Length > 0 && last.Length < 70 ? last + " · " + s : (last.Length >= 70 ? last : s);
        if (last.Length >= 70) lines.Add("    " + s);
        Trim();
    }
    void Trim () { while (lines.Count > 9) lines.RemoveAt(0); }

    void Exit (string why)
    {
        bool was = mode == Mode.Running;
        mode = Mode.Off;
        if (MolakPlayMode.SkipAll) MolakPlayMode.SetSkipAll(false);
        if (was && !string.IsNullOrEmpty(why)) MolakVfx.Stamp("醒", why, true);
    }

    // ---------- 갈림길 ----------
    MolakVn.UI.ChoiceHandlerButton[] ChoiceButtons ()
    {
        return FindObjectsByType<MolakVn.UI.ChoiceHandlerButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(b => b.isActiveAndEnabled).ToArray();
    }
    static readonly Regex Tags = new Regex("<[^>]+>");
    static string LabelOf (MolakVn.UI.ChoiceHandlerButton b)
    {
        foreach (var t in b.GetComponentsInChildren<TMPro.TMP_Text>(true))
        {
            if (t.gameObject.name == "Num") continue;
            string s = t.text ?? ""; int h = s.IndexOf('\n'); if (h >= 0) s = s.Substring(0, h);
            return Tags.Replace(s, "").Trim();
        }
        return "";
    }
    static string Sig (IEnumerable<MolakVn.UI.ChoiceHandlerButton> bs) { return string.Join("|", bs.Where(b => !b.Choice.Locked).Select(LabelOf).OrderBy(x => x).ToArray()); }

    void HookButtons ()
    {
        var bs = ChoiceButtons(); if (bs.Length == 0) { if (hooked.Count > 64) hooked.Clear(); return; }
        foreach (var b in bs)
        {
            int id = b.GetInstanceID(); if (hooked.Contains(id)) continue; hooked.Add(id);
            var ub = b.GetComponent<UnityEngine.UI.Button>(); if (ub == null) continue;
            var me = b;
            ub.onClick.AddListener(() => Record(me));
        }
    }
    void Record (MolakVn.UI.ChoiceHandlerButton b)
    {
        if (MolakPractice.Active || string.IsNullOrEmpty(batchKey)) return;
        var all = ChoiceButtons();
        cur.Add(new Entry { Key = batchKey, Label = LabelOf(b), Sig = Sig(all), Day = Mathf.RoundToInt(MolakStats.Get("day")) });
        SaveCur();
        // 회귀 아침의 맹세(이번 생엔 무엇을 하겠느냐)는 직접 고르고, 그다음부터 따라 걷는다
        if (mode == Mode.Armed && !consent) MolakStats.Set("autoready", 1f);   // 0.4.72 첫 저녁에 「지난 생의 길을 다시 밟는다」를 묻는다
        else if (mode == Mode.Paused) mode = Mode.Reading;   // 직접 고른 뒤 그날 남은 일은 직접 읽는다
    }

    float decideAt = -1f; string decidedKey = "";
    void Decide ()
    {
        var bs = ChoiceButtons();
        if (bs.Length == 0 || player.MainTrack == null || player.MainTrack.Playing) { decideAt = -1f; return; }
        if (decideAt < 0f) { decideAt = Time.unscaledTime; return; }
        if (Time.unscaledTime - decideAt < 0.25f) return;   // 단추가 다 깔릴 때까지
        decideAt = -1f;
        string key = batchKey;
        Entry rec;
        if (!prev.TryGetValue(key, out rec)) { Pause(null); return; }
        var open = bs.Where(b => !b.Choice.Locked).ToArray();
        var labels = open.Select(LabelOf).ToArray();
        var pick = open.FirstOrDefault(b => LabelOf(b) == rec.Label);
        var prevSet = new HashSet<string>((rec.Sig ?? "").Split('|'));
        bool newPath = labels.Any(l => !prevSet.Contains(l));
        if (rec.Fatal) { Pause("지난 생엔 「" + Short(rec.Label) + "」 — 그 길 끝에서 죽었다."); return; }
        if (Important(key) || HeroineEvent()) { Pause(null); return; }
        if (newPath) { Pause("지난 생엔 보이지 않던 길이 보인다."); return; }
        if (pick == null) { Pause(null); return; }
        Add(Short(rec.Label));
        var ub = pick.GetComponent<UnityEngine.UI.Button>();
        if (ub != null) ub.onClick.Invoke();
    }
    /// <summary>지난 생엔 없던 일: 요약을 걷고 직접 읽는다. 다음 날 아침(또는 장면이 바뀌면) 다시 따라 걷는다.</summary>
    void Read ()
    {
        mode = Mode.Reading;
        if (MolakPlayMode.SkipAll) MolakPlayMode.SetSkipAll(false);
    }
    void Pause (string note)
    {
        mode = Mode.Paused;
        if (MolakPlayMode.SkipAll) MolakPlayMode.SetSkipAll(false);
        if (!string.IsNullOrEmpty(note)) MolakVfx.Stamp("憶", note, true);
    }
    static string Short (string s)
    {
        s = Regex.Replace(s ?? "", "〔[^〕]*〕|\\([^)]*\\)", "").Trim();
        int dash = s.IndexOf(" — "); if (dash > 6) s = s.Substring(0, dash);
        return s.Length > 26 ? s.Substring(0, 25) + "…" : s;
    }

    static readonly string[] Small = { "", "첫날", "이틀째", "사흘째", "나흘째", "닷새째", "엿새째", "이레째", "여드레째", "아흐레째", "열흘째" };
    static readonly string[] Unit = { "", "하루", "이틀", "사흘", "나흘", "닷새", "엿새", "이레", "여드레", "아흐레" };
    static readonly string[] Tens = { "", "열", "스무", "서른", "마흔", "쉰", "예순", "일흔" };
    public static string DayWord (int d)
    {
        if (d <= 0) return "";
        if (d <= 10) return Small[d];
        int t = d / 10, u = d % 10;
        if (t >= Tens.Length) return d + "일째";
        return Tens[t] + (u == 0 ? (t == 2 ? "날" : "날") : Unit[u]) + "째";
    }

    // ---------- 하루 한 줄 ----------
    GUIStyle sHead, sLine, sHint; float bu = -1f;
    void OnGUI ()
    {
        if (mode != Mode.Running || MolakMenu.IsOpen || MolakTitle.Showing) return;
        float u = MolakGui.U;
        if (sHead == null || !Mathf.Approximately(bu, u))
        {
            bu = u;
            sHead = MolakGui.Label(30, u, TextAnchor.UpperCenter, false);
            sLine = MolakGui.Label(29, u, TextAnchor.UpperLeft, true); sLine.richText = true;
            sHint = MolakGui.Label(22, u, TextAnchor.LowerCenter, false);
        }
        GUI.depth = MolakGui.DepthVfx + 4;
        var ta = MolakVfx.TextArea();
        float a = Mathf.Clamp01((Time.unscaledTime - shownAt) / 0.35f);
        var g = GUI.color;
        GUI.color = new Color(0.955f, 0.94f, 0.905f, 0.985f * a); GUI.DrawTexture(ta, MolakGui.Px);
        GUI.color = g;
        MolakGui.Ink(sHead, new Color(0.45f, 0.36f, 0.24f, a));
        GUI.Label(new Rect(ta.x, ta.y + 18f * u, ta.width, 40f * u), "지난 생을 따라 걷는다", sHead);
        float y = ta.y + 78f * u;
        for (int i = 0; i < lines.Count; i++)
        {
            bool lastOne = i == lines.Count - 1;
            float la = a * (lastOne ? 1f : Mathf.Lerp(0.35f, 0.8f, i / (float)Mathf.Max(1, lines.Count - 1)));
            MolakGui.Ink(sLine, new Color(0.18f, 0.14f, 0.11f, la));
            var r = new Rect(ta.x + 34f * u, y, ta.width - 68f * u, 200f * u);
            float hh = sLine.CalcHeight(new GUIContent(lines[i]), r.width);
            GUI.Label(new Rect(r.x, y, r.width, hh), lines[i], sLine);
            y += hh + 10f * u;
        }
        // 0.4.72 지난 생이 끝난 날까지 — 붓 한 획이 그만큼 그어진다
        if (prevDay > 1)
        {
            float cd = MolakStats.Get("day"), p = Mathf.Clamp01((cd - startDay) / Mathf.Max(1f, prevDay - startDay));
            float bx = ta.x + 34f * u, bw = ta.width - 68f * u, by = ta.y + 62f * u;
            var g2 = GUI.color;
            GUI.color = new Color(0.55f, 0.48f, 0.40f, 0.35f * a); GUI.DrawTexture(new Rect(bx, by, bw, Mathf.Max(1f, 1f * u)), MolakGui.Px);
            GUI.color = new Color(0.16f, 0.12f, 0.10f, 0.8f * a);
            int seg = 60;
            for (int i = 0; i < seg; i++)
            {
                float f0 = i / (float)seg; if (f0 > p) break;
                float th = Mathf.Lerp(5f, 1.5f, f0) * u;
                GUI.DrawTexture(new Rect(bx + bw * f0, by - th * 0.5f, bw / seg + 1f, th), MolakGui.Px);
            }
            GUI.color = g2;
        }
        if (resolveIdx >= 0)
        {
            MolakGui.Ink(sHint, new Color(0.42f, 0.30f, 0.56f, a * 0.9f));
            GUI.Label(new Rect(ta.x + 20f * u, ta.yMax - 92f * u, ta.width - 40f * u, 40f * u), Resolve[resolveIdx], sHint);
        }
        MolakGui.Ink(sHint, new Color(0.45f, 0.4f, 0.34f, a * (0.6f + 0.25f * Mathf.Sin(Time.unscaledTime * 2.4f))));
        GUI.Label(new Rect(ta.x, ta.yMax - 46f * u, ta.width, 34f * u), "누르면 여기서 멈추고 직접 걷는다", sHint);
    }
}

/// <summary>0.4.72 @autolife do:follow|off — 회귀 첫 저녁의 갈림.</summary>
[Alias("autolife")]
public class AutoLifeCommand : MolakVn.Command
{
    public MolakVn.StringParameter Do;
#pragma warning disable CS1998
    public override async Awaitable Execute (MolakVn.ExecutionContext ctx)
#pragma warning restore CS1998
    {
        if (Assigned(Do) && Do.Value == "follow") MolakAutoLife.Follow(); else MolakAutoLife.Decline();
    }
}
