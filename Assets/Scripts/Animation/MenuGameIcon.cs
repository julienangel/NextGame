using UnityEngine;
using DG.Tweening;

public class MenuGameIcon : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _outerPart;

    [Header("Scale Animation")]
    [SerializeField] private float _scaleAmount = 1.1f;
    [SerializeField] private float _scaleDuration = 0.5f;

    [Header("Rotation Animation")]
    [SerializeField] private float _rotationDuration = 0.5f;

    [Header("Loop Settings")]
    [SerializeField] private float _loopInterval = 1f;
    [SerializeField] private float _middleDelay = 0.5f;

    private Sequence _cachedSequence;

    void Start()
    {
        if (_outerPart == null)
        {
            Debug.LogError("Outer part reference is not set!");
            return;
        }

        CreateAnimation();
    }

    private void CreateAnimation()
    {
        _cachedSequence = DOTween.Sequence();

        _cachedSequence.Append(_outerPart.DOScale(_scaleAmount, _scaleDuration).SetEase(Ease.InOutSine));
        _cachedSequence.Join(_outerPart.DORotate(new Vector3(0, 0, 90), _rotationDuration/2, RotateMode.LocalAxisAdd).SetEase(Ease.InOutSine));
        
        _cachedSequence.AppendInterval(_middleDelay);

        _cachedSequence.Append(_outerPart.DOScale(1f, _scaleDuration).SetEase(Ease.InOutSine));
        _cachedSequence.Join(_outerPart.DORotate(new Vector3(0, 0, 90), _rotationDuration/2, RotateMode.LocalAxisAdd).SetEase(Ease.InOutSine));

        _cachedSequence.AppendInterval(_loopInterval);

        _cachedSequence.SetLoops(-1, LoopType.Restart);
    }

    private void OnDestroy()
    {
        _cachedSequence?.Kill();
    }
}
