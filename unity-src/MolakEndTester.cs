using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Naninovel;
using UnityEngine;

/// <summary>엔딩 도달/밸런스/경로 검증용 자동 플레이.
/// Run(god)            — 능력치 보정 + 엔딩 지향, 1회차.
/// Run(false, N)       — 무보정 '균형' 정책으로 N회차.
/// Batch(policy, N, tag) — 정책별 N회차 연속 플레이(전역 진척 초기화 후). 결과는 Build/soak_results.csv 에 한 줄씩 기록.
///   정책: 0 균형 · 1 공격(늘 싸움) · 2 신중(안전 위주) · 3 무작위(경로 탐색) · 4 장사꾼(은자·구매) · 5 사술(저주 이용) · 6 학습(균형 + 죽음을 부른 선택은 다음 회차부터 피함)
/// Drive(picks)        — 지정 선택 주행.</summary>
public class MolakEndTester : MonoBehaviour
{
    public static MolakEndTester I;
    public static System.Text.StringBuilder Log = new System.Text.StringBuilder();
    public static System.Text.StringBuilder Lives = new System.Text.StringBuilder();
    public static string Status = "idle";
    /// <summary>소크 결과는 프로젝트 옆 Build 폴더에 남긴다.</summary>
    static string BuildDir { get { return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), "Build"); } }
    public static string ResultPath { get { return System.IO.Path.Combine(BuildDir, "soak_results.csv"); } }
    public static string DeathPath { get { return System.IO.Path.Combine(BuildDir, "soak_deaths.txt"); } }
    public static HashSet<string> ChoiceSeen = new HashSet<string>();
    public static HashSet<string> DeathChoices = new HashSet<string>();
    string lastEventChoice = "";
    static readonly HashSet<int> clickedSets = new HashSet<int>();   // 이미 고른 선택지 묶음(버튼 인스턴스) — 같은 묶음을 두 번 누르지 않는다
    public static int DoubleSkips;
    public bool god = true; public int maxLives = 1; public int lives = 0; public int policy = 0; public string runTag = "";
    public bool done;
    float lastProgress; int lastIdx = -1; float lastDay = -1; string lastScript = ""; bool inOver;
    int choicesThisLife; int duelsThisLife; int duelWinsThisLife; float lastDuelWin = -1f;
    static readonly string[] PreferGod = { "혈쇄문의 초대", "비무판", "정면으로 벤다", "산으로", "들어간다", "응한다", "벤다", "싸운다", "막는다" };
    static readonly string[] Avoid = { "처음 화면" };
    static readonly string[] ShopPref = { "봉인 문양", "잔류 진기", "검흔", "단련된 육신", "청심차", "구르고", "빈칸 방값", "또렷한 기억", "묻어둔 은자", "다 시 그 날 로" };
    static readonly string[] Violent = { "벤다", "검을 뽑", "맞선", "덤빈", "칼", "친다", "꺾는다", "베어", "찌른다", "공터로" };
    static readonly string[] Careful = { "물러", "지나", "피한", "숨", "돌아", "모른 척", "고개를 숙", "쉰다", "거절", "놓아준다", "기다린다" };

    public static void Run (bool godMode) { Run(godMode, 1); }
    public static void Run (bool godMode, int maxLives)
    {
        Ensure(); I.god = godMode; I.maxLives = maxLives; I.policy = 0; I.runTag = "run"; I.Begin();
    }
    public static void Batch (int policy, int lives, string tag)
    {
        Ensure(); DeathChoices.Clear(); I.god = false; I.maxLives = lives; I.policy = policy; I.runTag = tag; I.Begin();
    }
    static void Ensure ()
    {
        if (I == null) { var go = new GameObject("MolakEndTester"); DontDestroyOnLoad(go); I = go.AddComponent<MolakEndTester>(); }
    }
    void Begin ()
    {
        lives = 0; done = false; inOver = false; Log.Length = 0; Lives.Length = 0; Status = "running"; ResetLifeCounters();
        StopAllCoroutines(); StartCoroutine(Loop());
    }
    void ResetLifeCounters () { choicesThisLife = 0; duelsThisLife = 0; duelWinsThisLife = 0; }

    static readonly string[] TruePath = { "합벽", "석함", "제단", "구결", "검보", "소아", "그녀", "지붕", "장부를 편다", "사당", "적운", "운검", "지난 생" };
    static int Find (string[] labels, string s) { for (int i = 0; i < labels.Length; i++) if (labels[i].Contains(s)) return i; return -1; }
    static int FindAny (string[] labels, string[] keys) { for (int i = 0; i < labels.Length; i++) foreach (var k in keys) if (labels[i].Contains(k)) return i; return -1; }
    static float EstOdds () { float o = 0; for (int i = 0; i < 3; i++) o += MolakDuel.Odds(MolakDuel.MakeFoe("any"), 0); return o / 3f; }
    float overSeen = -1f;
    static int Rnd (int n) { return Random.Range(0, n); }
    static string G (string n) { try { return MolakStats.Get(n).ToString("0"); } catch { return "-"; } }

    string curPath = "";
    int Choose (string[] labels)
    {
        bool shop = labels.Any(l => l.Contains("다 시 그 날 로"));
        float hp = MolakStats.Get("hp"), hpm = Mathf.Max(1, MolakStats.Get("hpmax")), mn = MolakStats.Get("mn"), curse = MolakStats.Get("curse");
        float sw = MolakStats.Get("sw"), ng = MolakStats.Get("ng");
        if (shop)
        {
            if (policy == 3) { int k0 = Rnd(labels.Length); return k0; }
            foreach (var s in ShopPref) { int k = Find(labels, s); if (k >= 0) return k; }
        }
        // 업보 관리: 독·곪은 상처는 고치고, 갚을 수 있는 빚은 갚는다 (무작위 제외)
        if (policy != 3)
        {
            if (MolakStats.Get("taint") > 0 && policy != 1 && policy != 5)
            {
                int cure = FindAny(labels, new[] { "해독탕", "상처를 내민다", "금창약을 덜어 덧난" });
                if (cure >= 0) return cure;
                if (mn >= 8 || MolakStats.Get("f_herbbond") > 0 || MolakStats.Get("f_herbfriend") > 0)
                {
                    int go = FindAny(labels, new[] { "저자로 나간다", "상점가를 둘러본다", "백초당 — 백초 할멈" });
                    if (go >= 0) return go;
                }
            }
            int pay = Find(labels, "빚을 갚는다"); if (pay >= 0) return pay;
        }
        // 비무 중 전술
        if (Find(labels, "정면으로 벤다") >= 0)
        {
            float odds = MolakStats.Get("odds10");
            int curseOpt = FindAny(labels, new[] { "사술" });
            int killer = Find(labels, "살기를 실어");
            int back = Find(labels, "물러선다");
            switch (policy)
            {
                case 1: if (killer >= 0 && odds <= 6) return killer; return Find(labels, "정면으로 벤다");
                case 2: case 7: if (odds <= 4 && back >= 0) return back; return Find(labels, "정면으로 벤다");
                case 3: return Rnd(labels.Length);
                case 5: if (curseOpt >= 0) return curseOpt; return Find(labels, "정면으로 벤다");
                default:
                    if (odds <= 2 && back >= 0) return back;
                    if (killer >= 0 && odds <= 5) return killer;
                    return Find(labels, "정면으로 벤다");
            }
        }
        // 밤 행동
        if (Find(labels, "몸을 쉰다") >= 0 && Find(labels, "마당에서 검을") >= 0)
        {
            int wolf = Find(labels, "혈쇄문의 초대"), duel = Find(labels, "비무판"), backRing = Find(labels, "사설 비무판");
            int rest = Find(labels, "몸을 쉰다"), town = Find(labels, "저자로 나간다"), med = Find(labels, "기를 돌린다"), train = Find(labels, "마당에서 검을");
            int leave = Find(labels, "소아와 떠난다");
            if (policy == 3) return Rnd(labels.Length);
            if (hp <= hpm * 0.35f) return rest;
            if (mn < 12 && town >= 0) return town;   // 며칠치 방값(0.4.3)
            switch (policy)
            {
                case 1:
                    if (wolf >= 0) return wolf;
                    if (backRing >= 0) return backRing;
                    if (duel >= 0 && hp >= hpm * 0.45f) return duel;
                    return train;
                case 2:
                    if (wolf >= 0 && hp >= hpm * 0.9f && EstOdds() >= 75f) return wolf;
                    if (duel >= 0 && hp >= hpm * 0.7f && EstOdds() >= 72f) return duel;
                    if (hp < hpm * 0.6f) return rest;
                    if (curse < 50f && med >= 0 && ng <= sw) return med;
                    return train;
                case 7:   // 진엔딩을 노리는 자: 적운의 구결(42일+ 혈랑대의 밤)까지는 오래 살아남고,
                          //   구결·검보를 쥔 생부터는 서열을 올려 혈쇄문을 꺾는다(교주전은 혈쇄문 + 서열 10위 안)
                {
                    bool push = MolakStats.Get("g_redverse") > 0 || MolakStats.Get("f_hapbyeok") > 0;
                    if (wolf >= 0 && hp >= hpm * (push ? 0.7f : 0.9f) && EstOdds() >= (push ? 58f : 80f)) return wolf;
                    if (backRing >= 0 && push && hp >= hpm * 0.6f && EstOdds() >= 62f) return backRing;
                    if (duel >= 0 && hp >= hpm * (push ? 0.55f : 0.75f) && EstOdds() >= (push ? 55f : 70f)) return duel;
                }
                    if (hp < hpm * 0.65f) return rest;
                    if (curse >= 45f && med >= 0) return med;
                    if (med >= 0 && ng + 3 < sw) return med;
                    return train;
                case 4:
                    if (mn < 26 && town >= 0 && Rnd(2) == 0) return town;
                    if (duel >= 0 && hp >= hpm * 0.6f && EstOdds() >= 60f) return duel;
                    return train;
                case 5:
                    if (wolf >= 0 && hp >= hpm * 0.6f) return wolf;
                    if (duel >= 0 && hp >= hpm * 0.5f) return duel;
                    if (med >= 0) return med;
                    return train;
                default:
                    if (leave >= 0 && Rnd(6) == 0) return leave;
                    if (wolf >= 0 && hp >= hpm * 0.7f) return wolf;
                    if (duel >= 0 && hp >= hpm * 0.55f && EstOdds() >= 60f) return duel;
                    if (curse < 60f && med >= 0 && ng <= sw + 4) return med;
                    return train;
            }
        }
        if (Find(labels, "날품을 판다") >= 0 && policy != 3 && policy != 4) return Find(labels, "날품을 판다");
        // 일반 사건
        var ok = Enumerable.Range(0, labels.Length).Where(i => !Avoid.Any(a => labels[i].Contains(a))).ToArray();
        if (policy == 6 || policy == 7) { var ok2 = ok.Where(i => !DeathChoices.Contains(curPath + "|" + labels[i])).ToArray(); if (ok2.Length > 0) ok = ok2; }
        if (policy == 7)
        {
            // 떠나는 결말(원행·도주)은 피하고, 진엔딩 사슬의 낱말과 전생의 앎으로 열린 선택지를 고른다
            var stay = ok.Where(i => !labels[i].Contains("소아와 떠난다") && !labels[i].Contains("떠나자")).ToArray(); if (stay.Length > 0) ok = stay;
            var tp = ok.Where(i => TruePath.Any(k => labels[i].Contains(k))).ToArray();
            if (tp.Length > 0 && Rnd(6) > 0) return tp[Rnd(tp.Length)];
            int c7 = FindAny(labels, Careful); if (c7 >= 0 && ok.Contains(c7) && Rnd(3) > 0) return c7;
        }
        if (ok.Length == 0) return Rnd(labels.Length);
        switch (policy)
        {
            case 1: { int v = FindAny(labels, Violent); if (v >= 0 && Rnd(4) > 0) return v; break; }
            case 2: { int c = FindAny(labels, Careful); if (c >= 0 && Rnd(4) > 0) return c; break; }
            case 4: { var paid = ok.Where(i => labels[i].Contains("(은자")).ToArray(); if (paid.Length > 0 && mn >= 8 && Rnd(3) > 0) return paid[Rnd(paid.Length)]; break; }
            case 5: { int c = FindAny(labels, new[] { "사술", "문양", "저주" }); if (c >= 0 && Rnd(3) > 0) return c; break; }
        }
        return ok[Rnd(ok.Length)];
    }

    void WriteResult (string line)
    {
        try { System.IO.File.AppendAllText(ResultPath, line + "\n", new System.Text.UTF8Encoding(false)); } catch { }
    }
    string LifeRow (string outcome, string cause)
    {
        int seen = 0; try { foreach (var v in Engine.GetService<ICustomVariableManager>().Variables) if (v.Name.StartsWith("seen_") && v.Value.Type == CustomVariableValueType.Numeric && v.Value.Number > 0) seen++; } catch { }
        return string.Join(",", new[] { runTag, policy.ToString(), (lives).ToString(), outcome, cause, MolakStats.Get("day").ToString("0"), MolakStats.Get("rank").ToString("0"),
            MolakStats.Get("sw").ToString("0"), MolakStats.Get("ng").ToString("0"), MolakStats.Get("hp").ToString("0"), MolakStats.Get("hpmax").ToString("0"),
            MolakStats.Get("mn").ToString("0"), MolakStats.Get("curse").ToString("0"), MolakStats.Get("sl").ToString("0"), seen.ToString(), choicesThisLife.ToString(),
            duelsThisLife.ToString(), duelWinsThisLife.ToString(), MolakStats.Get("g_pts").ToString("0"),
            "rv" + G("g_redverse") + "hb" + G("f_hapbyeok") + "hd" + G("g_hapdone") + "sb" + G("f_soabond") });
    }

    IEnumerator Loop ()
    {
        var sp = Engine.GetServiceOrErr<IScriptPlayer>();
        sp.SetAutoPlay(true); sp.SetSkip(true);
        lastProgress = Time.realtimeSinceStartup;
        while (!done)
        {
            yield return new WaitForSecondsRealtime(0.08f);
            var tr = sp.MainTrack;
            float day = MolakStats.Get("day");
            string path = tr.PlayedScript != null ? tr.PlayedScript.Path : "-";
            if (path != lastScript) { lastScript = path; }
            if (tr.PlayedIndex != lastIdx || day != lastDay) { lastProgress = Time.realtimeSinceStartup; lastIdx = tr.PlayedIndex; lastDay = day; }
            // 비무 집계
            float dw = MolakStats.Get("duelwin");
            if (path != "MolakDuel" && lastDuelWin >= 0f) { duelsThisLife++; if (dw > 0) duelWinsThisLife++; lastDuelWin = -1f; }
            if (god && day >= 1)
            {
                MolakStats.Set("hp", Mathf.Max(MolakStats.Get("hp"), MolakStats.Get("hpmax") - 1));
                if (MolakStats.Get("sw") < 60) MolakStats.Set("sw", 60);
                if (MolakStats.Get("ng") < 60) MolakStats.Set("ng", 60);
                if (MolakStats.Get("curse") > 60) MolakStats.Set("curse", 60);
                if (MolakStats.Get("mn") < 20) MolakStats.Set("mn", 20);
            }
            string dc = "";
            dc = MolakStats.Str("deathcause");
            // 사인 기록은 죽음 라벨의 @set 이 돈 뒤에 읽는다(라벨에 막 들어선 순간엔 아직 "none")
            if (path == "MolakOver" && day >= 1 && !inOver && (dc == "" || dc == "none"))
            {
                if (overSeen < 0f) overSeen = Time.realtimeSinceStartup;
                if (Time.realtimeSinceStartup - overSeen < 1.5f) continue;
            }
            if (path != "MolakOver") overSeen = -1f;
            if (path == "MolakOver" && day >= 1 && !inOver)
            {
                inOver = true; lives++;
                bool clear = dc == "등극" || dc == "성사" || dc == "순검" || dc == "합벽";
                if (!clear && dc != "비무 패배" && dc != "포박" && dc != "사술 폭주" && dc != "독" && dc != "곪은 상처" && lastEventChoice != "") DeathChoices.Add(lastEventChoice);
                lastEventChoice = "";
                string dk = MolakStats.Str("dkind");
                var row = LifeRow(clear ? "END" : "DEAD", dc == "비무 패배" ? dc + "(" + dk + ")" : dc);
                Lives.AppendLine(row); WriteResult(row);
                try
                {
                    // 사인 추적: 죽기 직전 선택 열두 줄
                    var ll = Log.ToString().Split('\n'); int from = Mathf.Max(0, ll.Length - 13);
                    System.IO.File.AppendAllText(DeathPath, "== " + row + "\n" + string.Join("\n", ll, from, ll.Length - from) + "\n", new System.Text.UTF8Encoding(false));
                }
                catch { }
                if (lives >= maxLives || (clear && runTag == "run")) { Status = (clear ? "ending:" : "death:") + dc; done = true; yield break; }
                ResetLifeCounters();
            }
            if (path != "MolakOver") inOver = false;
            sp.SetAutoPlay(true); sp.SetSkip(true);
            if (tr.Playing) continue;
            var btns = FindObjectsByType<Naninovel.UI.ChoiceHandlerButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(b => b.isActiveAndEnabled && b.GetComponent<UnityEngine.UI.Button>() != null && b.GetComponent<UnityEngine.UI.Button>().interactable).ToArray();
            if (btns.Length > 0)
            {
                var labels = btns.Select(b => b.GetComponentInChildren<TMPro.TMP_Text>(true)?.text ?? "?").ToArray();
                int k = -1;
                if (god) { foreach (var pf in PreferGod) { for (int i = 0; i < btns.Length; i++) if (labels[i].Contains(pf)) { k = i; break; } if (k >= 0) break; } }
                curPath = path;
                if (k < 0) k = Choose(labels);
                k = Mathf.Clamp(k, 0, btns.Length - 1);
                int at = tr.PlayedIndex;
                // 카드가 막 떠오른 직후의 첫 탭은 오조작 방지로 무시될 수 있다 → 같은 카드를 다시 누른다
                float t0 = Time.realtimeSinceStartup; int tries = 0;
                while (!tr.Playing && Time.realtimeSinceStartup - t0 < 3f)
                {
                    if (btns[k] == null || !btns[k].isActiveAndEnabled) break;
                    if (tries == 0 || Time.realtimeSinceStartup - t0 > 0.5f * tries) { btns[k].GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); tries++; }
                    yield return null;
                }
                if (!tr.Playing) { DoubleSkips++; continue; }
                ChoiceSeen.Add(path + "|" + labels[k]);
                if (path != "MolakDuel" && path != "MolakOver" && Find(labels, "몸을 쉰다") < 0) lastEventChoice = path + "|" + labels[k];
                if (Find(labels, "정면으로 벤다") >= 0) lastDuelWin = 0f;
                choicesThisLife++;
                Log.AppendLine("d" + day + " hp" + MolakStats.Get("hp").ToString("0") + "/" + MolakStats.Get("hpmax").ToString("0") + (MolakStats.Get("taint") > 0 ? " T" + MolakStats.Get("taint") : "") + (MolakStats.Get("injury") > 0 ? " I" : "") + " [" + path + "#" + at + "] " + labels[k]);
                if (Log.Length > 200000) Log.Remove(0, 100000);
                if (tr.Playing)
                    foreach (var b in FindObjectsByType<Naninovel.UI.ChoiceHandlerButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                        if (b != null && b.gameObject != null) b.gameObject.SetActive(false);
                sp.SetAutoPlay(true); sp.SetSkip(true);
                lastProgress = Time.realtimeSinceStartup;
            }
            if (Time.realtimeSinceStartup - lastProgress > 15f)
            {
                var cmd = tr.PlayedCommand;
                string where = "[" + path + "] idx=" + tr.PlayedIndex + " cmd=" + (cmd != null ? cmd.GetType().Name + "@" + cmd.PlaybackSpot : "null") + " day=" + day;
                Log.AppendLine("STALL at " + where);
                WriteResult(runTag + "," + policy + "," + lives + ",STALL," + where.Replace(",", ";"));
                Status = "stall"; done = true; yield break;
            }
        }
    }

    // ---- 지정 경로 주행 ----
    public static string[] Picks = new string[0]; public static int PickIdx;
    public static void Drive (string[] picks)
    {
        Ensure();
        Picks = picks; PickIdx = 0; I.god = false; I.maxLives = 1; I.lives = 0; I.done = false; I.inOver = false; Log.Length = 0; Lives.Length = 0; Status = "driving"; I.StopAllCoroutines(); I.StartCoroutine(I.DriveLoop());
    }
    IEnumerator DriveLoop ()
    {
        var sp = Engine.GetServiceOrErr<IScriptPlayer>();
        sp.SetAutoPlay(true); sp.SetSkip(true); lastProgress = Time.realtimeSinceStartup; lastScript = "";
        while (!done)
        {
            yield return new WaitForSecondsRealtime(0.1f);
            var tr = sp.MainTrack;
            float day = MolakStats.Get("day");
            string path = tr.PlayedScript != null ? tr.PlayedScript.Path : "-";
            if (path != lastScript) { Log.AppendLine("d" + day + " -> script " + path); lastScript = path; }
            if (tr.PlayedIndex != lastIdx) { lastProgress = Time.realtimeSinceStartup; lastIdx = tr.PlayedIndex; }
            string dc = "";
            dc = MolakStats.Str("deathcause");
            sp.SetAutoPlay(true); sp.SetSkip(true);
            if (tr.Playing) continue;
            var btns = FindObjectsByType<Naninovel.UI.ChoiceHandlerButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(b => b.isActiveAndEnabled && b.GetComponent<UnityEngine.UI.Button>() != null && b.GetComponent<UnityEngine.UI.Button>().interactable).ToArray();
            if (btns.Length > 0)
            {
                var labels = btns.Select(b => b.GetComponentInChildren<TMPro.TMP_Text>(true)?.text ?? "?").ToArray();
                if (path == "MolakOver" && labels.Any(l => l.Contains("다 시 그 날 로"))) { Log.AppendLine("SHOP reached cause=" + dc); Status = "shop:" + dc; done = true; yield break; }
                int k = -1; bool matched = false;
                if (PickIdx < Picks.Length) { k = Find(labels, Picks[PickIdx]); matched = k >= 0; }
                if (k < 0) k = Choose(labels);
                k = Mathf.Clamp(k, 0, btns.Length - 1);
                Log.AppendLine("d" + day + " [" + path + "#" + tr.PlayedIndex + "] choice: " + labels[k] + "   (of " + btns.Length + ")");
                btns[k].GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                float t0 = Time.realtimeSinceStartup;
                while (!tr.Playing && Time.realtimeSinceStartup - t0 < 2f) yield return null;
                if (tr.Playing && matched) PickIdx++;
                if (tr.Playing)
                    foreach (var b in FindObjectsByType<Naninovel.UI.ChoiceHandlerButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                        if (b != null && b.gameObject != null) b.gameObject.SetActive(false);
                sp.SetAutoPlay(true); sp.SetSkip(true); lastProgress = Time.realtimeSinceStartup;
            }
            if (Time.realtimeSinceStartup - lastProgress > 15f)
            {
                var cmd = tr.PlayedCommand;
                Log.AppendLine("STALL at [" + path + "] idx=" + tr.PlayedIndex + " cmd=" + (cmd != null ? cmd.GetType().Name + "@" + cmd.PlaybackSpot : "null") + " day=" + day + " cause=" + dc);
                Status = "stall"; done = true; yield break;
            }
        }
    }
}
