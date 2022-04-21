using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class StartPanel : BasePanel
{
    private static StartPanel instance;
    public static StartPanel Instance => instance;
    protected void Awake() => instance = this;

    /// <summary>
    /// ��ʼ��Ϸ
    /// </summary>
    public void GameStart()
    {
        // ������Ϸ��ʼֵ
        GameMgr.Instance.GameStart();

        // �������̰�ť
        Chessboard.Instance.gameObject.GetComponent<UIButton>().enabled = true;

        // �������
        PlayPanel.Instance.ShowMe();
        HideMe();
    }

    /// <summary>
    /// �˳���Ϸ
    /// </summary>
    public void Exit() => Application.Quit(0);
}
