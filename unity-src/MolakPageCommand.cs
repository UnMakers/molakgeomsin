using MolakVn;
using UnityEngine;

/// <summary>@page — 시간·장소가 크게 건너뛰는 지점. 직전 본문은 탭을 기다리고, 다음 본문은 빈 화면에서 시작한다(MolakFlow가 처리).</summary>
[Alias("page")]
public class PageCommand : Command
{
    public override async Awaitable Execute (ExecutionContext ctx)
    {
        MolakFlow.Break();
        if (MolakPlayMode.SkipAll) return;
        MolakArt.Dip();   // 0.4.85 장면이 바뀌는 쪽 넘김: 그림 창을 잠깐 먹으로
        // 0.4.86 기준: 덮개가 걷히기 전엔 다음 글을 찍지 않는다(예전엔 덮개를 부르고 바로 넘어가 첫 글이 0.8초 덮개 밑에서 찍혔다).
        //        뒤에 @art 가 오면 그쪽(ArtCommand)이 기다린다. 장면이 바뀌어 번짐이 돌면 덮개는 생략되니 곧 풀린다. 건너뛰기 중엔 기다리지 않는다.
        var sp = Engine.GetService<IScriptPlayer>();
        if (sp != null && sp.Skipping) return;
        try { var tr = sp.MainTrack; var pl = tr.Playlist; int ni = tr.PlayedIndex + 1; if (pl != null && ni < pl.Count && pl[ni] is ArtCommand) return; } catch { }
        float until = Time.realtimeSinceStartup + 0.7f;
        while (MolakArt.Dipping && Time.realtimeSinceStartup < until) await Awaitable.NextFrameAsync();
    }
}
