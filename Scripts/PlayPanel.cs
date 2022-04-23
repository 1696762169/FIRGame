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
        // 设置初始时的回合提示信息
        playerTure.text = "你的回合";
        playerTure.SetColorNoAlpha(playingColor);
        AITure.text = "AI的回合\n";
        AITure.SetColorNoAlpha(unPlayingColor);
    }

    /// <summary>
    /// 更新回合提示信息与用时
    /// </summary>
    /// <param name="usedTime">回合用时</param>
    /// 更新提示信息时当前玩家信息已经更新了
    public void UpdateTips(float usedTime)
    {
        if (GameMgr.Instance.CurPlayer == E_Player.player)
        {
            playerTure.text = "你的回合";
            playerTure.SetColorNoAlpha(playingColor);
            AITure.text = string.Format("AI的回合\n用时\n{0:F3}s", usedTime);
            AITure.SetColorNoAlpha(unPlayingColor);
        }
        else
        {
            playerTure.text = string.Format("你的回合\n用时\n{0:F3}s", usedTime);
            playerTure.SetColorNoAlpha(unPlayingColor);
            AITure.text = "AI的回合";
            AITure.SetColorNoAlpha(playingColor);
        }
    }

    /// <summary>
    /// 重新开始游戏
    /// </summary>
    public void Restart()
    {
        // 设置面板
        EnsurePanel.Instance.ShowMe();
        HideMe();
    }

    /// <summary>
    /// 悔棋
    /// </summary>
    public void Cancel()
    {
        if (Chessboard.Instance.history.Count < 2)
            return;
        // 撤销玩家与AI各一步操作
        for (int i = 0; i < 2; ++i)
            Chessboard.Instance.DestroyPiece();
    }
}
