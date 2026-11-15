using System.Collections.Generic;
using System.Text.RegularExpressions;
using Naninovel;
using UnityEngine;

/// <summary>
/// 0.4.68 무협 낱말 풀이(언메이커: "무협을 모르는 사람은 표사 같은 단어를 이해하기 힘들다. 처음에는 표기를 해야").
///  · 본문에 어떤 낱말이 처음 나오면 글자리 아래쪽에 작은 풀이 쪽지가 뜬다: 「표사(鏢師) — 돈을 받고 짐과 사람을 지켜 주는 무사」.
///  · 한 기기에서 한 번 본 낱말은 다시 띄우지 않는다(PlayerPrefs). 메뉴 「길잡이」 맨 아래 「낱말 풀이」에서 본 것들을 다시 볼 수 있다.
///  · 본문 글자 수를 바꾸지 않는다(글이 드러나는 셈이 어긋나지 않게) — 풀이는 본문 밖 쪽지로만.
///  · MolakNames.Paint(본문 전처리)가 쪽마다 Scan 을 부른다.
/// </summary>
public class MolakGloss : MonoBehaviour
{
    public struct G { public string Term, Han, Text; }
    public static readonly G[] All = {
        new G { Term = "강호", Han = "江湖", Text = "무인들이 칼로 먹고사는 바깥세상. 관아의 법보다 칼과 의리가 먼저인 곳" },
        new G { Term = "무림", Han = "武林", Text = "무공을 익힌 사람들과 문파들이 이루는 세계" },
        new G { Term = "문파", Han = "門派", Text = "한 스승에게서 이어지는 무공 집안. 청운검파·적운검파 같은 것" },
        new G { Term = "검파", Han = "劍派", Text = "칼을 주로 쓰는 문파" },
        new G { Term = "장문인", Han = "掌門人", Text = "문파의 우두머리" },
        new G { Term = "사부", Han = "師父", Text = "무공을 가르쳐 준 스승. 아버지처럼 모신다" },
        new G { Term = "사형", Han = "師兄", Text = "같은 스승 밑에서 먼저 배운 남자 선배" },
        new G { Term = "사저", Han = "師姐", Text = "같은 스승 밑에서 먼저 배운 여자 선배" },
        new G { Term = "객잔", Han = "客棧", Text = "나그네가 묵고 밥을 먹는 여관 겸 주막" },
        new G { Term = "주루", Han = "酒樓", Text = "술과 음식을 파는 이층짜리 술집" },
        new G { Term = "표국", Han = "鏢局", Text = "돈을 받고 남의 짐(표물)과 사람을 먼 길 지켜 실어 나르는 집" },
        new G { Term = "표사", Han = "鏢師", Text = "표국에 고용되어 표물을 지키는 무사" },
        new G { Term = "표두", Han = "鏢頭", Text = "표사들을 이끄는 우두머리" },
        new G { Term = "표물", Han = "鏢物", Text = "표국이 맡아 지켜 나르는 짐" },
        new G { Term = "내공", Han = "內功", Text = "몸 안에 쌓는 기운. 높을수록 오래 버티고 칼에 힘이 실린다" },
        new G { Term = "단전", Han = "丹田", Text = "배꼽 아래, 내공이 모이는 자리" },
        new G { Term = "혈도", Han = "穴道", Text = "기운이 흐르는 몸의 길목. 막히면 기가 돌지 않는다" },
        new G { Term = "운기조식", Han = "運氣調息", Text = "앉아서 숨을 고르며 내공을 몸에 돌리는 수련" },
        new G { Term = "초식", Han = "招式", Text = "이름이 붙은 칼의 한 수. 정해진 움직임의 묶음" },
        new G { Term = "구결", Han = "口訣", Text = "무공의 요령을 외우기 쉽게 적은 글귀" },
        new G { Term = "경지", Han = "境地", Text = "무공의 높이. 이류 → 일류 → 절정 → 초절정 순으로 오른다" },
        new G { Term = "비무", Han = "比武", Text = "무공을 겨루는 공개 대련. 이기면 강호 서열이 오른다" },
        new G { Term = "서열록", Han = "序列錄", Text = "강호 무인들의 순위를 적은 장부. 이름이 오를수록 알려진다" },
        new G { Term = "하오문", Han = "下午門", Text = "거지·장사꾼·기생 같은 밑바닥 사람들의 소식 장사 조직" },
        new G { Term = "혈마교", Han = "血魔敎", Text = "십 년 전 무림맹을 무너뜨린 사교(邪敎). 이 땅을 쥐고 있다" },
        new G { Term = "경공", Han = "輕功", Text = "몸을 가볍게 하여 빨리 달리고 높이 뛰는 무공" },
        new G { Term = "은자", Han = "銀子", Text = "은으로 된 돈. 한 냥이면 객잔 하룻밤 값" },
        new G { Term = "시진", Han = "時辰", Text = "옛날 시간 단위. 한 시진은 지금의 두 시간" },
        new G { Term = "삼경", Han = "三更", Text = "한밤중(밤 11시~새벽 1시 무렵)" },
        new G { Term = "호법", Han = "護法", Text = "누가 수련·돌파하는 동안 곁을 지켜 주는 일" },
        new G { Term = "기연", Han = "奇緣", Text = "우연히 만나는 큰 복. 영약·비급·고수의 가르침 같은 것" },
        new G { Term = "협객", Han = "俠客", Text = "남을 위해 칼을 뽑는 의로운 무인" },
        new G { Term = "별호", Han = "別號", Text = "강호에서 사람들이 붙여 부르는 이름" },
        new G { Term = "낭자", Han = "娘子", Text = "젊은 여인을 높여 부르는 말" },
        new G { Term = "사술", Han = "邪術", Text = "사악한 술법. 여기서는 가슴의 문양이 부리는 힘" },
        new G { Term = "비록", Han = "秘錄", Text = "문파의 무공을 몰래 적어 전하는 책" },
        new G { Term = "금창약", Han = "金瘡藥", Text = "칼에 베인 상처에 바르는 약" },
        new G { Term = "관아", Han = "官衙", Text = "고을의 관청. 지금은 혈마교 손에 있다" },
        new G { Term = "전낭", Han = "錢囊", Text = "돈을 넣어 허리에 차는 주머니" },
        new G { Term = "합", Han = "合", Text = "칼이 한 번 맞부딪치는 한 차례" },
    };

    static Regex rx;
    static HashSet<string> seen;
    static readonly Queue<G> queue = new Queue<G>();
    static readonly List<(G g, float t0)> showing = new List<(G, float)>();
    static MolakGloss inst;
    const string Pref = "molak.gloss";
    const float Life = 7.5f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot ()
    {
        if (inst) return;
        var go = new GameObject("MolakGloss"); DontDestroyOnLoad(go); inst = go.AddComponent<MolakGloss>();
    }

    static void Build ()
    {
        if (rx != null) return;
        var terms = new List<string>(); foreach (var g in All) terms.Add(Regex.Escape(g.Term));
        terms.Sort((a, b) => b.Length.CompareTo(a.Length));
        // 낱말 앞이 한글 글자가 아니어야 한다(“칼합”의 '합' 같은 것 막기). '합'은 두 글자 이상 낱말보다 흔해서 따로 꼴을 좁힌다.
        rx = new Regex("(?<![가-힣])(" + string.Join("|", terms.ToArray()) + ")", RegexOptions.Compiled);
        seen = new HashSet<string>();
        try { foreach (var s in PlayerPrefs.GetString(Pref, "").Split(',')) if (s.Length > 0) seen.Add(s); } catch { }
    }

    public static bool Seen (string term) { Build(); return seen.Contains(term); }

    /// <summary>본문 한 쪽(태그 포함)을 훑어 처음 보는 낱말을 쪽지 대기열에 넣는다.</summary>
    public static void Scan (string text)
    {
        if (string.IsNullOrEmpty(text) || MolakTitle.Showing) return;
        Build();
        if (seen.Count >= All.Length) return;
        string plain = Regex.Replace(text, "<[^>]+>", "");
        bool changed = false;
        foreach (Match m in rx.Matches(plain))
        {
            string term = m.Groups[1].Value;
            if (seen.Contains(term)) continue;
            if (term == "합" && !(plain.Contains(" 합") || plain.Contains("합을") || plain.Contains("합에") || plain.Contains("합째"))) continue;
            foreach (var g in All) if (g.Term == term) { queue.Enqueue(g); break; }
            seen.Add(term); changed = true;
        }
        if (changed) { try { PlayerPrefs.SetString(Pref, string.Join(",", new List<string>(seen).ToArray())); PlayerPrefs.Save(); } catch { } }
    }

    /// <summary>「모든 생의 기억 지우기」 때 함께.</summary>
    public static void Reset () { Build(); seen.Clear(); try { PlayerPrefs.DeleteKey(Pref); } catch { } }

    GUIStyle sT; float bu = -1f;
    void OnGUI ()
    {
        if (!Engine.Initialized || MolakMenu.IsOpen || MolakTitle.Showing) return;
        float now = Time.unscaledTime;
        for (int i = showing.Count - 1; i >= 0; i--) if (now - showing[i].t0 > Life) showing.RemoveAt(i);
        while (showing.Count < 2 && queue.Count > 0) showing.Add((queue.Dequeue(), now));
        if (showing.Count == 0) return;
        float u = MolakGui.U;
        if (sT == null || !Mathf.Approximately(bu, u)) { bu = u; sT = MolakGui.Label(25, u, TextAnchor.UpperLeft, true); sT.richText = true; }
        GUI.depth = MolakGui.DepthVfx + 2;
        var ta = MolakVfx.TextArea();
        float pad = 12f * u, w = ta.width;
        var sb = new System.Text.StringBuilder(); float a = 0f;
        foreach (var s in showing)
        {
            float age = now - s.t0;
            a = Mathf.Max(a, age < 0.3f ? age / 0.3f : age > Life - 0.8f ? Mathf.Clamp01((Life - age) / 0.8f) : 1f);
            if (sb.Length > 0) sb.Append('\n');
            sb.Append("<b>" + s.g.Term + "</b><color=#8A7A66>(" + s.g.Han + ")</color> — " + s.g.Text);
        }
        string txt = sb.ToString();
        float hgt = sT.CalcHeight(new GUIContent(txt), w - pad * 2f) + pad * 1.6f;
        var r = new Rect(ta.x, ta.yMax - hgt, w, hgt);
        var g0 = GUI.color;
        GUI.color = new Color(0.98f, 0.96f, 0.91f, 0.96f * a); GUI.DrawTexture(r, MolakGui.Px);
        GUI.color = new Color(0.62f, 0.45f, 0.18f, 0.8f * a); GUI.DrawTexture(new Rect(r.x, r.y, Mathf.Max(2f, 3f * u), r.height), MolakGui.Px);
        GUI.color = g0;
        MolakGui.Ink(sT, new Color(0.2f, 0.16f, 0.12f, a));
        GUI.Label(new Rect(r.x + pad + 4f * u, r.y + pad * 0.8f, w - pad * 2f, hgt), txt, sT);
    }
}
