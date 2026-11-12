using System.Collections.Generic;
using Naninovel;
using UnityEngine;

/// <summary>
/// 0.4.24 갈림길 표식: 플레이어가 고른 것이 이야기를 갈랐다는 것을 그 자리에서 보여 준다.
///  - 화면 위쪽에 붉은 岐(갈림길) 인장과 함께 한 줄이 먹처럼 번졌다 사라진다. 대본은 멈추지 않는다.
///  - 이번 생의 갈림길로 적어 두었다가(lifemarks), 죽을 때 @black marks:true 가 "이번 생, 당신이 고른 것." 으로 다시 읽어 준다.
/// 큰 갈림에만 쓴다. 한 생에 서너 번이 알맞다.
/// </summary>
public class MolakMark : MonoBehaviour
{
    static MolakMark inst;
    const string Var = "lifemarks";
    const int Keep = 6;

    string line = "", sub = "";
    float t0 = -100f;
    const float Dur = 3.2f;
    GUIStyle sMain, sSub, sSeal; Font font; float styleU = -1f;
    static Texture2D px;

    public static List<string> Marks ()
    {
        var s = MolakStats.Str(Var);
        var list = new List<string>();
        if (string.IsNullOrEmpty(s) || s == "none") return list;
        foreach (var p in s.Split('|')) if (p.Length > 0) list.Add(p);
        return list;
    }

    public static void Record (string text)
    {
        var list = Marks();
        list.Add(text.Replace("|", " "));
        while (list.Count > Keep) list.RemoveAt(0);
        MolakStats.SetStr(Var, string.Join("|", list));
    }

    public static void Show (string text, string note)
    {
        if (inst == null) { var go = new GameObject("MolakMark"); DontDestroyOnLoad(go); inst = go.AddComponent<MolakMark>(); }
        inst.line = text; inst.sub = string.IsNullOrEmpty(note) ? "이 생이 여기서 갈린다." : note;
        inst.t0 = Time.realtimeSinceStartup;
    }

    void OnGUI ()
    {
        float e = Time.realtimeSinceStartup - t0;
        if (e < 0f || e > Dur) return;
        if (MolakBag.IsOpen || MolakMenu.IsOpen) return;   // 행낭·메뉴를 펼친 동안엔 가리지 않는다
        GUI.depth = -2500;
        if (px == null) { px = new Texture2D(1, 1); px.SetPixel(0, 0, Color.white); px.Apply(); }
        float w = Screen.width, h = Screen.height, u = Mathf.Min(w / 1080f, h / 1920f);
        if (sMain == null || !Mathf.Approximately(styleU, u))
        {
            if (font == null) font = Resources.Load<Font>("GowunBatang");
            sMain = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(38 * u), alignment = TextAnchor.MiddleLeft, richText = true, wordWrap = true, clipping = TextClipping.Overflow };
            sSub = new GUIStyle(sMain) { fontSize = Mathf.RoundToInt(27 * u) };
            sSeal = new GUIStyle(sMain) { fontSize = Mathf.RoundToInt(46 * u), alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            if (font != null) { sMain.font = font; sSub.font = font; sSeal.font = font; }
            styleU = u;
        }
        // 0.35초 번지고, 머물다, 0.7초 사라진다. 번질 때 살짝 아래로 내려앉는다.
        float a = e < 0.35f ? e / 0.35f : e > Dur - 0.7f ? (Dur - e) / 0.7f : 1f;
        a = Mathf.Clamp01(a);
        float drop = (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e / 0.35f))) * -14f * u;
        float bw = 960 * u, bh = 160 * u, bx = (w - bw) / 2f, by = h * 0.17f + drop;
        var g0 = GUI.color;
        // 먹띠(가운데 짙고 양끝 옅게)
        for (int i = 0; i < 12; i++)
        {
            float f = i / 11f, edge = 1f - Mathf.Abs(f - 0.5f) * 2f;
            GUI.color = new Color(0.04f, 0.03f, 0.03f, a * (0.55f + 0.4f * Mathf.Sqrt(edge)));
            GUI.DrawTexture(new Rect(bx + bw * f - bw / 22f, by, bw / 11f + 1f, bh), px);
        }
        GUI.color = new Color(0.75f, 0.2f, 0.18f, a * 0.85f);
        GUI.DrawTexture(new Rect(bx + 40 * u, by + bh - 3 * u, bw - 80 * u, 2 * u), px);
        // 붉은 인장 岐
        var seal = new Rect(bx + 46 * u, by + (bh - 92 * u) / 2f, 92 * u, 92 * u);
        GUI.color = new Color(0.72f, 0.14f, 0.12f, a * 0.95f); GUI.DrawTexture(seal, px);
        GUI.color = new Color(0.05f, 0.03f, 0.03f, a * 0.9f);
        GUI.DrawTexture(new Rect(seal.x + 6 * u, seal.y + 6 * u, seal.width - 12 * u, seal.height - 12 * u), px);
        GUI.color = g0;
        sSeal.normal.textColor = new Color(0.86f, 0.24f, 0.2f, a);
        GUI.Label(seal, "岐", sSeal);
        sMain.normal.textColor = new Color(0.95f, 0.9f, 0.8f, a);
        sSub.normal.textColor = new Color(0.9f, 0.76f, 0.66f, a);
        float tx = seal.xMax + 30 * u, tw = bx + bw - 40 * u - tx;
        GUI.Label(new Rect(tx, by + 22 * u, tw, 64 * u), line, sMain);
        GUI.Label(new Rect(tx, by + 90 * u, tw, 48 * u), "— " + sub, sSub);
    }
}

/// <summary>@mark text:"소아를 베었다" [note:"그녀의 두 번째 검은 끝내 오지 않았다."]
/// 갈림길 인장을 띄우고 이번 생의 갈림길로 적는다(대본은 멈추지 않는다). 읽은 글 넘기기 중에는 적기만 한다.</summary>
[Alias("mark")]
public class MarkCommand : Command
{
    public StringParameter Text;
    public StringParameter Note;

#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        if (!Assigned(Text) || string.IsNullOrEmpty(Text.Value)) return;
        MolakMark.Record(Text.Value);
        var sp = Engine.GetService<IScriptPlayer>();
        if (sp != null && sp.Skipping) return;
        MolakMark.Show(Text.Value, Assigned(Note) ? Note.Value : null);
        MolakSfx.Play("tap_brush", 0.8f, 0.82f);
    }
}
