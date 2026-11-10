using Naninovel;
using UnityEngine;

/// <summary>
/// 몰락검신 능력치 헬퍼 — 나니노벨 커스텀 변수 위에서 동작.
/// 변수: hp, hpmax, ng(내공), sw(검술), sl(살기), mn(은자), curse(저주), day, rank
/// </summary>
public static class MolakStats
{
    public static readonly string[] Keys = { "hp", "hpmax", "ng", "sw", "sl", "mn", "curse", "day", "rank" };

    static ICustomVariableManager Vars => Engine.GetService<ICustomVariableManager>();

    public static float Get (string name)
    {
        var vars = Vars;
        if (vars == null || !vars.VariableExists(name)) return 0f;
        var v = vars.GetVariableValue(name);
        return v.Type == CustomVariableValueType.Numeric ? v.Number : 0f;
    }

    public static void Set (string name, float value)
    {
        Vars?.SetVariableValue(name, new CustomVariableValue(value));
    }

    public static void Add (string name, float delta)
    {
        if (Mathf.Approximately(delta, 0f)) return;
        Set(name, Get(name) + delta);
    }

    /// <summary>저주 증가에 봉인 보정 적용 (sealLv, soaseal 변수 참조).</summary>
    public static float CurseFactor ()
    {
        var seal = Get("seallv");
        var soa = Get("soaseal");
        return (1f - 0.12f * seal) * (soa > 0 ? 0.85f : 1f);
    }

    public static void ClampAll ()
    {
        var hpMax = Mathf.Max(1f, Get("hpmax"));
        Set("hp", Mathf.Clamp(Get("hp"), 0f, hpMax));
        Set("curse", Mathf.Clamp(Get("curse"), 0f, 100f));
        foreach (var k in new[] { "ng", "sw", "sl", "mn" })
            if (Get(k) < 0f) Set(k, 0f);
    }

    /// <summary>체력 상한 재계산: 10 + floor((sw+ng)/9). 상한이 오르면 그만큼 회복.</summary>
    public static void GrowBody ()
    {
        var nm = 10f + Mathf.Floor((Get("sw") + Get("ng")) / 9f);
        var cur = Get("hpmax");
        if (nm > cur)
        {
            Set("hp", Get("hp") + (nm - cur));
            Set("hpmax", nm);
        }
    }
}
