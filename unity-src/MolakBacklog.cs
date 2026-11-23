using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// 0.5.0 지난 기록 — 지나온 글을 다시 읽는다(메뉴 → 기록 → 지난 기록). 예전에는 나니노벨의 대화 기록 창이 하던 일.
/// 찍힌 글과 고른 말을 차례로 적어 둔다(이번 판, 최근 MaxLines 줄 — 처음 화면으로 돌아가면 비운다). 저장에는 넣지 않는다.
/// </summary>
public class MolakBacklog : MonoBehaviour
{
    public const int MaxLines = 240;
    struct Row { public string Text; public bool Pick; public float H; }
    static readonly List<Row> rows = new List<Row>();
    static MolakBacklog inst;
    bool open; float openT, scrollY, dragY = -1f, laidW = -1f, contentH; bool hooked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot ()
    {
        rows.Clear();
        if (inst) return;
        var go = new GameObject("MolakBacklog"); DontDestroyOnLoad(go);
        inst = go.AddComponent<MolakBacklog>();
    }

    public static bool IsOpen => inst != null && inst.open;
    public static int Count => rows.Count;
    public static void Open () { if (inst != null) { inst.open = true; inst.openT = 0f; inst.laidW = -1f; inst.scrollY = 1e9f; } }
    public static void Clear () { rows.Clear(); if (inst != null) inst.laidW = -1f; }

    static readonly Regex Unknown = new Regex(@"</?(?!color|size|b>|i>|/b>|/i>)[a-zA-Z][^>]*>", RegexOptions.CultureInvariant);
    /// <summary>화면용 꾸밈(오른쪽 맞춤 등)은 걷고 빛·크기만 남긴다.</summary>
    static string Clean (string t) { return Unknown.Replace(t ?? "", "").Replace("​", "").Trim('\n', ' '); }

    /// <summary>찍힌 글 한 줄.</summary>
    public static void Line (string text) { Add(Clean(text), false); }
    /// <summary>고른 말.</summary>
    public static void Pick (string label)
    {
        string t = Clean(label); int cut = t.IndexOf('\n'); if (cut >= 0) t = t.Substring(0, cut);
        Add(MolakChoiceMeta.StripRisk(t), true);
    }
    static void Add (string t, bool pick)
    {
        if (string.IsNullOrEmpty(t)) return;
        rows.Add(new Row { Text = t, Pick = pick });
        if (rows.Count > MaxLines) rows.RemoveRange(0, rows.Count - MaxLines);
        if (inst != null) inst.laidW = -1f;
    }

    void Update ()
    {
        if (hooked || !MolakVn.Engine.Initialized) return;
        var st = MolakVn.Engine.GetService<MolakVn.IStateManager>(); if (st == null) return;
        st.OnResetStarted += Clear; st.OnGameLoadStarted += _ => Clear();
        hooked = true;
    }

    GUIStyle sRow, sPick; float builtU = -1f;
    void OnGUI ()
    {
        if (!open) return;
        if (Event.current.type == EventType.Repaint) openT += Time.unscaledDeltaTime;
        float u = MolakGui.U;
        if (sRow == null || !Mathf.Approximately(builtU, u))
        {
            builtU = u; laidW = -1f;
            sRow = MolakGui.Label(30, u, TextAnchor.UpperLeft, true);
            sPick = MolakGui.Label(28, u, TextAnchor.UpperLeft, true);
        }
        GUI.depth = MolakGui.DepthBlackout + 9;
        float w = Screen.width, h = Screen.height;
        MolakInputGuard.Guard(new Rect(0, 0, w, h));
        var g0 = GUI.color;
        MolakPaper.Backdrop(w, h);
        float cx = MolakPaper.Margin(w, u), cw = w - cx * 2f, y = MolakPaper.Top(h, u);
        MolakPaper.Header(ref y, cx, cw, "지난 기록", rows.Count == 0 ? "아직 지나온 글이 없다" : "지나온 글 · 아래가 가장 최근", u);
        var br = MolakPaper.BottomButton(w, h, u);
        float top = y, viewH = br.y - 24f * u - top, gap = 18f * u, tw = cw - 24f * u;
        if (!Mathf.Approximately(laidW, tw))
        {   // 줄 높이는 폭이 바뀌거나 글이 늘 때만 다시 잰다
            laidW = tw; contentH = 0f;
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                r.H = (r.Pick ? sPick : sRow).CalcHeight(new GUIContent(r.Pick ? "▸ " + r.Text : r.Text), tw) + gap;
                rows[i] = r; contentH += r.H;
            }
        }
        float maxS = Mathf.Max(0f, contentH - viewH);
        var ev = Event.current; var viewR = new Rect(cx, top, cw, viewH);
        if (ev.type == EventType.MouseDown && viewR.Contains(ev.mousePosition)) dragY = ev.mousePosition.y;
        else if (ev.type == EventType.MouseDrag && dragY >= 0f) { scrollY -= ev.mousePosition.y - dragY; dragY = ev.mousePosition.y; ev.Use(); }
        else if (ev.type == EventType.MouseUp) dragY = -1f;
        else if (ev.type == EventType.ScrollWheel && viewR.Contains(ev.mousePosition)) { scrollY += ev.delta.y * 30f * u; ev.Use(); }
        scrollY = Mathf.Clamp(scrollY, 0f, maxS);

        GUI.BeginGroup(viewR);
        float yy = -scrollY;
        MolakGui.Ink(sRow, MolakPaper.Ink); MolakGui.Ink(sPick, MolakPaper.Accent);
        foreach (var r in rows)
        {
            if (yy + r.H > 0f && yy < viewH) GUI.Label(new Rect(12f * u, yy, tw, r.H), r.Pick ? "▸ " + r.Text : r.Text, r.Pick ? sPick : sRow);
            yy += r.H;
        }
        GUI.EndGroup();

        if (MolakPaper.InkButton(br, "닫는다", u, openT > 0.2f)) { open = false; MolakInputGuard.MuteFor(0.25f); }
        GUI.Button(new Rect(0, 0, w, h), GUIContent.none, GUIStyle.none);
        GUI.color = g0;
    }
}
