#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 0.5.0 자체 실행기 점검(편집기 전용, 걷어 낼 때 같이 지운다).
///   (식 평가기는 착수 때 나니노벨 평가기와 12,156번 견줘 어긋남 0 을 확인했다 — 그 점검은 나니노벨과 함께 걷어 냈다.)
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
}
#endif
