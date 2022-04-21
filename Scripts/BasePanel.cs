using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BasePanel : MonoBehaviour
{
    //public Color tipsColor;
    //protected virtual void Start()
    //{
    //    UILabel tips = transform.gameObject.GetComponentInChildren<UILabel>();
    //    //if (tips != null)
    //        //tips.color = tipsColor;
    //}
    public virtual void ShowMe() => gameObject.SetActive(true);
    public virtual void HideMe() => gameObject.SetActive(false);
}
