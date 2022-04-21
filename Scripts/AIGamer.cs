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
        UpdateFeature = new UnityAction<int, int, int>[2, 2, 4];
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
    protected Features fs;
    // ��¼��������ǰ����������
    protected Features originFs;

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
    protected UnityAction<int, int, int>[,,] UpdateFeature;

    /// <summary>
    /// ��ʼ������
    /// </summary>
    public void Init()
    {
        board = new E_Cross[Chessboard.lineNum, Chessboard.lineNum];
        for (int i = 0; i < Chessboard.lineNum; ++i)
            for (int j = 0; j < Chessboard.lineNum; ++j)
                board[i, j] = E_Cross.none;
        fs = new Features();
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
        originFs = new Features(fs);
        // �������
        Vector2Int pos = new Vector2Int(Random.Range(0, 14), Random.Range(0, 14));
        while(board[pos.x, pos.y] != E_Cross.none)
        {
            pos.x = Random.Range(0, 14);
            pos.y = Random.Range(0, 14);
        }

        // ���������������ǰ������ͬ��
        fs = new Features(originFs);
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
        border[dir_hor, 0] = x + 4 >= Chessboard.lineNum ? Chessboard.lineNum - x - 1 : 4;
        border[dir_hor, 1] = x < 4 ? x : 4;
        // ����߽�
        border[dir_ver, 0] = y + 4 >= Chessboard.lineNum ? Chessboard.lineNum - y - 1 : 4;
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

        // ����������
        for (int nega = 0; nega < 2; ++nega)
        {
            // ����
            for (offset = 1; offset <= ni.border[dir_hor, nega]; ++offset)
                UpdateFeature[c, nega, offset](x + offset, y, ni.nearby[dir_hor]);
            // ����
            for (offset = 1; offset <= ni.border[dir_ver, 0]; ++offset)
                UpdateFeature[c, nega, offset](x, y + offset, ni.nearby[dir_ver]);
            // ����-����
            for (offset = 1; offset <= ni.border[dir_div, 0]; ++offset)
                UpdateFeature[c, nega, offset](x + offset, y + offset, ni.nearby[dir_div]);
            // ����-����
            for (offset = 1; offset <= ni.border[dir_back, 0]; ++offset)
                UpdateFeature[c, nega, offset](x + offset, y - offset, ni.nearby[dir_back]);
        }
    }

    /// ����Ϊ�������º���(UpdateFeature)
    /// C: �½�����(create) D: ��������(destroy)
    /// P: ����ƫ�� N: ����ƫ��
    /// ���ֱ�ʾ���ԭλ��ƫ����
    /// x: ������λ�ú�����
    /// y: ������λ��������
    /// nearby: ԭλ�ø÷����ϵ������Ų�
    protected void UF_C_P_1(int x, int y, int nearby)
    {

    }
    protected void UF_C_P_2(int x, int y, int nearby)
    {

    }
    protected void UF_C_P_3(int x, int y, int nearby)
    {

    }
    protected void UF_C_P_4(int x, int y, int nearby)
    {

    }
    protected void UF_C_N_1(int x, int y, int nearby)
    {

    }
    protected void UF_C_N_2(int x, int y, int nearby)
    {

    }
    protected void UF_C_N_3(int x, int y, int nearby)
    {

    }
    protected void UF_C_N_4(int x, int y, int nearby)
    {

    }
    protected void UF_D_P_1(int x, int y, int nearby)
    {

    }
    protected void UF_D_P_2(int x, int y, int nearby)
    {

    }
    protected void UF_D_P_3(int x, int y, int nearby)
    {

    }
    protected void UF_D_P_4(int x, int y, int nearby)
    {

    }
    protected void UF_D_N_1(int x, int y, int nearby)
    {

    }
    protected void UF_D_N_2(int x, int y, int nearby)
    {

    }
    protected void UF_D_N_3(int x, int y, int nearby)
    {

    }
    protected void UF_D_N_4(int x, int y, int nearby)
    {

    }

    protected float MaxValue()
    {
        throw new System.NotImplementedException();
    }
}
