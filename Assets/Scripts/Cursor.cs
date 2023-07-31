using System;
using UnityEngine;

public class Cursor : MonoBehaviour
{
    private Transform _cachedTransform;

    private void Awake()
    {
        _cachedTransform = transform;
    }

    public void InitialStart(Vector2 position)
    {
        _cachedTransform.localPosition = position;
        gameObject.SetActive(true);
    }

    private async void Move(Vector2 desiredPosition, Action OnMoveFinished = null)
    {
        var pos = transform.localPosition;

        while ((Vector2)pos != desiredPosition)
        {
            pos = Vector2.MoveTowards(pos, desiredPosition, 5 * Time.deltaTime);
            transform.localPosition = pos;
        }

        OnMoveFinished?.Invoke();
    }
}