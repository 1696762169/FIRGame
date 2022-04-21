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
    public bool win;                    // �Ƿ�ʤ��

    /// <summary>
    /// ��ʼ��Ϸ
    /// </summary>
    public void GameStart()
    {
        // �����������
        CurPlayer = startPlayer;
        CurPiece = E_PieceType.black;
        Chessboard.Instance.gameObject.GetComponent<UIButton>().enabled = CurPlayer == E_Player.player;

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
                win = true;
                EndPanel.Instance.ShowMe();         // չʾʤ������
            }
            else
                AIGo();                             // AI���в���
        }
    }

    /// <summary>
    /// ����AI�Ļغ�
    /// </summary>
    public void AIGo()
    {
        // ����Ȩ����AI
        SetCurrentPlayer(E_Player.AI);

        // AI���Ӳ������̴�������
        Vector2Int pos = AIGamer.Instance.Go();
        Chessboard.Instance.CreatePiece(pos.x, pos.y);

        // ���AI�Ƿ�ʤ��
        if (GoalTest())
        {
            win = false;
            EndPanel.Instance.ShowMe();         // չʾʧ�ܽ���
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

        // ��ʾ��ʾ��Ϣ
    }
    protected void Awake() => instance = this;
    protected bool GoalTest() => AIGamer.Instance.GoalTest;
}
