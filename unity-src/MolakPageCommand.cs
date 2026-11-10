using Naninovel;
using UnityEngine;

/// <summary>@page — 시간·장소가 크게 건너뛰는 지점. 직전 본문은 탭을 기다리고, 다음 본문은 빈 화면에서 시작한다(MolakFlow가 처리).</summary>
[Alias("page")]
public class PageCommand : Command
{
#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        MolakFlow.Break();
    }
}
