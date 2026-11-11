using Naninovel;
using UnityEngine;

/// <summary>
/// 자동 기록 — 폰은 앱이 언제 꺼질지 모른다(나니노벨 기본은 '끌 때 저장'뿐).
///  · 이야기가 손님의 입력을 기다리는 안정된 순간(글 넘김·선택지)에, 마지막 기록에서 90초가 지났으면 자동 저장.
///  · 앱이 뒤로 갈 때(OnApplicationPause) 입력 대기 중이면 즉시 자동 저장.
///  · 저장하면 오른쪽 위에 작은 먹 글씨로 '기록했다'.
///  · 타이틀 '이어하기'는 자동·빠른 저장 중 더 최근 것으로 곧장 이어 간다(LastKind).
/// </summary>
public class MolakAutoSave : MonoBehaviour
{
    static MolakAutoSave inst;
    float lastSave = -999f, toastT = -1f;
    bool hooked, waiting, saving;
    GUIStyle st;

    public const string KindKey = "molak_lastsave";
    /// <summary>"auto" | "quick" | ""</summary>
    public static string LastKind { get { return PlayerPrefs.GetString(KindKey, ""); } }
    public static void MarkQuick () { PlayerPrefs.SetString(KindKey, "quick"); PlayerPrefs.Save(); }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot ()
    {
        if (inst) return;
        var go = new GameObject("MolakAutoSave"); DontDestroyOnLoad(go);
        inst = go.AddComponent<MolakAutoSave>();
    }

    static bool InGame ()
    {
        try { return Engine.Initialized && MolakStats.Get("day") >= 1f && !MolakMenu.IsOpen; } catch { return false; }
    }

    void Update ()
    {
        if (!hooked && Engine.Initialized)
        {
            var pl = Engine.GetService<IScriptPlayer>();
            if (pl != null)
            {
                pl.OnAwaitInput += t => { waiting = true; TrySave(false); };
                pl.OnExecute += t => waiting = false;
                hooked = true;
            }
        }
        if (toastT >= 0f) { toastT += Time.unscaledDeltaTime; if (toastT > 1.8f) toastT = -1f; }
    }

    void OnApplicationPause (bool paused) { if (paused) TrySave(true); }
    void OnApplicationFocus (bool focus) { if (!focus) TrySave(true); }

    async void TrySave (bool force)
    {
        if (saving || !InGame() || !waiting) return;
        if (!force && Time.unscaledTime - lastSave < 90f) return;
        var pl = Engine.GetService<IScriptPlayer>();
        if (pl != null && pl.Skipping) return;
        var state = Engine.GetService<IStateManager>();
        if (state == null) return;
        saving = true;
        try
        {
            await state.AutoSave();
            lastSave = Time.unscaledTime;
            PlayerPrefs.SetString(KindKey, "auto"); PlayerPrefs.Save();
            if (!force) toastT = 0f;
        }
        catch (System.Exception e) { Debug.LogWarning("[MolakAutoSave] " + e.Message); }
        finally { saving = false; }
    }

    /// <summary>플레이 기록(시험판용): 생마다 한 줄 — 기기 안 persistentDataPath/molak_playlog.csv. 시험하는 사람이 파일째 보내 주면 된다.</summary>
    public static string LogPath { get { return System.IO.Path.Combine(Application.persistentDataPath, "molak_playlog.csv"); } }
    public static void LogLife (float gained)
    {
        try
        {
            var vars = Engine.GetService<ICustomVariableManager>();
            string cause = "";
            if (vars != null) { var v = vars.GetVariableValue("deathcause"); cause = v.ToString(); }
            bool head = !System.IO.File.Exists(LogPath);
            string G (string n) { return MolakStats.Get(n).ToString("0"); }
            var line = string.Join(",", new[] {
                System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"), G("g_runs"), cause.Replace(",", " "), G("day"), G("rank"),
                G("sw"), G("ng"), G("sl"), G("curse"), G("mn"), G("aff"), gained.ToString("0"), G("g_redverse"), G("f_hapbyeok") });
            System.IO.File.AppendAllText(LogPath, (head ? "시각,회귀,사인,날,서열,검술,내공,살기,저주,은자,소아,깨달음,적운구결,합벽검보\n" : "") + line + "\n");
        }
        catch (System.Exception e) { Debug.LogWarning("[MolakAutoSave] log " + e.Message); }
    }

    /// <summary>가장 최근 기록에서 이어 간다. 이어 갈 기록이 없으면 false.</summary>
    public static bool ContinueLatest ()
    {
        if (!Engine.Initialized) return false;
        var state = Engine.GetService<IStateManager>();
        if (state == null) return false;
        bool auto = state.AutoLoadAvailable, quick = state.QuickLoadAvailable;
        if (!auto && !quick) return false;
        bool useQuick = quick && (!auto || LastKind == "quick");
        if (useQuick) _ = state.QuickLoad(); else _ = state.AutoLoad();
        return true;
    }

    void OnGUI ()
    {
        if (toastT < 0f) return;
        float u = MolakTheme.U;
        if (st == null || st.fontSize != Mathf.RoundToInt(24 * u))
        {
            st = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(24 * u), alignment = TextAnchor.MiddleRight };
            var f = Resources.Load<Font>("GowunBatang"); if (f != null) st.font = f;
        }
        float a = toastT < 0.25f ? toastT / 0.25f : toastT > 1.3f ? (1.8f - toastT) / 0.5f : 1f;
        st.normal.textColor = new Color(MolakTheme.Ink3.r, MolakTheme.Ink3.g, MolakTheme.Ink3.b, 0.85f * a);
        GUI.Label(new Rect(Screen.width - 260 * u, 52 * u, 230 * u, 36 * u), "— 기록했다", st);
    }
}
