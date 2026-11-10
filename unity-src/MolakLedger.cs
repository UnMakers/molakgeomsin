using UnityEngine;

/// <summary>
/// 원수 장부 · 생을 넘어 — 내가 벤 자 / 나 때문에 죽은 자 / 살려 보낸 자.
/// 회차 안의 깃발(f_…)을 생이 끝날 때(@regress) 전역 카운터 g_lk_/g_ld_/g_ls_ 로 쌓는다.
/// 이름 없는 목(비무판 참수)과 살려 보낸 비무꾼은 killed / spared 카운터로 따로 센다.
/// </summary>
public static class MolakLedger
{
    public struct Entry
    {
        public string Id, Name, Kill, Death, Spare;
        public Entry (string id, string n, string k, string d, string s) { Id = id; Name = n; Kill = k; Death = d; Spare = s; }
    }

    public static readonly Entry[] All =
    {
        new Entry("hochi",   "독아단 아장 호치",       "f_hmsaved",       null,             null),
        new Entry("kyona",   "독아단주 견아",          "f_kyonadown",     null,             null),
        new Entry("wolf",    "혈쇄문주",               "f_wolfdown",      null,             null),
        new Entry("jinbaek", "대사형 진백",            "f_jinbaekdead",   null,             "f_jinbaektruth"),
        new Entry("namgung", "남궁세가 소가주 남궁현", "f_namgungdead",   null,             "f_namgungspared"),
        new Entry("baekri",  "무림맹 밀사 백리휘",     "f_baekridead",    null,             null),
        new Entry("maeng",   "폐사찰의 별 일곱",       null,              "f_maengsold",    null),
        new Entry("yong",    "산적 용개",              "f_yongkilled",    null,             "f_yongspared"),
        new Entry("yongfam", "용개의 누이와 조카",     null,              null,             "f_yongfamsaved"),
        new Entry("amh",     "암향루의 암향",          "f_amhdead",       null,             "f_amhally"),
        new Entry("gwakw",   "곽산의 아내",            null,              "f_gwakwifedied", null),
        new Entry("gwak",    "창잡이 곽산",            null,              null,             "f_gwakhelped"),
        new Entry("jang",    "쥐눈이 장",              null,              null,             "f_jangsaved"),
        new Entry("hajin",   "운락객잔 하진무",        null,              "f_hajindead",    null),
        new Entry("herb",    "백초 할멈",              null,              "f_herbdead",     "f_herbburned"),
        new Entry("soa",     "적운검파 소아",          null,              "f_soadead",      null),
    };

    static float G (string k) { return string.IsNullOrEmpty(k) ? 0f : MolakStats.Get(k); }

    /// <summary>생이 끝날 때 한 번. 이번 생의 깃발을 전역 장부에 더한다.</summary>
    public static void Accumulate ()
    {
        foreach (var e in All)
        {
            if (G(e.Kill) > 0) MolakStats.Add("g_lk_" + e.Id, 1f);
            if (G(e.Death) > 0) MolakStats.Add("g_ld_" + e.Id, 1f);
            if (G(e.Spare) > 0) MolakStats.Add("g_ls_" + e.Id, 1f);
        }
        MolakStats.Add("g_lk_nameless", G("killed"));
        MolakStats.Add("g_ls_nameless", G("spared"));
    }

    /// <summary>표시용 한 줄: "벤 것 2 · 나 때문에 1 · 살림 3". 아무것도 없으면 null.</summary>
    public static string Line (string id)
    {
        float k = MolakStats.Get("g_lk_" + id), d = MolakStats.Get("g_ld_" + id), s = MolakStats.Get("g_ls_" + id);
        var parts = new System.Collections.Generic.List<string>();
        if (k > 0) parts.Add("<color=#C03434>벤 것 " + k.ToString("0") + "</color>");
        if (d > 0) parts.Add("<color=#8C8378>나 때문에 " + d.ToString("0") + "</color>");
        if (s > 0) parts.Add("<color=#3E6E8E>살림 " + s.ToString("0") + "</color>");
        return parts.Count == 0 ? null : string.Join(" · ", parts.ToArray());
    }
}
