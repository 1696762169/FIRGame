using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ��������ö��
/// </summary>
public enum E_PieceType
{
    black,
    white,
}

/// <summary>
/// �������Ӵ���״̬
/// </summary>
public enum E_Cross
{
    none,
    player,
    AI,
}

/// <summary>
/// ������
/// ����������Ϣ����ͼƬ����
/// ��������ϵ���Ҳ�ΪX�������Ϸ�ΪY���������½�Ϊԭ��
/// </summary>
public class Chessboard : MonoBehaviour
{
    private static Chessboard instance;
    public static Chessboard Instance => instance;

    // ����������Ϣ
    [Tooltip("����������")]
    public int crossSize;
    [Tooltip("���̱߽�����")]
    public int edge;
    [Tooltip("���Ӽ������")]
    public int piecesGap;
    private int boardSize;                  // �����ܳ�������

    // ������Ϣ
    [HideInInspector]
    protected E_Cross[,] board;             // �����ϵ�������Ϣ
    public E_Cross[,] Board => board;
    private GameObject[,] pieceSprites;     // ����ͼƬ���󣬷�����������
    public const int lineNum = 15;          // ����������

    // ������ʷ
    public Stack<Vector2Int> history = new Stack<Vector2Int>();

    // �ڰ�����Ԥ����
    private GameObject blackPiece;
    private GameObject whitePiece;

    // ���� ��Ч���� ��Ļ����ϵ�µ�λ��
    private Rect EnableRect
    {
        get
        {
            Rect enableRect = new Rect();

            // UIRoot�ж���Ŀ��
            int rootWidth = transform.GetComponentInParent<UIRoot>().manualWidth;
            int rootHeight = transform.GetComponentInParent<UIRoot>().manualHeight;
            // ���ڿ����UIRoot�ж���Ŀ�ȵı�ֵ
            float rate = (float)Screen.width / rootWidth;

            // �������ռ�����
            int left = GetComponent<UITexture>().leftAnchor.absolute;
            // ������ֱ���򳤶�����
            int vertical = GetComponent<UITexture>().topAnchor.absolute - GetComponent<UITexture>().bottomAnchor.absolute;

            // ��������λ�����С
            enableRect.x = (left + edge) * rate;
            enableRect.y = (Screen.height - (boardSize - 2 * edge) * rate) / 2;
            enableRect.width = (boardSize - edge * 2) * rate;
            enableRect.height = enableRect.width;
            return enableRect;
        }
    }

    /// <summary>
    /// ��ҳ�������
    /// </summary>
    /// <returns>�Ƿ�ɹ�����</returns>
    public bool PlayerTryCreatePiece()
    {
        Rect enable = EnableRect;
        Vector3 mouse = Input.mousePosition;
        int x = (int)((mouse.x - enable.x) / enable.width * lineNum);
        int y = (int)((mouse.y - enable.y) / enable.height * lineNum);
        if (0 <= x && x < lineNum && 0 <= y && y < lineNum && board[x, y] == E_Cross.none)
        {
            CreatePiece(x, y);
            return true;
        }
        return false;
    }

    /// <summary>
    /// ��������
    /// </summary>
    public void CreatePiece(int x, int y)
    {
        // ��¼����������
        if (GameMgr.Instance.CurPlayer == E_Player.player)
            board[x, y] = E_Cross.player;
        else
            board[x, y] = E_Cross.AI;

        // ��������ͼƬ
        GameObject newPiece = Instantiate(GameMgr.Instance.CurPiece == E_PieceType.black ? blackPiece : whitePiece,
            Vector3.zero, Quaternion.identity, transform);
        UISprite sprite = newPiece.GetComponent<UISprite>();

        // ����ê����λ��
        int left = edge + x * crossSize + piecesGap;
        int right = left + crossSize - 2 * piecesGap;
        int bottom = edge + y * crossSize + piecesGap;
        int top = bottom + crossSize - 2 * piecesGap;
        sprite.SetAnchor(transform);
        sprite.leftAnchor.absolute = left;
        sprite.rightAnchor.absolute = right;
        sprite.bottomAnchor.absolute = bottom;
        sprite.topAnchor.absolute = top;

        // ��¼ͼƬ�����鲢����
        newPiece.SetActive(true);
        pieceSprites[x, y] = newPiece;

        // ��¼����һ������
        history.Push(new Vector2Int(x, y));

        // ͬ��AI״̬
        AIGamer.Instance.Synchronize(x, y, true);
    }

    /// <summary>
    /// ������һ������
    /// </summary>
    public void DestroyPiece()
    {
        if (history.Count == 0)
            return;
        Vector2Int pos = history.Pop();

        // ��¼����������
        board[pos.x, pos.y] = E_Cross.none;

        // ��¼ͼƬ�����鲢����
        Destroy(pieceSprites[pos.x, pos.y]);
        pieceSprites[pos.x, pos.y] = null;

        // ͬ��AI״̬
        AIGamer.Instance.Synchronize(pos.x, pos.y, false);
    }
    protected void Awake()
    {
        instance = this;

        // ����������
        Tools.LogNull(gameObject.GetComponent<UITexture>(), "����(Chessboard)û��ͼƬ(UITexture)", true);
        Tools.LogNull(gameObject.GetComponent<UIButton>(), "����(Chessboard)û�а�ť(UIButton)", true);
        Tools.LogNull(gameObject.GetComponent<BoxCollider2D>(), "����(Chessboard)û����ײ��(BoxCollider2D)", true);

        // ������ײ����Χ
        BoxCollider2D collider = gameObject.GetComponent<BoxCollider2D>();
        boardSize = gameObject.GetComponent<UITexture>().mainTexture.width;
        collider.size = new Vector2(boardSize - 2 * edge, boardSize - 2 * edge);

        // ��ʼ������
        board = new E_Cross[lineNum, lineNum];
        pieceSprites = new GameObject[lineNum, lineNum];
        for (int i = 0; i < lineNum; ++i)
            for (int j = 0; j < lineNum; ++j)
            {
                board[i, j] = E_Cross.none;
                pieceSprites[i, j] = null;
            }

        // ��ʼ���ڰ�����Ԥ����
        blackPiece = Resources.Load<GameObject>("Prefabs/Piece_Black");
        whitePiece = Resources.Load<GameObject>("Prefabs/Piece_White");
        Tools.LogNull<UISprite>(blackPiece, "δ�ҵ�����Ԥ����", true);
        Tools.LogNull<UISprite>(whitePiece, "δ�ҵ�����Ԥ����", true);
    }
}
