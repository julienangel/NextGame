using System.Collections;
using UnityEngine;

namespace Gameplay
{
    public class Cursor : MonoBehaviour
    {
        private Vector2Int _pos, _end;

        private bool _canMove = true;

        private BoardManager _board;
        private GameManager _gameManager;

        //save memory
        private readonly Vector2Int _right = Vector2Int.right;
        private readonly Vector2Int _left = Vector2Int.left;
        private readonly Vector2Int _up = Vector2Int.up;
        private readonly Vector2Int _down = Vector2Int.down;

        // Use this for initialization
        private void Start()
        {
            _canMove = true;
            _gameManager = GameManager.Instance;
            _board = _gameManager.BoardManager;
            _end = _pos;
        }

        private void Update()
        {
            if(_canMove)
            {
                if (Input.GetKeyDown(KeyCode.D))
                    Move(_right);
                else if (Input.GetKeyDown(KeyCode.A))
                    Move(_left);
                else if (Input.GetKeyDown(KeyCode.W))
                    Move(_up);
                else if (Input.GetKeyDown(KeyCode.S))
                    Move(_down);
            }
        }

        public void InitialStart(Vector2Int position)
        {
            _pos = _end = position;
            transform.localPosition = (Vector2)_pos;
            gameObject.SetActive(true);
        }


        public void Move(Vector2Int dir)
        {
            if(_board.AvailableToMove(_pos, dir))
            {
                _end = Vector2Int.RoundToInt((Vector2)transform.localPosition + dir);
                StartCoroutine(MoveToEnd());
            }
        }

        private IEnumerator MoveToEnd()
        {
            _pos = Vector2Int.RoundToInt(transform.localPosition);
            while (_pos != _end)
            {
                _canMove = false;
                _pos = Vector2Int.RoundToInt(Vector2.MoveTowards(_pos, _end, 5 * Time.deltaTime));
                transform.localPosition = (Vector2)_pos;
                yield return new WaitForEndOfFrame();
            }
            _canMove = true;
            if(_board.FinishedLevel())
            {
                _gameManager.levelNumber++;
            }
        }
    }
}
