using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EndPanel : BasePanel
{
    private static EndPanel instance;
    public static EndPanel Instance => instance;
    protected void Awake()
    {
        instance = this;
        HideMe();
    }

    // ����ϵ�����
    public UILabel tips;

    // ��ʾ������ɫ
    public Color winColor;
    public Color failColor;
    public override void ShowMe()
    {
        bool win = GameMgr.Instance.win;
        tips.text = win ? "��ϲʤ��!" : "��Ϸʧ��!";
        tips.color = win ? winColor : failColor;
        base.ShowMe();
    }

    /// <summary>
    /// �ص���ʼ�������¿�ʼ
    /// </summary>
    public void Restart()
    {
        // �������
        GameMgr.Instance.GameOver();

        // �������
        StartPanel.Instance.ShowMe();
        HideMe();
    }

    /// <summary>
    /// �˳���Ϸ
    /// </summary>
    public void Exit() => Application.Quit(0);
}
