﻿using System.Collections.Generic;

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
    //public Feature[,,] features = new Feature[4, lineMax, Chessboard.lineNum];
    /// <summary>
    /// 每个位置上以该位置为起始点的特征信息
    /// </summary>
    /// <param name="dir">特征方向</param>
    /// <param name="line">行列数</param>
    /// <param name="loca">该行上的位置</param>
    //public Feature this[int dir, int line, int loca] => features[dir, line, loca];

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

    // 特征是否被封堵
    public bool blocked;
    // 定义未被封堵时的特征评价函数值倍率
    public const int liveScale = 2;

    // 特征在方向坐标系上的位置
    public int dir;
    public int line;
    public int loca;

    // 可以考虑特征是否和其它特征在同一直线上连锁

    public Feature() : this(E_FeatureType.none, false, 0, 0, 0) { }
    public Feature(E_FeatureType type, bool blocked, int dir, int line, int loca)
    {
        this.type = type;
        this.blocked = blocked;
        this.dir = dir;
        this.line = line;
        this.loca = loca;
    }
    public Feature(Feature origin)
    {
        type = origin.type;
        blocked = origin.blocked;
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
    none = 0,
    single = 1000,
    near2 = 10000,
    jump2 = 9000,
    far2 = 8000,
    near3 = 30000,
    jump3 = 25000,
    near4 = 60000,
    jump4 = 45000,
    five = 10000000,    // 不设置为int.MaxValue 防止计算时溢出 但需要保证足够大使AI必定不会错过五连
    //dead = -1,
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
