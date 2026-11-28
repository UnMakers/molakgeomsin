using MolakVn;
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
    static bool synced; static object hookedSt; static float lastRuns = -1f, lastDay = -1f;
    /// <summary>옛 호출 자리(@fx). 이제는 Update 가 모든 변화를 잡으므로 아무것도 하지 않는다.</summary>
    public static void Delta (string key, float d) { }
    /// <summary>한꺼번에 바뀌는 순간(연습 몸 고르기 등) — 다음 프레임에 지금 값으로 다시 맞춘다.</summary>
    public static void Resync () { synced = false; }

    void Update ()
    {
        if (!Engine.Initialized) return;
        {   // 0.4.85 정적 깃발 대신 지금의 상태 관리자에 걸렸는지로 본다(플레이를 다시 켜면 옛 관리자에만 걸려 있던 것)
            var stm = Engine.GetService<IStateManager>();
            if (stm != null && !ReferenceEquals(stm, hookedSt)) { stm.OnResetStarted += () => synced = false; stm.OnGameLoadFinished += _ => synced = false; hookedSt = stm; }
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
            // 0.4.66 능력치 기록: 왜 바뀌었는지 한 줄
            string why = MolakStatLog.Resolve(fkeys[k]);
            if (why == MolakStatLog.QuietWhy) continue;   // 0.4.96 이야기 모드의 날짜 채우기는 알리지 않는다(읽는 화면에 「이야기 모드 — …」 알림이 줄줄이 떴다)
            flashT[k] = 0f; flashD[k] = d;
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
        // 0.5.5 (언메이커: '검' 글자만 이상하게 반짝인다) 칼등(첫 글자)이 너무 어두워 서슬이 지날 때 '검'만 번쩍였다 —
        //   두 글자의 바탕 빛을 가깝게, 서슬은 옅고 길게(0.6→0.9초) 지나가게.
        Color back = night ? new Color(0.68f, 0.75f, 0.82f) : new Color(0.19f, 0.27f, 0.36f);   // 칼등
        Color edge = night ? new Color(0.84f, 0.89f, 0.94f) : new Color(0.24f, 0.35f, 0.47f);   // 날
        Color glint = night ? new Color(1f, 1f, 1f) : new Color(0.36f, 0.56f, 0.74f);          // 서슬
        if (peak) glint = night ? new Color(1f, 0.93f, 0.70f) : new Color(0.66f, 0.50f, 0.16f);   // 극(極): 서슬이 금빛으로, 더 자주
        float period = peak ? 2.4f : 4.2f, sweep = 0.9f;
        float ph = Time.unscaledTime % period;
        float p = ph < sweep ? Mathf.Lerp(-0.6f, 1.6f, ph / sweep) : -9f;
        var sb = new System.Text.StringBuilder();
        int n = label.Length;
        for (int i = 0; i < n; i++)
        {
            float x = n <= 1 ? 0.5f : i / (float)(n - 1);
            var c = Color.Lerp(back, edge, x);
            float g = Mathf.Exp(-Mathf.Pow((x - p) / 0.45f, 2f));
            c = Color.Lerp(c, glint, g * 0.55f);
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
        // 0.4.87 쪽지는 시간으로만 걷는다(글을 넘겨도 남는다). 선택지·멈춤 장면·비무 패가 떠 있는 동안은 접어 두고 시간을 멈춘다 —
        //   0.4.80 엔 선택지가 뜨면 걷어 버려서, 누르자마자 사라지는 것처럼 보였다(낱말 쪽지와 같은 기준으로).
        bool foldWhy = MolakTheme.ChoiceCount > 0 || MolakVfx.Holding || MolakDuelCard.Visible || MolakGiyeonCard.Showing;
        if (foldWhy) { for (int i = 0; i < whyRows.Count; i++) { var r0 = whyRows[i]; r0.t0 += dt; whyRows[i] = r0; } }
        for (int i = whyRows.Count - 1; i >= 0; i--) if (Time.unscaledTime - whyRows[i].t0 > WhyLife) whyRows.RemoveAt(i);
        SlipH = 0f;
        if (whyRows.Count > 0 && !foldWhy)
        {
            if (sWhy == null || !Mathf.Approximately(whyU, u)) { whyU = u; sWhy = MolakGui.Label(28, u, TextAnchor.UpperLeft, true); sWhy.richText = true; }
            var ta = MolakVfx.SlipArea();   // 0.4.82 그림 화면이면 글자리 위 — 글자리를 가리지 않는다
            bool dark = MolakVfx.DarkSlip;
            float ww = ta.width, pad = 12f * u;
            var sb = new System.Text.StringBuilder();
            float wa = 0f;
            foreach (var r in whyRows)
            {
                float age = Time.unscaledTime - r.t0;
                wa = Mathf.Max(wa, age < 0.2f ? age / 0.2f : age > WhyLife - 0.6f ? Mathf.Clamp01((WhyLife - age) / 0.6f) : 1f);
                bool good = r.key == "curse" ? r.d < 0 : r.d > 0;
                string col = dark ? (r.key == "mn" ? "#E8C468" : r.key == "curse" ? "#B48AE0" : good ? "#7FD08A" : "#FF7A66")
                                  : (r.key == "mn" ? "#8A6A12" : r.key == "curse" ? "#6B3FA0" : good ? "#2E6E37" : "#A33226");
                if (sb.Length > 0) sb.Append('\n');
                // 0.4.74 이름은 제 빛(종이 쪽지라 낮 빛), 숫자는 좋음/나쁨 빛
                string[] kc = r.key == "hp" ? StatCol.Hp : r.key == "ng" ? StatCol.Ng : r.key == "sl" ? StatCol.Sl : r.key == "sw" ? StatCol.Sw : r.key == "insight" ? StatCol.Ins : null;
                string nameCol = kc != null ? kc[dark ? 1 : 0] : col;
                string nameTxt = r.key == "sw" ? SwordTag(MolakStatLog.Name(r.key), !dark, false) : "<color=" + nameCol + ">" + MolakStatLog.Name(r.key) + "</color>";
                sb.Append("<b>" + nameTxt + " <color=" + col + ">" + (r.d > 0 ? "+" : "−") + Mathf.Abs(Mathf.Round(r.d)).ToString("0") + "</color></b>  " + (string.IsNullOrEmpty(r.why) ? "" : r.why));
            }
            string wt = sb.ToString();
            float wh = sWhy.CalcHeight(new GUIContent(wt), ww - pad * 2f - 14f * u) + pad * 1.5f;
            var wr = new Rect(ta.x, ta.yMax - wh, ww, wh);
            SlipH = wh;
            // 0.4.80 창의 결: 흰 카드 · 먹 띠 · 아래 가는 줄(종이 쪽지라 먹색 화면에서도 낮 빛)
            if (dark)
            {   // 그림 화면: 옻칠 쪽지 · 금 띠 · 밝은 글
                GUI.color = new Color(0.067f, 0.059f, 0.055f, 0.92f * wa); GUI.DrawTexture(wr, MolakGui.Px);
                GUI.color = new Color(0.83f, 0.71f, 0.42f, 0.45f * wa); GUI.DrawTexture(new Rect(wr.x, wr.yMax - Mathf.Max(1f, u), wr.width, Mathf.Max(1f, u)), MolakGui.Px);
                GUI.color = new Color(0.83f, 0.71f, 0.42f, 0.95f * wa); GUI.DrawTexture(new Rect(wr.x, wr.y, Mathf.Max(2f, 6f * u), wr.height), MolakGui.Px);
                GUI.color = Color.white;
                MolakGui.Ink(sWhy, new Color(0.90f, 0.86f, 0.80f, wa));
            }
            else
            {
            GUI.color = new Color(1f, 0.988f, 0.961f, 0.97f * wa); GUI.DrawTexture(wr, MolakGui.Px);
            GUI.color = new Color(0.55f, 0.48f, 0.40f, 0.35f * wa); GUI.DrawTexture(new Rect(wr.x, wr.yMax - Mathf.Max(1f, u), wr.width, Mathf.Max(1f, u)), MolakGui.Px);
            GUI.color = new Color(0.122f, 0.102f, 0.078f, 0.9f * wa); GUI.DrawTexture(new Rect(wr.x, wr.y, Mathf.Max(2f, 8f * u), wr.height), MolakGui.Px);
            GUI.color = Color.white;
            MolakGui.Ink(sWhy, new Color(0.122f, 0.102f, 0.078f, wa));
            }
            GUI.Label(new Rect(wr.x + pad + 14f * u, wr.y + pad * 0.75f, ww - pad * 2f - 14f * u, wh), wt, sWhy);
        }
        GUI.color = Color.white;
    }
    const float ChipLife = 3.2f, WhyLife = 8.25f;   // 0.4.87 5.5 → ×1.5
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
        // 0.4.87 한 쪽지에 함께 뜬 줄은 함께 걷힌다 — 새 줄이 붙으면 앞줄들의 시계도 지금으로 맞춘다(줄마다 따로 빠지면 쪽지가 들쭉날쭉 줄어든다)
        for (int i = 0; i < whyRows.Count; i++) { var r0 = whyRows[i]; r0.t0 = now; whyRows[i] = r0; }
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
        GUI.depth = 0;
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
        if (MolakTheme.CinemaNow) { DrawCine(w, X0, W, u, hp, hpm, curse); DrawRankShift(w, u); return; }
        if (MolakTheme.TheaterNow) { DrawTheater(w, X0, W, u, hp, hpm, curse); DrawRankShift(w, u); return; }
        WarnShown = false;

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
        if (!MolakMenu.IsOpen && !MolakGuide.IsOpen && !MolakTruthView.IsOpen && !MolakStatLogView.IsOpen && !MolakBacklog.IsOpen && GUI.Button(new Rect(X0+W*0.18f, y, W*0.64f, lh + (expanded ? 104*u : 0f)), GUIContent.none, GUIStyle.none)) MolakGuide.Read("body");
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

    // ================= 0.4.82 그림 화면(F1) 위 수치 줄 =================
    // 왼쪽: 금빛 한자 이름 + 숫자(體 11/14 · 氣 · 殺 · 劍 · 銀 · 경지) 한 줄, 그 아래 가는 체력 선.
    // 오른쪽: 둥근 금테 단추 囊(행낭, 지닌 수) · ☰(메뉴 — MolakMenu 가 같은 자리에 그린다). 아래 글자리엔 서 있는 사람의 이름 인장.
    /// <summary>수치 줄 아래 몸의 업보 한 줄(부상·독·빚)이 떠 있나 — 마디 이름 표식이 그만큼 내려간다.</summary>
    public static bool WarnShown;
    GUIStyle sCH, sCN, sCS, sCW, sCB, sSealN; float cineU = -1f;
    static Texture2D hpGlow;
    /// <summary>0.4.85 피통: 양끝이 가늘고 가운데가 밝게 번지는 붉은 획(시안 두 번째).</summary>
    public static Texture2D HpGlow
    {
        get
        {
            if (hpGlow != null) return hpGlow;
            // 0.4.85 언메이커: 너무 굵다 → 가는 붉은 심 한 가닥 + 둘레에 옅게 번지는 빛(양끝은 바늘처럼).
            const int W = 512, H = 32;
            hpGlow = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[W * H];
            float cy = (H - 1) * 0.5f;
            for (int xx = 0; xx < W; xx++)
            {
                float t = xx / (W - 1f);
                float taper = t < 0.55f ? 1f : Mathf.Pow(Mathf.Cos((t - 0.55f) / 0.45f * Mathf.PI * 0.5f), 0.7f);   // 왼쪽은 꽉, 오른쪽만 바늘 끝
                float coreW = 0.5f + 1.6f * taper;          // 심 반폭(px)
                float glowW = 1.5f + 7.5f * taper;          // 번짐 반폭(px)
                for (int yy = 0; yy < H; yy++)
                {
                    float d = Mathf.Abs(yy - cy);
                    float core = Mathf.Clamp01(1f - d / coreW);
                    float glow = Mathf.Exp(-(d * d) / (glowW * glowW)) * 0.55f * taper;
                    float hot = Mathf.Clamp01(1f - d / (coreW * 0.55f)) * taper;
                    float a = Mathf.Clamp01(core + glow);
                    var c = Color.Lerp(new Color(0.78f, 0.08f, 0.12f), new Color(1f, 0.30f, 0.30f), core);
                    c = Color.Lerp(c, new Color(1f, 0.86f, 0.82f), hot * 0.85f);
                    px[yy * W + xx] = new Color(c.r, c.g, c.b, a);
                }
            }
            hpGlow.SetPixels32(px); hpGlow.Apply();
            return hpGlow;
        }
    }
    static readonly string[] RealmHanja = { "삼류", "이류", "일류", "절정", "초절정", "화경", "현경", "생사경", "신화경" };   // 0.4.85 언메이커: 체력 줄은 기본 한글로
    public static Rect CineBagButton (float w, float u)
    {
        float W = Mathf.Min(w, 1080*u), X0 = (w-W)/2f, b = MolakLayout.CineBtnU*u;
        return new Rect(X0 + W - 24*u - b*2f - 20*u, MolakLayout.CineTopU*u, b, b);
    }
    public static Rect CineMenuButton (float w, float u)
    {
        float W = Mathf.Min(w, 1080*u), X0 = (w-W)/2f, b = MolakLayout.CineBtnU*u;
        return new Rect(X0 + W - 24*u - b, MolakLayout.CineTopU*u, b, b);
    }

    void DrawCineStyles (float u)
    {
        if (sCH == null || !Mathf.Approximately(cineU, u))
        {
            cineU = u;
            var hj = MolakGui.Hanja != null ? MolakGui.Hanja : gowun;
            sCH = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(25*u), alignment = TextAnchor.MiddleLeft, richText = true, wordWrap = false, clipping = TextClipping.Overflow }; sCH.font = gowun;   // 0.4.85 한글 이름(한자 글꼴엔 한글이 없다)
            sCN = new GUIStyle(sCH) { fontSize = Mathf.RoundToInt(31*u) }; sCN.font = gowun;
            sCS = new GUIStyle(sCN) { fontSize = Mathf.RoundToInt(23*u) };
            sCW = new GUIStyle(sCN) { fontSize = Mathf.RoundToInt(24*u) };
            sCB = new GUIStyle(sCH) { fontSize = Mathf.RoundToInt(38*u), alignment = TextAnchor.MiddleCenter };
            sSealN = new GUIStyle(sCN) { fontSize = Mathf.RoundToInt(32*u), alignment = TextAnchor.MiddleCenter }; sSealN.font = MolakTypography.Heading(gowun);
            MolakGui.Ink(sCN, new Color(0.93f, 0.89f, 0.83f, 1f));
            MolakGui.Ink(sCS, new Color(0.66f, 0.60f, 0.53f, 1f));
            MolakGui.Ink(sCW, new Color(0.88f, 0.55f, 0.47f, 1f));
            MolakGui.Ink(sCB, new Color(0.83f, 0.71f, 0.42f, 1f));
            MolakGui.Ink(sSealN, new Color(0.95f, 0.91f, 0.85f, 1f));
        }
    }
    void DrawCine (float w, float X0, float W, float u, float hp, float hpm, float curse)
    {
        DrawCineStyles(u);
        // 저주: 맨 위 가는 줄(차오를수록 보랏빛, 깊으면 핏빛으로 맥이 뛴다)
        if (curse > 0f)
        {
            var cc = new Color(0.52f,0.36f,0.66f,0.9f);
            if (curse >= 80f) { float pulse = 0.6f+0.4f*Mathf.Sin(Time.time*5f); cc = Color.Lerp(cc, new Color(0.72f,0.18f,0.14f,1f), pulse); }
            Box(new Rect(0, 0, w, 5*u), new Color(0f, 0f, 0f, 0.35f));
            Box(new Rect(0, 0, w*Mathf.Clamp01(curse/100f), 5*u), cc);
        }
        float top = MolakLayout.CineTopU*u, bH = MolakLayout.CineBtnU*u;
        var bagR = CineBagButton(w, u);
        float x = X0 + 44*u, rowY = top + 6*u, rowH = 50*u, maxX = bagR.x - 24*u;
        bool lowHp = hp/hpm <= 0.34f;
        // 숫자들
        string[] keys = { "hp", "ng", "sl", "sw", "mn" };
        string[] hz = { "체력", "내공", "살기", "검술", "은자" };
        string[][] cols = { StatCol.Hp, StatCol.Ng, StatCol.Sl, StatCol.Sw, StatCol.Realm };
        float gap = 26*u;
        // 다 놓았을 때 넘치면 간격부터 줄인다
        float[] lw = new float[6], nw = new float[6]; string[] ls = new string[6], ns = new string[6];
        float total = 0f;
        for (int i = 0; i < 5; i++)
        {
            float v = MolakStats.Get(keys[i]);
            int tier = Tier(keys[i]);
            ls[i] = keys[i] == "sw" ? SwordTag(hz[i], false, false, tier >= 2) : "<color=" + StatCol.Of(cols[i]) + ">" + hz[i] + "</color>";
            ns[i] = keys[i] == "hp" ? (lowHp ? "<color=#E06A5A>" + Num("hp", hp) + "</color>" : StatNum("hp", hp, StatCol.Hp)) + "<size=" + Mathf.RoundToInt(22*u) + "><color=#A89A86>/" + hpm.ToString("0") + "</color></size>"
                  : keys[i] == "mn" ? Num("mn", v) : StatNum(keys[i], v, cols[i]);
            lw[i] = sCH.CalcSize(new GUIContent(ls[i])).x; nw[i] = sCN.CalcSize(new GUIContent(ns[i])).x;
            total += lw[i] + 8*u + nw[i];
        }
        ls[5] = "<color=" + StatCol.Of(StatCol.Realm) + ">" + RealmHanja[Mathf.Clamp(MolakRealm.Realm, 0, 8)] + "</color>";
        lw[5] = sCH.CalcSize(new GUIContent(ls[5])).x * 0.9f;
        total += lw[5];
        float avail = maxX - x;
        if (total + gap*5 > avail) gap = Mathf.Max(8*u, (avail - total) / 5f);
        float rx = x;
        for (int i = 0; i < 5; i++)
        {
            GUI.Label(new Rect(rx, rowY, lw[i] + 4*u, rowH), ls[i], sCH); rx += lw[i] + 8*u;
            GUI.Label(new Rect(rx, rowY + 1*u, nw[i] + 4*u, rowH), ns[i], sCN); rx += nw[i] + gap;
        }
        { var g0 = sCH.fontSize; sCH.fontSize = Mathf.RoundToInt(25*u); GUI.Label(new Rect(rx, rowY + 2*u, lw[5] + 10*u, rowH), ls[5], sCH); sCH.fontSize = g0; }
        // 누르면 「몸의 셈」
        var statR = new Rect(x - 10*u, top, maxX - x + 10*u, bH);
        MolakInputGuard.Guard(new Rect(X0, 0, W, top + bH + 20*u));
        if (!MolakMenu.IsOpen && !MolakGuide.IsOpen && !MolakTruthView.IsOpen && !MolakStatLogView.IsOpen && !MolakBacklog.IsOpen && !MolakBag.IsOpen && GUI.Button(statR, GUIContent.none, GUIStyle.none)) MolakGuide.Read("body");
        // 체력 선: 2단위 굵기, 연지에서 밝은 연분홍으로. 바닥이면 붉게 맥이 뛴다.
        {
            float lineW = 384*u, ly = rowY + rowH + 10*u, th = Mathf.Max(2f, 4*u);
            float rr = Mathf.Clamp01(hp/hpm);
            bool low = rr <= 0.34f;
            if (low && !wasLow && Event.current.type == EventType.Repaint) MolakSfx.Heart();
            if (Event.current.type == EventType.Repaint) wasLow = low;
            float pul = low ? 0.7f + 0.3f*Mathf.Sin(Time.time*4f) : 1f;
            Box(new Rect(x, ly, lineW, th), new Color(0.93f, 0.89f, 0.83f, 0.18f));
            var c0 = low ? new Color(0.75f, 0.16f, 0.13f, pul) : new Color(0.69f, 0.19f, 0.25f, 1f);
            var c1 = low ? new Color(0.92f, 0.36f, 0.30f, pul) : new Color(0.88f, 0.48f, 0.53f, 1f);
            int n = 24; float fw = lineW * rr, sw = fw / n;
            for (int i = 0; i < n && fw > 0.5f; i++) Box(new Rect(x + sw*i, ly, sw + 0.6f, th), Color.Lerp(c0, c1, (i + 0.5f) / n));
            // 몸의 업보(부상·독·빚)는 선 아래 한 줄로 — 없으면 아무것도
            string warn = Warn();
            WarnShown = warn.Length > 0;
            if (WarnShown) GUI.Label(new Rect(x, ly + 10*u, maxX - x, 36*u), warn, sCW);
        }
        // 囊 단추
        {
            GUI.DrawTexture(bagR, MolakGui.Ring);
            { var f0 = sCB.fontSize; sCB.fontSize = Mathf.RoundToInt(f0 * 0.62f); GUI.Label(bagR, "행낭", sCB); sCB.fontSize = f0; }
            int cnt = MolakBag.HaveCount;
            if (cnt > 0)
            {
                float d = 38*u; var br = new Rect(bagR.xMax - d + 4*u, bagR.y - 4*u, d, d);
                var g = GUI.color; GUI.color = new Color(0.61f, 0.18f, 0.14f, 1f); GUI.DrawTexture(br, MolakGui.Disc); GUI.color = g;
                var ss = sCS.fontSize; var sc = sCS.normal.textColor; var sa = sCS.alignment;
                sCS.fontSize = Mathf.RoundToInt(22*u); sCS.alignment = TextAnchor.MiddleCenter; MolakGui.Ink(sCS, new Color(0.93f, 0.89f, 0.83f, 1f));
                GUI.Label(br, cnt > 99 ? "99" : cnt.ToString(), sCS);
                sCS.fontSize = ss; sCS.alignment = sa; MolakGui.Ink(sCS, sc);
            }
            var hit = new Rect(bagR.x - 12*u, bagR.y - 12*u, bagR.width + 24*u, bagR.height + 24*u);
            MolakInputGuard.Guard(hit);
            if (!MolakBag.IsOpen && !MolakGuide.IsOpen && GUI.Button(hit, GUIContent.none, GUIStyle.none)) { MolakSfx.Confirm(); MolakBag.Open(); }
        }
        // 이름 인장: 그림 속 사람이 말하는 중일 때만(마지막 문단이 그 사람 쪽 대사) — 글자리 왼쪽 위 모서리에 걸친다
        {
            var pr = MolakScroll.CinemaPanelGui;
            string nm = MolakBridge.NameOf(MolakArt.CharId);
            if (pr.width > 1f && MolakScroll.CinemaPanelShown && MolakScroll.CharSpeaking && !string.IsNullOrEmpty(nm) && MolakBag.PanelTopPx >= Screen.height - 1f)
            {
                float a = 1f - MolakVfx.ChapterCover;
                var sz = sSealN.CalcSize(new GUIContent(nm));
                var sr = new Rect(pr.x + 40*u, pr.y - 30*u, sz.x + 52*u, 62*u);
                var g = GUI.color;
                GUI.color = new Color(0.83f, 0.71f, 0.42f, a); GUI.DrawTexture(sr, Px);
                float l = Mathf.Max(1f, 2*u);
                GUI.color = new Color(0.55f, 0.16f, 0.12f, a); GUI.DrawTexture(new Rect(sr.x + l, sr.y + l, sr.width - 2*l, sr.height - 2*l), Px);
                GUI.color = g;
                GUI.Label(new Rect(sr.x, sr.y + 1*u, sr.width, sr.height), nm, sSealN);
            }
        }
        // 능력치 변화 패: 수치 줄 아래
        DrawChips(X0, W, top + bH + (WarnShown ? 60*u : 24*u), u);
    }

    // ================= 0.4.82 극장판(D, 시안 DE4) =================
    // 위 옻칠 띠: 첫 줄 마디 이름(금) · ☰, 둘째 줄 體[가는 선] 숫자 · 氣 · 殺 · 劍 · 銀 · 경지. 그 아래 금줄 두 겹 사이가 그림 창.
    // 맨 아래 행낭 띠: 行囊 + 최근에 얻은 셋. 누르면 행낭이 띠 위에서 올라온다.
    public static Rect TheaterMenuButton (float w, float u)
    {
        float W = Mathf.Min(w, 1080*u), X0 = (w-W)/2f, s = MolakLayout.CanvasPx;
        float cy = (MolakLayout.ThInsetTop + MolakLayout.ThPinLineY) * s;   // 마디 이름 줄 오른쪽(고정)
        return new Rect(X0 + W - 20*u - 96*u, cy - 42*u, 96*u, 84*u);
    }
    GUIStyle sThPin, sThChip, sThBag; float thU = -1f;
    void DrawTheater (float w, float X0, float W, float u, float hp, float hpm, float curse)
    {
        float s = MolakLayout.CanvasPx, h = Screen.height;
        GUI.depth = 10;   // 위 띠·아래 띠는 ☰·행낭·창들 뒤에 깔린다
        if (sThPin == null || !Mathf.Approximately(thU, u))
        {
            thU = u;
            var hf = MolakTypography.Heading(gowun);
            sThPin = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(30*u), alignment = TextAnchor.MiddleLeft, richText = true, wordWrap = false, clipping = TextClipping.Clip }; sThPin.font = hf;
            sThBag = new GUIStyle(sThPin) { fontSize = Mathf.RoundToInt(28*u), alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Overflow }; sThBag.font = gowun;
            sThChip = new GUIStyle(sThPin) { fontSize = Mathf.RoundToInt(24*u), alignment = TextAnchor.MiddleCenter }; sThChip.font = gowun;
            MolakGui.Ink(sThPin, new Color(0.83f, 0.71f, 0.42f, 1f)); MolakGui.Ink(sThBag, new Color(0.83f, 0.71f, 0.42f, 1f)); MolakGui.Ink(sThChip, new Color(0.93f, 0.89f, 0.83f, 1f));
        }
        DrawCineStyles(u);
        var gold = new Color(0.83f, 0.71f, 0.42f, 0.7f);
        // 위 띠 · 금줄
        float bandB = MolakLayout.ThTopBand * s;
        Box(new Rect(0, 0, w, MolakLayout.ThArtTop * s), new Color(0.067f, 0.059f, 0.055f, 1f));
        Box(new Rect(0, bandB, w, Mathf.Max(1f, 2f * s)), gold);
        Box(new Rect(0, MolakLayout.ThArtBottom * s, w, Mathf.Max(1f, 2f * s)), gold);
        Box(new Rect(0, (MolakLayout.ThArtBottom + 8f) * s, w, Mathf.Max(1f, 1.5f * s)), new Color(0.83f, 0.71f, 0.42f, 0.25f));
        if (curse > 0f)
        {
            var cc = new Color(0.52f,0.36f,0.66f,0.9f);
            if (curse >= 80f) { float pulse = 0.6f+0.4f*Mathf.Sin(Time.time*5f); cc = Color.Lerp(cc, new Color(0.72f,0.18f,0.14f,1f), pulse); }
            Box(new Rect(0, 0, w*Mathf.Clamp01(curse/100f), 4*u), cc);
        }
        float x = X0 + 40*u;
        var menuR = TheaterMenuButton(w, u);
        // 0.4.83 위 띠는 수치 한 줄로 고정(마디 이름이 떠도 움직이지 않게). 마디 이름은 그림 창 왼쪽 위, 몸의 업보는 그림 창 오른쪽 위.
        // 0.4.83 위 띠 두 줄(언메이커): 수치는 아래 금줄에 붙여 고정, 그 위 남는 줄에 마디 이름(없으면 비워 둔다) · ☰.
        //   몸의 업보(부상·독·빚)는 마디 이름 줄 오른쪽(☰ 왼쪽)에 붉게.
        float l1 = (MolakLayout.ThInsetTop + MolakLayout.ThPinLineY) * s;
        string pin = MolakVfx.PinText;
        if (!string.IsNullOrEmpty(pin)) GUI.Label(new Rect(x, l1 - 26*u, menuR.x - x - 20*u, 52*u), pin, sThPin);
        string warn = Warn(); WarnShown = false;
        if (warn.Length > 0)
        {
            var wsz = sCW.CalcSize(new GUIContent(warn));
            var ws = sCW.alignment; sCW.alignment = TextAnchor.MiddleRight;
            GUI.Label(new Rect(menuR.x - 16*u - wsz.x - 4*u, l1 - 22*u, wsz.x + 4*u, 44*u), warn, sCW);
            sCW.alignment = ws;
        }
        // 수치 한 줄(아래 금줄 위에 고정)
        // 0.4.85 언메이커: 수치 한 줄 + 그 아래 체력 피통(양끝이 가늘고 가운데가 밝게 번지는 붉은 획)
        float l2 = (MolakLayout.ThInsetTop + MolakLayout.ThStatLineY) * s - 12*u, rowH = 50*u, ry = l2 - rowH * 0.5f;
        bool lowHp = hp/hpm <= 0.34f;
        string[] keys = { "hp", "ng", "sl", "sw", "mn" };
        string[] hz = { "체력", "내공", "살기", "검술", "은자" };
        string[][] cols = { StatCol.Hp, StatCol.Ng, StatCol.Sl, StatCol.Sw, StatCol.Realm };
        float rx = x, gap = 20*u;
        for (int i = 0; i < 5; i++)
        {
            float v = MolakStats.Get(keys[i]);
            int tier = Tier(keys[i]);
            string ls = keys[i] == "sw" ? SwordTag(hz[i], false, false, tier >= 2) : "<color=" + StatCol.Of(cols[i]) + ">" + hz[i] + "</color>";
            string ns = keys[i] == "hp" ? (lowHp ? "<color=#E06A5A>" + Num("hp", hp) + "</color>" : StatNum("hp", hp, StatCol.Hp)) : keys[i] == "mn" ? Num("mn", v) : StatNum(keys[i], v, cols[i]);
            float lw = sCH.CalcSize(new GUIContent(ls)).x;
            GUI.Label(new Rect(rx, ry, lw + 4*u, rowH), ls, sCH); rx += lw + 10*u;
            float nw = sCN.CalcSize(new GUIContent(ns)).x;
            GUI.Label(new Rect(rx, ry + 1*u, nw + 4*u, rowH), ns, sCN); rx += nw + gap;
        }
        { string rl = "<color=" + StatCol.Of(StatCol.Realm) + ">" + RealmHanja[Mathf.Clamp(MolakRealm.Realm, 0, 8)] + "</color>"; var g0 = sCH.fontSize; sCH.fontSize = Mathf.RoundToInt(25*u); GUI.Label(new Rect(rx, ry + 2*u, 200*u, rowH), rl, sCH); sCH.fontSize = g0; }
        if (false) {   // 2026-11-27 언메이커: 피통(체력 바) 뺌 — 글 자리를 한 줄 더 얻는다
            string hpRl = RealmHanja[Mathf.Clamp(MolakRealm.Realm, 0, 8)];
            float hpEnd = rx + sCH.CalcSize(new GUIContent(hpRl)).x;
            float hbW = Mathf.Min(280*u, hpEnd - x), hbH = 12*u, hbY = l2 + rowH * 0.5f - 3*u;
            float hpR = Mathf.Clamp01(hp/hpm);
            bool hpLow = hpR <= 0.34f;
            if (hpLow && !wasLow && Event.current.type == EventType.Repaint) MolakSfx.Heart();
            if (Event.current.type == EventType.Repaint) wasLow = hpLow;
            float hpPul = hpLow ? 0.65f + 0.35f*Mathf.Sin(Time.time*4f) : 1f;
            var hpTex = HpGlow;
            var hpG = GUI.color;
            GUI.color = new Color(0.93f, 0.89f, 0.83f, 0.10f);
            GUI.DrawTexture(new Rect(x, hbY, hbW, hbH), hpTex);
            GUI.color = hpLow ? new Color(1f, 0.55f, 0.5f, hpPul) : Color.white;
            if (hpR > 0f) GUI.DrawTexture(new Rect(x, hbY, hbW * hpR, hbH), hpTex);   // 줄어도 끝은 늘 뾰족하게
            GUI.color = hpG;
        }
        MolakInputGuard.Guard(new Rect(0, 0, w, bandB));
        if (!MolakMenu.IsOpen && !MolakGuide.IsOpen && !MolakTruthView.IsOpen && !MolakStatLogView.IsOpen && !MolakBacklog.IsOpen && !MolakBag.IsOpen && GUI.Button(new Rect(x - 10*u, l2 - 40*u, menuR.x - x, 80*u), GUIContent.none, GUIStyle.none)) MolakGuide.Read("body");
        // 맨 아래 띠: 왼쪽 「앞 쪽」, 가운데 行囊 + 최근에 얻은 것(누르면 행낭), 오른쪽 「자동」「빨리」(0.4.83 — 메뉴를 열지 않고 바로)
        if (MolakBag.PanelTopPx >= Screen.height - 1f)
        {
            float bb = MolakLayout.ThBottomBand * s, by0 = h - bb;
            var br = new Rect(0, by0, w, bb);
            Box(br, new Color(0.09f, 0.078f, 0.07f, 1f));
            Box(new Rect(0, by0, w, Mathf.Max(1f, 1.5f * s)), new Color(0.83f, 0.71f, 0.42f, 0.45f));
            float cy = by0 + MolakLayout.ThBottomBandH * s * 0.5f;
            MolakInputGuard.Guard(br);
            bool free = !MolakBag.IsOpen && !MolakGuide.IsOpen && !MolakMenu.IsOpen;
            var on = new Color(0.91f, 0.78f, 0.47f, 1f); var off = new Color(0.66f, 0.60f, 0.53f, 1f);
            // 왼쪽: 앞 쪽
            var backR = new Rect(X0 + 20*u, cy - 34*u, 150*u, 68*u);
            MolakGui.Ink(sThChip, MolakPageBack.Viewing ? on : off);
            GUI.Label(backR, "◁ 앞 쪽", sThChip);
            if (free && GUI.Button(backR, GUIContent.none, GUIStyle.none)) { MolakSfx.Confirm(); MolakPageBack.Back(); }
            // 오른쪽: 자동 · 빨리
            var fastR = new Rect(X0 + W - 20*u - 100*u, cy - 34*u, 100*u, 68*u);
            var autoR = new Rect(fastR.x - 100*u, cy - 34*u, 100*u, 68*u);
            MolakGui.Ink(sThChip, MolakPlayMode.Auto ? on : off); GUI.Label(autoR, "자동", sThChip);
            if (MolakPlayMode.Auto) Box(new Rect(autoR.x + 26*u, autoR.yMax - 14*u, autoR.width - 52*u, Mathf.Max(1f, 2*u)), on);
            MolakGui.Ink(sThChip, MolakPlayMode.Fast ? on : off); GUI.Label(fastR, "빨리", sThChip);
            if (MolakPlayMode.Fast) Box(new Rect(fastR.x + 26*u, fastR.yMax - 14*u, fastR.width - 52*u, Mathf.Max(1f, 2*u)), on);
            if (free && GUI.Button(autoR, GUIContent.none, GUIStyle.none)) { MolakSfx.Confirm(); MolakPlayMode.SetAuto(!MolakPlayMode.Auto); }
            if (free && GUI.Button(fastR, GUIContent.none, GUIStyle.none)) { MolakSfx.Confirm(); MolakPlayMode.SetFast(!MolakPlayMode.Fast); }
            MolakGui.Ink(sThChip, new Color(0.93f, 0.89f, 0.83f, 1f));
            // 가운데: 行囊 + 최근에 얻은 것(자리에 맞는 만큼)
            float midL = backR.xMax + 16*u, midR = autoR.x - 16*u;
            var rec = MolakBag.Recent(3);
            float cw0 = sThBag.CalcSize(new GUIContent("행낭")).x;
            var ws2 = new System.Collections.Generic.List<float>(); float tot = cw0;
            // 0.4.85 칸보다 긴 이름은 잘리던 것(언메이커) → 칸에 맞게 줄이고 「…」
            for (int ri = 0; ri < rec.Count; ri++)
            {
                string t0 = rec[ri];
                while (t0.Length > 2 && sThChip.CalcSize(new GUIContent(t0)).x + 28*u > 240*u) t0 = t0.Substring(0, t0.Length - 1);
                if (t0 != rec[ri]) rec[ri] = t0.TrimEnd(' ', '·') + "…";
            }
            foreach (var nm in rec)
            {
                float wv = sThChip.CalcSize(new GUIContent(nm)).x + 28*u;
                if (tot + 16*u + wv > midR - midL) break;
                ws2.Add(wv); tot += 16*u + wv;
            }
            float cx = midL + (midR - midL - tot) / 2f;
            GUI.Label(new Rect(cx, cy - 26*u, cw0, 52*u), "행낭", sThBag); cx += cw0 + 16*u;
            for (int i = 0; i < ws2.Count; i++)
            {
                var r = new Rect(cx, cy - 22*u, ws2[i], 44*u);
                float l = Mathf.Max(1f, 1.5f*u); var ec = new Color(0.83f, 0.71f, 0.42f, 0.4f);
                Box(new Rect(r.x, r.y, r.width, l), ec); Box(new Rect(r.x, r.yMax - l, r.width, l), ec); Box(new Rect(r.x, r.y, l, r.height), ec); Box(new Rect(r.xMax - l, r.y, l, r.height), ec);
                var cl = sThChip.clipping; sThChip.clipping = TextClipping.Clip; GUI.Label(r, rec[i], sThChip); sThChip.clipping = cl;
                cx += ws2[i] + 16*u;
            }
            var bagR = new Rect(midL, by0, midR - midL, MolakLayout.ThBottomBandH * s);
            if (free && GUI.Button(bagR, GUIContent.none, GUIStyle.none)) { MolakSfx.Confirm(); MolakBag.Open(); }
        }
        DrawChips(X0, W, 0f, u);
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
