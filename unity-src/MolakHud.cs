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
    struct Float { public int k; public float d; public float t; public string why; }
    static readonly System.Collections.Generic.List<Float> floats = new System.Collections.Generic.List<Float>();
    static GUIStyle sWall, wallSrc; static float wallU;
    static readonly string[] fkeys = { "hp", "ng", "sw", "sl", "mn", "curse", "insight" };
    static readonly string[] fnames = { "체력", "내공", "검술", "살기", "은자", "저주", "깨달음" };
    static readonly float[] flashT = { -1f, -1f, -1f, -1f, -1f, -1f, -1f };
    static readonly float[] flashD = new float[7];
    static readonly float[] lastV = new float[7];
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
            // 0.4.66 능력치 기록: 왜 바뀌었는지 한 줄
            string why = MolakStatLog.Resolve(fkeys[k]);
            MolakStatLog.Record(fkeys[k], d, why);
            AddWhy(fkeys[k], d, why);
            bool merged = false;
            for (int i = 0; i < floats.Count; i++)
                if (floats[i].k == k && floats[i].t < 0.8f) { var f = floats[i]; f.d += d; f.t = 0f; if (!string.IsNullOrEmpty(why) && f.why != why) f.why = string.IsNullOrEmpty(f.why) ? why : f.why + " · " + why; floats[i] = f; merged = true; break; }
            if (!merged) floats.Add(new Float { k = k, d = d, t = 0f, why = why });
        }
        for (int k = 0; k < flashT.Length; k++) if (flashT[k] >= 0f) { flashT[k] += Time.unscaledDeltaTime; if (flashT[k] > 2.2f) flashT[k] = -1f; }
    }

    static bool Good (int k, float d) { return fkeys[k] == "curse" ? d < 0 : d > 0; }
    /// <summary>상태 줄의 숫자: 막 바뀌었으면 굵게, 좋으면 푸르게 나쁘면 붉게.</summary>
    /// <summary>0.4.74 능력치 이름의 빛. 낮(종이)과 먹색 화면에 각각 맞춘 두 벌.
    /// 체력 = 연지(살아 있는 피) · 내공 = 쪽빛(단전의 물) · 살기 = 마른 피의 녹빛 · 검술 = 날 선 강철의 푸른빛 · 경지 = 금 · 깨달음 = 비취.</summary>
    public static class StatCol
    {
        public static readonly string[] Hp    = { "#B0345A", "#F08FAE" };
        public static readonly string[] Ng    = { "#3644A6", "#9AA8F5" };
        public static readonly string[] Sl    = { "#5A2410", "#D9875C" };
        public static readonly string[] Sw    = { "#1F6874", "#7CCBD6" };
        public static readonly string[] Realm = { "#8A6512", "#E3C26A" };
        public static readonly string[] Ins   = { "#256B57", "#86D4B7" };
        public static string Of (string[] c) { return MolakTheme.Night ? c[1] : c[0]; }
    }
    /// <summary>
    /// 0.4.78 검술 = 칼날(언메이커: 검술 색이 평범하다, 컨셉에 맞게). 한 가지 빛 대신 강철의 결 —
    /// 칼등 쪽 어두운 쇠빛에서 날 쪽 밝은 쇠빛으로 흐르고, 몇 초마다 서슬(빛 한 줄기)이 글자를 훑고 지나간다.
    /// 낮(종이)엔 짙은 강철에 푸른 서슬, 먹색 화면엔 은빛 강철에 흰 서슬.
    /// </summary>
    public static string SwordTag (string label, bool forceDay = false, bool bold = true, bool peak = false)
    {
        bool night = !forceDay && MolakTheme.Night;
        Color back = night ? new Color(0.50f, 0.58f, 0.66f) : new Color(0.13f, 0.18f, 0.24f);   // 칼등
        Color edge = night ? new Color(0.86f, 0.91f, 0.95f) : new Color(0.25f, 0.37f, 0.50f);   // 날
        Color glint = night ? new Color(1f, 1f, 1f) : new Color(0.42f, 0.70f, 0.92f);          // 서슬
        if (peak) glint = night ? new Color(1f, 0.93f, 0.70f) : new Color(0.80f, 0.58f, 0.12f);   // 극(極): 서슬이 금빛으로, 더 자주
        float period = peak ? 1.8f : 3.4f, sweep = 0.6f;
        float ph = Time.unscaledTime % period;
        float p = ph < sweep ? Mathf.Lerp(-0.6f, 1.6f, ph / sweep) : -9f;
        var sb = new System.Text.StringBuilder();
        int n = label.Length;
        for (int i = 0; i < n; i++)
        {
            float x = n <= 1 ? 0.5f : i / (float)(n - 1);
            var c = Color.Lerp(back, edge, x);
            float g = Mathf.Exp(-Mathf.Pow((x - p) / 0.38f, 2f));
            c = Color.Lerp(c, glint, g * 0.92f);
            sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(c)).Append('>').Append(label[i]).Append("</color>");
        }
        return bold ? "<b>" + sb + "</b>" : sb.ToString();
    }

    /// <summary>
    /// 0.4.78 능력치가 어느 선을 넘으면 강한 상태가 보이게(언메이커). 0 보통 · 1 강함(숫자도 제 빛, 굵게) · 2 극(이름에 빛이 흐른다).
    /// 선은 경지의 문턱에 맞췄다 — 검술 12(절정)/21(현경), 내공 11/20, 체력의 끝 20/30. 살기는 5/8, 짙어질수록 핏빛.
    /// </summary>
    public static int Tier (string key)
    {
        float v = key == "hp" ? MolakStats.Get("hpmax") : MolakStats.Get(key);
        switch (key)
        {
            case "sw": return v >= 21f ? 2 : v >= 12f ? 1 : 0;
            case "ng": return v >= 20f ? 2 : v >= 11f ? 1 : 0;
            case "hp": return v >= 30f ? 2 : v >= 20f ? 1 : 0;
            case "sl": return v >= 8f ? 2 : v >= 5f ? 1 : 0;
        }
        return 0;
    }

    /// <summary>극(極)의 이름: 제 빛에서 밝은 빛으로 물결이 흐른다. 살기는 핏빛이 맥박처럼 짙어졌다 옅어진다.</summary>
    static string Shimmer (string label, string[] c, bool blood)
    {
        Color baseC; ColorUtility.TryParseHtmlString(StatCol.Of(c), out baseC);
        Color hi = blood ? (MolakTheme.Night ? new Color(1f, 0.30f, 0.22f) : new Color(0.62f, 0.04f, 0.02f))
                         : (MolakTheme.Night ? Color.Lerp(baseC, Color.white, 0.75f) : Color.Lerp(baseC, new Color(0.80f, 0.58f, 0.12f), 0.65f));
        var sb = new System.Text.StringBuilder();
        int n = label.Length;
        float t = Time.unscaledTime;
        for (int i = 0; i < n; i++)
        {
            float k = blood ? 0.5f + 0.5f * Mathf.Sin(t * 3.2f) : 0.5f + 0.5f * Mathf.Sin(t * 2.4f - i * 1.1f);
            sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(Color.Lerp(baseC, hi, k))).Append('>').Append(label[i]).Append("</color>");
        }
        return "<b>" + sb + "</b>";
    }

    /// <summary>상태 줄의 이름 — 단계에 따라.</summary>
    static string StatName (string label, string key, string[] c)
    {
        int t = Tier(key);
        if (key == "sw") return SwordTag(label, false, true, t >= 2);
        if (t >= 2) return Shimmer(label, c, key == "sl");
        return Tag(label, c, t >= 1);
    }

    /// <summary>상태 줄의 숫자 — 막 바뀐 숫자는 좋음/나쁨 빛이 먼저, 아니면 단계에 따라 제 빛으로 굵게.</summary>
    static string StatNum (string key, float v, string[] c)
    {
        int k = System.Array.IndexOf(fkeys, key);
        if (k >= 0 && flashT[k] >= 0f) return Num(key, v);
        int t = Tier(key);
        string s = v.ToString("0");
        if (t == 0) return s;
        string col = key == "sl" ? (MolakTheme.Night ? (t >= 2 ? "#FF5A48" : "#E07A5E") : (t >= 2 ? "#8E140C" : "#7A2A14"))
                   : key == "sw" ? (MolakTheme.Night ? (t >= 2 ? "#E8F1F8" : "#B9CCDD") : (t >= 2 ? "#14324F" : "#2A4660"))
                   : StatCol.Of(c);
        return "<b><color=" + col + ">" + s + "</color></b>";
    }

    public static string Tag (string label, string[] c, bool bold = false)
    {
        string t = "<color=" + StatCol.Of(c) + ">" + label + "</color>";
        return bold ? "<b>" + t + "</b>" : t;
    }

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
        if (floats.Count == 0 && whyRows.Count == 0) return;
        if (sChip == null || !Mathf.Approximately(chipU, u)) { chipU = u; sChip = MolakGui.Label(34, u, TextAnchor.MiddleCenter, false); }
        float dt = Event.current.type == EventType.Repaint ? Time.unscaledDeltaTime : 0f;   // OnGUI 는 한 프레임에 여러 번 불린다
        for (int i = floats.Count - 1; i >= 0; i--)
        {
            var f = floats[i]; f.t += dt; floats[i] = f;
            if (f.t > ChipLife || Mathf.Round(f.d) == 0f) { floats.RemoveAt(i); continue; }
        }
        // 0.4.70 그림 위에 검은 패를 띄우지 않는다(언메이커: 그림 위로 수치가 뜨는 건 아니다).
        //   바뀐 수치와 까닭은 글자리 맨 아래 종이 쪽지 한 장에(낱말 풀이 쪽지와 같은 모양), 수치마다 한 줄.
        for (int i = whyRows.Count - 1; i >= 0; i--) if (Time.unscaledTime - whyRows[i].t0 > WhyLife) whyRows.RemoveAt(i);
        // 0.4.80 선택지가 뜨면 쪽지를 걷는다 — 글자리 맨 아래에 붙어 있어 아래 선택지 카드를 가렸다(0.4.79 검토). 지나간 까닭은 능력치 기록에 남는다.
        if (MolakTheme.ChoiceCount > 0 && whyRows.Count > 0) whyRows.Clear();
        SlipH = 0f;
        if (whyRows.Count > 0 && !MolakVfx.Holding && !MolakDuelCard.Visible && !MolakGiyeonCard.Showing)
        {
            if (sWhy == null || !Mathf.Approximately(whyU, u)) { whyU = u; sWhy = MolakGui.Label(24, u, TextAnchor.UpperLeft, true); sWhy.richText = true; }
            var ta = MolakVfx.TextArea();
            float ww = ta.width, pad = 12f * u;
            var sb = new System.Text.StringBuilder();
            float wa = 0f;
            foreach (var r in whyRows)
            {
                float age = Time.unscaledTime - r.t0;
                wa = Mathf.Max(wa, age < 0.2f ? age / 0.2f : age > WhyLife - 0.6f ? Mathf.Clamp01((WhyLife - age) / 0.6f) : 1f);
                bool good = r.key == "curse" ? r.d < 0 : r.d > 0;
                string col = r.key == "mn" ? "#8A6A12" : r.key == "curse" ? "#6B3FA0" : good ? "#2E6E37" : "#A33226";
                if (sb.Length > 0) sb.Append('\n');
                // 0.4.74 이름은 제 빛(종이 쪽지라 낮 빛), 숫자는 좋음/나쁨 빛
                string[] kc = r.key == "hp" ? StatCol.Hp : r.key == "ng" ? StatCol.Ng : r.key == "sl" ? StatCol.Sl : r.key == "sw" ? StatCol.Sw : r.key == "insight" ? StatCol.Ins : null;
                string nameCol = kc != null ? kc[0] : col;
                string nameTxt = r.key == "sw" ? SwordTag(MolakStatLog.Name(r.key), true, false) : "<color=" + nameCol + ">" + MolakStatLog.Name(r.key) + "</color>";
                sb.Append("<b>" + nameTxt + " <color=" + col + ">" + (r.d > 0 ? "+" : "−") + Mathf.Abs(Mathf.Round(r.d)).ToString("0") + "</color></b>  " + (string.IsNullOrEmpty(r.why) ? "" : r.why));
            }
            string wt = sb.ToString();
            float wh = sWhy.CalcHeight(new GUIContent(wt), ww - pad * 2f - 14f * u) + pad * 1.5f;
            var wr = new Rect(ta.x, ta.yMax - wh, ww, wh);
            SlipH = wh;
            // 0.4.80 창의 결: 흰 카드 · 먹 띠 · 아래 가는 줄(종이 쪽지라 먹색 화면에서도 낮 빛)
            GUI.color = new Color(1f, 0.988f, 0.961f, 0.97f * wa); GUI.DrawTexture(wr, MolakGui.Px);
            GUI.color = new Color(0.55f, 0.48f, 0.40f, 0.35f * wa); GUI.DrawTexture(new Rect(wr.x, wr.yMax - Mathf.Max(1f, u), wr.width, Mathf.Max(1f, u)), MolakGui.Px);
            GUI.color = new Color(0.122f, 0.102f, 0.078f, 0.9f * wa); GUI.DrawTexture(new Rect(wr.x, wr.y, Mathf.Max(2f, 8f * u), wr.height), MolakGui.Px);
            GUI.color = Color.white;
            MolakGui.Ink(sWhy, new Color(0.122f, 0.102f, 0.078f, wa));
            GUI.Label(new Rect(wr.x + pad + 14f * u, wr.y + pad * 0.75f, ww - pad * 2f - 14f * u, wh), wt, sWhy);
        }
        GUI.color = Color.white;
    }
    const float ChipLife = 3.2f, WhyLife = 5.5f;
    /// <summary>글자리 아래 수치 쪽지의 높이(낱말 풀이 쪽지가 그 위로 올라간다).</summary>
    public static float SlipH;
    GUIStyle sWhy; float whyU = -1f;
    struct WhyRow { public string key; public float d; public string why; public float t0; }
    readonly System.Collections.Generic.List<WhyRow> whyRows = new System.Collections.Generic.List<WhyRow>();
    void AddWhy (string key, float d, string why)
    {
        float now = Time.unscaledTime;
        for (int i = 0; i < whyRows.Count; i++)
            if (whyRows[i].key == key && now - whyRows[i].t0 < 0.8f)
            {
                var r = whyRows[i]; r.d += d; r.t0 = now;
                if (!string.IsNullOrEmpty(why) && (r.why ?? "").IndexOf(why) < 0) r.why = string.IsNullOrEmpty(r.why) ? why : r.why + " · " + why;
                if (Mathf.Round(r.d) == 0f) whyRows.RemoveAt(i); else whyRows[i] = r;
                return;
            }
        whyRows.Add(new WhyRow { key = key, d = d, why = why, t0 = now });
        while (whyRows.Count > 6) whyRows.RemoveAt(0);
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
        return MolakRealm.Current;   // 0.4.72 경지는 서열과 따로(벽과 폐관)
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
        MolakInputGuard.Guard(new Rect(X0, 0, W, y+lh+16*u + (expanded ? 104*u : 0f)));
        string hpCol = hp/hpm <= 0.34f ? (MolakTheme.Night ? "#E06A5A" : "#A33226") : "#" + ColorUtility.ToHtmlStringRGB(MolakTheme.InkText);
        // 날짜는 보이지 않는다(0.4.3) — 시간은 문양과 바깥 풍경으로만 느낀다
        // 0.4.74 이름마다 제 빛(언메이커: 검술·체력 같은 글씨에 각 컨셉에 맞는 색) — 숫자는 먹색, 막 바뀐 숫자만 푸르게/붉게
        bool lowHp = hp/hpm <= 0.34f;
        string stats =
            (lowHp ? "<color="+hpCol+">체력 "+Num("hp", hp)+"</color>" : StatName("체력", "hp", StatCol.Hp)+" "+StatNum("hp", hp, StatCol.Hp))+
            "    "+StatName("내공", "ng", StatCol.Ng)+" "+StatNum("ng", MolakStats.Get("ng"), StatCol.Ng)+
            "    "+StatName("살기", "sl", StatCol.Sl)+" "+StatNum("sl", MolakStats.Get("sl"), StatCol.Sl);
        GUI.Label(new Rect(X0+W*0.20f, y, W*0.62f, lh), stats, sStat);
        // 0.4.55 몸 상태 줄을 누르면 「몸의 셈」(각 수치가 무엇을 하는지)
        if (!MolakMenu.IsOpen && !MolakGuide.IsOpen && !MolakTruthView.IsOpen && !MolakStatLogView.IsOpen && GUI.Button(new Rect(X0+W*0.18f, y, W*0.64f, lh + (expanded ? 104*u : 0f)), GUIContent.none, GUIStyle.none)) MolakGuide.Read("body");
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
            string rtxt = (rank > 0 ? MolakDuel.RankText(rank) : "서열록에 이름 없음")+" · "+Tag(TierOf(rank), StatCol.Realm, true);
            { var bh = MolakStats.Str("byeolho", "none"); if (!string.IsNullOrEmpty(bh) && bh != "none") rtxt += " · <color=#8A3A30>" + bh + "</color>"; }   // 0.4.33 강호가 붙인 별호
            // 0.4.71 은자가 잘 안 보인다(언메이커) — 굵게, 낮엔 짙은 놋빛, 먹색 화면엔 밝은 금빛
            string mnCol = MolakTheme.Night ? "#E8C468" : "#6A4A00";
            string s2 = StatName("검술", "sw", StatCol.Sw)+" "+StatNum("sw", MolakStats.Get("sw"), StatCol.Sw)+"    <b><color="+mnCol+">은자 "+Num("mn", MolakStats.Get("mn"))+"냥</color></b>    "+rtxt;
            GUI.Label(new Rect(pad, y2, W-168*u, lh2), s2, sStat);
            // 0.4.72 다음 경지의 벽 — 무엇이 얼마나 남았나(검 · 기 · 깨달음)
            if (sWall == null || wallSrc != sStat || !Mathf.Approximately(wallU, u)) { wallU = u; wallSrc = sStat; sWall = new GUIStyle(sStat) { fontSize = Mathf.RoundToInt(sStat.fontSize * 0.78f) }; }
            GUI.Label(new Rect(pad, y2 + lh2 - 2*u, W-168*u, 32*u), MolakRealm.WallLine(), sWall);
        }

        // 0.4.57 능력치 변화 패: 상태 줄 바로 밑, 그림 띠 위
        DrawChips(X0, W, y + lh + (expanded ? 104*u : 16*u) + 18*u, u);

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
