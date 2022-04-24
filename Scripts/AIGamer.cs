//#define DEBUG_FEATURE
//#define DEBUG_FEATURENUM
//#define DEBUG_NODENUM
//#define DEBUG_MAXDEPTH
#define DEBUG_EVERYDEPTH_EVAL
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
    protected Vector2Int curNextPos;

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

    // (当前)搜索最大深度
    protected int maxDepth;
    protected int curDepth;
    protected int curMaxDepth;
    // 最大/当前搜索结点数
    protected int maxNodeNum;
    protected int nodeNum;

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
        maxDepth = Mathf.Min(maxDepth, Chessboard.lineNum * Chessboard.lineNum - Chessboard.Instance.history.Count);

        AlphaBetaSearch();
        // 将棋盘特征与计算前的特征同步
        AIFi = new FeatureInfo(originAIFi);
        playerFi = new FeatureInfo(originPlayerFi);

#if DEBUG_NODENUM
        Debug.Log($"搜索结点数：{nodeNum}");
#endif
#if DEBUG_MAXDEPTH
        Debug.Log($"搜索最大深度：{curMaxDepth}");
#endif

        return nextPos;
    }

    /* 以下为评价函数 */

    /// <summary>
    /// 当前状态的评价函数
    /// </summary>
    protected int Eval()
    {
        return (int)(Eval(E_Player.AI) - playerEvalScale * Eval(E_Player.player));
        //return (int)(AIEval - playerEvalScale * playerEval);
    }
    /// <summary>
    /// 统计某一方的评价函数值
    /// </summary>
    protected int Eval(E_Player side)
    {
        int eval = 0;
        FeatureInfo fi = side == E_Player.AI ? AIFi : playerFi;
        foreach (Feature f in fi.Features)
            eval += (int)f.type * (f.blocked ? 1 : Feature.liveScale);
#if DEBUG_FEATURE
        if (GameMgr.Instance.CurPlayer == E_Player.player)
            return eval;
        string sideStr = side == E_Player.player ? "玩家" : "AI";
        foreach (Feature f in fi.Features)
        {
            if (f.type != E_FeatureType.single)
                Debug.Log(string.Format("归属方：{0, -5}方向：{1,-3}行数：{2,-3}位置：{3,-3}类型：{4}", sideStr, f.dir, f.line, f.loca, f.type));
        }
#endif
        return eval;
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

                Feature f = new Feature(E_FeatureType.none, false, dir, line, loca);
                // 该分段过小时视为被封堵
                f.blocked = space == 5;

                // 该分段剩余空间 接下来按剩余空间搜索
                int lastSpace = sap.y - loca;
                // 记录当前特征使用的空间
                int used = 0;

                // 需要1空间
                if (loca + 1 > lastEnd)
                {
                    f.type = E_FeatureType.single;
                    f.blocked |= loca == 0 || pieces[loca - 1] == enemy || pieces[loca + 1] == enemy;
                    used = 1;
                }

                // 需要2空间
                if (lastSpace >= 2 && pieces[loca + 1] != E_Cross.none && loca + 2 > lastEnd)
                {
                    // 连二
                    f.type = E_FeatureType.near2;
                    f.blocked |= pieces[loca + 2] == enemy;
                    used = 2;
                }

                // 需要3空间
                if (lastSpace >= 3 && pieces[loca + 2] != E_Cross.none && loca + 3 > lastEnd)
                {
                    // 小跳二
                    if (pieces[loca + 1] == E_Cross.none)
                        f.type = E_FeatureType.jump2;
                    // 连三
                    else
                        f.type = E_FeatureType.near3;
                    f.blocked |= pieces[loca + 3] == enemy;
                    used = 3;
                }

                // 需要4空间
                if (lastSpace >= 4 && pieces[loca + 3] != E_Cross.none && loca + 4 > lastEnd)
                {
                    // 大跳二
                    if (pieces[loca + 1] == E_Cross.none && pieces[loca + 2] == E_Cross.none)
                        f.type = E_FeatureType.far2;
                    // 跳三
                    else if (pieces[loca + 1] != pieces[loca + 2])
                        f.type = E_FeatureType.jump3;
                    // 连四
                    else
                        f.type = E_FeatureType.near4;
                    f.blocked |= pieces[loca + 4] == enemy;
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
                        f.type = E_FeatureType.jump4;
                        used = 5;
                    }
                    // 连五
                    if (blankCount == 0)
                    {
                        f.type = E_FeatureType.five;
                        used = 5;
                    }
                    f.blocked |= pieces[loca + 5] == enemy;
                }

                // 加入特征
                fi.Features.Add(f);
                // 由于此位置已经产生了特征起始点 故后续的连续相同棋子都无需考虑
                while (++loca < sap.y && pieces[loca] != E_Cross.none)
                    ;
                // 记录当前特征后一个位置
                lastEnd = loca + used;
            }// end of each sap
        }// end of foreach statement
    }

    /* 以下为获取最有价值位置相关函数 */

    /// <summary>
    /// 根据强特征获取必须封堵/直接取胜的位置
    /// </summary>
    protected List<Vector2Int> GetUrgentPositions(Feature f, bool defence = true)
    {
        // 加入可能位置
        List<Vector2Int> positions = new List<Vector2Int>();
        switch (f.type)
        {
        case E_FeatureType.near4:
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca - 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 4));
            break;
        case E_FeatureType.jump4:
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 2));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 3));
            break;
        case E_FeatureType.near3:
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca - 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 3));
            break;
        case E_FeatureType.jump3:
            if (defence)
            {
                positions.Add(FeatureToBoard(f.dir, f.line, f.loca - 1));
                positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 4));
            }
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 2));
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
    /// 根据相互配合必须封堵/直接取胜的特征计算其配合位置
    /// </summary>
    protected Vector2Int GetValuablePositions(ref List<Vector2Int> positions, List<Feature> goodFs, ref bool double3)
    {
        int[,] added = new int[lineNum, lineNum];
        bool[,] is3 = new bool[lineNum, lineNum];
        Vector2Int bestFeatures = new Vector2Int(0, 0);
        if (goodFs.Count >= 2)
        {
            for (int i = 0; i < goodFs.Count; ++i)
            {
                foreach (Vector2Int pos in GetNearPositions(goodFs[i]))
                    if (added[pos.x, pos.y] != 0)
                    {
                        positions.Add(pos);
                        bestFeatures.x = added[pos.x, pos.y] - 1;
                        bestFeatures.y = i;
                        double3 = !(is3[pos.x, pos.y] || goodFs[i].type == E_FeatureType.near3 || goodFs[i].type == E_FeatureType.jump3);
                    }
                    else
                    {
                        added[pos.x, pos.y] = i + 1;
                        if (goodFs[i].type == E_FeatureType.near3 || goodFs[i].type == E_FeatureType.jump3)
                            is3[pos.x, pos.y] = true;
                    }
            }
        }
        return bestFeatures;
    }
    /// <summary>
    /// 根据弱特征获取其附近相关的位置
    /// </summary>
    protected List<Vector2Int> GetNearPositions(Feature f)
    {
        // 加入可能位置
        List<Vector2Int> positions = new List<Vector2Int>();
        switch (f.type)
        {
        case E_FeatureType.single:
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca - 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 1));
            break;
        case E_FeatureType.near2:
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca - 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 2));
            break;
        case E_FeatureType.jump2:
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca - 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 3));
            break;
        case E_FeatureType.far2:
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca - 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 2));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 4));
            break;
        case E_FeatureType.near3:
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca - 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 3));
            break;
        case E_FeatureType.jump3:
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca - 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 2));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 4));
            break;
        case E_FeatureType.near4:
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca - 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 4));
            break;
        case E_FeatureType.jump4:
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
    /// <summary>
    /// 根据所有弱特征获取其附近所有相关位置
    /// </summary>
    /// <param name="positions"></param>
    /// <param name="NormalFs"></param>
    protected void GetAllNearPositions(ref List<Vector2Int> positions, List<Feature> NormalFs)
    {
        // 初始化一个记录某位置是否已经加入考虑的数组
        bool[,] added = new bool[lineNum, lineNum];
        foreach (Feature f in NormalFs)
        {
            foreach (Vector2Int pos in
                GetNearPositions(f))
                if (!added[pos.x, pos.y])
                {
                    positions.Add(pos);
                    added[pos.x, pos.y] = true;
                }
        }
    }
    /// <summary>
    /// 将单特征纳入考虑 直到特征数量达到上限
    /// </summary>
    protected void AddSingleFeatures(ref List<Feature> fList, FeatureInfo fi)
    {
        const int maxFeatureNum = 20;
        foreach (Feature f in fi.Features)
        {
            if (fList.Count > maxFeatureNum)
                break;
            if (f.type == E_FeatureType.single)
                fList.Add(f);
        }
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
        List<Feature> selfGoodFeatures = new List<Feature>();
        List<Feature> selfNormalFeatures = new List<Feature>();
        List<Feature> enemyGoodFeatures = new List<Feature>();
        List<Feature> enemyNormalFeatures = new List<Feature>();
        // 记录可能有价值的位置
        List<Vector2Int> atkPositions = new List<Vector2Int>();
        List<Vector2Int> defPositions = new List<Vector2Int>();

        // 查找己方能成五的位置 并将己方有价值特征加入考虑
        foreach (Feature sf in selfFi.Features)
        {
            switch (sf.type)
            {
            // 己方能成五直接返回
            case E_FeatureType.near4:
            case E_FeatureType.jump4:
                return GetUrgentPositions(sf);
            // 己方有活三价值很大 有死三价值较大
            case E_FeatureType.near3:
            case E_FeatureType.jump3:
                if (sf.blocked)
                    selfGoodFeatures.Add(new Feature(sf));
                else
                    atkPositions = GetUrgentPositions(sf, false);
                    break;
            // 活二价值较大 死二纳入考虑
            case E_FeatureType.near2:
            case E_FeatureType.jump2:
            case E_FeatureType.far2:
                if (sf.blocked)
                    selfNormalFeatures.Add(new Feature(sf));
                else
                    selfGoodFeatures.Add(new Feature(sf));
                break;
            // 其它特征不予考虑
            default:
                break;
            }
        }

        // 查找必须封堵的位置 并将敌方有价值特征加入考虑
        foreach (Feature ef in enemyFi.Features)
        {
            switch (ef.type)
            {
            // 敌方有四必须封堵
            case E_FeatureType.near4:
            case E_FeatureType.jump4:
                return GetUrgentPositions(ef);
            // 敌方有活三急需封堵 有死三需要警惕
            case E_FeatureType.near3:
            case E_FeatureType.jump3:
                if (ef.blocked)
                    enemyGoodFeatures.Add(new Feature(ef));
                else
                    defPositions = GetUrgentPositions(ef);
                break;
            // 敌方有活二需要警惕 有死二纳入考虑
            case E_FeatureType.near2:
            case E_FeatureType.jump2:
            case E_FeatureType.far2:
                if (ef.blocked)
                    enemyNormalFeatures.Add(new Feature(ef));
                else
                    enemyGoodFeatures.Add(new Feature(ef));
                break;
            // 其它特征不予考虑
            default:
                break;
            }
        }

        // 返回可能存在的己方成活四的机会
        if (atkPositions.Count > 0)
            return atkPositions;
        // 返回封堵敌方活三的位置
        if (defPositions.Count > 0)
            return defPositions;

        // 计算强力进攻位置(非双三)
        bool selfDouble3 = false;
        GetValuablePositions(ref atkPositions, selfGoodFeatures, ref selfDouble3);
        if (atkPositions.Count > 0 && !selfDouble3)
            return atkPositions;

        // 考虑是否存在急需防守位置
        bool enemyDouble3 = false;
        Vector2Int defPos = GetValuablePositions(ref defPositions, enemyGoodFeatures, ref enemyDouble3);
        if (defPositions.Count > 0)
        {
            // 重新计算所有可防守位置
            defPositions.Clear();
            bool[,] added = new bool[lineNum, lineNum];
            foreach (Vector2Int pos in GetNearPositions(enemyGoodFeatures[defPos.x]))
                if (!added[pos.x, pos.y])
                {
                    defPositions.Add(pos);
                    added[pos.x, pos.y] = true;
                }
            foreach (Vector2Int pos in GetNearPositions(enemyGoodFeatures[defPos.y]))
                if (!added[pos.x, pos.y])
                {
                    defPositions.Add(pos);
                    added[pos.x, pos.y] = true;
                }
            return defPositions;
        }

        // 考虑自己的双三
        if (atkPositions.Count > 0)
            return atkPositions;

        // 将没有配合的较强特征化为普通特征
        //foreach (Feature sf in selfGoodFeatures)
        //    selfNormalFeatures.Add(sf);
        //foreach (Feature ef in enemyGoodFeatures)
        //    enemyNormalFeatures.Add(ef);

        // 没有二及以上的特征时考虑所有单特征
        bool allSingle = selfNormalFeatures.Count == 0 && enemyNormalFeatures.Count == 0;

        // 将所有单特征存到selfNormalFeatures中
        AddSingleFeatures(ref selfNormalFeatures, selfFi);
        AddSingleFeatures(ref enemyNormalFeatures, enemyFi);

        // 计算加入考虑的特征附近有价值的位置
        if (selfNormalFeatures.Count > 0 || enemyNormalFeatures.Count > 0)
        {
            // 只有单特征时考虑所有位置
            if (allSingle)
            {
                foreach (Feature ef in enemyNormalFeatures)
                    selfNormalFeatures.Add(ef);
                GetAllNearPositions(ref atkPositions, selfNormalFeatures);
                return atkPositions;
            }
            // 计算防守位置
            if (enemyNormalFeatures.Count > 0)
            {
                GetAllNearPositions(ref defPositions, enemyNormalFeatures);
                return defPositions;
            }
            // 计算进攻位置
            else
            {
                GetAllNearPositions(ref atkPositions, selfNormalFeatures);
                return atkPositions;
            }
        }

        // 没有任何特征时 返回所有空位
        for (int i = 0; i < lineNum; ++i)
            for (int j = 0; j < lineNum; ++j)
                if (board[i, j] == E_Cross.none)
                    atkPositions.Add(new Vector2Int(i, j));
        return atkPositions;
    }

    /* 以下为α-β剪枝搜索相关函数 */
    protected void AlphaBetaSearch()
    {
        nodeNum = 0;
        curDepth = 2;
        int maxValue = int.MinValue;
        int curValue;
        while (curDepth <= maxDepth && nodeNum <= maxNodeNum)
        {
            curValue = MaxValue(int.MinValue, int.MaxValue, 0);
#if DEBUG_EVERYDEPTH_EVAL
            Debug.Log(string.Format("搜索深度：{0} 评价值：{1, -10}位置：{2}", curDepth, curValue, curNextPos));
#endif
            if (curValue > maxValue)
            {
                maxValue = curValue;
                nextPos = curNextPos;
            }
            curDepth += 1;
        }
#if DEBUG_EVAL
        Debug.Log(string.Format("最大评价值：{0, -10}最佳位置：{1}", maxValue, nextPos));
#endif
    }
    protected int MaxValue(int alpha, int beta, int depth)
    {
        ++nodeNum;
        if (depth >= curDepth || nodeNum >= maxNodeNum)
        {
#if DEBUG_MAXDEPTH
            if (depth > curMaxDepth)
                curMaxDepth = depth;
#endif
            return Eval();
        }
        int eval = int.MinValue;
        int curEval;

        List<Vector2Int> positions = GetBestPositions(E_Player.AI);
        // 只有一个可能的位置时无需继续考虑
        if (positions.Count == 1)
        {
            // 更新特征
            board[positions[0].x, positions[0].y] = E_Cross.AI;
            UpdateAllFeatures(positions[0].x, positions[0].y);
            eval = Eval();
            if (depth == 0)
                curNextPos = positions[0];
            // 恢复特征
            board[positions[0].x, positions[0].y] = E_Cross.none;
            UpdateAllFeatures(positions[0].x, positions[0].y);
            return eval;
        }

        // 考虑所有有价值位置
        foreach (Vector2Int pos in positions)
        {
            // 更新特征
            board[pos.x, pos.y] = E_Cross.AI;
            UpdateAllFeatures(pos.x, pos.y);
            curEval = MinValue(alpha, beta, depth + 1);
            if (curEval > eval)
            {
                eval = curEval;
                // 只有位于最浅层时才设定下一步位置
                if (depth == 0)
                    curNextPos = pos;
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
#if DEBUG_MAXDEPTH
            if (depth > curMaxDepth)
                curMaxDepth = depth;
#endif
            return Eval();
        }
        int eval = int.MaxValue;
        List<Vector2Int> positions = GetBestPositions(E_Player.player);

        // 只有一个可能的位置时无需继续考虑
        if (positions.Count == 1)
        {
            // 更新特征
            board[positions[0].x, positions[0].y] = E_Cross.AI;
            UpdateAllFeatures(positions[0].x, positions[0].y);
            eval = Eval();
            // 恢复特征
            board[positions[0].x, positions[0].y] = E_Cross.none;
            UpdateAllFeatures(positions[0].x, positions[0].y);
            return eval;
        }

        // 考虑所有有价值位置
        foreach (Vector2Int pos in positions)
        {
            // 更新特征
            board[pos.x, pos.y] = E_Cross.player;
            UpdateAllFeatures(pos.x, pos.y);
            eval = Mathf.Min(MaxValue(alpha, beta, depth + 1), eval);
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
