using UnityEngine;

/// <summary>최소 효과음·진동 세트: 선택 탭(붓터치), 선택 확정(종이), 피해(북), 경지 상승(종), 말로(북 여운).</summary>
public class MolakSfx : MonoBehaviour
{
    static MolakSfx inst;
    AudioSource src;
    static readonly System.Collections.Generic.Dictionary<string, AudioClip> cache = new System.Collections.Generic.Dictionary<string, AudioClip>();
    public static bool Muted { get { return PlayerPrefs.GetInt("molak_sfx", 0) == 0; } set { PlayerPrefs.SetInt("molak_sfx", value ? 0 : 1); } }
    public static bool Haptic { get { return PlayerPrefs.GetInt("molak_haptic", 1) == 1; } set { PlayerPrefs.SetInt("molak_haptic", value ? 1 : 0); } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot ()
    {
        if (inst) return;
        var go = new GameObject("MolakSfx"); DontDestroyOnLoad(go);
        inst = go.AddComponent<MolakSfx>();
        inst.src = go.AddComponent<AudioSource>();
        inst.src.playOnAwake = false; inst.src.spatialBlend = 0f;
    }

    static AudioClip Clip (string name)
    {
        AudioClip c;
        if (cache.TryGetValue(name, out c)) return c;
        c = Resources.Load<AudioClip>("MolakSfx/" + name);
        cache[name] = c;
        return c;
    }

    public static void Play (string name, float vol = 1f, float pitch = 1f)
    {
        if (inst == null || Muted) return;
        var c = Clip(name); if (c == null) return;
        inst.src.pitch = pitch;
        inst.src.PlayOneShot(c, vol * SfxScale());
    }

    /// <summary>환경 설정 '효과음'·'전체 음량' 슬라이더(나니노벨 SFX·마스터 음량)를 따른다.</summary>
    static float SfxScale ()
    {
        if (!Naninovel.Engine.Initialized) return 1f;
        var am = Naninovel.Engine.GetService<Naninovel.IAudioManager>();
        return am != null ? am.SfxVolume * am.MasterVolume : 1f;
    }

    public static void Vibrate ()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (Haptic) Handheld.Vibrate();
#endif
    }

    public static void Tap ()     { Play("tap_brush", 0.9f, Random.Range(0.94f, 1.06f)); Vibrate(); }
    public static void Confirm () { Play("page_turn", 0.7f, Random.Range(0.96f, 1.04f)); }
    public static void Hit ()     { Play("hit_drum", 1f, Random.Range(0.95f, 1.05f)); Vibrate(); }
    public static void Bell ()    { Play("bell_rise", 0.8f); }
    public static void Death ()   { Play("death_drum", 1f); Vibrate(); }
    public static void Draw ()     { Play("sword_draw", 0.75f, Random.Range(0.97f, 1.03f)); }
    public static void Rewind ()   { Play("wind_rewind", 0.8f); }
    public static void TrueBell () { Play("true_bell", 1f); }
}
