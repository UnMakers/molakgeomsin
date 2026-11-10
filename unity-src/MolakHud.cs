using Naninovel;
using UnityEngine;

/// <summary>HUD v5 — 서울2033식 미니멀: 최상단 저주 스트립 + 한 줄 상태(한자 라벨) + 서열 연출.</summary>
public class MolakHud : MonoBehaviour
{
    static MolakHud instance;
    static Texture2D px;
    Font gowun;
    GUIStyle sDay, sStat, sRank, sBig, sGold, sTierC, sCurse;
    float builtU = -1f;

    static float rsFrom, rsTo; static bool rsTierUp; static string rsTier = ""; static float rsT = -1f;

    // 서랍(검술·은자·서열) 펼침 상태
    static bool expanded = true;   // 상시 상세 표시(검술·은자·서열)
    public static bool Expanded { get { return expanded; } }

    // 능력치 변화 플로팅 표시
    struct Float { public string text; public Color col; public float t; public float x; }
    static readonly System.Collections.Generic.List<Float> floats = new System.Collections.Generic.List<Float>();
    static readonly string[] fkeys = { "hp", "ng", "sw", "sl", "mn", "curse" };
    static readonly string[] fnames = { "체력", "내공", "검술", "살기", "은자", "저주" };
    public static void Delta (string key, float d)
    {
        if (Mathf.Abs(d) < 0.5f) return;
        int k = System.Array.IndexOf(fkeys, key); if (k < 0) return;
        bool good = key == "curse" ? d < 0 : d > 0;
        var c = good ? new Color(0.20f, 0.42f, 0.22f, 1f) : new Color(0.66f, 0.17f, 0.12f, 1f);
        if (key == "mn") c = good ? new Color(0.54f, 0.42f, 0.10f, 1f) : c;
        // 같은 키가 이미 떠 있으면 합산
        for (int i = 0; i < floats.Count; i++) if (floats[i].text.StartsWith(fnames[k]) && floats[i].t < 0.6f)
        {
            float prev; var parts = floats[i].text.Split(' ');
            if (parts.Length > 1 && float.TryParse(parts[1], out prev)) { d += prev; floats.RemoveAt(i); break; }
        }
        floats.Add(new Float { text = fnames[k] + " " + (d > 0 ? "+" : "") + Mathf.Round(d).ToString("0"), col = c, t = 0f, x = 0.18f + 0.13f*k });
    }

    static Color InkD { get { return MolakTheme.InkText; } }
    static Color InkM { get { return MolakTheme.InkMid; } }
    static bool builtNight;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap ()
    {
        if (instance != null) return;
        var go = new GameObject("MolakHud");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<MolakHud>();
    }

    void Awake ()
    {
        foreach (var o in FindObjectsByType<MolakHud>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (o != this) Destroy(o.gameObject);
    }

    public static void RankShift (float from, float to, bool tierUp, string tierName)
    {
        rsFrom = from <= 0 ? 8000000f : from; rsTo = to; rsTierUp = tierUp; rsTier = tierName; rsT = 0f;
        if (tierUp) MolakSfx.Bell(); else MolakSfx.Confirm();
    }

    static Texture2D Px { get { if (px == null) { px = new Texture2D(1,1); px.SetPixel(0,0,Color.white); px.Apply(); } return px; } }
    void Box (Rect r, Color c) { var g = GUI.color; GUI.color = c; GUI.DrawTexture(r, Px); GUI.color = g; }
    float U { get { return MolakTheme.U; } }

    void EnsureStyles (float u)
    {
        if (sDay != null && Mathf.Approximately(builtU, u) && builtNight == MolakTheme.Night) return;
        builtU = u; builtNight = MolakTheme.Night;
        if (gowun == null) gowun = Resources.Load<Font>("GowunBatang");
        System.Func<int, Color, TextAnchor, GUIStyle> mk = (size, col, an) =>
        {
            var s = new GUIStyle(GUI.skin.label){fontSize=Mathf.RoundToInt(size*u),alignment=an,richText=true};
            if (gowun != null) s.font = gowun;
            s.normal.textColor = col; return s;
        };
        sDay = mk(31, InkM, TextAnchor.MiddleLeft);
        sStat = mk(31, InkD, TextAnchor.MiddleCenter);
        sRank = mk(28, InkM, TextAnchor.MiddleRight);
        sCurse = mk(26, new Color(0.46f,0.32f,0.60f,1f), TextAnchor.MiddleRight);
        sBig = mk(72, MolakTheme.Ink, TextAnchor.MiddleCenter);
        sGold = mk(30, MolakTheme.Gold, TextAnchor.MiddleCenter);
        sTierC = mk(23, MolakTheme.Ink3, TextAnchor.MiddleCenter);
    }

    string TierOf (float r)
    {
        if (r <= 0) return "무명";
        if (r <= 1) return "멸도";
        if (r <= 10) return "천묵";
        if (r <= 100) return "허실";
        if (r <= 1000) return "발경";
        if (r <= 10000) return "화경";
        if (r <= 100000) return "관맥";
        if (r <= 1000000) return "운위";
        if (r <= 3000000) return "취위";
        return "식위";
    }

    void OnGUI ()
    {
        if (!Engine.Initialized) return;
        if (MolakMenu.IsOpen) return;
        float u = U;
        EnsureStyles(u);
        float w = Screen.width;
        float day = MolakStats.Get("day");
        if (day < 1) { DrawRankShift(w, u); return; }
        float W = Mathf.Min(w, 1080*u), X0 = (w-W)/2f;
        float hp=MolakStats.Get("hp"), hpm=Mathf.Max(1,MolakStats.Get("hpmax"));
        float curse=MolakStats.Get("curse"), rank=MolakStats.Get("rank");

        // 최상단 저주 스트립 (얇게, 전폭)
        float sh = 10*u;
        Box(new Rect(0,0,w,sh), new Color(0.84f,0.80f,0.73f,0.85f));
        var cc = new Color(0.52f,0.36f,0.66f,1f);
        if (curse >= 80f) { float pulse = 0.6f+0.4f*Mathf.Sin(Time.time*5f); cc = Color.Lerp(cc, new Color(0.72f,0.18f,0.14f,1f), pulse); }
        Box(new Rect(0,0,w*Mathf.Clamp01(curse/100f),sh), cc);
        if (curse >= 50f)
            GUI.Label(new Rect(X0+W-200*u, sh+4*u, 180*u, 30*u), "저주 "+curse.ToString("0")+"%", sCurse);

        // 한 줄 상태: 일수 · 체력 · 내공 · 살기 + 아랫줄 검술 · 은자 · 서열 (상시)
        float pad = X0 + 84*u;
        float y = 100*u, lh = 56*u;
        MolakInputGuard.Guard(new Rect(X0, 0, W, y+lh+16*u + (expanded ? 72*u : 0f)));
        string hpCol = hp/hpm <= 0.34f ? (MolakTheme.Night ? "#E06A5A" : "#A33226") : "#" + ColorUtility.ToHtmlStringRGB(MolakTheme.InkText);
        GUI.Label(new Rect(pad, y, 170*u, lh), day.ToString("0")+"일째", sDay);
        string stats =
            "<color="+hpCol+">체력 "+hp.ToString("0")+"</color>    내공 "+MolakStats.Get("ng").ToString("0")+
            "    살기 "+MolakStats.Get("sl").ToString("0");
        GUI.Label(new Rect(X0+W*0.20f, y, W*0.62f, lh), stats, sStat);
        // 가는 구분선
        Box(new Rect(pad, y+lh+4*u, W-168*u-112*u, Mathf.Max(1f,1*u)), new Color(0.55f,0.48f,0.40f,0.45f));

        // 서랍: 검술 · 은자 · 서열·경지
        if (expanded)
        {
            float y2 = y + lh + 10*u, lh2 = 50*u;
            Box(new Rect(pad, y2, W-168*u, lh2), MolakTheme.Night ? new Color(0.20f,0.18f,0.16f,0.85f) : new Color(0.96f,0.93f,0.87f,0.80f));
            string rtxt = rank > 0 ? rank.ToString("N0")+"위 · "+TierOf(rank) : "서열 없음";
            string s2 = "검술 "+MolakStats.Get("sw").ToString("0")+"    <color=#8A6D1F>은자 "+MolakStats.Get("mn").ToString("0")+"</color>    "+rtxt;
            GUI.Label(new Rect(pad, y2, W-168*u, lh2), s2, sStat);
        }

        // 플로팅 델타
        for (int i = floats.Count-1; i >= 0; i--)
        {
            var f = floats[i]; f.t += Time.deltaTime; floats[i] = f;
            if (f.t > 1.6f) { floats.RemoveAt(i); continue; }
            float k = f.t/1.6f;
            float alpha = k < 0.15f ? k/0.15f : (k > 0.7f ? (1f-k)/0.3f : 1f);
            float fy = y + lh + 18*u + (expanded ? 60*u : 0f) + (1f-Mathf.Pow(1f-k,2f))*26*u;
            var g = GUI.color; GUI.color = new Color(1,1,1,alpha);
            var st = new GUIStyle(sStat); st.normal.textColor = f.col; st.alignment = TextAnchor.MiddleLeft;
            GUI.Label(new Rect(X0+W*f.x, fy, 260*u, 40*u), f.text, st);
            GUI.color = g;
        }

        DrawRankShift(w, u);
    }

    void DrawRankShift (float w, float u)
    {
        if (rsT < 0f) return;
        rsT += Time.deltaTime;
        float roll = 1.6f, hold = 1.4f;
        if (rsT > roll + hold) { rsT = -1f; return; }
        float cw = Mathf.Min(w*0.86f, 760f*u), ch = 230f*u;
        float cx = (w-cw)/2f, cy = Screen.height*0.30f;
        float a = rsT > roll+hold-0.4f ? (roll+hold-rsT)/0.4f : 1f;
        var g = GUI.color;
        GUI.color = new Color(1,1,1,a);
        Box(new Rect(cx,cy,cw,ch), new Color(0.05f,0.04f,0.035f,0.95f));
        Box(new Rect(cx,cy,cw,2*u), rsTierUp ? MolakTheme.Gold : MolakTheme.Line);
        Box(new Rect(cx,cy+ch-2*u,cw,2*u), rsTierUp ? MolakTheme.Gold : MolakTheme.Line);
        float k = Mathf.Clamp01(rsT/roll);
        k = 1f-Mathf.Pow(1f-k, 3f);
        float cur = Mathf.Lerp(rsFrom, rsTo, k);
        GUI.Label(new Rect(cx,cy+26*u,cw,90*u), "서열 "+Mathf.Round(cur).ToString("N0")+"위", sBig);
        if (rsTierUp && rsT > roll*0.7f)
            GUI.Label(new Rect(cx,cy+130*u,cw,54*u), "─ "+rsTier+" 진입 ─", sGold);
        else if (rsFrom > rsTo)
            GUI.Label(new Rect(cx,cy+130*u,cw,54*u), "서열이 오른다", sTierC);
        GUI.color = g;
    }
}
