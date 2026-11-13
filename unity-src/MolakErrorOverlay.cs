using System.Collections.Generic;
using UnityEngine;

/// <summary>기기 디버그용 — 에러/예외 로그를 화면 하단에 표시 (탭하면 지움). 원인 파악 후 제거 예정.</summary>
public class MolakErrorOverlay : MonoBehaviour
{
    static MolakErrorOverlay instance;
    static readonly List<string> lines = new List<string>();
    GUIStyle st;
    float builtU = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap ()
    {
        if (!Application.isEditor && !Debug.isDebugBuild) return; // 릴리스 빌드에서는 비활성
        if (instance != null) return;
        var go = new GameObject("MolakErrorOverlay");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<MolakErrorOverlay>();
        Application.logMessageReceived += OnLog;
    }

    static void OnLog (string msg, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        if (msg.Contains("MCP-FOR-UNITY")) return;
        string first = msg.Split('\n')[0];
        string where = "";
        if (!string.IsNullOrEmpty(stack))
        {
            foreach (var l in stack.Split('\n'))
                if (l.Contains("Molak") || l.Contains("Naninovel")) { where = l.Trim(); break; }
            if (where.Length == 0) where = stack.Split('\n')[0].Trim();
        }
        if (where.Length > 90) where = where.Substring(0, 90);
        if (first.Length > 160) first = first.Substring(0, 160);
        lines.Add("[" + type + "] " + first + (where.Length > 0 ? "\n   " + where : ""));
        if (lines.Count > 6) lines.RemoveAt(0);
        try { System.IO.File.AppendAllText(System.IO.Path.Combine(Application.persistentDataPath, "molak_errors.txt"), System.DateTime.Now.ToString("HH:mm:ss ") + msg + "\n" + stack + "\n"); } catch { }
    }

    static Texture2D Px { get { return MolakGui.Px; } }

    void OnGUI ()
    {
        if (lines.Count == 0) return;
        float u = MolakGui.U;
        if (st == null || !Mathf.Approximately(builtU, u))
        {
            builtU = u;
            st = new GUIStyle(GUI.skin.label){ fontSize = Mathf.RoundToInt(22*u), wordWrap = true, richText = false, alignment = TextAnchor.UpperLeft };
            MolakGui.Ink(st, new Color(1f, 0.55f, 0.5f, 1f));
        }
        string text = string.Join("\n", lines.ToArray());
        float w = Screen.width - 24*u;
        float h = st.CalcHeight(new GUIContent(text), w) + 16*u;
        var r = new Rect(12*u, Screen.height - h - 12*u, w, h);
        var g = GUI.color; GUI.color = new Color(0f, 0f, 0f, 0.82f); GUI.DrawTexture(r, Px); GUI.color = g;
        GUI.Label(new Rect(r.x + 8*u, r.y + 8*u, r.width - 16*u, r.height - 16*u), text, st);
        if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition)) { lines.Clear(); Event.current.Use(); }
    }
}
