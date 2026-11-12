using Naninovel;
using UnityEngine;

/// <summary>
/// 0.4.24 먹이 덮는 순간: 화면이 검게 덮이고, 가운데에 한 줄씩 떠올랐다 진다(타이틀의 "눈을 뜬다."와 같은 결).
/// 의식이 꺼지는 순간, 되감기, 숨이 멎는 자리처럼 '한 박자 비워 두는' 데만 쓴다. 남발하면 힘이 빠진다.
/// 줄마다 hold 초가 지나거나 화면을 누르면 다음 줄로 넘어간다(눌러도 0.4초는 머문다). 끝나면 먹이 걷히며 대본이 이어진다.
/// 읽은 글 넘기기 중에는 건너뛴다.
/// </summary>
public class MolakBlackout : MonoBehaviour
{
    static MolakBlackout inst;
    public static bool Busy { get { return inst != null && inst.busy; } }

    string[] lines = new string[0];
    float hold = 1.8f;
    bool red, busy, stack;
    int idx;
    float phaseT;      // 지금 단계가 시작된 시각(실시간)
    int phase;         // 0 먹이 덮임, 1 줄 떠오름·머묾, 2 줄 짐, 3 먹이 걷힘
    float coverA;      // 먹 농도(걷힐 때 이어 쓰려고 기억)
    GUIStyle style; Font font; float styleU = -1f;
    static Texture2D px;

    /// <summary>stack:true — 첫 줄을 머리로 두고, 다음 줄들이 그 아래로 차례로 쌓인다(지우지 않음). 마지막 줄까지 머문 뒤 한꺼번에 진다.</summary>
    public static void Play (string[] lines, float hold, bool red, bool stack = false)
    {
        if (inst == null) { var go = new GameObject("MolakBlackout"); DontDestroyOnLoad(go); inst = go.AddComponent<MolakBlackout>(); }
        float now = Time.realtimeSinceStartup;
        float c = inst.CoverNow(now);   // 먹이 걷히던 중이면 그 농도에서 다시 덮는다(깜빡임 없이 이어지게)
        inst.lines = lines ?? new string[0];
        inst.hold = Mathf.Max(0.6f, hold);
        inst.red = red; inst.stack = stack;
        inst.idx = 0; inst.phase = 0; inst.phaseT = now - 0.9f * c; inst.busy = true;
    }

    float CoverNow (float t)
    {
        float e = t - phaseT;
        if (phase == 0) return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e / 0.9f));
        if (phase == 3) return 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e / 0.9f));
        return 1f;
    }

    static bool Pressed ()
    {
        var ts = UnityEngine.InputSystem.Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.wasPressedThisFrame) return true;
        var ms = UnityEngine.InputSystem.Mouse.current;
        if (ms != null && ms.leftButton.wasPressedThisFrame) return true;
        var kb = UnityEngine.InputSystem.Keyboard.current;
        return kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame);
    }

    void Update ()
    {
        if (phase == 3 && !busy) return;
        float t = Time.realtimeSinceStartup, e = t - phaseT;
        switch (phase)
        {
            case 0: if (e >= 0.9f) { Next(1, t); } break;
            case 1:
                if (idx >= lines.Length) { Next(3, t); busy = false; break; }
                if (e >= 0.6f + hold || (e >= 0.4f && Pressed()))
                {
                    if (stack && idx < lines.Length - 1) { idx++; Next(1, t); }
                    else Next(2, t);
                }
                break;
            case 2:
                if (e >= (stack ? 0.7f : 0.4f)) { idx = stack ? lines.Length : idx + 1; Next(idx < lines.Length ? 1 : 3, t); if (phase == 3) busy = false; }
                break;
        }
    }

    void Next (int p, float t) { phase = p; phaseT = t; }

    void OnGUI ()
    {
        float t = Time.realtimeSinceStartup, e = t - phaseT;
        float cover = CoverNow(t);
        if (phase == 3 && cover <= 0.001f) return;
        GUI.depth = -3000;
        if (px == null) { px = new Texture2D(1, 1); px.SetPixel(0, 0, Color.white); px.Apply(); }
        float w = Screen.width, h = Screen.height;
        if (busy) MolakInputGuard.Guard(new Rect(0, 0, w, h));
        var g0 = GUI.color;
        var ink = red ? new Color(0.10f, 0.015f, 0.012f, 1f) : new Color(0.02f, 0.018f, 0.016f, 1f);
        GUI.color = new Color(ink.r, ink.g, ink.b, cover); GUI.DrawTexture(new Rect(0, 0, w, h), px);
        GUI.color = g0;
        if ((phase == 1 || phase == 2) && idx < lines.Length)
        {
            float u = Mathf.Min(w / 1080f, h / 1920f);
            if (style == null || !Mathf.Approximately(styleU, u))
            {
                if (font == null) font = Resources.Load<Font>("GowunBatang");
                style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(46 * u), alignment = TextAnchor.MiddleCenter, richText = true, wordWrap = true, clipping = TextClipping.Overflow };
                if (font != null) style.font = font;
                styleU = u;
            }
            float a = phase == 1 ? Mathf.Clamp01(e / 0.6f) : 1f - Mathf.Clamp01(e / (stack ? 0.7f : 0.4f));
            var col = red ? new Color(0.92f, 0.72f, 0.66f, a) : new Color(0.88f, 0.82f, 0.72f, a);
            if (!stack)
            {
                style.normal.textColor = col;
                GUI.Label(new Rect(w * 0.08f, h * 0.42f, w * 0.84f, 170 * u), lines[idx], style);
            }
            else
            {
                // 머리 줄은 위쪽에 옅은 금빛으로, 고른 것들은 그 아래로 한 줄씩 쌓인다
                float all = phase == 2 ? a : 1f;
                int n = lines.Length;
                float top = h * 0.5f - (70f + (n - 1) * 84f) * u * 0.5f - 40f * u;
                for (int i = 0; i <= idx && i < n; i++)
                {
                    float ai = (i == idx && phase == 1) ? Mathf.Clamp01(e / 0.6f) : all;
                    float y = i == 0 ? top : top + 70f * u + (i - 1) * 84f * u + 30f * u;
                    style.normal.textColor = i == 0 ? new Color(0.79f, 0.64f, 0.3f, ai) : new Color(0.9f, 0.85f, 0.76f, ai);
                    GUI.Label(new Rect(w * 0.08f, y, w * 0.84f, 80 * u), lines[i], style);
                    if (i == 0)
                    {
                        GUI.color = new Color(0.72f, 0.2f, 0.18f, ai * 0.8f);
                        GUI.DrawTexture(new Rect(w * 0.5f - 90 * u, y + 82 * u, 180 * u, 2 * u), px);
                        GUI.color = g0;
                    }
                }
            }
        }
    }
}

/// <summary>@black text:"눈을 뜬다." — 먹이 화면을 덮고 가운데에 한 줄씩. 여러 줄은 | 로 나눈다.
/// hold:2.2 줄마다 머무는 초(기본 1.8), red:true 핏빛 먹. 끝날 때까지 대본을 멈춘다.</summary>
[Alias("black")]
public class BlackCommand : Command
{
    public StringParameter Text;
    public DecimalParameter Hold;
    public BooleanParameter Red;
    /// <summary>marks:true — 이번 생의 갈림길(@mark 로 적은 것)을 "이번 생에 고른 것" 아래로 한 줄씩 다시 읽는다. 적힌 게 없으면 건너뛴다.</summary>
    public BooleanParameter Marks;

    public override async Awaitable Execute (ExecutionContext ctx)
    {
        var sp = Engine.GetService<IScriptPlayer>();
        if (sp != null && sp.Skipping) return;
        var lines = (Assigned(Text) ? Text.Value : "").Split('|');
        float hold = Assigned(Hold) ? Hold.Value : 1.8f;
        bool stackMode = false;
        if (Assigned(Marks) && Marks.Value)
        {
            var m = MolakMark.Marks();
            if (m.Count == 0) return;
            for (int i = 0; i < m.Count; i++) if (!m[i].EndsWith(".")) m[i] += ".";
            m.Insert(0, Assigned(Text) ? Text.Value : "이번 생에 고른 것");
            lines = m.ToArray();
            if (!Assigned(Hold)) hold = 1.3f;
            stackMode = true;
        }
        MolakBlackout.Play(lines, hold, Assigned(Red) && Red.Value, stackMode);
        float limit = Time.realtimeSinceStartup + 2f + lines.Length * (hold + 1.2f);   // 안전판: 무슨 일이 있어도 대본은 이어진다
        while (MolakBlackout.Busy && Time.realtimeSinceStartup < limit) await Awaitable.NextFrameAsync();
    }
}
