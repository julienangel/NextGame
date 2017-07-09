using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager: MonoBehaviour  {

    public int[,] board;

    private Cursor cursor;

    public static BoardManager Create()
    {
        GameObject gameObject = new GameObject();
        gameObject.name = "BoardManager";
        BoardManager board = gameObject.AddComponent<BoardManager>();
        return board;
    }
	
    public void NewBoard(int size)
    {
        board = new int[size, size];
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                board[i, j] = 0;
            }
        }
    }

    public void AddOnBoard(int number, Vector2 pos)
    {
        board[(int)pos.x, (int)pos.y] = number;
    }
}
