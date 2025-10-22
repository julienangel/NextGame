using System;
using UnityEngine;
using DG.Tweening;

public class FinishPiece : MonoBehaviour
{
    private SpriteRenderer _borderSprite;
    [SerializeField] private SpriteRenderer _diamondSprite;
    [SerializeField] private Color _closedColor;
    [SerializeField] private Color _openedColor;

    private Tween _spinningTween;

    void Start()
    {
        _borderSprite = GetComponent<SpriteRenderer>();
    }

    public void LevelFinishedEffect(Action callback)
    {
        var cachedLayerIndex = _diamondSprite.sortingOrder;
        _diamondSprite.sortingOrder = 3;

        Sequence sequence = DOTween.Sequence();
        sequence.Append(_borderSprite.DOFade(0f, 0.15f));
        sequence.Append(transform.DOScale(Vector3.one * 30f, 0.5f));

        sequence.OnComplete(() =>
            {
                callback();
                Sequence sequence2 = DOTween.Sequence();
                sequence2.Append(transform.DOScale(Vector3.one, 0.15f));
                sequence2.OnComplete(() =>
                {
                    _diamondSprite.sortingOrder = cachedLayerIndex;
                    _borderSprite.DOFade(1f, 0);
                });
            }
        );
    }

    public void SetAsClosed()
    {
        var rotChild = transform.GetChild(0);
        _diamondSprite.color = _closedColor;
        
        //if (_spinningTween != null) _spinningTween.Kill();
        //_spinningTween = rotChild.DORotate(new Vector3(0, 0, 360), 3f, RotateMode.FastBeyond360)
        //    .SetEase(Ease.Linear)
        //    .SetLoops(-1);
    }

    public void SetAsOpened()
    {
        var rotChild = transform.GetChild(0);
        _diamondSprite.DOColor(_openedColor, 0.2f);

        //if (_spinningTween != null)
        //{
        //    float currentRotation = rotChild.rotation.eulerAngles.z;
        //    float targetRotation = Mathf.Ceil(currentRotation / 45f) * 45f;
        //    float remainingRotation = targetRotation - currentRotation;
        //
        //    _spinningTween.Kill();
        //    rotChild.DORotate(new Vector3(0, 0, targetRotation), remainingRotation / 360f * 3f, RotateMode.Fast)
        //        .SetEase(Ease.Linear);
        //}
    }

    private void OnEnable()
    {
        GameManager.PlayFinishPieceTransition += LevelFinishedEffect;
        Cursor.ReadyToFinish += SetAsOpened;
    }

    private void OnDisable()
    {
        GameManager.PlayFinishPieceTransition -= LevelFinishedEffect;
        Cursor.ReadyToFinish -= SetAsOpened;
    }
}