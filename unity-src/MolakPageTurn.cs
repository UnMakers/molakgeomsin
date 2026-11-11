using UnityEngine;

/// <summary>
/// 책장 넘김 — 본문이 비워질 때마다(새 쪽) 한 장을 넘긴다.
/// 앞 장이 오른쪽 끝에서 들려 왼쪽으로 넘어간다: 넘어가는 종이(뒷면은 조금 밝게), 접히는 모서리의 하이라이트,
/// 새 쪽 위로 드리우는 그림자. 0.5초, 종이 넘기는 소리. 화면 위 상태줄·아래 행낭 띠는 가리지 않는다.
/// </summary>
public class MolakPageTurn : MonoBehaviour
{
    static MolakPageTurn inst;
    float t = -1f, lastPlay = -9f;
    const float Dur = 0.52f;
    static Texture2D shade, curl;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot ()
    {
        if (inst) return;
        var go = new GameObject("MolakPageTurn"); DontDestroyOnLoad(go);
        inst = go.AddComponent<MolakPageTurn>();
    }

    public static void Play ()
    {
        if (inst == null) return;
        if (Time.unscaledTime - inst.lastPlay < 0.35f) return;      // 연달아 비워질 때는 한 번만
        try { var pl = Naninovel.Engine.GetService<Naninovel.IScriptPlayer>(); if (pl != null && pl.Skipping) return; } catch { }
        inst.lastPlay = Time.unscaledTime;
        inst.t = 0f;
        if (Time.unscaledTime - MolakSfx.LastConfirm > 0.5f) MolakSfx.Play("page_turn", 0.55f, Random.Range(0.95f, 1.06f));   // 선택지를 고른 소리와 겹치지 않게
    }

    static void Ensure ()
    {
        if (shade != null) return;
        // 가로 그라데이션: 왼쪽(넘어가는 종이 가장자리) 진하게 → 오른쪽 투명
        shade = new Texture2D(64, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int i = 0; i < 64; i++) { float k = i / 63f; shade.SetPixel(i, 0, new Color(0, 0, 0, Mathf.Pow(1f - k, 2.2f))); }
        shade.Apply();
        // 접힌 모서리: 가운데가 밝고 양끝이 어두운 원통 느낌
        curl = new Texture2D(64, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int i = 0; i < 64; i++)
        {
            float k = i / 63f;
            float light = Mathf.Sin(k * Mathf.PI);          // 0→1→0
            float v = Mathf.Lerp(0.78f, 1.06f, light);
            curl.SetPixel(i, 0, new Color(v, v, v, 1f));
        }
        curl.Apply();
    }

    void Update () { if (t >= 0f) { t += Time.unscaledDeltaTime; if (t > Dur) t = -1f; } }

    void OnGUI ()
    {
        if (t < 0f) return;
        Ensure();
        GUI.depth = -50;
        float u = MolakTheme.U, w = Screen.width, h = Screen.height;
        float top = 170f * u, bottom = h - 120f * u;     // 상태줄 아래 ~ 행낭 띠 위
        if (MolakArt.BandActive) top = Mathf.Max(top, (MolakArt.BandTop + MolakArt.BandH) * u * 1.0f);
        float k = Mathf.Clamp01(t / Dur);
        float e = k * k * (3f - 2f * k);                 // 부드럽게 출발·도착
        bool night = MolakTheme.Night;
        Color paper = night ? new Color(0.12f, 0.105f, 0.09f, 1f) : new Color(0.94f, 0.91f, 0.85f, 1f);
        Color back = night ? new Color(0.16f, 0.14f, 0.12f, 1f) : new Color(0.97f, 0.95f, 0.90f, 1f);

        // 앞 장의 오른쪽 끝(접히는 선)이 오른쪽 → 왼쪽으로. 넘어간 부분은 뒷면이 되어 접힌 선 왼쪽에 눕는다.
        float fold = Mathf.Lerp(w, 0f, e);
        float flipW = Mathf.Min(fold, (w - fold) * 0.55f + 10f * u);   // 넘어오는 뒷면 폭(원근으로 접혀 보이는 만큼)
        float ph = bottom - top;
        var g = GUI.color;
        float fade = k > 0.8f ? (1f - k) / 0.2f : 1f;

        // 1) 아직 안 넘어간 앞 장(빈 종이 — 옛 글은 이미 비워짐): 0 ~ fold
        GUI.color = new Color(paper.r, paper.g, paper.b, 0.96f * fade);
        GUI.DrawTexture(new Rect(0, top, fold, ph), Texture2D.whiteTexture);
        // 앞 장 위의 옛 줄 흔적(희미한 먹 줄 몇 개) — 넘기는 것이 '글이 있던 쪽'임을 보인다
        var ink = night ? new Color(0.9f, 0.86f, 0.78f, 0.10f * fade) : new Color(0.2f, 0.16f, 0.12f, 0.10f * fade);
        GUI.color = ink;
        for (int i = 0; i < 9; i++)
        {
            float ly = top + 60f * u + i * 92f * u; if (ly > bottom - 40f * u) break;
            float lw = Mathf.Min(fold - 90f * u, (w - 180f * u) * (i % 3 == 2 ? 0.62f : 0.94f));
            if (lw > 0) GUI.DrawTexture(new Rect(90f * u, ly, lw, 6f * u), Texture2D.whiteTexture);
        }
        // 2) 접히는 선 오른쪽(새 쪽) 위로 드리우는 그림자
        GUI.color = new Color(1, 1, 1, (night ? 0.55f : 0.32f) * fade * (1f - e * 0.6f));
        GUI.DrawTexture(new Rect(fold, top, 90f * u, ph), shade);
        // 3) 넘어가는 뒷면: 접히는 선에서 왼쪽으로 flipW 만큼, 원통처럼 밝게
        if (flipW > 1f)
        {
            GUI.color = new Color(back.r, back.g, back.b, 0.98f * fade);
            GUI.DrawTexture(new Rect(fold - flipW, top, flipW, ph), Texture2D.whiteTexture);
            GUI.color = new Color(back.r, back.g, back.b, 0.9f * fade);
            GUI.DrawTexture(new Rect(fold - flipW, top, flipW, ph), curl);
            // 뒷면 왼쪽 끝이 앞 장 위에 드리우는 얇은 그림자
            GUI.color = new Color(1, 1, 1, (night ? 0.45f : 0.22f) * fade);
            var m = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(-1f, 1f), new Vector2(fold - flipW, top));
            GUI.DrawTexture(new Rect(fold - flipW, top, 40f * u, ph), shade);
            GUI.matrix = m;
        }
        GUI.color = g;
    }
}
