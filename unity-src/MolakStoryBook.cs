using System.Collections.Generic;
using System.IO;
using System.Text;
using Naninovel;
using UnityEngine;

/// <summary>
/// 0.4.78 이야기 모드 장 고르기(언메이커: 스토리 모드를 세분화 — 맨날 처음부터 다시 해야 한다).
///  · 이야기 모드로 읽다가 마디 이름(@chapter)이 뜨는 자리에 닿으면, 그 순간의 변수 전부(이번 생 + 전역)와
///    대본·라벨을 기기에 적어 둔다(persistentDataPath/storybook). 줄 번호가 아니라 라벨로 적으니 대본을 고쳐도 덜 어긋난다.
///  · 제목 화면 「이야기 모드」 → 장 목록. 「서장부터」 또는 닿아 본 장을 골라 그 자리부터 읽는다.
///    장은 두 갈래로 묶는다 — 첫 생(소아를 잃는 길)과 둘째 생(적운의 구결을 품고 되돌리는 길).
///  · 기기에 적힌 장이 없으면 게임에 들어 있는 기본 책갈피(Resources/MolakData/storybook)를 쓴다.
///  · 장으로 뛰어들면 전역 변수가 그 장의 것으로 바뀐다. 들어가기 전 전역을 한 번 적어 두었다가,
///    「처음부터」·「비무 연습」·「서장부터」로 돌아갈 때 되돌린다(이름은 건드리지 않는다).
/// </summary>
public class MolakStoryBook : MonoBehaviour
{
    public static bool Open { get; private set; }
    static MolakStoryBook inst;

    public class Entry
    {
        public string key, title, sub, script, label;
        public bool second;      // 둘째 생(구결을 품은 생)
        public float day;
        public long order;
        public bool bundled;
        public string body;      // 변수 줄
    }

    static string Dir { get { return Path.Combine(Application.persistentDataPath, "storybook"); } }
    static string BackupPath { get { return Path.Combine(Dir, "_globals_backup.txt"); } }
    static bool jumping;

    // ---------------------------------------------------------------- 적기
    /// <summary>@chapter 가 실행될 때(넘기기 중이어도) 부른다. 이야기 모드에서만 적는다.</summary>
    public static void Mark (string title, string sub, Command cmd)
    {
        try
        {
            if (jumping || !MolakStory.On || MolakPractice.Active || cmd == null) return;
            var sp = Engine.GetService<IScriptPlayer>();
            var tr = sp != null ? sp.MainTrack : null;
            if (tr == null || tr.PlayedScript == null) return;
            string script = tr.PlayedScript.Path, label = null;
            try { label = tr.PlayedScript.GetLabelForLine(cmd.PlaybackSpot.LineIndex); } catch { }
            if (string.IsNullOrEmpty(label) || string.IsNullOrEmpty(title)) return;
            bool second = MolakStats.Get("g_redverse") > 0f;
            string key = Safe(script + "_" + label) + (second ? "__b" : "__a");
            Directory.CreateDirectory(Dir);
            string path = Path.Combine(Dir, key + ".txt");
            long order = System.DateTime.UtcNow.Ticks;
            if (File.Exists(path)) { var old = Parse(File.ReadAllText(path, Encoding.UTF8), key, false); if (old != null && old.order > 0) order = old.order; }
            var sb = new StringBuilder();
            sb.Append("#title\t").Append(Esc(title)).Append('\n');
            sb.Append("#sub\t").Append(Esc(sub ?? "")).Append('\n');
            sb.Append("#script\t").Append(script).Append('\n');
            sb.Append("#label\t").Append(label).Append('\n');
            sb.Append("#second\t").Append(second ? "1" : "0").Append('\n');
            sb.Append("#day\t").Append(MolakStats.Get("day").ToString("0")).Append('\n');
            sb.Append("#order\t").Append(order).Append('\n');
            var vars = Engine.GetService<ICustomVariableManager>();
            foreach (var v in vars.Variables)
            {
                if (v.Value.Type == CustomVariableValueType.Numeric) sb.Append(v.Name).Append("\tN\t").Append(v.Value.Number.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append('\n');
                else if (v.Value.Type == CustomVariableValueType.String) sb.Append(v.Name).Append("\tS\t").Append(Esc(v.Value.String ?? "")).Append('\n');
            }
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }
        catch (System.Exception e) { Debug.LogWarning("[MolakStoryBook] 적기 실패: " + e.Message); }
    }

    static string Safe (string s)
    {
        var sb = new StringBuilder();
        foreach (var c in s) sb.Append(char.IsLetterOrDigit(c) && c < 128 ? c : '_');
        return sb.ToString();
    }
    static string Esc (string s) { return s.Replace("\\", "\\\\").Replace("\t", "\\t").Replace("\n", "\\n"); }
    static string Unesc (string s)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '\\' && i + 1 < s.Length) { char n = s[++i]; sb.Append(n == 't' ? '\t' : n == 'n' ? '\n' : n); }
            else sb.Append(s[i]);
        }
        return sb.ToString();
    }

    static Entry Parse (string text, string key, bool bundled)
    {
        var e = new Entry { key = key, bundled = bundled, body = text };
        foreach (var raw in text.Split('\n'))
        {
            if (raw.Length == 0 || raw[0] != '#') continue;
            int t = raw.IndexOf('\t'); if (t < 0) continue;
            string k = raw.Substring(1, t - 1), v = raw.Substring(t + 1).TrimEnd('\r');
            switch (k)
            {
                case "title": e.title = Unesc(v); break;
                case "sub": e.sub = Unesc(v); break;
                case "script": e.script = v; break;
                case "label": e.label = v; break;
                case "second": e.second = v == "1"; break;
                case "day": float.TryParse(v, out e.day); break;
                case "order": long.TryParse(v, out e.order); break;
            }
        }
        return string.IsNullOrEmpty(e.script) || string.IsNullOrEmpty(e.label) ? null : e;
    }

    /// <summary>기기의 장 + 게임에 든 기본 장(기기에 같은 장이 없을 때만).</summary>
    public static List<Entry> All ()
    {
        var map = new Dictionary<string, Entry>();
        try
        {
            foreach (var ta in Resources.LoadAll<TextAsset>("MolakData/storybook"))
            {
                var e = Parse(ta.text, ta.name, true);
                if (e != null) map[e.key] = e;
            }
        }
        catch { }
        try
        {
            if (Directory.Exists(Dir))
                foreach (var f in Directory.GetFiles(Dir, "*.txt"))
                {
                    var key = Path.GetFileNameWithoutExtension(f);
                    if (key.StartsWith("_")) continue;
                    var e = Parse(File.ReadAllText(f, Encoding.UTF8), key, false);
                    if (e != null) map[key] = e;
                }
        }
        catch { }
        var list = new List<Entry>(map.Values);
        list.Sort((a, b) => a.second != b.second ? (a.second ? 1 : -1) : a.day != b.day ? a.day.CompareTo(b.day) : a.order.CompareTo(b.order));
        return list;
    }

    // ---------------------------------------------------------------- 전역 지키기
    static void BackupGlobals ()
    {
        if (File.Exists(BackupPath)) return;   // 이미 장 사이를 오가는 중 — 처음 들어가기 전의 것을 지킨다
        var vars = Engine.GetService<ICustomVariableManager>();
        var sb = new StringBuilder();
        foreach (var v in vars.Variables)
        {
            if (!v.Name.StartsWith("g_")) continue;
            if (v.Value.Type == CustomVariableValueType.Numeric) sb.Append(v.Name).Append("\tN\t").Append(v.Value.Number.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append('\n');
            else if (v.Value.Type == CustomVariableValueType.String) sb.Append(v.Name).Append("\tS\t").Append(Esc(v.Value.String ?? "")).Append('\n');
        }
        Directory.CreateDirectory(Dir);
        File.WriteAllText(BackupPath, sb.ToString(), Encoding.UTF8);
    }

    /// <summary>장으로 뛰어들기 전의 전역으로 되돌린다(적어 둔 것이 있을 때만). 「처음부터」·「비무 연습」·「서장부터」에서 부른다.</summary>
    public static void RestoreGlobals ()
    {
        try
        {
            if (!File.Exists(BackupPath) || !Engine.Initialized) return;
            var vars = Engine.GetService<ICustomVariableManager>();
            var keep = new HashSet<string>();
            foreach (var line in File.ReadAllText(BackupPath, Encoding.UTF8).Split('\n'))
            {
                var p = line.Split('\t'); if (p.Length < 3) continue;
                if (p[0].StartsWith("g_pname")) { keep.Add(p[0]); continue; }
                if (p[1] == "N") { float f; if (float.TryParse(p[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f)) vars.SetVariableValue(p[0], new CustomVariableValue(f)); }
                else vars.SetVariableValue(p[0], new CustomVariableValue(Unesc(p[2])));
                keep.Add(p[0]);
            }
            var extra = new List<(string, CustomVariableValueType)>();
            foreach (var v in vars.Variables) if (v.Name.StartsWith("g_") && !keep.Contains(v.Name)) extra.Add((v.Name, v.Value.Type));
            foreach (var (n, ty) in extra) vars.SetVariableValue(n, ty == CustomVariableValueType.String ? new CustomVariableValue("none") : new CustomVariableValue(0f));
            File.Delete(BackupPath);
            var st = Engine.GetService<IStateManager>(); if (st != null) { try { _ = st.SaveGlobal(); } catch { } }
        }
        catch (System.Exception e) { Debug.LogWarning("[MolakStoryBook] 전역 되돌리기 실패: " + e.Message); }
    }

    // ---------------------------------------------------------------- 뛰어들기
    public static void Jump (Entry e)
    {
        if (e == null || !Engine.Initialized) return;
        var sp = Engine.GetService<IScriptPlayer>();
        var st = Engine.GetService<IStateManager>();
        if (sp == null || st == null) return;
        try { BackupGlobals(); } catch (System.Exception ex) { Debug.LogWarning("[MolakStoryBook] 전역 적기 실패: " + ex.Message); }
        Close();
        jumping = true;
        MolakSfx.Rewind();
        MolakBlackout.Play(new[] { e.title }, 1.6f, false, false, () =>
        {
            MolakTitle.Hold(6f);
            st.ResetState(new string[0], () =>
            {
                Apply(e.body);
                MolakStats.Set("g_storypick", 1f);
                MolakStats.Set("storymode", 1f);
                MolakName.EnsurePairs();
                jumping = false;
                return ScriptTrackExtensions.LoadAndPlayAtLabel(sp.MainTrack, e.script, e.label);
            });
        });
    }

    static void Apply (string body)
    {
        var vars = Engine.GetService<ICustomVariableManager>();
        foreach (var n in MolakVarList.Nums) MolakStats.EnsureNum(n);
        foreach (var n in MolakVarList.Strings) MolakStats.EnsureStr(n);
        foreach (var line in body.Split('\n'))
        {
            if (line.Length == 0 || line[0] == '#') continue;
            var p = line.Split('\t'); if (p.Length < 3) continue;
            if (p[0].StartsWith("g_pname")) continue;   // 이름은 지금 플레이어의 것
            if (p[1] == "N") { float f; if (float.TryParse(p[2].TrimEnd('\r'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f)) vars.SetVariableValue(p[0], new CustomVariableValue(f)); }
            else if (p[1] == "S") vars.SetVariableValue(p[0], new CustomVariableValue(Unesc(p[2].TrimEnd('\r'))));
        }
    }

    // ---------------------------------------------------------------- 화면
    List<Entry> list;
    float scroll, scrollV, openedAt, downY = -1f, downScroll, lastY;
    bool dragging;
    System.Action onFresh;

    /// <summary>제목 화면에서 연다. onFresh = 「서장부터」를 골랐을 때(제목 화면의 깨어나기).</summary>
    public static void Show (System.Action onFresh)
    {
        if (inst == null) { var go = new GameObject("MolakStoryBook"); DontDestroyOnLoad(go); inst = go.AddComponent<MolakStoryBook>(); }
        inst.list = All();
        inst.scroll = 0f; inst.scrollV = 0f; inst.openedAt = Time.realtimeSinceStartup; inst.onFresh = onFresh;
        Open = true;
    }
    public static void Close () { Open = false; }

    GUIStyle sHead, sSub, sRowT, sRowS, sGroup, sBack, sNote;
    float styleU = -1f;
    void Styles (float u)
    {
        if (sHead != null && Mathf.Approximately(styleU, u)) return;
        styleU = u;
        var ink = new Color(0.12f, 0.10f, 0.08f, 1f);
        sHead = MolakGui.Label(64, u, TextAnchor.MiddleLeft, false); MolakGui.Ink(sHead, ink);
        sSub = MolakGui.Label(28, u, TextAnchor.UpperLeft, true); MolakGui.Ink(sSub, new Color(0.40f, 0.34f, 0.28f, 1f));
        sRowT = MolakGui.Label(40, u, TextAnchor.MiddleLeft, false); MolakGui.Ink(sRowT, ink);
        sRowS = MolakGui.Label(26, u, TextAnchor.MiddleLeft, false); MolakGui.Ink(sRowS, new Color(0.45f, 0.39f, 0.32f, 1f));
        sGroup = MolakGui.Label(30, u, TextAnchor.MiddleLeft, false); MolakGui.Ink(sGroup, new Color(0.55f, 0.16f, 0.12f, 1f));
        sBack = MolakGui.Label(34, u, TextAnchor.MiddleCenter, false); MolakGui.Ink(sBack, new Color(0.96f, 0.93f, 0.87f, 1f));
        sNote = MolakGui.Label(26, u, TextAnchor.UpperCenter, true); MolakGui.Ink(sNote, new Color(0.45f, 0.39f, 0.32f, 1f));
    }

    static void Box (Rect r, Color c) { var o = GUI.color; GUI.color = c; GUI.DrawTexture(r, MolakGui.Px); GUI.color = o; }

    void OnGUI ()
    {
        if (!Open) return;
        GUI.depth = MolakGui.DepthSettings - 20;
        float u = MolakGui.U, W = Screen.width, H = Screen.height;
        Styles(u);
        float a = Mathf.Clamp01((Time.realtimeSinceStartup - openedAt) / 0.25f);
        GUI.color = new Color(1, 1, 1, a);
        Box(new Rect(0, 0, W, H), new Color(0.96f, 0.94f, 0.89f, 0.985f));

        float m = Mathf.Max(48f * u, (W - 980f * u) * 0.5f), cw = W - m * 2f;
        float y = Mathf.Max(90f * u, H * 0.06f);
        GUI.Label(new Rect(m, y, cw, 80 * u), "이야기 모드", sHead); y += 84 * u;
        GUI.Label(new Rect(m, y, cw, 90 * u), "죽지 않고 이야기만 따라 읽는다. 한 번 닿은 마디는 여기 남아, 다음엔 그 자리부터 펼칠 수 있다.", sSub); y += 96 * u;
        Box(new Rect(m, y, 120 * u, Mathf.Max(1f, 2f * u)), new Color(0.55f, 0.16f, 0.12f, 0.9f)); y += 18 * u;

        // 아래 돌아가기 단추 자리
        float backH = 96 * u, backY = H - backH - Mathf.Max(60f * u, H * 0.04f);
        var view = new Rect(0, y, W, backY - 20 * u - y);

        // 목록 높이
        float rowH = 116 * u, gh = 70 * u, gapR = 14 * u;
        float total = rowH + gapR + 10 * u;
        bool anyA = false, anyB = false;
        foreach (var e in list) { if (e.second) anyB = true; else anyA = true; }
        total += (anyA ? gh : 0) + (anyB ? gh : 0) + list.Count * (rowH + gapR) + 140 * u;
        float maxScroll = Mathf.Max(0f, total - view.height);

        // 끌기(손가락·마우스)
        Vector2 pg; bool down = MolakGui.PointerGui(out pg);
        var ev = Event.current;
        if (ev.type == EventType.Repaint)
        {
            if (down && view.Contains(pg))
            {
                if (downY < 0f) { downY = pg.y; downScroll = scroll; lastY = pg.y; dragging = false; }
                if (Mathf.Abs(pg.y - downY) > 14f * u) dragging = true;
                if (dragging) { float d = lastY - pg.y; scroll = Mathf.Clamp(scroll + d, 0f, maxScroll); scrollV = d / Mathf.Max(0.001f, Time.unscaledDeltaTime); }
                lastY = pg.y;
            }
            else if (!down)
            {
                downY = -1f;
                if (Mathf.Abs(scrollV) > 1f) { scroll = Mathf.Clamp(scroll + scrollV * Time.unscaledDeltaTime, 0f, maxScroll); scrollV *= Mathf.Pow(0.04f, Time.unscaledDeltaTime); }
            }
        }
        if (ev.type == EventType.ScrollWheel && view.Contains(ev.mousePosition)) { scroll = Mathf.Clamp(scroll + ev.delta.y * 40f * u, 0f, maxScroll); ev.Use(); }

        bool canTap = Time.realtimeSinceStartup - openedAt > 0.35f && !dragging;
        GUI.BeginGroup(view);
        float yy = -scroll;
        // 서장부터
        if (Row(new Rect(m, yy, cw, rowH), "서장부터", "청운산장의 새벽 — 처음부터 읽는다", new Color(0.12f, 0.10f, 0.08f, 1f), canTap, u))
        {
            Close(); RestoreGlobals(); onFresh?.Invoke();
        }
        yy += rowH + gapR + 10 * u;
        for (int pass = 0; pass < 2; pass++)
        {
            bool second = pass == 1;
            if (second ? !anyB : !anyA) continue;
            GUI.Label(new Rect(m, yy + 10 * u, cw, gh - 10 * u), second ? "둘째 생 — 되돌리는 길" : "첫 생 — 잃는 길", sGroup);
            Box(new Rect(m, yy + gh - 6 * u, cw, Mathf.Max(1f, 1f * u)), new Color(0.55f, 0.16f, 0.12f, 0.35f));
            yy += gh;
            foreach (var e in list)
            {
                if (e.second != second) continue;
                var r = new Rect(m, yy, cw, rowH);
                if (r.yMax > -10f && r.y < view.height + 10f)
                {
                    string sub = string.IsNullOrEmpty(e.sub) ? "" : e.sub;
                    if (Row(r, e.title, sub, second ? new Color(0.20f, 0.30f, 0.52f, 1f) : new Color(0.55f, 0.16f, 0.12f, 1f), canTap, u)) { Jump(e); GUI.EndGroup(); return; }
                }
                yy += rowH + gapR;
            }
        }
        if (!anyA && !anyB)
            GUI.Label(new Rect(m, yy + 20 * u, cw, 120 * u), "아직 닿은 마디가 없다.\n이야기 모드로 읽다 보면, 지난 마디가 여기 하나씩 쌓인다.", sNote);
        GUI.EndGroup();

        // 흐림(목록 위아래)
        Box(new Rect(0, view.y, W, 10 * u), new Color(0.96f, 0.94f, 0.89f, 0.8f));

        // 돌아가기
        var br = new Rect(W * 0.5f - 200 * u, backY, 400 * u, backH);
        Box(br, new Color(0.12f, 0.10f, 0.08f, 0.92f));
        GUI.Label(br, "제목으로 돌아간다", sBack);
        if (Time.realtimeSinceStartup - openedAt > 0.35f && GUI.Button(br, GUIContent.none, GUIStyle.none)) Close();
        GUI.color = Color.white;
    }

    bool Row (Rect r, string title, string sub, Color bar, bool canTap, float u)
    {
        Box(r, new Color(1f, 0.99f, 0.96f, 0.95f));
        Box(new Rect(r.x, r.yMax - Mathf.Max(1f, 1f * u), r.width, Mathf.Max(1f, 1f * u)), new Color(0.55f, 0.48f, 0.40f, 0.35f));
        Box(new Rect(r.x, r.y, 10 * u, r.height), bar);
        GUI.Label(new Rect(r.x + 34 * u, r.y + 10 * u, r.width - 60 * u, 56 * u), title, sRowT);
        if (!string.IsNullOrEmpty(sub)) GUI.Label(new Rect(r.x + 34 * u, r.y + 64 * u, r.width - 60 * u, 40 * u), sub, sRowS);
        return canTap && GUI.Button(r, GUIContent.none, GUIStyle.none) && !dragging;
    }
}
