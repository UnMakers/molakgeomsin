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
            case "clash": MolakVfx.Clash(false); break;            // 칼이 맞부딪는 순간
            case "heavy": MolakVfx.Clash(true); break;             // 크게 맞는/베는 순간
            case "hit": MolakVfx.Blood(); MolakSfx.Hit(); MolakArt.Hurt(); break;   // 베였다
            case "beat": MolakVfx.Flash(new Color(0f, 0f, 0f, 0.55f), 0.9f); MolakSfx.Death(); break; // 숨이 멎는 한 박자(어둠이 덮였다 걷힌다)
            case "rewind": MolakVfx.Flash(new Color(0.93f, 0.90f, 0.98f, 0.6f), 1.6f); MolakVfx.Shake(6f); MolakSfx.Rewind(); break; // 시간이 거꾸로 감긴다
            case "dawn": MolakVfx.Flash(new Color(0.98f, 0.88f, 0.62f, 0.4f), 2.2f); MolakSfx.Bell(); break;   // 되감기지 않는 아침
            case "rain": MolakVfx.Rain(true); break;
            case "rainoff": MolakVfx.Rain(false); break;
            case "bloodrain": MolakVfx.Weather("bloodrain"); break;
            case "snow": MolakVfx.Weather("snow"); break;
            case "petals": MolakVfx.Weather("petals"); break;
            case "embers": MolakVfx.Weather("embers"); break;
            case "inferno": MolakVfx.Weather("inferno"); MolakVfx.Shake(8f); break;   // 불타는 산장 — 불티의 두 배와 아래에서 치솟는 열기
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

/// <summary>@chapter text:"꿰맨 자리" sub:"소아" — 이야기 마디가 시작될 때 띠 그림 위에 마디 이름을 붓 글씨로 잠깐 띄운다(입력은 막지 않는다).
/// 같은 생에 같은 마디를 다시 보거나 읽은 글 넘기기 중이면 띄우지 않는다.</summary>
[Alias("chapter")]
public class ChapterCommand : Command
{
    public StringParameter Text;
    public StringParameter Sub;
#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        var sp = Engine.GetService<IScriptPlayer>();
        if (sp != null && sp.Skipping) return;
        MolakVfx.Chapter(Assigned(Text) ? Text.Value : "", Assigned(Sub) ? Sub.Value : "");
    }
}
