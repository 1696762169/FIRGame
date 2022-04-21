using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Tools
{
    // 将时间浮点数(单位：秒)转为字符串
    public static string TimeToString(float time)
    {
        if (time < 60)
            return string.Format("{0:F2}秒", time);
        else if (time < 3600)
            return string.Format("{0}分{1}秒", (int)time / 60, (int)time % 60);
        else
            return string.Format("{0}时{1}分{2}秒", (int)time / 3600, (int)time % 3600 / 60, (int)time % 60);
    }

    /// <summary>
    /// 当物体为空时发出Warning/error
    /// </summary>
    /// <param name="obj">待检测的对象</param>
    /// <param name="tips">提示信息</param>
    /// <param name="error">是否产生error</param>
    public static void LogNull(object obj, string tips, bool error = false)
    {
        if (obj == null || obj.ToString() == "null")
        {
            if (error)
                Debug.LogError(tips);
            else
                Debug.LogWarning(tips);
        }
    }

    /// <summary>
    /// 当物体为空时发出Warning/error
    /// </summary>
    /// <typeparam name="T">检测物体必须挂载的组件</typeparam>
    /// <param name="obj">待检测的对象</param>
    /// <param name="tips">提示信息</param>
    /// <param name="error">是否产生error</param>
    public static void LogNull<T>(GameObject obj, string tips, bool error = false)
    {
        if (obj == null || obj.ToString() == "null" || obj.GetComponent<T>() == null)
        {
            if (error)
                Debug.LogError(tips);
            else
                Debug.LogWarning(tips);
        }
    }
}
