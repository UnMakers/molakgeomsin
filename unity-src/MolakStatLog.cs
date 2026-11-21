using System.Collections.Generic;
using System.Text.RegularExpressions;
using Naninovel;
using UnityEngine;

/// <summary>
/// 0.4.66 능력치 기록 — 체력·내공·검술·살기·은자·저주가 오르내릴 때마다 "왜"를 한 줄로 남긴다(언메이커).
///  · 이유를 아는 자리(하루 마감의 방값·굶주림·문양, 비무 판정, @fx why:)는 Why(key, 이유) 로 바로 적는다.
///  · 나머지는 그 변화 바로 앞의 일로 갈음한다: 막 고른 선택지가 본문보다 나중이면 그 선택지, 아니면 마지막 서술 한 줄.
///  · 기록은 저장 변수 statlog 에 담겨 불러오기에도 남는다. 생이 바뀌면 비운다. 최근 80줄까지.
///  · MolakHud 가 변화를 잡을 때 Resolve → Record 를 부르고, 떠오르는 패 아래에 그 이유를 한 줄 띄운다.
/// </summary>
public static class MolakStatLog
{
    public struct Entry { public int Day; public string Key; public int D; public string Why; }

    const string Var = "statlog";
    const int Cap = 80;
    const char RS = '\u001E', FS = '\u001F';

    static readonly Dictionary<string, (string why, float t)> pend = new Dictionary<string, (string, float)>();
    static string lastNarr = "", lastAny = "", lastPick = "";
    static float lastNarrT = -99f, lastAnyT = -99f, lastPickT = -99f;
    static float lastRecT = -99f;

    static readonly Regex Tag = new Regex("<[^>]+>|\\{[^}]*\\}");
    // 0.4.87 {변수}가 든 줄은 실행 전엔 번역 표지(|#~7359bf1a|)로만 읽힌다 — 까닭에 그 표지가 그대로 찍혔다. 표지는 지운다(남는 글이 없으면 그 줄은 까닭으로 쓰지 않는다).
    static readonly Regex L10n = new Regex("\\|#~?[0-9A-Za-z_\\-]+\\|");

    /// <summary>바로 뒤에 올 그 수치의 변화에 붙일 이유. key 가 "*" 이면 모든 수치.</summary>
    public static void Why (string key, string why)
    {
        if (string.IsNullOrEmpty(why)) return;
        pend[key ?? "*"] = (why, Time.unscaledTime);
    }

    /// <summary>MolakFlow 가 본문 한 줄을 찍을 때마다 알린다.</summary>
    public static void Line (string raw)
    {
        string s = Clean(raw);
        if (s.Length < 2) return;
        lastAny = s; lastAnyT = Time.unscaledTime;
        if (!IsDialogue(s)) { lastNarr = s; lastNarrT = Time.unscaledTime; }
    }

    /// <summary>선택지 카드를 누른 순간(MolakTheme 가 단추에 붙인다).</summary>
    public static void Pick (string label)
    {
        string s = MolakChoiceMeta.StripRisk(Clean(label ?? ""));
        int dash = s.IndexOf(" — "); if (dash > 0) s = s.Substring(0, dash);
        s = s.Trim();
        if (s.Length < 1) return;
        lastPick = s; lastPickT = Time.unscaledTime;
    }

    static readonly Dictionary<string, (string why, float t)> snap = new Dictionary<string, (string, float)>();
    static readonly HashSet<string> Tracked = new HashSet<string> { "hp", "ng", "sw", "sl", "mn", "curse" };
    /// <summary>MolakStats.Set 이 값을 바꾸는 바로 그 순간의 까닭을 붙잡아 둔다('빨리'면 HUD 가 알아챌 즈음엔 글이 몇 줄 더 지나가 있다).</summary>
    public static void Snap (string key)
    {
        if (!Tracked.Contains(key)) return;
        if (snap.TryGetValue(key, out var s0) && Time.unscaledTime - s0.t < 0.05f) return;
        snap[key] = (ResolveNow(key), Time.unscaledTime);
    }

    public static string Resolve (string key)
    {
        if (snap.TryGetValue(key, out var s) && Time.unscaledTime - s.t < 1.5f) { snap.Remove(key); return s.why; }
        return ResolveNow(key);
    }

    static string ResolveNow (string key)
    {
        float now = Time.unscaledTime;
        if (pend.TryGetValue(key, out var p) && now - p.t < 1.2f) return p.why;
        if (pend.TryGetValue("*", out var a) && now - a.t < 1.2f) return a.why;
        if (lastPickT > lastNarrT && lastPickT > lastAnyT - 0.05f && now - lastPickT < 90f) return Short(lastPick, 30);
        if (now - lastNarrT < 600f && lastNarr.Length > 0) return Short(FirstSentence(lastNarr), 30);
        if (lastAny.Length > 0) return Short(FirstSentence(lastAny), 30);
        return "";
    }

    /// <summary>MolakHud 가 잡은 변화 하나를 적는다. 같은 수치·같은 이유가 0.8초 안에 또 오면 한 줄로 합친다.</summary>
    public static void Record (string key, float d, string why)
    {
        int di = Mathf.RoundToInt(d);
        if (di == 0) return;
        var list = Load();
        int day = Mathf.RoundToInt(MolakStats.Get("day"));
        float now = Time.unscaledTime;
        if (list.Count > 0 && now - lastRecT < 0.8f)
        {
            var l = list[list.Count - 1];
            if (l.Key == key && l.Why == why && l.Day == day) { l.D += di; list[list.Count - 1] = l; if (l.D == 0) list.RemoveAt(list.Count - 1); Save(list); lastRecT = now; return; }
        }
        list.Add(new Entry { Day = day, Key = key, D = di, Why = why ?? "" });
        while (list.Count > Cap) list.RemoveAt(0);
        Save(list); lastRecT = now;
    }

    /// <summary>지금 생의 기록(오래된 것부터).</summary>
    public static List<Entry> Load ()
    {
        var list = new List<Entry>();
        string s = MolakStats.Str(Var, "");
        if (string.IsNullOrEmpty(s)) return list;
        var rows = s.Split(RS);
        // 첫 칸: 그 기록을 쓴 생(g_runs). 생이 바뀌었으면 지난 생 것이므로 버린다.
        if (rows.Length == 0 || rows[0] != Run()) return list;
        for (int i = 1; i < rows.Length; i++)
        {
            var f = rows[i].Split(FS);
            if (f.Length < 4) continue;
            int.TryParse(f[0], out int dd); int.TryParse(f[2], out int dv);
            list.Add(new Entry { Day = dd, Key = f[1], D = dv, Why = f[3] });
        }
        return list;
    }

    static void Save (List<Entry> list)
    {
        var sb = new System.Text.StringBuilder(Run());
        foreach (var e in list) sb.Append(RS).Append(e.Day).Append(FS).Append(e.Key).Append(FS).Append(e.D).Append(FS).Append((e.Why ?? "").Replace(RS, ' ').Replace(FS, ' '));
        MolakStats.SetStr(Var, sb.ToString());
    }

    static string Run () { return MolakStats.Get("g_runs").ToString("0"); }

    /// <summary>받침에 맞춘 을/를.</summary>
    public static string Eul (string w)
    {
        if (string.IsNullOrEmpty(w)) return "을";
        char c = w[w.Length - 1];
        if (c < 0xAC00 || c > 0xD7A3) return "을";
        return ((c - 0xAC00) % 28) == 0 ? "를" : "을";
    }

    public static string Name (string key)
    {
        switch (key) { case "hp": return "체력"; case "ng": return "내공"; case "sw": return "검술"; case "sl": return "살기"; case "mn": return "은자"; case "curse": return "저주"; case "insight": return "깨달음"; }
        return key;
    }

    static string Clean (string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = L10n.Replace(Tag.Replace(s, ""), "").Replace("\n", " ").Replace("\r", " ").Replace("​", "");
        return Regex.Replace(s, "\\s+", " ").Trim();
    }

    static bool IsDialogue (string s) { return s.Length > 0 && (s[0] == '"' || s[0] == '“' || s[0] == '「' || s[0] == '\''); }

    static string FirstSentence (string s)
    {
        for (int i = 6; i < s.Length - 1; i++)
            if ((s[i] == '.' || s[i] == '?' || s[i] == '!') && s[i + 1] == ' ') return s.Substring(0, i + 1);
        return s;
    }

    static string Short (string s, int n) { return s.Length <= n ? s : s.Substring(0, n - 1).TrimEnd() + "…"; }
}

/// <summary>메뉴의 「능력치 기록」 판 — 이번 생에 오르내린 것을 날짜별로.</summary>
public class MolakStatLogView : MonoBehaviour
{
    static MolakStatLogView inst;
    bool open; float openT; Vector2 scroll; float dragY = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot ()
    {
        if (inst) return;
        var go = new GameObject("MolakStatLogView"); DontDestroyOnLoad(go);
        inst = go.AddComponent<MolakStatLogView>();
    }
    public static bool IsOpen => inst != null && inst.open;
    public static void Open () { if (inst != null) { inst.open = true; inst.openT = 0f; inst.scroll = new Vector2(0f, 1e6f); } }

    GUIStyle sHead, sRow, sSub, sBtn, sDay, sNum; float builtU = -1f;
    void Styles (float u)
    {
        if (sHead != null && Mathf.Approximately(builtU, u)) return;
        builtU = u;
        sHead = MolakGui.Label(44, u, TextAnchor.MiddleCenter, false); sHead.font = MolakTypography.Heading(sHead.font); sHead.richText = true;
        sRow = MolakGui.Label(28, u, TextAnchor.MiddleLeft, false); sRow.richText = true; sRow.clipping = TextClipping.Clip;
        sNum = MolakGui.Label(28, u, TextAnchor.MiddleLeft, false); sNum.richText = true;
        sDay = MolakGui.Label(24, u, TextAnchor.MiddleLeft, false); sDay.richText = true;
        sSub = MolakGui.Label(24, u, TextAnchor.MiddleCenter, true); sSub.richText = true;
        sBtn = MolakGui.Label(32, u, TextAnchor.MiddleCenter, false);
    }

    void OnGUI ()
    {
        if (!open || !Engine.Initialized) return;
        openT += Event.current.type == EventType.Repaint ? Time.unscaledDeltaTime : 0f;
        float u = MolakGui.U; Styles(u);
        GUI.depth = MolakGui.DepthBlackout + 9;
        float w = Screen.width, h = Screen.height;
        MolakInputGuard.Guard(new Rect(0, 0, w, h));
        var g0 = GUI.color;
        // 0.4.80 창의 결(MolakPaper)
        var ink = MolakPaper.Ink; var gold = MolakPaper.Accent; var dim = MolakPaper.Sub;
        MolakPaper.Backdrop(w, h);
        float cx = MolakPaper.Margin(w, u), cw = w - cx * 2f;
        float y = MolakPaper.Top(h, u);
        var list = MolakStatLog.Load();
        MolakPaper.Header(ref y, cx, cw, "능력치 기록", list.Count == 0 ? "이번 생에는 아직 바뀐 능력치가 없다" : "이번 생 · 최근 " + list.Count + "줄 · 아래가 가장 최근", u);
        var br = MolakPaper.BottomButton(w, h, u);

        float rowH = 62f * u, top = y, bottom = br.y - 24f * u, viewH = bottom - top;
        // 날이 바뀌는 자리에 날짜 띠를 끼운다
        int rows = 0; int pd = -999;
        foreach (var e in list) { if (e.Day != pd) { rows++; pd = e.Day; } rows++; }
        float contentH = rows * rowH;
        float maxS = Mathf.Max(0f, contentH - viewH);
        // 끌어서 넘기기(모바일)
        var ev = Event.current;
        var viewR = new Rect(cx, top, cw, viewH);
        if (ev.type == EventType.MouseDown && viewR.Contains(ev.mousePosition)) dragY = ev.mousePosition.y;
        else if (ev.type == EventType.MouseDrag && dragY >= 0f) { scroll.y -= ev.mousePosition.y - dragY; dragY = ev.mousePosition.y; ev.Use(); }
        else if (ev.type == EventType.MouseUp) dragY = -1f;
        else if (ev.type == EventType.ScrollWheel && viewR.Contains(ev.mousePosition)) { scroll.y += ev.delta.y * 30f * u; ev.Use(); }
        scroll.y = Mathf.Clamp(scroll.y, 0f, maxS);

        GUI.BeginGroup(viewR);
        float yy = -scroll.y; pd = -999;
        foreach (var e in list)
        {
            if (e.Day != pd)
            {
                pd = e.Day;
                if (yy > -rowH && yy < viewH)
                {
                    MolakGui.Ink(sDay, gold);
                    GUI.Label(new Rect(8f * u, yy + 8f * u, cw, rowH - 8f * u), e.Day <= 0 ? "첫날 전" : "제 " + e.Day + "일", sDay);
                    MolakPaper.Box(new Rect(150f * u, yy + rowH * 0.6f, cw - 160f * u, Mathf.Max(1f, 1f * u)), new Color(gold.r, gold.g, gold.b, 0.30f));
                }
                yy += rowH;
            }
            if (yy > -rowH && yy < viewH)
            {
                bool good = e.Key == "curse" ? e.D < 0 : e.D > 0;
                bool nt = MolakTheme.Night;
                string col = e.Key == "mn" && good ? (nt ? "#F5D060" : "#7A5A10") : e.Key == "curse" && !good ? (nt ? "#C9A0FF" : "#6E3E9E") : good ? (nt ? "#8FB4E8" : "#2E4E8C") : (nt ? "#FF7A61" : "#9C2A22");
                MolakPaper.CardFace(new Rect(0, yy + 3f * u, cw, rowH - 6f * u), Color.clear, u);
                MolakGui.Ink(sNum, ink);
                GUI.Label(new Rect(18f * u, yy, 200f * u, rowH), "<color=" + col + ">" + MolakStatLog.Name(e.Key) + " " + (e.D > 0 ? "+" : "−") + Mathf.Abs(e.D) + "</color>", sNum);
                MolakGui.Ink(sRow, string.IsNullOrEmpty(e.Why) ? dim : ink);
                GUI.Label(new Rect(210f * u, yy, cw - 224f * u, rowH), string.IsNullOrEmpty(e.Why) ? "이유 모름" : e.Why, sRow);
            }
            yy += rowH;
        }
        GUI.EndGroup();

        if (MolakPaper.InkButton(br, "닫는다", u, openT > 0.2f)) { open = false; MolakInputGuard.MuteFor(0.25f); }
        GUI.Button(new Rect(0, 0, w, h), GUIContent.none, GUIStyle.none);
        GUI.color = g0;
    }
}
