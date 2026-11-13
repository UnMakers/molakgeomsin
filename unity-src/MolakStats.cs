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

    /// <summary>그 이름의 변수가 있는가.</summary>
    public static bool Has (string name)
    {
        var vars = Vars;
        return vars != null && vars.VariableExists(name);
    }

    /// <summary>문자열 변수. 없거나 숫자 변수면 fallback.</summary>
    public static string Str (string name, string fallback = "")
    {
        var vars = Vars;
        if (vars == null || !vars.VariableExists(name)) return fallback;
        var v = vars.GetVariableValue(name);
        if (v.Type != CustomVariableValueType.String || v.String == null) return fallback;
        return v.String;
    }

    public static void SetStr (string name, string value)
    {
        Vars?.SetVariableValue(name, new CustomVariableValue(value ?? ""));
    }

    /// <summary>없을 때만 기본값으로 만든다(옛 저장 호환).</summary>
    public static void EnsureNum (string name, float value = 0f) { if (!Has(name)) Set(name, value); }
    public static void EnsureStr (string name, string value = "none") { if (!Has(name)) SetStr(name, value); }

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
        return (1f - MolakRules.SealCutPerLv * seal) * (soa > 0 ? MolakRules.SoaSealMul : 1f);
    }

    public static void ClampAll ()
    {
        var hpMax = Mathf.Max(1f, Get("hpmax"));
        Set("hp", Mathf.Clamp(Get("hp"), 0f, hpMax));
        Set("curse", Mathf.Clamp(Get("curse"), 0f, MolakRules.CurseMax));
        foreach (var k in new[] { "ng", "sw", "sl", "mn" })
            if (Get(k) < 0f) Set(k, 0f);
    }

    /// <summary>체력 상한 재계산: 기본 + floor((sw+ng)/BodyGrowthDivisor). 상한이 오르면 그만큼 회복.</summary>
    public static void GrowBody ()
    {
        var nm = (Get("practice") > 0f ? MolakRules.BaseHp : MolakMemory.StartHp()) + Mathf.Floor((Get("sw") + Get("ng")) / MolakRules.BodyGrowthDivisor);   // 0.4.55 겪어 본 죽음·생존의 뜻만큼 바탕이 두텁다
        var cur = Get("hpmax");
        if (nm > cur)
        {
            Set("hp", Get("hp") + (nm - cur));
            Set("hpmax", nm);
        }
    }
}
