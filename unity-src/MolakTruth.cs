using System.Collections.Generic;
using Naninovel;
using UnityEngine;

/// <summary>
/// 0.4.58 「그 밤의 진실」 — 풀다 만 줄기가 다음 판의 이유가 되게(언메이커: 계속 하고 싶게).
///  · 줄기(질문) 다섯. 줄기마다 빈칸(실마리) 서넛~여섯. 빈칸은 전역 앎(g_)이 켜지면 채워진다 — 회귀해도 남는다.
///  · 채워지지 않은 빈칸 중 맨 앞의 것만 '어디서'를 귀띔한다(방법은 말하지 않는다). 나머지는 「· · ·」.
///  · 죽은 뒤(MolakMemory.OnLifeEnd) 「풀다 만 실마리」 두 줄: 이번 생에 나아간 줄기 먼저, 없으면 시작한 줄기, 그것도 없으면 첫 실마리.
///  · 메뉴(☰) → 「그 밤의 진실」.
/// </summary>
public static class MolakTruth
{
    public struct Slot { public string Key, Name, Text, Hint; public Slot (string k, string n, string t, string h) { Key = k; Name = n; Text = t; Hint = h; } }
    public struct Thread { public string Title; public Slot[] Slots; public Thread (string t, Slot[] s) { Title = t; Slots = s; } }

    public static readonly Thread[] All = {
        new Thread("대사형은 왜 산문을 열었나", new[] {
            new Slot("g_clue_bukdu",   "북두 쇠패",       "척후의 허리춤에서 떨어진 쇠패. 대사형이 매일 아침 밟던 일곱 점.", "나흘째 밤, 이층으로 올라오는 척후 — 그 허리춤"),
            new Slot("g_know_jinbaek", "막내야",          "막내라고 부를 때만 그의 검이 반 치 늦다.",                         "혈랑대주와 칼을 맞대는 날"),
            new Slot("g_sig_jinbaek",  "북두낙월",        "일곱 칼끝 가운데 여섯이 거짓이다.",                                 "대사형의 필살을 한 번 받아 본다"),
            new Slot("g_jbtruth",      "산문을 연 까닭",   "열면 사매 하나는 산다. 그 사매는 산사에서 머리를 깎았다.",           "복수의 끝 — 그가 입을 여는 자리"),
        }),
        new Thread("누가 이 문양을 새겼나", new[] {
            new Slot("g_oldmem1",   "한 검을 둘이",       "폭포 아래, 왼손은 사매 몫이었다.",                  "잠든 사이 남의 꿈을 꾼다 — 폭포 소리가 나는 꿈"),
            new Slot("g_oldmem2",   "찢어진 검보",        "사(邪)를 아는 검이 홀로면 사에 먹힌다.",            "그 꿈은 이어진다 — 빗속으로 반쪽이 떠난다"),
            new Slot("g_oldmem3",   "늦게 닿은 암자",     "제단 앞의 사태, 무릎에 마른 연잎.",                  "꿈의 셋째 밤 — 늦게 닿은 사람이 본 것"),
            new Slot("g_oldlid",    "뚜껑 안쪽",          "'왼쪽은 비워 두었소. 늦게 와도 되오.'",              "암자의 함 — 노인의 반쪽으로는 열리지 않는다"),
            new Slot("g_oldmem4",   "첫 열매",            "잿더미의 아이가 제 손으로 문양의 반을 도려냈다.",    "꿈의 마지막 — 잿더미에 선 아이"),
            new Slot("g_know_ripe", "검을 뽑지 않은 아이", "노인 앞에서 검을 뽑지 않고 반 각을 선 사람.",        "열매가 익는 날, 노인 앞에서"),
        }),
        new Thread("그 밤, 맹은 어디 있었나", new[] {
            new Slot("g_know_maeng",     "그 밤의 수레", "원군은 남궁의 피난 수레 열두 대를 지키고 있었다.", "무림맹 밀사가 운락에 온다 — 그의 말을 끝까지 듣는다"),
            new Slot("g_maenglate",      "반 시진",      "백리휘는 피가 다 식은 새벽에 온다.",                "혈랑대가 객잔을 덮치는 밤, 원군을 기다려 본다"),
            new Slot("g_know_maengsell", "맹의 저울",    "청운 막내 하나에 맹 잔당 일곱.",                    "밀사와의 거래가 깊어진 뒤"),
        }),
        new Thread("적운의 우물", new[] {
            new Slot("g_know_soa",  "적운의 쌍검",   "적운의 검은 두 번 오지 않는다. 한 번에 둘이 온다.", "소아와 칼을 맞댄다"),
            new Slot("g_soadied",   "그녀가 죽은 밤", "등 뒤를 맡긴다던 사람이 먼저 갔다.",               "그녀를 지키지 못한 생"),
            new Slot("g_soaname",   "그 이름",       "연(蓮). 사부만 그리 불렀다.",                       "그녀가 칼을 겨누는 밤에 듣는다"),
            new Slot("g_redverse",  "적운의 구결",   "우물 속에서 백 번 들은 열두 줄.",                   "그 밤 그녀와 함께 살아서 지붕으로 달아난다"),
        }),
        new Thread("운락 사람들이 숨긴 것", new[] {
            new Slot("g_know_hajin",     "관아 대문 앞",   "하진무는 대문을 끝내 두드리지 못했다.",            "하진무가 밤에 나가는 날, 뒤를 밟는다"),
            new Slot("g_know_jangsell",  "판 소식",        "뒷문에 서 있던 그 사람이 이 방을 판다. 서른 냥에.", "쥐눈이 장이 뒷문에 서던 무렵"),
            new Slot("g_know_hao",       "담보 칸의 딸",   "장의 딸, 열 살. 징세조 장부의 담보 칸.",           "장의 사정을 아는 생에 그를 다시 만난다"),
            new Slot("g_know_gwak",      "푸른 창날",      "곽산의 아내가 피를 토한다. 외상이 막혔다.",        "곽산과 다시 겨루는 판, 창날의 빛"),
            new Slot("g_herbdied",       "백초당이 타던 밤","교납 장부의 무게 칸.",                            "할멈이 교의 약재를 거절하는 생"),
            new Slot("g_know_yong",      "회목채의 누이",  "용개의 누이와 다섯 살 조카가 회목채에 있다.",     "회목촌의 매복"),
        }),
    };

    static bool Has (string k) { return MolakStats.Get(k) > 0f; }
    public static int Known (Thread t) { int n = 0; foreach (var s in t.Slots) if (Has(s.Key)) n++; return n; }
    public static int NextIndex (Thread t) { for (int i = 0; i < t.Slots.Length; i++) if (!Has(t.Slots[i].Key)) return i; return -1; }
    public static int TotalKnown () { int n = 0; foreach (var t in All) n += Known(t); return n; }
    public static int TotalSlots () { int n = 0; foreach (var t in All) n += t.Slots.Length; return n; }

    /// <summary>회차 시작 때 줄기마다 아는 수를 적어 둔다(이번 생에 나아간 줄기를 가리려고).</summary>
    public static void Snapshot ()
    {
        var parts = new List<string>();
        foreach (var t in All) parts.Add(Known(t).ToString());
        MolakStats.SetStr("truthsnap", string.Join(",", parts.ToArray()));
    }

    /// <summary>죽은 뒤 「풀다 만 실마리」 줄들(최대 2).</summary>
    public static List<string> Leads ()
    {
        var snap = (MolakStats.Str("truthsnap") ?? "").Split(',');
        var advanced = new List<int>(); var started = new List<int>();
        for (int i = 0; i < All.Length; i++)
        {
            int k = Known(All[i]), was = 0;
            if (i < snap.Length) int.TryParse(snap[i], out was);
            if (NextIndex(All[i]) < 0) continue;              // 다 푼 줄기
            if (k > was) advanced.Add(i); else if (k > 0) started.Add(i);
        }
        var pick = new List<int>(advanced);
        foreach (var i in started) if (pick.Count < 2) pick.Add(i);
        if (pick.Count == 0) { pick.Add(0); if (All.Length > 1) pick.Add(1); }
        var lines = new List<string>();
        foreach (var i in pick)
        {
            if (lines.Count >= 2) break;
            var t = All[i]; int nx = NextIndex(t); if (nx < 0) continue;
            lines.Add("「" + t.Title + "」 " + Known(t) + "/" + t.Slots.Length + (advanced.Contains(i) ? " <color=#C9A227>(이번 생에 풀린 것이 있다)</color>" : "") +
                      "<br><color=#8A7A66>　다음 실마리 — " + t.Slots[nx].Hint + "</color>");
        }
        return lines;
    }
}

/// <summary>메뉴의 「그 밤의 진실」 판.</summary>
public class MolakTruthView : MonoBehaviour
{
    static MolakTruthView inst;
    bool open; int detail = -1; float openT;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot ()
    {
        if (inst) return;
        var go = new GameObject("MolakTruthView"); DontDestroyOnLoad(go);
        inst = go.AddComponent<MolakTruthView>();
    }
    public static bool IsOpen => inst != null && inst.open;
    public static void Open () { if (inst != null) { inst.open = true; inst.detail = -1; inst.openT = 0f; } }

    GUIStyle sHead, sRow, sSub, sBtn, sBody; float builtU = -1f;
    void Styles (float u)
    {
        if (sHead != null && Mathf.Approximately(builtU, u)) return;
        builtU = u;
        sHead = MolakGui.Label(44, u, TextAnchor.MiddleCenter, false); sHead.font = MolakTypography.Heading(sHead.font); sHead.richText = true;
        sRow = MolakGui.Label(32, u, TextAnchor.UpperLeft, true); sRow.richText = true;
        sSub = MolakGui.Label(24, u, TextAnchor.UpperLeft, true); sSub.richText = true;
        sBody = MolakGui.Label(28, u, TextAnchor.UpperLeft, true); sBody.richText = true;
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
        var ink = new Color(0.93f, 0.89f, 0.81f, 1f); var gold = new Color(0.85f, 0.68f, 0.3f, 1f); var dim = new Color(0.62f, 0.58f, 0.52f, 1f);
        GUI.color = new Color(0.04f, 0.035f, 0.03f, 0.97f); GUI.DrawTexture(new Rect(0, 0, w, h), MolakGui.Px); GUI.color = Color.white;
        float cw = Mathf.Min(w * 0.88f, 900f * u), cx = (w - cw) / 2f;
        float y = Mathf.Max(60f * u, h * 0.06f);
        bool clicked = false;

        if (detail < 0)
        {
            MolakGui.Ink(sHead, gold);
            GUI.Label(new Rect(cx, y, cw, 64f * u), "그 밤의 진실", sHead); y += 70f * u;
            MolakGui.Ink(sSub, dim);
            GUI.Label(new Rect(cx, y, cw, 40f * u), "죽어도 남는 것으로 빈칸을 채운다 · " + MolakTruth.TotalKnown() + " / " + MolakTruth.TotalSlots(), sSub);
            y += 56f * u;
            for (int i = 0; i < MolakTruth.All.Length; i++)
            {
                var t = MolakTruth.All[i]; int k = MolakTruth.Known(t), n = t.Slots.Length, nx = MolakTruth.NextIndex(t);
                var r = new Rect(cx, y, cw, 150f * u);
                GUI.color = new Color(1f, 1f, 1f, 0.07f); GUI.DrawTexture(r, MolakGui.Px); GUI.color = Color.white;
                MolakGui.Ink(sRow, k > 0 ? ink : dim);
                GUI.Label(new Rect(r.x + 26f * u, r.y + 16f * u, r.width - 52f * u, 44f * u), t.Title, sRow);
                // 칸: 채운 칸은 금빛, 빈칸은 테두리만
                float bx = r.x + 26f * u, by = r.y + 66f * u, bs = 26f * u;
                for (int s = 0; s < n; s++)
                {
                    var b = new Rect(bx + s * (bs + 10f * u), by, bs, bs);
                    GUI.color = s < n && MolakStats.Get(t.Slots[s].Key) > 0f ? gold : new Color(1f, 1f, 1f, 0.18f);
                    GUI.DrawTexture(b, MolakGui.Px); GUI.color = Color.white;
                }
                MolakGui.Ink(sSub, dim);
                string sub = nx < 0 ? "<color=#C9A227>다 풀었다</color>" : "다음 실마리 — " + t.Slots[nx].Hint;
                GUI.Label(new Rect(r.x + 26f * u, by + bs + 10f * u, r.width - 52f * u, 44f * u), sub, sSub);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none) && openT > 0.25f) { detail = i; openT = 0f; clicked = true; MolakSfx.Confirm(); }
                y += 150f * u + 14f * u;
            }
        }
        else
        {
            var t = MolakTruth.All[detail]; int nx = MolakTruth.NextIndex(t);
            MolakGui.Ink(sHead, gold);
            GUI.Label(new Rect(cx, y, cw, 64f * u), t.Title, sHead); y += 90f * u;
            for (int s = 0; s < t.Slots.Length; s++)
            {
                var sl = t.Slots[s]; bool known = MolakStats.Get(sl.Key) > 0f;
                string head = known ? "<color=#C9A227>「" + sl.Name + "」</color>" : (s == nx ? "<color=#8A7A66>「 ? 」</color>" : "<color=#5E574F>· · ·</color>");
                string body = known ? sl.Text : (s == nx ? "다음 실마리 — " + sl.Hint : "");
                MolakGui.Ink(sBody, known ? ink : dim);
                float bh = sBody.CalcHeight(new GUIContent(head + "  " + body), cw);
                GUI.Label(new Rect(cx, y, cw, bh), head + "  " + body, sBody);
                y += bh + 22f * u;
            }
        }

        var br = new Rect(cx + cw * 0.25f, h - 140f * u, cw * 0.5f, 96f * u);
        GUI.color = new Color(0.16f, 0.13f, 0.11f, 1f); GUI.DrawTexture(br, MolakGui.Px); GUI.color = Color.white;
        MolakGui.Ink(sBtn, ink); GUI.Label(br, detail >= 0 ? "돌아간다" : "닫는다", sBtn);
        if (!clicked && GUI.Button(br, GUIContent.none, GUIStyle.none) && openT > 0.2f)
        {
            if (detail >= 0) { detail = -1; openT = 0f; } else { open = false; MolakInputGuard.MuteFor(0.25f); }
        }
        GUI.Button(new Rect(0, 0, w, h), GUIContent.none, GUIStyle.none);
        GUI.color = g0;
    }
}
