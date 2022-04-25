using System.Collections.Generic;

/// <summary>
/// 存储某一状态下所有特征
/// </summary>
/// 按特征方向存储
public class FeatureInfo
{
    public List<Feature> Features { get; set; }
    public Feature this[int index] => Features[index];
    // 能够产生有效特征的同方向斜线数
    public const int lineMax = Chessboard.lineNum * 2 - 1 - 2 * 4;
    public FeatureInfo() => Features = new List<Feature>();
    public FeatureInfo(FeatureInfo origin) => Features = new List<Feature>(origin.Features);
}

/// <summary>
/// 棋盘特征结构体
/// </summary>
public class Feature
{
    // 特征连子类型
    public E_FeatureType type;

    // 特征在方向坐标系上的位置
    public int dir;
    public int line;
    public int loca;

    // 可以考虑特征是否和其它特征在同一直线上连锁

    public Feature() : this(E_FeatureType.none, 0, 0, 0) { }
    public Feature(E_FeatureType type, int dir, int line, int loca)
    {
        this.type = type;
        this.dir = dir;
        this.line = line;
        this.loca = loca;
    }
    public Feature(Feature origin)
    {
        type = origin.type;
        dir = origin.dir;
        line = origin.line;
        loca = origin.loca;
    }
}

/// <summary>
/// 特征连子类型
/// </summary>
public enum E_FeatureType
{
    none,
    dead2,
    live2,
    dead3,
    live3,
    dead4,
    live4,
    five,
}

/// <summary>
/// 己方行棋后必杀情况枚举
/// </summary>
public enum E_GameOver
{
    none,
    selfDoubleLive3,
    enemyLive3,
    selfLive3Dead4,
    selfLive4,
    enemyDead4,
    enemyLive4,
    enemy5,
    self5,
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
