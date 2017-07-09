using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PiecesManager : MonoBehaviour
{
    //
    private List<Piece> _piecesList = new List<Piece>(225);
    private GameObject finish;

    //
    private int _piecesCount = 0;

    //
    private Piece piecePrefab;

    public static PiecesManager Create()
    {
        GameObject gameObject = new GameObject();
        gameObject.name = "PiecesHolder";
        PiecesManager piecesManager = gameObject.AddComponent<PiecesManager>();
        piecesManager._piecesCount = piecesManager._piecesList.Capacity;
        piecesManager.piecePrefab = Resources.Load<Piece>("Prefabs/Square");
        piecesManager.AddComponents(piecesManager._piecesCount);
        return piecesManager;
    }

    public void DisplayPiece(int i, Vector2 pos, int number)
    {
        _piecesList[i].gameObject.SetActive(true);
        _piecesList[i].transform.localPosition = pos;
        _piecesList[i].number = number;
    }

    public void DisplayFinish(Vector2 pos)
    {
        finish.SetActive(true);
        finish.transform.localPosition = pos;
    }

    public void AddComponents(int size)
    {
        Transform parent = this.transform;
        Piece newGo = null;
        //Pieces
        for (int i = 0; i < size; i++)
        {
            if (newGo == null)
            {
                newGo = Instantiate(piecePrefab, parent);
            }
            else
            {
                newGo = Instantiate(newGo, parent);
            }
            newGo.name = "Piece";
            _piecesList.Add(newGo);
        }
        //Finish temporarely
        finish = new GameObject();
        finish.AddComponent<SpriteRenderer>();
        finish.name = "Finish";
        finish.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Sprites/SquareNovo");
        finish.GetComponent<SpriteRenderer>().color = Color.blue;
        GC.Collect();
        DesativatePieces();
    }

    public void DesativatePieces()
    {
        for (int i = 0; i < _piecesCount; i++)
        {
            _piecesList[i].gameObject.SetActive(false);
        }
        finish.SetActive(false);
    }
}
