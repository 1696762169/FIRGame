using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnsurePanel : BasePanel
{
    private static EnsurePanel instance;
    public static EnsurePanel Instance => instance;
    protected void Awake()
    {
        instance = this;
        HideMe();
    }

    /// <summary>
    /// ȷ�����¿�ʼ
    /// </summary>
    public void Yes()
    {
        // �������
        GameMgr.Instance.GameOver();

        // �ر����̰�ť
        Chessboard.Instance.gameObject.GetComponent<UIButton>().enabled = false;

        // �������
        StartPanel.Instance.ShowMe();
        HideMe();
    }

    /// <summary>
    /// ������Ϸ
    /// </summary>
    public void No()
    {
        // �������
        PlayPanel.Instance.ShowMe();
        HideMe();
    }
}
