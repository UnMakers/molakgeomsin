using Naninovel;
using UnityEngine;

[Alias("ending")]
public class EndingCommand : Command
{
#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        var vars = Engine.GetServiceOrErr<ICustomVariableManager>();
        var id = MolakEnd.Resolve();
        var e = MolakEnd.Find(id);
        MolakBgm.Mood("end");
        bool isNew = false;
        var key = "g_end_" + id;
        if (!vars.VariableExists(key) || vars.GetVariableValue(key).Number < 1f)
        {
            isNew = true;
            vars.SetVariableValue(key, new CustomVariableValue(1f));
            var state = Engine.GetService<IStateManager>();
            if (state != null) await state.SaveGlobal();
        }
        vars.SetVariableValue("endtitle", new CustomVariableValue(e.Title));
        vars.SetVariableValue("g_lastend", new CustomVariableValue(id)); // 다음 회차 서두 변주용
        string cause0 = vars.VariableExists("deathcause") ? vars.GetVariableValue("deathcause").String : "";
        string kind0 = vars.VariableExists("dkind") ? vars.GetVariableValue("dkind").String : "";
        if (cause0 == "비무 패배" && !string.IsNullOrEmpty(kind0) && kind0 != "none") vars.SetVariableValue("g_died_" + kind0, new CustomVariableValue(1f));
        vars.SetVariableValue("endhanja", new CustomVariableValue(e.Hanja));
            vars.SetVariableValue("endkor", new CustomVariableValue(e.Kor));
        MolakStats.Set("endnew", isNew ? 1f : 0f);
        MolakEnd.Show(e.Hanja, e.Kor, e.Title, isNew);
    }
}
