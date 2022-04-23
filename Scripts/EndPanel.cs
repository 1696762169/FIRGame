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
    public Color deuceColor;
    public override void ShowMe()
    {
        if (GameMgr.Instance.result > 0)
        {
            tips.text = "恭喜胜利!";
            tips.SetColorNoAlpha(winColor);
        }
        else if (GameMgr.Instance.result < 0)
        {
            tips.text = "游戏失败!";
            tips.SetColorNoAlpha(failColor);
        }
        else
        {
            tips.text = "平局 ";
            tips.SetColorNoAlpha(deuceColor);
        }
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
