using Naninovel;
using UnityEngine;

/// <summary>장면 틴트·화면 이펙트 — @scene / @vfx 명령 + C# 훅.</summary>
public class MolakVfx : MonoBehaviour
{
    static MolakVfx instance;
    static Texture2D px;

    // 플래시 상태
    float flashT = -1f; Color flashC; float flashDur = 0.5f;
    // 흔들림
    float shakeT = -1f; float shakeAmp;
    // 날씨(0.4.9): rain · bloodrain · snow · petals · embers — 한 번에 하나
    bool rain; float[] rx; float[] ry; float[] rs; float[] rr;
    public static string WeatherKind = "";
    static Texture2D dot;
    // 장면 틴트 목표
    static Color tintTarget = new Color(0.97f, 0.95f, 0.92f, 1f);
    Color tintCur = tintTarget;

    public static readonly System.Collections.Generic.Dictionary<string, Color> Scenes =
        new System.Collections.Generic.Dictionary<string, Color> {
        { "day",    new Color(0.97f, 0.95f, 0.92f, 1f) },
        { "night",  new Color(0.80f, 0.81f, 0.88f, 1f) },
        { "dusk",   new Color(0.93f, 0.82f, 0.68f, 1f) },
        { "shrine", new Color(0.90f, 0.87f, 0.80f, 1f) },
        { "valley", new Color(0.84f, 0.87f, 0.88f, 1f) },
        { "blood",  new Color(0.92f, 0.78f, 0.74f, 1f) },
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap ()
    {
        if (instance != null) return;
        var go = new GameObject("MolakVfx");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<MolakVfx>();
    }

    static Texture2D Px { get { if (px == null) { px = new Texture2D(1,1); px.SetPixel(0,0,Color.white); px.Apply(); } return px; } }
    void Box (Rect r, Color c) { var g = GUI.color; GUI.color = c; GUI.DrawTexture(r, Px); GUI.color = g; }

    public static void Scene (string id)
    {
        Color c;
        if (Scenes.TryGetValue(id, out c)) tintTarget = c;
    }
    public static void Flash (Color c, float dur) { if (instance != null) { instance.flashC = c; instance.flashDur = dur; instance.flashT = 0f; } }
    public static void Blood () { Flash(new Color(0.62f, 0.08f, 0.05f, 0.5f), 0.7f); Shake(14f); }
    public static void Gold () { Flash(new Color(0.85f, 0.68f, 0.18f, 0.32f), 0.6f); }
    public static void CursePulse () { Flash(new Color(0.42f, 0.22f, 0.58f, 0.4f), 0.9f); }
    public static void Shake (float amp) { if (instance != null) { instance.shakeAmp = amp; instance.shakeT = 0f; } }
    public static void Rain (bool on) { Weather(on ? "rain" : ""); }
    /// <summary>날씨: "rain" 비, "bloodrain" 핏빛 비, "snow" 눈, "petals" 꽃잎, "embers" 불티, "" 갬</summary>
    public static void Weather (string kind)
    {
        if (instance == null) return;
        kind = kind ?? "";
        if (kind == WeatherKind) return;
        WeatherKind = kind;
        instance.SetRain(kind.Length > 0);
    }
    static Texture2D Dot
    {
        get
        {
            if (dot != null) return dot;
            int S = 32; dot = new Texture2D(S, S, TextureFormat.RGBA32, false); dot.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float dx = (x + 0.5f) / S - 0.5f, dy = (y + 0.5f) / S - 0.5f;
                float d = Mathf.Sqrt(dx*dx + dy*dy) * 2f;
                dot.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((1f - d) * 2.2f)));
            }
            dot.Apply(); return dot;
        }
    }

    void SetRain (bool on)
    {
        rain = on;
        if (on && rx == null)
        {
            int n = 90;
            rx = new float[n]; ry = new float[n]; rs = new float[n]; rr = new float[n];
            for (int i = 0; i < n; i++) { rx[i] = Random.value; ry[i] = Random.value; rs[i] = 0.7f + Random.value*0.8f; rr[i] = Random.value * 360f; }
        }
    }

    void Update ()
    {
        // 장면 틴트를 배경 스프라이트에 서서히 적용
        var tt = tintTarget; if (MolakTheme.Night) tt = new Color(tt.r*0.45f, tt.g*0.45f, tt.b*0.45f, 1f);
        tintCur = Color.Lerp(tintCur, tt, Time.deltaTime * 2.2f);
        var bd = GameObject.Find("MolakBackdrop");
        if (bd != null)
        {
            var sr = bd.GetComponent<SpriteRenderer>();
            if (sr != null && sr.color != tintCur) sr.color = tintCur;
        }
        // 흔들림(카메라)
        if (shakeT >= 0f)
        {
            shakeT += Time.deltaTime;
            var cam = Camera.main;
            if (cam != null)
            {
                float k = Mathf.Max(0f, 1f - shakeT / 0.45f);
                if (k <= 0f) { shakeT = -1f; }
                var off = Random.insideUnitCircle * (shakeAmp * 0.01f) * k;
                cam.transform.position = new Vector3(off.x, off.y, cam.transform.position.z);
            }
        }
    }

    void OnGUI ()
    {
        float w = Screen.width, h = Screen.height;
        // 플래시(비네트 느낌: 중앙은 옅고 가장자리 진하게 — 상/하/좌/우 밴드)
        if (flashT >= 0f)
        {
            flashT += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(flashT / flashDur);
            if (k <= 0f) flashT = -1f;
            else
            {
                var c = new Color(flashC.r, flashC.g, flashC.b, flashC.a * k);
                float band = h * 0.22f;
                Box(new Rect(0, 0, w, band), c);
                Box(new Rect(0, h - band, w, band), c);
                Box(new Rect(0, band, w * 0.10f, h - 2*band), c);
                Box(new Rect(w * 0.90f, band, w * 0.10f, h - 2*band), c);
                var cc = new Color(c.r, c.g, c.b, c.a * 0.45f);
                Box(new Rect(0, 0, w, h), cc);
            }
        }
        // 날씨
        if (rain && rx != null && Event.current.type == EventType.Repaint)
        {
            float u = w / 1080f, dt = Time.deltaTime, t = Time.time;
            string k = WeatherKind;
            int n = k == "snow" ? 80 : k == "petals" ? 34 : k == "embers" ? 40 : k == "bloodrain" ? 90 : 70;
            for (int i = 0; i < n && i < rx.Length; i++)
            {
                if (k == "rain" || k == "bloodrain")
                {
                    float sp = k == "bloodrain" ? 1.25f : 1.1f;
                    ry[i] += dt * sp * rs[i]; rx[i] -= dt * 0.12f * rs[i];
                    if (ry[i] > 1f) { ry[i] -= 1.1f; rx[i] = Random.value * 1.2f; }
                    var rc = k == "bloodrain" ? new Color(0.55f, 0.06f, 0.05f, 0.42f) : new Color(0.35f, 0.38f, 0.45f, 0.35f);
                    Box(new Rect(rx[i] * w, ry[i] * h, 2f * u * (k == "bloodrain" ? 1.3f : 1f), 34f * rs[i] * u), rc);
                }
                else if (k == "snow")
                {
                    ry[i] += dt * 0.07f * rs[i]; rx[i] += Mathf.Sin(t * 0.8f + i) * dt * 0.02f;
                    if (ry[i] > 1.02f) { ry[i] = -0.03f; rx[i] = Random.value; }
                    float sz = (7f + 7f * rs[i]) * u;
                    var g = GUI.color; GUI.color = new Color(1f, 1f, 1f, 0.85f);
                    GUI.DrawTexture(new Rect(rx[i] * w, ry[i] * h, sz, sz), Dot); GUI.color = g;
                }
                else if (k == "petals")
                {
                    ry[i] += dt * 0.06f * rs[i]; rx[i] += (Mathf.Sin(t * 1.1f + i * 1.7f) * 0.03f - 0.012f) * dt;
                    rr[i] += dt * 70f * rs[i];
                    if (ry[i] > 1.03f) { ry[i] = -0.04f; rx[i] = Random.value * 1.1f; }
                    float sw = 18f * u * rs[i], sh = 10f * u * rs[i];
                    var piv = new Vector2(rx[i] * w, ry[i] * h);
                    var m = GUI.matrix; GUIUtility.RotateAroundPivot(rr[i], piv);
                    var g = GUI.color; GUI.color = new Color(0.94f, 0.62f, 0.70f, 0.85f);
                    GUI.DrawTexture(new Rect(piv.x - sw/2f, piv.y - sh/2f, sw, sh), Dot); GUI.color = g; GUI.matrix = m;
                }
                else if (k == "embers")
                {
                    ry[i] -= dt * 0.09f * rs[i]; rx[i] += Mathf.Sin(t * 2f + i) * dt * 0.03f;
                    if (ry[i] < -0.03f) { ry[i] = 1.02f; rx[i] = Random.value; }
                    float sz = (4f + 5f * rs[i]) * u, fl = 0.55f + 0.45f * Mathf.Sin(t * 9f + i * 3f);
                    var g = GUI.color; GUI.color = new Color(0.95f, 0.45f, 0.12f, 0.75f * fl);
                    GUI.DrawTexture(new Rect(rx[i] * w, ry[i] * h, sz, sz), Dot); GUI.color = g;
                }
            }
        }
    }
}
