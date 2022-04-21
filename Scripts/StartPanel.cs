using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class StartPanel : BasePanel
{
    private static StartPanel instance;
    public static StartPanel Instance => instance;
    protected void Awake() => instance = this;

    /// <summary>
    /// 开始游戏
    /// </summary>
    public void GameStart()
    {
        // 设置游戏初始值
        GameMgr.Instance.GameStart();

        // 开启棋盘按钮
        Chessboard.Instance.gameObject.GetComponent<UIButton>().enabled = true;

        // 设置面板
        PlayPanel.Instance.ShowMe();
        HideMe();
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    public void Exit() => Application.Quit(0);
}
