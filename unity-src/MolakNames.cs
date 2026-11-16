using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// 0.4.59 본문 속 이름에 사람마다 다른 색(언메이커: 이름이 눈에 안 들어온다).
/// 0.4.60 세력 이름·초식 이름도(언메이커: 세력이름, 초식이름은 칠할 필요가 있다).
///  · 사람 = 굵게 + 사람마다 다른 색. 세력 = 그 세력의 우두머리와 같은 색 계열, 굵지 않게(사람과 갈래가 보이게).
///  · 초식·무공 = 쪽빛 한 가지 + 밑줄 — 사람·세력 색과 섞여도 '이건 칼 쓰는 법'으로 읽히게.
/// 0.4.61 이야기의 실마리(언메이커: 스토리에 중요한 것들도 표기): 문양·북두(0.4.66 복면은 뺐다)·운주 들판·연잎·산문·우물·반쪽·열매… 「그 밤의 진실」에 걸린 낱말은
///  글자색은 그대로 두고 옅은 금빛 형광(<mark>)을 깐다 — 진실 판의 금빛 칸과 같은 빛. 한 쪽에 같은 낱말은 처음 한 번만(같은 쪽에 '문양'이 다섯 번 나와도 첫 번째만).
/// 본문 TMP 전처리기(MolakTypography.DialogueStyler)에서 대사 가르기 전에 한 번 칠한다 — 대본은 그대로 둔다.
///  · 이미 색이 칠해진 구간(<color>…</color>, 첫 소개 같은 붉은 이름)은 건드리지 않는다.
///  · 낮(종이)과 먹색 화면에 각각 읽히는 색을 따로 둔다.
///  · 이 게임의 뜻 색과 겹치지 않게: 붉음(#C03434 위험) · 보라(#9A70C4 앎) · 금(#C9A227 보상)은 이름에 쓰지 않는다.
///  · 은자 액수("은자 3냥", "열 냥")는 엽전색으로 — 고를 때 셈에 들어가는 수라서.
/// </summary>
public static class MolakNames
{
    struct P { public string Rx, Day, Night; public int Kind; public P (string rx, string d, string n, int kind = 0) { Rx = rx; Day = d; Night = n; Kind = kind; } }
    const int Person = 0, Faction = 1, Art = 2, Clue = 3;
    const string ClueDay = "#E0B43C80", ClueNight = "#C9A22790";
    const string ArtDay = "#1F5878", ArtNight = "#8CCBEB";
    // 0.4.66 초식은 밑줄 대신 형광펜(언메이커) — 실마리(금빛)와 갈리게 옅은 쪽빛. 매번 칠한다.
    const string ArtMarkDay = "#7FB9DA70", ArtMarkNight = "#3F7FA690";

    // 초식·무공: 가장 먼저 건다(청운검법 → 청운, 혈쇄망 → 혈쇄문 보다 앞서게)
    static readonly P[] Arts = {
        new P("청운검결|운보\\(雲步\\)|운보|관\\(觀\\)|연\\(連\\)|낙\\(落\\)|운검비록|운상보|북두낙월|적운쌍영|회마창|혈쇄망|멸도벽|천뢰일섬|철산고|독아쇄|광견난무|집법단죄|청운검법|청운검결|제자검법|적운의 구결|횡소|기수식|혈천진|멸도(?!벽)", ArtDay, ArtNight, Art),
    };
    // 실마리: 「그 밤의 진실」 줄기에 걸린 낱말. 한 갈래마다 한 쪽에 한 번.
    static readonly P[] Clues = {
        new P("북두칠성|북두(?!를 거꾸로)", ClueDay, ClueNight, Clue),
        new P("사형의 패|일곱째의 패", ClueDay, ClueNight, Clue),
        new P("문양", ClueDay, ClueNight, Clue),
        new P("운주 들판", ClueDay, ClueNight, Clue),   // 0.4.66 소아가 일러 주는 무인 모임 자리(언메이커: 중요한 일인데 강조를)
        new P("연잎|蓮", ClueDay, ClueNight, Clue),
        new P("산문(?=을 열|을 연|이 열|을 닫)", ClueDay, ClueNight, Clue),
        new P("멸문", ClueDay, ClueNight, Clue),
        new P("검보|석함|제단", ClueDay, ClueNight, Clue),
        new P("반쪽", ClueDay, ClueNight, Clue),
        new P("우물(?!가)", ClueDay, ClueNight, Clue),
        new P("열매|씨앗", ClueDay, ClueNight, Clue),
        new P("구결", ClueDay, ClueNight, Clue),
        new P("해약", ClueDay, ClueNight, Clue),
    };
    // 세력: 사람 뒤에 건다(혈쇄문주 → 혈쇄문, 남궁현 → 남궁, 교주 → 교 보다 앞서게)
    static readonly P[] Factions = {
        new P("청운검파|청운산장|청운",                     "#4A6A8A", "#A8C2DC", Faction),   // 우리 — 옅은 쇠빛
        new P("적운검파|적운",                             "#9A4A3A", "#E29C8A", Faction),   // 붉은 구름 — 벽돌빛
        new P("운검류",                                   "#2E7D5E", "#86D6B4", Faction),   // 운검사태의 갈래
        new P("혈마교|마교|교도|(?<![가-힣])교(?=(?:의|가|는|를|에서|에게|에|도|와|로)(?![가-힣]))", "#6E1830", "#E27C90", Faction),
        new P("혈랑대",                                   "#5A3440", "#D49AA8", Faction),
        new P("혈쇄문",                                   "#4A3A5A", "#BBA8D0", Faction),
        new P("무림맹|(?<![가-힣])맹(?=(?:의|은|이|에|도|과|을|에서)(?![가-힣]))", "#3B4E7A", "#AFC0E8", Faction),
        new P("남궁세가|남궁(?!현)",                        "#4E5E6E", "#B8C8D8", Faction),
        new P("독아단",                                   "#5A5A2A", "#C8C890", Faction),
        new P("집법전|집법당",                             "#3A3A3A", "#C8C8C8", Faction),
        new P("하오문",                                   "#6A4A6A", "#D2AED2", Faction),
        new P("개방",                                     "#7A6440", "#D8C098", Faction),
        new P("징세조",                                   "#5E4A36", "#CCB096", Faction),
        new P("서열각",                                   "#5A5040", "#C8BCA4", Faction),
        new P("회목채",                                   "#66651A", "#D2CE6C", Faction),
    };

    // 긴 이름이 먼저 걸리게 순서를 둔다. 정규식 조각(뒤따르는 글자로 다른 낱말을 거른다).
    static readonly P[] People = {
        new P("소아",                                   "#B23A62", "#F08FB2"),   // 적운의 쌍검 — 장밋빛
        new P("하진무",                                 "#7A4A22", "#DDA772"),   // 객잔 주인 — 호두빛 갈색
        new P("혈랑대주|대사형|진백",                    "#2F5590", "#8FB4EC"),   // 대사형 — 쇠빛 푸름
        new P("백초 할멈|할멈",                          "#2F7A3A", "#8ED48C"),   // 약초 — 풀빛
        new P("백발(?:의)? ?노인|복면(?:을 쓴)? ?노인|노인(?![들네])", "#256E6E", "#7ED2D2"),   // 백발 노인 — 청록. 0.4.66 '백발 노인'·'복면 노인'은 통째로(언메이커: 노인에만 칠하면 헷갈린다)
        new P("혈천마|혈마교주|교주(?![전])",             "#6E1830", "#E27C90"),   // 교주 — 검붉은 자줏빛(위험 붉음보다 어둡게)
        new P("곽산",                                   "#A4471F", "#F0956A"),   // 창잡이 — 녹슨 쇠
        new P("용개",                                   "#66651A", "#D2CE6C"),   // 회목채 — 올리브
        new P("청묘",                                   "#1C7894", "#76C9E6"),   // 산사 비구니 — 물빛
        new P("암향(?!루)",                             "#92387F", "#E48CD4"),   // 암향루의 그녀 — 자홍
        new P("쥐눈이 장",                              "#5E5248", "#C7B6A6"),   // 장 — 쥐털 잿빛
        new P("참새",                                   "#8A6A12", "#E3C463"),   // 저잣거리 귀 — 황토
        new P("백리휘",                                 "#3B4E7A", "#AFC0E8"),   // 무림맹 밀사 — 남빛
        new P("남궁현",                                 "#4E5E6E", "#B8C8D8"),   // 세가 — 은회색
        new P("운검사태|운검(?![류비])",                 "#2E7D5E", "#86D6B4"),   // 연잎 — 연둣빛 청록
        new P("왕삼",                                   "#7A5A2E", "#D8B080"),   // 고리대 — 놋빛
        new P("염혁",                                   "#5B6B3A", "#BFD08F"),   // 0.4.70 장풍표국 표사 — 쑥빛(마충 자리)
        new P("단목결|다섯째 사형",                       "#5A4E78", "#C4B8E8"),   // 0.4.70 서열각의 칼 — 차가운 보랏빛 잿빛
        new P("철산도",                                 "#5A3A2A", "#CDA088"),   // 산적 — 흙빛
        new P("혈쇄문주",                               "#4A3A5A", "#BBA8D0"),   // 사슬 — 어둔 쇠보라
        new P("견아|호치",                              "#5A5A2A", "#C8C890"),   // 독아단 — 독풀빛
        new P("집법전주",                               "#3A3A3A", "#C8C8C8"),   // 집법 — 먹
        new P("꼬지|오치",                              "#A0603A", "#F0B088"),   // 아이들 — 살구빛
    };
    const string CoinDay = "#8C6B1F", CoinNight = "#E8C870";

    static Regex rx; static string[] dayHex, nightHex; static int[] kinds; static Regex coin;
    static readonly HashSet<int> clueSeen = new HashSet<int>();
    static void Build ()
    {
        if (rx != null) return;
        var sb = new StringBuilder();
        var all = new List<P>(); all.AddRange(Arts); all.AddRange(People); all.AddRange(Factions); all.AddRange(Clues);
        dayHex = new string[all.Count]; nightHex = new string[all.Count]; kinds = new int[all.Count];
        for (int i = 0; i < all.Count; i++)
        {
            if (i > 0) sb.Append('|');
            sb.Append("(?<p").Append(i).Append(">").Append(all[i].Rx).Append(')');
            dayHex[i] = all[i].Day; nightHex[i] = all[i].Night; kinds[i] = all[i].Kind;
        }
        rx = new Regex(sb.ToString(), RegexOptions.Compiled);
        // 은자 액수: "은자 3냥", "12냥", "열 냥", "스무 냥", "냥 반" 같은 꼴
        coin = new Regex(@"(?:은자\s*)?(?:\d+|[이삼사오육칠팔구]?[백천](?:[이삼사오육칠팔구]?십)?|한|두|세|네|닷|엿|열|스무|서른|마흔|쉰)\s?냥", RegexOptions.Compiled);
    }

    static readonly Dictionary<string, string> cacheDay = new Dictionary<string, string>(), cacheNight = new Dictionary<string, string>();
    static bool paintNight;

    /// <summary>태그 밖, 이미 칠한 색 구간 밖의 이름·은자 액수에 색을 입힌다.</summary>
    public static string Paint (string text, bool night)
    {
        if (string.IsNullOrEmpty(text)) return text;
        try { MolakGloss.Scan(text); } catch { }   // 0.4.68 무협 낱말 풀이 쪽지
        Build();
        var cache = night ? cacheNight : cacheDay;
        string hit; if (cache.TryGetValue(text, out hit)) return hit;
        var hexes = night ? nightHex : dayHex; string coinHex = night ? CoinNight : CoinDay;
        paintNight = night;
        // 0.4.68 대사 안의 액수는 칠하지 않는다(사람의 말 속 "삼백 냥"이 반만 칠해져 어색했다). 서술의 액수만.
        string tt = text.TrimStart(); bool dialogue = tt.Length > 0 && (tt[0] == '"' || tt[0] == '“' || tt[0] == '「');
        clueSeen.Clear();
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
                if (!dialogue) seg = coin.Replace(seg, m => "<color=" + coinHex + ">" + m.Value + "</color>");
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
            if (m.Groups["p" + k].Success)
            {
                string c = "<color=" + hexes[k] + ">" + m.Value + "</color>";
                if (kinds[k] == Person) return "<b>" + c + "</b>";   // 가는 명조라 색만으로는 약하다 — 굵게
                if (kinds[k] == Art) return "<mark=" + (paintNight ? ArtMarkNight : ArtMarkDay) + ">" + c + "</mark>";
                if (kinds[k] == Clue)
                {
                    if (clueSeen.Contains(k)) return m.Value;   // 한 쪽에 한 번
                    clueSeen.Add(k);
                    return "<mark=" + hexes[k] + ">" + m.Value + "</mark>";
                }
                return c;
            }
        return m.Value;
    }
}
