using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlayPanel : BasePanel
{
    private static PlayPanel instance;
    public static PlayPanel Instance => instance;

    public UILabel playerTure;
    public UILabel AITure;
    public Color playingColor;
    public Color unPlayingColor;
    protected void Awake()
    {
        instance = this;
        HideMe();
    }
    protected void OnEnable()
    {
        // ���ó�ʼʱ�Ļغ���ʾ��Ϣ
        playerTure.text = "��Ļغ�";
        playerTure.SetColorNoAlpha(playingColor);
        AITure.text = "AI�Ļغ�\n";
        AITure.SetColorNoAlpha(unPlayingColor);
    }

    /// <summary>
    /// ���»غ���ʾ��Ϣ����ʱ
    /// </summary>
    /// <param name="usedTime">�غ���ʱ</param>
    /// ������ʾ��Ϣʱ��ǰ�����Ϣ�Ѿ�������
    public void UpdateTips(float usedTime)
    {
        if (GameMgr.Instance.CurPlayer == E_Player.player)
        {
            playerTure.text = "��Ļغ�";
            playerTure.SetColorNoAlpha(playingColor);
            AITure.text = string.Format("AI�Ļغ�\n��ʱ\n{0:F3}s", usedTime);
            AITure.SetColorNoAlpha(unPlayingColor);
        }
        else
        {
            playerTure.text = string.Format("��Ļغ�\n��ʱ\n{0:F3}s", usedTime);
            playerTure.SetColorNoAlpha(unPlayingColor);
            AITure.text = "AI�Ļغ�";
            AITure.SetColorNoAlpha(playingColor);
        }
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
