//#define DEBUG_FEATURE
#define DEBUG_FEATURENUM
//#define DEBUG_EVAL
//#define DEBUG_SEARCH
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// AI�����
/// </summary>
public class AIGamer
{
    private static AIGamer instance = new AIGamer();
    public static AIGamer Instance => instance;
    private AIGamer() => Init();

    /// <summary>
    /// �����Ϸ�Ƿ����
    /// </summary>
    public bool GoalTest
    {
        get;
        protected set;
    }

    // ��¼�ĸ������Ӧ��ֵ
    public const int dir_hor = 0;        // ����
    public const int dir_ver = 1;        // ����
    public const int dir_div = 2;        // ����-����
    public const int dir_back = 3;       // ����-����

    // ����������
    protected const int lineNum = Chessboard.lineNum;
    // �����ڲ����������״̬
    protected E_Cross[,] board;
    // ��һ������λ��
    protected Vector2Int nextPos;

    // �����ڲ��������������
    protected FeatureInfo AIFi;
    protected FeatureInfo playerFi;
    // ��¼��������ǰ����������
    protected FeatureInfo originAIFi;
    protected FeatureInfo originPlayerFi;
    // ��ҵ�����ֵ
    protected int playerEval;
    // AI������ֵ
    protected int AIEval;
    // �������ֵ��AI����Ա���
    protected float playerEvalScale;

    // ����������
    protected int maxDepth;

    // ����Ƿ񴴽�������
    protected bool playing;

    /// <summary>
    /// ��ʼ������
    /// </summary>
    public void Init()
    {
        maxDepth = GameMgr.Instance.maxDepth;
        board = new E_Cross[lineNum, lineNum];
        for (int i = 0; i < lineNum; ++i)
            for (int j = 0; j < lineNum; ++j)
                board[i, j] = E_Cross.none;
        AIFi = new FeatureInfo();
        playerFi = new FeatureInfo();
        playing = false;
        playerEval = 0;
        AIEval = 0;
        playerEvalScale = GameMgr.Instance.startPlayer == E_Player.player ?
            GameMgr.Instance.playerFirstEvalScale : GameMgr.Instance.AIFirstEvalScale;
        GoalTest = false;
    }

    /// <summary>
    /// ʵ������/��������ʱͬ���ڲ�״̬
    /// </summary>
    /// <param name="create">�²����Ƿ�������</param>
    public void Synchronize(int x, int y, bool create)
    {
        // ��Ϸ�����������ʱ����ͬ��
        if (!playing && !create)
            return;
        else
            playing = true;

        // �����ڲ�״̬
        board[x, y] = Chessboard.Instance.Board[x, y];
        UpdateAllFeatures(x, y);

        // �������ۺ����ж��Ƿ������Ϸ
        if (create && Mathf.Abs(Eval()) > (int)E_FeatureType.five / 2)
            GoalTest = true;
    }

    /// <summary>
    /// ������һ��Ҫ���ӵ�����
    /// </summary>
    /// <returns>����õ��ĺϷ�����</returns>
    public Vector2Int Go()
    {
        // ��¼��������
        originAIFi = new FeatureInfo(AIFi);
        originPlayerFi = new FeatureInfo(playerFi);

        // �����̿���ʱ��������������
        maxDepth = Mathf.Min(maxDepth, Chessboard.lineNum * Chessboard.lineNum - Chessboard.Instance.history.Count);

        int maxEval = AlphaBetaSearch();
        // ���������������ǰ������ͬ��
        AIFi = new FeatureInfo(originAIFi);
        playerFi = new FeatureInfo(originPlayerFi);
#if DEBUG_EVAL
        Debug.Log(string.Format("�������ֵ��{0, -10}���λ�ã�{1}", maxEval, nextPos));
#endif
#if DEBUG_FEATURENUM
        int count = 0;
        foreach (Feature f in playerFi.features)
            if (f.type != E_FeatureType.none && f.type != E_FeatureType.dead)
                ++count;
        Debug.Log($"���������{count}��");
        count = 0;
        foreach (Feature f in AIFi.features)
            if (f.type != E_FeatureType.none && f.type != E_FeatureType.dead)
                ++count;
        Debug.Log($"AI������{count}��");
#endif
        return nextPos;
    }

    /* ����Ϊ���ۺ��� */

    /// <summary>
    /// ��ǰ״̬�����ۺ���
    /// </summary>
    protected int Eval()
    {
        //return (int)(Eval(E_Player.AI) - playerEvalScale * Eval(E_Player.player));
        return (int)(AIEval - playerEvalScale * playerEval);
    }
    /// <summary>
    /// ͳ��ĳһ�������ۺ���ֵ
    /// </summary>
    protected int Eval(E_Player side)
    {
        int eval = 0;
        FeatureInfo fi = side == E_Player.AI ? AIFi : playerFi;
        foreach (Feature f in fi.features)
        {
            switch (f.type)
            {
            case E_FeatureType.none:
            case E_FeatureType.dead:
                continue;
            default:
                eval += (int)f.type * (f.blocked ? 1 : Feature.liveScale);
                break;
            }
        }
#if DEBUG_FEATURE
        if (GameMgr.Instance.CurPlayer == E_Player.player)
            return eval;
        string sideStr = side == E_Player.player ? "���" : "AI";
        for (int dir =0;dir < 4; ++dir)
        {
            for (int line = 0;line < FeatureInfo.lineMax; ++line)
            {
                for (int loca = 0; loca < lineNum; ++loca)
                {
                    switch (fi[dir, line, loca].type)
                    {
                    case E_FeatureType.dead:
                    case E_FeatureType.none:
                    case E_FeatureType.single:
                        continue;
                    default:
                        Debug.Log(string.Format("��������{0, -5}����{1,-3}������{2,-3}λ�ã�{3,-3}���ͣ�{4}", sideStr, dir, line, loca, fi[dir, line, loca].type));
                        break;
                    }
                }
            }
        }
#endif
        return eval;
    }
    /// <summary>
    /// ͳ��ĳһ����ĳһ����ĳһ�е����ۺ���ֵ
    /// </summary>
    protected int Eval(E_Player side, int dir, int line)
    {
        int eval = 0;
        FeatureInfo fi = side == E_Player.AI ? AIFi : playerFi;
        for (int loca = 0; loca < lineNum; ++loca)
        {
            switch (fi[dir, line, loca].type)
            {
            case E_FeatureType.none:
            case E_FeatureType.dead:
                continue;
            default:
                eval += (int)fi[dir, line, loca].type * (fi[dir, line, loca].blocked ? 1 : Feature.liveScale);
                break;
            }
        }
        return eval;
    }

    /* ����Ϊ����������غ��� */

    /// <summary>
    /// ���ݷ����仯��λ�ø��µ�ǰ����״̬
    /// </summary>
    protected void UpdateAllFeatures(int x, int y)
    {
        E_Cross[] pieces = new E_Cross[lineNum + 1];
        // ���º���
        for (int i = 0; i < lineNum; ++i)
            pieces[i] = board[i, y];
        UpdateLineFeatures(E_Player.AI, dir_hor, y, pieces);
        UpdateLineFeatures(E_Player.player, dir_hor, y, pieces);
        // ��������
        for (int i = 0; i < lineNum; ++i)
            pieces[i] = board[x, i];
        UpdateLineFeatures(E_Player.AI, dir_ver, x, pieces);
        UpdateLineFeatures(E_Player.player, dir_ver, x, pieces);

        // ĳЩ��������Ӳ������б����Ч����
        int YmX = y - x;
        if (Mathf.Abs(YmX) <= lineNum - 5)
        {
            // ��������-����
            for (int i = Mathf.Max(0, YmX); i < lineNum - Mathf.Max(0, -YmX); ++i)
                pieces[i] = board[i - YmX, i];
            UpdateLineFeatures(E_Player.AI, dir_div, FeatureInfo.lineMax / 2 - YmX, pieces);
            UpdateLineFeatures(E_Player.player, dir_div, FeatureInfo.lineMax / 2 - YmX, pieces);
        }
        int XpYp1 = x + y + 1;
        if (Mathf.Abs(lineNum - XpYp1) <= lineNum - 5)
        {
            // ��������-����
            for (int i = Mathf.Max(0, XpYp1 - lineNum); i < Mathf.Min(lineNum, XpYp1); ++i)
                pieces[i] = board[i, XpYp1 - 1 - i];
            UpdateLineFeatures(E_Player.AI, dir_back, FeatureInfo.lineMax + 4 - XpYp1, pieces);
            UpdateLineFeatures(E_Player.player, dir_back, FeatureInfo.lineMax + 4 - XpYp1, pieces);
        }
    }
    /// <summary>
    /// ����ĳ������һ��ֱ�ߵ�ȫ������
    /// </summary>
    /// <param name="side">�����µ�����������</param>
    /// <param name="dir">ֱ�߷���</param>
    /// <param name="line">������</param>
    /// <param name="pieces">����ֱ���ϵ��������ӷֲ����</param>
    protected void UpdateLineFeatures(E_Player side, int dir, int line, E_Cross[] pieces)
    {
        // ����������
        FeatureInfo fi = side == E_Player.AI ? AIFi : playerFi;
        // ��������
        E_Cross enemy = side == E_Player.AI ? E_Cross.player : E_Cross.AI;
        // �����ܱ���Ӱ������ۺ���
        if (side == E_Player.player)
            playerEval -= Eval(E_Player.player, dir, line);
        else
            AIEval -= Eval(E_Player.AI, dir, line);
        // piecesβ������һ���������ӷ����ж�
        pieces[lineNum] = enemy;

        // ���������Ӱ�E_Cross.enemy��E_Feature.dead����Ϊ��� ��¼��������յ��һ��λ��
        List<Vector2Int> saps = new List<Vector2Int>();
        Vector2Int cur = new Vector2Int(-1, -1);
        bool newSap = true;
        for (int loca = 0; loca < lineNum; ++loca)
        {
            if (fi[dir, line, loca].type == E_FeatureType.dead || pieces[loca] == enemy)
            {
                if (cur.x != -1)
                {
                    saps.Add(cur);
                    cur.x = -1;
                }
                newSap = true;
            }
            else
            {
                if (newSap)
                {
                    cur = new Vector2Int(loca, loca + 1);
                    newSap = false;
                }
                else
                    ++cur.y;
            }
        }
        if (cur.x != -1)
        {
            saps.Add(cur);
            cur.x = -1;
        }

        // ������С����ķֶ���Ϊnone ���Ƴ��ֶ�
        for (int i = saps.Count - 1; i >= 0; --i)
        {
            if (saps[i].y - saps[i].x < 5)
            {
                for (int loca = saps[i].x; loca < saps[i].y; ++loca)
                    fi[dir, line, loca].type = E_FeatureType.none;
                saps.RemoveAt(i);
            }
        }

        // �ֱ����ÿ���ֶ�
        foreach (Vector2Int sap in saps)
        {
            // �÷ֶ��ܿռ�
            int space = sap.y - sap.x;
            // ��¼��һ���������Ƿ�Χ�ĺ�һ��λ�� ������������
            int lastEnd = -100;
            for (int loca = sap.x; loca < sap.y; ++loca)
            {
                // û�����ӵ�λ�ò�������Ϊ������ʼ��
                if (pieces[loca] == E_Cross.none)
                {
                    fi[dir, line, loca].type = E_FeatureType.none;
                    continue;
                }

                // �÷ֶι�Сʱ��Ϊ�����
                fi[dir, line, loca].blocked = space == 5;

                // �÷ֶ�ʣ��ռ� ��������ʣ��ռ�����
                int lastSpace = sap.y - loca;
                // ��¼��ǰ����ʹ�õĿռ�
                int used = 0;

                // ��Ҫ1�ռ�
                if (loca + 1 > lastEnd)
                {
                    fi[dir, line, loca].type = E_FeatureType.single;
                    fi[dir, line, loca].blocked |= loca == 0 || pieces[loca - 1] == enemy || pieces[loca + 1] == enemy;
                    used = 1;
                }

                // ��Ҫ2�ռ�
                if (lastSpace >= 2 && pieces[loca + 1] != E_Cross.none && loca + 2 > lastEnd)
                {
                    // ����
                    fi[dir, line, loca].type = E_FeatureType.near2;
                    fi[dir, line, loca].blocked |= pieces[loca + 2] == enemy;
                    used = 2;
                }

                // ��Ҫ3�ռ�
                if (lastSpace >= 3 && pieces[loca + 2] != E_Cross.none && loca + 3 > lastEnd)
                {
                    // С����
                    if (pieces[loca + 1] == E_Cross.none)
                        fi[dir, line, loca].type = E_FeatureType.jump2;
                    // ����
                    else
                        fi[dir, line, loca].type = E_FeatureType.near3;
                    fi[dir, line, loca].blocked |= pieces[loca + 3] == enemy;
                    used = 3;
                }

                // ��Ҫ4�ռ�
                if (lastSpace >= 4 && pieces[loca + 3] != E_Cross.none && loca + 4 > lastEnd)
                {
                    // ������
                    if (pieces[loca + 1] == E_Cross.none && pieces[loca + 2] == E_Cross.none)
                        fi[dir, line, loca].type = E_FeatureType.far2;
                    // ����
                    else if (pieces[loca + 1] != pieces[loca + 2])
                        fi[dir, line, loca].type = E_FeatureType.jump3;
                    // ����
                    else
                        fi[dir, line, loca].type = E_FeatureType.near4;
                    fi[dir, line, loca].blocked |= pieces[loca + 4] == enemy;
                    used = 4;
                }

                // ��Ҫ5�ռ�
                if (lastSpace >= 5 && pieces[loca + 4] != E_Cross.none && loca + 5 > lastEnd)
                {
                    // ͳ���м�����λ�õĿ�λ��
                    int blankCount = 0;
                    for (int i = 1; i <= 3; ++i)
                        blankCount += pieces[loca + i] == E_Cross.none ? 1 : 0;

                    // ����
                    if (blankCount == 1)
                    {
                        fi[dir, line, loca].type = E_FeatureType.jump4;
                        used = 5;
                    }
                    // ����
                    if (blankCount == 0)
                    {
                        fi[dir, line, loca].type = E_FeatureType.five;
                        used = 5;
                    }
                    fi[dir, line, loca].blocked |= pieces[loca + 5] == enemy;
                }

                // ���ڴ�λ���Ѿ�������������ʼ�� �ʺ�����������ͬ���Ӷ����迼��
                while (++loca < sap.y && pieces[loca] != E_Cross.none)
                    fi[dir, line, loca].type = E_FeatureType.none;
                // ��¼��ǰ������һ��λ��
                lastEnd = loca + used;
            }// end of each sap
        }// end of foreach statement

        // �������ۺ���
        if (side == E_Player.player)
            playerEval += Eval(E_Player.player, dir, line);
        else
            AIEval += Eval(E_Player.AI, dir, line);
    }

    /// <summary>
    /// ��������λ�û�ȡ�丽������
    /// </summary>
    /// ��������ά�ȷֱ�Ϊ ������ ���� �����ڸ��е�
    protected Feature[,,] GetNearbyFeatures(int x, int y)
    {
        Feature[,,] tempFs = new Feature[2, 4, lineNum];
        for (int i = 0; i < 4; ++i)
        {
            if (i == dir_div && (FeatureInfo.lineMax / 2 < y - x || FeatureInfo.lineMax / 2 < x - y)
                || i == dir_back && (x + y < 4 || FeatureInfo.lineMax + 3 < x + y))
                continue;
            Vector3Int fpos = BoardToFeature(x, y, i);
            for (int j = 0; j < lineNum; ++j)
                tempFs[0, i, j] = new Feature(AIFi[i, fpos.y, j]);
            for (int j = 0; j < lineNum; ++j)
                tempFs[1, i, j] = new Feature(playerFi[i, fpos.y, j]);
    }
        return tempFs;
    }
    /// <summary>
    /// ���ݲ����洢ĳ����λ�ø�������
    /// </summary>
    protected void SetNearbyFeatures(Feature[,,] tempFs, int x, int y)
    {
        for (int i = 0; i < 4; ++i)
        {
            if (i == dir_div && (FeatureInfo.lineMax / 2 < y - x || FeatureInfo.lineMax / 2 < x - y)
                || i == dir_back && (x + y < 4 || FeatureInfo.lineMax + 3 < x + y))
                continue;
            Vector3Int fpos = BoardToFeature(x, y, i);
            for (int j = 0; j < lineNum; ++j)
            {
                AIFi[i, fpos.y, j].type = tempFs[0, i, j].type;
                AIFi[i, fpos.y, j].blocked = tempFs[0, i, j].blocked;
            }
            for (int j = 0; j < lineNum; ++j)
            {
                playerFi[i, fpos.y, j].type = tempFs[1, i, j].type;
                playerFi[i, fpos.y, j].blocked = tempFs[1, i, j].blocked;
            }
        }
    }

    /* ����Ϊ��ȡ���м�ֵλ����غ��� */

    /// <summary>
    /// ����ǿ������ȡ�����µ�λ��
    /// </summary>
    protected List<Vector2Int> GetUrgentPositions(E_FeatureType type, int dir, int line, int loca)
    {
        // �������λ��
        List<Vector2Int> positions = new List<Vector2Int>();
        switch (type)
        {
        case E_FeatureType.near4:
            positions.Add(FeatureToBoard(dir, line, loca - 1));
            positions.Add(FeatureToBoard(dir, line, loca + 4));
            break;
        case E_FeatureType.jump4:
            positions.Add(FeatureToBoard(dir, line, loca + 1));
            positions.Add(FeatureToBoard(dir, line, loca + 2));
            positions.Add(FeatureToBoard(dir, line, loca + 3));
            break;
        case E_FeatureType.near3:
            positions.Add(FeatureToBoard(dir, line, loca - 1));
            positions.Add(FeatureToBoard(dir, line, loca + 3));
            break;
        case E_FeatureType.jump3:
            positions.Add(FeatureToBoard(dir, line, loca - 1));
            positions.Add(FeatureToBoard(dir, line, loca + 1));
            positions.Add(FeatureToBoard(dir, line, loca + 2));
            positions.Add(FeatureToBoard(dir, line, loca + 4));
            break;
        }
        // ��ȥ�������ӻ�߽���λ��
        for (int i = positions.Count - 1; i >= 0; --i)
        {
            if (positions[i].x < 0 || positions[i].y < 0 || 
                positions[i].x >= lineNum || positions[i].y >= lineNum || 
                board[positions[i].x, positions[i].y] != E_Cross.none)
                positions.RemoveAt(i);
        }
        return positions;
    }
    /// <summary>
    /// ������������ȡ�丽���м�ֵ��λ��
    /// </summary>
    protected List<Vector2Int> GetValuablePositions(E_FeatureType type, int dir, int line, int loca)
    {
        // �������λ��
        List<Vector2Int> positions = new List<Vector2Int>();
        switch (type)
        {
        case E_FeatureType.single:
            // ���������ض����ĸ������е� ��ֻ�迼��һ������
            positions.Add(FeatureToBoard(dir, line, loca - 1));
            break;
        case E_FeatureType.near2:
            positions.Add(FeatureToBoard(dir, line, loca - 1));
            positions.Add(FeatureToBoard(dir, line, loca + 2));
            break;
        case E_FeatureType.jump2:
            positions.Add(FeatureToBoard(dir, line, loca - 1));
            positions.Add(FeatureToBoard(dir, line, loca + 1));
            positions.Add(FeatureToBoard(dir, line, loca + 3));
            break;
        case E_FeatureType.far2:
            positions.Add(FeatureToBoard(dir, line, loca - 1));
            positions.Add(FeatureToBoard(dir, line, loca + 1));
            positions.Add(FeatureToBoard(dir, line, loca + 2));
            positions.Add(FeatureToBoard(dir, line, loca + 4));
            break;
        case E_FeatureType.near3:
            positions.Add(FeatureToBoard(dir, line, loca - 1));
            positions.Add(FeatureToBoard(dir, line, loca + 3));
            break;
        case E_FeatureType.jump3:
            positions.Add(FeatureToBoard(dir, line, loca - 1));
            positions.Add(FeatureToBoard(dir, line, loca + 1));
            positions.Add(FeatureToBoard(dir, line, loca + 2));
            positions.Add(FeatureToBoard(dir, line, loca + 4));
            break;
        }
        // ��ȥ�������ӻ�߽���λ��
        for (int i = positions.Count - 1; i >= 0; --i)
        {
            if (positions[i].x < 0 || positions[i].y < 0 ||
                positions[i].x >= lineNum || positions[i].y >= lineNum ||
                board[positions[i].x, positions[i].y] != E_Cross.none)
                positions.RemoveAt(i);
        }
        return positions;
    }
    /// <summary>
    /// ��ȡ���м�ֵ������λ��
    /// </summary>
    /// <param name="side">��ǰ׼��������</param>
    protected List<Vector2Int> GetBestPositions(E_Player side)
    {
        // �趨����
        FeatureInfo selfFi = side == E_Player.player ? playerFi : AIFi;
        FeatureInfo enemyFi = side == E_Player.player ? AIFi : playerFi;
        // ��¼�����м�ֵ������
        List<Vector3Int> selfFeatures = new List<Vector3Int>();
        List<Vector3Int> enemyFeatures = new List<Vector3Int>();
        // ��¼�����м�ֵ��λ��
        List<Vector2Int> positions = new List<Vector2Int>();

        // ���Ҽ����ܳ����λ�� ���������м�ֵ�������뿼��
        for (int dir = 0; dir < 4; ++dir)
        {
            for (int line = 0; line < FeatureInfo.lineMax; ++line)
            {
                for (int loca = 0; loca < lineNum; ++loca)
                {
                    switch (selfFi[dir, line, loca].type)
                    {
                    // �����ܳ���ֱ�ӷ���
                    case E_FeatureType.near4:
                    case E_FeatureType.jump4:
                        return GetUrgentPositions(selfFi[dir, line, loca].type, dir, line, loca);
                    // �����л�����ֵ�ܴ� ���������뿼��
                    case E_FeatureType.near3:
                    case E_FeatureType.jump3:
                        if (!selfFi[dir, line, loca].blocked)
                            positions = GetUrgentPositions(selfFi[dir, line, loca].type, dir, line, loca);
                        else
                            selfFeatures.Add(new Vector3Int(dir, line, loca));
                        break;
                    // �����������뿼��
                    case E_FeatureType.near2:
                    case E_FeatureType.jump2:
                    case E_FeatureType.far2:
                    case E_FeatureType.single:
                        selfFeatures.Add(new Vector3Int(dir, line, loca));
                        break;
                    // û���������迼��
                    default:
                        break;
                    }
                }
            }
        }

        // ���ұ����µ�λ�� �����з��м�ֵ�������뿼��
        for (int dir = 0; dir < 4; ++dir)
        {
            for (int line = 0; line < FeatureInfo.lineMax; ++line)
            {
                for (int loca = 0; loca < lineNum; ++loca)
                {
                    switch (enemyFi[dir, line, loca].type)
                    {
                    // �з����ı�����
                    case E_FeatureType.near4:
                    case E_FeatureType.jump4:
                        return GetUrgentPositions(enemyFi[dir, line, loca].type, dir, line, loca);
                    // �з��л��������� ���������뿼��
                    case E_FeatureType.near3:
                    case E_FeatureType.jump3:
                        if (!enemyFi[dir, line, loca].blocked)
                        {
                            foreach (Vector2Int pos in GetUrgentPositions(enemyFi[dir, line, loca].type, dir, line, loca))
                                positions.Add(pos);
                            return positions;
                        }
                        else
                            enemyFeatures.Add(new Vector3Int(dir, line, loca));
                        break;
                    // �з��ж����뿼��
                    case E_FeatureType.near2:
                    case E_FeatureType.jump2:
                    case E_FeatureType.far2:
                        enemyFeatures.Add(new Vector3Int(dir, line, loca));
                        break;
                    // �����������迼��
                    default:
                        break;
                    }
                }
            }
        }

        // ���ؿ��ܴ��ڵļ������ĵĻ���
        if (positions.Count > 0)
            return positions;

        // ������뿼�ǵ����������м�ֵ��λ��
        if (selfFeatures.Count > 0 || enemyFeatures.Count > 0)
        {
            // ��ʼ��һ����¼ĳλ���Ƿ��Ѿ����뿼�ǵ�����
            bool[,] added = new bool[lineNum, lineNum];
            // �������λ��
            foreach (Vector3Int f in enemyFeatures)
            {
                foreach (Vector2Int pos in 
                    GetValuablePositions(enemyFi[f.x, f.y, f.z].type, f.x,  f.y, f.z))
                    if (!added[pos.x, pos.y])
                    {
                        positions.Add(pos);
                        added[pos.x, pos.y] = true;
                    }
            }
            // �������λ��
            foreach (Vector3Int f in selfFeatures)
            {
                foreach (Vector2Int pos in
                    GetValuablePositions(selfFi[f.x, f.y, f.z].type, f.x, f.y, f.z))
                    if (!added[pos.x, pos.y])
                    {
                        positions.Add(pos);
                        added[pos.x, pos.y] = true;
                    }
            }
            return positions;
        }

        // û�к�������ʱ �������п�λ
        for (int i = 0; i < lineNum; ++i)
            for (int j = 0; j < lineNum; ++j)
                if (board[i, j] == E_Cross.none)
                    positions.Add(new Vector2Int(i, j));
        return positions;
    }

    /* ����Ϊ��-�¼�֦������غ��� */
    protected int AlphaBetaSearch()
    {
        return MaxValue(int.MinValue, int.MaxValue, 0);
    }
    protected int MaxValue(int alpha, int beta, int depth)
    {
        if (depth >= maxDepth)
            return Eval();
        int eval = int.MinValue;
        int curEval;

        List<Vector2Int> positions = GetBestPositions(E_Player.AI);
        foreach (Vector2Int pos in positions)
        {
            // �洢����ǰ����
            //List<Feature> fList = GetNearbyFeatures(pos.x, pos.y);
            // ��������
            board[pos.x, pos.y] = E_Cross.AI;
            UpdateAllFeatures(pos.x, pos.y);
            curEval = MinValue(alpha, beta, depth + 1);
            if (curEval > eval)
            {
                eval = curEval;
                if (depth == 0)
                {
                    nextPos.x = pos.x;
                    nextPos.y = pos.y;
                }
#if DEBUG_SEARCH
                Debug.Log(string.Format("��ǰ������ۺ�����{0, -10}��ǰλ��{1}", eval, nextPos));
#endif
            }
            // �ָ�����
            board[pos.x, pos.y] = E_Cross.none;
            UpdateAllFeatures(pos.x, pos.y);
            //SetNearbyFeatures(fList, pos.x, pos.y);
            if (eval >= beta)
                return eval;
            alpha = Mathf.Max(alpha, eval);
        }
        return eval;
    }
    protected int MinValue(int alpha, int beta, int depth)
    {
        if (depth >= maxDepth)
            return Eval();
        int eval = int.MaxValue;
        List<Vector2Int> positions = GetBestPositions(E_Player.player);
        foreach (Vector2Int pos in positions)
        {
            // �洢����ǰ����
            Feature[,,] tempFs = GetNearbyFeatures(pos.x, pos.y);
            // ��������
            board[pos.x, pos.y] = E_Cross.player;
            UpdateAllFeatures(pos.x, pos.y);
            eval = Mathf.Min(MaxValue(alpha, beta, depth + 1), eval);
            // �ָ�����
            board[pos.x, pos.y] = E_Cross.none;
            UpdateAllFeatures(pos.x, pos.y);
            //SetNearbyFeatures(tempFs, pos.x, pos.y);
            if (eval <= alpha)
                return eval;
            beta = Mathf.Min(beta, eval);
        }
        return eval;
    }

    /* ����Ϊ�ڲ����ߺ��� */
    protected Vector2Int FeatureToBoard(int dir, int line, int loca)
    {
        switch (dir)
        {
        case dir_hor:
            return new Vector2Int(loca, line);
        case dir_ver:
            return new Vector2Int(line, loca);
        case dir_div:
            return new Vector2Int(line + loca - lineNum + 5, loca);
        case dir_back:
            return new Vector2Int(loca, lineNum + 9 - line - loca);
        default:
            return new Vector2Int(-1, -1);
        }
    }
    protected Vector3Int BoardToFeature(int x, int y, int dir)
    {
        switch (dir)
        {
        case dir_hor:
            return new Vector3Int(dir, y, x);
        case dir_ver:
            return new Vector3Int(dir, x, y);
        case dir_div:
            return new Vector3Int(dir, FeatureInfo.lineMax / 2 - y + x, y);
        case dir_back:
            return new Vector3Int(dir, FeatureInfo.lineMax + 3 - x - y, x);
        default:
            return new Vector3Int(-1, -1, -1);
        }
    }
}
