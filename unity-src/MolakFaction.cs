using UnityEngine;

/// <summary>
/// 강호록(江湖錄) — 만나 본 세력의 기록. 설명 대신 그 세력 사람이 직접 한 말을 모은다.
/// Key: 처음 만났을 때 켜지는 전역(g_) 변수(여러 개면 | 로, 비우면 처음부터 앎). Lines: 각 줄을 여는 g_ 변수와 그 사람이 한 말.
/// Rep: 이번 생의 평판 변수(rep_maeng 등), "rank" 는 서열, 빈 문자열이면 표시 안 함.
/// 메뉴 「강호록」 탭(MolakMenu.DrawFaction)이 그린다.
/// </summary>
public static class MolakFaction
{
    public struct Line { public string Key, Text, Who; public Line (string k, string t, string w) { Key = k; Text = t; Who = w; } }
    public struct Entry
    {
        public string Key, Name, Hanja, Motto, Where, Rep; public Line[] Lines;
        public Entry (string k, string n, string h, string m, string w, string r, params Line[] l) { Key = k; Name = n; Hanja = h; Motto = m; Where = w; Rep = r; Lines = l; }
    }

    public static readonly Entry[] All =
    {
        new Entry("", "청운검파", "靑雲劍派", "검은 늦게 뽑고 빨리 거둔다.", "운락 위의 산", "",
            new Line("", "검은 늦게 뽑고 빨리 거두라던 사람.", "장문인을 떠올리며"),
            new Line("g_jbtruth", "열면 네 사매 하나는 산다.", "그 밤, 산문 앞에서")),
        new Entry("g_fac_gyo|g_fac_gwan", "혈마교", "血魔敎", "피는 위로 흐른다.", "게시벽 앞, 서열각의 탁자", "rank",
            new Line("g_fac_gyo", "이름은 적지 않소. 숫자만 적소. 숫자는 거짓말을 안 하니까.", "서열각 감찰사"),
            new Line("g_fac_gyo", "패를 찬 자는 패를 찬 자에게 비무를 청할 수 있소. 청을 받은 자는 물리지 못하오.", "서열각 감찰사"),
            new Line("g_fac_gyo2", "패는 서열각 것이다. 몸은 네가 가져라.", "달아나던 혈랑대원")),
        new Entry("g_fac_gwan", "관아 · 징세조", "官衙", "현령의 도장은 교의 글씨 밑에 작게 찍힌다.", "운락 관아 앞 게시벽", "",
            new Line("g_fac_gwan", "쉿, 혈마교 일에 입 대지 말게.", "수배서 앞의 장꾼"),
            new Line("g_know_hajin", "시집간 딸의 둘째가 징세 명부의 담보다.", "하진무의 사정"),
            new Line("g_know_hao", "내 이름이 징세 장부에 있어서요. 아버지 이름 옆에.", "계단 밑의 아이")),
        new Entry("g_fac_maeng", "무림맹 잔당", "武林盟", "맹은 아직 있다. 장부도 아직 있다.", "운주 비무 뒤, 객잔 마당", "rep_maeng",
            new Line("g_fac_maeng", "그 검을 마지막으로 본 게 불길 속이었으니.", "백리휘"),
            new Line("g_fac_maeng", "맹은 약조를 장부에 적소. 맹주가 죽어도 장부는 남으니까.", "백리휘"),
            new Line("g_know_maeng", "수레 열두 대를 지켰고, 그 수레의 주인들은 석 달 뒤에 무릎을 꿇었소.", "백리휘"),
            new Line("g_know_maengsell", "나는 그 저울을 달고 왔소. 그리고 여기 서서 그대에게 일러 주고 있소.", "백리휘")),
        new Entry("g_fac_namgung", "남궁세가", "南宮世家", "검은 꺾이지 않았다. 무릎만 굽혔을 뿐이다.", "운주 들판 심판석", "",
            new Line("g_fac_namgung", "서열록에 없는 문파는 부르지 않소. 문파는 이 마당에서 쓸모가 없소.", "남궁현"),
            new Line("g_know_namgung", "이 목은 이미 교의 것이니까. ……틀렸소. 아직 반은 내 것이오.", "남궁현")),
        new Entry("g_fac_gae", "개방 운락분타", "丐幇", "밥 먹었소? 아직이오. 그럼 앉으시오.", "약재 거리 담벼락, 다리 밑", "rep_gae",
            new Line("g_fac_gae", "하나는 낯선 칼. 둘은 관아. 셋은 늑대요.", "꼬지"),
            new Line("g_fac_gae2", "받고 배부른 놈은 내일부터 거지가 아니다. 나는 안 말린다. 안 먹을 뿐이다.", "다리 밑의 거지 중")),
        new Entry("g_fac_hao", "하오문", "下五門", "소식은 한 번만 판다.", "주루 계단 밑", "rep_heuk",
            new Line("g_fac_hao", "소식은 한 번만 파오. 이 바닥의 법이오. 그러니 비싸지.", "쥐눈이 장"),
            new Line("g_fac_hao2", "산 사람의 소식은 팔아도 된다. 교에는 안 판다. 그게 우리 마지막 법이다.", "다락의 노파"),
            new Line("g_know_hao", "그래도 파는 거요. 아비니까.", "쥐눈이 장")),
        new Entry("g_fac_nok", "녹림 · 옛 회목채", "綠林", "가난한 마을은 털지 않는다.", "나루터 짐꾼 줄", "rep_heuk",
            new Line("g_fac_nok", "같은 채요. 이름만 바꿨지.", "용개"),
            new Line("g_fac_nok", "저는…… 손목이 아까워서 나왔습니다요. 법 때문이 아니라.", "용개")),
        new Entry("g_fac_dok", "독아단", "毒牙團", "새 독은 제 몸에 먼저 친다.", "회목촌 골목", "",
            new Line("g_fac_dok", "우리가 독아단인 줄 알고 이러는 거냐.", "쇠이를 박은 졸개"),
            new Line("g_fac_nok", "녹림 법 같은 소리 하지 마라. 이젠 우리가 법이다. 교가 그렇게 말했다.", "쇠이를 박은 졸개")),
        new Entry("g_fac_swe", "혈쇄문", "血鎖門", "빚은 사슬로 갚는다.", "왕삼 전당포, 현 외곽 폐철장", "",
            new Line("g_fac_swe", "왕삼 어른 댁 빚은 끝에 가서 고리로 셈하오. 고리 하나에 열 냥.", "왕삼의 수하"),
            new Line("g_fac_swe2", "일곱. 일곱 번만 더 이기면 나는 사람이 되오.", "사슬 찬 문도")),
        new Entry("g_fac_amh", "암향루", "暗香樓", "손님의 얼굴은 잊고, 손님의 향은 기억한다.", "주막의 밤", "",
            new Line("g_fac_amh", "혼자 잔을 드시는 등이 쓸쓸해 보이시어요.", "암향"),
            new Line("g_fac_amh2", "매화는 오늘 갈 손님, 침향은 살려 보낼 손님.", "암향")),
        new Entry("g_fac_pyo", "장풍표국", "長風鏢局", "표물은 목숨보다 무겁다.", "표국 앞", "",
            new Line("g_fac_pyo", "장풍— 길을 비키시오!", "표사들의 표호"),
            new Line("g_fac_pyo2", "삼십 년일세. 독아단한테도 안 뺏긴 걸 교한테 뺏겼네.", "총표두 염가")),
        new Entry("g_fac_dang", "사천당가 방계", "唐家", "독을 아는 손이 약도 안다.", "백초당", "",
            new Line("g_fac_dang", "이 독은 내가 처음 배운 독이다. 그러니 내가 제일 잘 푼다.", "백초 할멈")),
        new Entry("g_fac_jeok", "적운검파", "赤雲劍派", "두 번 오지 않는다. 한 번에 둘이 온다.", "저잣거리 국밥집", "",
            new Line("g_fac_jeok", "남의 그릇이 더 맛있지. 내 그릇은 늘 먼저 비어서.", "소아"),
            new Line("g_redverse", "우물 속에서 백 번 들은 열두 줄.", "그 밤의 소아")),
    };

    static bool On (string keys)
    {
        if (string.IsNullOrEmpty(keys)) return true;
        foreach (var k in keys.Split('|')) if (MolakStats.Get(k) > 0f) return true;
        return false;
    }

    public static bool Met (Entry e) { return On(e.Key); }
    public static bool LineOn (Line l) { return On(l.Key); }
    public static int Count () { int n = 0; foreach (var e in All) if (Met(e)) n++; return n; }

    /// <summary>이번 생의 평판 한 줄. 서열이면 숫자, 신의면 ●○ 다섯 칸.</summary>
    public static string RepText (Entry e)
    {
        if (string.IsNullOrEmpty(e.Rep)) return null;
        if (e.Rep == "rank")
        {
            float r = MolakStats.Get("rank");
            return r > 0 ? "이번 생 · 서열 " + string.Format("{0:N0}", r) + "위" : "이번 생 · 무명";
        }
        int v = Mathf.Clamp(Mathf.RoundToInt(MolakStats.Get(e.Rep)), 0, 5);
        var sb = new System.Text.StringBuilder("이번 생의 신의 ");
        for (int i = 0; i < 5; i++) sb.Append(i < v ? '●' : '○');
        return sb.ToString();
    }
}
