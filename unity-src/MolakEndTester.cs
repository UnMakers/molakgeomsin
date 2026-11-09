using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Naninovel;
using UnityEngine;

/// <summary>엔딩 도달/밸런스 검증용 자동 플레이.
/// MolakEndTester.Run(god:true)  — 능력치 보정 + 엔딩 지향 선택, 1회차에서 종료.
/// MolakEndTester.Run(god:false, lives:N) — 무보정, 합리적 플레이어 정책으로 N회차까지 연속 플레이(상점 구매 포함). 엔딩 도달 시 종료.</summary>
public class MolakEndTester : MonoBehaviour
{
    public static MolakEndTester I;
    public static System.Text.StringBuilder Log = new System.Text.StringBuilder();
    public static System.Text.StringBuilder Lives = new System.Text.StringBuilder();
    public static string Status = "idle";
    public bool god = true; public int maxLives = 1; public int lives = 0;
    public bool done;
    float lastProgress; int lastIdx = -1; float lastDay = -1; string lastScript = ""; bool inOver;
    static readonly string[] PreferGod = { "혈쇄문의 초대", "비무판", "정면으로 벤다", "산으로", "들어간다", "응한다", "벤다", "싸운다", "막는다" };
    static readonly string[] Avoid = { "물러선다", "처음 화면", "도망", "포기" };
    static readonly string[] ShopPref = { "봉인 문양", "잔류 진기", "검흔", "단련된 육신", "전생의 기억", "묻어둔 은자", "다 시 그 날 로" };

    public static void Run (bool godMode) { Run(godMode, 1); }
    public static void Run (bool godMode, int maxLives)
    {
        if (I == null) { var go = new GameObject("MolakEndTester"); DontDestroyOnLoad(go); I = go.AddComponent<MolakEndTester>(); }
        I.god = godMode; I.maxLives = maxLives; I.lives = 0; I.done = false; I.inOver = false; Log.Length = 0; Lives.Length = 0; Status = "running"; I.StopAllCoroutines(); I.StartCoroutine(I.Loop());
    }

    static int Find (string[] labels, string s) { for (int i = 0; i < labels.Length; i++) if (labels[i].Contains(s)) return i; return -1; }

    int Policy (string[] labels)
    {
        // 상점
        if (labels.Any(l => l.Contains("다 시 그 날 로")))
        { foreach (var s in ShopPref) { int k = Find(labels, s); if (k >= 0) return k; } }
        // 비무 중 전술
        if (Find(labels, "정면으로 벤다") >= 0)
        {
            float odds = MolakStats.Get("odds10");
            if (odds <= 2 && Find(labels, "물러선다") >= 0) return Find(labels, "물러선다");
            if (Find(labels, "살기를 실어") >= 0 && odds <= 5) return Find(labels, "살기를 실어");
            return Find(labels, "정면으로 벤다");
        }
        // 밤 행동
        if (Find(labels, "몸을 쉰다") >= 0 && Find(labels, "마당에서 검을 간다") >= 0)
        {
            float hp = MolakStats.Get("hp"), hpm = Mathf.Max(1, MolakStats.Get("hpmax")), mn = MolakStats.Get("mn"), curse = MolakStats.Get("curse");
            float sw = MolakStats.Get("sw"), ng = MolakStats.Get("ng");
            int wolf = Find(labels, "혈쇄문의 초대");
            if (wolf >= 0 && hp >= hpm * 0.7f) return wolf;
            if (hp <= hpm * 0.4f) return Find(labels, "몸을 쉰다");
            if (mn < 5 && Find(labels, "저자로 나간다") >= 0) return Find(labels, "저자로 나간다");
            int duel = Find(labels, "비무판");
            if (duel >= 0 && hp >= hpm * 0.55f)
            {
                float o = 0; for (int i = 0; i < 3; i++) o += MolakDuel.Odds(MolakDuel.MakeFoe("any"), 0); o /= 3f;
                if (o >= 60f) return duel;
            }
            if (curse < 60f && ng <= sw + 4) return Find(labels, "가부좌");
            return Find(labels, "마당에서 검을 간다");
        }
        if (Find(labels, "날품을 판다") >= 0) return Find(labels, "날품을 판다");
        var ok = Enumerable.Range(0, labels.Length).Where(i => !Avoid.Any(a => labels[i].Contains(a))).ToArray();
        return ok.Length > 0 ? ok[Random.Range(0, ok.Length)] : Random.Range(0, labels.Length);
    }

    // ---- 지정 경로 주행: picks 순서대로 선택지 부분일치 선택, 없으면 정책. 상점(MolakOver 대기)에서 종료 ----
    public static string[] Picks = new string[0]; public static int PickIdx;
    public static void Drive (string[] picks)
    {
        if (I == null) { var go = new GameObject("MolakEndTester"); DontDestroyOnLoad(go); I = go.AddComponent<MolakEndTester>(); }
        Picks = picks; PickIdx = 0; I.god = false; I.maxLives = 1; I.lives = 0; I.done = false; I.inOver = false; Log.Length = 0; Lives.Length = 0; Status = "driving"; I.StopAllCoroutines(); I.StartCoroutine(I.DriveLoop());
    }
    IEnumerator DriveLoop ()
    {
        var sp = Engine.GetServiceOrErr<IScriptPlayer>();
        var vars = Engine.GetServiceOrErr<ICustomVariableManager>();
        sp.SetAutoPlay(true); sp.SetSkip(true); lastProgress = Time.time; lastScript = "";
        while (!done)
        {
            yield return new WaitForSeconds(0.15f);
            var tr = sp.MainTrack;
            float day = MolakStats.Get("day");
            string path = tr.PlayedScript != null ? tr.PlayedScript.Path : "-";
            if (path != lastScript) { Log.AppendLine("d" + day + " -> script " + path); lastScript = path; }
            if (tr.PlayedIndex != lastIdx) { lastProgress = Time.time; lastIdx = tr.PlayedIndex; }
            string dc = "";
            try { var v = vars.GetVariableValue("deathcause"); dc = v.Type == CustomVariableValueType.String ? v.String : ""; } catch { }
            sp.SetAutoPlay(true); sp.SetSkip(true);
            if (tr.Playing) continue;
            var btns = FindObjectsByType<Naninovel.UI.ChoiceHandlerButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(b => b.isActiveAndEnabled && b.GetComponent<UnityEngine.UI.Button>() != null && b.GetComponent<UnityEngine.UI.Button>().interactable).ToArray();
            if (btns.Length > 0)
            {
                var labels = btns.Select(b => b.GetComponentInChildren<TMPro.TMP_Text>(true)?.text ?? "?").ToArray();
                if (path == "MolakOver" && labels.Any(l => l.Contains("다 시 그 날 로"))) { Log.AppendLine("SHOP reached cause=" + dc + " labels=" + string.Join(" | ", labels)); Status = "shop:" + dc; done = true; yield break; }
                int k = -1;
                bool matched = false;
                if (PickIdx < Picks.Length) { k = Find(labels, Picks[PickIdx]); matched = k >= 0; }
                if (k < 0) k = Policy(labels);
                k = Mathf.Clamp(k, 0, btns.Length - 1);
                Log.AppendLine("d" + day + " [" + path + "#" + tr.PlayedIndex + "] choice: " + labels[k] + "   (of " + btns.Length + ")");
                btns[k].GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                float t0 = Time.time;
                while (!tr.Playing && Time.time - t0 < 2f) yield return null;
                if (tr.Playing && matched) PickIdx++;
                if (tr.Playing)
                    foreach (var b in FindObjectsByType<Naninovel.UI.ChoiceHandlerButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                        if (b != null && b.gameObject != null) b.gameObject.SetActive(false);
                sp.SetAutoPlay(true); sp.SetSkip(true); lastProgress = Time.time;
            }
            if (Time.time - lastProgress > 15f)
            {
                var cmd = tr.PlayedCommand;
                Log.AppendLine("STALL at [" + path + "] idx=" + tr.PlayedIndex + " cmd=" + (cmd != null ? cmd.GetType().Name + "@" + cmd.PlaybackSpot : "null") + " day=" + day + " cause=" + dc);
                Status = "stall"; done = true; yield break;
            }
        }
    }

    IEnumerator Loop ()
    {
        var sp = Engine.GetServiceOrErr<IScriptPlayer>();
        var vars = Engine.GetServiceOrErr<ICustomVariableManager>();
        sp.SetAutoPlay(true); sp.SetSkip(true);
        lastProgress = Time.time;
        while (!done)
        {
            yield return new WaitForSeconds(0.15f);
            var tr = sp.MainTrack;
            float day = MolakStats.Get("day");
            string path = tr.PlayedScript != null ? tr.PlayedScript.Path : "-";
            if (path != lastScript) { Log.AppendLine("d" + day + " rank=" + MolakStats.Get("rank") + " -> script " + path); lastScript = path; }
            if (tr.PlayedIndex != lastIdx || day != lastDay) { lastProgress = Time.time; lastIdx = tr.PlayedIndex; lastDay = day; }
            if (god && day >= 1)
            {
                MolakStats.Set("hp", Mathf.Max(MolakStats.Get("hp"), MolakStats.Get("hpmax") - 1));
                if (MolakStats.Get("sw") < 60) MolakStats.Set("sw", 60);
                if (MolakStats.Get("ng") < 60) MolakStats.Set("ng", 60);
                if (MolakStats.Get("curse") > 60) MolakStats.Set("curse", 60);
                if (MolakStats.Get("mn") < 20) MolakStats.Set("mn", 20);
            }
            string dc = "";
            try { var v = vars.GetVariableValue("deathcause"); dc = v.Type == CustomVariableValueType.String ? v.String : ""; } catch { }
            if (dc == "등극") { Log.AppendLine("ENDING REACHED day=" + day + " rank=" + MolakStats.Get("rank")); Lives.AppendLine("life" + (lives + 1) + ": ENDING day=" + day + " sw=" + MolakStats.Get("sw") + " ng=" + MolakStats.Get("ng")); Status = "ending"; done = true; yield break; }
            if (path == "MolakOver" && day >= 1 && !inOver)
            {
                inOver = true; lives++;
                Lives.AppendLine("life" + lives + ": day=" + day + " cause=" + dc + " rank=" + MolakStats.Get("rank") + " sw=" + MolakStats.Get("sw") + " ng=" + MolakStats.Get("ng") + " curse=" + Mathf.Round(MolakStats.Get("curse")) + " pts=" + MolakStats.Get("g_pts"));
                if (lives >= maxLives) { Status = "death:" + dc; done = true; yield break; }
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
                if (k < 0) k = Policy(labels);
                k = Mathf.Clamp(k, 0, btns.Length - 1);
                var pick = btns[k];
                Log.AppendLine("d" + day + " [" + path + "#" + tr.PlayedIndex + "] choice: " + labels[k]);
                pick.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                float t0 = Time.time;
                while (!tr.Playing && Time.time - t0 < 2f) yield return null;
                if (tr.Playing)
                    foreach (var b in FindObjectsByType<Naninovel.UI.ChoiceHandlerButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                        if (b != null && b.gameObject != null) b.gameObject.SetActive(false);
                sp.SetAutoPlay(true); sp.SetSkip(true);
                lastProgress = Time.time;
            }
            if (Time.time - lastProgress > 15f)
            {
                var cmd = tr.PlayedCommand;
                Log.AppendLine("STALL at [" + path + "] idx=" + tr.PlayedIndex + " cmd=" + (cmd != null ? cmd.GetType().Name + "@" + cmd.PlaybackSpot : "null") + " day=" + day);
                Status = "stall"; done = true; yield break;
            }
        }
    }
}
