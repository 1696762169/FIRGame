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
    /// 确认重新开始
    /// </summary>
    public void Yes()
    {
        // 清空棋盘
        GameMgr.Instance.GameOver();

        // 关闭棋盘按钮
        Chessboard.Instance.gameObject.GetComponent<UIButton>().enabled = false;

        // 设置面板
        StartPanel.Instance.ShowMe();
        HideMe();
    }

    /// <summary>
    /// 返回游戏
    /// </summary>
    public void No()
    {
        // 设置面板
        PlayPanel.Instance.ShowMe();
        HideMe();
    }
}
