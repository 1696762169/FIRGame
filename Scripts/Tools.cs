using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Tools
{
    // ��ʱ�両����(��λ����)תΪ�ַ���
    public static string TimeToString(float time)
    {
        if (time < 60)
            return string.Format("{0:F2}��", time);
        else if (time < 3600)
            return string.Format("{0}��{1}��", (int)time / 60, (int)time % 60);
        else
            return string.Format("{0}ʱ{1}��{2}��", (int)time / 3600, (int)time % 3600 / 60, (int)time % 60);
    }

    /// <summary>
    /// ������Ϊ��ʱ����Warning/error
    /// </summary>
    /// <param name="obj">�����Ķ���</param>
    /// <param name="tips">��ʾ��Ϣ</param>
    /// <param name="error">�Ƿ����error</param>
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
    /// ������Ϊ��ʱ����Warning/error
    /// </summary>
    /// <typeparam name="T">������������ص����</typeparam>
    /// <param name="obj">�����Ķ���</param>
    /// <param name="tips">��ʾ��Ϣ</param>
    /// <param name="error">�Ƿ����error</param>
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
