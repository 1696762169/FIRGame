using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ��ǰ�������
/// </summary>
public enum E_Player
{
    player,
    AI,
}

public class GameMgr : MonoBehaviour
{
    private static GameMgr instance;
    public static GameMgr Instance => instance;

    [Tooltip("�������")]
    public E_Player startPlayer = E_Player.player;
    [Tooltip("����������")]
    public int maxDepth;
    [Tooltip("������������")]
    public int maxNodeNum;

    public E_PieceType CurPiece         // ��ǰ����
    {
        get;
        protected set;
    }
    public E_Player CurPlayer           // ��ǰ���
    {
        get;
        protected set;
    }

    [HideInInspector]
    public int result;                  // ��Ϸ��� ����0�����ʤ�� С��0��AIʤ�� ����0��ƽ��

    protected int timer;                // �غϼ�ʱ��

    /// <summary>
    /// ��ʼ��Ϸ
    /// </summary>
    public void GameStart()
    {
        // �����������
        CurPlayer = startPlayer;
        CurPiece = E_PieceType.black;
        Chessboard.Instance.gameObject.GetComponent<UIButton>().enabled = CurPlayer == E_Player.player;

        // ��ʼ��ʱ
        timer = System.Environment.TickCount;

        // ��ʼAI��һ��
        if (CurPlayer == E_Player.AI)
        {
            Chessboard.Instance.CreatePiece(Chessboard.lineNum / 2, Chessboard.lineNum / 2);
            SetCurrentPlayer(E_Player.player);
        }
    }

    /// <summary>
    /// ������Ϸ
    /// </summary>
    public void GameOver()
    {
        // ���AI״̬
        AIGamer.Instance.Init();

        // �������
        while (Chessboard.Instance.history.Count != 0)
            Chessboard.Instance.DestroyPiece();
    }

    /// <summary>
    /// �������
    /// ��������̰�ť
    /// </summary>
    public void PlayerGo()
    {
        // �ж��Ƿ�ɹ�����
        if (Chessboard.Instance.PlayerTryCreatePiece())
        {
            if (GoalTest())                         // �������Ƿ�ʤ��
            {
                result = 1;
                PlayPanel.Instance.HideMe();
                EndPanel.Instance.ShowMe();         // չʾʤ������
            }
            else if (Chessboard.Instance.history.Count == Chessboard.lineNum * Chessboard.lineNum)
            {
                result = 0;
                PlayPanel.Instance.HideMe();
                EndPanel.Instance.ShowMe();         // չʾƽ�ֽ���
            }
            else
            {
                SetCurrentPlayer(E_Player.AI);      // ����Ȩ����AI
                StartCoroutine(AIGo());             // AI�����ж�
            }
        }
    }

    /// <summary>
    /// ����AI�Ļغ�
    /// </summary>
    public IEnumerator AIGo()
    {
        // �ȴ������������ʾ��Ϣ��Ⱦ��Ϻ��ٿ�ʼ����
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        // AI���Ӳ������̴�������
        Vector2Int pos = AIGamer.Instance.Go();
        Chessboard.Instance.CreatePiece(pos.x, pos.y);

        // ���AI�Ƿ�ʤ��
        if (GoalTest())
        {
            result = -1;
            PlayPanel.Instance.HideMe();
            EndPanel.Instance.ShowMe();         // չʾʧ�ܽ���
        }
        else if (Chessboard.Instance.history.Count == Chessboard.lineNum * Chessboard.lineNum)
        {
            result = 0;
            PlayPanel.Instance.HideMe();
            EndPanel.Instance.ShowMe();         // չʾƽ�ֽ���
        }
        else
            SetCurrentPlayer(E_Player.player);  // ����Ȩ�������
        System.GC.Collect();
    }

    /// <summary>
    /// ���õ�ǰ���
    /// </summary>
    protected void SetCurrentPlayer(E_Player newPlayer)
    {
        // ����/ʧ������
        bool enabled = newPlayer == E_Player.player;
        Chessboard.Instance.gameObject.GetComponent<UIButton>().enabled = enabled;
        foreach (UIButton button in PlayPanel.Instance.GetComponentsInChildren<UIButton>())
            button.enabled = enabled;

        // �������������
        if (CurPlayer != newPlayer)
        {
            CurPlayer = CurPlayer == E_Player.player ? E_Player.AI : E_Player.player;
            CurPiece = CurPiece == E_PieceType.black ? E_PieceType.white : E_PieceType.black;
        }

        // ������ʾ��Ϣ�����ü�ʱ��
        PlayPanel.Instance.UpdateTips((System.Environment.TickCount - timer) / 1000.0f);
        timer = System.Environment.TickCount;
    }
    protected void Awake() => instance = this;
    protected bool GoalTest() => AIGamer.Instance.GoalTest;
}
