using Naninovel;
using UnityEngine;

/// <summary>
/// 승부수(勝負手) — @risk on:sw need:+2
/// 숨겨 둔 문턱을 넘으면 늘 이기던 판정을, 보이는 승산으로 바꾼다. 선택지 글귀 끝의 〔검술+2〕 와 짝을 이룬다:
/// 카드 아래에는 「검술 14 대 18 · 승산 4할」이 뜨고, 고르면 이 명령이 같은 식으로 굴려 pass(1/0)를 남긴다.
/// need 가 "+2"/"-1"/"0" 꼴이면 그날의 강호의 격(MolakRules.Bar)에 더한 값, "12" 꼴이면 그 수 그대로.
/// </summary>
[Alias("risk")]
public class RiskCommand : Command
{
    [Alias("on")] public StringParameter On;
    public StringParameter Need;

#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        string stat = Assigned(On) ? On.Value : "sw";
        float day = MolakStats.Get("day");
        float need = MolakRules.RiskNeed(stat, Assigned(Need) ? Need.Value : "0", day);
        float have = MolakStats.Get(stat);
        int p = MolakRules.RiskOdds(stat, have, need);
        bool pass = Random.Range(0, 100) < p;
#if UNITY_EDITOR
        { float f = MolakStats.Get("riskforce"); if (f > 0) pass = f == 1f; }   // 시험용: 1 통과 2 실패
#endif
        MolakStats.Set("pass", pass ? 1f : 0f);
        MolakStats.Set("riskp", p);
        // 판정의 순간을 몸으로: 통하면 칼 소리와 금빛, 빗나가면 둔탁한 북과 흔들림. 낮은 승산을 뚫으면 더 크게.
        if (pass)
        {
            MolakSfx.Draw();
            MolakVfx.Stamp("通", "승산 " + MolakRules.OddsText(p) + " · 통했다", p <= 40);
            if (p <= 40) { MolakVfx.Gold(); MolakVfx.Shake(8f); }
        }
        else
        {
            MolakSfx.Hit();
            MolakVfx.Stamp("失", "승산 " + MolakRules.OddsText(p) + " · 빗나갔다", p >= 60);
            MolakVfx.Shake(p >= 60 ? 14f : 9f);
        }
    }
}
