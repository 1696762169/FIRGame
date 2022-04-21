using System.Collections.Generic;

/// <summary>
/// 存储某一状态下所有特征
/// </summary>
/// 按特征方向存储
public class Features
{
    /// <summary>
    /// 记录四个方向的特征
    /// </summary>
    /// 0 水平排布
    /// 1 竖直排布
    /// 2 右上-左下排布
    /// 3 左上-右下排布
    protected List<Feature>[,,] features;
    public List<Feature> this[int i, int j, int k] => features[i, j, k];

    /// <summary>
    /// 
    /// </summary>
    public static void First(int x, int y)
    {

    }

    

    public Features()
    {
        features = new List<Feature>[4, Chessboard.lineNum, Chessboard.lineNum];
        for (int i = 0; i < 4; ++i)
            for (int j = 0; j < Chessboard.lineNum; ++j)
                for (int k = 0; k < Chessboard.lineNum; ++k)
                    features[i, j, k] = new List<Feature>();
    }
    public Features(Features origin) : this()
    {
        for (int i = 0; i < 4; ++i)
            for (int j = 0; j < Chessboard.lineNum; ++j)
                for (int k = 0; k < Chessboard.lineNum; ++k)
                    foreach (Feature feature in origin.features[i, j, k])
                        features[i, j, k].Add(feature);
    }
}

/// <summary>
/// 某位置与棋盘特征的关联信息结构体
/// </summary>
public struct Feature
{
    // 特征起始点坐标 起始点指最左侧/下方/左下方/左上方
    //public int init_x;
    //public int init_y;

    // 该位置与该特征起始点在该方向上的相对位置 起始点指最左侧/下方/左下方/左上方
    public int location;
    // 特征连子类型
    public E_Adjacent adj;
    // 特征是否被封堵
    public bool alive;
    // 特征的归属方
    public E_Player side;

    public Feature(int location, E_Adjacent adj, bool alive, E_Player side)
    {
        //this.init_x = init_x;
        //this.init_y = init_y;
        this.location = location;
        this.adj = adj;
        this.alive = alive;
        this.side = side;
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
public enum E_Adjacent
{
    single,
    near2,
    jump2,
    far2,
    near3,
    jump3,
    near4,
    jump4,
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
