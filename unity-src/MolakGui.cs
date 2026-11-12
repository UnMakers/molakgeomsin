using UnityEngine;

/// <summary>
/// 0.4.28 IMGUI 글자 색 도우미. GUIStyle 은 상태(평소·마우스 올림·누름·초점)마다 글자 색을 따로 가진다.
/// 평소(normal)에만 색을 주면, PC에서 마우스를 올리거나 누르는 순간 기본 피부의 하얀 글자로 바뀐다(행낭 띠의 "행낭"이 하얘지던 버그).
/// 색은 언제나 이 함수로 준다.
/// </summary>
public static class MolakGui
{
    public static void Ink (GUIStyle s, Color c)
    {
        if (s == null) return;
        s.normal.textColor = c; s.hover.textColor = c; s.active.textColor = c; s.focused.textColor = c;
        s.onNormal.textColor = c; s.onHover.textColor = c; s.onActive.textColor = c; s.onFocused.textColor = c;
    }
}
