using System.Collections.Generic;
using Naninovel;
using UnityEngine;

/// <summary>
/// 0.4.56 광란(狂亂) — 지키지 못한 사람·당한 배신을 안고 눈을 뜬 아침의 연출.
/// 화면 가득 그 사람의 말·이름·장면의 조각이 아무 데서나 번쩍였다 사라지고, 붉은 기운이 심장 박동에 맞춰 조여 온다.
/// 같은 사람을 잃은 횟수(g_gr_*)가 쌓일수록 더 길고 더 빽빽하고 더 붉다. 끝에는 그 이름 하나가 가운데에 남는다.
/// 0.8초가 지나면 화면을 눌러 끝낼 수 있다. '빨리' 중에는 짧게 지나간다.
/// </summary>
public class MolakFrenzy : MonoBehaviour
{
    static MolakFrenzy inst;
    public static bool Playing => inst != null && inst.t >= 0f;

    class Shard { public string Text; public Vector2 Pos; public float Size, Rot, Born, Life; public bool Red; }
    readonly List<Shard> shards = new List<Shard>();
    string[] words = new string[0]; string center = "";
    float t = -1f, dur, nextShard, nextBeat; int level;
    GUIStyle st; float builtU = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot ()
    {
        if (inst) return;
        var go = new GameObject("MolakFrenzy"); DontDestroyOnLoad(go);
        inst = go.AddComponent<MolakFrenzy>();
    }

    public static void Play (string[] ws, string centerWord, int lvl, bool quick)
    {
        if (inst == null || ws == null || ws.Length == 0) return;
        inst.words = ws; inst.center = centerWord ?? ""; inst.level = Mathf.Clamp(lvl, 0, 3);
        inst.dur = quick ? 0.7f : 2.6f + 0.7f * inst.level;
        inst.t = 0f; inst.nextShard = 0f; inst.nextBeat = 0.05f; inst.shards.Clear();
        MolakSfx.Heart();
    }

    void Update ()
    {
        if (t < 0f) return;
        float dt = Time.unscaledDeltaTime; t += dt;
        float gap = Mathf.Lerp(0.16f, 0.05f, Mathf.Clamp01(t / dur)) / (1f + 0.35f * level);   // 갈수록 빽빽하게
        while (t >= nextShard && t < dur)
        {
            nextShard += gap * Random.Range(0.6f, 1.4f);
            var s = new Shard {
                Text = words[Random.Range(0, words.Length)],
                Pos = new Vector2(Random.Range(0.06f, 0.94f), Random.Range(0.08f, 0.92f)),
                Size = Random.Range(34f, 70f + 30f * level) * (Random.value < 0.12f ? 2.2f : 1f),
                Rot = Random.Range(-24f, 24f), Born = t, Life = Random.Range(0.28f, 0.7f), Red = Random.value < 0.35f + 0.12f * level };
            shards.Add(s);
        }
        while (t >= nextBeat && t < dur + 0.6f)
        {   // 심장 박동: 갈수록 빨라진다
            nextBeat += Mathf.Lerp(0.62f, 0.32f, Mathf.Clamp01(t / dur));
            MolakVfx.Shake(8f + 5f * level);
            if (Random.value < 0.5f) MolakSfx.Heart();
        }
        shards.RemoveAll(s => t - s.Born > s.Life);
        if (t > dur + 1.1f) { t = -1f; shards.Clear(); }
    }

    void OnGUI ()
    {
        if (t < 0f) return;
        float u = MolakGui.U, w = Screen.width, h = Screen.height;
        if (st == null || !Mathf.Approximately(builtU, u)) { builtU = u; st = MolakGui.Label(48, u, TextAnchor.MiddleCenter, false); }
        GUI.depth = MolakGui.DepthBlackout + 5;
        MolakInputGuard.Guard(new Rect(0, 0, w, h));
        var g0 = GUI.color; var m0 = GUI.matrix;
        float fadeOut = t > dur + 0.5f ? Mathf.Clamp01(1f - (t - dur - 0.5f) / 0.6f) : 1f;
        float beat = 0.5f + 0.5f * Mathf.Sin(t * Mathf.Lerp(9f, 17f, Mathf.Clamp01(t / dur)));
        // 먹빛 바탕 + 박동하는 붉은 기운
        GUI.color = new Color(0.03f, 0.015f, 0.012f, 0.93f * fadeOut); GUI.DrawTexture(new Rect(0, 0, w, h), MolakGui.Px);
        GUI.color = new Color(0.55f, 0.05f, 0.03f, (0.10f + 0.16f * beat + 0.06f * level) * fadeOut); GUI.DrawTexture(new Rect(0, 0, w, h), MolakGui.Px);
        // 가장자리를 조이는 붉은 띠(시야가 좁아진다)
        float edge = Mathf.Lerp(0.04f, 0.16f, Mathf.Clamp01(t / dur)) * Mathf.Min(w, h) * (0.85f + 0.3f * beat);
        GUI.color = new Color(0.32f, 0.02f, 0.02f, 0.55f * fadeOut);
        GUI.DrawTexture(new Rect(0, 0, w, edge), MolakGui.Px); GUI.DrawTexture(new Rect(0, h - edge, w, edge), MolakGui.Px);
        GUI.DrawTexture(new Rect(0, 0, edge, h), MolakGui.Px); GUI.DrawTexture(new Rect(w - edge, 0, edge, h), MolakGui.Px);
        GUI.color = Color.white;   // 글자 색은 스타일로만 — 띠 색이 남아 있으면 글자까지 어둡게 물든다
        // 조각들
        foreach (var s in shards)
        {
            float k = (t - s.Born) / s.Life;
            float a = k < 0.15f ? k / 0.15f : 1f - Mathf.Clamp01((k - 0.5f) / 0.5f);
            var p = new Vector2(s.Pos.x * w, s.Pos.y * h) + Random.insideUnitCircle * (2f + 3f * level) * u;   // 떨린다
            st.fontSize = Mathf.RoundToInt(s.Size * u);
            var size = st.CalcSize(new GUIContent(s.Text));
            GUI.matrix = m0; GUIUtility.RotateAroundPivot(s.Rot, p);
            var r = new Rect(p.x - size.x / 2f, p.y - size.y / 2f, size.x, size.y);
            if (s.Red) { MolakGui.Ink(st, new Color(0.62f, 0.06f, 0.04f, 0.5f * a * fadeOut)); GUI.Label(new Rect(r.x + 4f * u, r.y + 2f * u, r.width, r.height), s.Text, st); }   // 번진 핏자국
            MolakGui.Ink(st, s.Red ? new Color(0.86f, 0.16f, 0.1f, a * fadeOut) : new Color(0.92f, 0.88f, 0.8f, a * fadeOut));
            GUI.Label(r, s.Text, st);
        }
        GUI.matrix = m0;
        // 끝: 그 이름 하나
        if (t > dur * 0.82f && center.Length > 0)
        {
            float ca = Mathf.Clamp01((t - dur * 0.82f) / 0.25f) * fadeOut;
            st.fontSize = Mathf.RoundToInt((110f + 20f * level) * u);
            var jit = Random.insideUnitCircle * (t < dur ? 6f : 1.5f) * u;
            var cr = new Rect(jit.x, h * 0.42f + jit.y, w, 200f * u);
            MolakGui.Ink(st, new Color(0.55f, 0.04f, 0.03f, 0.7f * ca)); GUI.Label(new Rect(cr.x + 6f * u, cr.y + 4f * u, cr.width, cr.height), center, st);
            MolakGui.Ink(st, new Color(0.95f, 0.9f, 0.82f, ca)); GUI.Label(cr, center, st);
        }
        GUI.color = g0;
        // 0.8초 뒤부터 눌러서 끝낸다
        if (GUI.Button(new Rect(0, 0, w, h), GUIContent.none, GUIStyle.none) && t > 0.8f && t < dur + 0.5f) { t = dur + 0.5f; shards.Clear(); }
    }
}

/// <summary>@frenzy words:"늦었다|소아|왜" center:"소아" level:g_gr_soa — 광란 연출이 끝날 때까지 기다린다. level 은 전역 횟수 변수 이름(1회째 = 0단).</summary>
[Alias("frenzy")]
public class FrenzyCommand : Command
{
    [RequiredParameter] public StringParameter Words;
    public StringParameter Center;
    public StringParameter Level;

    public override async Awaitable Execute (ExecutionContext ctx)
    {
        int lvl = 0;
        if (Assigned(Level)) lvl = Mathf.Clamp(Mathf.RoundToInt(MolakStats.Get(Level.Value)) - 1, 0, 3);
        var sp = Engine.GetService<IScriptPlayer>();
        bool quick = sp != null && sp.Skipping;
        MolakFrenzy.Play(Words.Value.Split('|'), Assigned(Center) ? Center.Value : "", lvl, quick);
        while (MolakFrenzy.Playing) await Awaitable.NextFrameAsync();
    }
}
