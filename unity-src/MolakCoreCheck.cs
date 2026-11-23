#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using Naninovel;
using UnityEngine;

/// <summary>
/// 0.5.0 자체 실행기 점검(편집기 전용, 걷어 낼 때 같이 지운다).
///   ExprVsNaninovel(trials): 대본의 모든 조건식·@set 식을 나니노벨 평가기와 우리 평가기(MolakExpr)로 같은 변수 값에서 풀어 견준다.
///   ParseStats(): 대본 전부를 MolakScript 로 읽어 줄 수를 센다(tools/engine/nani_ir.py 의 수와 같아야 한다).
/// </summary>
public static class MolakCoreCheck
{
    static readonly string[] Scripts = { "Entry", "Molak01", "Molak02", "Molak03", "Molak04", "Molak05", "Molak06", "Molak07", "Molak08", "Molak09", "Molak10", "Molak11", "Molak12", "Molak13", "Molak14", "Molak15", "Molak16", "Molak17", "Molak18", "Molak19", "Molak20", "Molak21", "Molak22", "MolakDuel", "MolakEvents", "MolakOver" };

    public static string ParseStats ()
    {
        MolakScript.ClearCache();
        int text = 0, cmd = 0, choice = 0, label = 0, bad = 0; var sb = new StringBuilder(); var names = new SortedDictionary<string, int>();
        foreach (var n in Scripts)
        {
            var s = MolakScript.Get(n); if (s == null) { sb.AppendLine("없음 " + n); continue; }
            foreach (var l in s.Lines)
            {
                if (l.Kind == MolakLineKind.Text) text++;
                else if (l.Kind == MolakLineKind.Label) label++;
                else if (l.Kind == MolakLineKind.Choice) { choice++; if (l.Arg("goto") == null || string.IsNullOrEmpty(l.Pos)) { bad++; sb.AppendLine("선택지 꼴 " + n + ":" + l.LineNo); } }
                else { cmd++; int c; names.TryGetValue(l.Name, out c); names[l.Name] = c + 1; }
                // 이동 대상 확인
                string t = l.Kind == MolakLineKind.Choice ? l.Arg("goto") : (l.Kind == MolakLineKind.Cmd && (l.Name == "goto" || l.Name == "gosub")) ? l.Pos : null;
                if (t != null)
                {
                    int h = t.IndexOf('#'); string sn = h < 0 ? t : t.Substring(0, h), lb = h < 0 ? null : t.Substring(h + 1);
                    var ts = string.IsNullOrEmpty(sn) ? s : MolakScript.Get(sn);
                    if (ts == null || (!string.IsNullOrEmpty(lb) && !ts.Labels.ContainsKey(lb))) { bad++; sb.AppendLine("이동 대상 없음 " + n + ":" + l.LineNo + " " + t); }
                }
            }
        }
        sb.Insert(0, "글 " + text + " · 명령 " + cmd + " · 선택지 " + choice + " · 라벨 " + label + " · 명령 종류 " + names.Count + " · 어긋남 " + bad + "\n");
        return sb.ToString();
    }

    public static string ExprVsNaninovel (int trials, int seed)
    {
        var vars = Engine.GetService<ICustomVariableManager>();
        if (vars == null) return "나니노벨이 안 떠 있다(플레이 중에 부른다)";
        var conds = new HashSet<string>(); var sets = new HashSet<string>(); var ids = new HashSet<string>(); var strIds = new HashSet<string>();
        var idRx = new System.Text.RegularExpressions.Regex(@"[A-Za-z_][A-Za-z_0-9]*");
        foreach (var n in Scripts)
        {
            var s = MolakScript.Get(n); if (s == null) continue;
            foreach (var l in s.Lines)
            {
                if (l.Cond != null) conds.Add(l.Cond);
                if (l.Kind == MolakLineKind.Cmd && l.Name == "set" && l.Pos != null)
                {
                    int eq = l.Pos.IndexOf('='); string name = l.Pos.Substring(0, eq), rhs = l.Pos.Substring(eq + 1);
                    if (rhs.IndexOf('"') >= 0) strIds.Add(name);
                    else if (rhs.IndexOf("random", System.StringComparison.Ordinal) < 0) sets.Add(rhs);
                    ids.Add(name);
                }
            }
        }
        foreach (var c in conds)
        {
            string bare = System.Text.RegularExpressions.Regex.Replace(c, "\"[^\"]*\"", "");
            foreach (System.Text.RegularExpressions.Match m in idRx.Matches(bare)) ids.Add(m.Value);
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(c, @"([A-Za-z_][A-Za-z_0-9]*)\s*[!=]=\s*""")) strIds.Add(m.Groups[1].Value);
        }
        foreach (var c in sets) foreach (System.Text.RegularExpressions.Match m in idRx.Matches(c)) ids.Add(m.Value);
        ids.Remove("random"); ids.Remove("true"); ids.Remove("false");
        string[] words = { "none", "", "희생", "soa", "amh", "x" };
        var rnd = new System.Random(seed); int diff = 0, total = 0; var sb = new StringBuilder();
        bool wasOn = MolakCore.On;
        for (int t = 0; t < trials; t++)
        {
            foreach (var id in ids)
            {
                if (strIds.Contains(id)) { string w = words[rnd.Next(words.Length)]; vars.SetVariableValue(id, new CustomVariableValue(w)); MolakVars.Set(id, w); }
                else { float f = t == 0 ? 0f : rnd.Next(0, 4) == 0 ? rnd.Next(0, 100) : rnd.Next(0, 4); vars.SetVariableValue(id, new CustomVariableValue(f)); MolakVars.Set(id, f); }
            }
            foreach (var c in conds)
            {
                total++; bool a, b; string ea = null;
                try { a = Naninovel.ExpressionEvaluator.Evaluate<bool>(c); } catch (System.Exception e) { a = false; ea = e.Message; }
                b = MolakExpr.True(c);
                if (ea != null || a != b) { diff++; if (diff <= 25) sb.AppendLine("조건 " + c + " → 나니 " + (ea ?? a.ToString()) + " · 우리 " + b); }
            }
            foreach (var c in sets)
            {
                total++; float a, b; string ea = null;
                try { a = Naninovel.ExpressionEvaluator.Evaluate<float>(c); } catch (System.Exception e) { a = 0f; ea = e.Message; }
                b = MolakExpr.Number(c);
                if (ea != null || Mathf.Abs(a - b) > 0.0001f) { diff++; if (diff <= 25) sb.AppendLine("식 " + c + " → 나니 " + (ea ?? a.ToString()) + " · 우리 " + b); }
            }
        }
        MolakCore.On = wasOn;
        sb.Insert(0, "조건식 " + conds.Count + " · @set 식 " + sets.Count + " · 변수 " + ids.Count + "(글자 " + strIds.Count + ") · 견준 것 " + total + " · 어긋남 " + diff + "\n");
        return sb.ToString();
    }
}
#endif
