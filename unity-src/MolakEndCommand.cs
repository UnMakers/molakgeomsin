using Naninovel;
using UnityEngine;

[Alias("ending")]
public class EndingCommand : Command
{
#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        MolakBgm.Release();   // 0.4.94 대본이 쥔 곡은 말로에서 놓는다
        var id = MolakEnd.Resolve();
        var e = MolakEnd.Find(id);
        // 0.4.70 말로마다 곡: 합벽은 진말로 곡, 살아서 떠나는 말로는 밝은 곡, 나머지는 회귀의 곡
        MolakBgm.Mood(id == "hapbyeok" || id == "myeonhyang" || id == "unhae" || id == "myeongga" ? "true" : (id == "wonhaeng" || id == "donghaeng" || id == "sicheon" || id == "doju" || id == "jaegeon" || id == "muhyang" || id == "hwansok" || id == "mumyeong" || id == "unrak") ? "ending" : "end");
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
