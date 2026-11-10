using Naninovel;
using UnityEngine;

/// <summary>
/// 전생의 앎 — 되감겨도 남는 것들의 장부. 키는 전역(g_) 변수이고, 여러 키 중 하나라도 0보다 크면 안다.
/// 메뉴 「전생의 앎」 탭(MolakMenu.DrawKnow)과, 죽은 직후 "되감기지 않는 것" 한 줄(@knownew)이 이 표를 쓴다.
/// </summary>
public static class MolakKnow
{
    public struct Entry
    {
        public string Keys, Name, Desc, Where;
        public Entry (string k, string n, string d, string w) { Keys = k; Name = n; Desc = d; Where = w; }
    }

    public static readonly Entry[] All =
    {
        new Entry("g_runs",                    "되감긴 아침",      "하진무의 여식은 시집가서 둘째를 낳았다. 노인이 아직 하지 않은 이야기다.",           "운락객잔"),
        new Entry("g_end_ohin",                "젓가락 끝",        "'마지막이다' 뒤에는 칼이 온다. 어디서, 어느 각도로 오는지 목이 기억한다.",           "저잣거리 국밥집"),
        new Entry("g_soaname",                 "그 이름",          "연(蓮). 사부만 그리 불렀다. 부르는 사람이 둘이 되었다.",                             "저잣거리 국밥집"),
        new Entry("g_know_raid",               "그 밤",            "마흔이틀째 삼경. 혈랑대 열넷이 기와를 밟고 와서 계단으로 오른다.",                   "운락객잔"),
        new Entry("g_soadied",                 "그녀가 죽은 밤",   "등 뒤를 맡긴다던 사람이 먼저 갔다.",                                                   "운락객잔"),
        new Entry("g_redverse",                "적운의 구결",      "우물 속에서 백 번 들은 열두 줄. 청운의 열두 줄 위에 겹쳐 외운다.",                    "운검사태의 사당"),
        new Entry("g_know_cup|g_end_jagaek",   "매화 향 술",       "이 철에 매화는 없다. 사내의 칼은 왼 소매에 있다.",                                     "주막 구석"),
        new Entry("g_know_taxday|g_end_hyeop", "징세조가 오는 날", "해가 중천에 오르기 전, 붉은 끈 다섯. 우두머리는 칼을 뽑을 때 왼 무릎부터 굽힌다.",    "운락객잔 마당"),
        new Entry("g_know_wangsam|g_end_chae", "왕삼의 장부",      "궤짝 셋째 칸. 붓끝이 청운 쪽 이름 위에서 한 번 멈춘다.",                               "전당포 뒷방"),
        new Entry("g_end_wonsu",               "상복의 원한",      "늘인 목 하나의 값으로 상복 셋이 지붕을 넘어온다.",                                     "비무판, 그리고 그 뒤"),
        new Entry("g_end_ik",                  "개울의 여울목",    "물이 얕아지는 자리는 하류에 있다. 뛰어들지 않아도 된다.",                              "불어난 개울"),
        new Entry("g_end_amhyang|g_know_amhkey","단내 나는 술",    "암향의 손목에도 검푸른 실핏줄이 있다. 손님 하나에 해약 한 첩.",                        "주막의 밤"),
        new Entry("g_know_amhsister",          "향고 옆방",        "열두 살 아이. 바깥에서만 잠기는 문. 삼경에 문지기가 바뀐다.",                          "암향루"),
        new Entry("g_know_soa",                "적운의 쌍검",      "적운의 검은 두 번 오지 않는다. 한 번에 둘이 온다.",                                    "소아와 칼을 맞댈 때"),
        new Entry("g_know_hochi",              "뼈 장식 소리",     "독아단 아장 호치의 첫 수. 뼈 장식이 짤랑이고 반 호흡 뒤에 도끼가 온다.",               "회목촌"),
        new Entry("g_know_kyona",              "골짜기의 개",      "독아단주 견아의 첫 수.",                                                               "북쪽 골짜기"),
        new Entry("g_know_wolf|g_know_chainmob","사슬의 버릇",     "사슬이 왼 어깨를 돌 때, 오른 무릎이 반 호흡 늦게 따라온다.",                           "혈쇄문"),
        new Entry("g_know_jinbaek",            "대사형의 검",      "막내라고 부를 때만 검이 반 치 늦다.",                                                  "혈랑대"),
        new Entry("g_know_hunt1|g_know_hunt2", "혈랑대의 그물",    "척후조가 먼저 오고, 정예는 등 뒤에서 온다.",                                           "관도"),
        new Entry("g_know_hunt3",              "집법의 검",        "집법전주의 첫 수.",                                                                    "집법전"),
        new Entry("g_know_cheolsan",           "철산도",           "표국 어음을 품은 칼잡이의 첫 수.",                                                     "산길 주막"),
        new Entry("g_know_rival",              "곽산의 창",        "창이 길어서 첫 수가 멀다. 그다음은 가깝다.",                                           "비무판"),
        new Entry("g_jbtruth",                 "산문을 연 까닭",   "열면 네 사매 하나는 산다. 그 사매는 산사에서 머리를 깎았다. 보름마다 쌀자루가 놓인다.", "산사, 그리고 사당"),
        new Entry("g_know_hajin",              "관아 대문 앞",     "하진무는 대문을 끝내 두드리지 못했다. 시집간 딸의 둘째가 징세 명부의 담보다.",      "운락객잔 뒷문"),
        new Entry("g_hajindied",               "셈이 맞던 날",     "노인은 당신 앞에 서서 셈을 맞췄다. 주판알이 피 웅덩이로 굴러갔다.",                    "운락객잔 마당"),
        new Entry("g_know_jangsell",           "판 소식",          "열하루째 밤 뒷문에 서 있던 사람이 이 방을 판다. 서른 냥에.",                         "운락객잔의 삼경"),
        new Entry("g_know_hao",                "담보 칸의 딸",     "쥐눈이 장의 딸, 열 살. 징세조 장부의 담보 칸. 교는 이름을 지우지 않고 줄을 하나 더 긋는다.", "주루 계단 밑"),
        new Entry("g_know_maeng",              "그 밤의 수레",     "원군은 남궁의 피난 수레 열두 대를 지키고 있었다. 산 아래, 반 시진 거리.",            "성 서쪽 폐사찰"),
        new Entry("g_maenglate",               "반 시진",          "백리휘는 피가 다 식은 새벽에 온다. 칼에 피 한 방울 없이.",                            "야습 뒤의 객잔 마당"),
        new Entry("g_know_maengsell",          "맹의 저울",        "청운 막내 하나에 맹 잔당 일곱. 저울을 단 사람이 직접 일러 주러 왔다.",                "객잔 뒷문"),
        new Entry("g_know_namgung",            "닳은 패",          "남궁현은 패를 엄지로 문지른다. 던지고 싶은 손이다.",                                  "남궁현과 칼을 맞댈 때"),
        new Entry("g_know_gaeknot",            "매듭 셋",          "하나는 낯선 칼, 둘은 관아, 셋은 늑대. 엎은 그릇은 이 길로 다니지 마라.",               "담장 말뚝"),
        new Entry("g_know_yong",               "회목채의 누이",    "용개의 누이와 다섯 살 조카가 회목채에 있다. 견아가 당신의 길 하나와 바꾸자 했다.",    "객잔 뒷문, 북쪽 산길"),
        new Entry("g_know_gwak",               "푸른 창날",        "곽산의 아내가 피를 토한다. 백초당 외상이 막혔다. 독은 교 약재 창고 뒷문에서 샀다.", "객잔 앞 공터"),
        new Entry("g_gwakwifedied",            "거꾸로 꽂힌 창",   "독칼을 들춘 생, 약값이 끊긴 지 이레 만에 곽산의 아내가 죽었다.",                     "저자 공터"),
        new Entry("g_herbdied",                "백초당이 타던 밤", "꾸러미 속을 바꾸면 징세조가 보름 뒤에 알아챈다. 교납 장부의 무게 칸까지 바꿔야 한다.", "약재 거리"),
        new Entry("g_know_lord",               "교주의 첫 수",     "멸도의 벽이 어디서부터 오는지 몸이 안다.",                                             "혈마곡 대전"),
    };

    public static bool Known (Entry e)
    {
        foreach (var k in e.Keys.Split('|')) if (MolakStats.Get(k) > 0f) return true;
        return false;
    }

    public static int Count () { int n = 0; foreach (var e in All) if (Known(e)) n++; return n; }

    /// <summary>회차 시작 때 아는 것들을 적어 둔다(비전역 변수라 저장/불러오기에도 남는다).</summary>
    public static void Snapshot ()
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < All.Length; i++) if (Known(All[i])) sb.Append(i).Append(',');
        Engine.GetServiceOrErr<ICustomVariableManager>().SetVariableValue("knowsnap", new CustomVariableValue(sb.ToString()));
    }
}

/// <summary>@knownew — 이번 생에 새로 알게 된 것: knownew(이름들), knownewc(개수)</summary>
[Alias("knownew")]
public class KnowNewCommand : Command
{
#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        var vars = Engine.GetServiceOrErr<ICustomVariableManager>();
        string snap = "";
        try { var v = vars.GetVariableValue("knowsnap"); if (v.Type == CustomVariableValueType.String) snap = v.String; } catch { }
        var had = new System.Collections.Generic.HashSet<string>(snap.Split(','));
        var names = new System.Collections.Generic.List<string>();
        for (int i = 0; i < MolakKnow.All.Length; i++)
            if (MolakKnow.Known(MolakKnow.All[i]) && !had.Contains(i.ToString())) names.Add("「" + MolakKnow.All[i].Name + "」");
        vars.SetVariableValue("knownew", new CustomVariableValue(string.Join(" ", names.ToArray())));
        MolakStats.Set("knownewc", names.Count);
    }
}
