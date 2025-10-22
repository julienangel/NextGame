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

    public GameObject DisplayPiece(int i, Vector2 pos, int number)
    {
        _piecesList[i].gameObject.SetActive(true);
        _piecesList[i].transform.localPosition = pos;
        _piecesList[i].number = number;
        _piecesList[i].Initialize();
        return _piecesList[i].gameObject;
    }

    public GameObject DisplayFinish(Vector2 pos)
    {
        finish.SetActive(true);
        finish.GetComponent<SpriteRenderer>().sortingOrder = 1;
        finish.transform.localPosition = pos;
        return finish;
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
        finish = Instantiate(Resources.Load<GameObject>("Prefabs/Finish"), transform);
        finish.name = "Finish";
        GC.Collect();
        DesativatePieces();
    }

    public void DesativatePieces()
    {
        for (int i = 0; i < _piecesCount; i++)
        {
            _piecesList[i].gameObject.SetActive(false);
            _piecesList[i].gameObject.tag = "Untagged";
        }
        
        var finishComponent = finish.GetComponent<FinishPiece>();
        finishComponent.SetAsClosed();
        finish.SetActive(false);
    }
}
