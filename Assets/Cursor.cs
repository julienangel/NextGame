using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cursor : MonoBehaviour
{
    [HideInInspector]
    public Vector2 pos, end;

    [HideInInspector]
    public bool canMove = false;

    BoardManager board;

    // Use this for initialization
    void Start()
    {
        board = GameManager.GetInstance().board;
        end = pos;
    }

    void Update()
    {

    }

    public void InitialStart(Vector2 position)
    {
        pos = end = position;
        transform.localPosition = pos;
        gameObject.SetActive(true);
    }


    public void Move(Vector2 dir)
    {
        end = (Vector2)transform.localPosition + dir;
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
    }
}
