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
    private AIGamer()
    {
        // 初始化棋盘与特征
        Init();

        // 初始化特征更新委托数组
        UpdateFeature = new UnityAction<int, int, int, int>[2, 2, 4];
        UpdateFeature[0, 0, 1] = UF_C_P_1;
        UpdateFeature[0, 0, 2] = UF_C_P_2;
        UpdateFeature[0, 0, 3] = UF_C_P_3;
        UpdateFeature[0, 0, 4] = UF_C_P_4;
        UpdateFeature[0, 1, 1] = UF_C_N_1;
        UpdateFeature[0, 1, 2] = UF_C_N_2;
        UpdateFeature[0, 1, 3] = UF_C_N_3;
        UpdateFeature[0, 1, 4] = UF_C_N_4;
        UpdateFeature[1, 0, 1] = UF_D_P_1;
        UpdateFeature[1, 0, 2] = UF_D_P_2;
        UpdateFeature[1, 0, 3] = UF_D_P_3;
        UpdateFeature[1, 0, 4] = UF_D_P_4;
        UpdateFeature[1, 1, 1] = UF_D_N_1;
        UpdateFeature[1, 1, 2] = UF_D_N_2;
        UpdateFeature[1, 1, 3] = UF_D_N_3;
        UpdateFeature[1, 1, 4] = UF_D_N_4;
    }

    // 用于内部计算的棋盘状态
    protected E_Cross[,] board;
    // 用于内部计算的棋盘特征
    protected FeatureInfo AIFi;
    protected FeatureInfo playerFi;
    // 记录本次落子前的棋盘特征
    protected FeatureInfo originAIFi;
    protected FeatureInfo originPlayerFi;
    // 棋盘行列数
    protected const int lineNum = Chessboard.lineNum;

    // 标记是否创建过棋子
    protected bool playing;

    // 记录四个方向对应数值
    public const int dir_hor = 0;        // 横向
    public const int dir_ver = 1;        // 纵向
    public const int dir_div = 2;        // 左下-右上
    public const int dir_back = 3;       // 左上-右下

    // 定义棋子信息位模式
    protected const int bit_player = 0b00;  // 玩家棋子
    protected const int bit_AI = 0b01;      // AI棋子
    protected const int bit_none = 0b10;    // 没有棋子
    protected const int bit_edge = 0b11;    // 边界外 此项不可更改

    /// <summary>
    /// 存储更新某位置附近某一方向的特征的函数
    /// </summary>
    /// 坐标1: 0: 创建棋子 1: 销毁棋子
    /// 坐标2: 0: 正向偏移 1: 负向偏移
    /// 坐标3: 偏移量
    protected UnityAction<int, int, int, int>[,,] UpdateFeature;

    /// <summary>
    /// 初始化属性
    /// </summary>
    public void Init()
    {
        board = new E_Cross[lineNum, lineNum];
        for (int i = 0; i < lineNum; ++i)
            for (int j = 0; j < lineNum; ++j)
                board[i, j] = E_Cross.none;
        AIFi = new FeatureInfo();
        playerFi = new FeatureInfo();
        playing = false;
    }

    /// <summary>
    /// 玩家操作时同步内部状态
    /// </summary>
    /// <param name="create">新操作是否是落子</param>
    public void Synchronize(int x, int y, bool create)
    {
        // 游戏结束清空棋盘时无需同步
        if (!playing && !create)
            return;

        // 创建棋子时先同步内部棋盘
        if (create)
        {
            playing = true;
            board[x, y] = Chessboard.Instance.Board[x, y];
        }
        // 获取各方向边界与棋子排布情况
        NearbyInfo ni = new NearbyInfo(x, y);
        ni.border = GetBorder(x, y);
        ni.nearby = GetNearbyBit(x, y, ni.border);

        if (create)
            UpdateAllFeatures(ref ni, true);
        else
        {
            // 销毁棋子时需要最后同步内部棋盘
            UpdateAllFeatures(ref ni, false);
            board[x, y] = E_Cross.none;
        }
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
        // 随机落子
        Vector2Int pos = new Vector2Int(Random.Range(0, 14), Random.Range(0, 14));
        while(board[pos.x, pos.y] != E_Cross.none)
        {
            pos.x = Random.Range(0, 14);
            pos.y = Random.Range(0, 14);
        }

        // 将棋盘特征与计算前的特征同步
        AIFi = new FeatureInfo(originAIFi);
        playerFi = new FeatureInfo(originPlayerFi);
        return pos;
    }

    /// <summary>
    /// 检测游戏是否结束
    /// </summary>
    public bool GoalTest
    {
        get;
        protected set;
    }

    /// <summary>
    /// 当前状态的评价函数
    /// </summary>
    protected float Eval()
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// 获取某位置附近4格之内的边界
    /// </summary>
    /// <returns>四个方向的正负边界</returns>
    protected int[,] GetBorder(int x, int y)
    {
        int[,] border = new int[4, 2];
        // 横向边界
        border[dir_hor, 0] = x + 4 >= lineNum ? lineNum - x - 1 : 4;
        border[dir_hor, 1] = x < 4 ? x : 4;
        // 纵向边界
        border[dir_ver, 0] = y + 4 >= lineNum ? lineNum - y - 1 : 4;
        border[dir_ver, 1] = y < 4 ? y : 4;
        // 左下-右上边界
        border[dir_div, 0] = border[dir_hor, 0] < border[dir_ver, 0] ? border[dir_hor, 0] : border[dir_ver, 0];
        border[dir_div, 1] = border[dir_hor, 1] < border[dir_ver, 1] ? border[dir_hor, 1] : border[dir_ver, 1];
        // 左上-右下边界
        border[dir_back, 0] = border[dir_hor, 0] < border[dir_ver, 1] ? border[dir_hor, 0] : border[dir_ver, 1];
        border[dir_back, 1] = border[dir_hor, 1] < border[dir_ver, 0] ? border[dir_hor, 1] : border[dir_ver, 0];
        return border;
    }

    /// <summary>
    /// 获取某棋子附近各方向的棋子信息位存储模式
    /// </summary>
    /// <param name="border">各方向边界信息</param>
    /// <returns>各方向棋子排布情况
    /// 共9个位置 占用低18位存储
    /// 00:玩家棋子 01:AI棋子 10:空位 11:棋盘外</returns>
    protected int[] GetNearbyBit(int x, int y, int[,] border)
    {
        // 初始化为全边界外
        int[] nearby = new int[4] { -1, -1, -1, -1 };

        // 设置中心位置
        int setting = (0b11 ^ (int)board[x, y]) << 8;
        for (int i = 0; i < 4; ++i)
            nearby[i] ^= setting;

        // 设置横向
        for (int offset = 1; offset <= border[dir_hor, 0]; ++offset)
            nearby[dir_hor] ^= (0b11 ^ (int)board[x + offset, y]) << (4 + offset) * 2;
        for (int offset = 1; offset <= border[dir_hor, 1]; ++offset)
            nearby[dir_hor] ^= (0b11 ^ (int)board[x - offset, y]) << (4 - offset) * 2;
        // 设置纵向
        for (int offset = 1; offset <= border[dir_ver, 0]; ++offset)
            nearby[dir_ver] ^= (0b11 ^ (int)board[x, y + offset]) << (4 + offset) * 2;
        for (int offset = 1; offset <= border[dir_ver, 1]; ++offset)
            nearby[dir_ver] ^= (0b11 ^ (int)board[x, y - offset]) << (4 - offset) * 2;
        // 设置左下-右上
        for (int offset = 1; offset <= border[dir_div, 0]; ++offset)
            nearby[dir_div] ^= (0b11 ^ (int)board[x + offset, y + offset]) << (4 + offset) * 2;
        for (int offset = 1; offset <= border[dir_div, 1]; ++offset)
            nearby[dir_div] ^= (0b11 ^ (int)board[x - offset, y - offset]) << (4 - offset) * 2;
        // 设置左上-右下
        for (int offset = 1; offset <= border[dir_back, 0]; ++offset)
            nearby[dir_back] ^= (0b11 ^ (int)board[x + offset, y - offset]) << (4 + offset) * 2;
        for (int offset = 1; offset <= border[dir_back, 1]; ++offset)
            nearby[dir_back] ^= (0b11 ^ (int)board[x - offset, y + offset]) << (4 - offset) * 2;
        return nearby;
    }

    /// <summary>
    /// 根据发生变化的位置更新当前特征状态
    /// </summary>
    /// <param name="ni">附近信息</param>
    /// <param name="c">是否是新建棋子</param>
    protected void UpdateAllFeatures(ref NearbyInfo ni, bool create)
    {
        int x = ni.x;
        int y = ni.y;
        int offset;
        int c = create ? 0 : 1;

        //int lineMax;
        //if (dir == dir_hor || dir == dir_ver)
        //    lineMax = lineNum;
        //else
        //    lineMax = FeatureInfo.lineMax;

        //// 遍历正负向
        //for (int nega = 0; nega < 2; ++nega)
        //{
        //    // 横向
        //    for (offset = 1; offset <= ni.border[dir_hor, nega]; ++offset)
        //        UpdateFeature[c, nega, offset](x + offset, y, dir_hor, ni.nearby[dir_hor]);
        //    // 纵向
        //    for (offset = 1; offset <= ni.border[dir_ver, 0]; ++offset)
        //        UpdateFeature[c, nega, offset](x, y + offset, dir_ver, ni.nearby[dir_ver]);
        //    // 左下-右上
        //    for (offset = 1; offset <= ni.border[dir_div, 0]; ++offset)
        //        UpdateFeature[c, nega, offset](x + offset, y + offset, dir_div, ni.nearby[dir_div]);
        //    // 左上-右下
        //    for (offset = 1; offset <= ni.border[dir_back, 0]; ++offset)
        //        UpdateFeature[c, nega, offset](x + offset, y - offset, dir_back, ni.nearby[dir_back]);
        //}
    }



    /// <summary>
    /// 更新某方向上一条直线的全部特征
    /// </summary>
    /// <param name="side">待更新的特征归属方</param>
    /// <param name="dir">直线方向</param>
    /// <param name="line">行列数</param>
    /// <param name="pieces">该条直线上的所有棋子分布情况</param>
    /// 此时已将棋盘范围外与存在敌方棋子当作相同情况
    /// 且pieces尾部增加一个“敌方棋子”方便判断
    protected void UpdateLineFeatures(E_Player side, int dir, int line, E_Cross[] pieces)
    {
        // 待更新数组
        FeatureInfo fi = side == E_Player.AI ? AIFi : playerFi;
        // 对手棋子
        E_Cross enemy = side == E_Player.AI ? E_Cross.player : E_Cross.AI;

        // 将该行棋子按E_Cross.enemy与E_Feature.dead划分为多段 记录其起点与终点后一个位置
        List<Vector2Int> saps = new List<Vector2Int>();
        Vector2Int cur = new Vector2Int(-1, -1);
        bool newSap = true;
        for (int loca = 0; loca < lineNum; ++loca)
        {
            if (pieces[loca] == enemy || fi[dir, line, loca].type == E_FeatureType.dead)
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

        // 将长度小于五的分段设为dead 并移除分段
        for (int i = saps.Count - 1; i >= 0; --i)
        {
            if (saps[i].y - saps[i].x < 5)
            {
                for (int loca = saps[i].x; loca < saps[i].y; ++loca)
                    fi[dir, line, loca].type = E_FeatureType.dead;
                saps.RemoveAt(i);
            }
        }
       
        // 分别更新每个分段
        foreach (Vector2Int sap in saps)
        {
            // 该分段总空间
            int space = sap.y - sap.x;
            for (int loca = sap.x; loca < sap.y; ++loca)
            {
                // 没有棋子的位置不可能作为特征起始点
                if (pieces[loca] == E_Cross.none)
                {
                    fi[dir, line, loca].type = E_FeatureType.none;
                    continue;
                }

                // 将该特征附近的空间置为该分段总空间
                fi[dir, line, loca].space = space;

                // 该分段总空间与剩余空间 接下来按剩余空间搜索
                int lastSpace = sap.y - loca;

                // 需要1空间
                fi[dir, line, loca].type = E_FeatureType.single;
                fi[dir, line, loca].blocked = loca == 0 || pieces[loca - 1] == enemy || pieces[loca + 1] == enemy;

                // 需要2空间
                if (lastSpace >= 2 && pieces[loca + 1] != E_Cross.none)
                {
                    // 连二
                    fi[dir, line, loca].type = E_FeatureType.near2;
                    fi[dir, line, loca].blocked |= pieces[loca + 2] == enemy;
                }

                // 需要3空间
                if (lastSpace >= 3 && pieces[loca + 2] != E_Cross.none)
                {
                    // 小跳二
                    if (pieces[loca + 1] == E_Cross.none)
                        fi[dir, line, loca].type = E_FeatureType.jump2;
                    // 连三
                    else
                        fi[dir, line, loca].type = E_FeatureType.near3;
                    fi[dir, line, loca].blocked |= pieces[loca + 3] == enemy;
                }

                // 需要4空间
                if (lastSpace >= 4 && pieces[loca + 3] != E_Cross.none)
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
                }

                // 需要5空间
                if (lastSpace >= 5 && pieces[loca + 4] != E_Cross.none)
                {
                    // 统计中间三个位置的空位数
                    int blankCount = 0;
                    for (int i = 1; i <= 3; ++i)
                        blankCount += pieces[loca + i] == E_Cross.none ? 1 : 0;

                    // 跳四
                    if (blankCount == 1)
                        fi[dir, line, loca].type = E_FeatureType.jump4;
                    // 连五
                    if (blankCount == 0)
                        fi[dir, line, loca].type = E_FeatureType.five;
                    fi[dir, line, loca].blocked |= pieces[loca + 5] == enemy;
                }

                // 由于此位置已经产生了特征起始点 故后续的连续相同棋子都无需考虑
                while (++loca < sap.y && pieces[loca] != E_Cross.none)
                    fi[dir, line, loca].type = E_FeatureType.none;
            }// end of each sap
        }// end of foreach statement
    }



    /// 以下为特征更新函数(UpdateFeature)
    /// C: 新建棋子(create) D: 销毁棋子(destroy)
    /// P: 正向偏移 N: 负向偏移
    /// 数字表示相对原位置偏移量
    /// x: 待更新位置横坐标
    /// y: 待更新位置纵坐标
    /// dir: 相对方向
    /// nearby: 原位置该方向上的棋子排布
    protected void UF_C_P_1(int x, int y, int dir, int nearby)
    {
        
    }
    protected void UF_C_P_2(int x, int y, int dir, int nearby)
    {

    }
    protected void UF_C_P_3(int x, int y, int dir, int nearby)
    {

    }
    protected void UF_C_P_4(int x, int y, int dir, int nearby)
    {

    }
    protected void UF_C_N_1(int x, int y, int dir, int nearby)
    {

    }
    protected void UF_C_N_2(int x, int y, int dir, int nearby)
    {

    }
    protected void UF_C_N_3(int x, int y, int dir, int nearby)
    {

    }
    protected void UF_C_N_4(int x, int y, int dir, int nearby)
    {

    }
    protected void UF_D_P_1(int x, int y, int dir, int nearby)
    {

    }
    protected void UF_D_P_2(int x, int y, int dir, int nearby)
    {

    }
    protected void UF_D_P_3(int x, int y, int dir, int nearby)
    {

    }
    protected void UF_D_P_4(int x, int y, int dir, int nearby)
    {

    }
    protected void UF_D_N_1(int x, int y, int dir, int nearby)
    {

    }
    protected void UF_D_N_2(int x, int y, int dir, int nearby)
    {

    }
    protected void UF_D_N_3(int x, int y, int dir, int nearby)
    {

    }
    protected void UF_D_N_4(int x, int y, int dir, int nearby)
    {

    }

    protected float MaxValue()
    {
        throw new System.NotImplementedException();
    }
}
