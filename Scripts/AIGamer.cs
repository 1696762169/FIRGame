//#define DEBUG_FEATURENUM
#define DEBUG_NODENUM
//#define DEBUG_MAXDEPTH
//#define DEBUG_FAIL
//#define DEBUG_EVERYDEPTH_EVAL
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
    protected Vector2Int curPos;

    // �����ڲ��������������
    protected FeatureInfo AIFi;
    protected FeatureInfo playerFi;
    // ��¼��������ǰ����������
    protected FeatureInfo originAIFi;
    protected FeatureInfo originPlayerFi;

    // ����������ֵ
    protected int[] selfEval = new int[6] { 0, 20, 180, 150, 120, 100 };
    protected int[] enemyEval = new int[5] { 0, -60, -600, -8000, -10000 };
    protected int[] overEval = new int[9] 
    { 0, 99900000, -99950000, 99960000, 99970000, -99980000, -99990000, -100000000, 100000000 };

    // (��ǰ)����������
    protected int curDepth;
    protected int maxDepth;
    // ���/��ǰ���������
    protected int maxNodeNum;
    protected int nodeNum;
    // �Ƿ�����ʧ��
    protected bool fail;

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
        int eval = Eval(E_Player.AI);
        if (eval == overEval[(int)E_GameOver.self5] || eval == overEval[(int)E_GameOver.enemy5])
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
        curDepth = Mathf.Min(curDepth, Chessboard.lineNum * Chessboard.lineNum - Chessboard.Instance.history.Count);

        AlphaBetaSearch();
        // ���������������ǰ������ͬ��
        AIFi = new FeatureInfo(originAIFi);
        playerFi = new FeatureInfo(originPlayerFi);

#if DEBUG_NODENUM
        Debug.Log($"�����������{nodeNum}");
#endif
        return nextPos;
    }

    /* ����Ϊ���ۺ��� */

    /// <summary>
    /// ���������߷��ض���Ϊ�������ۺ���ֵ
    /// </summary>
    protected int Eval(E_Player side, List<Feature> overFeatures = null)
    {
        int eval = -10 * (int)Vector2Int.Distance(curPos, new Vector2Int(lineNum / 2, lineNum / 2));
        bool selfLive3 = false;
        bool selfDead4 = false;
        int scale = side == E_Player.player ? -1 : 1;
        FeatureInfo selfFi = side == E_Player.AI ? AIFi : playerFi;
        FeatureInfo enemyFi = side == E_Player.AI ? playerFi : AIFi;
        Feature atkFeature1 = null;
        Feature atkFeature2 = null;
        Feature defFeature = null;
        E_GameOver over = E_GameOver.none;

        // ͳ���ҷ�����
        foreach (Feature f in selfFi.Features)
        {
            switch (f.type)
            {
            case E_FeatureType.live3:
                if (selfDead4 && (int)over < (int)E_GameOver.selfLive3Dead4)
                {
                    over = E_GameOver.selfLive3Dead4;
                    atkFeature2 = f;
                }
                else if (selfLive3 && (int)over < (int)E_GameOver.selfDoubleLive3)
                {
                    over = E_GameOver.selfDoubleLive3;
                    atkFeature2 = f;
                }
                else if ((int)over < (int)E_GameOver.selfLive4)
                    atkFeature1 = f;
                selfLive3 = true;
                eval += selfEval[(int)f.type];
                break;
            case E_FeatureType.dead4:
                if (selfDead4 && (int)over < (int)E_GameOver.selfLive4)
                {
                    over = E_GameOver.selfLive4;
                    atkFeature2 = f;
                }
                else if (selfLive3 && (int)over < (int)E_GameOver.selfLive3Dead4)
                {
                    over = E_GameOver.selfLive3Dead4;
                    atkFeature2 = f;
                }
                else if ((int)over < (int)E_GameOver.selfLive4)
                    atkFeature1 = f;
                selfDead4 = true;
                eval += selfEval[(int)f.type];
                break;
            case E_FeatureType.live4:
                if ((int)over < (int)E_GameOver.selfLive4)
                {
                    over = E_GameOver.selfLive4;
                atkFeature1 = f;
                }
                break;
            case E_FeatureType.five:
                over = E_GameOver.self5;
                atkFeature1 = f;
                break;
            default:
                eval += selfEval[(int)f.type];
                break;
            }
        }

        // ͳ�Ƶз�����
        foreach (Feature f in enemyFi.Features)
        {
            switch (f.type)
            {
            case E_FeatureType.dead3:
                if (over == E_GameOver.selfDoubleLive3)
                    over = E_GameOver.none;
                eval += enemyEval[(int)f.type];
                break;
            case E_FeatureType.live3:
                if (!selfDead4 && (int)over < (int)E_GameOver.enemyLive3)
                {
                    over = E_GameOver.enemyLive3;
                    defFeature = f;
                }
                else if(over == E_GameOver.selfDoubleLive3)
                {
                    over = E_GameOver.none;
                    defFeature = f;
                }
                eval += enemyEval[(int)f.type];
                break;
            case E_FeatureType.dead4:
                if ((int)over < (int)E_GameOver.enemyDead4)
                {
                    over = E_GameOver.enemyDead4;
                    defFeature = f;
                }
                break;
            case E_FeatureType.live4:
                if ((int)over < (int)E_GameOver.enemyLive4)
                {
                    over = E_GameOver.enemyLive4;
                    defFeature = f;
                }
                break;
            case E_FeatureType.five:
                over = E_GameOver.enemy5;
                defFeature = f;
                break;
            default:
                eval += enemyEval[(int)f.type];
                break;
            }
        }

        // ��ùؼ�����
        if (overFeatures != null && over != E_GameOver.none)
        {
            if (overEval[(int)over] > 0)
            {
                overFeatures.Add(new Feature(atkFeature1));
                if (atkFeature2 != null)
                    overFeatures.Add(new Feature(atkFeature2));
            }
            else
                overFeatures.Add(new Feature(defFeature));
        }

        // ���ر�ɱ�����ͳ�Ƶõ�����������ֵ
        if (over != E_GameOver.none)
            return overEval[(int)over] * scale;
        else
            return eval * scale;
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

                Feature f = new Feature(E_FeatureType.none, dir, line, loca);

                // �÷ֶ�ʣ��ռ� ��������ʣ��ռ�����
                int lastSpace = sap.y - loca;
                // ��¼��ǰ����ʹ�õĿռ�
                int used = 0;
                // ��¼�Ƿ񱻷��
                bool blocked = space == 5 || loca == 0 || pieces[loca - 1] == enemy;

                // ��Ҫ2�ռ�
                if (lastSpace >= 2 && pieces[loca + 1] != E_Cross.none && loca + 2 > lastEnd)
                {
                    blocked |= pieces[loca + 2] == enemy;
                    // ��/���
                        f.type = blocked ? E_FeatureType.dead2 : E_FeatureType.live2;
                    used = 2;
                }

                // ��Ҫ3�ռ�
                if (lastSpace >= 3 && pieces[loca + 2] != E_Cross.none && loca + 3 > lastEnd)
                {
                    blocked |= pieces[loca + 3] == enemy;
                    if (pieces[loca + 1] == E_Cross.none)
                        // ��/���
                        f.type = blocked ? E_FeatureType.dead2 : E_FeatureType.live2;
                    else
                        // ��/����
                        f.type = blocked ? E_FeatureType.dead3 : E_FeatureType.live3;
                    used = 3;
                }

                // ��Ҫ4�ռ�
                if (lastSpace >= 4 && pieces[loca + 3] != E_Cross.none && loca + 4 > lastEnd)
                {
                    blocked |= pieces[loca + 4] == enemy;
                    if (pieces[loca + 1] == E_Cross.none && pieces[loca + 2] == E_Cross.none)
                        // ��/���
                        f.type = blocked ? E_FeatureType.dead2 : E_FeatureType.live2;
                    else if (pieces[loca + 1] != E_Cross.none && pieces[loca + 2] != E_Cross.none)
                        // ��/����
                        f.type = blocked ? E_FeatureType.dead4 : E_FeatureType.live4;
                    else
                        // ��/����
                        f.type = blocked ? E_FeatureType.dead3 : E_FeatureType.live3;
                    used = 4;
                }

                // ��Ҫ5�ռ�
                if (lastSpace >= 5 && pieces[loca + 4] != E_Cross.none && loca + 5 > lastEnd)
                {
                    // ͳ���м�����λ�õĿ�λ��
                    int blankCount = 0;
                    for (int i = 1; i <= 3; ++i)
                        blankCount += pieces[loca + i] == E_Cross.none ? 1 : 0;

                    blocked |= pieces[loca + 5] == enemy;
                    switch (blankCount)
                    {
                    case 3:     // ����
                        if (!blocked)
                            f.type = E_FeatureType.dead2;
                        break;
                    case 2:     // ����
                        f.type = E_FeatureType.dead3;
                        break;
                    case 1:     // ����
                        f.type = E_FeatureType.dead4;
                        break;
                    case 0:     // ����
                        f.type = E_FeatureType.five;
                        break;
                    }
                    if (!(blankCount == 0 && blocked))
                        used = 5;
                }

                // ��������
                if (f.type != E_FeatureType.none)
                {
                    // ��¼��ǰ������һ��λ��
                    lastEnd = loca + used;
                    fi.Features.Add(f);
                }
                // ���ڴ�λ���Ѿ�������������ʼ�� �ʺ�����������ͬ���Ӷ����迼��
                while (++loca < sap.y && pieces[loca] != E_Cross.none)
                    ;
            }// end of each sap
        }// end of foreach statement
    }

    /* ����Ϊ��ȡ���м�ֵλ����غ��� */

    /// <summary>
    /// ���ݼ���������ȡ����λ��
    /// </summary>
    protected List<Vector2Int> GetUrgentPositions(Feature f)
    {
        // �������λ��
        List<Vector2Int> positions = new List<Vector2Int>();
        switch (f.type)
        {
        case E_FeatureType.live4:
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca - 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 4));
            break;
        case E_FeatureType.dead4:
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca - 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 1));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 2));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 3));
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca + 4));
            break;
        case E_FeatureType.live3:
            positions.Add(FeatureToBoard(f.dir, f.line, f.loca - 1));
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
    protected List<Vector2Int> GetBestPositions(List<Feature> overFeatures, int depth)
    {
        // ��¼�����м�ֵ��λ��
        List<Vector2Int> positions = new List<Vector2Int>();

        // �б�ɱ����ʱ �����丽��λ��
        //if (overFeatures.Count != 0)
        //    //&& (depth == maxDepth - 1                   // ��ײ�ʱֻ�迼�Ǳ�ɱλ��
        //    //|| depth == 0 && positions.Count == 1       // �����ֻ��һ����ɱλ��ʱֱ�ӷ��ظ�λ��
        //    //|| overFeatures[0].type != E_FeatureType.five))
        //{
        //    foreach (Feature f in overFeatures)
        //        foreach (Vector2Int pos in GetUrgentPositions(f))
        //            positions.Add(pos);
        //    if (positions.Count != 0)
        //        return positions;
        //}
        

        // û���κ�����ʱ �����������Ӹ����Ŀ�λ
        bool[,] added = new bool[lineNum, lineNum];
        int posNum = 16;
        Vector2Int[] tempPositions = new Vector2Int[posNum];
        for (int i = 0; i < posNum; i++)
            tempPositions[i] = new Vector2Int();
        for (int i = 0; i < lineNum; ++i)
            for (int j = 0; j < lineNum; ++j)
                if (board[i, j] != E_Cross.none)
                {
                    tempPositions[0].Set(i + 1, j);
                    tempPositions[1].Set(i, j + 1);
                    tempPositions[2].Set(i + 1, j + 1);
                    tempPositions[3].Set(i + 1, j - 1);
                    tempPositions[4].Set(i - 1, j);
                    tempPositions[5].Set(i, j - 1);
                    tempPositions[6].Set(i - 1, j - 1);
                    tempPositions[7].Set(i - 1, j + 1);
                    tempPositions[8].Set(i + 2, j);
                    tempPositions[9].Set(i, j + 2);
                    tempPositions[10].Set(i + 2, j + 2);
                    tempPositions[11].Set(i + 2, j - 2);
                    tempPositions[12].Set(i - 2, j);
                    tempPositions[13].Set(i, j - 2);
                    tempPositions[14].Set(i - 2, j - 2);
                    tempPositions[15].Set(i - 2, j + 2);
                    foreach (Vector2Int pos in tempPositions)
                    {
                        try
                        {
                            if (board[pos.x, pos.y] == E_Cross.none && !added[pos.x, pos.y])
                            {
                                positions.Add(new Vector2Int(pos.x, pos.y));
                                added[pos.x, pos.y] = true;
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
        return positions;
    }

    /* ����Ϊ��-�¼�֦������غ��� */
    protected void AlphaBetaSearch()
    {
        nodeNum = 0;
        curDepth = maxDepth - 1;
        int maxValue;
        fail = false;
        do
        {
            curDepth++;
            maxValue = MaxValue(int.MinValue, int.MaxValue, 0);
        } while (nodeNum < maxNodeNum / 10);
#if DEBUG_EVAL
            Debug.Log(string.Format("�������ֵ��{0, -10}���λ�ã�{1}", maxValue, nextPos));
#endif
#if DEBUG_FAIL
        if (fail)
            Debug.LogWarning($"����{nodeNum}���ڵ����δ�������");
#endif
    }
    protected int MaxValue(int alpha, int beta, int depth)
    {
        ++nodeNum;
        if (depth >= curDepth || nodeNum >= maxNodeNum)
        {
            fail = nodeNum >= maxNodeNum;
            return Eval(E_Player.player);
        }

        int eval = int.MinValue;
        int curEval;
        List<Feature> overFeatures = new List<Feature>();
        Eval(E_Player.player, overFeatures);
        List<Vector2Int> positions = GetBestPositions(overFeatures, depth);
#if DEBUG_MAXDEPTH
        int nextDepth = positions.Count < 5 ? depth : depth + 1;
        if (nextDepth == depth)
            Debug.Log($"��{depth}���ظ�����");
#endif
        // ���������м�ֵλ��
        foreach (Vector2Int pos in positions)
        {
            // ��������
            board[pos.x, pos.y] = E_Cross.AI;
            UpdateAllFeatures(pos.x, pos.y);
            curPos = pos;
            curEval = MinValue(alpha, beta, depth + 1);
            //curEval = MinValue(alpha, beta, nextDepth);
            if (curEval > eval)
            {
                eval = curEval;
                // ֻ��λ����ǳ��ʱ���趨��һ��λ��
                if (depth == 0)
                    nextPos = pos;
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
            fail = nodeNum >= maxNodeNum;
            return Eval(E_Player.AI);
        }

        int eval = int.MaxValue;
        List<Feature> overFeatures = new List<Feature>();
        Eval(E_Player.AI, overFeatures);
        List<Vector2Int> positions = GetBestPositions(overFeatures, depth);
#if DEBUG_MAXDEPTH
        int nextDepth = positions.Count < 5 ? depth : depth + 1;
        if (nextDepth == depth)
            Debug.Log($"��{depth}���ظ�����");
#endif

        // ���������м�ֵλ��
        foreach (Vector2Int pos in positions)
        {
            // ��������
            board[pos.x, pos.y] = E_Cross.player;
            UpdateAllFeatures(pos.x, pos.y);
            curPos = pos;
            eval = Mathf.Min(MaxValue(alpha, beta, depth + 1), eval);
            //eval = Mathf.Min(MaxValue(alpha, beta, nextDepth), eval);
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
