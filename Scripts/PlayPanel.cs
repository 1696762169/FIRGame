using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayPanel : BasePanel
{
    private static PlayPanel instance;
    public static PlayPanel Instance => instance;
    protected void Awake()
    {
        instance = this;
        HideMe();
    }

    /// <summary>
    /// ���¿�ʼ��Ϸ
    /// </summary>
    public void Restart()
    {
        // �������
        EnsurePanel.Instance.ShowMe();
        HideMe();
    }

    /// <summary>
    /// ����
    /// </summary>
    public void Cancel()
    {
        if (Chessboard.Instance.history.Count < 2)
            return;
        // ���������AI��һ������
        for (int i = 0; i < 2; ++i)
            Chessboard.Instance.DestroyPiece();
    }
}
