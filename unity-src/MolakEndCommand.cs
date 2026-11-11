using Naninovel;
using UnityEngine;

[Alias("ending")]
public class EndingCommand : Command
{
#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        var id = MolakEnd.Resolve();
        var e = MolakEnd.Find(id);
        MolakBgm.Mood("end");
        bool isNew = false;
        var key = "g_end_" + id;
        if (MolakStats.Get(key) < 1f)
        {
            isNew = true;
            MolakStats.Set(key, 1f);
            var state = Engine.GetService<IStateManager>();
            if (state != null) await state.SaveGlobal();
        }
        MolakStats.SetStr("endtitle", e.Title);
        MolakStats.SetStr("g_lastend", id); // 다음 회차 서두 변주용
        string cause0 = MolakStats.Str("deathcause");
        string kind0 = MolakStats.Str("dkind");
        if (cause0 == "비무 패배" && !string.IsNullOrEmpty(kind0) && kind0 != "none") MolakStats.Set("g_died_" + kind0, 1f);
        MolakStats.SetStr("endhanja", e.Hanja);
        MolakStats.SetStr("endkor", e.Kor);
        MolakStats.Set("endnew", isNew ? 1f : 0f);
        MolakEnd.Show(e.Hanja, e.Kor, e.Title, isNew);
    }
}
