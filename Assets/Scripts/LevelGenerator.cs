using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator
{
    public int max_pieces;
    public int max_sum;
    public int num_max;
    public int size;
    public int[,] board;
    public Vector2 initial, atual;
    public Vector2 cursorPos;
    private bool procurarFim = false;
    private Level _level;
    private LevelSaver _lvlSaver;
    private int numberName = 0;

    //save memory
    Vector2 right = Vector2.right;
    Vector2 left = Vector2.left;
    Vector2 up = Vector2.up;
    Vector2 down = Vector2.down;

    //public static LevelGenerator Create()
    //{
    //    GameObject gameObject = new GameObject();
    //    gameObject.name = "GenerateLevels";
    //    LevelGenerator gerador = gameObject.AddComponent<LevelGenerator>();
    //    gerador.level = new Level();
    //    return gerador;
    //}

    public LevelGenerator()
    {
        _level = new Level();
        _lvlSaver = new LevelSaver();
    }

    public void GenerateLevel(int max_pieces, int num_max, int max_sum, int size, int numberName)
    {
        this.max_pieces = max_pieces;
        this.num_max = num_max;
        this.size = size;
        this.max_sum = max_sum;
        this.numberName = numberName;
        this._level.piecesInfoList = new List<PiecesInfo>();
        this._level.solucao = new List<Vector2>();
        this._level.size = new Vector2(size, size);

        //generate base
        board = new int[size, size];

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                board[j, i] = 0;
            }
        }

        //Choose initial
        initial = new Vector2(Random.Range(0, size), Random.Range(0, size));

        //save cursor initial position
        this._level.mousePos = initial;

        IncCoord(initial);

        atual = initial;

        for (int k = 0; k < max_sum; k++)
        {
            GerarDirecao();
        }

        procurarFim = true;

        while(procurarFim)
        {
            GerarDirecao();
        }

        SaveLevelInfo();
    }

    public void GerarDirecao()
    {
        //Verificar posicoes disponiveis
        List<int> disp = VerificarDisponiveis();
        int dir = disp[Random.Range(0, disp.Count)];


        if (disp.Count <= 0)
        {
            GenerateLevel(this.max_pieces, this.num_max, this.max_sum, this.size, this.numberName);
        }

        //Testar direita
        if (dir == 0)
        {
            if (procurarFim)
            {
                if (board[(int)(atual.x + 1), (int)atual.y] <= 0)
                {
                    atual += right;
                    _level.solucao.Add(right);
                    AdicionarFim(atual);
                }
                else
                {
                    atual += right;
                    _level.solucao.Add(right);
                    IncCoord(atual);
                }
            }
            else
            {
                atual += right;
                _level.solucao.Add(right);
                IncCoord(atual);
            }
        }
        // Testar esquerda
        if (dir == 1)
        {
            if (procurarFim)
            {
                if (board[(int)(atual.x - 1), (int)atual.y] <= 0)
                {
                    atual += left;
                    _level.solucao.Add(left);
                    AdicionarFim(atual);
                }
                else
                {
                    atual += left;
                    _level.solucao.Add(left);
                    IncCoord(atual);
                }
            }
            else
            {
                atual += left;
                _level.solucao.Add(left);
                IncCoord(atual);
            }
        }

        // Testar baixo
        if (dir == 2)
        {
            if (procurarFim)
            {
                if (board[(int)(atual.x), (int)atual.y - 1] <= 0)
                {
                    atual += down;
                    _level.solucao.Add(down);
                    AdicionarFim(atual);
                }
                else
                {
                    atual += down;
                    _level.solucao.Add(down);
                    IncCoord(atual);
                }
            }
            else
            {
                atual += down;
                _level.solucao.Add(down);
                IncCoord(atual);
            }
        }

        // Testar cima
        if (dir == 3)
        {
            if (procurarFim)
            {
                if (board[(int)(atual.x), (int)atual.y + 1] <= 0)
                {
                    atual += up;
                    _level.solucao.Add(up);
                    AdicionarFim(atual);
                }
                else
                {
                    atual += up;
                    _level.solucao.Add(up);
                    IncCoord(atual);
                }
            }
            else
            {
                atual += up;
                _level.solucao.Add(up);
                IncCoord(atual);
            }
        }
    }

    public List<int> VerificarDisponiveis()
    {
        List<int> disp = new List<int>();

        //Testar direita
        if (atual.x < size - 1)
        {
            if (board[(int)(atual.x + 1), (int)atual.y] < num_max)
            {
                disp.Add(0);
            }
            else
            {
                if (procurarFim)
                {
                    num_max++;
                    disp.Add(0);
                }
            }
        }

        // Testar esquerda
        if (atual.x > 0)
        {
            if (board[(int)(atual.x - 1), (int)atual.y] < num_max)
            {
                disp.Add(1);
            }
            else
            {
                if (procurarFim)
                {
                    num_max++;
                    disp.Add(1);
                }
            }
        }

        // Testar baixo
        if (atual.y > 0)
        {
            if (board[(int)(atual.x), (int)atual.y - 1] < num_max)
            {
                disp.Add(2);
            }
            else
            {
                if (procurarFim)
                {
                    num_max++;
                    disp.Add(2);
                }
            }
        }

        // Testar cima
        if (atual.y < size - 1)
        {
            if (board[(int)(atual.x), (int)atual.y + 1] < num_max)
            {
                disp.Add(3);
            }
            else
            {
                if (procurarFim)
                {
                    num_max++;
                    disp.Add(3);
                }
            }
        }

        return disp;
    }

    public void AdicionarFim(Vector2 pos)
    {
        board[(int)pos.x, (int)pos.y] = -1;
        procurarFim = false;
    }

    public void IncCoord(Vector2 pos)
    {
        board[(int)pos.x, (int)pos.y] += 1;
    }

    public void SaveLevelInfo()
    {
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if(board[i,j] > 0)
                {
                    PiecesInfo piece = new PiecesInfo();
                    piece.number = board[i, j];
                    piece.position = new Vector2(i, j);
                    _level.piecesInfoList.Add(piece);
                }
                else if(board[i,j] < 0)
                {
                    FinishInfo finish = new FinishInfo();
                    finish.pos = new Vector2(i, j);
                    _level.finishInfo = finish;
                }
            }
        }
        _lvlSaver.SaveInText("" + numberName, this._level);
    }

//    public void CreateLevel(int max_pieces, int num_max, int max_sum, int size)
//    {
//        this.max_pieces = max_pieces;
//        this.num_max = num_max;
//        this.max_sum = max_sum;
//        this.size = size;
//        GenerateLevel(max_pieces, num_max, max_sum, size);
        
//        //_gameManager.lvlLoader.SaveLevelAndMousePos(cursorPos, new Vector2(max_largura, max_altura), nivel, "level999", solucao);
//        //para atualizar a database
//#if UNITY_EDITOR
//        {
//            UnityEditor.AssetDatabase.Refresh();
//        }
//#endif
//        //_gameManager.LoadLevel(999);
//    }
}
