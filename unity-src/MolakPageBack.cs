using System.Collections.Generic;
using Naninovel;
using UnityEngine;

/// <summary>
/// 0.4.57 지난 쪽 넘겨 보기 — 본문을 왼쪽에서 오른쪽으로 쓸면 앞 쪽, 오른쪽에서 왼쪽으로 쓸면 다시 뒤 쪽.
///  · 쪽이 넘어갈 때(MolakFlow 의 쪽 비움) 그 쪽의 글을 적어 둔다. 보기만 한다 — 이야기는 되감기지 않는다.
///  · 선택지를 고르면 적어 둔 쪽을 비운다: 고른 뒤로는 고르기 전의 쪽으로 돌아가지 않는다.
///    0.4.95 인연별 이야기 모드는 비우지 않고 처음 쪽까지 다시 본다. 그 쪽의 그림도 함께 비추고, 보는 동안은 자동 읽기를 멈춘다.
///  · 지난 쪽을 보는 동안에는 화면을 눌러도 이야기가 넘어가지 않는다(누르면 지금 쪽으로 돌아온다).
///  · 새 글이 찍히거나(자동 진행 등) 상태가 초기화되면 지금 쪽으로 돌아온다.
/// 그림은 그대로 두고, 본문 칸(DialogueText)을 잠시 숨긴 채 같은 글꼴·같은 자리의 사본 칸에 지난 쪽을 그린다.
/// 0.4.64 넘기는 연출(언메이커: 뒷장으로 넘겼을 때 연출 필요): 종이 넘기는 소리 + 넘어오는 쪽이 넘기는 방향에서 미끄러져 들어오며 짙어지고,
///   그 위로 책장 그림자 한 줄이 쓸고 지나간다. 지금 쪽으로 돌아올 때는 지금 쪽 글이 옅게 떠올랐다 짙어진다(본문 칸은 움직이지 않는다).
/// </summary>
public class MolakPageBack : MonoBehaviour
{
    static MolakPageBack inst;
    static readonly List<string> pages = new List<string>();
    static readonly List<string> pageArt = new List<string>();   // 0.4.95 그 쪽이 찍힐 때 떠 있던 그림(장면|인물)
    const int MaxPages = 12;
    /// <summary>0.4.95 인연별 이야기 모드는 소설처럼 읽는 것이 목적이라 앞 쪽을 책장 넘기듯 처음까지 다시 본다(언메이커). 선택지를 지나도 비우지 않는다.</summary>
    const int MaxPagesStory = 3000;
    static int Cap { get { return MolakStory.Auto ? MaxPagesStory : MaxPages; } }
    static int view = -1;                 // 보고 있는 지난 쪽(pages 의 번호), -1 = 지금 쪽
    static TMPro.TextMeshProUGUI past;
    static TMPro.TMP_Text hiddenSrc;
    static float hintT = -1f; static string hintMsg = "";
    // 넘기는 연출
    const float AnimDur = 0.34f;
    static float animT0 = -1f; static int animDir;   // animDir −1: 앞 쪽(왼쪽에서 들어온다) · +1: 뒤 쪽(오른쪽에서)
    static bool animLive;                             // 지금 쪽으로 돌아오는 중(본문 칸을 짙게)
    static Vector2 pastBase;
    static CanvasGroup pastCg;
    bool hooked;

    public static bool Viewing => view >= 0;
    /// <summary>0.4.80 검증 도구(MolakProbe)가 지난 쪽을 읽는다</summary>
    public static IReadOnlyList<string> Pages => pages;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot ()
    {
        if (inst) return;
        var go = new GameObject("MolakPageBack"); DontDestroyOnLoad(go);
        inst = go.AddComponent<MolakPageBack>();
    }

    /// <summary>쪽이 넘어가기 직전, 그 쪽의 글을 적어 둔다.</summary>
    public static void Push (string text)
    {
        if (string.IsNullOrEmpty(text) || text.Replace("\u200B", "").Trim().Length == 0) return;   // 0.4.80 보이지 않는 한 글자만 있는 쪽은 적지 않는다
        if (pages.Count > 0 && pages[pages.Count - 1] == text) return;
        pages.Add(text); pageArt.Add(MolakArt.ShownScene + "|" + MolakArt.ShownChar);
        while (pages.Count > Cap) { pages.RemoveAt(0); pageArt.RemoveAt(0); }
    }

    /// <summary>선택지를 골랐거나 새 생·불러오기: 고르기 전의 쪽으로는 돌아가지 않는다.</summary>
    public static void Clear () { Close(); pages.Clear(); pageArt.Clear(); }

    public static void Back ()
    {
        if (pages.Count == 0) { Hint(MolakTheme.ChoiceCount > 0 || afterPick ? "선택한 뒤에는 앞 쪽으로 돌아갈 수 없다." : "앞 쪽이 없다."); return; }
        int to = view < 0 ? pages.Count - 1 : view - 1;
        if (to < 0) { Hint(MolakStory.Auto ? "첫 쪽이다." : "더 앞 쪽은 없다. 마지막으로 선택한 뒤부터 볼 수 있다."); return; }
        Show(to);
    }

    public static void Forward ()
    {
        if (view < 0) return;
        if (view + 1 >= pages.Count) { Close(); return; }
        Show(view + 1);
    }

    static bool afterPick;   // 바로 앞에서 선택지를 골라 쪽이 비었는가(안내 문구용)
    public static void MarkPicked () { afterPick = true; }
    public static void MarkPrinted () { afterPick = false; }

    static TMPro.TMP_Text DialogueText ()
    {
        foreach (var panel in Object.FindObjectsByType<Naninovel.UI.RevealableTextPrinterPanel>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            foreach (var t0 in panel.GetComponentsInChildren<TMPro.TMP_Text>(false))
                if (t0.gameObject.name == "DialogueText") return t0;
        return null;
    }

    static void Show (int i)
    {
        var src = DialogueText();
        if (src == null) return;
        if (past == null)
        {
            var go = new GameObject("MolakPastPage", typeof(RectTransform));
            past = go.AddComponent<TMPro.TextMeshProUGUI>();
            past.raycastTarget = false;
            go.AddComponent<UnityEngine.UI.LayoutElement>().ignoreLayout = true;   // 본문 칸의 레이아웃 그룹이 사본을 본문 아래로 밀어내지 않게
        }
        if (past.transform.parent != src.transform.parent) past.transform.SetParent(src.transform.parent, false);
        var a = src.rectTransform; var b = past.rectTransform;
        b.anchorMin = a.anchorMin; b.anchorMax = a.anchorMax; b.pivot = a.pivot;
        b.offsetMin = a.offsetMin; b.offsetMax = a.offsetMax; b.anchoredPosition = a.anchoredPosition; b.sizeDelta = a.sizeDelta;
        b.SetSiblingIndex(a.GetSiblingIndex() + 1);
        past.font = src.font; past.fontSharedMaterial = src.fontSharedMaterial;
        past.fontSize = src.fontSize; past.fontStyle = src.fontStyle; past.color = src.color;
        past.lineSpacing = src.lineSpacing; past.paragraphSpacing = src.paragraphSpacing;
        past.characterSpacing = src.characterSpacing; past.wordSpacing = src.wordSpacing;
        past.margin = src.margin; past.richText = src.richText;
        past.textWrappingMode = src.textWrappingMode; past.alignment = src.alignment;
        past.textPreprocessor = MolakTypography.BodyStyler;   // 0.4.59 지난 쪽도 이름 색·대사 먹색 그대로
        past.text = pages[i];
        if (i < pageArt.Count) { var pa = pageArt[i].Split('|'); MolakArt.Preview(pa[0], pa.Length > 1 ? pa[1] : ""); }   // 0.4.95 그 쪽의 그림도 함께
        past.maxVisibleCharacters = 99999;
        past.gameObject.SetActive(true);
        Hide(src, true);
        hiddenSrc = src;
        // 넘기는 방향: 지금 쪽에서 앞으로, 또는 더 앞 쪽으로 = 왼쪽에서 / 다시 뒤 쪽으로 = 오른쪽에서
        animDir = (view < 0 || i < view) ? -1 : 1;
        view = i;
        pastBase = b.anchoredPosition;
        if (pastCg == null) { pastCg = past.GetComponent<CanvasGroup>(); if (pastCg == null) pastCg = past.gameObject.AddComponent<CanvasGroup>(); }   // 유니티 객체는 ?? 로 거르면 안 된다
        pastCg.alpha = 0f;
        animLive = false; animT0 = Time.unscaledTime;
        Apply(0f);
        MolakSfx.Page(); MolakSfx.Vibrate();
        if (Engine.Initialized) { var sp = Engine.GetService<IScriptPlayer>(); if (sp != null && sp.AutoPlaying) sp.SetAutoPlay(false); }   // 0.4.95 지난 쪽을 읽는 동안 이야기가 저절로 넘어가지 않게
    }

    public static void Close ()
    {
        if (view < 0 && (past == null || !past.gameObject.activeSelf)) return;
        bool wasViewing = view >= 0;
        view = -1;
        MolakArt.EndPreview();
        if (MolakPlayMode.Auto && Engine.Initialized) { var sp = Engine.GetService<IScriptPlayer>(); if (sp != null && !sp.AutoPlaying) sp.SetAutoPlay(true); }
        if (past != null) { past.gameObject.SetActive(false); past.rectTransform.anchoredPosition = pastBase; }
        var live = hiddenSrc;
        if (live != null) Hide(live, false);
        hiddenSrc = null;
        // 지금 쪽으로 돌아오는 연출: 오른쪽에서 넘어오듯 지금 쪽 글이 옅게 떠올라 짙어진다
        if (wasViewing && live != null)
        {
            liveTx = live; animLive = true; animDir = 1; animT0 = Time.unscaledTime;
            var cg = live.GetComponent<CanvasGroup>(); if (cg != null) cg.alpha = 0.15f;
        }
    }
    static TMPro.TMP_Text liveTx;

    /// <summary>연출 한 장면: e(0→1, 끝이 부드럽게)에 맞춰 넘어오는 쪽을 미끄러뜨리고 짙게.</summary>
    static void Apply (float e)
    {
        if (animLive)
        {
            if (liveTx != null) { var cg = liveTx.GetComponent<CanvasGroup>(); if (cg != null) cg.alpha = Mathf.Lerp(0.15f, 1f, e); }
            return;
        }
        if (past == null) return;
        float w = past.rectTransform.rect.width;
        past.rectTransform.anchoredPosition = pastBase + new Vector2((1f - e) * animDir * w * 0.22f, 0f);
        if (pastCg != null) pastCg.alpha = e;
    }

    /// <summary>본문 칸을 숨긴다 — 한자 등 대체 글꼴은 딸린 칸(SubMesh)에 따로 그려지므로 CanvasGroup 으로 통째로.</summary>
    static void Hide (TMPro.TMP_Text src, bool hide)
    {
        var cg = src.GetComponent<CanvasGroup>();
        if (cg == null) { if (!hide) return; cg = src.gameObject.AddComponent<CanvasGroup>(); }
        cg.alpha = hide ? 0f : 1f;
    }

    static void Hint (string m) { hintMsg = m; hintT = 0f; }

    void Update ()
    {
        if (!hooked && Engine.Initialized)
        {
            var st = Engine.GetService<IStateManager>();
            if (st != null) { st.OnResetStarted += Clear; st.OnGameLoadStarted += _ => Clear(); hooked = true; }
        }
        if (hintT >= 0f) { hintT += Time.unscaledDeltaTime; if (hintT > 1.8f) hintT = -1f; }
        if (animT0 >= 0f)
        {
            float t = Mathf.Clamp01((Time.unscaledTime - animT0) / AnimDur);
            float e = 1f - (1f - t) * (1f - t) * (1f - t);
            Apply(e);
            if (t >= 1f) { animT0 = -1f; if (animLive && liveTx != null) { var cg = liveTx.GetComponent<CanvasGroup>(); if (cg != null) cg.alpha = 1f; } animLive = false; }
        }
        // 메뉴·행낭이 열리면 지금 쪽으로
        if (view >= 0 && (MolakMenu.IsOpen || MolakBag.IsOpen)) Close();
    }

    GUIStyle st; float builtU = -1f;
    void OnGUI ()
    {
        if (view < 0 && hintT < 0f && animT0 < 0f) return;
        float u = MolakGui.U, w = Screen.width, h = Screen.height;
        // 책장 그림자: 넘기는 동안 넘기는 방향으로 한 줄 쓸고 지나간다(먹빛, 끝으로 갈수록 옅게)
        if (animT0 >= 0f && Event.current.type == EventType.Repaint)
        {
            float t = Mathf.Clamp01((Time.unscaledTime - animT0) / AnimDur);
            float e = 1f - (1f - t) * (1f - t) * (1f - t);
            float x = animDir < 0 ? Mathf.Lerp(-0.1f * w, 1.05f * w, e) : Mathf.Lerp(1.05f * w, -0.1f * w, e);
            float bw = 120f * u, fade = 1f - e;
            var gc = GUI.color;
            GUI.depth = MolakGui.DepthVfx - 9;
            for (int s = 0; s < 8; s++)
            {
                float k = s / 8f;   // 가운데가 짙고 뒤로 갈수록 옅은 띠
                float sx = x - animDir * k * bw;
                GUI.color = new Color(0.16f, 0.11f, 0.07f, 0.22f * fade * (1f - k));
                GUI.DrawTexture(new Rect(sx - bw / 16f, 0, bw / 8f, h), MolakGui.Px);
            }
            GUI.color = new Color(0.98f, 0.95f, 0.88f, 0.10f * fade);   // 넘어오는 종이의 밝은 가장자리
            GUI.DrawTexture(new Rect(x - 2f * u, 0, 4f * u, h), MolakGui.Px);
            GUI.color = gc;
            if (view < 0 && hintT < 0f) return;
        }
        if (st == null || !Mathf.Approximately(builtU, u)) { builtU = u; st = MolakGui.Label(24, u, TextAnchor.MiddleCenter, false); }
        GUI.depth = MolakGui.DepthVfx - 10;
        var g0 = GUI.color;
        string msg = view >= 0 ? (MolakStory.Auto ? "지난 쪽 " + (view + 1) + " / " + pages.Count + " · 왼쪽으로 밀면 뒤로, 누르면 지금 쪽으로" : "지난 쪽 " + (pages.Count - view) + " · 왼쪽으로 밀거나 누르면 지금 쪽으로") : hintMsg;
        float a = view >= 0 ? 1f : (hintT < 1.4f ? 1f : Mathf.Clamp01(1f - (hintT - 1.4f) / 0.4f));
        var r = new Rect(w / 2f - 330f * u, h * 0.83f, 660f * u, 56f * u);
        GUI.color = new Color(0.08f, 0.06f, 0.05f, 0.82f * a); GUI.DrawTexture(r, MolakGui.Px); GUI.color = Color.white;
        MolakGui.Ink(st, new Color(0.95f, 0.91f, 0.84f, a)); GUI.Label(r, msg, st);
        GUI.color = g0;
        if (view >= 0)
        {
            // 지난 쪽을 보는 동안: 화면을 눌러도 이야기는 넘어가지 않는다. 쓸지 않고 누르기만 하면 지금 쪽으로.
            MolakInputGuard.Guard(new Rect(0, 0, w, h));
            var e = Event.current;
            if (e.type == EventType.MouseUp && !MolakScroll.DraggedThisPress) { Close(); e.Use(); MolakInputGuard.MuteFor(0.25f); }
        }
    }
}
