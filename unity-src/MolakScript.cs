using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 0.5.0 자체 실행기 — 대본(.nani) 읽기. 나니노벨 없이 몰락검신 대본을 그대로 읽는다(tools/engine/nani_ir.py 와 같은 문법).
///   # 라벨 · ; 주석 · @명령 [위치값] 이름:값 … [if:조건] · @choice "글" goto:… [if:…] · 그 밖의 줄은 본문(글).
/// 대본은 Resources/MolakNani/이름.txt (편집기에서는 Assets/Scenario/이름.nani 를 바로 읽는다 — 고친 대본이 곧장 돈다).
/// </summary>
public enum MolakLineKind { Label, Text, Cmd, Choice }

public class MolakLine
{
    public MolakLineKind Kind;
    public string Name;                         // 명령 이름 · 라벨 이름
    public string Pos;                          // 위치값(@set 의 식, @goto 의 자리, @print 의 글, @wait 의 초) · 본문 글
    public Dictionary<string, string> Args;     // 이름:값 (if 는 빼고)
    public string Cond;                         // if: 조건(없으면 null)
    public int LineNo;                          // 1부터
    public string Arg (string k, string fallback = null) { string v; return Args != null && Args.TryGetValue(k, out v) ? v : fallback; }
    public bool Has (string k) { return Args != null && Args.ContainsKey(k); }
}

public class MolakScript
{
    public string Name;
    public readonly List<MolakLine> Lines = new List<MolakLine>();
    public readonly Dictionary<string, int> Labels = new Dictionary<string, int>();   // 라벨 → 그 라벨 줄의 번호(Lines 안)

    static readonly Dictionary<string, MolakScript> cache = new Dictionary<string, MolakScript>();
    public static void ClearCache () { cache.Clear(); }

    public static MolakScript Get (string name)
    {
        MolakScript s;
        if (cache.TryGetValue(name, out s)) return s;
        string src = LoadSource(name);
        if (src == null) { Debug.LogError("[Molak] 대본이 없다: " + name); return null; }
        s = Parse(name, src); cache[name] = s; return s;
    }

    public static string LoadSource (string name)
    {
#if UNITY_EDITOR
        string p = System.IO.Path.Combine(Application.dataPath, "Scenario", name + ".nani");
        if (System.IO.File.Exists(p)) return System.IO.File.ReadAllText(p, Encoding.UTF8);
#endif
        var ta = Resources.Load<TextAsset>("MolakNani/" + name);
        return ta != null ? ta.text : null;
    }

    public static MolakScript Parse (string name, string src)
    {
        var s = new MolakScript { Name = name };
        if (src.Length > 0 && src[0] == '﻿') src = src.Substring(1);
        var rows = src.Split('\n');
        for (int i = 0; i < rows.Length; i++)
        {
            string raw = rows[i].TrimEnd('\r'), t = raw.Trim();
            if (t.Length == 0 || t[0] == ';') continue;
            if (t[0] == '#')
            {
                string lb = t.Substring(1).Trim();
                s.Labels[lb] = s.Lines.Count;
                s.Lines.Add(new MolakLine { Kind = MolakLineKind.Label, Name = lb, LineNo = i + 1 });
                continue;
            }
            if (t[0] == '@')
            {
                var ln = ParseCommand(t); ln.LineNo = i + 1;
                s.Lines.Add(ln);
                continue;
            }
            s.Lines.Add(new MolakLine { Kind = MolakLineKind.Text, Pos = Unescape(raw), LineNo = i + 1 });
        }
        return s;
    }

    /// <summary>'@이름 [위치값] 키:값 키:"값"' — 위치값은 첫 낱말이 키:값 꼴이 아닐 때.</summary>
    static MolakLine ParseCommand (string t)
    {
        int p = 1, n = t.Length;
        while (p < n && (char.IsLetterOrDigit(t[p]) || t[p] == '_')) p++;
        var ln = new MolakLine { Kind = MolakLineKind.Cmd, Name = t.Substring(1, p - 1) };
        while (p < n && t[p] == ' ') p++;
        if (p < n && !StartsWithKey(t, p))
        {
            if (t[p] == '"') ln.Pos = ReadQuoted(t, ref p);
            else ln.Pos = ReadBare(t, ref p);
        }
        while (p < n)
        {
            while (p < n && t[p] == ' ') p++;
            if (p >= n) break;
            int ks = p; while (p < n && (char.IsLetterOrDigit(t[p]) || t[p] == '_')) p++;
            if (p >= n || t[p] != ':' || p == ks) { while (p < n && t[p] != ' ') p++; continue; }   // 키:값 꼴이 아니면 지나친다
            string key = t.Substring(ks, p - ks); p++;
            string val;
            if (p < n && t[p] == '"') val = ReadQuoted(t, ref p);
            else val = ReadBare(t, ref p);
            if (key == "if") ln.Cond = val;
            else { if (ln.Args == null) ln.Args = new Dictionary<string, string>(); ln.Args[key] = val; }
        }
        if (ln.Name == "choice") ln.Kind = MolakLineKind.Choice;
        return ln;
    }

    static bool StartsWithKey (string t, int p)
    {
        int q = p; while (q < t.Length && (char.IsLetterOrDigit(t[q]) || t[q] == '_')) q++;
        return q > p && q < t.Length && t[q] == ':';
    }

    /// <summary>따옴표로 시작하지 않는 값: 빈칸에서 끝난다. 다만 값 안의 "…" 속 빈칸은 값의 일부다(if:deathcause!="소아의 검" · @set x="또 다른").</summary>
    static string ReadBare (string t, ref int p)
    {
        int s0 = p; bool q = false;
        while (p < t.Length)
        {
            char c = t[p];
            if (c == '\\' && p + 1 < t.Length) { p += 2; continue; }
            if (c == '"') q = !q;
            else if (c == ' ' && !q) break;
            p++;
        }
        return t.Substring(s0, p - s0);
    }

    /// <summary>따옴표 값: \" 는 따옴표, \\ 는 역슬래시. p 는 닫는 따옴표 다음으로.</summary>
    static string ReadQuoted (string t, ref int p)
    {
        var sb = new StringBuilder(); p++;
        while (p < t.Length && t[p] != '"')
        {
            if (t[p] == '\\' && p + 1 < t.Length) { sb.Append(t[p + 1]); p += 2; continue; }
            sb.Append(t[p]); p++;
        }
        if (p < t.Length) p++;
        return sb.ToString();
    }

    /// <summary>본문의 역슬래시 이스케이프(\[ \] \" \{ \})를 푼다.</summary>
    public static string Unescape (string t)
    {
        if (t.IndexOf('\\') < 0) return t;
        var sb = new StringBuilder(t.Length);
        for (int i = 0; i < t.Length; i++)
        {
            if (t[i] == '\\' && i + 1 < t.Length && "[]\"{}\\".IndexOf(t[i + 1]) >= 0) { sb.Append(t[i + 1]); i++; continue; }
            sb.Append(t[i]);
        }
        return sb.ToString();
    }
}
