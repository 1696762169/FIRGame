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
    private AIGamer()
    {
        // ��ʼ������������
        Init();

        // ��ʼ����������ί������
        UpdateFeature = new UnityAction<int, int, int, int>[2, 2, 4];
        UpdateFeature[0, 0, 1] = UF_C_P_1;
        UpdateFeature[0, 0, 2] = UF_C_P_2;
        UpdateFeature[0, 0, 3] = UF_C_P_3;
        UpdateFeature[0, 0, 4] = UF_C_P_4;
        UpdateFeature[0, 1, 1] = UF_C_N_1;
        UpdateFeature[0, 1, 2] = UF_C_N_2;
        UpdateFeature[0, 1, 3] = UF_C_N_3;
        UpdateFeature[0, 1, 4] = UF_C_N_4;
        UpdateFeature[1, 0, 1] = UF_D_P_1;
        UpdateFeature[1, 0, 2] = UF_D_P_2;
        UpdateFeature[1, 0, 3] = UF_D_P_3;
        UpdateFeature[1, 0, 4] = UF_D_P_4;
        UpdateFeature[1, 1, 1] = UF_D_N_1;
        UpdateFeature[1, 1, 2] = UF_D_N_2;
        UpdateFeature[1, 1, 3] = UF_D_N_3;
        UpdateFeature[1, 1, 4] = UF_D_N_4;
    }

    // �����ڲ����������״̬
    protected E_Cross[,] board;
    // �����ڲ��������������
    protected FeatureInfo AIFi;
    protected FeatureInfo playerFi;
    // ��¼��������ǰ����������
    protected FeatureInfo originAIFi;
    protected FeatureInfo originPlayerFi;
    // ����������
    protected const int lineNum = Chessboard.lineNum;

    // ����Ƿ񴴽�������
    protected bool playing;

    // ��¼�ĸ������Ӧ��ֵ
    public const int dir_hor = 0;        // ����
    public const int dir_ver = 1;        // ����
    public const int dir_div = 2;        // ����-����
    public const int dir_back = 3;       // ����-����

    // ����������Ϣλģʽ
    protected const int bit_player = 0b00;  // �������
    protected const int bit_AI = 0b01;      // AI����
    protected const int bit_none = 0b10;    // û������
    protected const int bit_edge = 0b11;    // �߽��� ����ɸ���

    /// <summary>
    /// �洢����ĳλ�ø���ĳһ����������ĺ���
    /// </summary>
    /// ����1: 0: �������� 1: ��������
    /// ����2: 0: ����ƫ�� 1: ����ƫ��
    /// ����3: ƫ����
    protected UnityAction<int, int, int, int>[,,] UpdateFeature;

    /// <summary>
    /// ��ʼ������
    /// </summary>
    public void Init()
    {
        board = new E_Cross[lineNum, lineNum];
        for (int i = 0; i < lineNum; ++i)
            for (int j = 0; j < lineNum; ++j)
                board[i, j] = E_Cross.none;
        AIFi = new FeatureInfo();
        playerFi = new FeatureInfo();
        playing = false;
    }

    /// <summary>
    /// ��Ҳ���ʱͬ���ڲ�״̬
    /// </summary>
    /// <param name="create">�²����Ƿ�������</param>
    public void Synchronize(int x, int y, bool create)
    {
        // ��Ϸ�����������ʱ����ͬ��
        if (!playing && !create)
            return;

        // ��������ʱ��ͬ���ڲ�����
        if (create)
        {
            playing = true;
            board[x, y] = Chessboard.Instance.Board[x, y];
        }
        // ��ȡ������߽��������Ų����
        NearbyInfo ni = new NearbyInfo(x, y);
        ni.border = GetBorder(x, y);
        ni.nearby = GetNearbyBit(x, y, ni.border);

        if (create)
            UpdateAllFeatures(ref ni, true);
        else
        {
            // ��������ʱ��Ҫ���ͬ���ڲ�����
            UpdateAllFeatures(ref ni, false);
            board[x, y] = E_Cross.none;
        }
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
        // �������
        Vector2Int pos = new Vector2Int(Random.Range(0, 14), Random.Range(0, 14));
        while(board[pos.x, pos.y] != E_Cross.none)
        {
            pos.x = Random.Range(0, 14);
            pos.y = Random.Range(0, 14);
        }

        // ���������������ǰ������ͬ��
        AIFi = new FeatureInfo(originAIFi);
        playerFi = new FeatureInfo(originPlayerFi);
        return pos;
    }

    /// <summary>
    /// �����Ϸ�Ƿ����
    /// </summary>
    public bool GoalTest
    {
        get;
        protected set;
    }

    /// <summary>
    /// ��ǰ״̬�����ۺ���
    /// </summary>
    protected float Eval()
    {
        throw new System.NotImplementedException();
    }

    /// <summary>
    /// ��ȡĳλ�ø���4��֮�ڵı߽�
    /// </summary>
    /// <returns>�ĸ�����������߽�</returns>
    protected int[,] GetBorder(int x, int y)
    {
        int[,] border = new int[4, 2];
        // ����߽�
        border[dir_hor, 0] = x + 4 >= lineNum ? lineNum - x - 1 : 4;
        border[dir_hor, 1] = x < 4 ? x : 4;
        // ����߽�
        border[dir_ver, 0] = y + 4 >= lineNum ? lineNum - y - 1 : 4;
        border[dir_ver, 1] = y < 4 ? y : 4;
        // ����-���ϱ߽�
        border[dir_div, 0] = border[dir_hor, 0] < border[dir_ver, 0] ? border[dir_hor, 0] : border[dir_ver, 0];
        border[dir_div, 1] = border[dir_hor, 1] < border[dir_ver, 1] ? border[dir_hor, 1] : border[dir_ver, 1];
        // ����-���±߽�
        border[dir_back, 0] = border[dir_hor, 0] < border[dir_ver, 1] ? border[dir_hor, 0] : border[dir_ver, 1];
        border[dir_back, 1] = border[dir_hor, 1] < border[dir_ver, 0] ? border[dir_hor, 1] : border[dir_ver, 0];
        return border;
    }

    /// <summary>
    /// ��ȡĳ���Ӹ����������������Ϣλ�洢ģʽ
    /// </summary>
    /// <param name="border">������߽���Ϣ</param>
    /// <returns>�����������Ų����
    /// ��9��λ�� ռ�õ�18λ�洢
    /// 00:������� 01:AI���� 10:��λ 11:������</returns>
    protected int[] GetNearbyBit(int x, int y, int[,] border)
    {
        // ��ʼ��Ϊȫ�߽���
        int[] nearby = new int[4] { -1, -1, -1, -1 };

        // ��������λ��
        int setting = (0b11 ^ (int)board[x, y]) << 8;
        for (int i = 0; i < 4; ++i)
            nearby[i] ^= setting;

        // ���ú���
        for (int offset = 1; offset <= border[dir_hor, 0]; ++offset)
            nearby[dir_hor] ^= (0b11 ^ (int)board[x + offset, y]) << (4 + offset) * 2;
        for (int offset = 1; offset <= border[dir_hor, 1]; ++offset)
            nearby[dir_hor] ^= (0b11 ^ (int)board[x - offset, y]) << (4 - offset) * 2;
        // ��������
        for (int offset = 1; offset <= border[dir_ver, 0]; ++offset)
            nearby[dir_ver] ^= (0b11 ^ (int)board[x, y + offset]) << (4 + offset) * 2;
        for (int offset = 1; offset <= border[dir_ver, 1]; ++offset)
            nearby[dir_ver] ^= (0b11 ^ (int)board[x, y - offset]) << (4 - offset) * 2;
        // ��������-����
        for (int offset = 1; offset <= border[dir_div, 0]; ++offset)
            nearby[dir_div] ^= (0b11 ^ (int)board[x + offset, y + offset]) << (4 + offset) * 2;
        for (int offset = 1; offset <= border[dir_div, 1]; ++offset)
            nearby[dir_div] ^= (0b11 ^ (int)board[x - offset, y - offset]) << (4 - offset) * 2;
        // ��������-����
        for (int offset = 1; offset <= border[dir_back, 0]; ++offset)
            nearby[dir_back] ^= (0b11 ^ (int)board[x + offset, y - offset]) << (4 + offset) * 2;
        for (int offset = 1; offset <= border[dir_back, 1]; ++offset)
            nearby[dir_back] ^= (0b11 ^ (int)board[x - offset, y + offset]) << (4 - offset) * 2;
        return nearby;
    }

    /// <summary>
    /// ���ݷ����仯��λ�ø��µ�ǰ����״̬
    /// </summary>
    /// <param name="ni">������Ϣ</param>
    /// <param name="c">�Ƿ����½�����</param>
    protected void UpdateAllFeatures(ref NearbyInfo ni, bool create)
    {
        int x = ni.x;
        int y = ni.y;
        int offset;
        int c = create ? 0 : 1;

        //int lineMax;
        //if (dir == dir_hor || dir == dir_ver)
        //    lineMax = lineNum;
        //else
        //    lineMax = FeatureInfo.lineMax;

        //// ����������
        //for (int nega = 0; nega < 2; ++nega)
        //{
        //    // ����
        //    for (offset = 1; offset <= ni.border[dir_hor, nega]; ++offset)
        //        UpdateFeature[c, nega, offset](x + offset, y, dir_hor, ni.nearby[dir_hor]);
        //    // ����
        //    for (offset = 1; offset <= ni.border[dir_ver, 0]; ++offset)
        //        UpdateFeature[c, nega, offset](x, y + offset, dir_ver, ni.nearby[dir_ver]);
        //    // ����-����
        //    for (offset = 1; offset <= ni.border[dir_div, 0]; ++offset)
        //        UpdateFeature[c, nega, offset](x + offset, y + offset, dir_div, ni.nearby[dir_div]);
        //    // ����-����
        //    for (offset = 1; offset <= ni.border[dir_back, 0]; ++offset)
        //        UpdateFeature[c, nega, offset](x + offset, y - offset, dir_back, ni.nearby[dir_back]);
        //}
    }



    /// <summary>
    /// ����ĳ������һ��ֱ�ߵ�ȫ������
    /// </summary>
    /// <param name="side">�����µ�����������</param>
    /// <param name="dir">ֱ�߷���</param>
    /// <param name="line">������</param>
    /// <param name="pieces">����ֱ���ϵ��������ӷֲ����</param>
    /// ��ʱ�ѽ����̷�Χ������ڵз����ӵ�����ͬ���
    /// ��piecesβ������һ�����з����ӡ������ж�
    protected void UpdateLineFeatures(E_Player side, int dir, int line, E_Cross[] pieces)
    {
        // ����������
        FeatureInfo fi = side == E_Player.AI ? AIFi : playerFi;
        // ��������
        E_Cross enemy = side == E_Player.AI ? E_Cross.player : E_Cross.AI;

        // ���������Ӱ�E_Cross.enemy��E_Feature.dead����Ϊ��� ��¼��������յ��һ��λ��
        List<Vector2Int> saps = new List<Vector2Int>();
        Vector2Int cur = new Vector2Int(-1, -1);
        bool newSap = true;
        for (int loca = 0; loca < lineNum; ++loca)
        {
            if (pieces[loca] == enemy || fi[dir, line, loca].type == E_FeatureType.dead)
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

        // ������С����ķֶ���Ϊdead ���Ƴ��ֶ�
        for (int i = saps.Count - 1; i >= 0; --i)
        {
            if (saps[i].y - saps[i].x < 5)
            {
                for (int loca = saps[i].x; loca < saps[i].y; ++loca)
                    fi[dir, line, loca].type = E_FeatureType.dead;
                saps.RemoveAt(i);
            }
        }
       
        // �ֱ����ÿ���ֶ�
        foreach (Vector2Int sap in saps)
        {
            // �÷ֶ��ܿռ�
            int space = sap.y - sap.x;
            for (int loca = sap.x; loca < sap.y; ++loca)
            {
                // û�����ӵ�λ�ò�������Ϊ������ʼ��
                if (pieces[loca] == E_Cross.none)
                {
                    fi[dir, line, loca].type = E_FeatureType.none;
                    continue;
                }

                // �������������Ŀռ���Ϊ�÷ֶ��ܿռ�
                fi[dir, line, loca].space = space;

                // �÷ֶ��ܿռ���ʣ��ռ� ��������ʣ��ռ�����
                int lastSpace = sap.y - loca;

                // ��Ҫ1�ռ�
                fi[dir, line, loca].type = E_FeatureType.single;
                fi[dir, line, loca].blocked = loca == 0 || pieces[loca - 1] == enemy || pieces[loca + 1] == enemy;

                // ��Ҫ2�ռ�
                if (lastSpace >= 2 && pieces[loca + 1] != E_Cross.none)
                {
                    // ����
                    fi[dir, line, loca].type = E_FeatureType.near2;
                    fi[dir, line, loca].blocked |= pieces[loca + 2] == enemy;
                }

                // ��Ҫ3�ռ�
                if (lastSpace >= 3 && pieces[loca + 2] != E_Cross.none)
                {
                    // С����
                    if (pieces[loca + 1] == E_Cross.none)
                        fi[dir, line, loca].type = E_FeatureType.jump2;
                    // ����
                    else
                        fi[dir, line, loca].type = E_FeatureType.near3;
                    fi[dir, line, loca].blocked |= pieces[loca + 3] == enemy;
                }

                // ��Ҫ4�ռ�
                if (lastSpace >= 4 && pieces[loca + 3] != E_Cross.none)
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
                }

                // ��Ҫ5�ռ�
                if (lastSpace >= 5 && pieces[loca + 4] != E_Cross.none)
                {
                    // ͳ���м�����λ�õĿ�λ��
                    int blankCount = 0;
                    for (int i = 1; i <= 3; ++i)
                        blankCount += pieces[loca + i] == E_Cross.none ? 1 : 0;

                    // ����
                    if (blankCount == 1)
                        fi[dir, line, loca].type = E_FeatureType.jump4;
                    // ����
                    if (blankCount == 0)
                        fi[dir, line, loca].type = E_FeatureType.five;
                    fi[dir, line, loca].blocked |= pieces[loca + 5] == enemy;
                }

                // ���ڴ�λ���Ѿ�������������ʼ�� �ʺ�����������ͬ���Ӷ����迼��
                while (++loca < sap.y && pieces[loca] != E_Cross.none)
                    fi[dir, line, loca].type = E_FeatureType.none;
            }// end of each sap
        }// end of foreach statement
    }



    /// ����Ϊ�������º���(UpdateFeature)
    /// C: �½�����(create) D: ��������(destroy)
    /// P: ����ƫ�� N: ����ƫ��
    /// ���ֱ�ʾ���ԭλ��ƫ����
    /// x: ������λ�ú�����
    /// y: ������λ��������
    /// dir: ��Է���
    /// nearby: ԭλ�ø÷����ϵ������Ų�
    protected void UF_C_P_1(int x, int y, int dir, int nearby)
    {
        
    }
    protected void UF_C_P_2(int x, int y, int dir, int nearby)
    {

    }
    protected void UF_C_P_3(int x, int y, int dir, int nearby)
    {

    }
    protected void UF_C_P_4(int x, int y, int dir, int nearby)
    {

    }
    protected void UF_C_N_1(int x, int y, int dir, int nearby)
    {

    }
    protected void UF_C_N_2(int x, int y, int dir, int nearby)
    {

    }
    protected void UF_C_N_3(int x, int y, int dir, int nearby)
    {

    }
    protected void UF_C_N_4(int x, int y, int dir, int nearby)
    {

    }
    protected void UF_D_P_1(int x, int y, int dir, int nearby)
    {

    }
    protected void UF_D_P_2(int x, int y, int dir, int nearby)
    {

    }
    protected void UF_D_P_3(int x, int y, int dir, int nearby)
    {

    }
    protected void UF_D_P_4(int x, int y, int dir, int nearby)
    {

    }
    protected void UF_D_N_1(int x, int y, int dir, int nearby)
    {

    }
    protected void UF_D_N_2(int x, int y, int dir, int nearby)
    {

    }
    protected void UF_D_N_3(int x, int y, int dir, int nearby)
    {

    }
    protected void UF_D_N_4(int x, int y, int dir, int nearby)
    {

    }

    protected float MaxValue()
    {
        throw new System.NotImplementedException();
    }
}
