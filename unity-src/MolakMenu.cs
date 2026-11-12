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
    float legendT = -1f;
    /// <summary>처음 색 인장이 뜬 순간 한 번: 인장 색 범례를 화면 위쪽에 잠깐 보여 준다.</summary>
    public static void ShowLegend ()
    {
        if (inst2 == null) inst2 = UnityEngine.Object.FindFirstObjectByType<MolakMenu>();
        if (inst2 != null) inst2.legendT = 0f;
    }

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
        sHead = mk(30, MolakTheme.Ink3, TextAnchor.MiddleCenter);
        sHead.font = MolakTypography.Heading(gowun);   // 묶음 머리·제목은 송명
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

    /// <summary>메뉴 묶음 머리: 왼쪽에 작은 제목, 그 아래 가는 금빛 선.</summary>
    void MenuSection (float x, ref float y, float wd, float hgt, string title)
    {
        var st = new GUIStyle(sHead) { alignment = TextAnchor.LowerLeft, fontSize = Mathf.RoundToInt(28 * builtU) };
        st.normal.textColor = MolakTheme.Gold;
        GUI.Label(new Rect(x + 6 * builtU, y, wd, hgt - 10 * builtU), title, st);
        Box(new Rect(x, y + hgt - 6 * builtU, wd, Mathf.Max(1f, 1 * builtU)), MolakTheme.Gold * new Color(1, 1, 1, 0.45f));
        y += hgt;
    }

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

        // 인장 범례 (한 번)
        if (legendT >= 0f)
        {
            legendT += Time.deltaTime;
            if (legendT > 7f) legendT = -1f;
            else
            {
                float a = legendT < 0.4f ? legendT / 0.4f : legendT > 6.2f ? (7f - legendT) / 0.8f : 1f;
                var g = GUI.color; GUI.color = new Color(1, 1, 1, a);
                float lw = Mathf.Min(w * 0.92f, 980*u), lx = (w - lw) / 2f, ly = h * 0.10f, lh = 190*u;
                Box(new Rect(lx, ly, lw, lh), new Color(0.05f,0.04f,0.035f,0.94f));
                Box(new Rect(lx, ly, lw, Mathf.Max(1f, 2*u)), MolakTheme.Line);
                var ls = new GUIStyle(sToast){ alignment = TextAnchor.UpperLeft, wordWrap = true, richText = true, fontSize = Mathf.RoundToInt(27*u) };
                GUI.Label(new Rect(lx + 28*u, ly + 18*u, lw - 56*u, lh - 30*u),
                    "선택지 앞 인장의 색은 그 길이 열린 까닭이다.\n" +
                    "<color=#8E62B8>■</color> 지난 생   <color=#4A7398>■</color> 물건·인연   <color=#5A524C>■</color> 능력치   <color=#B08A30>■</color> 은자   <color=#7A2A68>■</color> 사술   <color=#B03A2A>■</color> 그 밖\n" +
                    "<size=85%>회색 작은 글씨는 그 선택지를 연 조건이다.</size>", ls);
                GUI.color = g;
            }
        }
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
            { // 세 줄 아이콘 (고운바탕에 ☰ 글리프가 없어 기기에서 □로 나오던 것)
                var bc = MolakTheme.Night ? new Color(0.88f,0.84f,0.78f,1f) : new Color(0.20f,0.16f,0.13f,1f);
                float bw0 = 44*u, bt = 5*u, gap0 = 12*u;
                float bx0 = burgerR.x + (burgerR.width - bw0)/2f, by0 = burgerR.y + (burgerR.height - (3*bt + 2*gap0))/2f;
                for (int bi = 0; bi < 3; bi++) Box(new Rect(bx0, by0 + bi*(bt+gap0), bw0, bt), bc);
            }
            if (GUI.Button(burgerR, GUIContent.none, GUIStyle.none)) { open = true; SetProcessInput(false); }
            return;
        }

        // ===== 메뉴 오버레이 =====
        MolakInputGuard.Guard(new Rect(0,0,w,h));
        Box(new Rect(0,0,w,h), new Color(0.055f,0.045f,0.038f,0.95f));   // 뒤의 본문이 비치지 않게 거의 덮는다
        if (ledger) { DrawLedger(w, h, u); return; }
        if (codex) { DrawCodex(w, h, u); return; }
        if (knowTab) { DrawKnow(w, h, u); return; }
        if (facTab) { DrawFaction(w, h, u); return; }
        var sp = Engine.GetService<IScriptPlayer>();
        var state = Engine.GetService<IStateManager>();
        var ui = Engine.GetService<IUIManager>();

        // 0.4.12: 메뉴를 네 묶음으로 정리한다 — 진행 · 기록 · 저장 · (환경 설정 / 처음으로).
        // 글자 크기·배경음·먹색·효과음은 '환경 설정' 한 곳에만 둔다(예전엔 여기와 환경 설정 두 곳에 있어 메뉴가 열다섯 줄이었다).
        float bw = Mathf.Min(w*0.84f, 860*u), gap = 16*u;
        float bx = (w-bw)/2f;
        float rowH = 96*u, tileH = 128*u, secH = 50*u;
        float totalH = 96*u + (secH + rowH + gap) + (secH + 3*(tileH+gap)) + (secH + rowH + gap) + (rowH + 24*u);
        float by = Mathf.Max(48*u, (h-totalH)/2f);
        GUI.Label(new Rect(bx, by, bw, 60*u), "— 운락객잔 —", sHead);
        if (GUI.Button(new Rect(bx+bw-70*u, by-6*u, 70*u, 70*u), "×", sHead)) { open = false; SetProcessInput(true); return; }
        by += 96*u;

        // ── 진행
        MenuSection(bx, ref by, bw, secH, "진행");
        {
            float tw = (bw - 2*gap) / 3f;
            bool auto = sp != null && sp.AutoPlaying, skip = sp != null && sp.Skipping;
            if (Btn(new Rect(bx, by, tw, rowH), "자동 " + (auto ? "●" : "○"), auto, u) && sp != null) sp.SetAutoPlay(!auto);
            if (Btn(new Rect(bx+tw+gap, by, tw, rowH), "빨리 " + (skip ? "●" : "○"), skip, u) && sp != null) sp.SetSkip(!skip);
            if (Btn(new Rect(bx+2*(tw+gap), by, tw, rowH), "읽은 글 ▶▶", false, u))
            {   // 이미 본 글은 빠르게 — 처음 보는 글과 선택지에서 멈춘다(나니노벨 읽은 글 넘기기)
                open = false; SetProcessInput(true);
                if (sp != null) { sp.SkipMode = PlayerSkipMode.ReadOnly; sp.SetSkip(true); Toast("읽은 글을 넘긴다 — 처음 보는 글에서 멈춘다."); }
            }
            by += rowH + gap;
        }

        // ── 기록 (두 줄 카드)
        MenuSection(bx, ref by, bw, secH, "기록");
        {
            int ends = 0; foreach (var e in MolakEnd.All) if (MolakStats.Get("g_end_" + e.Id) > 0) ends++;
            string[] t = { "지난 기록", "원수 장부", "말로록", "남은 기억", "강호록" };
            string[] sub = { "지나온 글을 다시 읽는다", "벤 자, 살린 자, 나 때문에 죽은 자", "본 말로 " + ends + " / " + MolakEnd.All.Length, "되감기지 않는 것", "문파와 사람들" };
            float tw = (bw - gap) / 2f;
            for (int i = 0; i < t.Length; i++)
            {
                var r = new Rect(bx + (i % 2) * (tw + gap), by + (i / 2) * (tileH + gap), i == 4 ? bw : tw, tileH);
                if (!Btn(r, t[i] + "\n<size=" + Mathf.RoundToInt(22*u) + "><color=#8C8378>" + sub[i] + "</color></size>", false, u)) continue;
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
                    case 3: knowTab = true; break;
                    case 4: facTab = true; break;
                }
            }
            by += 3 * (tileH + gap);
        }

        // ── 저장
        MenuSection(bx, ref by, bw, secH, "저장");
        {
            float tw = (bw - gap) / 2f;
            if (Btn(new Rect(bx, by, tw, rowH), "빠른 저장", false, u) && state != null) { state.QuickSave(); MolakAutoSave.MarkQuick(); Toast("빠르게 저장했다."); }
            if (Btn(new Rect(bx+tw+gap, by, tw, rowH), "저장 · 불러오기", false, u))
            {
                open = false; SetProcessInput(true);
                var sl = ui != null ? ui.GetUI<Naninovel.UI.ISaveLoadUI>() as Naninovel.UI.IManagedUI : null;
                if (sl != null) sl.ChangeVisibility(true, 0.15f);
            }
            by += rowH + gap;
        }

        // ── 아래 줄: 환경 설정 · 처음 화면으로(두 번 탭 확인)
        {
            by += 8*u;
            float tw = (bw - gap) / 2f;
            if (Btn(new Rect(bx, by, tw, rowH), "환경 설정", false, u)) { open = false; MolakSettings.Open(); }
            if (confirmT > 0f) confirmT -= Time.deltaTime;
            string rl = confirmT > 0f ? "다시 탭하면 처음으로" : "처음 화면으로";
            if (Btn(new Rect(bx+tw+gap, by, tw, rowH), rl, confirmT > 0f, u))
            {
                if (confirmT > 0f) { confirmT = 0f; open = false; SetProcessInput(true); if (state != null) state.ResetState(); }
                else confirmT = 3f;
            }
        }
        // 바깥 탭 → 닫기
        if (Event.current.type == EventType.MouseDown) { open = false; SetProcessInput(true); }
    }

    UnityEngine.Vector2 ledgerScroll;
    void DrawLedger (float w, float h, float u)
    {
        System.Func<string,float> gv = n => { try { return MolakStats.Get(n); } catch { return 0f; } };
        float bw = Mathf.Min(w*0.84f, 860*u), rh = 96*u, gap = 12*u;
        float bx = (w-bw)/2f, by = Mathf.Max(50*u, h*0.07f);
        GUI.Label(new Rect(bx, by, bw, 60*u), "— 원수 장부 · 청운의 빚 —", sHead);
        by += 76*u;
        {   // 0.4.9: 이번 생의 뜻
            int vw = Mathf.RoundToInt(gv("vow"));
            if (vw > 0)
            {
                string vt = vw == 1 ? "복수 — 서열록 천 위 안" : vw == 2 ? "인연 — 등을 맡길 사람" : "생존 — 그물이 조여 올 때까지";
                var vs = new GUIStyle(sBtn){alignment=TextAnchor.MiddleCenter, fontSize=Mathf.RoundToInt(28*u), richText=true};
                vs.normal.textColor = MolakTheme.Ink3;
                GUI.Label(new Rect(bx, by - 14*u, bw, 44*u), "이번 생의 뜻 · " + vt, vs);
                by += 40*u;
            }
        }
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
            var nameStyle = new GUIStyle(sBtn){alignment=TextAnchor.MiddleLeft, fontSize=Mathf.RoundToInt(32*u)};
            var stStyle = new GUIStyle(sBtn){alignment=TextAnchor.MiddleRight, fontSize=Mathf.RoundToInt(27*u), richText=true};
            stStyle.normal.textColor = MolakTheme.Ink3;
            GUI.Label(new Rect(r.x+28*u, r.y, r.width*0.62f, r.height), names[i], nameStyle);
            GUI.Label(new Rect(r.x+r.width*0.5f, r.y, r.width*0.5f-28*u, r.height), states[i], stStyle);
        }
        // ---- 생을 넘어: 벤 자 · 나 때문에 죽은 자 · 살려 보낸 자 ----
        float y2 = by + 5*(rh+gap) + 10*u;
        GUI.Label(new Rect(bx, y2, bw, 50*u), "— 생을 넘어 쌓인 장부 —", sHead);
        y2 += 62*u;
        var rows = new System.Collections.Generic.List<string[]>();
        foreach (var e in MolakLedger.All) { var ln = MolakLedger.Line(e.Id); if (ln != null) rows.Add(new[]{ e.Name, ln }); }
        float nk = gv("g_lk_nameless"), ns = gv("g_ls_nameless");
        if (nk > 0 || ns > 0)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (nk > 0) parts.Add("<color=#C03434>벤 목 " + nk.ToString("0") + "</color>");
            if (ns > 0) parts.Add("<color=#3E6E8E>살려 보낸 목 " + ns.ToString("0") + "</color>");
            rows.Add(new[]{ "비무판의 이름 없는 자들", string.Join(" · ", parts.ToArray()) });
        }
        float lh = 78*u, listH = h - y2 - 140*u;
        var nmS = new GUIStyle(sBtn){alignment=TextAnchor.MiddleLeft, fontSize=Mathf.RoundToInt(27*u)};
        var cS = new GUIStyle(sBtn){alignment=TextAnchor.MiddleRight, fontSize=Mathf.RoundToInt(24*u), richText=true};
        if (rows.Count == 0)
        {
            var em = new GUIStyle(sBtn){alignment=TextAnchor.UpperCenter, fontSize=Mathf.RoundToInt(25*u), wordWrap=true};
            em.normal.textColor = MolakTheme.Ink3;
            GUI.Label(new Rect(bx, y2, bw, 90*u), "아직 아무도 적히지 않았다. 생이 끝날 때마다, 벤 자와 살린 자와 나 때문에 죽은 자가 여기 남는다.", em);
        }
        else
        {
            ledgerScroll = TouchScroll(1, new Rect(bx, y2, bw, listH), ledgerScroll, rows.Count*lh);
            ledgerScroll = GUI.BeginScrollView(new Rect(bx, y2, bw, listH), ledgerScroll, new Rect(0, 0, bw-24*u, rows.Count*lh));
            for (int i = 0; i < rows.Count; i++)
            {
                var r = new Rect(0, i*lh, bw-24*u, lh-8*u);
                Box(r, MolakTheme.Raise);
                GUI.Label(new Rect(r.x+22*u, r.y, r.width*0.55f, r.height), rows[i][0], nmS);
                GUI.Label(new Rect(r.x+r.width*0.4f, r.y, r.width*0.6f-22*u, r.height), rows[i][1], cS);
            }
            GUI.EndScrollView();
        }
        var back = new Rect(bx, h - 124*u, bw, 100*u);
        if (Btn(back, "돌아간다", false, u)) ledger = false;
    }


    // ===== 손가락 스크롤: IMGUI 스크롤뷰는 터치 드래그를 받지 않으므로 직접 끈다(관성 포함) =====
    bool tsDrag; float tsStartY, tsStartScroll, tsLastY, tsVel, tsLastT; int tsId = -1;
    Vector2 TouchScroll (int id, Rect view, Vector2 scroll, float contentH)
    {
        float maxY = Mathf.Max(0f, contentH - view.height);
        var e = Event.current;
        if (e.type == EventType.MouseDown && view.Contains(e.mousePosition))
        {
            tsDrag = true; tsId = id; tsStartY = e.mousePosition.y; tsStartScroll = scroll.y; tsLastY = tsStartY; tsVel = 0f; tsLastT = Time.unscaledTime;
        }
        else if (e.type == EventType.MouseDrag && tsDrag && tsId == id)
        {
            float y = e.mousePosition.y, now = Time.unscaledTime;
            scroll.y = Mathf.Clamp(tsStartScroll - (y - tsStartY), 0f, maxY);
            float dt = Mathf.Max(0.001f, now - tsLastT);
            tsVel = Mathf.Lerp(tsVel, -(y - tsLastY) / dt, 0.5f);
            tsLastY = y; tsLastT = now;
            e.Use();
        }
        else if (e.type == EventType.MouseUp && tsId == id) { tsDrag = false; }
        else if (e.type == EventType.Repaint && !tsDrag && tsId == id && Mathf.Abs(tsVel) > 5f)
        {
            scroll.y = Mathf.Clamp(scroll.y + tsVel * Time.unscaledDeltaTime, 0f, maxY);
            tsVel *= Mathf.Pow(0.06f, Time.unscaledDeltaTime);   // 1초에 6%로 줄어드는 관성
        }
        return scroll;
    }

    bool facTab;
    UnityEngine.Vector2 facScroll;
    void DrawFaction (float w, float h, float u)
    {
        float bw = Mathf.Min(w*0.88f, 900*u);
        float bx = (w-bw)/2f, by = Mathf.Max(50*u, h*0.08f);
        GUI.Label(new Rect(bx, by, bw, 56*u), "— 강호록 · " + MolakFaction.Count() + " / " + MolakFaction.All.Length + " —", sHead);
        by += 76*u;
        float gap = 14*u, lineH = 74*u;
        float listH = h - by - 150*u;
        float cw = bw-24*u;
        var heights = new float[MolakFaction.All.Length];
        float total = 0;
        for (int i = 0; i < heights.Length; i++)
        {
            var e = MolakFaction.All[i];
            float hh = 112*u;
            if (MolakFaction.Met(e))
            {
                hh = 150*u;
                foreach (var l in e.Lines) if (MolakFaction.LineOn(l)) hh += lineH;
                if (MolakFaction.RepText(e) != null) hh += 44*u;
            }
            heights[i] = hh; total += hh + gap;
        }
        var view = new Rect(bx, by, bw, listH);
        facScroll = TouchScroll(2, view, facScroll, total);
        facScroll = GUI.BeginScrollView(view, facScroll, new Rect(0, 0, cw, total));
        var nameS = new GUIStyle(sBtn){alignment=TextAnchor.UpperLeft, fontSize=Mathf.RoundToInt(34*u), wordWrap=false, richText=true};
        var mottoS = new GUIStyle(sBtn){alignment=TextAnchor.UpperLeft, fontSize=Mathf.RoundToInt(26*u), wordWrap=true, fontStyle=FontStyle.Italic};
        mottoS.normal.textColor = MolakTheme.Ink3;
        var lineS = new GUIStyle(sBtn){alignment=TextAnchor.UpperLeft, fontSize=Mathf.RoundToInt(25*u), wordWrap=true, richText=true};
        var whoS = new GUIStyle(sBtn){alignment=TextAnchor.UpperRight, fontSize=Mathf.RoundToInt(21*u)};
        whoS.normal.textColor = MolakTheme.Ink3;
        var repS = new GUIStyle(sBtn){alignment=TextAnchor.UpperLeft, fontSize=Mathf.RoundToInt(24*u)};
        repS.normal.textColor = MolakTheme.Night ? new Color(0.28f,0.44f,0.60f,1f) : new Color(0.20f,0.33f,0.47f,1f);
        float y = 0;
        for (int i = 0; i < MolakFaction.All.Length; i++)
        {
            var e = MolakFaction.All[i];
            bool met = MolakFaction.Met(e);
            var r = new Rect(0, y, cw, heights[i]);
            Box(r, MolakTheme.Raise);
            Box(new Rect(r.x, r.y, Mathf.Max(2f,5*u), r.height), met ? MolakChoiceMeta.SealColor(MolakChoiceMeta.Item, MolakTheme.Night) : MolakTheme.Line);
            float x0 = r.x + 28*u, iw = r.width - 56*u;
            if (!met)
            {
                var ghost = new GUIStyle(nameS); ghost.normal.textColor = new Color(0.42f,0.37f,0.32f,1f);
                var sb = new System.Text.StringBuilder(); foreach (var ch in e.Name) sb.Append(ch == ' ' ? ' ' : '□');
                GUI.Label(new Rect(x0, r.y+14*u, iw, 48*u), sb.ToString(), ghost);
                GUI.Label(new Rect(x0, r.y+62*u, iw, 40*u), "아직 만나지 못했다 · " + e.Where, mottoS);
            }
            else
            {
                GUI.Label(new Rect(x0, r.y+14*u, iw, 48*u), e.Name + "  <size=70%>" + e.Hanja + "</size>", nameS);
                GUI.Label(new Rect(x0, r.y+66*u, iw, 70*u), "“" + e.Motto + "”", mottoS);
                float ly = r.y + 150*u;
                var rep = MolakFaction.RepText(e);
                if (rep != null) { GUI.Label(new Rect(x0, ly - 12*u, iw, 40*u), rep, repS); ly += 44*u; }
                foreach (var l in e.Lines)
                {
                    if (!MolakFaction.LineOn(l)) continue;
                    GUI.Label(new Rect(x0, ly, iw, lineH-24*u), "\"" + l.Text + "\"", lineS);
                    GUI.Label(new Rect(x0, ly + lineH - 30*u, iw, 28*u), "— " + l.Who, whoS);
                    ly += lineH;
                }
            }
            y += heights[i] + gap;
        }
        GUI.EndScrollView();
        if (Btn(new Rect(bx, by + listH + 16*u, bw, 100*u), "돌아간다", false, u)) facTab = false;
    }

    bool knowTab;
    UnityEngine.Vector2 knowScroll;
    void DrawKnow (float w, float h, float u)
    {
        float bw = Mathf.Min(w*0.88f, 900*u);
        float bx = (w-bw)/2f, by = Mathf.Max(50*u, h*0.08f);
        int got = MolakKnow.Count();
        GUI.Label(new Rect(bx, by, bw, 56*u), "— 남은 기억 · " + got + " / " + MolakKnow.All.Length + " —", sHead);
        by += 76*u;
        float rh = 150*u, gap = 14*u;
        float listH = h - by - 150*u;
        var view = new Rect(bx, by, bw, listH);
        var content = new Rect(0, 0, bw-24*u, MolakKnow.All.Length*(rh+gap));
        knowScroll = TouchScroll(3, view, knowScroll, content.height);
        knowScroll = GUI.BeginScrollView(view, knowScroll, content);
        var nameS = new GUIStyle(sBtn){alignment=TextAnchor.UpperLeft, fontSize=Mathf.RoundToInt(34*u), wordWrap=true};
        var descS = new GUIStyle(sBtn){alignment=TextAnchor.UpperLeft, fontSize=Mathf.RoundToInt(25*u), wordWrap=true};
        descS.normal.textColor = MolakTheme.Ink3;
        var whereS = new GUIStyle(sBtn){alignment=TextAnchor.UpperRight, fontSize=Mathf.RoundToInt(23*u)};
        whereS.normal.textColor = MolakTheme.Night ? new Color(0.62f,0.50f,0.78f,1f) : new Color(0.42f,0.27f,0.58f,1f);
        for (int i = 0; i < MolakKnow.All.Length; i++)
        {
            var e = MolakKnow.All[i];
            bool has = MolakKnow.Known(e);
            var r = new Rect(0, i*(rh+gap), bw-24*u, rh);
            Box(r, MolakTheme.Raise);
            Box(new Rect(r.x,r.y,Mathf.Max(2f,5*u),r.height), has ? MolakChoiceMeta.SealColor(MolakChoiceMeta.Know, MolakTheme.Night) : MolakTheme.Line);
            if (has)
            {
                GUI.Label(new Rect(r.x+28*u, r.y+14*u, r.width*0.6f, 48*u), e.Name, nameS);
                GUI.Label(new Rect(r.x+r.width*0.45f, r.y+18*u, r.width*0.55f-24*u, 40*u), e.Where, whereS);
                GUI.Label(new Rect(r.x+28*u, r.y+64*u, r.width-56*u, r.height-70*u), e.Desc, descS);
            }
            else
            {
                var ghost = new GUIStyle(nameS); ghost.normal.textColor = new Color(0.42f,0.37f,0.32f,1f);
                var sb = new System.Text.StringBuilder(); foreach (var ch in e.Name) sb.Append(ch == ' ' ? ' ' : '□');
                GUI.Label(new Rect(r.x+28*u, r.y+14*u, r.width*0.6f, 48*u), sb.ToString(), ghost);
                GUI.Label(new Rect(r.x+r.width*0.45f, r.y+18*u, r.width*0.55f-24*u, 40*u), e.Where, whereS);
                GUI.Label(new Rect(r.x+28*u, r.y+64*u, r.width-56*u, r.height-70*u), "아직 모른다. " + e.Where + "에서 한 번은 겪어 봐야 안다 — 살아서든, 죽어서든.", descS);
            }
        }
        GUI.EndScrollView();
        var back = new Rect(bx, by + listH + 16*u, bw, 100*u);
        if (Btn(back, "돌아간다", false, u)) knowTab = false;
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
        codexScroll = TouchScroll(4, view, codexScroll, content.height);
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
