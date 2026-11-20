using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 0.4.70 장면에 깔리는 소리(언메이커: 그림만 바뀌고 소리는 없다). 장면 그림이 바뀌면 그 자리의 소리로 1.5초에 걸쳐 갈아탄다.
/// 파일: Resources/MolakSfx/amb_*. 없으면 조용히 넘어간다. 음량은 효과음 설정을 따르고, 배경음보다 낮게 깐다.
/// 비·눈 같은 날씨가 장면보다 먼저다.
/// </summary>
public class MolakAmbience : MonoBehaviour
{
    static MolakAmbience inst;
    AudioSource a, b;   // 갈아탈 때 두 개를 엇갈려 쓴다
    string cur = "", want = "";
    float mix = 1f;     // 0 = a, 1 = b 쪽이 소리 남
    bool useB;
    const float Level = 0.32f;

    static readonly Dictionary<string, string> Map = new Dictionary<string, string>
    {
        {"rain","amb_rain"},{"flood","amb_rain"},{"soanight","amb_rain"},
        {"manorfire","amb_fire"},{"herbfire","amb_fire"},{"raid","amb_fire"},{"bloodroof","amb_fire"},
        {"raidnight","amb_roof"},{"stairs","amb_roof"},
        {"market","amb_market"},{"nightmarket","amb_market"},{"beggars","amb_market"},{"village","amb_market"},
        {"night","amb_night"},{"roofmoon","amb_night"},{"well","amb_night"},{"hut","amb_night"},{"camp","amb_night"},
        {"ferry","amb_river"},{"falls","amb_river"},
        {"temple","amb_temple"},{"templeyard","amb_temple"},{"shrine","amb_temple"},{"gate","amb_temple"},
        {"innhall","amb_tavern"},{"teahouse","amb_tavern"},{"cup","amb_tavern"},
        {"pass","amb_snow"},
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot ()
    {
        if (inst) return;
        var go = new GameObject("MolakAmbience"); DontDestroyOnLoad(go);
        inst = go.AddComponent<MolakAmbience>();
        inst.a = Make(go); inst.b = Make(go);
    }
    static AudioSource Make (GameObject go)
    {
        var s = go.AddComponent<AudioSource>(); s.playOnAwake = false; s.loop = true; s.spatialBlend = 0f; s.volume = 0f; s.priority = 64; return s;
    }

    string weather = "", scene = "";
    public static void Scene (string key) { if (inst == null) return; inst.scene = key ?? ""; inst.Pick(); }
    public static void Weather (string kind) { if (inst == null) return; inst.weather = kind ?? ""; inst.Pick(); }

    void Pick ()
    {
        string w = weather == "rain" || weather == "bloodrain" ? "amb_rain" : weather == "snow" ? "amb_snow" : weather == "inferno" || weather == "embers" ? "amb_fire" : "";
        string s; if (!Map.TryGetValue(scene, out s)) s = "";
        if (scene.StartsWith("duel")) s = "";   // 비무 중엔 함성·쇳소리만
        want = w.Length > 0 ? w : s;
    }

    void Update ()
    {
        if (want != cur)
        {
            cur = want;
            var next = useB ? a : b; useB = !useB;
            next.Stop(); next.clip = null;
            if (cur.Length > 0) StartCoroutine(LoadPlay(next, cur));   // 0.4.85 그 자리에서 읽으면(동기) 장면이 바뀌는 프레임이 멈춰 먹번짐이 끊겨 보였다 — 뒤에서 읽는다
            mix = 0f;
        }
        mix = Mathf.MoveTowards(mix, 1f, Time.unscaledDeltaTime / 1.5f);
        float vol = Level * Scale();
        bool muted = MolakSfx.Muted || MolakTitle.Showing;
        var on = useB ? b : a; var off = useB ? a : b;
        on.volume = muted ? 0f : vol * mix;
        off.volume = muted ? 0f : vol * (1f - mix);
        if (mix >= 1f && off.isPlaying) off.Stop();
    }

    static readonly System.Collections.Generic.Dictionary<string, AudioClip> clips = new System.Collections.Generic.Dictionary<string, AudioClip>();
    System.Collections.IEnumerator LoadPlay (AudioSource src, string name)
    {
        AudioClip c;
        if (!clips.TryGetValue(name, out c) || c == null)
        {
            var req = Resources.LoadAsync<AudioClip>("MolakSfx/" + name);
            yield return req;
            c = req.asset as AudioClip; if (c != null) clips[name] = c;
        }
        if (c == null || cur != name) yield break;   // 읽는 사이에 다른 자리로 바뀌었으면 버린다
        if (c.loadState == AudioDataLoadState.Unloaded) c.LoadAudioData();
        while (c.loadState == AudioDataLoadState.Loading) yield return null;
        if (cur != name) yield break;
        src.clip = c; src.time = Random.Range(0f, Mathf.Max(0f, c.length - 1f)); src.Play();
    }

    static float Scale ()
    {
        if (!Naninovel.Engine.Initialized) return 1f;
        var am = Naninovel.Engine.GetService<Naninovel.IAudioManager>();
        return am != null ? am.SfxVolume * am.MasterVolume : 1f;
    }
}
