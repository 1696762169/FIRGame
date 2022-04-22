//#define DEBUG_FEATURE
#define DEBUG_EVAL
#define DEBUG_SEARCH
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// AI玩家类
/// </summary>
public class AIGamer
{
    private static AIGamer instance = new AIGamer();
    public static AIGamer Instance => instance;
    private AIGamer() => Init();

    // 棋盘行列数
    protected const int lineNum = Chessboard.lineNum;
    // 用于内部计算的棋盘状态
    protected E_Cross[,] board;
    // 下一步行棋位置
    protected Vector2Int nextPos;

    // 用于内部计算的棋盘特征
    protected FeatureInfo AIFi;
    protected FeatureInfo playerFi;
    // 记录本次落子前的棋盘特征
    protected FeatureInfo originAIFi;
    protected FeatureInfo originPlayerFi;

    // 搜索最大深度
    protected int maxDepth;

    // 标记是否创建过棋子
    protected bool playing;

    /// <summary>
    /// 检测游戏是否结束
    /// </summary>
    public bool GoalTest
    {
        get;
        protected set;
    }

    // 记录四个方向对应数值
    public const int dir_hor = 0;        // 横向
    public const int dir_ver = 1;        // 纵向
    public const int dir_div = 2;        // 左下-右上
    public const int dir_back = 3;       // 左上-右下

    /// <summary>
    /// 初始化属性
    /// </summary>
    public void Init()
    {
        maxDepth = GameMgr.Instance.maxDepth;
        board = new E_Cross[lineNum, lineNum];
        for (int i = 0; i < lineNum; ++i)
            for (int j = 0; j < lineNum; ++j)
                board[i, j] = E_Cross.none;
        AIFi = new FeatureInfo();
        playerFi = new FeatureInfo();
        playing = false;
        GoalTest = false;
    }

    /// <summary>
    /// 实际生成/销毁棋子时同步内部状态
    /// </summary>
    /// <param name="create">新操作是否是落子</param>
    public void Synchronize(int x, int y, bool create)
    {
        // 游戏结束清空棋盘时无需同步
        if (!playing && !create)
            return;
        else
            playing = true;

        // 更新内部状态
        board[x, y] = Chessboard.Instance.Board[x, y];
        UpdateAllFeatures(x, y);

        // 调用评价函数判断是否结束游戏
        if (create && Mathf.Abs(Eval()) > (int)E_FeatureType.five / 2)
            GoalTest = true;
    }

    /// <summary>
    /// 计算下一个要落子的坐标
    /// </summary>
    /// <returns>计算得到的合法坐标</returns>
    public Vector2Int Go()
    {
        // 记录棋盘特征
        originAIFi = new FeatureInfo(AIFi);
        originPlayerFi = new FeatureInfo(playerFi);

        // 一层搜索落子
        //int maxEval = int.MinValue;
        //int curEval;
        //Vector2Int pos = new Vector2Int();
        //for (int i = 0; i < lineNum; ++i)
        //{
        //    for (int j = 0; j < lineNum; ++j)
        //    {
        //        if (board[i, j] == E_Cross.none)
        //        {
        //            board[i, j] = E_Cross.AI;
        //            UpdateAllFeatures(i, j);
        //            curEval = Eval();
        //            if (curEval > maxEval)
        //            {
        //                maxEval = curEval;
        //                pos.x = i;
        //                pos.y = j;
        //            }
        //            board[i, j] = E_Cross.none;
        //            UpdateAllFeatures(i, j);
        //        }
        //    }
        //}

        int maxEval = AlphaBetaSearch();
        // 将棋盘特征与计算前的特征同步
        AIFi = new FeatureInfo(originAIFi);
        playerFi = new FeatureInfo(originPlayerFi);
#if DEBUG_EVAL
        Debug.Log(string.Format("最大评价值：{0, -10}最佳位置：{1}", maxEval, nextPos));
#endif
        return nextPos;
    }

    /// <summary>
    /// 当前状态的评价函数
    /// </summary>
    protected int Eval()
    {
        return Eval(E_Player.AI) - 20 * Eval(E_Player.player);
        
    }
    /// <summary>
    /// 统计某一方的评价函数值
    /// </summary>
    protected int Eval(E_Player side)
    {
        int eval = 0;
        FeatureInfo fi = side == E_Player.AI ? AIFi : playerFi;
        foreach (Feature f in fi.features)
        {
            switch (f.type)
            {
            case E_FeatureType.none:
            case E_FeatureType.dead:
                continue;
            default:
                eval += (int)f.type * (f.blocked ? 1 : Feature.liveScale);
                break;
            }
        }
#if DEBUG_FEATURE
        if (GameMgr.Instance.CurPlayer == E_Player.player)
            return eval;
        string sideStr = side == E_Player.player ? "玩家" : "AI";
        for (int dir =0;dir < 4; ++dir)
        {
            for (int line = 0;line < FeatureInfo.lineMax; ++line)
            {
                for (int loca = 0; loca < lineNum; ++loca)
                {
                    switch (fi[dir, line, loca].type)
                    {
                    case E_FeatureType.dead:
                    case E_FeatureType.none:
                    case E_FeatureType.single:
                        continue;
                    default:
                        Debug.Log(string.Format("归属方：{0, -5}方向：{1,-3}行数：{2,-3}位置：{3,-3}类型：{4}", sideStr, dir, line, loca, fi[dir, line, loca].type));
                        break;
                    }
                }
            }
        }
#endif
        return eval;
    }

    /// <summary>
    /// 根据发生变化的位置更新当前特征状态
    /// </summary>
    protected void UpdateAllFeatures(int x, int y)
    {
        E_Cross[] pieces = new E_Cross[lineNum + 1];
        // 更新横向
        for (int i = 0; i < lineNum; ++i)
            pieces[i] = board[i, y];
        UpdateLineFeatures(E_Player.AI, dir_hor, y, pieces);
        UpdateLineFeatures(E_Player.player, dir_hor, y, pieces);
        // 更新纵向
        for (int i = 0; i < lineNum; ++i)
            pieces[i] = board[x, i];
        UpdateLineFeatures(E_Player.AI, dir_ver, x, pieces);
        UpdateLineFeatures(E_Player.player, dir_ver, x, pieces);

        // 某些区域的落子不会产生斜向有效特征
        int YmX = y - x;
        if (Mathf.Abs(YmX) <= lineNum - 5)
        {
            // 更新左下-右上
            for (int i = Mathf.Max(0, YmX); i < lineNum - Mathf.Max(0, -YmX); ++i)
                pieces[i] = board[i - YmX, i];
            UpdateLineFeatures(E_Player.AI, dir_div, FeatureInfo.lineMax / 2 - YmX, pieces);
            UpdateLineFeatures(E_Player.player, dir_div, FeatureInfo.lineMax / 2 - YmX, pieces);
        }
        int XpYp1 = x + y + 1;
        if (Mathf.Abs(lineNum - XpYp1) <= lineNum - 5)
        {
            // 更新左上-右下
            for (int i = Mathf.Max(0, XpYp1 - lineNum); i < Mathf.Min(lineNum, XpYp1); ++i)
                pieces[i] = board[i, XpYp1 - 1 - i];
            UpdateLineFeatures(E_Player.AI, dir_back, FeatureInfo.lineMax + 4 - XpYp1, pieces);
            UpdateLineFeatures(E_Player.player, dir_back, FeatureInfo.lineMax + 4 - XpYp1, pieces);
        }
    }

    /// <summary>
    /// 更新某方向上一条直线的全部特征
    /// </summary>
    /// <param name="side">待更新的特征归属方</param>
    /// <param name="dir">直线方向</param>
    /// <param name="line">行列数</param>
    /// <param name="pieces">该条直线上的所有棋子分布情况</param>
    protected void UpdateLineFeatures(E_Player side, int dir, int line, E_Cross[] pieces)
    {
        // 待更新数组
        FeatureInfo fi = side == E_Player.AI ? AIFi : playerFi;
        // 对手棋子
        E_Cross enemy = side == E_Player.AI ? E_Cross.player : E_Cross.AI;
        // pieces尾部增加一个对手棋子方便判断
        pieces[lineNum] = enemy;

        // 将该行棋子按E_Cross.enemy与E_Feature.dead划分为多段 记录其起点与终点后一个位置
        List<Vector2Int> saps = new List<Vector2Int>();
        Vector2Int cur = new Vector2Int(-1, -1);
        bool newSap = true;
        for (int loca = 0; loca < lineNum; ++loca)
        {
            if (fi[dir, line, loca].type == E_FeatureType.dead || pieces[loca] == enemy)
            {
                if (cur.x != -1)
                {
                    saps.Add(cur);
                    cur.x = -1;
                }
                newSap = true;
            }
            else
            {
                if (newSap)
                {
                    cur = new Vector2Int(loca, loca + 1);
                    newSap = false;
                }
                else
                    ++cur.y;
            }
        }
        if (cur.x != -1)
        {
            saps.Add(cur);
            cur.x = -1;
        }

        // 将长度小于五的分段设为none 并移除分段
        for (int i = saps.Count - 1; i >= 0; --i)
        {
            if (saps[i].y - saps[i].x < 5)
            {
                for (int loca = saps[i].x; loca < saps[i].y; ++loca)
                    fi[dir, line, loca].type = E_FeatureType.none;
                saps.RemoveAt(i);
            }
        }
       
        // 分别更新每个分段
        foreach (Vector2Int sap in saps)
        {
            // 该分段总空间
            int space = sap.y - sap.x;
            // 记录上一个特征覆盖范围的后一个位置 避免特征连锁
            int lastEnd = -100;
            for (int loca = sap.x; loca < sap.y; ++loca)
            {
                // 没有棋子的位置不可能作为特征起始点
                if (pieces[loca] == E_Cross.none)
                {
                    fi[dir, line, loca].type = E_FeatureType.none;
                    continue;
                }

                // 该分段过小时视为被封堵
                fi[dir, line, loca].blocked = space == 5;

                // 该分段剩余空间 接下来按剩余空间搜索
                int lastSpace = sap.y - loca;
                // 记录当前特征使用的空间
                int used = 0;

                // 需要1空间
                if (loca + 1 > lastEnd)
                {
                    fi[dir, line, loca].type = E_FeatureType.single;
                    fi[dir, line, loca].blocked |= loca == 0 || pieces[loca - 1] == enemy || pieces[loca + 1] == enemy;
                    used = 1;
                }

                // 需要2空间
                if (lastSpace >= 2 && pieces[loca + 1] != E_Cross.none && loca + 2 > lastEnd)
                {
                    // 连二
                    fi[dir, line, loca].type = E_FeatureType.near2;
                    fi[dir, line, loca].blocked |= pieces[loca + 2] == enemy;
                    used = 2;
                }

                // 需要3空间
                if (lastSpace >= 3 && pieces[loca + 2] != E_Cross.none && loca + 3 > lastEnd)
                {
                    // 小跳二
                    if (pieces[loca + 1] == E_Cross.none)
                        fi[dir, line, loca].type = E_FeatureType.jump2;
                    // 连三
                    else
                        fi[dir, line, loca].type = E_FeatureType.near3;
                    fi[dir, line, loca].blocked |= pieces[loca + 3] == enemy;
                    used = 3;
                }

                // 需要4空间
                if (lastSpace >= 4 && pieces[loca + 3] != E_Cross.none && loca + 4 > lastEnd)
                {
                    // 大跳二
                    if (pieces[loca + 1] == E_Cross.none && pieces[loca + 2] == E_Cross.none)
                        fi[dir, line, loca].type = E_FeatureType.far2;
                    // 跳三
                    else if (pieces[loca + 1] != pieces[loca + 2])
                        fi[dir, line, loca].type = E_FeatureType.jump3;
                    // 连四
                    else
                        fi[dir, line, loca].type = E_FeatureType.near4;
                    fi[dir, line, loca].blocked |= pieces[loca + 4] == enemy;
                    used = 4;
                }

                // 需要5空间
                if (lastSpace >= 5 && pieces[loca + 4] != E_Cross.none && loca + 5 > lastEnd)
                {
                    // 统计中间三个位置的空位数
                    int blankCount = 0;
                    for (int i = 1; i <= 3; ++i)
                        blankCount += pieces[loca + i] == E_Cross.none ? 1 : 0;

                    // 跳四
                    if (blankCount == 1)
                    {
                        fi[dir, line, loca].type = E_FeatureType.jump4;
                        used = 5;
                    }
                    // 连五
                    if (blankCount == 0)
                    {
                        fi[dir, line, loca].type = E_FeatureType.five;
                        used = 5;
                    }
                    fi[dir, line, loca].blocked |= pieces[loca + 5] == enemy;
                }

                // 由于此位置已经产生了特征起始点 故后续的连续相同棋子都无需考虑
                while (++loca < sap.y && pieces[loca] != E_Cross.none)
                    fi[dir, line, loca].type = E_FeatureType.none;
                // 记录当前特征后一个位置
                lastEnd = loca + used;
            }// end of each sap
        }// end of foreach statement
    }

    /* 以下为α-β剪枝搜索相关函数 */
    protected int AlphaBetaSearch()
    {
        return MaxValue(int.MinValue, int.MaxValue, 0);
    }
    protected int MaxValue(int alpha, int beta, int depth)
    {
        if (depth >= maxDepth)
            return Eval();
        int eval = int.MinValue;
        int curEval;
        for (int i = 0; i < lineNum; ++i)
        {
            for (int j = 0; j < lineNum; ++j)
            {
                if (board[i, j] == E_Cross.none)
                {
                    board[i, j] = E_Cross.AI;
                    UpdateAllFeatures(i, j);
                    curEval = MinValue(alpha, beta, depth + 1);
                    if (curEval > eval)
                    {
                        eval = curEval;
                        nextPos.x = i;
                        nextPos.y = j;
#if DEBUG_SEARCH
                        Debug.Log(string.Format("当前最大评价函数：{0, -10}当前位置{1}", eval, nextPos));
#endif
                    }
                    board[i, j] = E_Cross.none;
                    UpdateAllFeatures(i, j);
                    if (eval >= beta)
                        return eval;
                    alpha = Mathf.Max(alpha, eval);
                }
            }
        }
        return eval;
    }
    protected int MinValue(int alpha, int beta, int depth)
    {
        if (depth >= maxDepth)
            return Eval();
        int eval = int.MaxValue;
        for (int i = 0; i < lineNum; ++i)
        {
            for (int j = 0; j < lineNum; ++j)
            {
                if (board[i, j] == E_Cross.none)
                {
                    board[i, j] = E_Cross.player;
                    UpdateAllFeatures(i, j);
                    eval = Mathf.Min(MaxValue(alpha, beta, depth + 1), eval);
                    board[i, j] = E_Cross.none;
                    UpdateAllFeatures(i, j);
                    if (eval <= alpha)
                        return eval;
                    beta = Mathf.Min(beta, eval);
                }
            }
        }
        return eval;
    }
}
