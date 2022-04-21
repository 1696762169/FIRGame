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

    // 面板上的内容
    public UILabel tips;

    // 提示文字颜色
    public Color winColor;
    public Color failColor;
    public override void ShowMe()
    {
        bool win = GameMgr.Instance.win;
        tips.text = win ? "恭喜胜利!" : "游戏失败!";
        tips.color = win ? winColor : failColor;
        base.ShowMe();
    }

    /// <summary>
    /// 回到开始界面重新开始
    /// </summary>
    public void Restart()
    {
        // 清空棋盘
        GameMgr.Instance.GameOver();

        // 设置面板
        StartPanel.Instance.ShowMe();
        HideMe();
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    public void Exit() => Application.Quit(0);
}
