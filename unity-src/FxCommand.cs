using Naninovel;
using UnityEngine;

/// <summary>
/// 능력치 일괄 변경 커맨드. 사용 예: @fx sw:2 hp:-1 curse:3
/// curse 양수 증가에는 봉인 보정이 자동 적용된다.
/// 0.4.3: 밤 행동(검·운기·날품·쉼)은 며칠 밤치 — nmul 변수(밤 행동 첫 줄에서 3, NightEnd 에서 0)가 1보다 크면 모든 변화량에 곱한다(날짜 제외).
/// </summary>
[Alias("fx")]
public class FxCommand : Command
{
    public IntegerParameter Hp;
    public IntegerParameter Ng;
    public IntegerParameter Sw;
    public IntegerParameter Sl;
    public IntegerParameter Mn;
    public IntegerParameter Curse;
    public IntegerParameter Day;

#pragma warning disable CS1998
    public override async Awaitable Execute (ExecutionContext ctx)
#pragma warning restore CS1998
    {
        float k = Mathf.Max(1f, MolakStats.Get("nmul"));
        if (Assigned(Hp))
        {
            float v = Hp.Value * k; MolakStats.Add("hp", v); MolakHud.Delta("hp", v);
            // 맞은 만큼 화면이 흔들린다: 한 칸은 움찔, 두 칸은 북소리, 넉 칸부터는 핏빛
            if (v <= -1) { MolakVfx.Shake(Mathf.Min(18f, 5f + 3f * -v)); MolakArt.Hurt(); }
            if (v <= -2) MolakSfx.Hit();
            if (v <= -4) MolakVfx.Blood();
        }
        if (Assigned(Ng)) { float v = Ng.Value * k; MolakStats.Add("ng", v); MolakHud.Delta("ng", v); }
        if (Assigned(Sw))
        {
            float dsw = 0f;
            // 검술 인플레 캡: 내공이 받쳐주지 않으면 수련 이득 반감 (웹판 규칙) — 며칠 밤이면 밤마다 따로 굴린다
            for (int i = 0; i < Mathf.RoundToInt(k); i++)
            {
                float one = Sw.Value;
                if (one > 0 && MolakStats.Get("sw") + dsw >= (MolakStats.Get("ng") + 5f) * 2f && Random.value < 0.5f) one = 0f;
                dsw += one;
            }
            MolakStats.Add("sw", dsw); MolakHud.Delta("sw", dsw);
        }
        if (Assigned(Sl)) { float v = Sl.Value * k; MolakStats.Add("sl", v); MolakHud.Delta("sl", v); }
        if (Assigned(Mn)) { float v = Mn.Value * k; MolakStats.Add("mn", v); MolakHud.Delta("mn", v); if (v >= 10) MolakVfx.Gold(); }
        if (Assigned(Day)) MolakStats.Add("day", Day.Value);
        if (Assigned(Curse))
        {
            float d = Curse.Value * k;
            if (d > 0) d = Mathf.Max(1f, Mathf.Round(d * MolakStats.CurseFactor()));
            MolakStats.Add("curse", d); MolakHud.Delta("curse", d);
            if (d >= 3) MolakVfx.CursePulse();   // 문양이 크게 한 번 뛴다
        }
        if (Assigned(Sw) || Assigned(Ng)) MolakStats.GrowBody();
        MolakStats.ClampAll();
    }
}
