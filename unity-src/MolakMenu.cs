using Naninovel;
using UnityEngine;

/// <summary>모바일 한국어 메뉴 — 우상단 ☰ → 자동/스킵/기록/저장/불러오기/설정/처음으로.</summary>
public class MolakMenu : MonoBehaviour
{
    static MolakMenu instance;
    float confirmT;
    public static bool IsOpen { get { return instance != null && instance.open; } }
    static Texture2D px;
    Font gowun;
    GUIStyle sBtn, sBtnOn, sHead, sToast, sBurger;
    float builtU = -1f;
    bool open;
    bool ledger;
    bool codex;
    static MolakMenu inst2;

    public static void OpenCodex ()
    {
        if (inst2 == null) inst2 = UnityEngine.Object.FindFirstObjectByType<MolakMenu>();
        if (inst2 != null) { inst2.open = true; inst2.codex = true; }
    }
    float toastT = -1f; string toastMsg = "";

    void Awake ()
    {
        foreach (var o in FindObjectsByType<MolakMenu>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (o != this) Destroy(o.gameObject);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap ()
    {
        if (instance != null) return;
        var go = new GameObject("MolakMenu");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<MolakMenu>();
    }

    static Texture2D Px { get { if (px == null) { px = new Texture2D(1,1); px.SetPixel(0,0,Color.white); px.Apply(); } return px; } }
    void Box (Rect r, Color c) { var g = GUI.color; GUI.color = c; GUI.DrawTexture(r, Px); GUI.color = g; }
    float U { get { return MolakTheme.U; } }

    void EnsureStyles (float u)
    {
        if (sBtn != null && Mathf.Approximately(builtU, u)) return;
        builtU = u;
        if (gowun == null) gowun = Resources.Load<Font>("GowunBatang");
        System.Func<int, Color, TextAnchor, GUIStyle> mk = (size, col, an) =>
        {
            var s = new GUIStyle(GUI.skin.label){fontSize=Mathf.RoundToInt(size*u),alignment=an,richText=true};
            if (gowun != null) s.font = gowun;
            s.normal.textColor = col; return s;
        };
        sBtn = mk(34, MolakTheme.Ink, TextAnchor.MiddleCenter);
        sBtnOn = mk(34, MolakTheme.Gold, TextAnchor.MiddleCenter);
        sHead = mk(28, MolakTheme.Ink3, TextAnchor.MiddleCenter);
        sToast = mk(28, MolakTheme.Ink, TextAnchor.MiddleCenter);
        sBurger = mk(58, new Color(0.28f,0.23f,0.18f,1f), TextAnchor.MiddleCenter);
    }

    void SetProcessInput (bool v)
    {
        try
        {
            var im = Engine.GetService<IInputManager>();
            if (im == null) return;
            var p = im.GetType().GetProperty("ProcessInput");
            if (p != null && p.CanWrite) p.SetValue(im, v);
        }
        catch {}
    }

    void Toast (string msg) { toastMsg = msg; toastT = 0f; }

    bool Btn (Rect r, string label, bool highlight, float u)
    {
        bool over = r.Contains(Event.current.mousePosition);
        Box(r, over ? new Color(0.19f,0.15f,0.12f,1f) : MolakTheme.Raise);
        Box(new Rect(r.x,r.y,r.width,Mathf.Max(1f,1*u)), MolakTheme.Line);
        Box(new Rect(r.x,r.yMax-Mathf.Max(1f,1*u),r.width,Mathf.Max(1f,1*u)), MolakTheme.Line);
        GUI.Label(r, label, highlight ? sBtnOn : sBtn);
        bool hit = GUI.Button(r, GUIContent.none, GUIStyle.none);
        if (hit) MolakSfx.Confirm();
        return hit;
    }

    void OnGUI ()
    {
        if (!Engine.Initialized) return;
        float u = U; EnsureStyles(u);
        float w = Screen.width, h = Screen.height;
        float day = 0f;
        try { day = MolakStats.Get("day"); } catch {}

        // 토스트
        if (toastT >= 0f)
        {
            toastT += Time.deltaTime;
            if (toastT > 1.6f) toastT = -1f;
            else
            {
                float a = toastT > 1.2f ? (1.6f-toastT)/0.4f : 1f;
                var g = GUI.color; GUI.color = new Color(1,1,1,a);
                Box(new Rect(w/2f-220*u, h*0.78f, 440*u, 70*u), new Color(0.05f,0.04f,0.035f,0.92f));
                GUI.Label(new Rect(w/2f-220*u, h*0.78f, 440*u, 70*u), toastMsg, sToast);
                GUI.color = g;
            }
        }

        if (day < 1 && !open && !codex) return; // 서장 전·타이틀에선 숨김 (말로록 열람은 허용)

        // ☰ 버튼 (우상단)
        float Wc = Mathf.Min(w, 1080*u), X0c = (w-Wc)/2f;
        var burgerR = new Rect(X0c+Wc-184*u, 84*u, 104*u, 74*u);
        MolakInputGuard.Guard(new Rect(burgerR.x-20*u, burgerR.y-20*u, burgerR.width+40*u, burgerR.height+40*u));
        if (!open)
        {
            GUI.Label(burgerR, "☰", sBurger);
            if (GUI.Button(burgerR, GUIContent.none, GUIStyle.none)) { open = true; SetProcessInput(false); }
            return;
        }

        // ===== 메뉴 오버레이 =====
        MolakInputGuard.Guard(new Rect(0,0,w,h));
        Box(new Rect(0,0,w,h), new Color(0f,0f,0f,0.72f));
        if (ledger) { DrawLedger(w, h, u); return; }
        if (codex) { DrawCodex(w, h, u); return; }
        var sp = Engine.GetService<IScriptPlayer>();
        var state = Engine.GetService<IStateManager>();
        var ui = Engine.GetService<IUIManager>();

        float bw = Mathf.Min(w*0.84f, 860*u), bh = 104*u, gap = 18*u;
        float bx = (w-bw)/2f;
        string[] labels = { "지난 기록", "원수 장부", "말로록", "빠른 저장", "저장 / 불러오기", "환경 설정" };
        float totalH = 100*u + 3*(bh+gap) + labels.Length*(bh+gap) + 40*u + 84*u;
        float by = Mathf.Max(60*u, (h-totalH)/2f);
        GUI.Label(new Rect(bx, by, bw, 60*u), "— 운락객잔 —", sHead);
        // ✕ 닫기
        if (GUI.Button(new Rect(bx+bw-70*u, by-6*u, 70*u, 70*u), "✕", sHead)) { open = false; SetProcessInput(true); return; }
        by += 100*u;
        // 토글 두 개: 자동 진행 · 빨리 넘기기
        float hw = (bw-gap)/2f;
        bool auto = sp != null && sp.AutoPlaying, skip = sp != null && sp.Skipping;
        if (Btn(new Rect(bx, by, hw, bh), "자동 진행 " + (auto ? "●" : "○"), auto, u) && sp != null) sp.SetAutoPlay(!auto);
        if (Btn(new Rect(bx+hw+gap, by, hw, bh), "빨리 넘기기 " + (skip ? "●" : "○"), skip, u) && sp != null) sp.SetSkip(!skip);
        by += bh + gap;
        // 표시 설정: 먹색 화면 · 효과음
        if (Btn(new Rect(bx, by, hw, bh), "먹색 " + (MolakTheme.Night ? "●" : "○"), MolakTheme.Night, u)) { MolakTheme.Night = !MolakTheme.Night; }
        if (Btn(new Rect(bx+hw+gap, by, hw, bh), "효과음 " + (MolakSfx.Muted ? "○" : "●"), !MolakSfx.Muted, u)) { MolakSfx.Muted = !MolakSfx.Muted; }
        by += bh + gap;
        // 글자 크기: [ − ]  글자 크기 100%  [ + ]  (80%~140%)
        {
            float sw2 = 150*u;
            int fs = MolakTheme.FontStep;
            if (Btn(new Rect(bx, by, sw2, bh), "－", false, u) && fs > MolakTheme.FontMin) MolakTheme.FontStep = fs - 1;
            Box(new Rect(bx+sw2+gap, by, bw-2*(sw2+gap), bh), MolakTheme.Raise);
            var mid = new GUIStyle(sBtn){ alignment = TextAnchor.MiddleCenter, richText = true };
            GUI.Label(new Rect(bx+sw2+gap, by, bw-2*(sw2+gap), bh), "글자 크기  <b>" + Mathf.RoundToInt(MolakTheme.FontScale*100f) + "%</b>", mid);
            if (Btn(new Rect(bx+bw-sw2, by, sw2, bh), "＋", false, u) && fs < MolakTheme.FontMax) MolakTheme.FontStep = fs + 1;
        }
        by += bh + gap;
        for (int i = 0; i < labels.Length; i++)
        {
            var r = new Rect(bx, by + i*(bh+gap), bw, bh);
            if (!Btn(r, labels[i], false, u)) continue;
            switch (i)
            {
                case 0:
                {
                    open = false; SetProcessInput(true);
                    var blog = ui != null ? ui.GetUI<Naninovel.UI.IBacklogUI>() as Naninovel.UI.IManagedUI : null;
                    if (blog != null) blog.ChangeVisibility(true, 0.15f);
                    break;
                }
                case 1: ledger = true; break;
                case 2: codex = true; break;
                case 3: if (state != null) { state.QuickSave(); Toast("빠르게 저장했다."); } break;
                case 4:
                {
                    open = false; SetProcessInput(true);
                    var sl = ui != null ? ui.GetUI<Naninovel.UI.ISaveLoadUI>() as Naninovel.UI.IManagedUI : null;
                    if (sl != null) sl.ChangeVisibility(true, 0.15f);
                    break;
                }
                case 5:
                {
                    open = false; SetProcessInput(true);
                    var st = ui != null ? ui.GetUI<Naninovel.UI.ISettingsUI>() as Naninovel.UI.IManagedUI : null;
                    if (st != null) st.ChangeVisibility(true, 0.15f);
                    break;
                }
            }
        }
        // 처음 화면으로: 아래쪽, 두 번 탭 확인
        float ry = by + labels.Length*(bh+gap) + 40*u;
        if (confirmT > 0f) confirmT -= Time.deltaTime;
        string rl = confirmT > 0f ? "정말 처음으로? — 다시 탭" : "처음 화면으로";
        if (Btn(new Rect(bx+bw*0.15f, ry, bw*0.7f, 84*u), rl, confirmT > 0f, u))
        {
            if (confirmT > 0f) { confirmT = 0f; open = false; SetProcessInput(true); if (state != null) state.ResetState(); }
            else confirmT = 3f;
        }
        // 바깥 탭 → 닫기
        if (Event.current.type == EventType.MouseDown) { open = false; SetProcessInput(true); }
    }

    void DrawLedger (float w, float h, float u)
    {
        float g0;
        System.Func<string,float> gv = n => { try { return MolakStats.Get(n); } catch { return 0f; } };
        float bw = Mathf.Min(w*0.84f, 860*u), rh = 124*u, gap = 16*u;
        float bx = (w-bw)/2f, by = Mathf.Max(60*u, h*0.14f);
        GUI.Label(new Rect(bx, by, bw, 60*u), "— 원수 장부 · 청운의 빚 —", sHead);
        by += 90*u;
        string[] names = {
            "독아단 아장 호치",
            "독아단주 견아",
            "혈쇄문주",
            "혈랑대주 ???",
            "혈마교주 혈천마"
        };
        string[] states = new string[5];
        states[0] = gv("f_hmsaved") > 0 ? "<color=#C03434>베었다</color>" : "추적 중";
        states[1] = gv("b_ochi") < 1 ? "― 아직 모른다 ―" : (gv("f_kyonadown") > 0 ? "<color=#C03434>베었다</color>" : "북쪽 골짜기");
        if (gv("b_ochi") < 1) names[1] = "독아단주 ???";
        states[2] = gv("f_wolfdown") > 0 ? "<color=#C03434>베었다</color>" : "서열 77위";
        if (gv("f_jinbaek") > 0) names[3] = "혈랑대주 진백 — 대사형";
        states[3] = gv("b_r1x") < 1 ? "― 아직 모른다 ―" : (gv("f_jinbaekdead") > 0 ? "<color=#C03434>베었다</color>" : (gv("f_jinbaektruth") > 0 ? "<color=#C9A227>무릎 꿇렸다</color>" : (gv("f_jinbaek") > 0 ? "성 밖 사당" : "산문을 연 자")));
        states[4] = "하늘의 끝, 서열 1위";
        for (int i = 0; i < 5; i++)
        {
            var r = new Rect(bx, by + i*(rh+gap), bw, rh);
            Box(r, MolakTheme.Raise);
            Box(new Rect(r.x,r.y,r.width,Mathf.Max(1f,1*u)), MolakTheme.Line);
            Box(new Rect(r.x,r.yMax-Mathf.Max(1f,1*u),r.width,Mathf.Max(1f,1*u)), MolakTheme.Line);
            var nameStyle = new GUIStyle(sBtn){alignment=TextAnchor.MiddleLeft, fontSize=Mathf.RoundToInt(36*u)};
            var stStyle = new GUIStyle(sBtn){alignment=TextAnchor.MiddleRight, fontSize=Mathf.RoundToInt(30*u)};
            stStyle.normal.textColor = MolakTheme.Ink3;
            GUI.Label(new Rect(r.x+28*u, r.y, r.width*0.62f, r.height), names[i], nameStyle);
            GUI.Label(new Rect(r.x+r.width*0.5f, r.y, r.width*0.5f-28*u, r.height), states[i], stStyle);
        }
        var back = new Rect(bx, by + 5*(rh+gap) + 30*u, bw, 104*u);
        if (Btn(back, "돌아간다", false, u)) ledger = false;
        if (Event.current.type == EventType.MouseDown) ledger = false;
        g0 = 0f;
    }

    UnityEngine.Vector2 codexScroll;
    void DrawCodex (float w, float h, float u)
    {
        System.Func<string,float> gv = n => { try { return MolakStats.Get(n); } catch { return 0f; } };
        float bw = Mathf.Min(w*0.88f, 900*u);
        float bx = (w-bw)/2f, by = Mathf.Max(50*u, h*0.08f);
        int got = 0;
        foreach (var e in MolakEnd.All) if (gv("g_end_" + e.Id) > 0) got++;
        GUI.Label(new Rect(bx, by, bw, 56*u), "— 말로록 · " + got + " / " + MolakEnd.All.Length + " —", sHead);
        by += 76*u;
        float rh = 124*u, gap = 14*u;
        float listH = h - by - 150*u;
        var view = new Rect(bx, by, bw, listH);
        var content = new Rect(0, 0, bw-24*u, MolakEnd.All.Length*(rh+gap));
        codexScroll = GUI.BeginScrollView(view, codexScroll, content);
        var hj = new GUIStyle(sBtn){alignment=TextAnchor.MiddleCenter, fontSize=Mathf.RoundToInt(46*u)};
        hj.normal.textColor = MolakTheme.Blood;
        var nameS = new GUIStyle(sBtn){alignment=TextAnchor.MiddleLeft, fontSize=Mathf.RoundToInt(34*u)};
        var hintS = new GUIStyle(sBtn){alignment=TextAnchor.MiddleLeft, fontSize=Mathf.RoundToInt(26*u)};
        hintS.normal.textColor = MolakTheme.Ink3;
        for (int i = 0; i < MolakEnd.All.Length; i++)
        {
            var e = MolakEnd.All[i];
            bool has = gv("g_end_" + e.Id) > 0;
            var r = new Rect(0, i*(rh+gap), bw-24*u, rh);
            Box(r, MolakTheme.Raise);
            Box(new Rect(r.x,r.y,r.width,Mathf.Max(1f,1*u)), MolakTheme.Line);
            Box(new Rect(r.x,r.yMax-Mathf.Max(1f,1*u),r.width,Mathf.Max(1f,1*u)), MolakTheme.Line);
            if (has)
            {
                GUI.Label(new Rect(r.x+16*u, r.y, 130*u, r.height), e.Kor, hj);
                GUI.Label(new Rect(r.x+160*u, r.y+8*u, r.width-180*u, r.height*0.55f), e.Title, nameS);
                GUI.Label(new Rect(r.x+160*u, r.y+r.height*0.52f, r.width-180*u, r.height*0.42f), e.Hint, hintS);
            }
            else
            {
                // 실루엣: 한자 인장을 먹 그림자처럼, 제목은 글자 수만 남긴 빈칸
                var dim = new GUIStyle(hj); dim.normal.textColor = new Color(0.30f,0.26f,0.23f,1f); dim.fontSize = Mathf.RoundToInt(52*u);
                GUI.Label(new Rect(r.x+16*u, r.y, 130*u, r.height), e.Hanja, dim);
                var sb = new System.Text.StringBuilder();
                foreach (var ch in e.Title) sb.Append(ch == ' ' ? ' ' : '□');
                var ghost = new GUIStyle(nameS); ghost.normal.textColor = new Color(0.42f,0.37f,0.32f,1f);
                GUI.Label(new Rect(r.x+160*u, r.y+8*u, r.width-180*u, r.height*0.55f), sb.ToString() + "   <size=" + Mathf.RoundToInt(20*u) + ">아직 보지 못한 말로</size>", ghost);
                GUI.Label(new Rect(r.x+160*u, r.y+r.height*0.52f, r.width-180*u, r.height*0.42f), e.Hint, hintS);
            }
        }
        GUI.EndScrollView();
        var back = new Rect(bx, by + listH + 16*u, bw, 100*u);
        if (Btn(back, "돌아간다", false, u))
        {
            codex = false;
            float d2 = 0f; try { d2 = MolakStats.Get("day"); } catch {}
            if (d2 < 1f) { open = false; SetProcessInput(true); }
        }
    }
}
