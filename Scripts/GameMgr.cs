using System.Collections;
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
    [Tooltip("最大搜索深度")]
    public int maxDepth;
    [Tooltip("最大搜索结点数")]
    public int maxNodeNum;

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
    public int result;                  // 游戏结果 大于0则玩家胜利 小于0则AI胜利 等于0则平局

    protected int timer;                // 回合计时器

    /// <summary>
    /// 开始游戏
    /// </summary>
    public void GameStart()
    {
        // 设置先手玩家
        CurPlayer = startPlayer;
        CurPiece = E_PieceType.black;
        Chessboard.Instance.gameObject.GetComponent<UIButton>().enabled = CurPlayer == E_Player.player;

        // 开始计时
        timer = System.Environment.TickCount;

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
                result = 1;
                PlayPanel.Instance.HideMe();
                EndPanel.Instance.ShowMe();         // 展示胜利界面
            }
            else if (Chessboard.Instance.history.Count == Chessboard.lineNum * Chessboard.lineNum)
            {
                result = 0;
                PlayPanel.Instance.HideMe();
                EndPanel.Instance.ShowMe();         // 展示平局界面
            }
            else
            {
                SetCurrentPlayer(E_Player.AI);      // 控制权交给AI
                StartCoroutine(AIGo());             // AI进行行动
            }
        }
    }

    /// <summary>
    /// 进入AI的回合
    /// </summary>
    public IEnumerator AIGo()
    {
        // 等待玩家棋子与提示信息渲染完毕后再开始计算
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        // AI落子并在棋盘创建棋子
        Vector2Int pos = AIGamer.Instance.Go();
        Chessboard.Instance.CreatePiece(pos.x, pos.y);

        // 检测AI是否胜利
        if (GoalTest())
        {
            result = -1;
            PlayPanel.Instance.HideMe();
            EndPanel.Instance.ShowMe();         // 展示失败界面
        }
        else if (Chessboard.Instance.history.Count == Chessboard.lineNum * Chessboard.lineNum)
        {
            result = 0;
            PlayPanel.Instance.HideMe();
            EndPanel.Instance.ShowMe();         // 展示平局界面
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

        // 更新提示信息并重置计时器
        PlayPanel.Instance.UpdateTips((System.Environment.TickCount - timer) / 1000.0f);
        timer = System.Environment.TickCount;
    }
    protected void Awake() => instance = this;
    protected bool GoalTest() => AIGamer.Instance.GoalTest;
}
