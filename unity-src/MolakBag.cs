using System.Collections.Generic;
using Naninovel;
using UnityEngine;

/// <summary>
/// 행낭 — 화면 맨 아래 띠에서 위로 쓸어 올리면 올라오는 서랍.
/// 닫힌 상태에선 하단 한 줄에 '가장 최근에 얻은 것'만 보여 준다. 목록은 최근 획득 순, 손가락 드래그로 부드럽게 스크롤(관성).
/// </summary>
public class MolakBag : MonoBehaviour
{
    public struct Entry { public string Flag, Name, Desc, Effect; public Entry (string f, string n, string d, string e) { Flag = f; Name = n; Desc = d; Effect = e; } }

    public static readonly Entry[] Items = {
        new Entry("f_proknot", "청운 매듭", "하산 패에 달려 있던 푸른 매듭. 끝이 그을렸다. '너도 하산할 때 달아라. 삼십 년쯤 뒤에.'", ""),
        new Entry("f_projujube", "사저의 대추", "돌처럼 굳은 마른 대추 둘. '밤에 먹을 거면 둘은 있어야죠.'", ""),
        new Entry("f_soaledger", "적운의 장부", "줄마다 끼니가 적혀 있다. 끝마다 '빚'이라 썼다가 지웠다.", ""),
        new Entry("f_cmform", "아홉째 초식 · 운해(雲海)", "횡소가 끝나는 자리에서 시작하는 검. 사저가 장작으로 가르쳤다.", "검술 +2"),
        new Entry("f_rankplate", "무명의 쇠패", "서열각 감찰사가 내준 쇠패. 숫자 자리가 비어 있다. 패를 찬 자는 서열각의 것이다.", "혈마교 무리가 함부로 칼을 대지 못한다"),
        new Entry("f_hat", "죽립과 잿빛 장포", "첫날 옷전에서 산 것. 챙을 내리면 이마에서 콧날까지 그늘이 진다.", "얼굴을 가린다"),
        new Entry("f_whetstone", "청석 숫돌", "눈먼 숫돌 장수의 청석. 일 년 묵은 녹이 한 시간 만에 벗겨졌다.", "검술 +1"),
        new Entry("f_salve", "백초당 금창약", "할멈이 '상처나 고치거라' 하며 내준 한 통.", "곪은 상처를 한 번 다스린다"),
        new Entry("f_honed", "벼린 칼날", "대장간 무고가 사흘 밤을 들여 날을 세웠다.", "검술 +1"),
        new Entry("f_bracer", "가죽 호완(護腕)", "소가죽 두 겹. 칼을 받아도 뼈까지는 오지 않는다.", "체력 상한 +1"),
        new Entry("f_vest", "철편 조끼", "옷 안에 쇳조각을 엮어 덧댔다. 무겁지만 한 칼은 막아 준다.", "체력 상한 +2"),
        new Entry("f_antidote", "해독환(解毒丸)", "할멈이 기름종이에 싸 준 검은 환약 한 알.", "독이 돌 때 밤에 삼킨다"),
        new Entry("f_reforge", "다시 세운 날", "무고가 이레 밤을 들여 한 번 더 세운 날. 얇아진 만큼 무엇이든 한 번은 벤다.", "검술 +2"),
        new Entry("f_amhcomb", "이 빠진 빗", "설의 어머니 것. 연의 머리를 빗을 때만 쓴다. 공자 손에 들려 있으면 다음에 또 올라오실 테니까요.", ""),
        new Entry("f_cmtablet", "반쯤 탄 위패", "곳간 쌀가마 뒤에 대사형이 숨겨 둔 것. 「운검사태」 넉 자가 남았다.", ""),
        new Entry("f_jbrice", "대사형의 쌀자루", "매듭은 대사형 매듭. 그의 손으론 오늘도 못 갖다 준다던 것.", ""),
        new Entry("f_herbrecipe", "사부의 처방지", "막내. 몸이 차고 기가 얕음. 정심단 반 알씩. 온 알은 독이 됨.", ""),
        new Entry("f_janglast", "쥐눈이 장의 장부", "마지막 줄에 당신 이름. 집법전주는 오른손으로 법을 쓰고 왼손으로 사람을 쓴다.", "집법전주와 겨룰 때 유리하다"),
        new Entry("f_jaha", "자하단(紫霞丹) · 복용", "청운이 망하던 해 할멈이 지어 둔 마지막 한 알.", "저주 −12"),
        new Entry("f_incense", "안식향", "머리맡에 피우면 가슴의 열이 조금 낮게 끓는다.", "저주 −2"),
        new Entry("f_cheonggang", "청강검(靑鋼劍)", "청운검파 장문인의 검. 손에 감기듯 맞는다.", ""),
        new Entry("f_guardsword", "표국의 검", "문 닫은 표국의 총표두 염가가 맡긴 검. 뼈대가 좋다.", "받을 때 검술 +1"),
        new Entry("f_bilok", "운검비록(雲劍秘錄)", "청운의 비록. 구결이 손끝을 이끈다.", "밤 검술 수련이 더 잘 오른다"),
        new Entry("f_biloklearn", "운검비록 · 익힘", "비록의 구결을 몸에 들였다.", "밤 검술 수련이 더 잘 오른다"),
        new Entry("f_bilokhalf", "운검비록 · 반쪽", "반만 읽어 낸 비록. 수련이 반 치씩 더 쌓인다.", "밤 검술 수련이 더 잘 오른다"),
        new Entry("f_relicpae", "일곱째 사형의 패", "야시에서 되찾은 청운의 패. 가슴 문양 위에 품고 다닌다.", ""),
        new Entry("f_inkcord", "검은 결끈", "북쪽 짐에서 나온 검끈. 뽑는 손이 반 호흡 빨라진다.", "검술 +1"),
        new Entry("f_talisman2", "진짜 부적", "부적 장수가 사과로 건넨, 제 손으로 쓴 부적.", "저주 −4"),
        new Entry("f_acupgone", "금침 한 쌈", "북으로 떠난 금침 노인의 마지막 처방.", "저주 −4, 내공 +1"),
        new Entry("f_blackpill", "흑단환(黑斷丸) · 복용", "독으로 독을 눌렀다. 대신 몸의 그릇이 한 치 줄었다.", "저주 −15, 체력 상한 −2"),
        new Entry("f_sutrashard", "경판 조각", "불탄 산사에서 살아남은 두 줄.", ""),
        new Entry("f_newrobe", "무명 새 옷", "포목전 주인이 지어 준 옷. 소매가 검을 뽑을 때 걸리지 않는다.", "체력 +1"),
        new Entry("f_frost", "솜옷", "하진무가 말없이 두고 간 겨울옷.", "눈 오는 아침의 한기를 막는다"),
        new Entry("f_letter", "맡긴 편지", "받을 사람 없는 편지. 하진무가 품에 넣었다.", ""),
        new Entry("f_janginfo", "혈천진 보법 서찰", "혈마곡 입구 진법의 틈. 왼, 왼, 오른.", "교주와 겨룰 때 유리하다"),
    };
    public static readonly Entry[] Lore = {
        new Entry("f_jinlure", "북쪽 고개의 횃불", "정예를 북으로 끌고 간 맨 앞의 횃불.", "혈랑대와 겨룰 때 유리하다"),
        new Entry("g_oldsister", "셋째 잔", "노인이 사매 몫으로 따라 둔 잔. 둘이 한 검을 쓰던 시절이 있었다.", "회귀해도 남는다"),
        new Entry("f_northstep", "북두를 거꾸로", "요광에서 천추로. 다섯째 별에서 숨을 쉬지 않는다.", "교주와 겨룰 때 유리하다"),
        new Entry("f_storylore", "이야기꾼의 대목", "열 번째 산문, 청운산장에서 장문인은 검을 뽑지 않고 서 있었다고 한다.", ""),
        new Entry("f_herbpulse", "두 개의 맥", "백초 할멈이 짚었다. 하나는 네 것이 아니라고.", ""),
        new Entry("f_breath", "토탁(吐濁) 구결", "거지 중의 숨법. 들숨 여섯, 날숨 아홉.", "운기할 때 저주가 덜 오른다"),
        new Entry("f_breath2", "토탁 · 두 번째 구결", "날숨 아홉 끝의 반 호흡.", "운기할 때 저주가 한층 덜 오른다"),
        new Entry("f_tea", "청심차 비방", "연잎 석 냥, 감초 한 냥, 자하근. 새벽마다 한 사발.", "사술이 조금 덜 자란다"),
        new Entry("f_seedknow", "씨앗(種)의 정체", "가슴의 문양은 병이 아니라 누가 심은 글자다.", ""),
        new Entry("f_sutraknow", "경판의 두 줄", "심은 자 죽으면 씨 또한 죽는다. 씨가 다 익으면 심은 자가 거둔다.", ""),
        new Entry("f_monklore", "혈천교 토벌의 기억", "교주도 누군가의 밭이었다. 거두는 자가 되지 마라.", ""),
        new Entry("f_oldface", "노인의 얼굴", "왼눈 위에서 턱까지 내려오는 검상.", ""),
        new Entry("f_dokinfo", "독아단의 약점", "아장 호치의 버릇을 안다.", "호치와 겨룰 때 유리하다"),
        new Entry("f_chaininfo", "혈쇄문의 사슬 길", "사슬이 도는 길을 안다.", "혈쇄문주와 겨룰 때 유리하다"),
        new Entry("f_wolfedge", "혈쇄문주의 허점", "", "혈쇄문주와 겨룰 때 유리하다"),
        new Entry("f_lordedge", "교주의 허점", "", "교주와 겨룰 때 유리하다"),
        new Entry("f_valleyroute", "북쪽 골짜기 샛길", "소금길, 나루 윗길, 폐사. 횃불이 드나드는 길.", ""),
        new Entry("f_roadinfo", "소금길", "교가 막은 관도 대신 장사치들만 아는 길.", ""),
        new Entry("f_taxroute", "징세조의 은자 길", "보름 장날, 소금길 나루 윗길, 호위 둘.", ""),
        new Entry("f_amhwarn", "암향루의 향", "단내 나는 쪽이 독, 쓴내 나는 쪽이 약.", ""),
        new Entry("f_spyinfo", "교 안의 불만", "교주를 원치 않는 자들이 당신을 지켜본다.", ""),
        new Entry("f_relicinfo", "북쪽 짐", "보름에 한 번 북쪽에서 짐이 내려온다. 끌려간 사람만 빼고.", ""),
        new Entry("f_beggarnet", "거지의 눈", "담 밑을 세는 붉은 끈. 뒷문 담은 낮다.", ""),
        new Entry("f_deadrumor", "사망 도장", "수배서에 '死' 자가 찍혔다. 며칠은 조용할 것이다.", ""),
        new Entry("g_redverse", "적운의 구결 · 전생", "우물 속에서 백 번 들은 말. 죽어서도 잃어버리지 말라 했다.", "회귀해도 남는다"),
        new Entry("f_gaeknot", "꼬지의 매듭", "하나는 낯선 칼, 둘은 관아, 셋은 늑대. 엎어 둔 그릇은 이 길로 다니지 말라는 뜻.", ""),
        new Entry("f_maengdeal", "맹의 거래", "진백의 목과 청운의 현판. 백리휘의 장부에 적혔다.", ""),
        new Entry("f_platefake", "별 매듭의 패 끈", "강에 떠오른 표사의 패 끈. 서열각 장부의 얼굴 칸은 남의 이름이다.", ""),
        new Entry("f_hapbyeok", "운검합벽(雲劍合璧)", "두 사람이 하나의 초식을 나누어 쓰는 검. 등을 맡긴 검이 곁에 있어야 한다.", ""),
        new Entry("f_finger", "잘린 새끼손가락", "왕삼의 장부에서 이름 한 줄이 지워진 값.", "검술 −3"),
        new Entry("f_wangsam", "왕삼의 장부", "빚쟁이들을 마당에 눕혔다. 장부는 아직 어딘가에 있다.", ""),
    };
    public static readonly Entry[] Bonds = {
        new Entry("f_soastitch", "소아 · 꿰맨 자리", "삐뚤게 꿰맨 일곱 땀. 그녀 장부의 첫 '빚 아님'.", ""),
        new Entry("f_soachild", "적운의 아이", "사슬 창고에서 데려온 아이. 그녀 그릇을 통째로 받았다.", "혈쇄문주와 겨룰 때 유리하다"),
        new Entry("f_amhname", "설(雪)", "루에서 부르는 이름 말고. 그 이름을 부르는 사람은 동생 하나였다.", ""),
        new Entry("f_amhturn", "침향 한 대", "매화는 오늘 갈 손님, 침향은 살려 보낼 손님.", ""),
        new Entry("f_yeonsaved", "연(蓮)", "매화 향은 비싸고 침향은 싸요. 언니는 침향을 더 많이 팔아요.", ""),
        new Entry("f_yongcrew", "나루의 지게꾼들", "가난한 짐은 삯을 안 받는다. 지게꾼끼리 다시 세운 법.", "야습과 몰이의 밤, 골목 어귀를 막아 준다"),
        new Entry("f_hajindot", "장부의 점", "점 찍은 칸은 돌아온다는 뜻이오. 이 장부에 점 찍힌 칸은 객이 처음이오.", ""),
        new Entry("f_gwakdinner", "곽산의 국", "둘이 와야 국이 준다.", ""),
        new Entry("f_gwakwifesaved", "석 달 치 약값", "판돈이오. 다음 비무판에서 내가 지면 갚으시오.", ""),
        new Entry("f_cmfiresaved", "그을린 법당 귀퉁이", "둘이 막았어요. 그 밤엔 혼자였는데.", ""),
        new Entry("f_yongspear", "작대기와 창", "짐 지는 손은 위로 받치고, 창 쥐는 손은 앞으로 민다.", "야습과 몰이의 밤, 지게꾼들의 작대기가 골목을 엇대어 막는다"),
        new Entry("f_hajingrave", "성 밖 언덕의 봉분", "주판 소리를 좋아하던 사람. 객잔이 망하지 않고 있다는 소리.", ""),
        new Entry("f_northvill", "우물가의 노파", "먹을 걸 주는 걸 보니 교 사람이 아니구려.", ""),
        new Entry("f_twoledgers", "창틀과 지붕", "처음엔 둘 다 이 사내 목을 노렸다. 지금은 하나는 맥을 짚고, 하나는 지붕을 건넌다.", ""),
        new Entry("f_amhnames", "향로 앞의 이름들", "경은 아무 이름도 안 부르거든요. 그래서 이름을 불렀다. 서른을 넘겨서.", ""),
        new Entry("f_wolfboy", "늑대 가죽의 아이", "발목을 묶어 보냈다. 칼보다 먼저 이름을 대라고 배운 아이.", "혈천진의 길을 안다"),
        new Entry("f_jbheard", "보름 밤의 목소리", "사부님, 저는 이번에도 문을 열어야 합니까.", ""),
        new Entry("f_amhwait", "남쪽의 약조", "기다리는 건 잘해요. 루에서 평생 그것만 했으니까요.", "교주와 겨룰 때 한 번 돕는다"),
        new Entry("f_cmsword", "막내의 목검", "입문하던 날 대사형이 깎아 준 것. 그 밤 그가 품에 넣고 문을 열었다.", ""),
        new Entry("f_cmally", "초막의 약조", "목은 올리지 마세요. 대신 살아서 오세요.", "교주와 겨룰 때 유리하다"),
        new Entry("f_maengvow", "백리휘의 약조", "'이번엔 늦지 않겠소. 내 이름으로.'", "그 밤 야습에 원군이 온다"),
        new Entry("f_maenglord", "맹의 칼 셋", "혈천진의 세 눈에 하나씩 들겠다고 했다.", "혈천진을 함께 뚫는다"),
        new Entry("f_namgungdoor", "남궁현의 측문", "'그날 내가 문 앞에 서 있다면, 그건 그대를 막으려는 게 아니오.'", "교주와 겨룰 때 한 번 돕는다"),
        new Entry("f_haoowe", "다락의 빚", "'하오문은 빚을 장부에 안 적소. 머리에 적지.'", "관아 그물을 먼저 귀띔받는다"),
        new Entry("f_gaerice", "다리 밑의 솥", "밥 먹었소? 아직이오. 그럼 앉으시오.", "굶는 날 한 그릇을 얻는다"),
        new Entry("f_alms", "저잣거리 거지 아이", "은자 한 냥. '뒷골목 담은 낮소.'", ""),
        new Entry("f_thiefkid", "소매치기 아이", "쫓아오지 않은 어른을 아이는 기억한다.", ""),
        new Entry("f_soabond", "소아 · 약속", "같은 달 아래.", ""),
        new Entry("f_soaseal", "소아의 봉인법", "쌍검의 문파가 쓰는 봉인.", "저주가 더디게 자란다"),
        new Entry("f_herbbond", "백초 할멈 · 지게", "산허리까지 반나절. 청운 사람들이 내 약을 많이 사갔지.", ""),
        new Entry("f_herbfriend", "백초 할멈 · 단골", "", ""),
        new Entry("f_hajinfree", "하진무 · 빈칸 방값", "셈이 안 맞는 방은 빈칸으로 둔다.", "방값을 받지 않는다"),
        new Entry("f_sparfriend", "낭인 마충", "돌아오면 백 합.", ""),
        new Entry("f_guardfriend", "표국 염가", "총표두 자리를 비워 두겠소.", ""),
        new Entry("f_rivalfriend", "곽산", "창을 쓰는 장신의 호적수.", "쫓기는 밤, 등을 받쳐 준다"),
        new Entry("f_jangfriend", "쥐눈이 장", "소식을 사고파는 사이.", ""),
        new Entry("f_ferryfree", "사공의 약속", "삿대질 값은 안 받겠소.", "그물이 닫힐 때 밤배를 그냥 태워 준다"),
        new Entry("f_vigil", "마당의 배웅", "아무도 가지 말라고 하지 않았다.", ""),
        new Entry("f_soablades", "적운의 쌍검 한 자루", "그녀가 메고 다니던 무게.", ""),
        new Entry("f_sparedally", "살려 보낸 비무꾼", "'살려 준 목숨, 한 번은 갚겠소.'", "위태로울 때 한 번 돕는다"),
        new Entry("f_hajinhurt", "하진무 · 무명 감은 손목", "그는 아무 말도 하지 않는다.", ""),
    };

    static MolakBag inst;
    static Texture2D px;
    Font gowun; GUIStyle sHead, sName, sDesc, sEff, sSec, sHint, sHintName, sTab; float builtU = -1f;
    int tab = -1;   // 펼친 행낭의 탭(0 물건 · 1 비방·소식 · 2 인연), -1 이면 가장 최근에 얻은 쪽
    float openT; bool open;
    // 목록 스크롤 (직접 구현: 손가락 드래그 + 관성)
    float listScroll, listVel, listMax; bool listDragging; float lastDragY; float lastDragT;
    // 획득 순서 추적
    readonly List<string> order = new List<string>();
    readonly Dictionary<string, bool> had = new Dictionary<string, bool>();
    float pollT;

    public static bool IsOpen => inst != null && inst.open;
    /// <summary>목록이 맨 위에 있으면(또는 짧으면) 아래로 쓸어 닫을 수 있다</summary>
    public static bool ListAtTop => inst == null || inst.listScroll <= 2f;
    public static float PanelTopPx => inst == null ? Screen.height : inst.panelTopPx;
    float panelTopPx;
    public static void Open () { if (inst != null && !MolakMenu.IsOpen) { inst.open = true; inst.listScroll = 0f; inst.listVel = 0f; inst.tab = -1; } }
    public static void Close () { if (inst != null) inst.open = false; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot ()
    {
        if (inst) return;
        var go = new GameObject("MolakBag"); DontDestroyOnLoad(go);
        inst = go.AddComponent<MolakBag>();
    }

    static Texture2D Px { get { if (px == null) { px = new Texture2D(1,1); px.SetPixel(0,0,Color.white); px.Apply(); } return px; } }
    static void Box (Rect r, Color c) { var g = GUI.color; GUI.color = new Color(c.r, c.g, c.b, c.a * g.a); GUI.DrawTexture(r, Px); GUI.color = g; }
    static float Get (string n) { try { return MolakStats.Get(n); } catch { return 0f; } }

    void EnsureStyles (float u)
    {
        if (sName != null && Mathf.Approximately(builtU, u)) return;
        builtU = u;
        if (gowun == null) gowun = Resources.Load<Font>("GowunBatang");
        System.Func<int, TextAnchor, GUIStyle> mk = (size, an) => { var s = new GUIStyle(GUI.skin.label){ fontSize = Mathf.RoundToInt(size*u), alignment = an, richText = true, wordWrap = true }; if (gowun != null) s.font = gowun; return s; };
        sHead = mk(40, TextAnchor.MiddleCenter);
        sSec = mk(26, TextAnchor.MiddleLeft);
        sName = mk(36, TextAnchor.UpperLeft);
        sDesc = mk(28, TextAnchor.UpperLeft);
        sEff = mk(27, TextAnchor.UpperLeft);
        sTab = mk(30, TextAnchor.MiddleCenter);
        sHint = mk(28, TextAnchor.MiddleCenter);
        sHintName = mk(40, TextAnchor.MiddleCenter);
    }

    Entry? Find (string flag)
    {
        foreach (var t in new[]{ Items, Lore, Bonds }) foreach (var e in t) if (e.Flag == flag) return e;
        return null;
    }

    Rect listViewGui; bool viewValid; float lastPY; bool wasDownBag;
    static Vector2 PointerGui (out bool down)
    {
        down = false; Vector2 p = new Vector2(-1f, -1f);
        var ts = UnityEngine.InputSystem.Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.isPressed) { down = true; p = ts.primaryTouch.position.ReadValue(); }
        else if (UnityEngine.InputSystem.Mouse.current != null) { var ms = UnityEngine.InputSystem.Mouse.current; down = ms.leftButton.isPressed; p = ms.position.ReadValue(); }
        return new Vector2(p.x, Screen.height - p.y);
    }
    void ScrollStep (float dt)
    {
        bool down; var p = PointerGui(out down);
        float u = MolakTheme.U;
        if (down && !wasDownBag && viewValid && listViewGui.Contains(p)) { listDragging = true; lastPY = p.y; listVel = 0f; }
        if (listDragging)
        {
            if (!down) listDragging = false;
            else
            {
                float dy = p.y - lastPY; lastPY = p.y;
                float over = listScroll < 0f ? -listScroll : (listScroll > listMax ? listScroll - listMax : 0f);
                float k = 1f / (1f + over / (34f*u));          // 끝을 넘으면 빠르게 무거워짐 — 행낭은 '여기가 끝'이라는 손맛만
                listScroll -= dy * k;
                float inst = dt > 0f ? -dy / dt : 0f;
                listVel = Mathf.Lerp(listVel, inst, 0.35f);
                listVel = Mathf.Clamp(listVel, -7000f*u, 7000f*u);
            }
        }
        if (!listDragging)
        {
            listScroll += listVel * dt;
            listVel *= Mathf.Pow(0.15f, dt);                   // 완만한 감속
            if (listScroll < 0f) { listVel = listVel < 0f ? listVel * Mathf.Pow(0.0001f, dt) : listVel; listScroll = Mathf.Lerp(listScroll, 0f, 1f - Mathf.Pow(0.002f, dt)); if (listScroll > -0.5f && listVel <= 0f) listScroll = 0f; }
            else if (listScroll > listMax) { listVel = listVel > 0f ? listVel * Mathf.Pow(0.0001f, dt) : listVel; listScroll = Mathf.Lerp(listScroll, listMax, 1f - Mathf.Pow(0.002f, dt)); if (listScroll < listMax + 0.5f && listVel >= 0f) listScroll = listMax; }
            if (Mathf.Abs(listVel) < 4f*u) listVel = 0f;
        }
        listScroll = Mathf.Clamp(listScroll, -70f*u, listMax + 70f*u);
        wasDownBag = down;
    }

    void Update ()
    {
        if (MolakMenu.IsOpen && open) open = false;
        openT = Mathf.MoveTowards(openT, open ? 1f : 0f, Time.unscaledDeltaTime * 6f);
        // 부드러운 스크롤: 손가락을 1:1로 따라가고, 놓으면 관성으로 미끄러지며 끝에서는 고무줄처럼 되돌아온다
        if (open) ScrollStep(Mathf.Min(Time.unscaledDeltaTime, 0.05f)); else { listDragging = false; listVel = 0f; viewValid = false; }
        // 획득 순서 추적 (초당 4회)
        pollT -= Time.unscaledDeltaTime;
        if (pollT <= 0f && Engine.Initialized)
        {
            pollT = 0.25f;
            foreach (var t in new[]{ Items, Lore, Bonds })
                foreach (var e in t)
                {
                    bool now = Get(e.Flag) > 0f;
                    bool before; had.TryGetValue(e.Flag, out before);
                    if (now && !before) { order.Remove(e.Flag); order.Add(e.Flag); }
                    if (!now && before) order.Remove(e.Flag);
                    had[e.Flag] = now;
                }
        }
    }

    void OnGUI ()
    {
        if (!Engine.Initialized) return;
        float u = MolakTheme.U;
        EnsureStyles(u);
        float w = Screen.width, h = Screen.height;
        bool night = MolakTheme.Night;
        float day = Get("day");

        // ---- 닫힘: 하단 한 줄 (최근 획득 1개 + 손잡이) ----
        if (openT <= 0.001f)
        {
            panelTopPx = h;
            if (day < 1f || MolakMenu.IsOpen) return;
            // 하단 띠: 손잡이 + '행낭' + 최근 획득 + 개수 — 위로 쓸어 올리면 열림
            var hintCol = night ? new Color(0.86f,0.82f,0.75f,1f) : new Color(0.20f,0.16f,0.13f,1f);
            var subCol = night ? "#B8AE9E" : "#5C5046";
            string latest = order.Count > 0 ? order[order.Count - 1] : null;
            var le = latest != null ? Find(latest) : null;
            sHint.normal.textColor = hintCol; sHintName.normal.textColor = hintCol;
            float bandH = MolakLayout.BagStripH*u, by0 = h - bandH;   // 띠 아래 절반은 기기 막대 여백, 글은 위 절반
            Box(new Rect(0, by0, w, bandH), night ? new Color(0.10f,0.09f,0.085f,0.94f) : new Color(0.93f,0.90f,0.84f,0.94f));
            Box(new Rect(0, by0, w, Mathf.Max(1f, 2*u)), night ? new Color(0.45f,0.40f,0.35f,0.9f) : new Color(0.55f,0.48f,0.40f,0.7f));
            Box(new Rect(w/2f - 56*u, by0 + 12*u, 112*u, 7*u), night ? new Color(0.7f,0.65f,0.58f,0.9f) : new Color(0.35f,0.30f,0.25f,0.75f));
            int nI = 0, nL = 0, nB = 0;
            foreach (var e0 in Items) if (Get(e0.Flag) > 0f) nI++;
            foreach (var e0 in Lore) if (Get(e0.Flag) > 0f) nL++;
            foreach (var e0 in Bonds) if (Get(e0.Flag) > 0f) nB++;
            string head = "<b>행낭</b>";
            string body;
            if (le.HasValue)
            {
                var e = le.Value;
                body = head + "   <color=" + subCol + ">최근</color>  " + e.Name + (string.IsNullOrEmpty(e.Effect) ? "" : "  <color=#B08A1E>" + e.Effect + "</color>");
            }
            else body = head + "   <color=" + subCol + ">비어 있다</color>";
            GUI.Label(new Rect(0, by0 + 26*u, w, 64*u), body, sHintName);
            GUI.Label(new Rect(0, by0 + 88*u, w, 44*u), "<color=" + subCol + ">물건 " + nI + "  ·  비방·소식 " + nL + "  ·  인연 " + nB + "      위로 올려 열기</color>", sHint);
            return;
        }

        // ---- 열림 ---- (0.4.25: 거의 온 화면, 물건/비방·소식/인연 탭, 항목마다 카드 한 장)
        MolakInputGuard.Guard(new Rect(0, 0, w, h));
        float panelH = h * 0.84f;
        float y = h - panelH * Mathf.SmoothStep(0f, 1f, openT);
        panelTopPx = y;
        Box(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, 0.45f * openT));
        var panel = new Rect(0, y, w, panelH);
        var ev = Event.current;
        if (ev.type == EventType.MouseDown && !panel.Contains(ev.mousePosition)) { open = false; ev.Use(); }

        var paper = night ? new Color(0.09f, 0.08f, 0.075f, 1f) : new Color(0.965f, 0.945f, 0.905f, 1f);
        Box(panel, paper);
        Box(new Rect(0, y, w, 3*u), MolakTheme.Blood);
        Box(new Rect(w/2f - 60*u, y + 14*u, 120*u, 6*u), night ? new Color(0.6f,0.55f,0.5f,0.8f) : new Color(0.35f,0.3f,0.25f,0.6f));
        var ink = night ? new Color(0.92f, 0.89f, 0.84f, 1f) : new Color(0.13f, 0.10f, 0.08f, 1f);
        var ink3 = night ? new Color(0.74f, 0.70f, 0.64f, 1f) : new Color(0.30f, 0.26f, 0.22f, 1f);
        var effC = night ? new Color(0.88f, 0.72f, 0.36f, 1f) : new Color(0.52f, 0.36f, 0.05f, 1f);
        sHead.normal.textColor = ink; sSec.normal.textColor = MolakTheme.Blood; sName.normal.textColor = ink; sDesc.normal.textColor = ink3; sEff.normal.textColor = effC;
        GUI.Label(new Rect(0, y + 30*u, w, 64*u), "— 행낭(行囊) —", sHead);
        var xr = new Rect(w - 130*u, y + 20*u, 110*u, 84*u);
        var xs = new GUIStyle(sHead){ fontSize = Mathf.RoundToInt(44*u) };
        GUI.Label(xr, "×", xs);
        if (ev.type == EventType.MouseDown && xr.Contains(ev.mousePosition)) { open = false; ev.Use(); return; }

        float cx = Mathf.Max(36*u, (w - Mathf.Min(w*0.92f, 960*u))/2f), cw = w - 2*cx;
        // ---- 탭 ----
        var sections = new[] { "물건", "비방 · 소식", "인연" };
        var tables = new[] { Items, Lore, Bonds };
        var accents = new[] { MolakTheme.Blood, new Color(0.36f, 0.48f, 0.66f, 1f), MolakTheme.Gold };
        var have = new List<Entry>[3]; int newest = -1, newestIdx = -1;
        for (int s = 0; s < 3; s++)
        {
            have[s] = new List<Entry>();
            foreach (var e in tables[s]) if (Get(e.Flag) > 0f) { have[s].Add(e); int oi = order.IndexOf(e.Flag); if (oi > newestIdx) { newestIdx = oi; newest = s; } }
            have[s].Sort((a, b) => order.IndexOf(b.Flag).CompareTo(order.IndexOf(a.Flag)));   // 최근이 위
        }
        if (tab < 0 || have[tab].Count == 0) { tab = newest >= 0 ? newest : 0; if (have[tab].Count == 0) for (int s = 0; s < 3; s++) if (have[s].Count > 0) { tab = s; break; } }
        float ty = y + 108*u, th = 78*u, tw = cw / 3f;
        Box(new Rect(cx, ty + th, cw, Mathf.Max(1f, 2*u)), new Color(ink3.r, ink3.g, ink3.b, 0.3f));
        for (int s = 0; s < 3; s++)
        {
            var tr = new Rect(cx + tw*s, ty, tw, th);
            bool on = s == tab, empty = have[s].Count == 0;
            if (on) { Box(new Rect(tr.x + 4*u, tr.y, tr.width - 8*u, tr.height), night ? new Color(0.16f,0.14f,0.13f,1f) : new Color(0.91f,0.88f,0.82f,1f)); Box(new Rect(tr.x + 4*u, tr.yMax - 4*u, tr.width - 8*u, 4*u), accents[s]); }
            var c = on ? ink : new Color(ink3.r, ink3.g, ink3.b, empty ? 0.4f : 0.9f);
            sTab.normal.textColor = c;
            GUI.Label(tr, sections[s] + "  <size=" + Mathf.RoundToInt(24*u) + ">" + have[s].Count + "</size>", sTab);
            if (ev.type == EventType.MouseDown && tr.Contains(ev.mousePosition) && !empty) { if (tab != s) { tab = s; listScroll = 0f; listVel = 0f; } ev.Use(); }
        }

        // ---- 목록 (최근 획득 순, 카드) ----
        float listY = ty + th + 18*u;
        float bottomPad = (MolakLayout.BagStripH - MolakLayout.BagStripContentH)*u;   // 기기 막대 여백
        float listH = y + panelH - bottomPad - 12*u - listY;
        var list = have[tab];
        if (list.Count == 0)
        {
            sDesc.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(cx, listY + 40*u, cw, 140*u), "아직 아무것도 없다.\n'빈 행낭이 가장 가볍다.' 사형이 하던 말이다.", sDesc);
            sDesc.alignment = TextAnchor.UpperLeft;
            listMax = 0f; viewValid = false;
            return;
        }
        float total = 0f;
        foreach (var e in list) total += RowH(e, cw, u) + 14*u;
        listMax = Mathf.Max(0f, total + 24*u - listH);
        var view = new Rect(cx, listY, cw, listH);
        listViewGui = view; viewValid = true;   // 드래그는 Update(ScrollStep)에서 입력 시스템으로 처리
        var card = night ? new Color(0.135f, 0.12f, 0.11f, 1f) : new Color(0.995f, 0.985f, 0.96f, 1f);
        var edge = new Color(ink3.r, ink3.g, ink3.b, night ? 0.25f : 0.22f);
        GUI.BeginGroup(view);
        float ry = -listScroll, pad = 26*u, inW = cw - pad*2 - 8*u;
        foreach (var e in list)
        {
            float rh = RowH(e, cw, u);
            if (ry + rh > 0 && ry < listH)
            {
                var r = new Rect(0, ry, cw, rh);
                Box(r, card);
                Box(new Rect(r.x, r.y, r.width, Mathf.Max(1f, 1*u)), edge); Box(new Rect(r.x, r.yMax - Mathf.Max(1f, 1*u), r.width, Mathf.Max(1f, 1*u)), edge);
                Box(new Rect(r.x, r.y, 6*u, r.height), accents[tab]);
                float yy = ry + 18*u, xx = pad + 8*u;
                GUI.Label(new Rect(xx, yy, inW, 48*u), e.Name, sName);
                yy += 50*u;
                if (!string.IsNullOrEmpty(e.Effect)) { float eh = sEff.CalcHeight(new GUIContent("▸ " + e.Effect), inW); GUI.Label(new Rect(xx, yy, inW, eh), "▸ " + e.Effect, sEff); yy += eh + 6*u; }
                if (!string.IsNullOrEmpty(e.Desc)) GUI.Label(new Rect(xx, yy, inW, ry + rh - yy), e.Desc, sDesc);
            }
            ry += rh + 14*u;
        }
        GUI.EndGroup();
        // 위/아래 페이드: 잘린 게 아니라 더 있다는 표시
        int steps = 14; float fh = 64*u / steps;
        if (listScroll > 2f) for (int i = 0; i < steps; i++) Box(new Rect(cx, listY + i*fh, cw, fh + 1f), new Color(paper.r, paper.g, paper.b, 1f - (float)i/steps));
        if (listScroll < listMax - 2f)
        {
            for (int i = 0; i < steps; i++) Box(new Rect(cx, listY + listH - (i+1)*fh, cw, fh + 1f), new Color(paper.r, paper.g, paper.b, 1f - (float)i/steps));
        }
        // 목록 아래(기기 막대 자리)는 종이로 덮어 경계를 깔끔하게, 더 있으면 그 자리에 한 줄
        Box(new Rect(0, listY + listH, w, h - (listY + listH)), paper);
        if (listScroll < listMax - 2f)
        {
            sTab.normal.textColor = new Color(ink3.r, ink3.g, ink3.b, 0.9f);
            GUI.Label(new Rect(cx, listY + listH + 4*u, cw, 44*u), "<size=" + Mathf.RoundToInt(25*u) + ">▾ 아래에 더 있다</size>", sTab);
        }
    }

    float RowH (Entry e, float cw, float u)
    {
        float inW = cw - 26*u*2 - 8*u;
        float eh = string.IsNullOrEmpty(e.Effect) ? 0f : sEff.CalcHeight(new GUIContent("▸ " + e.Effect), inW) + 6*u;
        float dh = string.IsNullOrEmpty(e.Desc) ? 0f : sDesc.CalcHeight(new GUIContent(e.Desc), inW);
        return 18*u + 50*u + eh + dh + 22*u;
    }
}
