using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 글로 된 표(대사·문장 묶음)를 코드 밖 Resources/MolakData/*.txt 에서 읽는다.
/// 코드 안에 긴 문장 표를 박아 두면 문장 하나 고치는 데도 스크립트를 열어야 했다 — 이제는 글 파일만 고치면 된다.
/// 형식: '#' 로 시작하는 줄과 빈 줄은 건너뛴다. Map 은 "열쇠 = 값", List 는 한 줄에 하나.
/// </summary>
public static class MolakData
{
    const string Folder = "MolakData/";
    static readonly Dictionary<string, Dictionary<string, string>> maps = new Dictionary<string, Dictionary<string, string>>();
    static readonly Dictionary<string, List<string>> lists = new Dictionary<string, List<string>>();

    static IEnumerable<string> Rows (string name)
    {
        var ta = Resources.Load<TextAsset>(Folder + name);
        if (ta == null) { Debug.LogWarning("[MolakData] 없음: Resources/" + Folder + name + ".txt"); yield break; }
        foreach (var raw in ta.text.Split('\n'))
        {
            var line = raw.TrimEnd('\r').Trim();
            if (line.Length == 0 || line[0] == '#') continue;
            yield return line;
        }
    }

    public static IReadOnlyDictionary<string, string> Map (string name)
    {
        if (maps.TryGetValue(name, out var m)) return m;
        m = new Dictionary<string, string>();
        foreach (var line in Rows(name))
        {
            int eq = line.IndexOf('=');
            if (eq <= 0) continue;
            m[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
        }
        maps[name] = m;
        return m;
    }

    public static IReadOnlyList<string> List (string name)
    {
        if (lists.TryGetValue(name, out var l)) return l;
        l = new List<string>(Rows(name));
        lists[name] = l;
        return l;
    }
}
