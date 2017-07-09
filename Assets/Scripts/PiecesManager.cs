using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PiecesManager : MonoBehaviour
{
    //
    private List<Piece> _piecesList = new List<Piece>(225);

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

    public void AddComponents(int size)
    {
        Transform parent = this.transform;
        Piece newGo = null;
        for (int i = 0; i < size; i++)
        {
            if (newGo == null)
            {
                newGo = piecePrefab;
            }
            else
            {
                newGo = Instantiate(newGo, parent);
            }
            newGo.name = "Piece";
            _piecesList.Add(newGo);
        }
        GC.Collect();
        DesativatePieces();
    }

    public void DesativatePieces()
    {
        for (int i = 0; i < _piecesCount; i++)
        {
            _piecesList[i].gameObject.SetActive(false);
        }
    }
}
