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
            case "fireon": MolakVfx.Weather("inferno"); break;   // 0.4.66 장면이 바뀌어도 불길을 이어 간다(흔들림 없이) — 뒷산으로 달아나도 산장이 타고 있다
            case "clear": MolakVfx.Weather(""); break;
            case "tier": MolakVfx.Gold(); MolakVfx.Shake(6f); MolakVfx.Stamp("境", MolakStats.Str("tiername", ""), true); MolakSfx.Bell(); break;   // 0.4.33 경지 돌파
            // 0.4.66 경지의 밤: 어떤 길로 넘었느냐에 따라 빛·소리·인장이 다르다(세 박자로 터진다)
            case "bt_sword": Breakthrough(new Color(1f, 0.86f, 0.42f, 0.55f), "劍", "sword"); break;
            case "bt_qi": Breakthrough(new Color(0.70f, 0.86f, 1f, 0.50f), "氣", "qi"); break;
            case "bt_hyeop": Breakthrough(new Color(1f, 0.96f, 0.86f, 0.55f), "俠", "hyeop"); break;
            case "bt_blood": Breakthrough(new Color(0.75f, 0.06f, 0.05f, 0.55f), "殺", "blood"); break;
            case "bt_curse": Breakthrough(new Color(0.42f, 0.18f, 0.62f, 0.60f), "邪", "curse"); break;
            case "crowd": MolakVfx.Shake(3f); MolakSfx.Play("page_turn", 0.5f, 0.7f); break;   // 구경꾼이 술렁인다
        }
        await Awaitable.NextFrameAsync();
    }

    /// <summary>경지 돌파 세 박자: ① 빛이 몸 안에서 터지고 흔들림 ② 숨 한 번 뒤 인장(길의 한 글자 + 경지 이름) ③ 종(또는 심장·문양의 숨)으로 닫는다.</summary>
    static async void Breakthrough (Color c, string hanja, string path)
    {
        string tier = MolakStats.Str("tiername", "");
        MolakVfx.Flash(new Color(c.r, c.g, c.b, c.a * 0.6f), 0.5f); MolakVfx.Shake(path == "blood" || path == "curse" ? 16f : 9f);
        if (path == "sword") MolakSfx.Clash(true); else if (path == "blood") MolakSfx.Heart(); else if (path == "curse") MolakSfx.CurseBreath(); else MolakSfx.Draw();
        try { await Awaitable.WaitForSecondsAsync(0.7f); } catch { return; }
        MolakVfx.Flash(c, path == "qi" ? 2.4f : 1.6f);
        if (path == "curse") MolakVfx.CursePulse();
        MolakVfx.Stamp(hanja, tier.Length > 0 ? tier + " — 마디가 닫혔다" : "마디가 닫혔다", true);
        if (path == "sword") MolakVfx.Shake(12f);
        try { await Awaitable.WaitForSecondsAsync(0.9f); } catch { return; }
        if (path == "hyeop") MolakSfx.TrueBell(); else if (path == "blood") { MolakSfx.Heart(); MolakVfx.Flash(new Color(0.5f, 0f, 0f, 0.3f), 0.8f); } else if (path == "curse") MolakSfx.Death(); else MolakSfx.Bell();
        if (path == "sword" || path == "qi" || path == "hyeop") MolakVfx.Gold();
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
        // 0.4.41: 그림이 먼저 — 새 그림이 거의 다 떠오를 때까지(0.4.42부터 최대 0.3초) 글을 붙들어 둔다.
        //         예전엔 글이 바로 찍히기 시작해 그림이 대사 뒤에 바뀌는 것처럼 보였다. 읽은 글 넘기기·자동 시험 중엔 기다리지 않는다.
        var sp = Engine.GetService<IScriptPlayer>();
        bool skip = (sp != null && sp.Skipping) || MolakBlackout.Auto;
        float until = Time.realtimeSinceStartup + 0.3f;
        while (!skip && MolakArt.Settling && Time.realtimeSinceStartup < until)
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
