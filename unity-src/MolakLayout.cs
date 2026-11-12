using UnityEngine;

/// <summary>
/// 화면 배치 수치를 한곳에 모은다. 캔버스 단위(900×1600 세로 기준)로 적는다.
/// 예전에는 MolakTheme·MolakScroll 곳곳에 150·30·34 같은 숫자가 흩어져 있어, 한쪽만 고치면 다른 쪽과 어긋났다.
/// </summary>
public static class MolakLayout
{
    // ---- 캔버스 ----
    public static readonly Vector2 Canvas = new Vector2(900f, 1600f);

    // ---- 하단 띠·본문 ----
    /// <summary>화면 아래 행낭 띠 높이. 선택지 목록은 이 위에 쌓인다.
    /// 0.4.22: 150 → 300. 휴대폰 아래 내비게이션 막대(제스처 바)와 글자가 겹쳐서, 띠를 두 배로 키우고 글은 위쪽 절반(BagStripContentH)에만 둔다.</summary>
    public const float BagStripH = 300f;
    /// <summary>행낭 띠 안에서 글과 손잡이가 차지하는 위쪽 높이. 그 아래는 기기 막대에 내주는 여백.</summary>
    public const float BagStripContentH = 150f;
    /// <summary>본문 창과 선택지 사이 숨 쉴 틈.</summary>
    public const float AboveChoices = 30f;
    /// <summary>본문 창이 아무리 눌려도 남겨 두는 최소 높이.</summary>
    public const float BodyMinH = 300f;
    /// <summary>본문 창 좌우 여백, 삽화 띠가 없을 때의 위 여백, 삽화 띠 아래 틈.</summary>
    public const float BodySide = 70f, BodyTop = 215f, BelowBand = 24f;
    /// <summary>위로 쓸어 행낭을 여는 하단 구역 높이(선택지가 떠 있을 때 / 없을 때).</summary>
    public const float BagSwipeZoneWithChoices = 300f, BagSwipeZone = 430f;
    /// <summary>본문 글자 기본 크기(설정의 글자 크기 배율이 곱해진다).</summary>
    public const float BodyFont = 36f;

    // ---- 선택지 목록 ----
    public const float ChoicePanelW = 850f;
    public const float CardMaxW = 840f;
    public const float CardWidthRatio = 0.84f;
    public const int CardPadLeft = 90, CardPadRight = 36;   // 왼쪽은 인장 자리
    /// <summary>선택지가 뜬 뒤 눌리지 않는 시간(초) — 글을 빠르게 넘기다 읽지도 않고 누르는 일을 막는다.
    /// 0.4.8부터 마지막 글 뒤 한 번 더 눌러야 선택지가 뜨므로 짧게 둔다(길면 '눌렀는데 안 눌린다'로 느껴짐).</summary>
    public const float ChoiceLock = 0.35f;

    /// <summary>
    /// 선택지 개수에 따른 카드 단계. 개수가 많을수록 카드·글자·인장을 줄여 화면 위로 넘치지 않게 한다.
    /// 여섯 개 이상(회귀 상점 등)은 '촘촘', 아홉 개 이상은 '빽빽'.
    /// </summary>
    public struct Tier
    {
        public float CardH, Gap, Font, TextPad, Seal, SealX, Numeral, Numeral2;
        public bool Compact;
    }

    static readonly Tier Dense   = new Tier { CardH = 60f,  Gap = 3f, Font = 28f, TextPad = 12f, Seal = 36f, SealX = 42f, Numeral = 21f, Numeral2 = 15f, Compact = true };
    static readonly Tier Tight   = new Tier { CardH = 76f,  Gap = 4f, Font = 30f, TextPad = 18f, Seal = 36f, SealX = 42f, Numeral = 21f, Numeral2 = 15f, Compact = true };
    static readonly Tier Medium  = new Tier { CardH = 100f, Gap = 6f, Font = 34f, TextPad = 34f, Seal = 44f, SealX = 48f, Numeral = 26f, Numeral2 = 18f, Compact = false };
    static readonly Tier Roomy   = new Tier { CardH = 116f, Gap = 6f, Font = 34f, TextPad = 34f, Seal = 44f, SealX = 48f, Numeral = 26f, Numeral2 = 18f, Compact = false };

    public static Tier For (int choices)
    {
        if (choices >= 9) return Dense;
        if (choices >= 6) return Tight;
        if (choices >= 4) return Medium;
        return Roomy;
    }

    /// <summary>선택지 목록이 차지할 높이(실측이 없을 때의 어림).</summary>
    public static float ChoiceListH (int choices)
    {
        var t = For(choices);
        return choices * (t.CardH + t.Gap);
    }

    /// <summary>선택지가 떠 있을 때 본문 창의 아래 한계(캔버스 바닥에서의 거리).</summary>
    public static float BodyBottom (int choices, float measuredListH = 0f)
    {
        if (choices <= 0) return BagStripH;
        float h = measuredListH > 10f ? measuredListH : ChoiceListH(choices);
        return BagStripH + h + AboveChoices;
    }

    /// <summary>인장 숫자(一 二 三 …). 열다섯을 넘으면 마지막 것을 되풀이한다.</summary>
    public static readonly string[] Numerals = { "一", "二", "三", "四", "五", "六", "七", "八", "九", "十", "十一", "十二", "十三", "十四", "十五" };
    public static string Numeral (int index) { return Numerals[Mathf.Clamp(index, 0, Numerals.Length - 1)]; }
}

/// <summary>나니노벨 기본 UI 안에서 찾아 쓰는 오브젝트 경로. 나니노벨 판올림으로 경로가 바뀌면 여기만 고친다.</summary>
public static class MolakUi
{
    public const string ChoiceListPath = "Naninovel<Runtime>/UI/ChoiceHandler/ButtonList";
    public const string PrinterPanelPath = "Naninovel<Runtime>/UI/TextPrinter/Dialogue/Content/PrinterPanel";

    static GameObject choiceList;
    /// <summary>선택지 버튼 목록. 한 번 찾으면 사라질 때까지 기억해 둔다(매 프레임 GameObject.Find 를 피한다).</summary>
    public static GameObject ChoiceList
    {
        get
        {
            if (choiceList == null) choiceList = GameObject.Find(ChoiceListPath);
            return choiceList;
        }
    }

    public static GameObject PrinterPanel { get { return GameObject.Find(PrinterPanelPath); } }
}
