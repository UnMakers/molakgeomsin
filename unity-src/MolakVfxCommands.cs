using Naninovel;
using UnityEngine;

[Alias("vfx")]
public class VfxCommand : Command, IMolakMoment
{
    public StringParameter Kind;
    /// <summary>0.4.90 인장이 찍히는 종류(경지 돌파 tier · 경지의 밤 bt_*)만 글자리를 덮는다. 빛·흔들림·날씨는 글 위에 얹힐 뿐이라 지나친다.</summary>
    public bool CoversText { get { var k = Assigned(Kind) ? Kind.Value : ""; return k == "tier" || k.StartsWith("bt_"); } }
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
    /// <summary>0.4.88 곁에 함께 세울 사람(두 사람 세우기). none 이면 혼자.</summary>
    [Alias("with")] public StringParameter WithId;
    [Alias("off")] public BooleanParameter Off;
    public override async Awaitable Execute (ExecutionContext ctx)
    {
        if (Assigned(Off) && Off.Value) MolakArt.Clear();
        if (Assigned(SceneId)) MolakArt.Scene(SceneId.Value);
        if (Assigned(CharId)) MolakArt.Char(CharId.Value);
        if (Assigned(WithId)) MolakArt.With(WithId.Value);
        // 0.4.85 바로 이어지는 @art 도 지금 함께 세운다(@art scene:morning → @art char:hajinmu). 둘 사이에서 쪽 넘김 탭을 기다리면
        //   번짐이 바탕 그림으로 한 번, 탭 뒤에 인물 자리 그림으로 또 한 번 돌았다. 뒤의 @art 가 실행될 땐 이미 같은 그림이라 아무것도 안 한다.
        try
        {
            var sp0 = Engine.GetService<IScriptPlayer>(); var tr0 = sp0 != null ? sp0.MainTrack : null; var pl0 = tr0 != null ? tr0.Playlist : null;
            for (int i = tr0 != null ? tr0.PlayedIndex + 1 : -1, n = 0; pl0 != null && i >= 0 && i < pl0.Count && n < 3; i++, n++)
            {
                var nx = pl0[i] as ArtCommand; if (nx == null) break;
                if (nx.ConditionalExpression.HasValue) break;   // 조건이 붙은 줄은 제때 실행되게 둔다
                if (nx.Off != null && nx.Off.HasValue && nx.Off.Value) break;
                if (nx.SceneId != null && nx.SceneId.HasValue && !nx.SceneId.DynamicValue) MolakArt.Scene(nx.SceneId.Value);
                if (nx.CharId != null && nx.CharId.HasValue && !nx.CharId.DynamicValue) MolakArt.Char(nx.CharId.Value);
                if (nx.WithId != null && nx.WithId.HasValue && !nx.WithId.DynamicValue) MolakArt.With(nx.WithId.Value);
            }
        }
        catch { }
        await Awaitable.NextFrameAsync();
        // 0.4.41: 그림이 먼저 — 새 그림이 거의 다 떠오를 때까지(0.4.42부터 최대 0.3초) 글을 붙들어 둔다.
        //         예전엔 글이 바로 찍히기 시작해 그림이 대사 뒤에 바뀌는 것처럼 보였다. 읽은 글 넘기기·자동 시험 중엔 기다리지 않는다.
        var sp = Engine.GetService<IScriptPlayer>();
        bool skip = (sp != null && sp.Skipping) || MolakBlackout.Auto;
        // 0.4.85 바로 다음 줄도 @art 면(@art scene:morning → @art char:hajinmu) 기다리지 않는다 — 기다리는 동안 번짐이 바탕 그림으로 한 번, 인물 자리 그림으로 또 한 번 따로 돌았다
        bool nextArt = false;
        try { var tr = sp != null ? sp.MainTrack : null; var pl = tr != null ? tr.Playlist : null; int ni = tr != null ? tr.PlayedIndex + 1 : -1; if (pl != null && ni >= 0 && ni < pl.Count && pl[ni] is ArtCommand) { skip = true; nextArt = true; } } catch { }
        float until = Time.realtimeSinceStartup + 0.3f;
        while (!skip && MolakArt.Settling && Time.realtimeSinceStartup < until)
            await Awaitable.NextFrameAsync();
        // 0.4.85 장면이 바뀌는 쪽(@page 뒤 새 장면): 먹이 걷힐 때까지(최대 0.6초) 첫 글을 붙든다 — 앞 장면 끝 대사에서 바로 넘어가지 않게
        until = Time.realtimeSinceStartup + 0.7f;
        bool skipDip = (sp != null && sp.Skipping) || nextArt;   // 0.4.86 덮개 기다림은 자동 시험 중에도 한다(시험이 실제와 같게)
        while (!skipDip && MolakArt.Dipping && Time.realtimeSinceStartup < until)
            await Awaitable.NextFrameAsync();
    }
}

/// <summary>@chapter text:"꿰맨 자리" sub:"소아" desc:"한 줄 설명(선택)" — 이야기 마디가 시작될 때 띠 그림 위에 마디 이름을 붓 글씨로 잠깐 띄운다(입력은 막지 않는다).
/// 같은 생에 같은 마디를 다시 보거나 읽은 글 넘기기 중이면 띄우지 않는다.</summary>
/// <summary>0.4.83 @spk id:soa — 바로 다음 한 줄을 누가 말하는지 적는다(그림 속 인물을 또렷이·이름 인장·선택지 위 요약에 쓴다).
/// id 는 인물 그림 이름(soa, hajinmu …)의 앞부분, 주인공 대사는 me. 적지 않은 줄은 예전처럼 글로 어림한다.</summary>
[Alias("spk")]
public class SpeakerCommand : Command
{
    [RequiredParameter] public StringParameter Id;
#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        MolakFlow.PendingSpeaker = Id.Value;
    }
}

[Alias("chapter")]
public class ChapterCommand : Command, IMolakMoment
{
    public bool CoversText => true;   // 0.4.90 글자리를 덮는다(IMolakMoment)
    public StringParameter Text;
    public StringParameter Sub;
    /// <summary>0.4.82 그림 화면의 마디 이름 아래 회색 작은 글씨 한 줄(없으면 비운다).</summary>
    public StringParameter Desc;
#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        MolakStoryBook.Mark(Assigned(Text) ? Text.Value : "", Assigned(Sub) ? Sub.Value : "", this);   // 0.4.78 이야기 모드 책갈피
        MolakBgm.Release();   // 0.4.94 새 마디 — 앞 장면이 쥔 곡을 놓는다
        var sp = Engine.GetService<IScriptPlayer>();
        if (sp != null && sp.Skipping) return;
        MolakVfx.Chapter(Assigned(Text) ? Text.Value : "", Assigned(Sub) ? Sub.Value : "", Assigned(Desc) ? Desc.Value : "");
        // 0.4.83 마디 이름을 누를 때까지 다음 글을 찍지 않는다 — 전에는 마디 이름 뒤에서 글이 이미 다 나와 있어서, 걷으면 한꺼번에 보였다(언메이커)
        while (MolakVfx.ChapterHolding)
        {
            if (sp != null && sp.Skipping) break;
            if (MolakPlayMode.SkipAll) break;
            if (!Engine.Initialized || (sp != null && sp.MainTrack != null && !sp.MainTrack.Playing)) break;   // 불러오기·처음으로 등으로 멈췄으면
            await Awaitable.NextFrameAsync();
        }
        // 0.4.85 기준(본문이 가려진 동안 글이 드러나지 않는다): 마디 이름이 걷히는 0.6초 동안에도 글자리는 아직 덮여 있다 — 거의 다 걷힌 뒤에 글을 찍는다
        float until = Time.realtimeSinceStartup + 0.8f;
        while (MolakVfx.ChapterCover > 0.15f && Time.realtimeSinceStartup < until && !(sp != null && sp.Skipping) && !MolakPlayMode.SkipAll)
            await Awaitable.NextFrameAsync();
    }
}

/// <summary>0.4.85 @gesture kind:lunge|recoil|tremble|sink|bounce|nod|turn — 서 있는 인물의 몸짓(서술 낱말로도 저절로 나온다).</summary>
[Alias("gesture")]
public class GestureCommand : Command
{
    public StringParameter Kind;
#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        if (Assigned(Kind)) MolakArt.Gesture(Kind.Value);
    }
}

/// <summary>0.4.94 @bgm mood:boss|true|sorrow|tension|soa|…|hush|auto — 그 장면 안에서 곡을 쥔다(MolakBgm.Hold). hush 는 소리를 거두고, auto 는 놓는다.</summary>
[Alias("bgm")]
public class BgmCommand : Command
{
    public StringParameter Mood;
#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        MolakBgm.Hold(Assigned(Mood) ? Mood.Value : "auto");
    }
}
