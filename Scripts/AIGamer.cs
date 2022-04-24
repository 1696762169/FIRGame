//#define DEBUG_FEATURE
//#define DEBUG_FEATURENUM
//#define DEBUG_NODENUM
//#define DEBUG_MAXDEPTH
#define DEBUG_EVERYDEPTH_EVAL
#define DEBUG_EVAL
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
    protected Vector2Int curNextPos;

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

    // (��ǰ)����������
    protected int maxDepth;
    protected int curDepth;
    protected int curMaxDepth;
    // ���/��ǰ���������
    protected int maxNodeNum;
    protected int nodeNum;

    // ����Ƿ񴴽�������
    protected bool playing;

    /// <summary>
    /// ��ʼ������
    /// </summary>
    public void Init()
    {
        maxDepth = GameMgr.Instance.maxDepth;
        maxNodeNum = GameMgr.Instance.maxNodeNum;
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
#if DEBUG_FEATURENUM
        if (GameMgr.Instance.CurPlayer == E_Player.player)
            return;
        int count = 0;
        foreach (Feature f in playerFi.Features)
            if (f.type != E_FeatureType.none)
                ++count;
        Debug.Log($"���������{count}��");
        count = 0;
        foreach (Feature f in AIFi.Features)
            if (f.type != E_FeatureType.none)
                ++count;
        Debug.Log($"AI������{count}��");
#endif
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

        AlphaBetaSearch();
        // ���������������ǰ������ͬ��
        AIFi = new FeatureInfo(originAIFi);
        playerFi = new FeatureInfo(originPlayerFi);

#if DEBUG_NODENUM
        Debug.Log($"�����������{nodeNum}");
#endif
#if DEBUG_MAXDEPTH
        Debug.Log($"���������ȣ�{curMaxDepth}");
#endif

        return nextPos;
    }

    /* ����Ϊ���ۺ��� */

    /// <summary>
    /// ��ǰ״̬�����ۺ���
    /// </summary>
    protected int Eval()
    {
        return (int)(Eval(E_Player.AI) - playerEvalScale * Eval(E_Player.player));
        //return (int)(AIEval - playerEvalScale * playerEval);
    }
    /// <summary>
    /// ͳ��ĳһ�������ۺ���ֵ
    /// </summary>
    protected int Eval(E_Player side)
    {
        int eval = 0;
        FeatureInfo fi = side == E_Player.AI ? AIFi : playerFi;
        foreach (Feature f in fi.Features)
            eval += (int)f.type * (f.blocked ? 1 : Feature.liveScale);
#if DEBUG_FEATURE
        if (GameMgr.Instance.CurPlayer == E_Player.player)
            return eval;
        string sideStr = side == E_Player.player ? "���" : "AI";
        foreach (Feature f in fi.Features)
        {
            if (f.type != E_FeatureType.single)
                Debug.Log(string.Format("��������{0, -5}����{1,-3}������{2,-3}λ�ã�{3,-3}���ͣ�{4}", sideStr, f.dir, f.line, f.loca, f.type));
        }
#endif
        return eval;
    }

    /* ����Ϊ����������غ��� */

    /// <summary>
    /// ���ݷ����仯��λ�ø��µ�ǰ����״̬
    /// </summary>
    protected void UpdateAllFeatures(int x, int y)
    {
        // ɾ�����й�������
        for (int i = playerFi.Features.Count - 1; i >= 0; --i)
            if (IsAttached_BF(x, y, playerFi[i].dir, playerFi[i].line))
                playerFi.Features.RemoveAt(i);
        for (int i = AIFi.Features.Count - 1; i >= 0; --i)
            if (IsAttached_BF(x, y, AIFi[i].dir, AIFi[i].line))
                AIFi.Features.RemoveAt(i);

        E_Cross[] pieces = new E_Cross[lineNum + 1];
        // ���º���
        for (int i = 0; i < lineNum; ++i)
            pieces[i] = board[i, y];
        AddLineFeatures(E_Player.AI, dir_hor, y, pieces);
        AddLineFeatures(E_Player.player, dir_hor, y, pieces);
        // ��������
        for (int i = 0; i < lineNum; ++i)
            pieces[i] = board[x, i];
        AddLineFeatures(E_Player.AI, dir_ver, x, pieces);
        AddLineFeatures(E_Player.player, dir_ver, x, pieces);

        // ĳЩ��������Ӳ������б����Ч����
        int YmX = y - x;
        if (Mathf.Abs(YmX) <= lineNum - 5)
        {
            // ��������-����
            for (int i = Mathf.Max(0, YmX); i < lineNum - Mathf.Max(0, -YmX); ++i)
                pieces[i] = board[i - YmX, i];
            AddLineFeatures(E_Player.AI, dir_div, FeatureInfo.lineMax / 2 - YmX, pieces, Mathf.Max(0, YmX), lineNum - Mathf.Max(0, -YmX));
            AddLineFeatures(E_Player.player, dir_div, FeatureInfo.lineMax / 2 - YmX, pieces, Mathf.Max(0, YmX), lineNum - Mathf.Max(0, -YmX));
        }
        int XpYp1 = x + y + 1;
        if (Mathf.Abs(lineNum - XpYp1) <= lineNum - 5)
        {
            // ��������-����
            for (int i = Mathf.Max(0, XpYp1 - lineNum); i < Mathf.Min(lineNum, XpYp1); ++i)
                pieces[i] = board[i, XpYp1 - 1 - i];
            AddLineFeatures(E_Player.AI, dir_back, FeatureInfo.lineMax + 4 - XpYp1, pieces, Mathf.Max(0, XpYp1 - lineNum), Mathf.Min(lineNum, XpYp1));
            AddLineFeatures(E_Player.player, dir_back, FeatureInfo.lineMax + 4 - XpYp1, pieces, Mathf.Max(0, XpYp1 - lineNum), Mathf.Min(lineNum, XpYp1));
        }
    }
    /// <summary>
    /// ����ĳ������һ��ֱ�ߵ�ȫ������
    /// </summary>
    /// <param name="side">�����µ�����������</param>
    /// <param name="dir">ֱ�߷���</param>
    /// <param name="line">������</param>
    /// <param name="pieces">����ֱ���ϵ��������ӷֲ����</param>
    protected void AddLineFeatures(E_Player side, int dir, int line, E_Cross[] pieces, int start = 0, int end = lineNum)
    {
        // ����������
        FeatureInfo fi = side == E_Player.AI ? AIFi : playerFi;
        // ��������
        E_Cross enemy = side == E_Player.AI ? E_Cross.player : E_Cross.AI;
        // piecesβ������һ���������ӷ����ж�
        pieces[end] = enemy;

        // ���������Ӱ�E_Cross.enemy��E_Feature.dead����Ϊ��� ��¼��������յ��һ��λ��
        List<Vector2Int> saps = new List<Vector2Int>();
        Vector2Int cur = new Vector2Int(-1, -1);
        bool newSap = true;
        for (int loca = start; loca < end; ++loca)
        {
            if (pieces[loca] == enemy)
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

        // ������С����ķֶ��Ƴ�
        for (int i = saps.Count - 1; i >= 0; --i)
            if (saps[i].y - saps[i].x < 5)
                saps.RemoveAt(i);

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
                    continue;

                Feature f = new Feature(E_FeatureType.none, false, dir, line, loca);
                // �÷ֶι�Сʱ��Ϊ�����
                f.blocked = space == 5;

                // �÷ֶ�ʣ��ռ� ��������ʣ��ռ�����
                int lastSpace = sap.y - loca;
                // ��¼��ǰ����ʹ�õĿռ�
                int used = 0;

                // ��Ҫ1�ռ�
                if (loca + 1 > lastEnd)
                {
                    f.type = E_FeatureType.single;
                    f.blocked |= loca == 0 || pieces[loca - 1] == enemy || pieces[loca + 1] == enemy;
                    used = 1;
                }

                // ��Ҫ2�ռ�
                if (lastSpace >= 2 && pieces[loca + 1] != E_Cross.none && loca + 2 > lastEnd)
                {
                    // ����
                    f.type = E_FeatureType.near2;
                    f.blocked |= pieces[loca + 2] == enemy;
                    used = 2;
                }

                // ��Ҫ3�ռ�
                if (lastSpace >= 3 && pieces[loca + 2] != E_Cross.none && loca + 3 > lastEnd)
                {
                    // С����
                    if (pieces[loca + 1] == E_Cross.none)
                        f.type = E_FeatureType.jump2;
                    // ����
                    else
                        f.type = E_FeatureType.near3;
                    f.blocked |= pieces[loca + 3] == enemy;
                    used = 3;
                }

                // ��Ҫ4�ռ�
                if (lastSpace >= 4 && pieces[loca + 3] != E_Cross.none && loca + 4 > lastEnd)
                {
                    // ������
                    if (pieces[loca + 1] == E_Cross.none && pieces[loca + 2] == E_Cross.none)
                        f.type = E_FeatureType.far2;
                    // ����
                    else if (pieces[loca + 1] != pieces[loca + 2])
                        f.type = E_FeatureType.jump3;
                    // ����
                    else
                        f.type = E_FeatureType.near4;
                    f.blocked |= pieces[loca + 4] == enemy;
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
                        f.type = E_FeatureType.jump4;
                        used = 5;
                    }
                    // ����
                    if (blankCount == 0)
                    {
                        f.type = E_FeatureType.five;
                        used = 5;
                    }
                    f.blocked |= pieces[loca + 5] == enemy;
                }

                // ��������
                fi.Features.Add(f);
                // ���ڴ�λ���Ѿ�������������ʼ�� �ʺ�����������ͬ���Ӷ����迼��
                while (++loca < sap.y && pieces[loca] != E_Cross.none)
                    ;
                // ��¼��ǰ������һ��λ��
                lastEnd = loca + used;
            }// end of each sap
        }// end of foreach statement
    }

    /* ����Ϊ��ȡ���м�ֵλ����غ��� */

    /// <summary>
    /// ����ǿ������ȡ������/ֱ��ȡʤ��λ��
    /// </summary>
    protected List<Vector2Int> GetUrgentPositions(Feature f, bool defence = true)
    {
        // �������λ��
        List<Vector2Int> positions = new List<Vector2Int>();
        switch (f.type)
        {
        case E_FeatureType.near4:
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca - 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 4));
            break;
        case E_FeatureType.jump4:
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 2));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 3));
            break;
        case E_FeatureType.near3:
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca - 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 3));
            break;
        case E_FeatureType.jump3:
            if (defence)
            {
                positions.Add(FeatureToBoard(f.dir, f.line, f.loca - 1));
                positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 4));
            }
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 2));
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
    /// �����໥��ϱ�����/ֱ��ȡʤ���������������λ��
    /// </summary>
    protected Vector2Int GetValuablePositions(ref List<Vector2Int> positions, List<Feature> goodFs, ref bool double3)
    {
        int[,] added = new int[lineNum, lineNum];
        bool[,] is3 = new bool[lineNum, lineNum];
        Vector2Int bestFeatures = new Vector2Int(0, 0);
        if (goodFs.Count >= 2)
        {
            for (int i = 0; i < goodFs.Count; ++i)
            {
                foreach (Vector2Int pos in GetNearPositions(goodFs[i]))
                    if (added[pos.x, pos.y] != 0)
                    {
                        positions.Add(pos);
                        bestFeatures.x = added[pos.x, pos.y] - 1;
                        bestFeatures.y = i;
                        double3 = !(is3[pos.x, pos.y] || goodFs[i].type == E_FeatureType.near3 || goodFs[i].type == E_FeatureType.jump3);
                    }
                    else
                    {
                        added[pos.x, pos.y] = i + 1;
                        if (goodFs[i].type == E_FeatureType.near3 || goodFs[i].type == E_FeatureType.jump3)
                            is3[pos.x, pos.y] = true;
                    }
            }
        }
        return bestFeatures;
    }
    /// <summary>
    /// ������������ȡ�丽����ص�λ��
    /// </summary>
    protected List<Vector2Int> GetNearPositions(Feature f)
    {
        // �������λ��
        List<Vector2Int> positions = new List<Vector2Int>();
        switch (f.type)
        {
        case E_FeatureType.single:
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca - 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 1));
            break;
        case E_FeatureType.near2:
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca - 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 2));
            break;
        case E_FeatureType.jump2:
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca - 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 3));
            break;
        case E_FeatureType.far2:
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca - 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 2));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 4));
            break;
        case E_FeatureType.near3:
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca - 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 3));
            break;
        case E_FeatureType.jump3:
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca - 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 2));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 4));
            break;
        case E_FeatureType.near4:
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca - 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 4));
            break;
        case E_FeatureType.jump4:
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 2));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 3));
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
    /// ����������������ȡ�丽���������λ��
    /// </summary>
    /// <param name="positions"></param>
    /// <param name="NormalFs"></param>
    protected void GetAllNearPositions(ref List<Vector2Int> positions, List<Feature> NormalFs)
    {
        // ��ʼ��һ����¼ĳλ���Ƿ��Ѿ����뿼�ǵ�����
        bool[,] added = new bool[lineNum, lineNum];
        foreach (Feature f in NormalFs)
        {
            foreach (Vector2Int pos in
                GetNearPositions(f))
                if (!added[pos.x, pos.y])
                {
                    positions.Add(pos);
                    added[pos.x, pos.y] = true;
                }
        }
    }
    /// <summary>
    /// �����������뿼�� ֱ�����������ﵽ����
    /// </summary>
    protected void AddSingleFeatures(ref List<Feature> fList, FeatureInfo fi)
    {
        const int maxFeatureNum = 20;
        foreach (Feature f in fi.Features)
        {
            if (fList.Count > maxFeatureNum)
                break;
            if (f.type == E_FeatureType.single)
                fList.Add(f);
        }
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
        List<Feature> selfGoodFeatures = new List<Feature>();
        List<Feature> selfNormalFeatures = new List<Feature>();
        List<Feature> enemyGoodFeatures = new List<Feature>();
        List<Feature> enemyNormalFeatures = new List<Feature>();
        // ��¼�����м�ֵ��λ��
        List<Vector2Int> atkPositions = new List<Vector2Int>();
        List<Vector2Int> defPositions = new List<Vector2Int>();

        // ���Ҽ����ܳ����λ�� ���������м�ֵ�������뿼��
        foreach (Feature sf in selfFi.Features)
        {
            switch (sf.type)
            {
            // �����ܳ���ֱ�ӷ���
            case E_FeatureType.near4:
            case E_FeatureType.jump4:
                return GetUrgentPositions(sf);
            // �����л�����ֵ�ܴ� ��������ֵ�ϴ�
            case E_FeatureType.near3:
            case E_FeatureType.jump3:
                if (sf.blocked)
                    selfGoodFeatures.Add(new Feature(sf));
                else
                    atkPositions = GetUrgentPositions(sf, false);
                    break;
            // �����ֵ�ϴ� �������뿼��
            case E_FeatureType.near2:
            case E_FeatureType.jump2:
            case E_FeatureType.far2:
                if (sf.blocked)
                    selfNormalFeatures.Add(new Feature(sf));
                else
                    selfGoodFeatures.Add(new Feature(sf));
                break;
            // �����������迼��
            default:
                break;
            }
        }

        // ���ұ����µ�λ�� �����з��м�ֵ�������뿼��
        foreach (Feature ef in enemyFi.Features)
        {
            switch (ef.type)
            {
            // �з����ı�����
            case E_FeatureType.near4:
            case E_FeatureType.jump4:
                return GetUrgentPositions(ef);
            // �з��л��������� ��������Ҫ����
            case E_FeatureType.near3:
            case E_FeatureType.jump3:
                if (ef.blocked)
                    enemyGoodFeatures.Add(new Feature(ef));
                else
                    defPositions = GetUrgentPositions(ef);
                break;
            // �з��л����Ҫ���� ���������뿼��
            case E_FeatureType.near2:
            case E_FeatureType.jump2:
            case E_FeatureType.far2:
                if (ef.blocked)
                    enemyNormalFeatures.Add(new Feature(ef));
                else
                    enemyGoodFeatures.Add(new Feature(ef));
                break;
            // �����������迼��
            default:
                break;
            }
        }

        // ���ؿ��ܴ��ڵļ����ɻ��ĵĻ���
        if (atkPositions.Count > 0)
            return atkPositions;
        // ���ط�µз�������λ��
        if (defPositions.Count > 0)
            return defPositions;

        // ����ǿ������λ��(��˫��)
        bool selfDouble3 = false;
        GetValuablePositions(ref atkPositions, selfGoodFeatures, ref selfDouble3);
        if (atkPositions.Count > 0 && !selfDouble3)
            return atkPositions;

        // �����Ƿ���ڼ������λ��
        bool enemyDouble3 = false;
        Vector2Int defPos = GetValuablePositions(ref defPositions, enemyGoodFeatures, ref enemyDouble3);
        if (defPositions.Count > 0)
        {
            // ���¼������пɷ���λ��
            defPositions.Clear();
            bool[,] added = new bool[lineNum, lineNum];
            foreach (Vector2Int pos in GetNearPositions(enemyGoodFeatures[defPos.x]))
                if (!added[pos.x, pos.y])
                {
                    defPositions.Add(pos);
                    added[pos.x, pos.y] = true;
                }
            foreach (Vector2Int pos in GetNearPositions(enemyGoodFeatures[defPos.y]))
                if (!added[pos.x, pos.y])
                {
                    defPositions.Add(pos);
                    added[pos.x, pos.y] = true;
                }
            return defPositions;
        }

        // �����Լ���˫��
        if (atkPositions.Count > 0)
            return atkPositions;

        // ��û����ϵĽ�ǿ������Ϊ��ͨ����
        //foreach (Feature sf in selfGoodFeatures)
        //    selfNormalFeatures.Add(sf);
        //foreach (Feature ef in enemyGoodFeatures)
        //    enemyNormalFeatures.Add(ef);

        // û�ж������ϵ�����ʱ�������е�����
        bool allSingle = selfNormalFeatures.Count == 0 && enemyNormalFeatures.Count == 0;

        // �����е������浽selfNormalFeatures��
        AddSingleFeatures(ref selfNormalFeatures, selfFi);
        AddSingleFeatures(ref enemyNormalFeatures, enemyFi);

        // ������뿼�ǵ����������м�ֵ��λ��
        if (selfNormalFeatures.Count > 0 || enemyNormalFeatures.Count > 0)
        {
            // ֻ�е�����ʱ��������λ��
            if (allSingle)
            {
                foreach (Feature ef in enemyNormalFeatures)
                    selfNormalFeatures.Add(ef);
                GetAllNearPositions(ref atkPositions, selfNormalFeatures);
                return atkPositions;
            }
            // �������λ��
            if (enemyNormalFeatures.Count > 0)
            {
                GetAllNearPositions(ref defPositions, enemyNormalFeatures);
                return defPositions;
            }
            // �������λ��
            else
            {
                GetAllNearPositions(ref atkPositions, selfNormalFeatures);
                return atkPositions;
            }
        }

        // û���κ�����ʱ �������п�λ
        for (int i = 0; i < lineNum; ++i)
            for (int j = 0; j < lineNum; ++j)
                if (board[i, j] == E_Cross.none)
                    atkPositions.Add(new Vector2Int(i, j));
        return atkPositions;
    }

    /* ����Ϊ��-�¼�֦������غ��� */
    protected void AlphaBetaSearch()
    {
        nodeNum = 0;
        curDepth = 2;
        int maxValue = int.MinValue;
        int curValue;
        while (curDepth <= maxDepth && nodeNum <= maxNodeNum)
        {
            curValue = MaxValue(int.MinValue, int.MaxValue, 0);
#if DEBUG_EVERYDEPTH_EVAL
            Debug.Log(string.Format("������ȣ�{0} ����ֵ��{1, -10}λ�ã�{2}", curDepth, curValue, curNextPos));
#endif
            if (curValue > maxValue)
            {
                maxValue = curValue;
                nextPos = curNextPos;
            }
            curDepth += 1;
        }
#if DEBUG_EVAL
        Debug.Log(string.Format("�������ֵ��{0, -10}���λ�ã�{1}", maxValue, nextPos));
#endif
    }
    protected int MaxValue(int alpha, int beta, int depth)
    {
        ++nodeNum;
        if (depth >= curDepth || nodeNum >= maxNodeNum)
        {
#if DEBUG_MAXDEPTH
            if (depth > curMaxDepth)
                curMaxDepth = depth;
#endif
            return Eval();
        }
        int eval = int.MinValue;
        int curEval;

        List<Vector2Int> positions = GetBestPositions(E_Player.AI);
        // ֻ��һ�����ܵ�λ��ʱ�����������
        if (positions.Count == 1)
        {
            // ��������
            board[positions[0].x, positions[0].y] = E_Cross.AI;
            UpdateAllFeatures(positions[0].x, positions[0].y);
            eval = Eval();
            if (depth == 0)
                curNextPos = positions[0];
            // �ָ�����
            board[positions[0].x, positions[0].y] = E_Cross.none;
            UpdateAllFeatures(positions[0].x, positions[0].y);
            return eval;
        }

        // ���������м�ֵλ��
        foreach (Vector2Int pos in positions)
        {
            // ��������
            board[pos.x, pos.y] = E_Cross.AI;
            UpdateAllFeatures(pos.x, pos.y);
            curEval = MinValue(alpha, beta, depth + 1);
            if (curEval > eval)
            {
                eval = curEval;
                // ֻ��λ����ǳ��ʱ���趨��һ��λ��
                if (depth == 0)
                    curNextPos = pos;
#if DEBUG_SEARCH
                Debug.Log(string.Format("��ǰ������ۺ�����{0, -10}��ǰλ��{1}", eval, nextPos));
#endif
            }
            // �ָ�����
            board[pos.x, pos.y] = E_Cross.none;
            UpdateAllFeatures(pos.x, pos.y);
            if (eval >= beta)
                return eval;
            alpha = Mathf.Max(alpha, eval);
        }
        return eval;
    }
    protected int MinValue(int alpha, int beta, int depth)
    {
        ++nodeNum;
        if (depth >= curDepth || nodeNum >= maxNodeNum)
        {
#if DEBUG_MAXDEPTH
            if (depth > curMaxDepth)
                curMaxDepth = depth;
#endif
            return Eval();
        }
        int eval = int.MaxValue;
        List<Vector2Int> positions = GetBestPositions(E_Player.player);

        // ֻ��һ�����ܵ�λ��ʱ�����������
        if (positions.Count == 1)
        {
            // ��������
            board[positions[0].x, positions[0].y] = E_Cross.AI;
            UpdateAllFeatures(positions[0].x, positions[0].y);
            eval = Eval();
            // �ָ�����
            board[positions[0].x, positions[0].y] = E_Cross.none;
            UpdateAllFeatures(positions[0].x, positions[0].y);
            return eval;
        }

        // ���������м�ֵλ��
        foreach (Vector2Int pos in positions)
        {
            // ��������
            board[pos.x, pos.y] = E_Cross.player;
            UpdateAllFeatures(pos.x, pos.y);
            eval = Mathf.Min(MaxValue(alpha, beta, depth + 1), eval);
            // �ָ�����
            board[pos.x, pos.y] = E_Cross.none;
            UpdateAllFeatures(pos.x, pos.y);
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
    protected bool IsAttached_BF(int x, int y, int dir, int line)
    {
        switch (dir)
        {
        case dir_hor:
            return y == line;
        case dir_ver:
            return x == line;
        case dir_div:
            return y - x == FeatureInfo.lineMax / 2 - line;
        case dir_back:
            return x + y == FeatureInfo.lineMax + 3 - line;
        default:
            return false;
        }
    }
}
