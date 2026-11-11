using Naninovel;
using UnityEngine;

[Alias("vfx")]
public class VfxCommand : Command
{
    public StringParameter Kind;
    public override async Awaitable Execute (ExecutionContext ctx)
    {
        var k = Assigned(Kind) ? Kind.Value : "none";
        switch (k)
        {
            case "blood": MolakVfx.Blood(); break;
            case "gold": MolakVfx.Gold(); break;
            case "curse": MolakVfx.CursePulse(); break;
            case "shake": MolakVfx.Shake(14f); break;
            case "rain": MolakVfx.Rain(true); break;
            case "rainoff": MolakVfx.Rain(false); break;
            case "bloodrain": MolakVfx.Weather("bloodrain"); break;
            case "snow": MolakVfx.Weather("snow"); break;
            case "petals": MolakVfx.Weather("petals"); break;
            case "embers": MolakVfx.Weather("embers"); break;
            case "clear": MolakVfx.Weather(""); break;
        }
        await Awaitable.NextFrameAsync();
    }
}

[Alias("scene")]
public class SceneCommand : Command
{
    public StringParameter Id;
    public override async Awaitable Execute (ExecutionContext ctx)
    {
        if (Assigned(Id)) { MolakVfx.Scene(Id.Value); MolakArt.Scene(Id.Value); }
        await Awaitable.NextFrameAsync();
    }
}


[Alias("art")]
public class ArtCommand : Command
{
    [Alias("scene")] public StringParameter SceneId;
    [Alias("char")] public StringParameter CharId;
    [Alias("off")] public BooleanParameter Off;
    public override async Awaitable Execute (ExecutionContext ctx)
    {
        if (Assigned(Off) && Off.Value) MolakArt.Clear();
        if (Assigned(SceneId)) MolakArt.Scene(SceneId.Value);
        if (Assigned(CharId)) MolakArt.Char(CharId.Value);
        await Awaitable.NextFrameAsync();
    }
}
