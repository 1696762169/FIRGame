//#define DEBUG_FEATURE
#define DEBUG_FEATURENUM
//#define DEBUG_EVAL
//#define DEBUG_SEARCH
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
    // 玩家的评价值
    protected int playerEval;
    // AI的评价值
    protected int AIEval;
    // 玩家评价值与AI的相对比例
    protected float playerEvalScale;

    // 搜索最大深度
    protected int maxDepth;

    // 标记是否创建过棋子
    protected bool playing;

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
        playerEval = 0;
        AIEval = 0;
        playerEvalScale = GameMgr.Instance.startPlayer == E_Player.player ?
            GameMgr.Instance.playerFirstEvalScale : GameMgr.Instance.AIFirstEvalScale;
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

        // 当棋盘快满时减少搜索最大层数
        maxDepth = Mathf.Min(maxDepth, Chessboard.lineNum * Chessboard.lineNum - Chessboard.Instance.history.Count);

        int maxEval = AlphaBetaSearch();
        // 将棋盘特征与计算前的特征同步
        AIFi = new FeatureInfo(originAIFi);
        playerFi = new FeatureInfo(originPlayerFi);
#if DEBUG_EVAL
        Debug.Log(string.Format("最大评价值：{0, -10}最佳位置：{1}", maxEval, nextPos));
#endif
#if DEBUG_FEATURENUM
        int count = 0;
        foreach (Feature f in playerFi.features)
            if (f.type != E_FeatureType.none && f.type != E_FeatureType.dead)
                ++count;
        Debug.Log($"玩家特征有{count}个");
        count = 0;
        foreach (Feature f in AIFi.features)
            if (f.type != E_FeatureType.none && f.type != E_FeatureType.dead)
                ++count;
        Debug.Log($"AI特征有{count}个");
#endif
        return nextPos;
    }

    /* 以下为评价函数 */

    /// <summary>
    /// 当前状态的评价函数
    /// </summary>
    protected int Eval()
    {
        //return (int)(Eval(E_Player.AI) - playerEvalScale * Eval(E_Player.player));
        return (int)(AIEval - playerEvalScale * playerEval);
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
    /// 统计某一方在某一方向某一行的评价函数值
    /// </summary>
    protected int Eval(E_Player side, int dir, int line)
    {
        int eval = 0;
        FeatureInfo fi = side == E_Player.AI ? AIFi : playerFi;
        for (int loca = 0; loca < lineNum; ++loca)
        {
            switch (fi[dir, line, loca].type)
            {
            case E_FeatureType.none:
            case E_FeatureType.dead:
                continue;
            default:
                eval += (int)fi[dir, line, loca].type * (fi[dir, line, loca].blocked ? 1 : Feature.liveScale);
                break;
            }
        }
        return eval;
    }

    /* 以下为特征更新相关函数 */

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
        // 消除受本行影响的评价函数
        if (side == E_Player.player)
            playerEval -= Eval(E_Player.player, dir, line);
        else
            AIEval -= Eval(E_Player.AI, dir, line);
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

        // 更新评价函数
        if (side == E_Player.player)
            playerEval += Eval(E_Player.player, dir, line);
        else
            AIEval += Eval(E_Player.AI, dir, line);
    }

    /// <summary>
    /// 根据棋盘位置获取其附近特征
    /// </summary>
    /// 特征数组维度分别为 归属方 方向 特征在该行的
    protected Feature[,,] GetNearbyFeatures(int x, int y)
    {
        Feature[,,] tempFs = new Feature[2, 4, lineNum];
        for (int i = 0; i < 4; ++i)
        {
            if (i == dir_div && (FeatureInfo.lineMax / 2 < y - x || FeatureInfo.lineMax / 2 < x - y)
                || i == dir_back && (x + y < 4 || FeatureInfo.lineMax + 3 < x + y))
                continue;
            Vector3Int fpos = BoardToFeature(x, y, i);
            for (int j = 0; j < lineNum; ++j)
                tempFs[0, i, j] = new Feature(AIFi[i, fpos.y, j]);
            for (int j = 0; j < lineNum; ++j)
                tempFs[1, i, j] = new Feature(playerFi[i, fpos.y, j]);
    }
        return tempFs;
    }
    /// <summary>
    /// 根据参数存储某棋盘位置附近特征
    /// </summary>
    protected void SetNearbyFeatures(Feature[,,] tempFs, int x, int y)
    {
        for (int i = 0; i < 4; ++i)
        {
            if (i == dir_div && (FeatureInfo.lineMax / 2 < y - x || FeatureInfo.lineMax / 2 < x - y)
                || i == dir_back && (x + y < 4 || FeatureInfo.lineMax + 3 < x + y))
                continue;
            Vector3Int fpos = BoardToFeature(x, y, i);
            for (int j = 0; j < lineNum; ++j)
            {
                AIFi[i, fpos.y, j].type = tempFs[0, i, j].type;
                AIFi[i, fpos.y, j].blocked = tempFs[0, i, j].blocked;
            }
            for (int j = 0; j < lineNum; ++j)
            {
                playerFi[i, fpos.y, j].type = tempFs[1, i, j].type;
                playerFi[i, fpos.y, j].blocked = tempFs[1, i, j].blocked;
            }
        }
    }

    /* 以下为获取最有价值位置相关函数 */

    /// <summary>
    /// 根据强特征获取必须封堵的位置
    /// </summary>
    protected List<Vector2Int> GetUrgentPositions(E_FeatureType type, int dir, int line, int loca)
    {
        // 加入可能位置
        List<Vector2Int> positions = new List<Vector2Int>();
        switch (type)
        {
        case E_FeatureType.near4:
            positions.Add(FeatureToBoard(dir, line, loca - 1));
            positions.Add(FeatureToBoard(dir, line, loca + 4));
            break;
        case E_FeatureType.jump4:
            positions.Add(FeatureToBoard(dir, line, loca + 1));
            positions.Add(FeatureToBoard(dir, line, loca + 2));
            positions.Add(FeatureToBoard(dir, line, loca + 3));
            break;
        case E_FeatureType.near3:
            positions.Add(FeatureToBoard(dir, line, loca - 1));
            positions.Add(FeatureToBoard(dir, line, loca + 3));
            break;
        case E_FeatureType.jump3:
            positions.Add(FeatureToBoard(dir, line, loca - 1));
            positions.Add(FeatureToBoard(dir, line, loca + 1));
            positions.Add(FeatureToBoard(dir, line, loca + 2));
            positions.Add(FeatureToBoard(dir, line, loca + 4));
            break;
        }
        // 减去已有棋子或边界外位置
        for (int i = positions.Count - 1; i >= 0; --i)
        {
            if (positions[i].x < 0 || positions[i].y < 0 || 
                positions[i].x >= lineNum || positions[i].y >= lineNum || 
                board[positions[i].x, positions[i].y] != E_Cross.none)
                positions.RemoveAt(i);
        }
        return positions;
    }
    /// <summary>
    /// 根据弱特征获取其附近有价值的位置
    /// </summary>
    protected List<Vector2Int> GetValuablePositions(E_FeatureType type, int dir, int line, int loca)
    {
        // 加入可能位置
        List<Vector2Int> positions = new List<Vector2Int>();
        switch (type)
        {
        case E_FeatureType.single:
            // 单个特征必定是四个方向都有的 故只需考虑一个方向
            positions.Add(FeatureToBoard(dir, line, loca - 1));
            break;
        case E_FeatureType.near2:
            positions.Add(FeatureToBoard(dir, line, loca - 1));
            positions.Add(FeatureToBoard(dir, line, loca + 2));
            break;
        case E_FeatureType.jump2:
            positions.Add(FeatureToBoard(dir, line, loca - 1));
            positions.Add(FeatureToBoard(dir, line, loca + 1));
            positions.Add(FeatureToBoard(dir, line, loca + 3));
            break;
        case E_FeatureType.far2:
            positions.Add(FeatureToBoard(dir, line, loca - 1));
            positions.Add(FeatureToBoard(dir, line, loca + 1));
            positions.Add(FeatureToBoard(dir, line, loca + 2));
            positions.Add(FeatureToBoard(dir, line, loca + 4));
            break;
        case E_FeatureType.near3:
            positions.Add(FeatureToBoard(dir, line, loca - 1));
            positions.Add(FeatureToBoard(dir, line, loca + 3));
            break;
        case E_FeatureType.jump3:
            positions.Add(FeatureToBoard(dir, line, loca - 1));
            positions.Add(FeatureToBoard(dir, line, loca + 1));
            positions.Add(FeatureToBoard(dir, line, loca + 2));
            positions.Add(FeatureToBoard(dir, line, loca + 4));
            break;
        }
        // 减去已有棋子或边界外位置
        for (int i = positions.Count - 1; i >= 0; --i)
        {
            if (positions[i].x < 0 || positions[i].y < 0 ||
                positions[i].x >= lineNum || positions[i].y >= lineNum ||
                board[positions[i].x, positions[i].y] != E_Cross.none)
                positions.RemoveAt(i);
        }
        return positions;
    }
    /// <summary>
    /// 获取最有价值的落子位置
    /// </summary>
    /// <param name="side">当前准备落子者</param>
    protected List<Vector2Int> GetBestPositions(E_Player side)
    {
        // 设定敌我
        FeatureInfo selfFi = side == E_Player.player ? playerFi : AIFi;
        FeatureInfo enemyFi = side == E_Player.player ? AIFi : playerFi;
        // 记录可能有价值的特征
        List<Vector3Int> selfFeatures = new List<Vector3Int>();
        List<Vector3Int> enemyFeatures = new List<Vector3Int>();
        // 记录可能有价值的位置
        List<Vector2Int> positions = new List<Vector2Int>();

        // 查找己方能成五的位置 并将己方有价值特征加入考虑
        for (int dir = 0; dir < 4; ++dir)
        {
            for (int line = 0; line < FeatureInfo.lineMax; ++line)
            {
                for (int loca = 0; loca < lineNum; ++loca)
                {
                    switch (selfFi[dir, line, loca].type)
                    {
                    // 己方能成五直接返回
                    case E_FeatureType.near4:
                    case E_FeatureType.jump4:
                        return GetUrgentPositions(selfFi[dir, line, loca].type, dir, line, loca);
                    // 己方有活三价值很大 有死三纳入考虑
                    case E_FeatureType.near3:
                    case E_FeatureType.jump3:
                        if (!selfFi[dir, line, loca].blocked)
                            positions = GetUrgentPositions(selfFi[dir, line, loca].type, dir, line, loca);
                        else
                            selfFeatures.Add(new Vector3Int(dir, line, loca));
                        break;
                    // 其它特征纳入考虑
                    case E_FeatureType.near2:
                    case E_FeatureType.jump2:
                    case E_FeatureType.far2:
                    case E_FeatureType.single:
                        selfFeatures.Add(new Vector3Int(dir, line, loca));
                        break;
                    // 没有特征不予考虑
                    default:
                        break;
                    }
                }
            }
        }

        // 查找必须封堵的位置 并将敌方有价值特征加入考虑
        for (int dir = 0; dir < 4; ++dir)
        {
            for (int line = 0; line < FeatureInfo.lineMax; ++line)
            {
                for (int loca = 0; loca < lineNum; ++loca)
                {
                    switch (enemyFi[dir, line, loca].type)
                    {
                    // 敌方有四必须封堵
                    case E_FeatureType.near4:
                    case E_FeatureType.jump4:
                        return GetUrgentPositions(enemyFi[dir, line, loca].type, dir, line, loca);
                    // 敌方有活三急需封堵 有死三纳入考虑
                    case E_FeatureType.near3:
                    case E_FeatureType.jump3:
                        if (!enemyFi[dir, line, loca].blocked)
                        {
                            foreach (Vector2Int pos in GetUrgentPositions(enemyFi[dir, line, loca].type, dir, line, loca))
                                positions.Add(pos);
                            return positions;
                        }
                        else
                            enemyFeatures.Add(new Vector3Int(dir, line, loca));
                        break;
                    // 敌方有二纳入考虑
                    case E_FeatureType.near2:
                    case E_FeatureType.jump2:
                    case E_FeatureType.far2:
                        enemyFeatures.Add(new Vector3Int(dir, line, loca));
                        break;
                    // 其它特征不予考虑
                    default:
                        break;
                    }
                }
            }
        }

        // 返回可能存在的己方成四的机会
        if (positions.Count > 0)
            return positions;

        // 计算加入考虑的特征附近有价值的位置
        if (selfFeatures.Count > 0 || enemyFeatures.Count > 0)
        {
            // 初始化一个记录某位置是否已经加入考虑的数组
            bool[,] added = new bool[lineNum, lineNum];
            // 计算防守位置
            foreach (Vector3Int f in enemyFeatures)
            {
                foreach (Vector2Int pos in 
                    GetValuablePositions(enemyFi[f.x, f.y, f.z].type, f.x,  f.y, f.z))
                    if (!added[pos.x, pos.y])
                    {
                        positions.Add(pos);
                        added[pos.x, pos.y] = true;
                    }
            }
            // 计算进攻位置
            foreach (Vector3Int f in selfFeatures)
            {
                foreach (Vector2Int pos in
                    GetValuablePositions(selfFi[f.x, f.y, f.z].type, f.x, f.y, f.z))
                    if (!added[pos.x, pos.y])
                    {
                        positions.Add(pos);
                        added[pos.x, pos.y] = true;
                    }
            }
            return positions;
        }

        // 没有合适特征时 返回所有空位
        for (int i = 0; i < lineNum; ++i)
            for (int j = 0; j < lineNum; ++j)
                if (board[i, j] == E_Cross.none)
                    positions.Add(new Vector2Int(i, j));
        return positions;
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

        List<Vector2Int> positions = GetBestPositions(E_Player.AI);
        foreach (Vector2Int pos in positions)
        {
            // 存储更新前特征
            //List<Feature> fList = GetNearbyFeatures(pos.x, pos.y);
            // 更新特征
            board[pos.x, pos.y] = E_Cross.AI;
            UpdateAllFeatures(pos.x, pos.y);
            curEval = MinValue(alpha, beta, depth + 1);
            if (curEval > eval)
            {
                eval = curEval;
                if (depth == 0)
                {
                    nextPos.x = pos.x;
                    nextPos.y = pos.y;
                }
#if DEBUG_SEARCH
                Debug.Log(string.Format("当前最大评价函数：{0, -10}当前位置{1}", eval, nextPos));
#endif
            }
            // 恢复特征
            board[pos.x, pos.y] = E_Cross.none;
            UpdateAllFeatures(pos.x, pos.y);
            //SetNearbyFeatures(fList, pos.x, pos.y);
            if (eval >= beta)
                return eval;
            alpha = Mathf.Max(alpha, eval);
        }
        return eval;
    }
    protected int MinValue(int alpha, int beta, int depth)
    {
        if (depth >= maxDepth)
            return Eval();
        int eval = int.MaxValue;
        List<Vector2Int> positions = GetBestPositions(E_Player.player);
        foreach (Vector2Int pos in positions)
        {
            // 存储更新前特征
            Feature[,,] tempFs = GetNearbyFeatures(pos.x, pos.y);
            // 更新特征
            board[pos.x, pos.y] = E_Cross.player;
            UpdateAllFeatures(pos.x, pos.y);
            eval = Mathf.Min(MaxValue(alpha, beta, depth + 1), eval);
            // 恢复特征
            board[pos.x, pos.y] = E_Cross.none;
            UpdateAllFeatures(pos.x, pos.y);
            //SetNearbyFeatures(tempFs, pos.x, pos.y);
            if (eval <= alpha)
                return eval;
            beta = Mathf.Min(beta, eval);
        }
        return eval;
    }

    /* 以下为内部工具函数 */
    protected Vector2Int FeatureToBoard(int dir, int line, int loca)
    {
        switch (dir)
        {
        case dir_hor:
            return new Vector2Int(loca, line);
        case dir_ver:
            return new Vector2Int(line, loca);
        case dir_div:
            return new Vector2Int(line + loca - lineNum + 5, loca);
        case dir_back:
            return new Vector2Int(loca, lineNum + 9 - line - loca);
        default:
            return new Vector2Int(-1, -1);
        }
    }
    protected Vector3Int BoardToFeature(int x, int y, int dir)
    {
        switch (dir)
        {
        case dir_hor:
            return new Vector3Int(dir, y, x);
        case dir_ver:
            return new Vector3Int(dir, x, y);
        case dir_div:
            return new Vector3Int(dir, FeatureInfo.lineMax / 2 - y + x, y);
        case dir_back:
            return new Vector3Int(dir, FeatureInfo.lineMax + 3 - x - y, x);
        default:
            return new Vector3Int(-1, -1, -1);
        }
    }
}
