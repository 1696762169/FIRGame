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
        UpdateFeature = new UnityAction<int, int, int>[2, 2, 4];
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
    protected Features fs;
    // 记录本次落子前的棋盘特征
    protected Features originFs;

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
    protected UnityAction<int, int, int>[,,] UpdateFeature;

    /// <summary>
    /// 初始化属性
    /// </summary>
    public void Init()
    {
        board = new E_Cross[Chessboard.lineNum, Chessboard.lineNum];
        for (int i = 0; i < Chessboard.lineNum; ++i)
            for (int j = 0; j < Chessboard.lineNum; ++j)
                board[i, j] = E_Cross.none;
        fs = new Features();
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
        originFs = new Features(fs);
        // 随机落子
        Vector2Int pos = new Vector2Int(Random.Range(0, 14), Random.Range(0, 14));
        while(board[pos.x, pos.y] != E_Cross.none)
        {
            pos.x = Random.Range(0, 14);
            pos.y = Random.Range(0, 14);
        }

        // 将棋盘特征与计算前的特征同步
        fs = new Features(originFs);
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
        border[dir_hor, 0] = x + 4 >= Chessboard.lineNum ? Chessboard.lineNum - x - 1 : 4;
        border[dir_hor, 1] = x < 4 ? x : 4;
        // 纵向边界
        border[dir_ver, 0] = y + 4 >= Chessboard.lineNum ? Chessboard.lineNum - y - 1 : 4;
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

        // 遍历正负向
        for (int nega = 0; nega < 2; ++nega)
        {
            // 横向
            for (offset = 1; offset <= ni.border[dir_hor, nega]; ++offset)
                UpdateFeature[c, nega, offset](x + offset, y, ni.nearby[dir_hor]);
            // 纵向
            for (offset = 1; offset <= ni.border[dir_ver, 0]; ++offset)
                UpdateFeature[c, nega, offset](x, y + offset, ni.nearby[dir_ver]);
            // 左下-右上
            for (offset = 1; offset <= ni.border[dir_div, 0]; ++offset)
                UpdateFeature[c, nega, offset](x + offset, y + offset, ni.nearby[dir_div]);
            // 左上-右下
            for (offset = 1; offset <= ni.border[dir_back, 0]; ++offset)
                UpdateFeature[c, nega, offset](x + offset, y - offset, ni.nearby[dir_back]);
        }
    }

    /// 以下为特征更新函数(UpdateFeature)
    /// C: 新建棋子(create) D: 销毁棋子(destroy)
    /// P: 正向偏移 N: 负向偏移
    /// 数字表示相对原位置偏移量
    /// x: 待更新位置横坐标
    /// y: 待更新位置纵坐标
    /// nearby: 原位置该方向上的棋子排布
    protected void UF_C_P_1(int x, int y, int nearby)
    {

    }
    protected void UF_C_P_2(int x, int y, int nearby)
    {

    }
    protected void UF_C_P_3(int x, int y, int nearby)
    {

    }
    protected void UF_C_P_4(int x, int y, int nearby)
    {

    }
    protected void UF_C_N_1(int x, int y, int nearby)
    {

    }
    protected void UF_C_N_2(int x, int y, int nearby)
    {

    }
    protected void UF_C_N_3(int x, int y, int nearby)
    {

    }
    protected void UF_C_N_4(int x, int y, int nearby)
    {

    }
    protected void UF_D_P_1(int x, int y, int nearby)
    {

    }
    protected void UF_D_P_2(int x, int y, int nearby)
    {

    }
    protected void UF_D_P_3(int x, int y, int nearby)
    {

    }
    protected void UF_D_P_4(int x, int y, int nearby)
    {

    }
    protected void UF_D_N_1(int x, int y, int nearby)
    {

    }
    protected void UF_D_N_2(int x, int y, int nearby)
    {

    }
    protected void UF_D_N_3(int x, int y, int nearby)
    {

    }
    protected void UF_D_N_4(int x, int y, int nearby)
    {

    }

    protected float MaxValue()
    {
        throw new System.NotImplementedException();
    }
}
