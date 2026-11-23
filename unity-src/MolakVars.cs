using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// 0.5.0 자체 실행기 — 변수 저장소. 숫자(float) 또는 글자. 이름이 g_ 로 시작하면 전역(생을 넘어 남는다 — 따로 저장).
/// MolakStats 가 이 저장소를 감싼다(MolakCore.On 일 때).
/// </summary>
public static class MolakVars
{
    public struct Val
    {
        public bool IsStr; public float Num; public string Str;
        public static Val N (float n) { return new Val { Num = n }; }
        public static Val S (string s) { return new Val { IsStr = true, Str = s ?? "" }; }
        public override string ToString () { return IsStr ? Str : Fmt(Num); }
    }

    static readonly Dictionary<string, Val> map = new Dictionary<string, Val>(2048);
    public static int Count { get { return map.Count; } }
    public static IEnumerable<KeyValuePair<string, Val>> All { get { return map; } }
    /// <summary>바뀔 때마다 늘어난다(저장할 것이 있는지 볼 때).</summary>
    public static int Version;

    public static bool Has (string n) { return map.ContainsKey(n); }
    public static bool TryGet (string n, out Val v) { return map.TryGetValue(n, out v); }
    public static float Num (string n) { Val v; return map.TryGetValue(n, out v) && !v.IsStr ? v.Num : 0f; }
    public static string Str (string n, string fallback = "") { Val v; return map.TryGetValue(n, out v) && v.IsStr && v.Str != null ? v.Str : fallback; }
    public static void Set (string n, float x) { map[n] = Val.N(x); Version++; }
    public static void Set (string n, string s) { map[n] = Val.S(s); Version++; }
    public static void Set (string n, Val v) { map[n] = v; Version++; }
    public static void Remove (string n) { map.Remove(n); Version++; }
    public static bool IsGlobal (string n) { return n.StartsWith("g_", System.StringComparison.Ordinal); }

    /// <summary>글에 끼워 넣을 때의 숫자 꼴: 정수면 정수로, 아니면 소수 그대로(문화권 무관).</summary>
    public static string Fmt (float x) { return x == (float)System.Math.Round(x) ? ((long)System.Math.Round(x)).ToString(CultureInfo.InvariantCulture) : x.ToString(CultureInfo.InvariantCulture); }

    /// <summary>전역이 아닌 변수를 모두 지운다(새 판).</summary>
    public static void ClearLocals ()
    {
        var del = new List<string>();
        foreach (var k in map.Keys) if (!IsGlobal(k)) del.Add(k);
        foreach (var k in del) map.Remove(k);
        Version++;
    }
    public static void ClearAll () { map.Clear(); Version++; }

    // ---- 저장: 한 줄에 하나 — 이름<TAB>n|s<TAB>값 (값의 줄바꿈·탭·역슬래시는 \n \t \\ 로) ----
    public static string Dump (bool globals)
    {
        var sb = new StringBuilder(map.Count * 16);
        foreach (var kv in map)
        {
            if (IsGlobal(kv.Key) != globals) continue;
            sb.Append(kv.Key).Append('\t');
            if (kv.Value.IsStr) sb.Append("s\t").Append(Esc(kv.Value.Str));
            else sb.Append("n\t").Append(kv.Value.Num.ToString("R", CultureInfo.InvariantCulture));
            sb.Append('\n');
        }
        return sb.ToString();
    }

    /// <summary>Dump 로 적은 것을 읽어 덮어쓴다. replace 면 같은 갈래(전역/판)의 옛 변수를 먼저 지운다.</summary>
    public static void Load (string text, bool globals, bool replace)
    {
        if (replace)
        {
            var del = new List<string>();
            foreach (var k in map.Keys) if (IsGlobal(k) == globals) del.Add(k);
            foreach (var k in del) map.Remove(k);
        }
        if (string.IsNullOrEmpty(text)) { Version++; return; }
        foreach (var row in text.Split('\n'))
        {
            if (row.Length == 0) continue;
            int a = row.IndexOf('\t'); if (a <= 0) continue;
            int b = row.IndexOf('\t', a + 1); if (b < 0) continue;
            string name = row.Substring(0, a), ty = row.Substring(a + 1, b - a - 1), val = row.Substring(b + 1);
            if (ty == "s") map[name] = Val.S(Unesc(val));
            else { float f; if (float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out f)) map[name] = Val.N(f); }
        }
        Version++;
    }

    static string Esc (string s) { return (s ?? "").Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\t", "\\t").Replace("\r", ""); }
    static string Unesc (string s)
    {
        if (s.IndexOf('\\') < 0) return s;
        var sb = new StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '\\' && i + 1 < s.Length) { char c = s[++i]; sb.Append(c == 'n' ? '\n' : c == 't' ? '\t' : c); }
            else sb.Append(s[i]);
        }
        return sb.ToString();
    }
}
