using Naninovel;
using UnityEngine;

/// <summary>말로(末路) 시스템 — 사인별 엔딩 판정·연출·영구 기록(말로록).</summary>
public class MolakEnd : MonoBehaviour
{
    public struct Entry { public string Id, Hanja, Kor, Title, Hint; public Entry(string i,string h,string k,string t,string n){Id=i;Hanja=h;Kor=k;Title=t;Hint=n;} }

    public static readonly Entry[] All = new Entry[]
    {
        new Entry("simma",     "心魔", "심마", "마음이 먼저 꺾였다",   "저주가 가득 찼을 때"),
        new Entry("gwaro",     "過勞", "과로", "검보다 몸이 먼저",     "몸을 돌보지 않았을 때"),
        new Entry("gongbok",   "空腹", "공복", "강호는 밥부터다",      "전낭이 비었을 때"),
        new Entry("hoengsa",   "橫死", "횡사", "이름 없는 검 아래",    "서열전에서 쓰러졌을 때"),
        new Entry("dokni",     "毒牙", "독아", "독니에 물리다",        "독아단의 아장과 싸웠을 때"),
        new Entry("gyeona",    "犬牙", "견아", "골짜기의 개",          "북쪽 골짜기의 주인과 싸웠을 때"),
        new Entry("soseul",    "鎖", "쇄",   "사슬에 감기다",        "사슬의 문파와 싸웠을 때"),
        new Entry("sahyeong",  "師兄", "사형", "대사형의 검",          "산문을 연 자와 마주했을 때"),
        new Entry("hyeolcheon","血天", "혈천", "하늘은 높았다",        "서열 일위에 도전했을 때"),
        new Entry("ohin",      "誤認", "오인", "같은 뿌리에 베이다",   "적운의 눈을 속이려 했을 때"),
        new Entry("jahae",     "自害", "자해", "제 검에 베이다",       "익지 않은 초식을 탐했을 때"),
        new Entry("amhyang",   "暗香", "암향", "향에 잠기다",          "암향루의 밤이 깊었을 때"),
        new Entry("sicheon",   "弑天", "시천", "하늘을 벤 자",         "혈천마를 베었을 때"),
        new Entry("donghaeng", "同行", "동행", "같은 달 아래",         "약속을 지키고 하늘을 베었을 때"),
        new Entry("seongsa",   "成邪", "성사", "열매가 익었다",        "노인을 본 뒤, 저주가 다 익었을 때"),
        new Entry("ipma",      "入魔", "입마", "무릎을 꿇은 검",       "교주의 부름에 응했을 때"),
        new Entry("wonhaeng",  "遠行", "원행", "복수보다 먼 길",       "약속한 사람과 강호를 등졌을 때"),
        new Entry("sungeom",   "殉劍", "순검", "베고, 쓰러지다",       "저주를 안은 채 하늘을 베었을 때"),
        new Entry("pobak",     "縛",   "포박", "사슬에 끌려가다",      "예순 날이 지나도록 끝을 내지 못했을 때"),
        new Entry("wonsu",     "怨讐", "원수", "벤 칼이 돌아오다",     "목을 친 자의 사람들이 찾아왔을 때"),
        new Entry("chae",      "債",   "채",   "빚은 목으로 갚는다",   "갚지 못할 빚을 졌을 때"),
        new Entry("jungdok",   "中毒", "중독", "피가 검게 식다",       "독을 품은 채 날을 보냈을 때"),
        new Entry("pasang",    "破傷", "파상", "상처가 먼저 죽었다",   "덧난 상처를 버려두었을 때"),
        new Entry("jagaek",    "刺客", "자객", "술잔 너머의 칼",       "교의 눈에 오래 띄었을 때"),
        new Entry("sagyeol",   "死決", "사결", "둘 중 하나만 걸어 나간다", "생사결에 응했을 때"),
        new Entry("hyeop",     "俠",   "협",   "남의 일에 칼을 뽑다",  "남을 위해 칼을 뽑았을 때"),
        new Entry("ik",        "溺",   "익",   "물이 먼저 받아 주었다", "불어난 물에 뛰어들었을 때"),
    };

    public static string Resolve ()
    {
        var vars = Engine.GetServiceOrErr<ICustomVariableManager>();
        string cause = vars.VariableExists("deathcause") ? vars.GetVariableValue("deathcause").String : "";
        string kind = vars.VariableExists("dkind") ? vars.GetVariableValue("dkind").String : "";
        if (cause == "등극") return MolakStats.Get("f_soabond") > 0 ? "donghaeng" : "sicheon";
        if (cause == "성사") return "seongsa";
        if (cause == "입마") return "ipma";
        if (cause == "원행") return "wonhaeng";
        if (cause == "순검") return "sungeom";
        if (cause == "포박") return "pobak";
        if (cause == "사술 폭주") return "simma";
        if (cause == "탈진") return "gwaro";
        if (cause == "아사") return "gongbok";
        if (cause == "소아의 오인" || cause == "소아의 검") return "ohin";
        if (cause == "미숙한 초식") return "jahae";
        if (cause == "미인계") return "amhyang";
        if (cause == "원수") return "wonsu";
        if (cause == "빚") return "chae";
        if (cause == "독") return "jungdok";
        if (cause == "곪은 상처") return "pasang";
        if (cause == "자객") return "jagaek";
        if (cause == "생사결") return "sagyeol";
        if (cause == "협") return "hyeop";
        if (cause == "익사") return "ik";
        if (cause == "비무 패배")
        {
            if (kind == "hochi") return "dokni";
            if (kind == "kyona") return "gyeona";
            if (kind == "wolf" || kind == "chainmob") return "soseul";
            if (kind == "jinbaek") return "sahyeong";
            if (kind == "lord") return "hyeolcheon";
            return "hoengsa";
        }
        return "hoengsa";
    }

    public static Entry Find (string id) { foreach (var e in All) if (e.Id == id) return e; return All[3]; }

    // ---------- 연출 ----------
    static MolakEnd instance;
    static Texture2D px;
    Font gowun;
    GUIStyle sHanja, sTitle, sNew;
    float builtU = -1f;
    static string cHanja = "", cKor = "", cTitle = ""; static bool cNew; static float cT = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap ()
    {
        if (instance != null) return;
        var go = new GameObject("MolakEnd");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<MolakEnd>();
    }

    public static void Show (string hanja, string kor, string title, bool isNew) { cHanja = hanja; cKor = kor; cTitle = title; cNew = isNew; cT = 0f; MolakSfx.Death(); }

    static Texture2D Px { get { if (px == null) { px = new Texture2D(1,1); px.SetPixel(0,0,Color.white); px.Apply(); } return px; } }
    void Box (Rect r, Color c) { var g = GUI.color; GUI.color = c; GUI.DrawTexture(r, Px); GUI.color = g; }

    void EnsureStyles (float u)
    {
        if (sHanja != null && Mathf.Approximately(builtU, u)) return;
        builtU = u;
        if (gowun == null) gowun = Resources.Load<Font>("GowunBatang");
        System.Func<int, Color, GUIStyle> mk = (size, col) =>
        {
            var s = new GUIStyle(GUI.skin.label){fontSize=Mathf.RoundToInt(size*u),alignment=TextAnchor.MiddleCenter,richText=true};
            if (gowun != null) s.font = gowun;
            s.normal.textColor = col; return s;
        };
        sHanja = mk(110, MolakTheme.Ink);
        sTitle = mk(34, MolakTheme.Ink3);
        sNew = mk(24, MolakTheme.Gold);
    }

    void OnGUI ()
    {
        if (cT < 0f) return;
        cT += Time.deltaTime;
        float dur = 3.4f, tail = 0.8f; // 카드 뒤 먹색 여운
        if (cT > dur + tail) { cT = -1f; return; }
        float u = MolakTheme.U;
        EnsureStyles(u);
        float w = Screen.width, h = Screen.height;
        float bgA = cT < 0.4f ? cT/0.4f : (cT > dur ? Mathf.Clamp01((dur + tail - cT)/tail) : 1f);
        Box(new Rect(0,0,w,h), new Color(0.03f,0.025f,0.02f, 0.86f*bgA));
        if (cT > dur) return;
        float a = cT < 0.4f ? cT/0.4f : (cT > dur-0.5f ? (dur-cT)/0.5f : 1f);
        var g = GUI.color; GUI.color = new Color(1,1,1,a);
        float cw = Mathf.Min(w*0.86f, 780*u), ch = 340*u;
        float cx = (w-cw)/2f, cy = h*0.28f;
        Box(new Rect(cx,cy,cw,ch), new Color(0.05f,0.04f,0.035f,0.96f));
        Box(new Rect(cx,cy,cw,2*u), MolakTheme.Blood);
        Box(new Rect(cx,cy+ch-2*u,cw,2*u), MolakTheme.Blood);
        GUI.Label(new Rect(cx,cy+26*u,cw,60*u), "— 말로 · "+cKor+" —", sTitle);
        GUI.Label(new Rect(cx,cy+80*u,cw,150*u), cHanja, sHanja);
        GUI.Label(new Rect(cx,cy+232*u,cw,50*u), cTitle, sTitle);
        if (cNew) GUI.Label(new Rect(cx,cy+282*u,cw,42*u), "처음 보는 말로 — 말로록에 새겨졌다", sNew);
        GUI.color = g;
    }
}
