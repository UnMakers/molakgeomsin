using UnityEngine;

/// <summary>최소 효과음·진동 세트: 선택 탭(붓터치), 선택 확정(종이), 피해(북), 경지 상승(종), 말로(북 여운).</summary>
public class MolakSfx : MonoBehaviour
{
    static MolakSfx inst;
    AudioSource src;
    static readonly System.Collections.Generic.Dictionary<string, AudioClip> cache = new System.Collections.Generic.Dictionary<string, AudioClip>();
    // 0.4.89 기본값이 '꺼짐'(0)이라 처음 설치한 기기에선 효과음 슬라이더를 한 번 움직이기 전까지 붓·인장·북이 하나도 나지 않았다 → 기본 '켜짐'.
    //  끄는 길은 환경 설정의 효과음 슬라이더(0이면 SfxScale 이 0)뿐이다.
    public static bool Muted { get { return PlayerPrefs.GetInt("molak_sfx", 1) == 0; } set { PlayerPrefs.SetInt("molak_sfx", value ? 0 : 1); } }
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

    public static void Play (string name, float vol = 1f, float pitch = 1f) { Try(name, vol, pitch); }

    /// <summary>있으면 틀고 true. 0.4.32: 새 효과음이 빠진 빌드에서도 옛 소리로 물러설 수 있게.</summary>
    public static bool Try (string name, float vol = 1f, float pitch = 1f)
    {
        if (inst == null || Muted) return true;
        var c = Clip(name); if (c == null) return false;
        inst.src.pitch = pitch;
        inst.src.PlayOneShot(c, vol * SfxScale());
        return true;
    }

    /// <summary>환경 설정 '효과음'·'전체 음량' 슬라이더(나니노벨 SFX·마스터 음량)를 따른다.</summary>
    static float SfxScale ()
    {
        if (!MolakVn.Engine.Initialized) return 1f;
        var am = MolakVn.Engine.GetService<MolakVn.IAudioManager>();
        return am != null ? am.SfxVolume * am.MasterVolume : 1f;
    }

    public static void Vibrate ()
    {
#if UNITY_ANDROID || UNITY_IOS
        if (Haptic) Handheld.Vibrate();
#endif
    }

    public static void Tap ()     { Play("tap_brush", 0.9f, Random.Range(0.94f, 1.06f)); Vibrate(); }
    public static float LastConfirm = -9f;
    public static void Confirm () { LastConfirm = Time.unscaledTime; if (!Try("ui_menu", 0.6f, Random.Range(0.97f, 1.03f))) Play("page_turn", 0.7f, Random.Range(0.96f, 1.04f)); }
    // 0.4.70 ComfyUI(Stable Audio Open)로 뽑은 소리. 파일이 없으면 옛 소리로 물러선다.
    public static void TitleBtn () { LastConfirm = Time.unscaledTime; if (!Try("ui_title", 0.75f)) Confirm(); }
    public static void Page ()    { Try("page_turn2", 0.38f, Random.Range(0.96f, 1.05f)); }
    public static void Chapter () { if (!Try("chapter_pluck", 0.7f)) Bell(); }
    public static void Dawn ()    { Try("day_rooster", 0.45f); }
    public static void Gloss ()   { Try("gloss_paper", 0.35f); }
    public static void Follow ()  { Try("autolife_breath", 0.5f); }
    public static void CardPlace () { if (!Try("card_place", 0.75f, Random.Range(0.95f, 1.05f))) Tap(); }
    public static void CardFlip () { Try("card_flip", 0.6f, Random.Range(0.95f, 1.05f)); }
    public static void Momentum () { Try("momentum_drum", 0.85f); }
    public static void Win ()     { Try("win_gong", 0.8f); }
    public static void Lose ()    { Try("lose_drum", 0.85f); }
    public static void Crowd ()   { Try("crowd_cheer", 0.45f); }
    public static void Frenzy ()  { Try("frenzy_whisper", 0.7f); }
    public static void Hit ()     { Play("hit_drum", 1f, Random.Range(0.95f, 1.05f)); Vibrate(); }
    public static void Bell ()    { Play("bell_rise", 0.8f); }
    public static void Death ()   { Play("death_drum", 1f); Vibrate(); }
    public static void Draw ()     { Play("sword_draw", 0.75f, Random.Range(0.97f, 1.03f)); }
    public static void Rewind ()   { Play("wind_rewind", 0.8f); }
    public static void TrueBell () { Play("true_bell", 1f); }
    // 0.4.32 소리 점검: 칼끼리 맞는 쇳소리, 인장 찍는 소리, 몸이 바닥일 때의 심장, 사술이 숨 쉬는 소리
    public static void Clash (bool heavy) { if (heavy && Try("sword_clash_heavy", 0.85f, Random.Range(0.96f, 1.02f))) return; if (!Try("sword_clash", heavy ? 0.9f : 0.6f, Random.Range(heavy ? 0.92f : 1.02f, heavy ? 1f : 1.1f))) Draw(); }
    public static void Stamp ()   { if (!Try("seal_stamp", 0.85f, Random.Range(0.95f, 1.05f))) Play("tap_brush", 0.8f, 0.82f); }

    // 0.4.89 인장 소리 한 벌: 붓이 원을 긋고(0~0.5초) → 돌도장이 내려앉고(SealHit) → 인장 빛깔대로 여운이 남는다.
    //  · 어떤 여운인지는 MolakSeal.KindOf(한자)가 정한다 — 파일 이름 seal_<종류>(good·bad·know·skill·fork). 부르는 곳은 한자만 넘긴다.
    //  · brush=false 는 원이 이미 그려진 뒤에 부르는 곳: 찍히는 자리부터 튼다.
    //  · 파일이 없는 빌드에선 옛 도장 소리로 물러선다.
    public const float SealHit = 0.8f;
    AudioSource sealSrc;
    public static void Seal (string hanja, bool big = false, bool brush = true)
    {
        if (inst == null || Muted) return;
        var c = Clip("seal_" + MolakSeal.KindOf(hanja ?? "").ToString().ToLowerInvariant());
        if (c == null) { Stamp(); return; }
        if (inst.sealSrc == null) { inst.sealSrc = inst.gameObject.AddComponent<AudioSource>(); inst.sealSrc.playOnAwake = false; inst.sealSrc.spatialBlend = 0f; }
        var s = inst.sealSrc;   // 인장은 한 번에 하나 — 새 인장이 찍히면 앞의 여운을 끊는다
        s.Stop(); s.clip = c; s.pitch = big ? 0.96f : 1f;
        s.volume = (big ? 1f : 0.85f) * SfxScale();
        s.time = brush ? 0f : Mathf.Min(c.length - 0.05f, Mathf.Max(0f, SealHit - 0.03f));
        s.Play();
        if (big) Vibrate();
    }
    public static void Heart ()   { Try("heartbeat", 0.9f); }
    public static void CurseBreath () { Try("curse_breath", 0.7f); }
}
