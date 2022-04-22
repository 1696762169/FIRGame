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
        base.ShowMe();
        tips.text = GameMgr.Instance.win ? "��ϲʤ��!" : "��Ϸʧ��!";
        tips.SetColorNoAlpha(GameMgr.Instance.win ? winColor : failColor);
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
