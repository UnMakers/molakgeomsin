using System.Collections.Generic;

/// <summary>
/// 0.4.91 맡은 일 — Resources/MolakData/quests.txt 를 읽어 행낭의 「맡은 일」 칸을 채운다(언메이커: 부탁을 하고 수락하면 다음에 올 때 뭔가 하는 퀘스트 형식).
/// 형식: id = 제목 | 받은 깃발 | 끝난 깃발 | 지금 할 일 | 맡긴 사람 | 끝 날(0 = 끝없음)
/// 기준: 받은 깃발이 서 있고, 끝난 깃발이 서지 않았고, 끝 날을 넘기지 않았으면 「맡은 일」이다.
/// 새 맡은 일은 사건 둘(부탁 · 이어지는 쪽)과 이 파일 한 줄이면 된다 — 코드는 고치지 않는다.
/// </summary>
public static class MolakQuest
{
    public struct Quest { public string Id, Title, Took, Done, Todo, Giver; public float EndDay; }
    static List<Quest> all;
    static MolakBag.Entry[] entries;

    public static IReadOnlyList<Quest> All { get { Load(); return all; } }

    /// <summary>행낭 칸에 그릴 꼴(Flag = 받은 깃발 — 행낭이 얻은 순서를 이 깃발로 잰다).</summary>
    public static MolakBag.Entry[] Entries { get { Load(); return entries; } }

    static void Load ()
    {
        if (all != null) return;
        all = new List<Quest>();
        foreach (var kv in MolakData.Map("quests"))
        {
            var c = kv.Value.Split('|');
            string Col (int i) => i < c.Length ? c[i].Trim() : "";
            float end; float.TryParse(Col(5), out end);
            if (Col(1).Length == 0) continue;
            all.Add(new Quest { Id = kv.Key, Title = Col(0), Took = Col(1), Done = Col(2), Todo = Col(3), Giver = Col(4), EndDay = end });
        }
        entries = new MolakBag.Entry[all.Count];
        for (int i = 0; i < all.Count; i++)
        {
            var q = all[i];
            entries[i] = new MolakBag.Entry(q.Took, q.Title, q.Todo, q.Giver.Length > 0 ? "맡긴 사람 · " + q.Giver : "");
        }
    }

    /// <summary>받은 깃발로 그 맡은 일이 지금 진행 중인가.</summary>
    public static bool Active (string tookFlag)
    {
        Load();
        foreach (var q in all)
        {
            if (q.Took != tookFlag) continue;
            if (MolakStats.Get(q.Took) <= 0f) return false;
            if (q.Done.Length > 0 && MolakStats.Get(q.Done) > 0f) return false;
            if (q.EndDay > 0f && MolakStats.Get("day") > q.EndDay) return false;
            return true;
        }
        return false;
    }
}
