using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 棋子类型枚举
/// </summary>
public enum E_PieceType
{
    black,
    white,
}

/// <summary>
/// 棋盘落子处的状态
/// </summary>
public enum E_Cross
{
    none,
    player,
    AI,
}

/// <summary>
/// 棋盘类
/// 管理棋子信息及其图片对象
/// 棋盘坐标系以右侧为X正方向，上方为Y正方向，左下角为原点
/// </summary>
public class Chessboard : MonoBehaviour
{
    private static Chessboard instance;
    public static Chessboard Instance => instance;

    // 棋盘像素信息
    [Tooltip("网格宽高像素")]
    public int crossSize;
    [Tooltip("棋盘边界像素")]
    public int edge;
    [Tooltip("棋子间隔像素")]
    public int piecesGap;
    private int boardSize;                  // 棋盘总长宽像素

    // 棋子信息
    [HideInInspector]
    protected E_Cross[,] board;             // 棋盘上的棋子信息
    public E_Cross[,] Board => board;
    private GameObject[,] pieceSprites;     // 棋子图片对象，方便销毁棋子
    public const int lineNum = 15;          // 棋盘行列数

    // 行棋历史
    public Stack<Vector2Int> history = new Stack<Vector2Int>();

    // 黑白棋子预设体
    private GameObject blackPiece;
    private GameObject whitePiece;

    // 棋盘 有效区域 屏幕坐标系下的位置
    private Rect EnableRect
    {
        get
        {
            Rect enableRect = new Rect();

            // UIRoot中定义的宽高
            int rootWidth = transform.GetComponentInParent<UIRoot>().manualWidth;
            int rootHeight = transform.GetComponentInParent<UIRoot>().manualHeight;
            // 窗口宽度与UIRoot中定义的宽度的比值
            float rate = (float)Screen.width / rootWidth;

            // 棋盘左侧空间像素
            int left = GetComponent<UITexture>().leftAnchor.absolute;
            // 棋盘竖直方向长度像素
            int vertical = GetComponent<UITexture>().topAnchor.absolute - GetComponent<UITexture>().bottomAnchor.absolute;

            // 计算区域位置与大小
            enableRect.x = (left + edge) * rate;
            enableRect.y = (Screen.height - (boardSize - 2 * edge) * rate) / 2;
            enableRect.width = (boardSize - edge * 2) * rate;
            enableRect.height = enableRect.width;
            return enableRect;
        }
    }

    /// <summary>
    /// 玩家尝试落子
    /// </summary>
    /// <returns>是否成功落子</returns>
    public bool PlayerTryCreatePiece()
    {
        Rect enable = EnableRect;
        Vector3 mouse = Input.mousePosition;
        int x = (int)((mouse.x - enable.x) / enable.width * lineNum);
        int y = (int)((mouse.y - enable.y) / enable.height * lineNum);
        if (0 <= x && x < lineNum && 0 <= y && y < lineNum && board[x, y] == E_Cross.none)
        {
            CreatePiece(x, y);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 生成棋子
    /// </summary>
    public void CreatePiece(int x, int y)
    {
        // 记录到棋子数组
        if (GameMgr.Instance.CurPlayer == E_Player.player)
            board[x, y] = E_Cross.player;
        else
            board[x, y] = E_Cross.AI;

        // 创建精灵图片
        GameObject newPiece = Instantiate(GameMgr.Instance.CurPiece == E_PieceType.black ? blackPiece : whitePiece,
            Vector3.zero, Quaternion.identity, transform);
        UISprite sprite = newPiece.GetComponent<UISprite>();

        // 设置锚点与位置
        int left = edge + x * crossSize + piecesGap;
        int right = left + crossSize - 2 * piecesGap;
        int bottom = edge + y * crossSize + piecesGap;
        int top = bottom + crossSize - 2 * piecesGap;
        sprite.SetAnchor(transform);
        sprite.leftAnchor.absolute = left;
        sprite.rightAnchor.absolute = right;
        sprite.bottomAnchor.absolute = bottom;
        sprite.topAnchor.absolute = top;

        // 记录图片到数组并激活
        newPiece.SetActive(true);
        pieceSprites[x, y] = newPiece;

        // 记录到上一步棋子
        history.Push(new Vector2Int(x, y));

        // 同步AI状态
        AIGamer.Instance.Synchronize(x, y, true);
    }

    /// <summary>
    /// 销毁上一颗棋子
    /// </summary>
    public void DestroyPiece()
    {
        if (history.Count == 0)
            return;
        Vector2Int pos = history.Pop();

        // 记录到棋子数组
        board[pos.x, pos.y] = E_Cross.none;

        // 记录图片到数组并销毁
        Destroy(pieceSprites[pos.x, pos.y]);
        pieceSprites[pos.x, pos.y] = null;

        // 同步AI状态
        AIGamer.Instance.Synchronize(pos.x, pos.y, false);
    }
    protected void Awake()
    {
        instance = this;

        // 检查自身组件
        Tools.LogNull(gameObject.GetComponent<UITexture>(), "棋盘(Chessboard)没有图片(UITexture)", true);
        Tools.LogNull(gameObject.GetComponent<UIButton>(), "棋盘(Chessboard)没有按钮(UIButton)", true);
        Tools.LogNull(gameObject.GetComponent<BoxCollider2D>(), "棋盘(Chessboard)没有碰撞器(BoxCollider2D)", true);

        // 设置碰撞器范围
        BoxCollider2D collider = gameObject.GetComponent<BoxCollider2D>();
        boardSize = gameObject.GetComponent<UITexture>().mainTexture.width;
        collider.size = new Vector2(boardSize - 2 * edge, boardSize - 2 * edge);

        // 初始化数组
        board = new E_Cross[lineNum, lineNum];
        pieceSprites = new GameObject[lineNum, lineNum];
        for (int i = 0; i < lineNum; ++i)
            for (int j = 0; j < lineNum; ++j)
            {
                board[i, j] = E_Cross.none;
                pieceSprites[i, j] = null;
            }

        // 初始化黑白棋子预设体
        blackPiece = Resources.Load<GameObject>("Prefabs/Piece_Black");
        whitePiece = Resources.Load<GameObject>("Prefabs/Piece_White");
        Tools.LogNull<UISprite>(blackPiece, "未找到黑子预设体", true);
        Tools.LogNull<UISprite>(whitePiece, "未找到白子预设体", true);
    }
}
