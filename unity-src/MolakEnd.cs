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
        new Entry("seongsa",   "成邪", "성사", "열매가 익었다",        "노인의 얼굴을 본 뒤, 저주가 여든여덟을 넘겼을 때"),
        new Entry("ipma",      "入魔", "입마", "무릎을 꿇은 검",       "그물이 닫히기 전 집법전주 앞에서, 부름에 무릎을 꿇었을 때"),
        new Entry("wonhaeng",  "遠行", "원행", "복수보다 먼 길",       "약조를 맺은 사람과, 서른여드레 넘어 밤배를 탔을 때"),
        new Entry("sungeom",   "殉劍", "순검", "베고, 쓰러지다",       "저주가 여든을 넘긴 채 교주를 베었을 때"),
        new Entry("pobak",     "縛",   "포박", "사슬에 끌려가다",      "두 달이 차도록 끝을 내지 못했을 때 — 관아의 그물"),
        new Entry("wonsu",     "怨讐", "원수", "벤 칼이 돌아오다",     "목을 친 자의 사람들이 찾아왔을 때"),
        new Entry("chae",      "債",   "채",   "빚은 목으로 갚는다",   "갚지 못할 빚을 졌을 때"),
        new Entry("jungdok",   "中毒", "중독", "피가 검게 식다",       "독을 품은 채 날을 보냈을 때"),
        new Entry("pasang",    "破傷", "파상", "상처가 먼저 죽었다",   "덧난 상처를 버려두었을 때"),
        new Entry("jagaek",    "刺客", "자객", "술잔 너머의 칼",       "교의 눈에 오래 띄었을 때"),
        new Entry("sagyeol",   "死決", "사결", "둘 중 하나만 걸어 나간다", "생사결에 응했을 때"),
        new Entry("hyeop",     "俠",   "협",   "남의 일에 칼을 뽑다",  "남을 위해 칼을 뽑았을 때"),
        new Entry("ik",        "溺",   "익",   "물이 먼저 받아 주었다", "불어난 물에 뛰어들었을 때"),
        new Entry("yaseup",    "夜襲", "야습", "계단 셋째 칸",          "혈랑대가 객잔을 덮친 밤"),
        new Entry("hapbyeok",  "合璧", "합벽", "두 검이 하나로",        "그 밤 그녀에게 받은 열두 줄을 들고, 다음 생의 사당으로"),
        new Entry("doju",      "逃走", "도주", "그물 밖으로",           "관아의 그물이 닫히기 전, 운락을 빠져나갔을 때"),
        new Entry("jaegeon",   "再建", "재건", "맹의 깃발 아래",        "무림맹 밀사와 거래하고, 대사형의 목을 벤 뒤에"),
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
        if (cause == "야습") return "yaseup";
        if (cause == "합벽") return "hapbyeok";
        if (cause == "재건") return "jaegeon";
        if (cause == "도주") return "doju";
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

    public static void Show (string hanja, string kor, string title, bool isNew)
    {
        cHanja = hanja; cKor = kor; cTitle = title; cNew = isNew; cT = 0f;
        cTrue = hanja == "合璧";
        if (cTrue) MolakSfx.TrueBell(); else MolakSfx.Death();
    }
    static bool cTrue;
    static Texture2D dawnTex, ensoTex, diskTex; static Font hanjaFont;

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
        if (cTrue) { DrawTrue(); return; }
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
    /// <summary>
    /// 진(眞) 말로 · 합벽 전용 카드 — 핏빛 테두리 대신 새벽: 먹빛에서 새벽빛으로 번지는 하늘, 떠오르는 해,
    /// 타이틀의 원상이 금빛으로 감기며 멈추고, 그 안에 合璧. 종소리(true_bell)와 함께 7초.
    /// </summary>
    void DrawTrue ()
    {
        if (Event.current.type == EventType.Repaint) cT += Time.deltaTime;   // OnGUI는 한 프레임에 여러 번 불린다
        float dur = 7.2f, tail = 1.4f;
        if (cT > dur + tail) { cT = -1f; cTrue = false; return; }
        float u = MolakTheme.U; EnsureStyles(u);
        float w = Screen.width, h = Screen.height;
        if (dawnTex == null)
        {
            dawnTex = new Texture2D(1, 128, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int i = 0; i < 128; i++)
            {
                float k = i / 127f;   // 0 = 아래, 1 = 위
                var c = Color.Lerp(new Color(0.78f, 0.52f, 0.30f), new Color(0.10f, 0.11f, 0.15f), Mathf.SmoothStep(0f, 1f, k));
                dawnTex.SetPixel(0, i, c);
            }
            dawnTex.Apply();
            int n = 128; diskTex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                diskTex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d)));
            }
            diskTex.Apply();
        }
        if (ensoTex == null) ensoTex = Resources.Load<Texture2D>("MolakTitleEnso");
        if (hanjaFont == null) hanjaFont = Resources.Load<Font>("MolakHanja");

        float out0 = cT > dur ? Mathf.Clamp01((dur + tail - cT) / tail) : 1f;
        var g = GUI.color;
        // 먹빛 → 새벽
        GUI.color = new Color(0.03f, 0.025f, 0.02f, Mathf.Clamp01(cT / 0.4f) * out0);
        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        float dawn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((cT - 0.6f) / 2.4f)) * out0;
        GUI.color = new Color(1, 1, 1, dawn * 0.92f);
        GUI.DrawTexture(new Rect(0, 0, w, h), dawnTex);
        // 떠오르는 해(아래에서)
        float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((cT - 0.8f) / 4.5f));
        float sr = Mathf.Min(w, h) * 0.55f;
        float sy = Mathf.Lerp(h * 1.02f, h * 0.80f, rise);
        GUI.color = new Color(1f, 0.80f, 0.45f, 0.55f * dawn);
        GUI.DrawTexture(new Rect(w / 2f - sr, sy - sr, sr * 2f, sr * 2f), diskTex);
        if (cT > dur + 0.01f) { GUI.color = g; return; }

        float a = cT < 0.8f ? 0f : (cT > dur - 0.7f ? (dur - cT) / 0.7f : Mathf.Clamp01((cT - 0.8f) / 0.8f));
        float cy = h * 0.40f, er = Mathf.Min(w * 0.78f, 760 * u);
        // 금빛 원상: 감기며 멈춘다
        if (ensoTex != null)
        {
            float k = Mathf.Clamp01((cT - 0.6f) / 2.4f);
            float ease = 1f - Mathf.Pow(1f - k, 3f);
            float ang = -540f * (1f - ease) - cT * 1.5f;
            var m = GUI.matrix;
            GUIUtility.RotateAroundPivot(ang, new Vector2(w / 2f, cy));
            GUI.color = new Color(0.86f, 0.70f, 0.32f, 0.9f * Mathf.Clamp01((cT - 0.6f) / 0.6f) * (cT > dur - 0.7f ? (dur - cT) / 0.7f : 1f));
            GUI.DrawTexture(new Rect(w / 2f - er / 2f, cy - er / 2f, er, er), ensoTex);
            GUI.matrix = m;
        }
        GUI.color = new Color(1, 1, 1, a);
        var sH = new GUIStyle(sHanja) { fontSize = Mathf.RoundToInt(150 * u) };
        if (hanjaFont != null) sH.font = hanjaFont;
        sH.normal.textColor = new Color(0.95f, 0.84f, 0.55f, 1f);
        var sT = new GUIStyle(sTitle); sT.normal.textColor = new Color(0.93f, 0.88f, 0.78f, 1f);
        Shadowed(new Rect(0, cy - er / 2f - 90 * u, w, 60 * u), "— 진(眞) 말로 · " + cKor + " —", sT, u, a);
        Shadowed(new Rect(0, cy - 110 * u, w, 220 * u), cHanja, sH, u, a);
        Shadowed(new Rect(0, cy + er / 2f + 30 * u, w, 56 * u), cTitle, sT, u, a);
        float b = Mathf.Clamp01((cT - 2.6f) / 1.0f) * (cT > dur - 0.7f ? (dur - cT) / 0.7f : 1f);
        GUI.color = new Color(1, 1, 1, b);
        var sE = new GUIStyle(sTitle) { fontSize = Mathf.RoundToInt(28 * u) };
        sE.normal.textColor = new Color(0.98f, 0.94f, 0.86f, 0.95f);
        Shadowed(new Rect(w * 0.06f, cy + er / 2f + 100 * u, w * 0.88f, 50 * u), "죽음이 스승이었다. 이번에는 살아남은 것이 가르친다.", sE, u, b);
        if (cNew)
        {
            var sN = new GUIStyle(sNew); sN.normal.textColor = new Color(0.98f, 0.90f, 0.66f, 1f);
            Shadowed(new Rect(0, cy + er / 2f + 160 * u, w, 42 * u), "처음 보는 말로 — 말로록 맨 끝 칸이 채워졌다", sN, u, b);
        }
        GUI.color = g;
    }

    static void Shadowed (Rect r, string text, GUIStyle st, float u, float alpha)
    {
        var keep = st.normal.textColor;
        var sh = new GUIStyle(st); sh.normal.textColor = new Color(0.05f, 0.03f, 0.02f, 0.55f);
        var g = GUI.color; GUI.color = new Color(1, 1, 1, alpha);
        GUI.Label(new Rect(r.x + 2f * u, r.y + 3f * u, r.width, r.height), text, sh);
        GUI.Label(r, text, st);
        GUI.color = g;
    }
}
