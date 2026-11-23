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
        try { var pl = MolakVn.Engine.GetService<MolakVn.IScriptPlayer>(); if (pl != null && pl.Skipping) return; } catch { }
        lastPlay = Time.unscaledTime;
        MolakSfx.Page();   // 0.4.70 새로 뽑은 아주 작은 한지 소리(옛 소리는 뺐다)
    }
}
