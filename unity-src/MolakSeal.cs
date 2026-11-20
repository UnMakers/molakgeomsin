using UnityEngine;

/// <summary>
/// 0.4.85 인장(언메이커: 아이콘이 짜치다 → 시안 3 「먹 원 · 붉은 획」, 인장 전부 이 결로, 상황에 따라 빛이 달라도 된다).
/// 붓으로 원(원상 — 먹빛 바탕에선 흰 붓, 종이 바탕에선 검은 먹)을 먼저 긋고(0.5초), 그 안에 한 글자가 찍힌다(0.45초, 크게 → 제자리). 글자는 인주가 덜 묻은 듯 군데군데 빈다.
/// 빛: 좋은 판정(通·破·勢·先·勝) 주홍, 나쁜 판정(失·傷·敗) 검붉게 식고 원이 끊기며 글자 아래로 핏물이 흐른다(시안 4에서), 앎(憶·醒) 보랏빛, 초식(觀·步·落·絶) 금빛, 경지(境) 금빛, 갈림(岐) 주홍.
/// IMGUI 에서 쓴다: Draw(box, 글자, 지난 시간, 투명도, 글꼴, 바탕빛).
/// </summary>
public static class MolakSeal
{
    public enum Kind { Good, Bad, Know, Skill, Fork }
    public static Kind KindOf (string h)
    {
        switch (h)
        {
            case "失": case "傷": case "敗": return Kind.Bad;
            case "憶": case "醒": return Kind.Know;
            case "觀": case "步": case "落": case "絶": case "境": return Kind.Skill;
            case "岐": return Kind.Fork;
            default: return Kind.Good;
        }
    }
    public static Color ColorOf (Kind k)
    {
        switch (k)
        {
            case Kind.Bad: return new Color(0.66f, 0.09f, 0.09f, 1f);    // 식은 피(먹빛 바탕에서도 읽히게)
            case Kind.Know: return new Color(0.62f, 0.42f, 0.86f, 1f);   // 지난 생의 앎(보랏빛)
            case Kind.Skill: return new Color(0.86f, 0.68f, 0.30f, 1f);  // 금빛
            default: return new Color(0.80f, 0.22f, 0.15f, 1f);          // 주홍
        }
    }
    /// <summary>원이 다 그어지고 글자가 찍히는 데 걸리는 시간(초)</summary>
    public const float Dur = 0.95f;

    static Texture2D dot, speck;
    static Texture2D Dot
    {
        get
        {
            if (dot != null) return dot;
            const int N = 32; dot = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                float dx = (x + 0.5f) / N * 2f - 1f, dy = (y + 0.5f) / N * 2f - 1f, r = Mathf.Sqrt(dx * dx + dy * dy);
                byte a = (byte)(255 * Mathf.Clamp01((1f - r) * 6f));   // 가장자리만 살짝 부드럽게
                px[y * N + x] = new Color32(255, 255, 255, a);
            }
            dot.SetPixels32(px); dot.Apply(); return dot;
        }
    }
    /// <summary>인주가 덜 묻은 자리(바탕빛으로 덮는 얼룩) — 알파가 곧 빠진 정도</summary>
    static Texture2D Speck
    {
        get
        {
            if (speck != null) return speck;
            const int N = 128; speck = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.DontSave };
            var px = new Color32[N * N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                float g = Mathf.PerlinNoise(x * 0.09f + 3.3f, y * 0.09f + 1.7f) * 0.6f + Mathf.PerlinNoise(x * 0.31f + 8f, y * 0.31f) * 0.4f;
                float a = Mathf.Clamp01((g - 0.6f) * 6f);
                px[y * N + x] = new Color32(255, 255, 255, (byte)(255 * a));
            }
            speck.SetPixels32(px); speck.Apply(); return speck;
        }
    }

    static GUIStyle sGlyph; static int glyphSize = -1; static Font glyphFont;
    static float Hash (int seed, int i) { uint h = (uint)(seed * 73856093) ^ (uint)(i * 19349663); h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15; return (h & 0xFFFF) / 65535f; }

    /// <param name="box">인장 자리(정사각)</param>
    /// <param name="t">찍기 시작한 뒤 지난 시간(초). Dur 를 넘으면 다 찍힌 모습</param>
    /// <param name="back">인장 뒤 바탕빛(인주 빠짐을 이 빛으로 덮는다)</param>
    public static void Draw (Rect box, string hanja, float t, float alpha, Font font, Color back, bool paper = false)
    {
        if (Event.current == null || Event.current.type != EventType.Repaint || alpha <= 0.001f || string.IsNullOrEmpty(hanja)) return;
        var kind = KindOf(hanja); var col = ColorOf(kind); bool broken = kind == Kind.Bad;
        int seed = hanja[0];
        float S = box.width; var c = box.center; var g0 = GUI.color;
        // ① 먹 원: 붓머리에서 굵고 끝으로 갈수록 가늘고 옅게, 끝에서 먹이 튄다
        float p = Mathf.Clamp01(t / 0.5f);
        float R = S * 0.36f, start = -2.2f + Hash(seed, 1) * 0.3f, len = (broken ? 1.55f : 1.88f) * Mathf.PI * p;
        const int Dabs = 150;
        int nD = Mathf.CeilToInt(Dabs * p);
        // 먹빛 바탕에선 검은 먹 원이 바탕에 묻혀 안 보였다(언메이커) → 제목 화면의 원상처럼 흰 붓으로. 종이 바탕은 검은 먹.
        var ink = paper ? new Color(0.09f, 0.07f, 0.06f, 1f) : new Color(0.82f, 0.78f, 0.70f, 1f);
        for (int k = 0; k < nD; k++)
        {
            float tk = k / (float)(Dabs - 1), ang = start + (broken ? 1.55f : 1.88f) * Mathf.PI * tk;
            float wid = S * (0.055f * Mathf.Sin(Mathf.PI * Mathf.Min(1f, tk * 1.15f)) + 0.012f);
            float x = c.x + Mathf.Cos(ang) * R * (1f + 0.03f * Mathf.Sin(tk * 9f)), y = c.y + Mathf.Sin(ang) * R * (1f + 0.03f * Mathf.Cos(tk * 7f));
            GUI.color = new Color(ink.r, ink.g, ink.b, (paper ? 0.92f - 0.25f * tk : 0.78f - 0.38f * tk) * alpha);   // 흰 붓은 끝으로 갈수록 마른다
            GUI.DrawTexture(new Rect(x - wid, y - wid, wid * 2f, wid * 2f), Dot);
            if (tk > 0.75f && (k & 1) == 0)
                for (int q = 0; q < 2; q++)
                {
                    float r0 = wid * 0.28f * Hash(seed, k * 7 + q), ox = (Hash(seed, k * 3 + q) - 0.5f) * wid * 3f, oy = (Hash(seed, k * 5 + q) - 0.5f) * wid * 3f;
                    GUI.color = new Color(ink.r, ink.g, ink.b, (paper ? 0.5f : 0.35f) * alpha); GUI.DrawTexture(new Rect(x + ox - r0, y + oy - r0, r0 * 2f, r0 * 2f), Dot);
                }
        }
        // ② 글자: 원이 거의 다 그어질 때 위에서 눌러 찍힌다
        float qg = Mathf.Clamp01((t - 0.45f) / 0.45f);
        if (qg > 0f)
        {
            int fs = Mathf.RoundToInt(S * 0.46f);
            if (sGlyph == null || glyphSize != fs || glyphFont != font)
            {
                glyphSize = fs; glyphFont = font;
                sGlyph = new GUIStyle(GUI.skin.label) { fontSize = fs, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, clipping = TextClipping.Overflow };
                if (font != null) sGlyph.font = font;
            }
            float e = 1f + 0.35f * Mathf.Pow(1f - qg, 3f), ga = Mathf.Min(1f, qg * 2.2f) * alpha;
            var m = GUI.matrix; GUIUtility.ScaleAroundPivot(new Vector2(e, e), c); GUIUtility.RotateAroundPivot(-2.5f, c);
            var gr = new Rect(c.x - S * 0.32f, c.y - S * 0.32f + S * 0.01f, S * 0.64f, S * 0.64f);
            GUI.color = Color.white;   // 원을 그리던 먹빛이 글자에 곱해져 글자가 검게 나오던 것
            MolakGui.Ink(sGlyph, new Color(col.r, col.g, col.b, ga));
            GUI.Label(gr, hanja, sGlyph);
            // 인주 빠짐: 글자 위에 바탕빛 얼룩(글자 밖은 바탕과 같은 빛이라 보이지 않는다)
            GUI.color = new Color(back.r, back.g, back.b, 0.85f * ga);
            float off = Hash(seed, 9);
            GUI.DrawTextureWithTexCoords(new Rect(c.x - S * 0.26f, c.y - S * 0.26f, S * 0.52f, S * 0.52f), Speck, new Rect(off, off * 0.7f, 0.9f, 0.9f));
            GUI.matrix = m;
            // 찍히는 순간 인주가 튄다
            if (qg >= 1f || t > 0.9f)
            {
                GUI.color = new Color(col.r, col.g, col.b, 0.7f * alpha);
                for (int i = 0; i < 9; i++)
                {
                    float ang = Hash(seed, 40 + i) * Mathf.PI * 2f, d = S * (0.27f + 0.08f * Hash(seed, 60 + i)), r0 = S * 0.008f * (1f + 2f * Hash(seed, 80 + i));
                    GUI.DrawTexture(new Rect(c.x + Mathf.Cos(ang) * d - r0, c.y + Mathf.Sin(ang) * d - r0, r0 * 2f, r0 * 2f), Dot);
                }
            }
        }
        // ③ 나쁜 판정: 글자 아래로 핏물이 흘러내린다(시안 4)
        if (kind == Kind.Bad && t > 0.9f)
        {
            float gr = Mathf.Clamp01((t - 0.9f) / 0.9f); gr = 1f - (1f - gr) * (1f - gr);
            float[] xs = { -0.14f, 0.02f, 0.13f }, ls = { 1f, 0.6f, 0.8f };
            float y0 = c.y + S * 0.17f, wd = S * 0.022f;
            GUI.color = new Color(col.r * 0.9f, col.g * 0.9f, col.b * 0.9f, 0.92f * alpha);
            for (int i = 0; i < 3; i++)
            {
                float hgt = S * (0.06f + 0.11f * ls[i]) * gr, x = c.x + S * xs[i] + (Hash(seed, 90 + i) - 0.5f) * S * 0.03f;
                GUI.DrawTexture(new Rect(x - wd / 2f, y0, wd, hgt), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(x - wd * 0.75f, y0 + hgt - wd * 0.75f, wd * 1.5f, wd * 1.5f), Dot);   // 맺힌 방울
            }
        }
        GUI.color = g0;
    }
}
