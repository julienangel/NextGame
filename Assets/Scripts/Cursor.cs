using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cursor : MonoBehaviour
{
    [HideInInspector]
    public Vector2 pos, end;

    [HideInInspector]
    public bool canMove = true;

    BoardManager board;
    GameManager gameManager;

    //save memory
    Vector2 right = Vector2.right;
    Vector2 left = Vector2.left;
    Vector2 up = Vector2.up;
    Vector2 down = Vector2.down;

    // Use this for initialization
    void Start()
    {
        canMove = true;
        gameManager = GameManager.GetInstance();
        board = GameManager.GetInstance().board;
        end = pos;
    }

    void Update()
    {
        if(canMove)
        {
            if (Input.GetKeyDown(KeyCode.D))
                Move(right);
            else if (Input.GetKeyDown(KeyCode.A))
                Move(left);
            else if (Input.GetKeyDown(KeyCode.W))
                Move(up);
            else if (Input.GetKeyDown(KeyCode.S))
                Move(down);
        }
    }

    public void InitialStart(Vector2 position)
    {
        pos = end = position;
        transform.localPosition = pos;
        gameObject.SetActive(true);
    }


    public void Move(Vector2 dir)
    {
        if(board.AvailableToMove(pos, dir))
        {
            end = (Vector2)transform.localPosition + dir;
            StartCoroutine(MoveToEnd());
        }
    }

    IEnumerator MoveToEnd()
    {
        pos = transform.localPosition;
        while (pos != end)
        {
            canMove = false;
            pos = Vector2.MoveTowards(pos, end, 5 * Time.deltaTime);
            transform.localPosition = pos;
            yield return new WaitForEndOfFrame();
        }
        canMove = true;
        if(board.FinishedLevel())
        {
            GameManager.GetInstance().levelNumber++;
            GameManager.GetInstance().PlayUnlockedLevel();
        }
    }
}
