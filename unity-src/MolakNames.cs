using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// 0.4.59 본문 속 이름에 사람마다 다른 색(언메이커: 이름이 눈에 안 들어온다).
/// 본문 TMP 전처리기(MolakTypography.DialogueStyler)에서 대사 가르기 전에 한 번 칠한다 — 대본은 그대로 둔다.
///  · 이미 색이 칠해진 구간(<color>…</color>, 첫 소개 같은 붉은 이름)은 건드리지 않는다.
///  · 낮(종이)과 먹색 화면에 각각 읽히는 색을 따로 둔다.
///  · 이 게임의 뜻 색과 겹치지 않게: 붉음(#C03434 위험) · 보라(#9A70C4 앎) · 금(#C9A227 보상)은 이름에 쓰지 않는다.
///  · 은자 액수("은자 3냥", "열 냥")는 엽전색으로 — 고를 때 셈에 들어가는 수라서.
/// </summary>
public static class MolakNames
{
    struct P { public string Rx, Day, Night; public P (string rx, string d, string n) { Rx = rx; Day = d; Night = n; } }

    // 긴 이름이 먼저 걸리게 순서를 둔다. 정규식 조각(뒤따르는 글자로 다른 낱말을 거른다).
    static readonly P[] People = {
        new P("소아",                                   "#B23A62", "#F08FB2"),   // 적운의 쌍검 — 장밋빛
        new P("하진무",                                 "#7A4A22", "#DDA772"),   // 객잔 주인 — 호두빛 갈색
        new P("혈랑대주|대사형|진백",                    "#2F5590", "#8FB4EC"),   // 대사형 — 쇠빛 푸름
        new P("백초 할멈|할멈",                          "#2F7A3A", "#8ED48C"),   // 약초 — 풀빛
        new P("노인(?![들네])",                          "#256E6E", "#7ED2D2"),   // 복면 노인 — 청록
        new P("혈천마|혈마교주|교주(?![전])",             "#6E1830", "#E27C90"),   // 교주 — 검붉은 자줏빛(위험 붉음보다 어둡게)
        new P("곽산",                                   "#A4471F", "#F0956A"),   // 창잡이 — 녹슨 쇠
        new P("용개",                                   "#66651A", "#D2CE6C"),   // 회목채 — 올리브
        new P("청묘",                                   "#1C7894", "#76C9E6"),   // 산사 비구니 — 물빛
        new P("암향(?!루)",                             "#92387F", "#E48CD4"),   // 암향루의 그녀 — 자홍
        new P("쥐눈이 장",                              "#5E5248", "#C7B6A6"),   // 장 — 쥐털 잿빛
        new P("참새",                                   "#8A6A12", "#E3C463"),   // 저잣거리 귀 — 황토
        new P("백리휘",                                 "#3B4E7A", "#AFC0E8"),   // 무림맹 밀사 — 남빛
        new P("남궁현",                                 "#4E5E6E", "#B8C8D8"),   // 세가 — 은회색
        new P("운검사태",                               "#2E7D5E", "#86D6B4"),   // 연잎 — 연둣빛 청록
        new P("왕삼",                                   "#7A5A2E", "#D8B080"),   // 고리대 — 놋빛
        new P("마충",                                   "#5B6B3A", "#BFD08F"),   // 낭인 — 쑥빛
        new P("철산도",                                 "#5A3A2A", "#CDA088"),   // 산적 — 흙빛
        new P("혈쇄문주",                               "#4A3A5A", "#BBA8D0"),   // 사슬 — 어둔 쇠보라
        new P("견아|호치",                              "#5A5A2A", "#C8C890"),   // 독아단 — 독풀빛
        new P("집법전주",                               "#3A3A3A", "#C8C8C8"),   // 집법 — 먹
        new P("꼬지|오치",                              "#A0603A", "#F0B088"),   // 아이들 — 살구빛
    };
    const string CoinDay = "#8C6B1F", CoinNight = "#E8C870";

    static Regex rx; static string[] dayHex, nightHex; static Regex coin;
    static void Build ()
    {
        if (rx != null) return;
        var sb = new StringBuilder();
        dayHex = new string[People.Length]; nightHex = new string[People.Length];
        for (int i = 0; i < People.Length; i++)
        {
            if (i > 0) sb.Append('|');
            sb.Append("(?<p").Append(i).Append(">").Append(People[i].Rx).Append(')');
            dayHex[i] = People[i].Day; nightHex[i] = People[i].Night;
        }
        rx = new Regex(sb.ToString(), RegexOptions.Compiled);
        // 은자 액수: "은자 3냥", "12냥", "열 냥", "스무 냥", "냥 반" 같은 꼴
        coin = new Regex(@"(?:은자\s*)?(?:\d+|한|두|세|네|닷|엿|열|스무|서른|마흔|쉰|백|천)\s?냥", RegexOptions.Compiled);
    }

    static readonly Dictionary<string, string> cacheDay = new Dictionary<string, string>(), cacheNight = new Dictionary<string, string>();

    /// <summary>태그 밖, 이미 칠한 색 구간 밖의 이름·은자 액수에 색을 입힌다.</summary>
    public static string Paint (string text, bool night)
    {
        if (string.IsNullOrEmpty(text)) return text;
        Build();
        var cache = night ? cacheNight : cacheDay;
        string hit; if (cache.TryGetValue(text, out hit)) return hit;
        var hexes = night ? nightHex : dayHex; string coinHex = night ? CoinNight : CoinDay;
        var outp = new StringBuilder(text.Length + 64);
        int colorDepth = 0, i = 0;
        while (i < text.Length)
        {
            if (text[i] == '<')
            {
                int e = text.IndexOf('>', i);
                if (e < 0) { outp.Append(text, i, text.Length - i); break; }
                string tag = text.Substring(i, e - i + 1);
                if (tag.StartsWith("<color", System.StringComparison.OrdinalIgnoreCase)) colorDepth++;   // 이미 칠한 구간(첫 소개의 붉은 이름 등)
                else if (tag.StartsWith("</color", System.StringComparison.OrdinalIgnoreCase)) colorDepth = System.Math.Max(0, colorDepth - 1);
                outp.Append(tag); i = e + 1; continue;
            }
            int next = text.IndexOf('<', i); if (next < 0) next = text.Length;
            string seg = text.Substring(i, next - i);
            if (colorDepth > 0) outp.Append(seg);
            else
            {
                seg = coin.Replace(seg, m => "<color=" + coinHex + ">" + m.Value + "</color>");
                // 은자 색을 칠한 뒤라 그 안의 글자는 다시 칠하지 않게 조각으로 나눠 이름을 칠한다
                outp.Append(PaintNames(seg, hexes));
            }
            i = next;
        }
        string res = outp.ToString();
        if (cache.Count > 400) cache.Clear();
        cache[text] = res;
        return res;
    }

    static string PaintNames (string seg, string[] hexes)
    {
        if (seg.IndexOf('<') < 0) return rx.Replace(seg, m => Wrap(m, hexes));
        // 은자 색 태그가 섞인 조각: 태그 밖만
        var sb = new StringBuilder(); int d = 0, i = 0;
        while (i < seg.Length)
        {
            if (seg[i] == '<') { int e = seg.IndexOf('>', i); string tag = seg.Substring(i, e - i + 1); d += tag.StartsWith("</") ? -1 : 1; sb.Append(tag); i = e + 1; continue; }
            int n = seg.IndexOf('<', i); if (n < 0) n = seg.Length;
            string s = seg.Substring(i, n - i);
            sb.Append(d > 0 ? s : rx.Replace(s, m => Wrap(m, hexes)));
            i = n;
        }
        return sb.ToString();
    }

    static string Wrap (Match m, string[] hexes)
    {
        for (int k = 0; k < hexes.Length; k++)
            if (m.Groups["p" + k].Success) return "<b><color=" + hexes[k] + ">" + m.Value + "</color></b>";   // 가는 명조라 색만으로는 약하다 — 굵게
        return m.Value;
    }
}
