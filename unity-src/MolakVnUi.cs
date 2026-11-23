using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 0.5.0 자체 실행기 — 무대(글 칸·선택지 묶음)와 인쇄기·선택지·입력.
//   글 칸과 선택지 묶음은 예전과 같은 뼈대·같은 이름으로 짓는다(PrinterPanel/Printer/TextPanel/DialogueText · ButtonList/ContentPanel/Mask/단추).
//   배치·꾸밈은 그대로 MolakScroll·MolakTheme 이 맡는다. 여기서는 뼈대만 세운다.
namespace MolakVn
{
    /// <summary>무대: 화면에 깔리는 uGUI 뼈대를 코드로 짓는다(프리팹 없이).</summary>
    public class Stage : MonoBehaviour
    {
        public const string RootName = "Molak<Runtime>";
        public UI.RevealableTextPrinterPanel PrinterPanel;
        public UI.ChoiceHandlerPanel ChoicePanel;

        static RectTransform Rt (string name, Transform parent, params Type[] comps)
        {
            var go = new GameObject(name, typeof(RectTransform));
            foreach (var c in comps) go.AddComponent(c);
            var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
            go.layer = 5;
            return rt;
        }
        static void Fill (RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
        static void MakeCanvas (GameObject go, int order)
        {
            var cv = go.AddComponent<Canvas>(); cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = order;
            var cs = go.AddComponent<CanvasScaler>(); cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; cs.referenceResolution = MolakLayout.Canvas; cs.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            go.AddComponent<GraphicRaycaster>();
            go.AddComponent<CanvasGroup>();
        }

        public static Stage Build ()
        {
            var old = GameObject.Find(RootName); if (old != null) DestroyImmediate(old);
            var root = new GameObject(RootName); DontDestroyOnLoad(root);
            var stage = root.AddComponent<Stage>();
            var ui = new GameObject("UI").transform; ui.SetParent(root.transform, false);

            // 누름을 uGUI 단추로 보내는 것(선택지 카드)
            if (UnityEngine.EventSystems.EventSystem.current == null && FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
                es.transform.SetParent(root.transform, false);
            }

            // ---- 글 칸: UI/TextPrinter/Dialogue/Content/PrinterPanel/Printer/TextPanel/DialogueText ----
            var tp = new GameObject("TextPrinter").transform; tp.SetParent(ui, false);
            var dlg = new GameObject("Dialogue", typeof(RectTransform)); dlg.transform.SetParent(tp, false); dlg.layer = 5;
            MakeCanvas(dlg, 1);
            var panel = dlg.AddComponent<UI.RevealableTextPrinterPanel>();
            var content = Rt("Content", dlg.transform, typeof(HorizontalLayoutGroup)); Fill(content);
            var hlg = content.GetComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = true; hlg.childControlHeight = true; hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true; hlg.childAlignment = TextAnchor.UpperLeft;
            Rt("LeftSpace", content, typeof(LayoutElement)).GetComponent<LayoutElement>().flexibleWidth = 0f;
            var pp = Rt("PrinterPanel", content, typeof(LayoutElement));
            { var le = pp.GetComponent<LayoutElement>(); le.minWidth = 0f; le.preferredWidth = 1100f; }
            Rt("RightSpace", content, typeof(LayoutElement)).GetComponent<LayoutElement>().flexibleWidth = 0f;
            var printer = Rt("Printer", pp, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            printer.anchorMin = new Vector2(0f, 1f); printer.anchorMax = new Vector2(1f, 1f); printer.pivot = new Vector2(0.5f, 1f);
            printer.offsetMin = new Vector2(0f, -300f); printer.offsetMax = Vector2.zero;
            {
                var vlg = printer.GetComponent<VerticalLayoutGroup>();
                vlg.padding = new RectOffset(10, 10, 10, 0); vlg.spacing = 10f; vlg.childAlignment = TextAnchor.UpperLeft;
                vlg.childControlWidth = true; vlg.childControlHeight = true; vlg.childForceExpandWidth = false; vlg.childForceExpandHeight = false;
                var csf = printer.GetComponent<ContentSizeFitter>(); csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            var textPanel = Rt("TextPanel", printer, typeof(Image), typeof(LayoutElement), typeof(VerticalLayoutGroup));
            {
                var img = textPanel.GetComponent<Image>(); img.color = new Color(0f, 0f, 0f, 0f); img.raycastTarget = false;
                var le = textPanel.GetComponent<LayoutElement>(); le.minHeight = 250f; le.flexibleWidth = 1f;
                var vlg = textPanel.GetComponent<VerticalLayoutGroup>();
                vlg.padding = new RectOffset(25, 25, 15, 60); vlg.spacing = 0f; vlg.childAlignment = TextAnchor.UpperLeft;
                vlg.childControlWidth = true; vlg.childControlHeight = true; vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = true;
            }
            var dt = Rt("DialogueText", textPanel, typeof(TMPro.TextMeshProUGUI));
            var tx = dt.GetComponent<TMPro.TextMeshProUGUI>();
            tx.fontSize = MolakLayout.BodyFont; tx.alignment = TMPro.TextAlignmentOptions.TopLeft; tx.richText = true; tx.raycastTarget = false;
            tx.textWrappingMode = TMPro.TextWrappingModes.Normal; tx.overflowMode = TMPro.TextOverflowModes.Overflow;
            tx.color = new Color(0.8f, 0.76f, 0.7f, 1f); tx.text = "";
            panel.Text = tx;
            panel.Group = dlg.GetComponent<CanvasGroup>(); panel.Group.blocksRaycasts = false;
            panel.SetVisible(false);
            stage.PrinterPanel = panel;

            // ---- 선택지 묶음: UI/ChoiceHandler/ButtonList/ContentPanel/Mask/단추들 ----
            var ch = new GameObject("ChoiceHandler").transform; ch.SetParent(ui, false);
            var list = new GameObject("ButtonList", typeof(RectTransform)); list.transform.SetParent(ch, false); list.layer = 5;
            MakeCanvas(list, 2);
            var cpanel = list.AddComponent<UI.ChoiceHandlerPanel>();
            var cp = Rt("ContentPanel", list.transform, typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            cp.anchorMin = new Vector2(0.5f, 0.5f); cp.anchorMax = new Vector2(0.5f, 0.5f); cp.pivot = new Vector2(0.5f, 0.5f);   // 아래 붙이기·폭은 MolakTheme 이 한다
            cp.sizeDelta = new Vector2(MolakLayout.ChoicePanelW, 100f);
            {
                var img = cp.GetComponent<Image>(); img.color = new Color(0f, 0f, 0f, 0f); img.raycastTarget = false;
                var vlg = cp.GetComponent<VerticalLayoutGroup>(); vlg.spacing = 12f; vlg.childAlignment = TextAnchor.MiddleCenter;
                vlg.childControlWidth = true; vlg.childControlHeight = true; vlg.childForceExpandWidth = false; vlg.childForceExpandHeight = true;
                cp.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            var mask = Rt("Mask", cp, typeof(RectMask2D), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            {
                var vlg = mask.GetComponent<VerticalLayoutGroup>(); vlg.spacing = 12f; vlg.childAlignment = TextAnchor.MiddleCenter;
                vlg.childControlWidth = true; vlg.childControlHeight = true; vlg.childForceExpandWidth = false; vlg.childForceExpandHeight = true;
                mask.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            cpanel.Holder = mask; cpanel.Group = list.GetComponent<CanvasGroup>();
            cpanel.SetVisible(false);
            stage.ChoicePanel = cpanel;
            return stage;
        }
    }

    // ---------------------------------------------------------------- 인쇄기
    public interface ITextPrinterActor
    {
        string Id { get; }
        bool Visible { get; set; }
        float RevealProgress { get; set; }
        void CollectMessages (ICollection<PrintedMessage> messages);
        void SetMessages (IReadOnlyList<PrintedMessage> messages);
        void AddMessage (PrintedMessage message);
        void AppendText (LocalizableText text);
        void ClearMessages ();
    }
    public interface ITextPrinterManager
    {
        string DefaultPrinterId { get; }
        float BaseRevealSpeed { get; set; }
        float BaseAutoDelay { get; set; }
        bool ActorExists (string id);
        ITextPrinterActor GetActor (string id);
    }

    /// <summary>글 칸 하나. 찍힌 글(문단들)을 이어 한 칸에 넣고, 새로 붙은 글만 한 글자씩 드러낸다.</summary>
    public class TextPrinter : ITextPrinterActor
    {
        public string Id { get { return "Dialogue"; } }
        readonly UI.RevealableTextPrinterPanel panel;
        readonly List<PrintedMessage> messages = new List<PrintedMessage>();
        readonly System.Text.StringBuilder sb = new System.Text.StringBuilder(2048);
        public TextPrinter (UI.RevealableTextPrinterPanel p) { panel = p; }

        public bool Visible { get { return panel.Shown; } set { panel.SetVisible(value); } }
        public float RevealProgress { get { return panel.Progress; } set { panel.Progress = value; } }
        public bool Revealing { get { return panel.Revealing; } }
        public void CompleteReveal () { panel.Complete(); }
        public Awaitable Reveal (float delay) { return panel.Reveal(delay); }

        void Push ()
        {
            sb.Length = 0;
            foreach (var m in messages) sb.Append(m.Text.ToString());
            panel.SetText(sb.ToString());
        }
        public void CollectMessages (ICollection<PrintedMessage> list) { foreach (var m in messages) list.Add(m); }
        public void SetMessages (IReadOnlyList<PrintedMessage> list) { var copy = new List<PrintedMessage>(list); messages.Clear(); messages.AddRange(copy); Push(); }
        public void AddMessage (PrintedMessage m) { messages.Add(m); Push(); }
        public void AppendText (LocalizableText t)
        {
            if (messages.Count == 0) { AddMessage(new PrintedMessage(t)); return; }
            messages[messages.Count - 1] = new PrintedMessage(messages[messages.Count - 1].Text + t);
            Push();
        }
        public void ClearMessages () { messages.Clear(); Push(); }
    }

    public class TextPrinterManager : ITextPrinterManager
    {
        /// <summary>글자 하나가 나오는 가장 긴 틈(초) — 빠르기 0 일 때. 빠르기 1 이면 틈이 없다.</summary>
        public const float MaxRevealDelay = 0.06f;
        /// <summary>자동 읽기: 글자 하나에 두는 가장 긴 틈(초).</summary>
        public const float MaxAutoWaitDelay = 0.02f;
        public readonly TextPrinter Default;
        public string DefaultPrinterId { get { return Default.Id; } }
        public float BaseRevealSpeed { get; set; }
        public float BaseAutoDelay { get; set; }
        public TextPrinterManager (UI.RevealableTextPrinterPanel panel)
        {
            Default = new TextPrinter(panel);
            BaseRevealSpeed = PlayerPrefs.GetFloat("molak_reveal", 0.5f);
            BaseAutoDelay = PlayerPrefs.GetFloat("molak_autodelay", 0.5f);
        }
        public bool ActorExists (string id) { return id == Default.Id; }
        public ITextPrinterActor GetActor (string id) { return Default; }
    }

    // ---------------------------------------------------------------- 선택지
    public struct Choice
    {
        public string Id, Summary, ScriptPath, GotoPath; public bool Locked;
        public Choice (string id, string summary, string script, string gotoPath, bool locked) { Id = id; Summary = summary; ScriptPath = script; GotoPath = gotoPath; Locked = locked; }
    }
    public interface IChoiceHandlerActor
    {
        bool Visible { get; set; }
        void AddChoice (Choice choice);
        void RemoveChoice (string id);
        void CollectChoices (ICollection<Choice> choices);
    }
    public interface IChoiceHandlerManager
    {
        string DefaultHandlerId { get; }
        bool ActorExists (string id);
        IChoiceHandlerActor GetActor (string id);
    }
    public class ChoiceHandler : IChoiceHandlerActor
    {
        readonly UI.ChoiceHandlerPanel panel;
        readonly List<Choice> choices = new List<Choice>();
        public ChoiceHandler (UI.ChoiceHandlerPanel p) { panel = p; panel.Owner = this; }
        public bool Visible { get { return panel.Shown; } set { panel.SetVisible(value); } }
        public void AddChoice (Choice c) { choices.Add(c); panel.Add(c); panel.SetVisible(true); }
        public void RemoveChoice (string id) { choices.RemoveAll(c => c.Id == id); panel.Remove(id); if (choices.Count == 0) panel.SetVisible(false); }
        public void CollectChoices (ICollection<Choice> list) { foreach (var c in choices) list.Add(c); }
        public void Clear () { choices.Clear(); panel.Clear(); panel.SetVisible(false); }
        /// <summary>골랐다: 선택지를 걷고, 그 선택지가 가리키는 자리부터 재생한다.</summary>
        internal void Pick (string id)
        {
            int i = choices.FindIndex(c => c.Id == id);
            if (i < 0 || choices[i].Locked) return;
            var c0 = choices[i];
            MolakBacklog.Pick(c0.Summary);
            Clear();
            Engine.Player.Track.PlayFromChoice(c0);
        }
    }
    public class ChoiceHandlerManager : IChoiceHandlerManager
    {
        public readonly ChoiceHandler Default;
        public string DefaultHandlerId { get { return "ButtonList"; } }
        public ChoiceHandlerManager (UI.ChoiceHandlerPanel panel) { Default = new ChoiceHandler(panel); }
        public bool ActorExists (string id) { return id == DefaultHandlerId; }
        public IChoiceHandlerActor GetActor (string id) { return Default; }
    }

    // ---------------------------------------------------------------- 입력('계속')
    public interface IInputHandle
    {
        event Action OnStart;
        event Action OnEnd;
        string Id { get; }
        bool Active { get; }
        bool Muted { get; set; }
        void Activate (Vector2 force);
        void Activate (float force);
    }
    public class InputHandle : IInputHandle
    {
        public event Action OnStart, OnEnd;
        public string Id { get; private set; }
        public bool Active { get; private set; }
        public bool Muted { get; set; }
        readonly InputManager owner;
        public InputHandle (string id, InputManager m) { Id = id; owner = m; }
        public void Activate (float force) { Activate(new Vector2(force, 0f)); }
        public void Activate (Vector2 force)
        {
            bool on = force != Vector2.zero;
            if (on && (Muted || !owner.ProcessInput)) return;
            if (on == Active) return;
            Active = on;
            var h = on ? OnStart : OnEnd;
            if (h != null) foreach (var d in h.GetInvocationList()) try { ((Action)d)(); } catch (Exception e) { Debug.LogException(e); }
        }
    }
    public interface IInputManager { IInputHandle GetInput (string id); bool ProcessInput { get; set; } }
    public class InputManager : MonoBehaviour, IInputManager
    {
        readonly Dictionary<string, InputHandle> handles = new Dictionary<string, InputHandle>();
        public bool ProcessInput { get; set; } = true;
        InputHandle cont; bool keyHeld;
        public IInputHandle GetInput (string id)
        {
            InputHandle h;
            if (!handles.TryGetValue(id, out h)) { h = new InputHandle(id, this); handles[id] = h; }
            return h;
        }
        void Awake ()
        {
            cont = (InputHandle)GetInput("Continue"); GetInput("Skip");
            cont.OnStart += () => { if (Engine.Player != null) Engine.Player.HandleContinue(); };
        }
        void Update ()
        {   // 글쇠(Space·Enter)도 '계속' — 손가락·마우스 누름은 MolakInputGuard 가 뗄 때 보내 준다
            var kb = UnityEngine.InputSystem.Keyboard.current;
            bool down = kb != null && (kb.spaceKey.isPressed || kb.enterKey.isPressed);
            if (down && !keyHeld) cont.Activate(1f);
            else if (!down && keyHeld) cont.Activate(0f);
            keyHeld = down;
        }
    }
}

namespace MolakVn.UI
{
    public class UITextPrinterPanel : MonoBehaviour { }

    /// <summary>글 칸: 글을 넣고(이미 보이던 글자는 그대로) 새 글자를 차례로 드러낸다. 막 나온 글자 몇은 옅게 떠오른다.</summary>
    public class RevealableTextPrinterPanel : UITextPrinterPanel
    {
#pragma warning disable CS0414
        [SerializeField] int autoLineBreaks = 0;
#pragma warning restore CS0414
        public TMPro.TMP_Text Text;
        public CanvasGroup Group;
        public bool Shown { get; private set; }
        /// <summary>아직 드러나지 않은 글자가 있다(누르면 한꺼번에 보인다).</summary>
        public bool Revealing { get; private set; }
        /// <summary>막 나온 글자가 또렷해질 때까지의 길이(글자 수).</summary>
        const float Fade = 5f;
        float shown;     // 드러난 글자 수(소수 — 시간에 따라 는다). 다 드러난 뒤에도 Fade 만큼 더 가며 꼬리를 또렷하게 한다
        int total;       // 지금 글의 글자 수(꾸밈 태그 빼고)
        int solid;       // 여기까지는 또렷하다(앞서 찍힌 글)
        int ticket; bool paint;

        void OnEnable () { Canvas.willRenderCanvases += Paint; }
        void OnDisable () { Canvas.willRenderCanvases -= Paint; }

        public void SetVisible (bool v) { Shown = v; if (Group != null) Group.alpha = v ? 1f : 0f; }

        public void SetText (string t)
        {
            shown = Mathf.Min(shown, total);          // 지금까지 드러난 글자는 그대로
            solid = Mathf.FloorToInt(shown);
            Text.text = t ?? "";
            Text.ForceMeshUpdate();
            total = Text.textInfo != null ? Text.textInfo.characterCount : 0;
            if (shown > total) shown = total;
            if (solid > total) solid = total;
            Apply();
        }
        void Apply () { Text.maxVisibleCharacters = shown >= total ? 99999 : Mathf.CeilToInt(shown); paint = true; }

        public float Progress
        {
            get { return total <= 0 ? 1f : Mathf.Clamp01(shown / total); }
            set { ticket++; Revealing = false; shown = Mathf.Clamp01(value) * total; solid = Mathf.FloorToInt(shown); Apply(); }
        }
        public void Complete () { ticket++; Revealing = false; shown = total; solid = total; Apply(); }

        /// <summary>아직 안 보이는 글자를 delay 초에 하나씩 드러낸다. 중간에 Complete(누름·넘기기)가 오면 바로 끝난다.
        /// 마지막 글자가 나오면 끝난 것으로 치고(다음 줄·기다림으로 넘어간다), 꼬리가 또렷해지는 것은 뒤에서 마저 한다.</summary>
        public async Awaitable Reveal (float delay)
        {
            int my = ++ticket;
            if (delay <= 0.0001f || shown >= total) { shown = total; solid = total; Revealing = false; Apply(); return; }
            Revealing = true;
            while (my == ticket && shown < total)
            {
                await Awaitable.NextFrameAsync();
                if (my != ticket) return;
                shown = Mathf.Min(total, shown + Mathf.Min(Time.unscaledDeltaTime, 0.1f) / delay);
                Apply();
            }
            if (my != ticket) return;
            Revealing = false;
            _ = Tail(my, delay);
        }
        async Awaitable Tail (int my, float delay)
        {
            float end = total + Fade;
            while (my == ticket && shown < end)
            {
                await Awaitable.NextFrameAsync();
                if (my != ticket) return;
                shown = Mathf.Min(end, shown + Mathf.Min(Time.unscaledDeltaTime, 0.1f) / delay);
                paint = true;
            }
            if (my == ticket) { shown = total; solid = total; paint = true; }
        }

        /// <summary>글자 빛을 입힌다 — TMP 가 그물을 다시 짠 뒤(같은 프레임)에 불린다. 막 나온 Fade 글자만 옅게.</summary>
        void Paint ()
        {
            if (!paint || Text == null) return;
            paint = false;
            var ti = Text.textInfo; if (ti == null || ti.characterInfo == null) return;
            int n = Mathf.Min(total, ti.characterCount); if (n > ti.characterInfo.Length) n = ti.characterInfo.Length;
            int from = Mathf.Max(0, Mathf.Min(solid, Mathf.FloorToInt(shown - Fade)) - 1), to = Mathf.Min(n - 1, Mathf.CeilToInt(shown));
            bool any = false;
            for (int i = from; i <= to; i++)
            {
                var ci = ti.characterInfo[i]; if (!ci.isVisible) continue;
                float a = i < solid ? 1f : Mathf.Clamp01((shown - i) / Fade);
                int mi = ci.materialReferenceIndex; if (mi < 0 || mi >= ti.meshInfo.Length) continue;
                var cols = ti.meshInfo[mi].colors32; int v = ci.vertexIndex;
                if (cols == null || v + 3 >= cols.Length) continue;
                byte ba = (byte)Mathf.RoundToInt(ci.color.a * a);
                if (cols[v].a == ba) continue;
                cols[v].a = ba; cols[v + 1].a = ba; cols[v + 2].a = ba; cols[v + 3].a = ba;
                any = true;
            }
            if (any) Text.UpdateVertexData(TMPro.TMP_VertexDataUpdateFlags.Colors32);
        }
    }

    public class ChoiceHandlerButton : MonoBehaviour { public Choice Choice; }

    /// <summary>선택지 묶음: 선택지마다 단추 하나. 꾸밈(카드·인장·잠김)은 MolakTheme 이 입힌다.</summary>
    public class ChoiceHandlerPanel : MonoBehaviour
    {
        public RectTransform Holder; public CanvasGroup Group; public ChoiceHandler Owner;
        public bool Shown { get; private set; }
        readonly List<ChoiceHandlerButton> buttons = new List<ChoiceHandlerButton>();
        string pending;

        public void SetVisible (bool v) { Shown = v; if (Group != null) { Group.alpha = v ? 1f : 0f; Group.blocksRaycasts = v; Group.interactable = v; } }

        public void Add (Choice c)
        {
            var go = new GameObject("ChoiceButton(Clone)", typeof(RectTransform), typeof(Image), typeof(Button), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            go.layer = 5; go.transform.SetParent(Holder, false);
            var vlg = go.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(92, 36, 10, 10); vlg.childAlignment = TextAnchor.MiddleLeft;
            vlg.childControlWidth = true; vlg.childControlHeight = true; vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = true;
            var b = go.AddComponent<ChoiceHandlerButton>(); b.Choice = c;
            var tg = new GameObject("Text", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI)); tg.layer = 5; tg.transform.SetParent(go.transform, false);
            var tx = tg.GetComponent<TMPro.TextMeshProUGUI>();
            tx.text = c.Summary ?? ""; tx.fontSize = 31f; tx.alignment = TMPro.TextAlignmentOptions.MidlineLeft; tx.richText = true; tx.raycastTarget = false;
            tx.textWrappingMode = TMPro.TextWrappingModes.Normal; tx.overflowMode = TMPro.TextOverflowModes.Overflow;
            string id = c.Id;
            go.GetComponent<Button>().onClick.AddListener(() => { if (pending == null) pending = id; });
            buttons.Add(b);
        }
        public void Remove (string id)
        {
            for (int i = buttons.Count - 1; i >= 0; i--)
                if (buttons[i] == null || buttons[i].Choice.Id == id) { Kill(buttons[i]); buttons.RemoveAt(i); }
        }
        public void Clear () { foreach (var b in buttons) Kill(b); buttons.Clear(); pending = null; }
        static void Kill (ChoiceHandlerButton b)
        {
            if (b == null) return;
            b.gameObject.SetActive(false);   // 이번 프레임부터 화면·찾기에서 빠진다
            Destroy(b.gameObject);
        }
        void Update ()
        {   // 누른 프레임의 다른 듣는 이(효과음·고른 일 적기)가 끝난 뒤, 다음 프레임에 고른 것을 처리한다
            if (pending == null) return;
            string id = pending; pending = null;
            if (Owner != null) Owner.Pick(id);
        }
    }
}
