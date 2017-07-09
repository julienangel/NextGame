using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackGroundManager : MonoBehaviour
{
    //
    private Sprite _bgPiece;
    private List<GameObject> _backgroundPieces = new List<GameObject>(225);
    //
    private int _bgPiecesCount;

    void Start()
    {

    }

    public static BackGroundManager Create()
    {
        GameObject gameObject = new GameObject();
        gameObject.name = "Background Manager";
        BackGroundManager backgroundManager = gameObject.AddComponent<BackGroundManager>();
        backgroundManager._bgPiecesCount = backgroundManager._backgroundPieces.Capacity;
        backgroundManager._bgPiece = Resources.Load<Sprite>("Sprites/BackgroundPieceNovo");
        backgroundManager.AddComponentToPieces(backgroundManager._bgPiecesCount);
        backgroundManager.DesativatePieces();
        return backgroundManager;
    }

    public void DisplayBackground(int levelSize)
    {
        int pos = 0;
        for (int i = 0; i < levelSize; i++)
        {
            for (int j = 0; j < levelSize; j++)
            {
                _backgroundPieces[pos].SetActive(true);
                _backgroundPieces[pos].GetComponent<SpriteRenderer>().sprite = _bgPiece;
                _backgroundPieces[pos].transform.localPosition = new Vector2(i, j);
                pos++;
            }
        }
        Camera.main.transform.localPosition = new Vector3(levelSize / 2, levelSize / 2, -10);
    }

    public void AddComponentToPieces(int size)
    {
        Transform parent = this.transform;
        GameObject newGo = null;
        for (int i = 0; i < size; i++)
        {
            if (newGo == null)
            {
                newGo = new GameObject();
                newGo.transform.parent = parent;
                newGo.AddComponent<SpriteRenderer>();
            }
            else
            {
                newGo = GameObject.Instantiate(newGo, parent);
            }
            newGo.name = "Background";
            _backgroundPieces.Add(newGo);
        }
        GC.Collect();
    }

    public void DesativatePieces()
    {
        for (int i = 0; i < _bgPiecesCount; i++)
        {
            _backgroundPieces[i].GetComponent<SpriteRenderer>().sprite = null;
            _backgroundPieces[i].SetActive(false);
        }
    }
}
