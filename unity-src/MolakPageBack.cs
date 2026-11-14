using System.Collections.Generic;
using Naninovel;
using UnityEngine;

/// <summary>
/// 0.4.57 지난 쪽 넘겨 보기 — 본문을 왼쪽에서 오른쪽으로 쓸면 앞 쪽, 오른쪽에서 왼쪽으로 쓸면 다시 뒤 쪽.
///  · 쪽이 넘어갈 때(MolakFlow 의 쪽 비움) 그 쪽의 글을 적어 둔다. 보기만 한다 — 이야기는 되감기지 않는다.
///  · 선택지를 고르면 적어 둔 쪽을 비운다: 고른 뒤로는 고르기 전의 쪽으로 돌아가지 않는다.
///  · 지난 쪽을 보는 동안에는 화면을 눌러도 이야기가 넘어가지 않는다(누르면 지금 쪽으로 돌아온다).
///  · 새 글이 찍히거나(자동 진행 등) 상태가 초기화되면 지금 쪽으로 돌아온다.
/// 그림은 그대로 두고, 본문 칸(DialogueText)을 잠시 숨긴 채 같은 글꼴·같은 자리의 사본 칸에 지난 쪽을 그린다.
/// </summary>
public class MolakPageBack : MonoBehaviour
{
    static MolakPageBack inst;
    static readonly List<string> pages = new List<string>();
    const int MaxPages = 12;
    static int view = -1;                 // 보고 있는 지난 쪽(pages 의 번호), -1 = 지금 쪽
    static TMPro.TextMeshProUGUI past;
    static TMPro.TMP_Text hiddenSrc;
    static float hintT = -1f; static string hintMsg = "";
    bool hooked;

    public static bool Viewing => view >= 0;

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
        if (string.IsNullOrEmpty(text) || text.Trim().Length == 0) return;
        if (pages.Count > 0 && pages[pages.Count - 1] == text) return;
        pages.Add(text);
        while (pages.Count > MaxPages) pages.RemoveAt(0);
    }

    /// <summary>선택지를 골랐거나 새 생·불러오기: 고르기 전의 쪽으로는 돌아가지 않는다.</summary>
    public static void Clear () { Close(); pages.Clear(); }

    public static void Back ()
    {
        if (pages.Count == 0) { Hint(MolakTheme.ChoiceCount > 0 || afterPick ? "고른 뒤로는 앞 쪽으로 돌아가지 않는다." : "앞 쪽이 없다."); return; }
        int to = view < 0 ? pages.Count - 1 : view - 1;
        if (to < 0) { Hint("더 앞 쪽은 없다. 선택지를 고른 자리부터다."); return; }
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
        past.text = pages[i];
        past.maxVisibleCharacters = 99999;
        past.gameObject.SetActive(true);
        Hide(src, true);
        hiddenSrc = src;
        view = i;
        MolakSfx.Confirm();
    }

    public static void Close ()
    {
        if (view < 0 && (past == null || !past.gameObject.activeSelf)) return;
        view = -1;
        if (past != null) past.gameObject.SetActive(false);
        if (hiddenSrc != null) Hide(hiddenSrc, false);
        hiddenSrc = null;
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
        // 메뉴·행낭이 열리면 지금 쪽으로
        if (view >= 0 && (MolakMenu.IsOpen || MolakBag.IsOpen)) Close();
    }

    GUIStyle st; float builtU = -1f;
    void OnGUI ()
    {
        if (view < 0 && hintT < 0f) return;
        float u = MolakGui.U, w = Screen.width, h = Screen.height;
        if (st == null || !Mathf.Approximately(builtU, u)) { builtU = u; st = MolakGui.Label(24, u, TextAnchor.MiddleCenter, false); }
        GUI.depth = MolakGui.DepthVfx - 10;
        var g0 = GUI.color;
        string msg = view >= 0 ? "지난 쪽 " + (pages.Count - view) + " · 왼쪽으로 밀면 돌아온다 · 누르면 지금 쪽" : hintMsg;
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
