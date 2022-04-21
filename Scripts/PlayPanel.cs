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
