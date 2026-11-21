using System.Collections.Generic;

/// <summary>
/// 0.4.88 인물 표 — Resources/MolakData/cast.txt 한 곳에서 이름·자리 그림·갈래를 읽는다.
/// 예전에는 MolakBridge(CharName·SealName)와 MolakArt(HomeDay·HomeNight)에 같은 id를 네 번 손으로 적었다.
/// 새 인물이나 표정 그림이 생기면 cast.txt 에 한 줄만 더한다.
/// 형식: id = 이름 | 낮 자리 | 밤 자리 | 주연/조연
/// </summary>
public static class MolakCast
{
    static Dictionary<string, string> main, other, day, night;

    public static Dictionary<string, string> MainNames { get { Load(); return main; } }
    public static Dictionary<string, string> OtherNames { get { Load(); return other; } }
    public static Dictionary<string, string> HomeDay { get { Load(); return day; } }
    public static Dictionary<string, string> HomeNight { get { Load(); return night; } }

    static void Load ()
    {
        if (main != null) return;
        main = new Dictionary<string, string>(); other = new Dictionary<string, string>();
        day = new Dictionary<string, string>(); night = new Dictionary<string, string>();
        foreach (var kv in MolakData.Map("cast"))
        {
            var c = kv.Value.Split('|');
            string Col (int i) => i < c.Length ? c[i].Trim() : "";
            string nm = Col(0), d = Col(1), n = Col(2), kind = Col(3);
            if (nm.Length > 0) (kind == "주연" ? main : other)[kv.Key] = nm;
            if (d.Length > 0) day[kv.Key] = d;
            if (n.Length > 0) night[kv.Key] = n;
        }
    }
}
