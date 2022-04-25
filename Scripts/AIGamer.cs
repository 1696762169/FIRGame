//#define DEBUG_FEATURENUM
#define DEBUG_NODENUM
//#define DEBUG_MAXDEPTH
//#define DEBUG_FAIL
//#define DEBUG_EVERYDEPTH_EVAL
#define DEBUG_EVAL
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
    protected Vector2Int curPos;

    // 用于内部计算的棋盘特征
    protected FeatureInfo AIFi;
    protected FeatureInfo playerFi;
    // 记录本次落子前的棋盘特征
    protected FeatureInfo originAIFi;
    protected FeatureInfo originPlayerFi;

    // 特征的评价值
    protected int[] selfEval = new int[6] { 0, 20, 180, 150, 120, 100 };
    protected int[] enemyEval = new int[5] { 0, -60, -600, -8000, -10000 };
    protected int[] overEval = new int[9] 
    { 0, 99900000, -99950000, 99960000, 99970000, -99980000, -99990000, -100000000, 100000000 };

    // (当前)搜索最大深度
    protected int curDepth;
    protected int maxDepth;
    // 最大/当前搜索结点数
    protected int maxNodeNum;
    protected int nodeNum;
    // 是否搜索失败
    protected bool fail;

    // 标记是否创建过棋子
    protected bool playing;

    /// <summary>
    /// 初始化属性
    /// </summary>
    public void Init()
    {
        maxDepth = GameMgr.Instance.maxDepth;
        maxNodeNum = GameMgr.Instance.maxNodeNum;
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
        int eval = Eval(E_Player.AI);
        if (eval == overEval[(int)E_GameOver.self5] || eval == overEval[(int)E_GameOver.enemy5])
            GoalTest = true;
#if DEBUG_FEATURENUM
        if (GameMgr.Instance.CurPlayer == E_Player.player)
            return;
        int count = 0;
        foreach (Feature f in playerFi.Features)
            if (f.type != E_FeatureType.none)
                ++count;
        Debug.Log($"玩家特征有{count}个");
        count = 0;
        foreach (Feature f in AIFi.Features)
            if (f.type != E_FeatureType.none)
                ++count;
        Debug.Log($"AI特征有{count}个");
#endif
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
        curDepth = Mathf.Min(curDepth, Chessboard.lineNum * Chessboard.lineNum - Chessboard.Instance.history.Count);

        AlphaBetaSearch();
        // 将棋盘特征与计算前的特征同步
        AIFi = new FeatureInfo(originAIFi);
        playerFi = new FeatureInfo(originPlayerFi);

#if DEBUG_NODENUM
        Debug.Log($"搜索结点数：{nodeNum}");
#endif
        return nextPos;
    }

    /* 以下为评价函数 */

    /// <summary>
    /// 根据行棋者返回对其为正的评价函数值
    /// </summary>
    protected int Eval(E_Player side, List<Feature> overFeatures = null)
    {
        int eval = -10 * (int)Vector2Int.Distance(curPos, new Vector2Int(lineNum / 2, lineNum / 2));
        bool selfLive3 = false;
        bool selfDead4 = false;
        int scale = side == E_Player.player ? -1 : 1;
        FeatureInfo selfFi = side == E_Player.AI ? AIFi : playerFi;
        FeatureInfo enemyFi = side == E_Player.AI ? playerFi : AIFi;
        Feature atkFeature1 = null;
        Feature atkFeature2 = null;
        Feature defFeature = null;
        E_GameOver over = E_GameOver.none;

        // 统计我方特征
        foreach (Feature f in selfFi.Features)
        {
            switch (f.type)
            {
            case E_FeatureType.live3:
                if (selfDead4 && (int)over < (int)E_GameOver.selfLive3Dead4)
                {
                    over = E_GameOver.selfLive3Dead4;
                    atkFeature2 = f;
                }
                else if (selfLive3 && (int)over < (int)E_GameOver.selfDoubleLive3)
                {
                    over = E_GameOver.selfDoubleLive3;
                    atkFeature2 = f;
                }
                else if ((int)over < (int)E_GameOver.selfLive4)
                    atkFeature1 = f;
                selfLive3 = true;
                eval += selfEval[(int)f.type];
                break;
            case E_FeatureType.dead4:
                if (selfDead4 && (int)over < (int)E_GameOver.selfLive4)
                {
                    over = E_GameOver.selfLive4;
                    atkFeature2 = f;
                }
                else if (selfLive3 && (int)over < (int)E_GameOver.selfLive3Dead4)
                {
                    over = E_GameOver.selfLive3Dead4;
                    atkFeature2 = f;
                }
                else if ((int)over < (int)E_GameOver.selfLive4)
                    atkFeature1 = f;
                selfDead4 = true;
                eval += selfEval[(int)f.type];
                break;
            case E_FeatureType.live4:
                if ((int)over < (int)E_GameOver.selfLive4)
                {
                    over = E_GameOver.selfLive4;
                atkFeature1 = f;
                }
                break;
            case E_FeatureType.five:
                over = E_GameOver.self5;
                atkFeature1 = f;
                break;
            default:
                eval += selfEval[(int)f.type];
                break;
            }
        }

        // 统计敌方特征
        foreach (Feature f in enemyFi.Features)
        {
            switch (f.type)
            {
            case E_FeatureType.dead3:
                if (over == E_GameOver.selfDoubleLive3)
                    over = E_GameOver.none;
                eval += enemyEval[(int)f.type];
                break;
            case E_FeatureType.live3:
                if (!selfDead4 && (int)over < (int)E_GameOver.enemyLive3)
                {
                    over = E_GameOver.enemyLive3;
                    defFeature = f;
                }
                else if(over == E_GameOver.selfDoubleLive3)
                {
                    over = E_GameOver.none;
                    defFeature = f;
                }
                eval += enemyEval[(int)f.type];
                break;
            case E_FeatureType.dead4:
                if ((int)over < (int)E_GameOver.enemyDead4)
                {
                    over = E_GameOver.enemyDead4;
                    defFeature = f;
                }
                break;
            case E_FeatureType.live4:
                if ((int)over < (int)E_GameOver.enemyLive4)
                {
                    over = E_GameOver.enemyLive4;
                    defFeature = f;
                }
                break;
            case E_FeatureType.five:
                over = E_GameOver.enemy5;
                defFeature = f;
                break;
            default:
                eval += enemyEval[(int)f.type];
                break;
            }
        }

        // 获得关键特征
        if (overFeatures != null && over != E_GameOver.none)
        {
            if (overEval[(int)over] > 0)
            {
                overFeatures.Add(new Feature(atkFeature1));
                if (atkFeature2 != null)
                    overFeatures.Add(new Feature(atkFeature2));
            }
            else
                overFeatures.Add(new Feature(defFeature));
        }

        // 返回必杀情况或统计得到的特征评价值
        if (over != E_GameOver.none)
            return overEval[(int)over] * scale;
        else
            return eval * scale;
    }

    /* 以下为特征更新相关函数 */

    /// <summary>
    /// 根据发生变化的位置更新当前特征状态
    /// </summary>
    protected void UpdateAllFeatures(int x, int y)
    {
        // 删除所有关联特征
        for (int i = playerFi.Features.Count - 1; i >= 0; --i)
            if (IsAttached_BF(x, y, playerFi[i].dir, playerFi[i].line))
                playerFi.Features.RemoveAt(i);
        for (int i = AIFi.Features.Count - 1; i >= 0; --i)
            if (IsAttached_BF(x, y, AIFi[i].dir, AIFi[i].line))
                AIFi.Features.RemoveAt(i);

        E_Cross[] pieces = new E_Cross[lineNum + 1];
        // 更新横向
        for (int i = 0; i < lineNum; ++i)
            pieces[i] = board[i, y];
        AddLineFeatures(E_Player.AI, dir_hor, y, pieces);
        AddLineFeatures(E_Player.player, dir_hor, y, pieces);
        // 更新纵向
        for (int i = 0; i < lineNum; ++i)
            pieces[i] = board[x, i];
        AddLineFeatures(E_Player.AI, dir_ver, x, pieces);
        AddLineFeatures(E_Player.player, dir_ver, x, pieces);

        // 某些区域的落子不会产生斜向有效特征
        int YmX = y - x;
        if (Mathf.Abs(YmX) <= lineNum - 5)
        {
            // 更新左下-右上
            for (int i = Mathf.Max(0, YmX); i < lineNum - Mathf.Max(0, -YmX); ++i)
                pieces[i] = board[i - YmX, i];
            AddLineFeatures(E_Player.AI, dir_div, FeatureInfo.lineMax / 2 - YmX, pieces, Mathf.Max(0, YmX), lineNum - Mathf.Max(0, -YmX));
            AddLineFeatures(E_Player.player, dir_div, FeatureInfo.lineMax / 2 - YmX, pieces, Mathf.Max(0, YmX), lineNum - Mathf.Max(0, -YmX));
        }
        int XpYp1 = x + y + 1;
        if (Mathf.Abs(lineNum - XpYp1) <= lineNum - 5)
        {
            // 更新左上-右下
            for (int i = Mathf.Max(0, XpYp1 - lineNum); i < Mathf.Min(lineNum, XpYp1); ++i)
                pieces[i] = board[i, XpYp1 - 1 - i];
            AddLineFeatures(E_Player.AI, dir_back, FeatureInfo.lineMax + 4 - XpYp1, pieces, Mathf.Max(0, XpYp1 - lineNum), Mathf.Min(lineNum, XpYp1));
            AddLineFeatures(E_Player.player, dir_back, FeatureInfo.lineMax + 4 - XpYp1, pieces, Mathf.Max(0, XpYp1 - lineNum), Mathf.Min(lineNum, XpYp1));
        }
    }
    /// <summary>
    /// 增加某方向上一条直线的全部特征
    /// </summary>
    /// <param name="side">待更新的特征归属方</param>
    /// <param name="dir">直线方向</param>
    /// <param name="line">行列数</param>
    /// <param name="pieces">该条直线上的所有棋子分布情况</param>
    protected void AddLineFeatures(E_Player side, int dir, int line, E_Cross[] pieces, int start = 0, int end = lineNum)
    {
        // 待更新数组
        FeatureInfo fi = side == E_Player.AI ? AIFi : playerFi;
        // 对手棋子
        E_Cross enemy = side == E_Player.AI ? E_Cross.player : E_Cross.AI;
        // pieces尾部增加一个对手棋子方便判断
        pieces[end] = enemy;

        // 将该行棋子按E_Cross.enemy与E_Feature.dead划分为多段 记录其起点与终点后一个位置
        List<Vector2Int> saps = new List<Vector2Int>();
        Vector2Int cur = new Vector2Int(-1, -1);
        bool newSap = true;
        for (int loca = start; loca < end; ++loca)
        {
            if (pieces[loca] == enemy)
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

        // 将长度小于五的分段移除
        for (int i = saps.Count - 1; i >= 0; --i)
            if (saps[i].y - saps[i].x < 5)
                saps.RemoveAt(i);

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
                    continue;

                Feature f = new Feature(E_FeatureType.none, dir, line, loca);

                // 该分段剩余空间 接下来按剩余空间搜索
                int lastSpace = sap.y - loca;
                // 记录当前特征使用的空间
                int used = 0;
                // 记录是否被封堵
                bool blocked = space == 5 || loca == 0 || pieces[loca - 1] == enemy;

                // 需要2空间
                if (lastSpace >= 2 && pieces[loca + 1] != E_Cross.none && loca + 2 > lastEnd)
                {
                    blocked |= pieces[loca + 2] == enemy;
                    // 死/活二
                        f.type = blocked ? E_FeatureType.dead2 : E_FeatureType.live2;
                    used = 2;
                }

                // 需要3空间
                if (lastSpace >= 3 && pieces[loca + 2] != E_Cross.none && loca + 3 > lastEnd)
                {
                    blocked |= pieces[loca + 3] == enemy;
                    if (pieces[loca + 1] == E_Cross.none)
                        // 死/活二
                        f.type = blocked ? E_FeatureType.dead2 : E_FeatureType.live2;
                    else
                        // 死/活三
                        f.type = blocked ? E_FeatureType.dead3 : E_FeatureType.live3;
                    used = 3;
                }

                // 需要4空间
                if (lastSpace >= 4 && pieces[loca + 3] != E_Cross.none && loca + 4 > lastEnd)
                {
                    blocked |= pieces[loca + 4] == enemy;
                    if (pieces[loca + 1] == E_Cross.none && pieces[loca + 2] == E_Cross.none)
                        // 死/活二
                        f.type = blocked ? E_FeatureType.dead2 : E_FeatureType.live2;
                    else if (pieces[loca + 1] != E_Cross.none && pieces[loca + 2] != E_Cross.none)
                        // 死/活四
                        f.type = blocked ? E_FeatureType.dead4 : E_FeatureType.live4;
                    else
                        // 死/活三
                        f.type = blocked ? E_FeatureType.dead3 : E_FeatureType.live3;
                    used = 4;
                }

                // 需要5空间
                if (lastSpace >= 5 && pieces[loca + 4] != E_Cross.none && loca + 5 > lastEnd)
                {
                    // 统计中间三个位置的空位数
                    int blankCount = 0;
                    for (int i = 1; i <= 3; ++i)
                        blankCount += pieces[loca + i] == E_Cross.none ? 1 : 0;

                    blocked |= pieces[loca + 5] == enemy;
                    switch (blankCount)
                    {
                    case 3:     // 死二
                        if (!blocked)
                            f.type = E_FeatureType.dead2;
                        break;
                    case 2:     // 死三
                        f.type = E_FeatureType.dead3;
                        break;
                    case 1:     // 死四
                        f.type = E_FeatureType.dead4;
                        break;
                    case 0:     // 成五
                        f.type = E_FeatureType.five;
                        break;
                    }
                    if (!(blankCount == 0 && blocked))
                        used = 5;
                }

                // 加入特征
                if (f.type != E_FeatureType.none)
                {
                    // 记录当前特征后一个位置
                    lastEnd = loca + used;
                    fi.Features.Add(f);
                }
                // 由于此位置已经产生了特征起始点 故后续的连续相同棋子都无需考虑
                while (++loca < sap.y && pieces[loca] != E_Cross.none)
                    ;
            }// end of each sap
        }// end of foreach statement
    }

    /* 以下为获取最有价值位置相关函数 */

    /// <summary>
    /// 根据己方特征获取进攻位置
    /// </summary>
    protected List<Vector2Int> GetUrgentPositions(Feature f)
    {
        // 加入可能位置
        List<Vector2Int> positions = new List<Vector2Int>();
        switch (f.type)
        {
        case E_FeatureType.live4:
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca - 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 4));
            break;
        case E_FeatureType.dead4:
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca - 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 2));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 3));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 4));
            break;
        case E_FeatureType.live3:
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca - 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 2));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 3));
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
    protected List<Vector2Int> GetBestPositions(List<Feature> overFeatures, int depth)
    {
        // 记录可能有价值的位置
        List<Vector2Int> positions = new List<Vector2Int>();

        // 有必杀特征时 返回其附近位置
        //if (overFeatures.Count != 0)
        //    //&& (depth == maxDepth - 1                   // 最底层时只需考虑必杀位置
        //    //|| depth == 0 && positions.Count == 1       // 最顶层且只有一个必杀位置时直接返回该位置
        //    //|| overFeatures[0].type != E_FeatureType.five))
        //{
        //    foreach (Feature f in overFeatures)
        //        foreach (Vector2Int pos in GetUrgentPositions(f))
        //            positions.Add(pos);
        //    if (positions.Count != 0)
        //        return positions;
        //}
        

        // 没有任何特征时 返回所有棋子附近的空位
        bool[,] added = new bool[lineNum, lineNum];
        int posNum = 16;
        Vector2Int[] tempPositions = new Vector2Int[posNum];
        for (int i = 0; i < posNum; i++)
            tempPositions[i] = new Vector2Int();
        for (int i = 0; i < lineNum; ++i)
            for (int j = 0; j < lineNum; ++j)
                if (board[i, j] != E_Cross.none)
                {
                    tempPositions[0].Set(i + 1, j);
                    tempPositions[1].Set(i, j + 1);
                    tempPositions[2].Set(i + 1, j + 1);
                    tempPositions[3].Set(i + 1, j - 1);
                    tempPositions[4].Set(i - 1, j);
                    tempPositions[5].Set(i, j - 1);
                    tempPositions[6].Set(i - 1, j - 1);
                    tempPositions[7].Set(i - 1, j + 1);
                    tempPositions[8].Set(i + 2, j);
                    tempPositions[9].Set(i, j + 2);
                    tempPositions[10].Set(i + 2, j + 2);
                    tempPositions[11].Set(i + 2, j - 2);
                    tempPositions[12].Set(i - 2, j);
                    tempPositions[13].Set(i, j - 2);
                    tempPositions[14].Set(i - 2, j - 2);
                    tempPositions[15].Set(i - 2, j + 2);
                    foreach (Vector2Int pos in tempPositions)
                    {
                        try
                        {
                            if (board[pos.x, pos.y] == E_Cross.none && !added[pos.x, pos.y])
                            {
                                positions.Add(new Vector2Int(pos.x, pos.y));
                                added[pos.x, pos.y] = true;
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
        return positions;
    }

    /* 以下为α-β剪枝搜索相关函数 */
    protected void AlphaBetaSearch()
    {
        nodeNum = 0;
        curDepth = maxDepth - 1;
        int maxValue;
        fail = false;
        do
        {
            curDepth++;
            maxValue = MaxValue(int.MinValue, int.MaxValue, 0);
        } while (nodeNum < maxNodeNum / 10);
#if DEBUG_EVAL
            Debug.Log(string.Format("最大评价值：{0, -10}最佳位置：{1}", maxValue, nextPos));
#endif
#if DEBUG_FAIL
        if (fail)
            Debug.LogWarning($"搜索{nodeNum}个节点后仍未搜索完毕");
#endif
    }
    protected int MaxValue(int alpha, int beta, int depth)
    {
        ++nodeNum;
        if (depth >= curDepth || nodeNum >= maxNodeNum)
        {
            fail = nodeNum >= maxNodeNum;
            return Eval(E_Player.player);
        }

        int eval = int.MinValue;
        int curEval;
        List<Feature> overFeatures = new List<Feature>();
        Eval(E_Player.player, overFeatures);
        List<Vector2Int> positions = GetBestPositions(overFeatures, depth);
#if DEBUG_MAXDEPTH
        int nextDepth = positions.Count < 5 ? depth : depth + 1;
        if (nextDepth == depth)
            Debug.Log($"第{depth}层重复计算");
#endif
        // 考虑所有有价值位置
        foreach (Vector2Int pos in positions)
        {
            // 更新特征
            board[pos.x, pos.y] = E_Cross.AI;
            UpdateAllFeatures(pos.x, pos.y);
            curPos = pos;
            curEval = MinValue(alpha, beta, depth + 1);
            //curEval = MinValue(alpha, beta, nextDepth);
            if (curEval > eval)
            {
                eval = curEval;
                // 只有位于最浅层时才设定下一步位置
                if (depth == 0)
                    nextPos = pos;
#if DEBUG_SEARCH
                Debug.Log(string.Format("当前最大评价函数：{0, -10}当前位置{1}", eval, nextPos));
#endif
            }
            // 恢复特征
            board[pos.x, pos.y] = E_Cross.none;
            UpdateAllFeatures(pos.x, pos.y);
            if (eval >= beta)
                return eval;
            alpha = Mathf.Max(alpha, eval);
        }
        return eval;
    }
    protected int MinValue(int alpha, int beta, int depth)
    {
        ++nodeNum;
        if (depth >= curDepth || nodeNum >= maxNodeNum)
        {
            fail = nodeNum >= maxNodeNum;
            return Eval(E_Player.AI);
        }

        int eval = int.MaxValue;
        List<Feature> overFeatures = new List<Feature>();
        Eval(E_Player.AI, overFeatures);
        List<Vector2Int> positions = GetBestPositions(overFeatures, depth);
#if DEBUG_MAXDEPTH
        int nextDepth = positions.Count < 5 ? depth : depth + 1;
        if (nextDepth == depth)
            Debug.Log($"第{depth}层重复计算");
#endif

        // 考虑所有有价值位置
        foreach (Vector2Int pos in positions)
        {
            // 更新特征
            board[pos.x, pos.y] = E_Cross.player;
            UpdateAllFeatures(pos.x, pos.y);
            curPos = pos;
            eval = Mathf.Min(MaxValue(alpha, beta, depth + 1), eval);
            //eval = Mathf.Min(MaxValue(alpha, beta, nextDepth), eval);
            // 恢复特征
            board[pos.x, pos.y] = E_Cross.none;
            UpdateAllFeatures(pos.x, pos.y);
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
    protected bool IsAttached_BF(int x, int y, int dir, int line)
    {
        switch (dir)
        {
        case dir_hor:
            return y == line;
        case dir_ver:
            return x == line;
        case dir_div:
            return y - x == FeatureInfo.lineMax / 2 - line;
        case dir_back:
            return x + y == FeatureInfo.lineMax + 3 - line;
        default:
            return false;
        }
    }
}
