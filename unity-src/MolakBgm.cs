using UnityEngine;

/// <summary>
/// 배경음 — 앱이 켜지는 순간부터 끝까지 Resources/MolakBgm/bgm_00~ 을 차례로 틀고, 끝나면 처음으로 돌아간다.
/// (원본은 2시간 51분짜리 한 파일 — 유니티 임포터가 메모리 부족으로 못 받아서 곡 사이 무음 지점에서 13토막으로 잘랐다)
/// 나니노벨 상태(저장 불러오기·처음으로)와 무관하게 DontDestroyOnLoad 오브젝트에서 재생.
/// 음량의 기준은 나니노벨 설정의 BGM 음량(환경 설정 슬라이더) × 마스터 음량. 처음 설치 때 BGM 음량을 50%로 맞춘다.
/// 클립은 스트리밍 로드, 한 번에 한 토막만 들고 있는다.
/// </summary>
public class MolakBgm : MonoBehaviour
{
    public const float DefaultVolume = 0.5f;
    const string InitKey = "molak_bgm_init1";
    static MolakBgm inst;
    AudioSource src;
    bool settingsReady, loading, started;
    int index = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot ()
    {
        if (inst) return;
        var go = new GameObject("MolakBgm"); DontDestroyOnLoad(go);
        inst = go.AddComponent<MolakBgm>();
        inst.src = go.AddComponent<AudioSource>();
        inst.src.playOnAwake = false; inst.src.spatialBlend = 0f;
        inst.src.priority = 0; inst.src.ignoreListenerPause = true;
        inst.src.volume = DefaultVolume;
        inst.src.loop = false;
        inst.Next();
    }

    void Next ()
    {
        if (loading) return;
        StartCoroutine(Load(index + 1));
    }

    System.Collections.IEnumerator Load (int i)
    {
        loading = true;
        var req = Resources.LoadAsync<AudioClip>("MolakBgm/bgm_" + i.ToString("00"));
        yield return req;
        var clip = req.asset as AudioClip;
        if (clip == null && i != 0)
        {
            i = 0;   // 마지막 토막 다음 → 처음으로
            req = Resources.LoadAsync<AudioClip>("MolakBgm/bgm_00");
            yield return req;
            clip = req.asset as AudioClip;
        }
        loading = false;
        if (clip == null) yield break;
        var old = src.clip;
        index = i;
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
        // 한 토막이 끝나면 다음 토막
        if (src.clip != null && !loading)
        {
            if (src.isPlaying) started = true;
            else if (started && Application.isFocused) Next();
        }
        float v = Mathf.Clamp01(am.BgmVolume * am.MasterVolume);
        src.volume = Mathf.MoveTowards(src.volume, v, Time.unscaledDeltaTime * 2f);
    }
}
