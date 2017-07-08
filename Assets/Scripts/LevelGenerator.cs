using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    public string levelString = "";
    public string solution = "";
    public int max_pieces;
    public int max_sum;
    public int num_max;
    public int size;
    public int[,] board;
    public Vector2 initial, atual;
    public Vector2 cursorPos;
    private bool procurarFim = false;

    public static LevelGenerator Create()
    {
        GameObject gameObject = new GameObject();
        gameObject.name = "GenerateLevels";
        LevelGenerator gerador = gameObject.AddComponent<LevelGenerator>();
        return gerador;
    }

    public void GenerateLevel(int max_pieces, int num_max, int max_sum, int size)
    {
        levelString = "";
        solution = "";
        this.max_pieces = max_pieces;
        this.num_max = num_max;
        this.size = size;

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
        IncCoord(initial);
        atual = initial;

        //save cursor initial position
        cursorPos = new Vector2(atual.x, atual.y);

        //to instantiate the cursor on the right position
        for (int i = 0; i < size; i++)
        {
            if (atual.y == i)
            {
                cursorPos = new Vector2(atual.x, size - (i + 1));
                break;
            }
        }

        for (int k = 0; k < max_sum; k++)
        {
            GerarDirecao();
        }

        procurarFim = true;

        while(procurarFim)
        {
            GerarDirecao();
        }

        GerarString();
    }

    public void GerarDirecao()
    {
        //Verificar posicoes disponiveis
        List<int> disp = VerificarDisponiveis();
        int dir = disp[Random.Range(0, disp.Count)];

        if (disp.Count <= 0)
        {
            CreateLevel(10, 5, 15, 5);
        }

        //Testar direita
        if (dir == 0)
        {
            if (procurarFim)
            {
                if (board[(int)(atual.x + 1), (int)atual.y] <= 0)
                {
                    atual += Vector2.right;
                    solution += "0";
                    AdicionarFim(atual);
                }
                else
                {
                    atual += Vector2.right;
                    solution += "0";
                    IncCoord(atual);
                }
            }
            else
            {
                atual += Vector2.right;
                solution += "0";
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
                    atual += Vector2.left;
                    solution += "1";
                    AdicionarFim(atual);
                }
                else
                {
                    atual += Vector2.left;
                    solution += "1";
                    IncCoord(atual);
                }
            }
            else
            {
                atual += Vector2.left;
                solution += "1";
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
                    atual += Vector2.down;
                    solution += "3";
                    AdicionarFim(atual);
                }
                else
                {
                    atual += Vector2.down;
                    solution += "3";
                    IncCoord(atual);
                }
            }
            else
            {
                atual += Vector2.down;
                solution += "3";
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
                    atual += Vector2.up;
                    solution += "2";
                    AdicionarFim(atual);
                }
                else
                {
                    atual += Vector2.up;
                    solution += "2";
                    IncCoord(atual);
                }
            }
            else
            {
                atual += Vector2.up;
                solution += "2";
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

    public void GerarString()
    {
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (board[j, i] == -1)
                {
                    levelString += ".,";
                }
                else
                {
                    levelString += board[j, i].ToString() + ",";
                }
            }
            if (i != size - 1)
                levelString += "\n";
        }
        Debug.Log(levelString);
    }

    public void CreateLevel(int max_pieces, int num_max, int max_sum, int size)
    {
        this.max_pieces = max_pieces;
        this.num_max = num_max;
        this.max_sum = max_sum;
        this.size = size;
        GenerateLevel(max_pieces, num_max, max_sum, size);
        
        //_gameManager.lvlLoader.SaveLevelAndMousePos(cursorPos, new Vector2(max_largura, max_altura), nivel, "level999", solucao);
        //para atualizar a database
#if UNITY_EDITOR
        {
            UnityEditor.AssetDatabase.Refresh();
        }
#endif
        //_gameManager.LoadLevel(999);
    }
}
