using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

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

    [Header("Movement Settings")]
    [SerializeField] private float moveDuration = 0.2f;
    [SerializeField] private Ease moveEase = Ease.OutQuad;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationDuration = 0.2f;
    [SerializeField] private float rotations = 1f;
    [SerializeField] private Ease rotationEase = Ease.Linear;

    [Header("Scale Settings")]
    [SerializeField] private float scaleAmount = 1.2f;
    [SerializeField] private float scaleDuration = 0.1f;
    [SerializeField] private Ease scaleEase = Ease.OutQuad;

    public static event Action ReadyToFinish;

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
        var swipe = SwipeDetector.Instance.GetSwipe();
        if(canMove && swipe != null)
        {
            Move(SwipeDirectionMapping(swipe.Direction));
            //
            // if (Input.GetKeyDown(KeyCode.D))
            //     Move(right);
            // else if (Input.GetKeyDown(KeyCode.A))
            //     Move(left);
            // else if (Input.GetKeyDown(KeyCode.W))
            //     Move(up);
            // else if (Input.GetKeyDown(KeyCode.S))
            //     Move(down);
        }
    }

    private Vector2 SwipeDirectionMapping(SwipeDirection direction)
    {
        switch (direction)
        {
            case SwipeDirection.Right:
                return right;
            case SwipeDirection.Left:
                return left;
            case SwipeDirection.Up:
                return up;
            case SwipeDirection.Down:
                return down;
            default:
                return Vector2.zero;
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
            MoveToEnd();
        }
        
        //After moving, check with the board if the player can finish on next move
        
        if(board.CanFinish()) ReadyToFinish?.Invoke();
    }

    void MoveToEnd()
    {
        canMove = false;
        pos = end;

        // Kill any existing tweens on this transform
        transform.DOKill();

        // Create a sequence to play all animations together
        Sequence moveSequence = DOTween.Sequence();

        // Move tween
        moveSequence.Append(transform.DOLocalMove(end, moveDuration).SetEase(moveEase));

        // Rotation tween (join means it plays at the same time as the move)
        moveSequence.Join(transform.DOLocalRotate(new Vector3(0, 0, 360f * rotations), rotationDuration, RotateMode.FastBeyond360).SetEase(rotationEase));

        // Scale tween - scale up then back down
        moveSequence.Join(transform.DOScale(scaleAmount, scaleDuration).SetEase(scaleEase).SetLoops(2, LoopType.Yoyo));

        // On complete callback
        moveSequence.OnComplete(() =>
        {
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            canMove = true;
            if(board.FinishedLevel())
            {
                GameManager.GetInstance().levelNumber++;
                GameManager.GetInstance().PlayUnlockedLevel();
            }
        });
    }
}
