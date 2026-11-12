using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// 글꼴과 글 읽힘새(0.4.12).
/// - 본문: 고운바탕 그대로, 줄 간격과 문단 간격을 넓혀 숨을 준다. 서술은 한 톤 옅은 먹, 대사는 짙은 먹으로 갈라 눈이 대사를 먼저 잡게 한다.
/// - 제목·묶음 머리·말로 카드·선택지: 송명(Song Myung, OFL). 붓으로 쓴 명조라 무협의 맛이 나면서도 작은 크기에서 읽힌다.
///   송명에 없는 글자(한자·특수 문장부호)는 고운바탕으로 넘어간다(TMP 폴백).
/// </summary>
public static class MolakTypography
{
    // ---- 본문 숨 ----
    /// <summary>줄 간격(TMP 단위, 글자 높이의 1/100). 한국어 명조는 기본 줄 간격이 촘촘해 20 정도를 더한다.</summary>
    public const float BodyLineSpacing = 20f;
    /// <summary>문단(줄바꿈) 사이 간격. 대사와 서술을 빈 줄로 나누는 0.4.10 규칙과 함께 쓴다.</summary>
    public const float BodyParagraphSpacing = 14f;

    public static Color Narration { get { return MolakTheme.Night ? new Color(0.80f, 0.76f, 0.70f, 1f) : new Color(0.25f, 0.21f, 0.18f, 1f); } }
    public static string DialogueHex { get { return MolakTheme.Night ? "#F6EFE2" : "#140E0A"; } }

    // ---- 송명 ----
    static Font songFont;
    static TMPro.TMP_FontAsset songTMP;
    public static Font SongFont { get { if (songFont == null) songFont = Resources.Load<Font>("SongMyung"); return songFont; } }
    /// <summary>IMGUI 제목 글꼴: 송명이 있으면 송명, 없으면 주어진 글꼴. 송명은 한자가 없으니 한글 제목에만 쓴다.</summary>
    public static Font Heading (Font fallback) { var f = SongFont; return f != null ? f : fallback; }

    /// <summary>송명 TMP 글꼴(동적). 처음 부를 때 만들고, 없는 글자는 fallback 으로 넘긴다.</summary>
    public static TMPro.TMP_FontAsset SongTMP (TMPro.TMP_FontAsset fallback)
    {
        if (songTMP != null) return songTMP;
        var f = SongFont;
        if (f == null) return fallback;
        songTMP = TMPro.TMP_FontAsset.CreateFontAsset(f);
        if (songTMP == null) return fallback;
        songTMP.name = "SongMyung SDF (runtime)";
        if (fallback != null)
        {
            if (songTMP.fallbackFontAssetTable == null) songTMP.fallbackFontAssetTable = new System.Collections.Generic.List<TMPro.TMP_FontAsset>();
            songTMP.fallbackFontAssetTable.Add(fallback);
        }
        return songTMP;
    }

    // ---- 대사 가르기 ----
    /// <summary>본문 TMP 에 붙는 전처리기: 따옴표로 여는 줄을 짙은 먹으로 감싼다. 글이 드러나는 속도(보이는 글자 수)에는 영향이 없다.</summary>
    public class DialogueStyler : TMPro.ITextPreprocessor
    {
        static readonly Regex Line = new Regex("^((?:\\s|<[^>]+>)*)([\"“「].*)$", RegexOptions.Multiline);
        public string PreprocessText (string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('"') < 0 && text.IndexOf('“') < 0 && text.IndexOf('「') < 0) return text;
            string hex = DialogueHex;
            return Line.Replace(text, m => m.Groups[1].Value + "<color=" + hex + ">" + m.Groups[2].Value.TrimEnd('\r') + "</color>");
        }
    }
    static readonly DialogueStyler styler = new DialogueStyler();

    static int lastNight = -1;
    /// <summary>본문 TMP 하나에 읽힘새를 입힌다(매 프레임 불러도 바뀐 것만 고친다). 먹색 화면을 켜고 끄면 대사 색을 다시 입힌다.</summary>
    public static void ApplyBody (TMPro.TMP_Text tx)
    {
        int n = MolakTheme.Night ? 1 : 0;
        if (tx.textPreprocessor != styler || n != lastNight) { tx.textPreprocessor = styler; tx.havePropertiesChanged = true; tx.SetAllDirty(); lastNight = n; }
        if (Mathf.Abs(tx.lineSpacing - BodyLineSpacing) > 0.1f) tx.lineSpacing = BodyLineSpacing;
        if (Mathf.Abs(tx.paragraphSpacing - BodyParagraphSpacing) > 0.1f) tx.paragraphSpacing = BodyParagraphSpacing;
    }
}
