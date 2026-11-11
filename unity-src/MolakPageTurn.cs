using UnityEngine;

/// <summary>
/// 책장 넘기는 소리 — 본문이 비워질 때마다(새 쪽) 종이 넘기는 소리만 낸다. 화면 연출은 없다.
/// 선택지를 고른 소리와 겹치지 않게, 연달아 비워질 때는 한 번만, 읽은 글을 넘기는 중엔 생략.
/// </summary>
public static class MolakPageTurn
{
    static float lastPlay = -9f;

    public static void Play ()
    {
        if (Time.unscaledTime - lastPlay < 0.35f) return;
        try { var pl = Naninovel.Engine.GetService<Naninovel.IScriptPlayer>(); if (pl != null && pl.Skipping) return; } catch { }
        lastPlay = Time.unscaledTime;
        if (Time.unscaledTime - MolakSfx.LastConfirm > 0.5f) MolakSfx.Play("page_turn", 0.55f, Random.Range(0.95f, 1.06f));
    }
}
