using System.Text;
using Naninovel;
using UnityEngine;

/// <summary>
/// 0.4.88 체세(體勢) 비무 — 시험판(비무 연습의 호치 한 명).
/// 삼세 상성(受›攻 · 變›守 · 斬›虛) 위에 전략을 얹는다.
///  · 체세: 상대의 자세. 상성을 맞히면 한 칸, 약점 응수면 두 칸 깨진다. 다 깨지면 파세(破勢) — 상대는 한 차례 쉬고,
///    그 차례엔 무엇이든 두 배, 「落」이 열린다. 파세가 끝나면 체세가 다 돌아온다.
///  · 차례 줄: 상대의 '지금'과 '다음'. 지금은 검술로 가끔 읽히고(허초가 찌르기로 보일 수 있다), 잔상(지난 생에 이 상대에게
///    죽어 본 몸)은 둘 다를 보랏빛으로 보여 준다 — 잔상은 속지 않는다. 「觀」도 둘 다 보여 준다. 이막이 되면 잔상이 깨진다(상대가 판을 바꾼다).
///  · 고를 것은 셋(受·變·斬)이 바탕. 그때그때 하나가 더 뜬다 — 큰 수가 오면 雲步, 읽히지 않으면 觀, 무너뜨리면 落.
///  · 이막: 기혈이 반 아래로 꺾이면 체세가 하나 늘고 약점 하나가 사라지며, 두 차례 동안 「독아쇄」를 모은다.
///    모으는 동안 파세를 내면 끊기고, 못 끊으면 크게 맞는다(운보로 비켜설 수 있다).
/// 대본: MolakDuel.nani #StRun. 화면: 그림 창 위에 체세·기혈·약점·차례 줄·기를 그린다.
/// </summary>
public static class MolakStance
{
    public static bool Active;
    // 상대
    public static string FoeName = "독아단 아장 호치";
    public static int FoeHp, FoeHpMax, Shield, ShieldMax, Phase, Charge;   // Charge: 큰 수까지 남은 차례(0 = 안 모음)
    public static bool WeakJu = true, WeakByeon = true;     // 受·變 약점(이막엔 變 사라짐)
    public static bool KnowJu, KnowByeon;                   // 이 판(또는 지난 생)에 드러났나
    // 나
    public static int Bp, Boost, Broken, UnboCd, GwanT, Turn;
    public static bool Jansang, JansangBroken;
    // 차례 줄: 0 攻 · 1 守 · 2 虛
    public static readonly int[] Queue = new int[3];
    // 허초가 찌르기로 보이는 칸(검술로만 본 칸은 속는다 — 잔상·觀은 속지 않는다)
    public static readonly bool[] Fake = new bool[3];
    static bool lostLast, pendCharge;
    public static bool GwanUsed;   // 觀은 막마다 한 번
    public static string LastSeal = "";

    static float V (string k) { return MolakStats.Get(k); }
    static void S (string k, float v) { MolakStats.Set(k, v); }
    static void T (string k, string v) { MolakStats.SetStr(k, v); }

    public static string StanceName (int s) { return s == 0 ? "攻" : s == 1 ? "守" : "虛"; }
    public static string StanceWord (int s) { return s == 0 ? "찌르러 온다" : s == 1 ? "웅크려 받는다" : "허초로 흔든다"; }

    public static void Init ()
    {
        Active = true;
        FoeHpMax = FoeHp = 24; ShieldMax = Shield = 4; Phase = 1; Charge = 0;
        WeakJu = true; WeakByeon = true;
        Jansang = V("pjan") > 0 || V("g_died_hochi") > 0;
        KnowJu = Jansang; KnowByeon = false;
        JansangBroken = false;
        Bp = 1; Boost = 0; Broken = 0; UnboCd = 0; GwanT = 0; Turn = 1; lostLast = false; pendCharge = false; GwanUsed = false;
        for (int i = 0; i < 3; i++) Set(i, Roll(false));
        S("send", 0); S("duelwin", 0);
        T("foename", FoeName);
        Sync();
        T("stell", Jansang
            ? "<color=#9A70C4>지난 생, 이 도끼에 목이 떨어졌다.</color> 다음 수 하나가 보랏빛으로 먼저 보인다."
            : "호치가 도끼를 어깨에서 내린다. 입가의 흉터가 웃는다.");
    }

    /// <summary>호치의 성미: 찌르기가 잦고, 합에서 밀리면 웅크린다. 이막엔 찌르고 흔들기를 번갈아 한다.</summary>
    static int Roll (bool afterLoss)
    {
        if (Phase >= 2) return Random.value < 0.5f ? 0 : 2;
        if (afterLoss && Random.value < 0.7f) return 1;
        float r = Random.value;
        return r < 0.55f ? 0 : r < 0.75f ? 1 : 2;
    }

    static void Set (int i, int st) { Queue[i] = st; Fake[i] = st == 2 && Random.value < 0.35f; }
    /// <summary>화면에 보이는 수: 검술로만 읽은 칸이면 허초가 찌르기로 보일 수 있다.</summary>
    public static int Shown (int i) { return (Fake[i] && GwanT <= 0 && !PurpleSlot(i)) ? 0 : Queue[i]; }
    public static bool SeeSlot (int i)
    {
        if (GwanT > 0) return true;
        if (i == 0) return ReadNow || (Jansang && !JansangBroken);
        if (i == 1) return Jansang && !JansangBroken;
        return false;
    }
    public static bool PurpleSlot (int i) { return GwanT <= 0 && Jansang && !JansangBroken && (i == 1 || (i == 0 && !ReadNow)); }
    /// <summary>이번 차례 첫 칸을 검술로 읽어 냈나 — 검술이 높을수록 자주(8 → 열에 둘, 20 → 열에 여섯, 34 → 열에 여덟 남짓).</summary>
    public static bool ReadNow;

    /// <summary>차례 머리: 상대의 이번 수를 정하고 글·변수를 맞춘다.</summary>
    public static void Pre ()
    {
        if (lostLast && Phase < 2) Set(0, Roll(true));   // 밀린 뒤엔 웅크린다 — 차례 줄의 첫 칸이 바뀔 수 있다
        lostLast = false;
        ReadNow = Random.value < Mathf.Clamp01((V("sw") - 6f) / 30f) * 0.7f + 0.1f;
        if (Charge > 0) { Queue[0] = 1; Fake[0] = false; }                       // 모으는 동안엔 웅크린 채다
        Sync();
        var sb = new StringBuilder();
        if (Broken > 0) sb.Append("<color=#E0553F>호치의 무릎이 꺾여 있다.</color> 이번 차례엔 무엇이든 두 배로 들어간다.");
        else if (Charge > 0) sb.Append("<color=#C03434>호치가 몸을 웅크리고 도끼날을 혀로 핥는다.</color> 「독아쇄」까지 " + (Charge == 1 ? "한 차례" : "두 차례") + ". 그 전에 체세를 다 깨면 끊긴다.");
        else if (SeeSlot(0)) sb.Append("호치가 " + StanceWord(Shown(0)) + "." + (PurpleSlot(0) ? " <color=#9A70C4>지난 생에 본 그대로다.</color>" : ""));
        else sb.Append("호치의 어깨가 낮다. 무엇이 올지 아직 읽히지 않는다.");
        T("stell", sb.ToString());
    }

    public static void BoostUp () { if (Bp > Boost && Boost < 2) Boost++; Sync(); Pre(); }
    public static void BoostDown () { if (Boost > 0) Boost--; Sync(); Pre(); }
    public static void Gwan ()
    {
        if (V("ng") < 2 || GwanUsed) return;
        GwanUsed = true;
        MolakStats.Add("ng", -2f);
        GwanT = 2; KnowJu = true; KnowByeon = WeakByeon || KnowByeon;
        Sync(); Pre();
        T("stell", "<color=#C9A227>觀</color> — 눈을 반쯤 감는다. 도끼가 지나갈 길 셋이 한꺼번에 보인다. 약점도 드러난다.");
    }

    /// <summary>몸과 상대의 격 차이: 검술 8이면 1.5배, 20이면 그대로, 34면 0.8배.</summary>
    static float Scale () { return Mathf.Clamp(1f + (20f - V("sw")) / 24f, 0.8f, 1.5f); }

    static int Beats (int me, int foe)   // 1 이김 · 0 비김 · -1 짐   (me: 0 受 · 1 變 · 2 斬)
    {
        if (me == 0) return foe == 0 ? 1 : foe == 1 ? 0 : -1;
        if (me == 1) return foe == 1 ? 1 : foe == 2 ? 0 : -1;
        return foe == 2 ? 1 : foe == 0 ? 0 : -1;
    }

    /// <summary>act: 1 受 · 2 變 · 3 斬 · 4 雲步 · 6 落</summary>
    public static void Act (int act)
    {
        var sb = new StringBuilder();
        int foe = Queue[0];
        bool foeActs = Broken <= 0;      // 무너진 상대는 이번 차례 쉰다
        int hits = 1;
        float mul = Broken > 0 ? 2f : 1f;
        bool breakNow = false; LastSeal = "";

        if (act == 4)
        {   // 雲步: 이번 수를 비켜선다. 치지 않는다. 기 하나.
            UnboCd = 2; Bp = Mathf.Min(4, Bp + 1);
            if (foeActs && Charge == 1) sb.Append(Jansang
                ? "<color=#9AA8F5>雲步</color> — 도끼가 세 번째로 물어뜯는다. <color=#9A70C4>지난 생엔 바로 여기서 목이 떨어졌다.</color> 이번엔 반 걸음 먼저 비켜서 있다. 바닥 돌만 쪼개진다."
                : "<color=#9AA8F5>雲步</color> — 독아쇄가 방금 서 있던 자리를 물어뜯는다. 바닥 돌이 쪼개진다.");
            else sb.Append("<color=#9AA8F5>雲步</color> — 반 걸음 비켜선다. 도끼가 옷자락만 스친다. 숨이 고르다.");
            if (foeActs && Charge == 1) Charge = 0;
        }
        else if (act == 6)
        {   // 落: 파세 때만. 크게 벤다. 저주 +4.
            int d = Mathf.RoundToInt(4 * mul);
            FoeHp -= d; MolakStats.Add("curse", 4f);
            MolakVfx.Flash(new Color(0.7f, 0.1f, 0.08f, 1f), 0.4f); MolakVfx.Shake(14f); MolakSfx.Clash(true);
            sb.Append("<color=#E0553F>落</color> — 문양이 팔을 타고 내려와 칼끝에 맺힌다. 위에서 아래로, 한 획. 기혈 −" + d + ".");
        }
        else
        {
            int me = act - 1;
            int r = Beats(me, foe);
            if (!foeActs) r = 1;   // 쉬는 상대에겐 무엇이든 들어간다
            if (r > 0)
            {
                bool weak = (me == 0 && WeakJu) || (me == 1 && WeakByeon);
                if (weak) { if (me == 0) KnowJu = true; else KnowByeon = true; }
                int d = Mathf.RoundToInt(2 * hits * mul);
                FoeHp -= d;
                if (Broken <= 0)
                {
                    int sh = (weak ? 2 : 1) * hits;
                    Shield = Mathf.Max(0, Shield - sh);
                    if (Shield == 0) breakNow = true;
                }
                lostLast = true;
                MolakVfx.Flash(new Color(1f, 0.95f, 0.85f, 1f), 0.18f); MolakVfx.Shake(6f + 3f * hits); MolakSfx.Clash(hits > 1);
                sb.Append(MeLine(me, foe, foeActs) + (hits > 1 ? " " + hits + "번." : "") + " 기혈 −" + d + (weak && Broken <= 0 ? " · <color=#7FD08A>약점</color> — 체세가 두 칸씩 깨진다." : "."));
            }
            else if (r == 0)
            {
                if (me == 2 && foe == 0) { MolakStats.Add("hp", -2f); FoeHp -= 1; sb.Append("칼과 도끼가 한 번씩 서로의 어깨를 스친다. 둘 다 피를 본다. 체력 −2."); MolakSfx.Clash(false); }
                else { sb.Append(me == 0 ? "받으려던 칼이 허공을 받는다. 호치는 오지 않았다." : "흔든 칼끝이 허초에 허초로 맞선다. 아무 일도 없다."); }
            }
            else
            {
                int dmg = Mathf.RoundToInt(((foe == 0 ? 5 : foe == 1 ? 2 : 4) + (Phase >= 2 ? 1 : 0)) * Scale());
                MolakStats.Add("hp", -dmg);
                MolakVfx.Flash(new Color(0.6f, 0.05f, 0.05f, 1f), 0.25f); MolakVfx.Shake(10f); MolakSfx.Hit();
                sb.Append(LoseLine(me, foe) + " 체력 −" + dmg + ".");
            }
        }

        // 큰 수(독아쇄)가 터지는가
        if (Charge > 0 && act != 4)
        {
            if (breakNow) { Charge = 0; sb.Append(" <color=#E3C26A>웅크린 자세가 무너지며 독아쇄가 끊긴다.</color>"); }
            else if (Charge == 1 && foeActs)
            {
                int big = Mathf.RoundToInt(9 * Scale()); MolakStats.Add("hp", -big); S("taint", Mathf.Max(V("taint"), 1f));
                MolakVfx.Flash(new Color(0.2f, 0.5f, 0.1f, 1f), 0.5f); MolakVfx.Shake(18f); MolakSfx.Hit();
                sb.Append(" <color=#C03434>독아쇄</color> — 도끼가 세 번 물어뜯는다. 체력 −" + big + ", 상처에 독이 든다." + (Jansang ? " <color=#9A70C4>같은 자리, 같은 도끼. 또 반 걸음 늦었다.</color>" : ""));
                Charge = 0;
            }
            else Charge--;
        }

        if (breakNow)
        {
            Broken = 1; Charge = 0;
            LastSeal = "破";
            sb.Append(" <color=#E0553F>파세(破勢)</color> — 호치의 무릎이 꺾인다.");
        }
        else if (Broken > 0)
        {
            Broken--;
            if (Broken == 0) { Shield = ShieldMax; sb.Append(" 호치가 도끼를 짚고 일어선다. 자세가 돌아온다."); if (pendCharge) { pendCharge = false; Charge = 2; } }
        }

        // 이막
        if (Phase == 1 && FoeHp <= FoeHpMax / 2)
        {
            FoeHp = FoeHpMax / 2;   // 막이 바뀌는 자리에서 멈춘다 — 이막을 건너뛰고 끝나지 않게
            Phase = 2; ShieldMax = 5; GwanUsed = false; WeakByeon = false; JansangBroken = Jansang;
            if (Broken <= 0) { Shield = ShieldMax; Charge = 2; } else pendCharge = true;   // 무너져 있으면 일어선 뒤에 모은다
            for (int i = 1; i < 3; i++) Set(i, Roll(false));   // 판을 바꾼다 — 줄 서 있던 수도 바뀐다
            sb.Append("\n<color=#C03434>호치가 웃음을 거둔다.</color> " + (Jansang ? "도끼를 고쳐 쥐는 손이 지난 생엔 본 적 없는 손이다. <color=#9A70C4>보랏빛 줄이 흩어진다.</color>" : "도끼를 고쳐 쥐는 손이 달라졌다.") + " 체세가 다섯으로 늘고, 흔들기는 더 약점이 아니다.");
        }

        // 차례를 넘긴다
        Queue[0] = Queue[1]; Fake[0] = Fake[1]; Queue[1] = Queue[2]; Fake[1] = Fake[2]; Set(2, Roll(false));
        if (UnboCd > 0 && act != 4) UnboCd--;
        if (GwanT > 0) GwanT--;
        if (act != 4) Bp = Mathf.Min(4, Bp + 1);
        Turn++;

        if (FoeHp <= 0) { FoeHp = 0; S("send", 1); S("duelwin", 1); sb.Append("\n<color=#C9A227>호치가 도끼를 놓친다.</color> 무릎이 흙에 닿는다." + (Jansang ? "\n<color=#9A70C4>지난 생의 내가 쓰러졌던 자리를 밟고 선다.</color> 목이 아직 붙어 있다." : "")); Active = false; }
        else if (V("hp") <= 0) { S("hp", 1f); S("send", 1); S("duelwin", 0); sb.Append("\n<color=#C03434>다리가 풀린다.</color> 도끼날이 목 앞에서 멈춘다. 연습판이라 멈춘 것이다."); Active = false; }
        T("sres", sb.ToString());
        Sync();
    }

    static string MeLine (int me, int foe, bool foeActs)
    {
        if (!foeActs) return me == 0 ? "쓰러진 자리로 칼을 되받아 친다." : me == 1 ? "흔들 것도 없다. 빈 옆구리를 긋는다." : "정면으로 내리친다.";
        if (me == 0) return "찌르러 온 도끼를 칼등으로 받아 흘리고, 돌아 나오며 벤다.";
        if (me == 1) return "웅크린 어깨 앞에서 칼끝을 두 번 흔든다. 받으려던 도끼가 엇나간다.";
        return "허초에 속지 않는다. 흔들리는 도끼 사이로 곧장 내리친다.";
    }
    static string LoseLine (int me, int foe)
    {
        if (foe == 0) return "흔든 칼끝 사이로 도끼가 그대로 들어온다.";
        if (foe == 1) return "웅크린 호치에게 정면으로 들어갔다가 도끼 자루에 갈비를 맞는다.";
        return "받으려 세운 칼 앞에서 도끼가 방향을 꺾는다. 허초였다.";
    }

    static void Sync ()
    {
        S("sbp", Bp); S("sboost", Boost); S("sbroken", Broken); S("scd", UnboCd); S("sphase", Phase);
        S("scanboost", 0); S("scharge", Charge); S("sread", SeeSlot(0) ? 1 : 0);
        S("sgwan", GwanUsed ? 0 : 1);
    }

    public static void End () { Active = false; }
}

/// <summary>@stance do:init|pre|boost|unboost|gwan|end  /  @stance act:1~6</summary>
[Alias("stance")]
public class StanceCommand : Command
{
    public StringParameter Do;
    public IntegerParameter Act;

#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        if (Assigned(Act))
        {
            MolakStance.Act(Act.Value);
            if (MolakStance.LastSeal == "破") MolakVfx.Stamp("破", "파세 — 호치가 한 차례 쉰다", true);
            return;
        }
        var d = Assigned(Do) ? Do.Value : "pre";
        if (d == "init") MolakStance.Init();
        else if (d == "boost") MolakStance.BoostUp();
        else if (d == "unboost") MolakStance.BoostDown();
        else if (d == "gwan") MolakStance.Gwan();
        else if (d == "end") MolakStance.End();
        else MolakStance.Pre();
    }
}

/// <summary>그림 창 위의 판: 체세 · 기혈 · 약점 · 차례 줄 · 큰 수 · 기.</summary>
public class MolakStanceView : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot () { var go = new GameObject("MolakStanceView"); DontDestroyOnLoad(go); go.AddComponent<MolakStanceView>(); }

    GUIStyle sS, sL, sH; float bu = -1f;

    static Rect Band ()
    {
        if (MolakTheme.TheaterNow) { float s = MolakLayout.CanvasPx; return new Rect(0f, MolakLayout.ThArtTop * s, Screen.width, MolakLayout.ThArtH * s); }
        float unit = Mathf.Min(Screen.width / 900f, Screen.height / 1600f);
        float top = Mathf.Max(0f, (Screen.height - 1600f * unit) * 0.5f);
        float w = Mathf.Min(Screen.width, 900f * unit);
        return new Rect((Screen.width - w) * 0.5f, top + MolakArt.BandTop * unit, w, MolakArt.BandH * unit);
    }

    static void Box (Rect r, Color c) { var g = GUI.color; GUI.color = c; GUI.DrawTexture(r, MolakGui.Px); GUI.color = g; }
    static void Frame (Rect r, Color c, float t) { Box(new Rect(r.x, r.y, r.width, t), c); Box(new Rect(r.x, r.yMax - t, r.width, t), c); Box(new Rect(r.x, r.y, t, r.height), c); Box(new Rect(r.xMax - t, r.y, t, r.height), c); }

    void OnGUI ()
    {
        if (!MolakStance.Active || !Engine.Initialized || MolakMenu.IsOpen || MolakTitle.Showing) return;
        float u = MolakGui.U;
        if (sS == null || !Mathf.Approximately(bu, u))
        {
            bu = u;
            sS = MolakGui.Label(26, u, TextAnchor.MiddleCenter, false);
            sL = MolakGui.Label(24, u, TextAnchor.MiddleLeft, false);
            sH = MolakGui.Label(34, u, TextAnchor.MiddleCenter, false); if (MolakGui.Hanja != null) sH.font = MolakGui.Hanja;
        }
        GUI.depth = MolakGui.DepthVfx - 5;
        var b = Band();
        float pad = 14f * u;
        var gold = new Color(0.83f, 0.71f, 0.42f, 1f); var ink = new Color(0.07f, 0.06f, 0.055f, 0.86f);
        var paper = new Color(0.93f, 0.90f, 0.85f, 1f); var dim = new Color(0.66f, 0.60f, 0.52f, 1f);
        var red = new Color(0.88f, 0.33f, 0.25f, 1f); var purple = new Color(0.78f, 0.66f, 0.93f, 1f); var green = new Color(0.50f, 0.82f, 0.54f, 1f);

        // ── 위: 이름 · 기혈 · 체세 · 약점
        var top = new Rect(b.x + pad, b.y + pad, b.width - pad * 2f, 112f * u);
        Box(top, ink);
        MolakGui.Ink(sL, paper);
        GUI.Label(new Rect(top.x + 12f * u, top.y + 6f * u, top.width * 0.5f, 34f * u), "호치" + (MolakStance.Phase >= 2 ? "  <color=#E0553F>이막</color>" : "") + "   <color=#A89F92><size=" + Mathf.RoundToInt(20 * u) + ">기혈 " + MolakStance.FoeHp + "/" + MolakStance.FoeHpMax + "</size></color>", sL);
        // 기혈 막대
        var hb = new Rect(top.x + top.width * 0.42f, top.y + 18f * u, top.width * 0.54f, 8f * u);
        Box(hb, new Color(0.2f, 0.12f, 0.1f, 1f));
        Box(new Rect(hb.x, hb.y, hb.width * Mathf.Clamp01(MolakStance.FoeHp / (float)MolakStance.FoeHpMax), hb.height), red);
        // 체세 칸
        float sx = top.x + 12f * u, sy = top.y + 48f * u, sw = 30f * u;
        MolakGui.Ink(sL, dim); GUI.Label(new Rect(sx, sy, 70f * u, 30f * u), "체세", sL);
        for (int i = 0; i < MolakStance.ShieldMax; i++)
        {
            var r = new Rect(sx + 72f * u + i * (sw + 8f * u), sy + 2f * u, sw, 26f * u);
            bool on = MolakStance.Broken <= 0 && i < MolakStance.Shield;
            Box(r, on ? paper : new Color(0.23f, 0.19f, 0.13f, 1f));
        }
        // 약점
        string wk = "약점 " + (MolakStance.KnowJu && MolakStance.WeakJu ? "<color=#7FD08A>受</color>" : "<color=#6B5E4C>?</color>") + " · "
                  + (MolakStance.Phase >= 2 ? "<color=#4A4036>變 없음</color>" : (MolakStance.KnowByeon ? "<color=#7FD08A>變</color>" : "<color=#6B5E4C>?</color>"));
        MolakGui.Ink(sL, dim); GUI.Label(new Rect(top.x + top.width * 0.58f, sy, top.width * 0.42f, 30f * u), wk, sL);
        MolakGui.Ink(sL, dim); GUI.Label(new Rect(top.x + 12f * u, sy + 32f * u, top.width, 28f * u), "<size=" + Mathf.RoundToInt(20 * u) + ">受 › 攻   變 › 守   斬 › 虛</size>", sL);

        // ── 아래: 차례 줄(상대의 다음 수 셋)
        float cw = 88f * u, ch = 70f * u, cy = b.yMax - ch - pad;
        Box(new Rect(b.x + pad - 6f * u, cy - 36f * u, 2f * (cw + 8f * u) + 6f * u + (MolakStance.JansangBroken ? 150f * u : 0f), ch + 42f * u), ink);
        MolakGui.Ink(sS, dim);
        GUI.Label(new Rect(b.x + pad, cy - 32f * u, cw, 28f * u), "지금", sS);
        GUI.Label(new Rect(b.x + pad + cw + 8f * u, cy - 32f * u, cw, 28f * u), "다음", sS);
        for (int i = 0; i < 2; i++)
        {
            var r = new Rect(b.x + pad + i * (cw + 8f * u), cy, cw, ch);
            bool see = MolakStance.SeeSlot(i), pur = MolakStance.PurpleSlot(i);
            bool skip = i == 0 && MolakStance.Broken > 0;
            bool chg = i == 0 && MolakStance.Charge > 0;
            Box(r, ink);
            Frame(r, skip ? dim : chg ? red : pur ? purple : see ? gold : dim, Mathf.Max(1f, 2f * u));
            if (skip) { MolakGui.Ink(sS, dim); GUI.Label(r, "쉰다", sS); continue; }
            if (chg) { MolakGui.Ink(sS, red); GUI.Label(r, "독아쇄\n" + MolakStance.Charge, sS); continue; }
            if (see) { int sh0 = MolakStance.Shown(i); MolakGui.Ink(sH, pur ? purple : sh0 == 0 ? red : paper); GUI.Label(r, MolakStance.StanceName(sh0), sH); }
            else { MolakGui.Ink(sH, dim); GUI.Label(r, "?", sH); }
        }
        // 잔상이 깨졌다
        if (MolakStance.JansangBroken) { MolakGui.Ink(sL, new Color(0.62f, 0.52f, 0.75f, 0.9f)); GUI.Label(new Rect(b.x + pad + 2f * (cw + 8f * u) + 6f * u, cy, 200f * u, ch), "잔상이\n깨졌다", sL); }

        GUI.color = Color.white;
    }
}
