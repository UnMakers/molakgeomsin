using Naninovel;
using UnityEngine;

/// <summary>HUD v5 — 서울2033식 미니멀: 최상단 저주 스트립 + 한 줄 상태(한자 라벨) + 서열 연출.</summary>
public class MolakHud : MonoBehaviour
{
    static MolakHud instance;
    Font gowun;
    GUIStyle sDay, sStat, sRank, sBig, sGold, sTierC, sCurse, sWarn, sBody; bool bodyNight; static bool wasLow = true;
    float builtU = -1f;

    static float rsFrom, rsTo; static bool rsTierUp; static string rsTier = ""; static float rsT = -1f;

    // 서랍(검술·은자·서열) 펼침 상태
    static bool expanded = true;   // 상시 상세 표시(검술·은자·서열)
    public static bool Expanded { get { return expanded; } }

    // 능력치 변화 플로팅 표시
    // 0.4.57 능력치 변화 — 예전엔 @fx 로 바뀐 것만, 상태 줄 밑에 작은 글씨로 떴다가 사라져 잘 안 보였다(언메이커).
    //  · 어디서 바뀌든(@fx, @set, 비무, 하루 마감) 화면에 보이는 정수가 바뀌면 잡는다(MolakHud.Update 가 매 프레임 견준다).
    //  · 상태 줄의 그 숫자가 2.2초 동안 굵고 붉게/푸르게 바뀌고,
    //  · 그림 띠 위에 어두운 패 하나("체력 −2")가 튀어나왔다가 2.4초 뒤 사라진다. 같은 수치는 0.8초 안이면 합친다.
    //  · 새 생·불러오기·비무 연습 몸 고르기처럼 한꺼번에 바뀌는 때는 띄우지 않는다.
    struct Float { public int k; public float d; public float t; }
    static readonly System.Collections.Generic.List<Float> floats = new System.Collections.Generic.List<Float>();
    static readonly string[] fkeys = { "hp", "ng", "sw", "sl", "mn", "curse" };
    static readonly string[] fnames = { "체력", "내공", "검술", "살기", "은자", "저주" };
    static readonly float[] flashT = { -1f, -1f, -1f, -1f, -1f, -1f };
    static readonly float[] flashD = new float[6];
    static readonly float[] lastV = new float[6];
    static bool synced, hookedReset; static float lastRuns = -1f, lastDay = -1f;
    /// <summary>옛 호출 자리(@fx). 이제는 Update 가 모든 변화를 잡으므로 아무것도 하지 않는다.</summary>
    public static void Delta (string key, float d) { }
    /// <summary>한꺼번에 바뀌는 순간(연습 몸 고르기 등) — 다음 프레임에 지금 값으로 다시 맞춘다.</summary>
    public static void Resync () { synced = false; }

    void Update ()
    {
        if (!Engine.Initialized) return;
        if (!hookedReset)
        {
            var stm = Engine.GetService<IStateManager>();
            if (stm != null) { stm.OnResetStarted += () => synced = false; stm.OnGameLoadFinished += _ => synced = false; hookedReset = true; }
        }
        float day = MolakStats.Get("day"), runs = MolakStats.Get("g_runs");
        if (day < 1f || MolakPractice.Active) { synced = false; return; }
        if (!synced || runs != lastRuns || day < lastDay)
        {
            for (int k = 0; k < fkeys.Length; k++) lastV[k] = Mathf.Round(MolakStats.Get(fkeys[k]));
            synced = true; lastRuns = runs; lastDay = day; floats.Clear();
            return;
        }
        lastDay = day;
        for (int k = 0; k < fkeys.Length; k++)
        {
            float cur = Mathf.Round(MolakStats.Get(fkeys[k]));
            if (cur == lastV[k]) continue;
            float d = cur - lastV[k]; lastV[k] = cur;
            flashT[k] = 0f; flashD[k] = d;
            bool merged = false;
            for (int i = 0; i < floats.Count; i++)
                if (floats[i].k == k && floats[i].t < 0.8f) { var f = floats[i]; f.d += d; f.t = 0f; floats[i] = f; merged = true; break; }
            if (!merged) floats.Add(new Float { k = k, d = d, t = 0f });
        }
        for (int k = 0; k < flashT.Length; k++) if (flashT[k] >= 0f) { flashT[k] += Time.unscaledDeltaTime; if (flashT[k] > 2.2f) flashT[k] = -1f; }
    }

    static bool Good (int k, float d) { return fkeys[k] == "curse" ? d < 0 : d > 0; }
    /// <summary>상태 줄의 숫자: 막 바뀌었으면 굵게, 좋으면 푸르게 나쁘면 붉게.</summary>
    static string Num (string key, float v)
    {
        int k = System.Array.IndexOf(fkeys, key);
        string s = v.ToString("0");
        if (k < 0 || flashT[k] < 0f) return s;
        string c = Good(k, flashD[k]) ? (MolakTheme.Night ? "#7FD08A" : "#2E7D3A") : (MolakTheme.Night ? "#FF7A66" : "#B3261E");
        return "<b><color=" + c + ">" + s + "</color></b>";
    }

    GUIStyle sChip; float chipU = -1f;
    void DrawChips (float X0, float W, float yTop, float u)
    {
        if (floats.Count == 0) return;
        if (sChip == null || !Mathf.Approximately(chipU, u)) { chipU = u; sChip = MolakGui.Label(34, u, TextAnchor.MiddleCenter, false); }
        float dt = Event.current.type == EventType.Repaint ? Time.unscaledDeltaTime : 0f;   // OnGUI 는 한 프레임에 여러 번 불린다
        var items = new System.Collections.Generic.List<(string txt, Color col, float a, float sc, float rise)>();
        for (int i = floats.Count - 1; i >= 0; i--)
        {
            var f = floats[i]; f.t += dt; floats[i] = f;
            if (f.t > 2.4f || Mathf.Round(f.d) == 0f) { floats.RemoveAt(i); continue; }
        }
        float total = 0f, gap = 14f * u;
        var sizes = new System.Collections.Generic.List<Vector2>();
        foreach (var f in floats)
        {
            string txt = fnames[f.k] + " " + (f.d > 0 ? "+" : "−") + Mathf.Abs(Mathf.Round(f.d)).ToString("0");
            var sz = sChip.CalcSize(new GUIContent(txt)); sz.x += 36f * u; sz.y = 58f * u;
            sizes.Add(sz); total += sz.x + gap;
        }
        float x = X0 + (W - total + gap) / 2f;
        var g0 = GUI.color; var m0 = GUI.matrix;
        for (int i = 0; i < floats.Count; i++)
        {
            var f = floats[i]; var sz = sizes[i];
            float a = f.t < 0.12f ? f.t / 0.12f : (f.t > 1.9f ? Mathf.Clamp01((2.4f - f.t) / 0.5f) : 1f);
            float sc = f.t < 0.18f ? Mathf.Lerp(1.35f, 1f, f.t / 0.18f) : 1f;
            float rise = Mathf.Min(1f, f.t / 2.4f) * 12f * u;
            var r = new Rect(x, yTop - rise, sz.x, sz.y);
            GUI.matrix = m0; GUIUtility.ScaleAroundPivot(new Vector2(sc, sc), r.center);
            GUI.color = new Color(0.07f, 0.055f, 0.045f, 0.88f * a); GUI.DrawTexture(r, MolakGui.Px);
            bool good = Good(f.k, f.d);
            Color c = fkeys[f.k] == "mn" && good ? new Color(0.98f, 0.82f, 0.38f, a)
                    : fkeys[f.k] == "curse" && !good ? new Color(0.80f, 0.62f, 1f, a)
                    : good ? new Color(0.55f, 0.88f, 0.58f, a) : new Color(1f, 0.46f, 0.38f, a);
            GUI.color = new Color(c.r, c.g, c.b, a); GUI.DrawTexture(new Rect(r.x, r.y, 6f * u, r.height), MolakGui.Px);   // 왼쪽 색띠
            GUI.color = Color.white;
            string txt = fnames[f.k] + " " + (f.d > 0 ? "+" : "−") + Mathf.Abs(Mathf.Round(f.d)).ToString("0");
            MolakGui.Ink(sChip, c); GUI.Label(new Rect(r.x + 4f * u, r.y, r.width, r.height), txt, sChip);
            x += sz.x + gap;
        }
        GUI.matrix = m0; GUI.color = g0;
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

    static Texture2D Px { get { return MolakGui.Px; } }
    void Box (Rect r, Color c) { var g = GUI.color; GUI.color = c; GUI.DrawTexture(r, Px); GUI.color = g; }
    float U { get { return MolakTheme.U; } }

    void EnsureStyles (float u)
    {
        if (sDay != null && Mathf.Approximately(builtU, u) && builtNight == MolakTheme.Night) return;
        builtU = u; builtNight = MolakTheme.Night;
        if (gowun == null) gowun = MolakGui.Gowun;
        System.Func<int, Color, TextAnchor, GUIStyle> mk = (size, col, an) =>
        {
            var s = new GUIStyle(GUI.skin.label){fontSize=Mathf.RoundToInt(size*u),alignment=an,richText=true};
            if (gowun != null) s.font = gowun;
            MolakGui.Ink(s, col); return s;
        };
        sDay = mk(32, InkD, TextAnchor.MiddleLeft);
        sStat = mk(31, InkD, TextAnchor.MiddleCenter);
        sRank = mk(28, InkM, TextAnchor.MiddleRight);
        sCurse = mk(26, new Color(0.46f,0.32f,0.60f,1f), TextAnchor.MiddleRight);
        sWarn = mk(26, MolakTheme.Night ? new Color(0.88f,0.42f,0.35f,1f) : new Color(0.62f,0.18f,0.13f,1f), TextAnchor.MiddleLeft);
        sBig = mk(72, MolakTheme.Ink, TextAnchor.MiddleCenter);
        sGold = mk(30, MolakTheme.Gold, TextAnchor.MiddleCenter);
        sTierC = mk(23, MolakTheme.Ink3, TextAnchor.MiddleCenter);
    }

    static string Warn ()
    {
        var parts = new System.Collections.Generic.List<string>();
        float inj = MolakStats.Get("injury"), taint = MolakStats.Get("taint"), debt = MolakStats.Get("debt");
        if (taint > 0) parts.Add((MolakStats.Get("taintkind") == 1 ? "독" : "곪은 상처") + (taint >= 4 ? " — 깊다" : taint >= 2 ? " — 번진다" : ""));
        if (inj > 0) parts.Add("부상");
        if (debt > 0)
        {
            float left = MolakStats.Get("debtday") - MolakStats.Get("day");
            parts.Add("빚 " + debt.ToString("0") + "냥 · " + (left > 3 ? "독촉 전" : left > 0 ? "곧 독촉" : "독촉이 왔다"));
        }
        // 0.4.9: 날짜 대신 관아 그물 — 예순 날(포박)이 다가오는 것을 숫자 없이 알린다
        float dd = MolakStats.Get("day");
        if (MolakStats.Get("b_capture") < 1f && MolakStats.Get("f_dojuroute") < 1f)
        {
            if (dd >= MolakRules.NetCloseDay) parts.Add("그물이 닫히기 직전");
            else if (dd >= MolakRules.NetWarnDay) parts.Add("관아 그물이 조여 온다");
        }
        return string.Join("   ", parts.ToArray());
    }

    /// <summary>0.4.32 숫자 대신 몸으로 읽는 한 줄: 업보(부상·독·빚)가 없을 때 맨 윗줄에 흐리게 뜬다.</summary>
    static string Body ()
    {
        float hp = MolakStats.Get("hp"), hpm = Mathf.Max(1f, MolakStats.Get("hpmax")), r = hp / hpm;
        float curse = MolakStats.Get("curse"), mn = MolakStats.Get("mn");
        var parts = new System.Collections.Generic.List<string>();
        parts.Add(r >= 0.85f ? "숨이 고르다" : r >= 0.6f ? "몸이 묵직하다" : r >= 0.34f ? "온몸이 욱신거린다" : "입안에 피 맛이 돈다");
        if (curse >= 90f) parts.Add("문양이 숨을 쉰다");
        else if (curse >= 75f) parts.Add("문양이 뜨겁다");
        else if (curse >= 50f) parts.Add("문양이 저릿하다");
        else if (curse >= 25f) parts.Add("문양이 미지근하다");
        if (mn <= 1f) parts.Add("전낭이 비었다");
        return string.Join(" · ", parts.ToArray());
    }

    string TierOf (float r)
    {
        if (r <= 0) return "무명";
        return MolakDuel.TierName(r);   // 0.4.35 경지 이름은 한곳에서
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

        // 몸에 남은 업보: 부상 · 독 · 곪은 상처 · 빚 — 맨 윗줄, 저주 표시 왼편
        string warn = Warn();
        if (warn.Length > 0) GUI.Label(new Rect(X0 + 124*u, sh+6*u, W - 124*u - 220*u, 36*u), warn, sWarn);
        else
        {
            if (sBody == null || !Mathf.Approximately(sBody.fontSize, Mathf.RoundToInt(25*u)) || bodyNight != MolakTheme.Night)
            { sBody = new GUIStyle(sWarn) { fontSize = Mathf.RoundToInt(25*u) }; MolakGui.Ink(sBody, new Color(InkM.r, InkM.g, InkM.b, 0.8f)); bodyNight = MolakTheme.Night; }
            GUI.Label(new Rect(X0 + 124*u, sh+6*u, W - 124*u - 220*u, 36*u), Body(), sBody);
        }

        // 한 줄 상태: 체력 · 내공 · 살기 + 아랫줄 검술 · 은자 · 서열 (상시)
        float pad = X0 + 84*u;
        float y = 100*u, lh = 56*u;
        MolakInputGuard.Guard(new Rect(X0, 0, W, y+lh+16*u + (expanded ? 72*u : 0f)));
        string hpCol = hp/hpm <= 0.34f ? (MolakTheme.Night ? "#E06A5A" : "#A33226") : "#" + ColorUtility.ToHtmlStringRGB(MolakTheme.InkText);
        // 날짜는 보이지 않는다(0.4.3) — 시간은 문양과 바깥 풍경으로만 느낀다
        string stats =
            "<color="+hpCol+">체력 "+Num("hp", hp)+"</color>    내공 "+Num("ng", MolakStats.Get("ng"))+
            "    살기 "+Num("sl", MolakStats.Get("sl"));
        GUI.Label(new Rect(X0+W*0.20f, y, W*0.62f, lh), stats, sStat);
        // 0.4.55 몸 상태 줄을 누르면 「몸의 셈」(각 수치가 무엇을 하는지)
        if (!MolakMenu.IsOpen && !MolakGuide.IsOpen && GUI.Button(new Rect(X0+W*0.18f, y, W*0.64f, lh + (expanded ? 72*u : 0f)), GUIContent.none, GUIStyle.none)) MolakGuide.Read("body");
        // 가는 구분선 + 체력 붓 획(0.4.32): 구분선 위로 남은 체력만큼 먹 획이 그어진다. 끝으로 갈수록 가늘고, 바닥이면 붉다.
        float lineW = W-168*u-112*u;
        Box(new Rect(pad, y+lh+4*u, lineW, Mathf.Max(1f,1*u)), new Color(0.55f,0.48f,0.40f,0.45f));
        {
            float rr = Mathf.Clamp01(hp/hpm), sw2 = lineW * rr;
            bool low = rr <= 0.34f;
            if (low && !wasLow && Event.current.type == EventType.Repaint) MolakSfx.Heart();   // 바닥으로 떨어지는 순간 한 번
            if (Event.current.type == EventType.Repaint) wasLow = low;
            float pul = low ? 0.75f + 0.25f*Mathf.Sin(Time.time*4f) : 1f;
            var ink = low ? (MolakTheme.Night ? new Color(0.88f,0.42f,0.35f,0.9f*pul) : new Color(0.64f,0.18f,0.13f,0.9f*pul))
                          : (MolakTheme.Night ? new Color(0.85f,0.80f,0.72f,0.75f) : new Color(0.16f,0.13f,0.11f,0.72f));
            // 1px 남짓한 세로 조각으로 촘촘히 — 굵기만 줄이고 농도는 같게 둬야 이음매가 안 보인다
            int seg = Mathf.Clamp(Mathf.CeilToInt(sw2 / Mathf.Max(1f, 1.5f*u)), 8, 400);
            float segW = sw2 / seg;
            for (int i = 0; i < seg; i++)
            {
                float f0 = i/(float)seg;
                float th = (Mathf.Lerp(6f, 1.6f, f0*f0) + (f0 < 0.04f ? (0.04f-f0)*40f : 0f)) * u;   // 붓이 눌렸다 빠지듯, 시작점은 살짝 뭉툭
                float x0 = Mathf.Floor(pad + segW*i), x1 = Mathf.Floor(pad + segW*(i+1));
                if (x1 <= x0) continue;
                // 굵기의 소수 부분은 위아래 한 줄의 옅기로 — 계단이 지지 않게
                float cy = Mathf.Round(y+lh+4.5f*u), core = Mathf.Max(1f, Mathf.Floor(th)), frac = (th - core) * 0.5f;
                float top = cy - Mathf.Floor(core/2f);
                Box(new Rect(x0, top, x1 - x0, core), ink);
                if (frac > 0.02f)
                {
                    var soft = new Color(ink.r, ink.g, ink.b, ink.a * frac);
                    Box(new Rect(x0, top - 1f, x1 - x0, 1f), soft);
                    Box(new Rect(x0, top + core, x1 - x0, 1f), soft);
                }
            }
        }

        // 서랍: 검술 · 은자 · 서열·경지
        if (expanded)
        {
            float y2 = y + lh + 10*u, lh2 = 50*u;
            Box(new Rect(pad, y2, W-168*u, lh2), MolakTheme.Night ? new Color(0.20f,0.18f,0.16f,0.85f) : new Color(0.96f,0.93f,0.87f,0.80f));
            string rtxt = rank > 0 ? MolakDuel.RankText(rank)+" · "+TierOf(rank) : "서열록에 이름 없음";
            { var bh = MolakStats.Str("byeolho", "none"); if (!string.IsNullOrEmpty(bh) && bh != "none") rtxt += " · <color=#8A3A30>" + bh + "</color>"; }   // 0.4.33 강호가 붙인 별호
            string s2 = "검술 "+Num("sw", MolakStats.Get("sw"))+"    <color=#8A6D1F>은자 "+Num("mn", MolakStats.Get("mn"))+"</color>    "+rtxt;
            GUI.Label(new Rect(pad, y2, W-168*u, lh2), s2, sStat);
        }

        // 0.4.57 능력치 변화 패: 상태 줄 바로 밑, 그림 띠 위
        DrawChips(X0, W, y + lh + (expanded ? 72*u : 16*u) + 18*u, u);

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
        GUI.Label(new Rect(cx,cy+26*u,cw,90*u), "서열 "+(cur > MolakDuel.ListEdge ? "서열록 밖" : Mathf.Round(MolakDuel.ShowRank(cur)).ToString("N0")+"위"), sBig);
        if (rsTierUp && rsT > roll*0.7f)
            GUI.Label(new Rect(cx,cy+130*u,cw,54*u), "─ "+rsTier+" 진입 ─", sGold);
        else if (rsFrom > rsTo)
            GUI.Label(new Rect(cx,cy+130*u,cw,54*u), "서열이 오른다", sTierC);
        GUI.color = g;
    }
}
