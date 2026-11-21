using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 배경음 — 앱이 켜지는 순간부터 끝까지 끊기지 않고 흐른다. 나니노벨 상태(저장 불러오기·처음으로)와 무관하게 DontDestroyOnLoad 오브젝트에서 재생.
///
/// 곡 묶음(Resources/MolakBgm/{묶음}_00, _01 …)을 차례로 틀고 끝나면 묶음의 처음으로.
///  · bgm    — 기본 묶음(지금은 테스트 곡 2시간 51분을 무음 지점에서 13토막으로 자른 것)
///  · title / day / duel / raid / end — 장면 묶음. 파일이 있으면 그 장면에 들어설 때 그 묶음으로 갈아타고(1.2초 엇갈림),
///    없으면 기본 묶음을 그대로 이어 튼다. 곡을 넣으려면 Resources/MolakBgm/duel_00.mp3 처럼 이름만 맞춰 두면 된다.
/// 장면 판단: 타이틀(서장 전) · 비무 삽화(scene:duel) · 야습(scene:raid / bloodroof) · 말로 화면 — MolakArt·MolakEnd·MolakTitle 이 Mood() 를 부른다.
///
/// 음량의 기준은 나니노벨 설정의 BGM 음량(환경 설정의 배경음) × 마스터 음량. 처음 설치 때 배경음을 50%로 맞춘다.
/// 클립은 스트리밍 로드, 한 번에 한 토막만 들고 있는다.
/// </summary>
public class MolakBgm : MonoBehaviour
{
    public const float DefaultVolume = 0.5f;
    /// <summary>음원 자체 세기 보정(0.4.4: 테스트 곡이 커서 반으로) — 설정의 배경음 %는 그대로, 실제 소리만 줄인다.</summary>
    public const float Trim = 0.5f;
    const string InitKey = "molak_bgm_init1";
    static MolakBgm inst;
    AudioSource src;
    bool settingsReady, loading, started;
    string set = "title";               // 지금 틀고 있는 묶음(0.4.85 옛 시험곡 bgm_* 를 뺐다 — 처음엔 제목 곡)
    string mood = "";                   // 지금 장면
    readonly Dictionary<string, int> pos = new Dictionary<string, int>();   // 묶음별 마지막 토막(돌아오면 다음 곡부터)
    static readonly Dictionary<string, bool> has = new Dictionary<string, bool>();
    float fade = 1f;                    // 갈아탈 때 0으로 내렸다 올린다
    string pendingSet;
    float moodT;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot ()
    {
        if (inst) return;
        var go = new GameObject("MolakBgm"); DontDestroyOnLoad(go);
        inst = go.AddComponent<MolakBgm>();
        inst.src = go.AddComponent<AudioSource>();
        inst.src.playOnAwake = false; inst.src.spatialBlend = 0f; inst.src.loop = false;
        inst.src.priority = 0; inst.src.ignoreListenerPause = true;
        inst.src.volume = DefaultVolume * Trim;
        inst.Next();
    }

    static bool Has (string s)
    {
        bool b;
        if (has.TryGetValue(s, out b)) return b;
        // 존재 확인만: 스트리밍 클립이라 헤더만 읽는다
        var c = Resources.Load<AudioClip>("MolakBgm/" + s + "_00");
        b = c != null;
        if (c != null) Resources.UnloadAsset(c);
        has[s] = b; return b;
    }

    // ---- 0.4.94 대본이 쥐는 곡(@bgm mood:…) — 언메이커: 중요하고 멋있는 장면 연출, 너무 오바하지 말고 ----
    /// <summary>대본이 쥔 묶음. 쥐는 동안 그림이 바뀌어도(Commit→Mood) 곡은 그대로다. "hush" 는 소리를 거두고 멎는다.
    /// 기준: 쥔 곡은 그 장면 안에서만 — 날이 바뀌거나, 새 마디(@chapter)·말로(@ending)·제목 화면이 오면 저절로 풀린다.</summary>
    public static string Held { get; private set; }
    static float heldDay = -1f;
    public static void Hold (string m)
    {
        if (inst == null) return;
        if (string.IsNullOrEmpty(m) || m == "auto") { Release(); return; }
        Held = m; try { heldDay = MolakStats.Get("day"); } catch { heldDay = -1f; }
        if (m == "hush") return;
        string want = Has(m) ? m : Fallback(m);
        inst.mood = m;
        if (want != inst.set || inst.pendingSet != null) inst.pendingSet = want;
    }
    public static void Release ()
    {
        if (Held == null) return;
        Held = null;
        if (inst != null) { inst.mood = ""; Mood(MolakArt.CurrentMood()); }
    }

    /// <summary>장면이 바뀔 때. 그 장면 묶음이 있으면 갈아타고, 없으면 기본 묶음을 그대로 둔다.</summary>
    public static void Mood (string m)
    {
        if (inst == null) return;
        m = m ?? "";
        if (Held != null) return;   // 대본이 곡을 쥐고 있다
        if (inst.mood == m) return;
        inst.mood = m;
        string want = (m.Length > 0 && Has(m)) ? m : Fallback(m);
        if (want == inst.set && inst.pendingSet == null) return;
        inst.pendingSet = want;
    }

    /// <summary>0.4.70 묶음이 없으면 가까운 묶음으로: 장면 묶음 → 낮/밤 → 옛 기본 묶음.</summary>
    static string Fallback (string m)
    {
        string near = m == "fire" ? "tension" : m == "boss" ? "duel" : m == "heroine" ? "soa" : m == "rain" ? "night" : m == "sorrow" ? "night" : m == "prologue" ? "day"
                    : m == "ending" ? "true" : m == "true" ? "ending" : m == "raid" ? "fire" : "";
        if (near.Length > 0 && Has(near)) return near;
        if (m == "night" || m == "rain" || m == "sorrow") { if (Has("night")) return "night"; }
        if (Has("day")) return "day";
        return "title";
    }

    void Next ()
    {
        if (loading) return;
        int last; if (!pos.TryGetValue(set, out last)) last = -1;
        StartCoroutine(Load(set, last + 1));
    }

    System.Collections.IEnumerator Load (string s, int i)
    {
        loading = true;
        var req = Resources.LoadAsync<AudioClip>("MolakBgm/" + s + "_" + i.ToString("00"));
        yield return req;
        var clip = req.asset as AudioClip;
        if (clip == null && i != 0)
        {
            i = 0;   // 묶음 끝 → 처음으로
            req = Resources.LoadAsync<AudioClip>("MolakBgm/" + s + "_00");
            yield return req;
            clip = req.asset as AudioClip;
        }
        loading = false;
        if (clip == null) yield break;
        var old = src.clip;
        set = s; pos[s] = i;
        src.clip = clip;
        src.Play();
        started = false;
        if (old != null && old != clip) Resources.UnloadAsset(old);
    }

    /// <summary>현재 BGM 음량(0~1). 엔진이 뜨기 전엔 기본값.</summary>
    public static float Volume
    {
        get
        {
            if (!Naninovel.Engine.Initialized) return DefaultVolume;
            var am = Naninovel.Engine.GetService<Naninovel.IAudioManager>();
            return am != null ? am.BgmVolume : DefaultVolume;
        }
        set
        {
            if (!Naninovel.Engine.Initialized) return;
            var am = Naninovel.Engine.GetService<Naninovel.IAudioManager>();
            if (am == null) return;
            am.BgmVolume = Mathf.Clamp01(value);
            SaveSettings();
        }
    }

    static void SaveSettings ()
    {
        try { var st = Naninovel.Engine.GetService<Naninovel.IStateManager>(); if (st != null) st.SaveSettings(); }
        catch (System.Exception) { }
    }

    void Update ()
    {
        if (!Naninovel.Engine.Initialized) return;
        var am = Naninovel.Engine.GetService<Naninovel.IAudioManager>();
        if (am == null) return;
        if (!settingsReady)
        {
            settingsReady = true;
            // 처음 설치(또는 이 기능이 처음 들어간 판): 배경음을 반으로
            if (PlayerPrefs.GetInt(InitKey, 0) == 0)
            {
                PlayerPrefs.SetInt(InitKey, 1); PlayerPrefs.Save();
                am.BgmVolume = DefaultVolume;
                SaveSettings();
            }
        }
        // 타이틀(서장 전)과 본편의 경계
        if ((moodT -= Time.unscaledDeltaTime) <= 0f)
        {
            moodT = 0.5f;
            float day = 0f; try { day = MolakStats.Get("day"); } catch { }
            if (Held != null && (MolakTitle.Showing || day != heldDay)) Release();
            if (Held != null) { moodT = 0.5f; goto Fading; }
            if (MolakTitle.Showing) { if (mood != "end" && mood != "ending" && mood != "true") Mood("title"); }
            else if (day < 1f) { if (mood == "title" || mood == "") Mood("prologue"); }
            else if (mood == "title" || mood == "end" || mood == "ending" || mood == "true" || mood == "prologue") Mood(MolakArt.CurrentMood());
        }
        Fading:
        // 묶음 갈아타기: 소리를 내린 뒤 새 묶음의 다음 곡
        if (Held == "hush") fade = Mathf.MoveTowards(fade, 0f, Time.unscaledDeltaTime / 2.2f);   // 소리를 천천히 거둔다
        else if (pendingSet != null)
        {
            fade = Mathf.MoveTowards(fade, 0f, Time.unscaledDeltaTime / 1.2f);
            if (fade <= 0f && !loading)
            {
                int last; if (!pos.TryGetValue(pendingSet, out last)) last = -1;
                StartCoroutine(Load(pendingSet, last + 1));
                pendingSet = null;
            }
        }
        else
        {
            fade = Mathf.MoveTowards(fade, 1f, Time.unscaledDeltaTime / 1.2f);
            // 한 토막이 끝나면 다음 토막
            if (src.clip != null && !loading)
            {
                if (src.isPlaying) started = true;
                else if (started && Application.isFocused) Next();
            }
        }
        float v = Mathf.Clamp01(am.BgmVolume * am.MasterVolume) * fade * Trim;
        src.volume = Mathf.MoveTowards(src.volume, v, Time.unscaledDeltaTime * 2f);
    }
}
