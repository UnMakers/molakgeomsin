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
    /// <summary>0.4.27: 줄은 눌러야 넘어간다. 자동 시험(소크·대본 걷기) 중에만 hold 초 뒤 저절로 넘긴다.</summary>
    public static bool Auto;

    string[] lines = new string[0];
    float hold = 1.8f;
    bool red, busy, stack;
    int idx;
    float phaseT;      // 지금 단계가 시작된 시각(실시간)
    int phase;         // 0 먹이 덮임, 1 줄 떠오름·머묾, 2 줄 짐, 3 먹이 걷힘, 4 글자가 지직이며 뒤집힘(turn)
    /// <summary>0.4.86 turn: 마지막 줄(「숨이 멎는다.」)이 떠 머문 뒤, 글자가 지직이며 찢기고 이 문장으로 뒤집힌다. 핏빛 먹은 검은 먹으로 식는다.
    /// 플레이어가 수없이 본 죽음 화면을 한 번 배신하는 자리 — 대본에서 정해 둔 한 자리에만 쓴다.</summary>
    string turn; bool turned;
    const float TurnDur = 1.5f;
    float coverA;      // 먹 농도(걷힐 때 이어 쓰려고 기억)
    GUIStyle style; float styleU = -1f;
    /// <summary>stack:true — 첫 줄을 머리로 두고, 다음 줄들이 그 아래로 차례로 쌓인다(지우지 않음). 마지막 줄까지 머문 뒤 한꺼번에 진다.</summary>
    System.Action onDone;   // 0.4.40: 줄이 다 지고 먹이 걷히기 시작할 때 한 번(타이틀의 "눈을 뜬다." → 게임 시작)

    public static void Play (string[] lines, float hold, bool red, bool stack = false, System.Action onDone = null, string turn = null)
    {
        if (inst == null) { var go = new GameObject("MolakBlackout"); DontDestroyOnLoad(go); inst = go.AddComponent<MolakBlackout>(); }
        float now = Time.realtimeSinceStartup;
        float c = inst.CoverNow(now);   // 먹이 걷히던 중이면 그 농도에서 다시 덮는다(깜빡임 없이 이어지게)
        inst.lines = lines ?? new string[0];
        inst.hold = Mathf.Max(0.6f, hold);
        inst.red = red; inst.stack = stack;
        inst.idx = 0; inst.phase = 0; inst.phaseT = now - 0.9f * c; inst.busy = true; inst.onDone = onDone;
        inst.turn = string.IsNullOrEmpty(turn) || stack ? null : turn; inst.turned = false;
    }

    float CoverNow (float t)
    {
        float e = t - phaseT;
        if (phase == 0) return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e / 0.9f));
        if (phase == 3) return 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(e / 0.9f));
        return 1f;
    }

    static bool Pressed () { return MolakGui.TapDown(); }

    void Update ()
    {
        if (phase == 3 && !busy) return;
        float t = Time.realtimeSinceStartup, e = t - phaseT;
        switch (phase)
        {
            case 0: if (e >= 0.9f) { Next(1, t); } break;
            case 1:
                if (idx >= lines.Length) { Next(3, t); Finish(); break; }
                // 홑줄: 떠오른 뒤 눌러야 넘어간다. 쌓기: 고른 것들은 저절로 차례로 쌓이고(누르면 바로 다음), 다 쌓인 뒤엔 눌러야 닫힌다.
                bool last = !stack || idx >= lines.Length - 1;
                bool timeUp = e >= 0.6f + hold;
                bool tap = e >= 0.4f && Pressed();
                bool auto = Auto || AutoPlaying();   // 자동 진행을 켜 둔 플레이어에게는 예전처럼 저절로 넘긴다
                // 뒤집힘: 마지막 줄이 머문 뒤(누르든 기다리든) 지게 두지 않고 찢는다. 지직이는 동안은 누름을 받지 않는다.
                if (turn != null && !turned && idx == lines.Length - 1 && (timeUp || tap)) { Next(4, t); MolakSfx.CurseBreath(); break; }
                if (stack && !last) { if (timeUp || tap) { idx++; Next(1, t); } }
                else if (tap || (auto && timeUp)) Next(2, t);
                break;
            case 2:
                if (e >= (stack ? 0.7f : 0.4f)) { idx = stack ? lines.Length : idx + 1; Next(idx < lines.Length ? 1 : 3, t); if (phase == 3) Finish(); }
                break;
            case 4:
                // 다 찢기면 새 문장이 박힌다. 떠오르는 0.6초는 건너뛰고 곧장 다 보인 채로 시작한다.
                if (e >= TurnDur) { lines[idx] = turn; turned = true; red = false; Next(1, t - 0.6f); MolakSfx.Heart(); }
                break;
        }
    }

    void Next (int p, float t) { phase = p; phaseT = t; }

    void Finish ()
    {
        busy = false;
        var cb = onDone; onDone = null;
        if (cb != null) { try { cb(); } catch (System.Exception e) { Debug.LogException(e); } }
    }

    static bool AutoPlaying ()
    {
        try { var sp = Engine.GetService<IScriptPlayer>(); return sp != null && sp.AutoPlaying; } catch { return false; }
    }

    void OnGUI ()
    {
        float t = Time.realtimeSinceStartup, e = t - phaseT;
        float cover = CoverNow(t);
        if (phase == 3 && cover <= 0.001f) return;
        GUI.depth = MolakGui.DepthBlackout;
        
        float w = Screen.width, h = Screen.height;
        if (busy)
        {
            MolakInputGuard.Guard(new Rect(0, 0, w, h));
            // 먹이 덮인 동안 뒤에 깔린 ☰·행낭 같은 IMGUI 단추가 눌리지 않게 마우스·터치 이벤트를 여기서 먹는다(넘기기는 입력 시스템으로 따로 읽는다)
            var ev = Event.current;
            if (ev != null && (ev.type == EventType.MouseDown || ev.type == EventType.MouseUp || ev.type == EventType.MouseDrag)) ev.Use();
        }
        var g0 = GUI.color;
        var ink = red ? MolakGui.InkBlood : MolakGui.InkBlack;
        if (phase == 4) ink = Color.Lerp(MolakGui.InkBlood, MolakGui.InkBlack, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((e - 0.55f) / 0.8f)));   // 핏빛이 식는다
        GUI.color = new Color(ink.r, ink.g, ink.b, cover); GUI.DrawTexture(new Rect(0, 0, w, h), MolakGui.Px);
        GUI.color = g0;
        if (phase == 4 && idx < lines.Length)
        {
            float u4 = MolakGui.U;
            if (style == null || !Mathf.Approximately(styleU, u4)) { style = MolakGui.Label(46, u4, TextAnchor.MiddleCenter); styleU = u4; }
            DrawTurn(w, h, u4, e);
        }
        if ((phase == 1 || phase == 2) && idx < lines.Length)
        {
            float u = MolakGui.U;
            if (style == null || !Mathf.Approximately(styleU, u))
            {
                style = MolakGui.Label(46, u, TextAnchor.MiddleCenter);
                styleU = u;
            }
            float a = phase == 1 ? Mathf.Clamp01(e / 0.6f) : 1f - Mathf.Clamp01(e / (stack ? 0.7f : 0.4f));
            var col = red ? new Color(0.92f, 0.72f, 0.66f, a) : new Color(MolakGui.InkOnBlack.r, MolakGui.InkOnBlack.g, MolakGui.InkOnBlack.b, a);
            // 0.4.37: 쌓기 그림이 '▼ 표시' 조건의 else 에 매달려 있어서, 홑줄 모드에서도 ▼ 가 안 보이는 순간(떠오를 때·질 때)마다
            //         앞 줄들이 금빛 머리줄·붉은 밑줄과 함께 겹쳐 그려졌다(글자가 두 번 뜨던 것). 홑줄/쌓기를 먼저 가른다.
            if (!stack)
            {
                float dx = 0f;
                if (turned)
                {
                    col = new Color(0.95f, 0.88f, 0.72f, a);   // 뒤집힌 문장은 핏빛이 아니라 먹 위의 금빛
                    float sk = Mathf.Clamp01((0.95f - e) / 0.35f);   // 박히는 순간 한 번 떨린다
                    dx = Mathf.Sin(e * 90f) * 6f * u * sk;
                }
                MolakGui.Ink(style, col);
                GUI.Label(new Rect(w * 0.08f + dx, h * 0.42f, w * 0.84f, 170 * u), lines[idx], style);
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
                    MolakGui.Ink(style, i == 0 ? new Color(0.79f, 0.64f, 0.3f, ai) : new Color(0.9f, 0.85f, 0.76f, ai));
                    GUI.Label(new Rect(w * 0.08f, y, w * 0.84f, 80 * u), lines[i], style);
                    if (i == 0)
                    {
                        GUI.color = new Color(0.72f, 0.2f, 0.18f, ai * 0.8f);
                        GUI.DrawTexture(new Rect(w * 0.5f - 90 * u, y + 82 * u, 180 * u, 2 * u), MolakGui.Px);
                        GUI.color = g0;
                    }
                }
            }
            // 누르라는 표시: 마지막 줄까지 다 떠오르면 아래쪽에 ▼ 가 천천히 숨 쉰다
            if (phase == 1 && e >= 0.6f && (!stack || idx >= lines.Length - 1))
            {
                float pa = Mathf.Clamp01((e - 0.6f) / 0.5f) * (0.35f + 0.3f * Mathf.Sin((e - 0.6f) * 3.2f));
                var hs = new GUIStyle(style) { fontSize = Mathf.RoundToInt(30 * u) };
                MolakGui.Ink(hs, red ? new Color(0.92f, 0.72f, 0.66f, pa) : new Color(0.88f, 0.82f, 0.72f, pa));
                GUI.Label(new Rect(0, h * 0.80f, w, 60 * u), "▼", hs);
            }
        }
    }

    static float Hash (int a, int b) { uint x = (uint)(a * 73856093) ^ (uint)(b * 19349663) ^ 0x9E3779B9u; x ^= x >> 13; x *= 0x5bd1e995; x ^= x >> 15; return (x & 0xFFFF) / 65535f; }
    const string Noise = "숨멎아직쓰러█▓▒░ㅁㅅㅈ#";
    /// <summary>글자를 p 확률로 깨진 획으로 바꾼다(빈칸·문장부호는 둔다)</summary>
    static string Corrupt (string s, int tick, float p)
    {
        if (p <= 0.001f) return s;
        var sb = new System.Text.StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            char ch = s[i];
            if (char.IsWhiteSpace(ch) || char.IsPunctuation(ch) || Hash(tick * 31 + i, 3) >= p) sb.Append(ch);
            else sb.Append(Noise[(int)(Hash(tick, i + 9) * Noise.Length) % Noise.Length]);
        }
        return sb.ToString();
    }

    /// <summary>뒤집힘 그리기(TurnDur 초): 한 번 움찔 → 멎은 듯 고요 → 가로로 찢기며 색이 갈라지고 획이 깨진다 → 뒤 문장이 틈으로 비친다 → 박힌다.</summary>
    void DrawTurn (float w, float h, float u, float e)
    {
        var rect = new Rect(w * 0.08f, h * 0.42f, w * 0.84f, 170 * u);
        int tick = Mathf.FloorToInt(e * 24f);   // 1/24초마다 무늬가 바뀐다
        float amp;
        if (e < 0.12f) amp = 0.55f;             // 움찔
        else if (e < 0.42f) amp = 0f;           // 고요(본 게 맞나 싶은 사이)
        else amp = Mathf.Clamp01((e - 0.42f) / 0.45f) * (1f - Mathf.Clamp01((e - 1.28f) / 0.2f));
        float mix = Mathf.Clamp01((e - 0.85f) / 0.4f);
        bool showTo = e >= 1.28f || Hash(tick, 7) < mix;
        string txt = Corrupt(showTo ? turn : lines[idx], tick, amp * (showTo ? 1f - mix : 1f) * 0.5f);
        var baseC = showTo ? new Color(0.95f, 0.88f, 0.72f, 1f) : new Color(0.92f, 0.72f, 0.66f, 1f);
        const int Strips = 7;
        float sh = rect.height / Strips, pad = 70f * u;
        for (int i = 0; i < Strips; i++)
        {
            float off = (Hash(tick, i) - 0.5f) * 2f * amp * 48f * u * (Hash(tick, i + 20) < 0.45f ? 1f : 0.15f);
            GUI.BeginGroup(new Rect(rect.x - pad, rect.y + i * sh, rect.width + pad * 2f, sh + 1f));
            var inner = new Rect(pad + off, -i * sh, rect.width, rect.height);
            if (amp > 0.05f)
            {
                MolakGui.Ink(style, new Color(0.85f, 0.12f, 0.10f, 0.55f * amp)); GUI.Label(new Rect(inner.x - 7f * u * amp, inner.y, inner.width, inner.height), txt, style);
                MolakGui.Ink(style, new Color(0.75f, 0.90f, 0.95f, 0.35f * amp)); GUI.Label(new Rect(inner.x + 6f * u * amp, inner.y, inner.width, inner.height), txt, style);
            }
            float a = (amp > 0.3f && Hash(tick, i + 40) < 0.12f) ? 0.2f : 1f;   // 띠 하나가 잠깐 비는 것
            MolakGui.Ink(style, new Color(baseC.r, baseC.g, baseC.b, a));
            GUI.Label(inner, txt, style);
            GUI.EndGroup();
        }
        // 화면을 가로지르는 잡음 줄
        if (amp > 0.1f)
            for (int k = 0; k < 5; k++)
            {
                if (Hash(tick, k + 60) > amp) continue;
                GUI.color = new Color(0.9f, 0.85f, 0.8f, 0.10f + 0.15f * Hash(tick, k + 90));
                GUI.DrawTexture(new Rect(0, Hash(tick, k + 80) * h, w, (1f + 3f * Hash(tick, k + 70)) * u), MolakGui.Px);
            }
        // 새 문장이 박히는 순간 흰 번쩍임
        float fl = Mathf.Clamp01(1f - Mathf.Abs(e - 1.32f) / 0.12f);
        if (fl > 0f) { GUI.color = new Color(1f, 0.96f, 0.9f, 0.2f * fl); GUI.DrawTexture(new Rect(0, 0, w, h), MolakGui.Px); }
        GUI.color = Color.white;
    }
}

/// <summary>@black text:"눈을 뜬다." — 먹이 화면을 덮고 가운데에 한 줄씩. 여러 줄은 | 로 나눈다.
/// hold:2.2 줄마다 머무는 초(기본 1.8), red:true 핏빛 먹. 끝날 때까지 대본을 멈춘다.
/// turn:"아직, 쓰러질 수 없다." — 마지막 줄이 머문 뒤 지직이며 이 문장으로 뒤집힌다(되감기지 않는 죽음 화면).</summary>
[Alias("black")]
public class BlackCommand : Command
{
    public StringParameter Text;
    public DecimalParameter Hold;
    public BooleanParameter Red;
    /// <summary>marks:true — 이번 생의 갈림길(@mark 로 적은 것)을 "이번 생에 고른 것" 아래로 한 줄씩 다시 읽는다. 적힌 게 없으면 건너뛴다.</summary>
    public BooleanParameter Marks;
    public StringParameter Turn;

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
        MolakBlackout.Play(lines, hold, Assigned(Red) && Red.Value, stackMode, null, Assigned(Turn) ? Turn.Value : null);
        // 눌러야 넘어가므로 시간 안전판은 자동 시험 때만 둔다
        float limit = MolakBlackout.Auto ? Time.realtimeSinceStartup + 2f + lines.Length * (hold + 1.2f) + (Assigned(Turn) ? 4f : 0f) : float.MaxValue;
        while (MolakBlackout.Busy && Time.realtimeSinceStartup < limit) await Awaitable.NextFrameAsync();
    }
}
