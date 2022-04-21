using System.Collections.Generic;

/// <summary>
/// 存储某一状态下所有特征
/// </summary>
/// 按特征方向存储
public class FeatureInfo
{
    /// <summary>
    /// 记录四个方向的特征
    /// </summary>
    /// 0 水平排布
    /// 1 竖直排布
    /// 2 右上-左下排布
    /// 3 左上-右下排布
    protected Feature[,,] features = new Feature[4, lineMax, Chessboard.lineNum];
    /// <summary>
    /// 每个位置上以该位置为起始点的特征信息
    /// </summary>
    /// <param name="dir">特征方向</param>
    /// <param name="line">行列数</param>
    /// <param name="loca">该行上的位置</param>
    public Feature this[int dir, int line, int loca] => features[dir, line, loca];
    // 能够产生有效特征的同方向斜线数
    public const int lineMax = Chessboard.lineNum * 2 - 1 - 2 * 4;
    public FeatureInfo()
    {
        // 新建特征
        for (int dir = 0; dir < 4; ++dir)
            for (int line = 0; line < lineMax; ++line)
                for (int loca = 0; loca < Chessboard.lineNum; ++loca)
                    features[dir, line, loca] = new Feature();

        // 将斜线上棋盘外的位置置为dead
        for (int line = 0; line < Chessboard.lineNum - 4 - 1; ++line)
            for (int loca = 0; loca < Chessboard.lineNum - line - 4 - 1; ++loca)
            {
                features[AIGamer.dir_div, line, loca].type = E_FeatureType.dead;
                features[AIGamer.dir_back, line, loca].type = E_FeatureType.dead;
            }
        for (int line = lineMax - 1; line > lineMax / 2; --line)
        {
            for (int loca = Chessboard.lineNum - 1; loca > lineMax - line  + 4 - 1; --loca)
            {
                features[AIGamer.dir_div, line, loca].type = E_FeatureType.dead;
                features[AIGamer.dir_back, line, loca].type = E_FeatureType.dead;
            }
        }
    }
    public FeatureInfo(FeatureInfo origin)
    {
        // 复制所有特征
        for (int dir = 0; dir < 4; ++dir)
            for (int line = 0; line < lineMax; ++line)
                for (int loca = 0; loca < Chessboard.lineNum; ++loca)
                    features[dir, line, loca] = new Feature(origin[dir, line, loca]);
    }
}

/// <summary>
/// 棋盘特征结构体
/// </summary>
public class Feature
{
    // 特征连子类型
    public E_FeatureType type;
    // 特征所处位置未被封堵的最大空间
    public int space;
    // 特征是否在相邻位置被单边封堵
    public bool blocked;
    // 可以考虑特征是否和其它特征在同一直线上连锁

    public Feature() : this(E_FeatureType.none, Chessboard.lineNum, false) { }
    public Feature(E_FeatureType type, int space, bool blocked)
    {
        this.type = type;
        this.space = space;
        this.blocked = blocked;
    }
    public Feature(Feature origin)
    {
        type = origin.type;
        space = origin.space;
        blocked = origin.blocked;
    }
}

/// <summary>
/// 某位置附近的棋子排布
/// </summary>
public struct NearbyInfo
{
    // 该位置的横纵坐标
    public int x;
    public int y;
    // 边界信息
    public int[,] border;
    // 棋子排布
    public int[] nearby;

    public NearbyInfo(int x, int y)
    {
        this.x = x;
        this.y = y;
        border = null;
        nearby = null;
    }
}

/// <summary>
/// 特征连子类型
/// </summary>
public enum E_FeatureType
{
    none = 0,
    single,
    near2,
    jump2,
    far2,
    near3,
    jump3,
    near4,
    jump4,
    five,
    dead,
}

/// <summary>
/// 某一特征的排布方式
/// </summary>
//public enum E_Direction
//{
//    // 水平排布
//    horizontal,
//    // 竖直排布
//    vertical,
//    // 右上-左下排布
//    divide,
//    // 左上-右下排布
//    backslash,
//}

// 横向
// 纵向
// 左下-右上
// 左上-右下
