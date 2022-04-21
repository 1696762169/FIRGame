using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 当前玩家类型
/// </summary>
public enum E_Player
{
    player,
    AI,
}

public class GameMgr : MonoBehaviour
{
    private static GameMgr instance;
    public static GameMgr Instance => instance;

    [Tooltip("先手玩家")]
    public E_Player startPlayer = E_Player.player;

    public E_PieceType CurPiece         // 当前棋子
    {
        get;
        protected set;
    }
    public E_Player CurPlayer           // 当前玩家
    {
        get;
        protected set;
    }

    [HideInInspector]
    public bool win;                    // 是否胜利

    /// <summary>
    /// 开始游戏
    /// </summary>
    public void GameStart()
    {
        // 设置先手玩家
        CurPlayer = startPlayer;
        CurPiece = E_PieceType.black;
        Chessboard.Instance.gameObject.GetComponent<UIButton>().enabled = CurPlayer == E_Player.player;

        // 开始AI第一步
        if (CurPlayer == E_Player.AI)
        {
            Chessboard.Instance.CreatePiece(Chessboard.lineNum / 2, Chessboard.lineNum / 2);
            SetCurrentPlayer(E_Player.player);
        }
    }

    /// <summary>
    /// 结束游戏
    /// </summary>
    public void GameOver()
    {
        // 清空AI状态
        AIGamer.Instance.Init();

        // 清空棋盘
        while (Chessboard.Instance.history.Count != 0)
            Chessboard.Instance.DestroyPiece();
    }

    /// <summary>
    /// 玩家下棋
    /// 需绑定至棋盘按钮
    /// </summary>
    public void PlayerGo()
    {
        // 判断是否成功落子
        if (Chessboard.Instance.PlayerTryCreatePiece())
        {
            if (GoalTest())                         // 检测玩家是否胜利
            {
                win = true;
                EndPanel.Instance.ShowMe();         // 展示胜利界面
            }
            else
                AIGo();                             // AI进行操作
        }
    }

    /// <summary>
    /// 进入AI的回合
    /// </summary>
    public void AIGo()
    {
        // 控制权交给AI
        SetCurrentPlayer(E_Player.AI);

        // AI落子并在棋盘创建棋子
        Vector2Int pos = AIGamer.Instance.Go();
        Chessboard.Instance.CreatePiece(pos.x, pos.y);

        // 检测AI是否胜利
        if (GoalTest())
        {
            win = false;
            EndPanel.Instance.ShowMe();         // 展示失败界面
        }
        else
            SetCurrentPlayer(E_Player.player);  // 控制权交给玩家
        System.GC.Collect();
    }

    /// <summary>
    /// 设置当前玩家
    /// </summary>
    protected void SetCurrentPlayer(E_Player newPlayer)
    {
        // 激活/失活输入
        bool enabled = newPlayer == E_Player.player;
        Chessboard.Instance.gameObject.GetComponent<UIButton>().enabled = enabled;
        foreach (UIButton button in PlayPanel.Instance.GetComponentsInChildren<UIButton>())
            button.enabled = enabled;

        // 交换玩家与棋子
        if (CurPlayer != newPlayer)
        {
            CurPlayer = CurPlayer == E_Player.player ? E_Player.AI : E_Player.player;
            CurPiece = CurPiece == E_PieceType.black ? E_PieceType.white : E_PieceType.black;
        }

        // 显示提示信息
    }
    protected void Awake() => instance = this;
    protected bool GoalTest() => AIGamer.Instance.GoalTest;
}
