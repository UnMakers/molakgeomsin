using System.Collections.Generic;
using Naninovel;
using UnityEngine;

/// <summary>
/// 행낭 — 화면 맨 아래(선택지 아래 띠)에서 위로 쓸어 올리면 올라오는 서랍.
/// 플래그 변수로만 존재하던 물건·비방·인연을 획득한 것만 모아 보여 준다. 아래로 쓸거나 바깥을 탭하면 닫힘.
/// </summary>
public class MolakBag : MonoBehaviour
{
    public struct Entry { public string Flag, Name, Desc, Effect; public Entry (string f, string n, string d, string e) { Flag = f; Name = n; Desc = d; Effect = e; } }

    public static readonly Entry[] Items = {
        new Entry("f_cheonggang", "청강검(靑鋼劍)", "청운검파 장문인의 검. 손에 맞는다는 것이 문제다.", ""),
        new Entry("f_guardsword", "표국의 검", "문 닫은 표국의 총표두 염가가 맡긴 검. 뼈대가 좋다.", "받을 때 검술 +1"),
        new Entry("f_bilok", "운검비록(雲劍秘錄)", "청운의 비록. 구결이 손끝을 이끈다.", "밤 검술 수련 효율 상승"),
        new Entry("f_biloklearn", "운검비록 · 익힘", "비록의 구결을 몸에 들였다.", "밤 검술 수련 효율 상승"),
        new Entry("f_bilokhalf", "운검비록 · 반쪽", "반만 읽은 비록. 반 치가 쌓인다.", "밤 검술 수련 효율 상승"),
        new Entry("f_relicpae", "일곱째 사형의 패", "야시에서 되찾은 청운의 패. 가슴 문양 위에 품고 다닌다.", ""),
        new Entry("f_inkcord", "검은 결끈", "북쪽 짐에서 나온 좋은 검의 끈. 뽑을 때 반 호흡이 준다.", "검술 +1"),
        new Entry("f_talisman2", "진짜 부적", "부적 장수가 사과로 건넨, 제 손으로 쓴 부적.", "저주 −4"),
        new Entry("f_acupgone", "금침 한 쌈", "북으로 떠난 금침 노인의 마지막 처방.", "저주 −4, 내공 +1"),
        new Entry("f_blackpill", "흑단환(黑斷丸) · 복용", "독으로 독을 눌렀다. 그릇이 한 치 줄었다.", "저주 −15, 체력 상한 −2"),
        new Entry("f_sutrashard", "경판 조각", "불탄 산사에서 살아남은 두 줄.", ""),
        new Entry("f_newrobe", "무명 새 옷", "포목전 주인이 지어 준 옷. 소매가 검을 뽑을 때 걸리지 않는다.", "체력 +1"),
        new Entry("f_frost", "솜옷", "하진무가 말없이 두고 간 겨울옷.", ""),
        new Entry("f_letter", "맡긴 편지", "받을 사람 없는 편지. 하진무가 품에 넣었다.", ""),
        new Entry("f_janginfo", "혈천진 보법 서찰", "혈마곡 입구 진법의 틈. 왼, 왼, 오른.", "혈마교주 비무 보정"),
    };
    public static readonly Entry[] Lore = {
        new Entry("f_breath", "토탁(吐濁) 구결", "거지 중의 숨법. 들숨 여섯, 날숨 아홉.", "운기 시 저주 증가 감소"),
        new Entry("f_breath2", "토탁 · 두 번째 구결", "날숨 아홉 끝의 반 호흡.", "운기 시 저주 증가 더 감소"),
        new Entry("f_tea", "청심차 비방", "연잎 석 냥, 감초 한 냥, 자하근. 매일 새벽 한 사발.", "매일 저주 성장 −0.15"),
        new Entry("f_seedknow", "씨앗(種)의 정체", "가슴의 문양은 병이 아니라 누가 심은 글자다.", ""),
        new Entry("f_sutraknow", "경판의 두 줄", "심은 자 죽으면 씨 또한 죽는다. 씨가 다 익으면 심은 자가 거둔다.", ""),
        new Entry("f_monklore", "혈천교 토벌의 기억", "교주도 누군가의 밭이었다. 거두는 자가 되지 마라.", ""),
        new Entry("f_oldface", "노인의 얼굴", "왼눈 위에서 턱까지 내려오는 검상.", ""),
        new Entry("f_dokinfo", "독아단의 약점", "아장 호치의 버릇을 안다.", "호치 비무 보정"),
        new Entry("f_chaininfo", "혈쇄문의 사슬 길", "사슬이 도는 길을 안다.", "혈쇄문주 비무 보정"),
        new Entry("f_wolfedge", "혈쇄문주의 허점", "", "혈쇄문주 비무 보정"),
        new Entry("f_lordedge", "교주의 허점", "", "혈마교주 비무 보정"),
        new Entry("f_valleyroute", "북쪽 골짜기 샛길", "소금길—나루 윗길—폐사. 횃불이 받아 가는 길.", ""),
        new Entry("f_roadinfo", "소금길", "교가 막은 관도 대신 장사치들만 아는 길.", ""),
        new Entry("f_taxroute", "징세조의 은자 길", "보름 장날, 소금길 나루 윗길, 호위 둘.", ""),
        new Entry("f_amhwarn", "암향루의 향", "단내 나는 쪽이 독, 쓴내 나는 쪽이 약.", ""),
        new Entry("f_spyinfo", "교 안의 불만", "교주를 원치 않는 자들이 당신을 지켜본다.", ""),
        new Entry("f_relicinfo", "북쪽 짐", "보름에 한 번, 사람 것만 빼고 내려온다.", ""),
        new Entry("f_beggarnet", "거지의 눈", "담 밑을 세는 붉은 끈. 뒷문 담은 낮다.", ""),
        new Entry("f_deadrumor", "사망 도장", "수배서에 '死'. 며칠은 조용하다.", ""),
    };
    public static readonly Entry[] Bonds = {
        new Entry("f_soabond", "소아 · 약속", "같은 달 아래.", ""),
        new Entry("f_soaseal", "소아의 봉인법", "쌍검의 문파가 쓰는 봉인.", "저주 성장 −15%"),
        new Entry("f_herbbond", "백초 할멈 · 지게", "산허리까지 반나절. 청운 사람들이 내 약을 많이 사갔지.", ""),
        new Entry("f_herbfriend", "백초 할멈 · 단골", "", ""),
        new Entry("f_hajinfree", "하진무 · 빈칸 방값", "셈이 안 맞는 방은 빈칸으로 둔다.", "방값 면제"),
        new Entry("f_sparfriend", "낭인 마충", "돌아오면 백 합.", ""),
        new Entry("f_guardfriend", "표국 염가", "총표두 자리를 비워 두겠소.", ""),
        new Entry("f_rivalfriend", "곽산", "창을 쓰는 장신의 호적수.", ""),
        new Entry("f_jangfriend", "쥐눈이 장", "정보상과의 거래 관계.", ""),
        new Entry("f_ferryfree", "사공의 약속", "삿대질 값은 안 받겠소.", ""),
        new Entry("f_vigil", "마당의 배웅", "아무도 가지 말라고 하지 않았다.", ""),
    };

    static MolakBag inst;
    static Texture2D px;
    Font gowun; GUIStyle sHead, sName, sDesc, sEff, sSec, sHint; float builtU = -1f;
    float openT;            // 0 닫힘 ~ 1 열림
    bool open;
    Vector2 scroll;

    public static bool IsOpen => inst != null && inst.open;
    public static void Open () { if (inst != null && !MolakMenu.IsOpen) { inst.open = true; inst.scroll = Vector2.zero; } }
    public static void Close () { if (inst != null) inst.open = false; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot ()
    {
        if (inst) return;
        var go = new GameObject("MolakBag"); DontDestroyOnLoad(go);
        inst = go.AddComponent<MolakBag>();
    }

    static Texture2D Px { get { if (px == null) { px = new Texture2D(1,1); px.SetPixel(0,0,Color.white); px.Apply(); } return px; } }
    static void Box (Rect r, Color c) { var g = GUI.color; GUI.color = new Color(c.r, c.g, c.b, c.a * g.a); GUI.DrawTexture(r, Px); GUI.color = g; }
    static float Get (string n) { try { return MolakStats.Get(n); } catch { return 0f; } }

    void EnsureStyles (float u)
    {
        if (sName != null && Mathf.Approximately(builtU, u)) return;
        builtU = u;
        if (gowun == null) gowun = Resources.Load<Font>("GowunBatang");
        System.Func<int, TextAnchor, GUIStyle> mk = (size, an) => { var s = new GUIStyle(GUI.skin.label){ fontSize = Mathf.RoundToInt(size*u), alignment = an, richText = true, wordWrap = true }; if (gowun != null) s.font = gowun; return s; };
        sHead = mk(40, TextAnchor.MiddleCenter);
        sSec = mk(26, TextAnchor.MiddleLeft);
        sName = mk(32, TextAnchor.UpperLeft);
        sDesc = mk(25, TextAnchor.UpperLeft);
        sEff = mk(23, TextAnchor.UpperRight);
        sHint = mk(20, TextAnchor.MiddleCenter);
    }

    void Update ()
    {
        if (MolakMenu.IsOpen && open) open = false;
        openT = Mathf.MoveTowards(openT, open ? 1f : 0f, Time.unscaledDeltaTime * 5f);
    }

    void OnGUI ()
    {
        if (!Engine.Initialized) return;
        float u = MolakTheme.U;
        EnsureStyles(u);
        float w = Screen.width, h = Screen.height;
        bool night = MolakTheme.Night;
        float day = Get("day");

        // 닫힘 상태: 하단 손잡이(인게임에서만)
        if (openT <= 0.001f)
        {
            if (day < 1f || MolakMenu.IsOpen) return;
            var hb = new Rect(w/2f - 70*u, h - 26*u, 140*u, 5*u);
            Box(hb, night ? new Color(0.75f,0.7f,0.62f,0.55f) : new Color(0.25f,0.2f,0.16f,0.45f));
            sHint.normal.textColor = night ? new Color(0.75f,0.7f,0.62f,0.8f) : new Color(0.35f,0.3f,0.25f,0.8f);
            GUI.Label(new Rect(w/2f - 100*u, h - 60*u, 200*u, 30*u), "행낭 ⌃", sHint);
            return;
        }

        // 열림: 전체 가드 + 바깥 탭으로 닫기
        MolakInputGuard.Guard(new Rect(0, 0, w, h));
        float panelH = h * 0.64f;
        float y = h - panelH * Mathf.SmoothStep(0f, 1f, openT);
        var dim = new Rect(0, 0, w, h);
        Box(dim, new Color(0f, 0f, 0f, 0.35f * openT));
        if (Event.current.type == EventType.MouseDown && !new Rect(0, y, w, panelH).Contains(Event.current.mousePosition)) { open = false; Event.current.Use(); }

        var panel = new Rect(0, y, w, panelH);
        Box(panel, night ? new Color(0.09f, 0.08f, 0.075f, 0.98f) : new Color(0.965f, 0.945f, 0.905f, 0.98f));
        Box(new Rect(0, y, w, 2*u), MolakTheme.Blood);
        // 손잡이
        Box(new Rect(w/2f - 60*u, y + 14*u, 120*u, 5*u), night ? new Color(0.6f,0.55f,0.5f,0.8f) : new Color(0.35f,0.3f,0.25f,0.6f));
        var ink = night ? new Color(0.9f, 0.87f, 0.82f, 1f) : MolakTheme.Ink;
        var ink3 = night ? new Color(0.68f, 0.64f, 0.58f, 1f) : MolakTheme.Ink3;
        sHead.normal.textColor = ink; sSec.normal.textColor = MolakTheme.Blood; sName.normal.textColor = ink; sDesc.normal.textColor = ink3; sEff.normal.textColor = MolakTheme.Gold;
        GUI.Label(new Rect(0, y + 28*u, w, 60*u), "— 행낭(行囊) —", sHead);
        // 닫기
        var xr = new Rect(w - 110*u, y + 30*u, 80*u, 56*u);
        var xs = new GUIStyle(sHead){ fontSize = Mathf.RoundToInt(34*u) };
        GUI.Label(xr, "✕", xs);
        if (Event.current.type == EventType.MouseDown && xr.Contains(Event.current.mousePosition)) { open = false; Event.current.Use(); }

        // 목록
        float cx = Mathf.Max(40*u, (w - Mathf.Min(w*0.9f, 920*u))/2f), cw = w - 2*cx;
        float listY = y + 100*u, listH = panelH - 120*u;
        var sections = new[] { "물건", "비방 · 정보", "인연" };
        var tables = new[] { Items, Lore, Bonds };
        // 높이 계산
        float total = 0f; int shown = 0;
        var rows = new List<object>();
        for (int s = 0; s < 3; s++)
        {
            var have = new List<Entry>();
            foreach (var e in tables[s]) if (Get(e.Flag) > 0f) have.Add(e);
            if (have.Count == 0) continue;
            rows.Add(sections[s]); total += 52*u;
            foreach (var e in have) { rows.Add(e); total += RowH(e, cw, u); shown++; }
        }
        if (shown == 0)
        {
            sDesc.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(cx, listY, cw, 120*u), "아직 아무것도 없다.\n빈 행낭이 가장 가볍다—라고 사형은 말했었다.", sDesc);
            sDesc.alignment = TextAnchor.UpperLeft;
            return;
        }
        var view = new Rect(cx, listY, cw, listH);
        var content = new Rect(0, 0, cw - 20*u, total + 20*u);
        scroll = GUI.BeginScrollView(view, scroll, content, GUIStyle.none, GUIStyle.none);
        float ry = 0f;
        foreach (var o in rows)
        {
            if (o is string)
            {
                GUI.Label(new Rect(0, ry + 10*u, cw, 36*u), "· " + (string)o, sSec);
                Box(new Rect(0, ry + 48*u, cw - 20*u, Mathf.Max(1f, 1*u)), new Color(ink3.r, ink3.g, ink3.b, 0.35f));
                ry += 52*u; continue;
            }
            var e = (Entry)o;
            float rh = RowH(e, cw, u);
            GUI.Label(new Rect(0, ry + 10*u, cw*0.62f, 40*u), e.Name, sName);
            if (!string.IsNullOrEmpty(e.Effect)) GUI.Label(new Rect(cw*0.55f, ry + 14*u, cw*0.45f - 24*u, 34*u), e.Effect, sEff);
            if (!string.IsNullOrEmpty(e.Desc)) GUI.Label(new Rect(0, ry + 50*u, cw - 24*u, rh - 54*u), e.Desc, sDesc);
            ry += rh;
        }
        GUI.EndScrollView();
    }

    float RowH (Entry e, float cw, float u)
    {
        float dh = string.IsNullOrEmpty(e.Desc) ? 0f : sDesc.CalcHeight(new GUIContent(e.Desc), cw - 24*u);
        return 58*u + dh + 14*u;
    }
}
