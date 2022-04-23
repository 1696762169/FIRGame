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
    public Color deuceColor;
    public override void ShowMe()
    {
        if (GameMgr.Instance.result > 0)
        {
            tips.text = "��ϲʤ��!";
            tips.SetColorNoAlpha(winColor);
        }
        else if (GameMgr.Instance.result < 0)
        {
            tips.text = "��Ϸʧ��!";
            tips.SetColorNoAlpha(failColor);
        }
        else
        {
            tips.text = "ƽ�� ";
            tips.SetColorNoAlpha(deuceColor);
        }
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
