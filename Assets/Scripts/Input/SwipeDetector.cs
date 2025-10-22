using System.Collections.Generic;
using UnityEngine;

public enum SwipeDirection
{
    Left,
    Right,
    Up,
    Down
}

public class Swipe
{
    public SwipeDirection Direction { get; private set; }
    public Vector2 StartPosition { get; private set; }
    public Vector2 EndPosition { get; private set; }
    public float TimeAtFront { get; set; }

    public Swipe(SwipeDirection direction, Vector2 startPosition, Vector2 endPosition)
    {
        Direction = direction;
        StartPosition = startPosition;
        EndPosition = endPosition;
        TimeAtFront = 0f;
    }
}

public class SwipeDetector : MonoBehaviour
{
    public static SwipeDetector Instance { get; private set; }

    [Header("Swipe Settings")]
    [SerializeField] private float _minSwipeDistance = 50f;
    [SerializeField] private float _minTimeBetweenSwipes = 0.1f;
    [SerializeField] private int _maxQueueSize = 3;
    [SerializeField] private float _maxSwipeLifetime = 2f;
    
    [Header("Swipe Visualization")]
    [SerializeField] private ParticleSystem _swipeParticles;

    private Queue<Swipe> _swipeQueue = new Queue<Swipe>();
    private Vector2? _swipeStartPosition;
    private float _lastSwipeTime;
    private Camera _mainCamera;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _mainCamera = Camera.main;

        if (_swipeParticles != null)
        {
            _swipeParticles.Stop();
        }
    }

    private void Update()
    {
        RemoveExpiredSwipes();
        DetectSwipe();
        UpdateTrailPosition();
    }

    private void DetectSwipe()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _swipeStartPosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0) && _swipeStartPosition.HasValue)
        {
            Vector2 endPosition = Input.mousePosition;
            Vector2 swipeDelta = endPosition - _swipeStartPosition.Value;

            if (swipeDelta.magnitude >= _minSwipeDistance)
            {
                float timeSinceLastSwipe = Time.time - _lastSwipeTime;

                if (timeSinceLastSwipe >= _minTimeBetweenSwipes)
                {
                    SwipeDirection? direction = GetSwipeDirection(swipeDelta);

                    if (direction.HasValue)
                    {
                        // If queue is full, remove the oldest swipe
                        if (_swipeQueue.Count >= _maxQueueSize)
                        {
                            _swipeQueue.Dequeue();
                        }

                        var swipe = new Swipe(direction.Value, _swipeStartPosition.Value, endPosition);
                        _swipeQueue.Enqueue(swipe);
                        _lastSwipeTime = Time.time;
                        Debug.Log($"Detected swipe: {direction.Value}");

                        // Spawn particles at the end of the swipe
                        //SpawnSwipeParticles(swipe);
                    }
                }
            }

            _swipeStartPosition = null;
        }
    }

    private void SpawnSwipeParticles(Swipe swipe)
    {
        if (_swipeParticles != null && _mainCamera != null)
        {
            float centerScreenX = Screen.width / 2f;
            float screenY = Screen.height * 0.3f;
            Vector3 worldPos = _mainCamera.ScreenToWorldPoint(new Vector3(centerScreenX, screenY, 10f));
            _swipeParticles.transform.position = worldPos;

            float swipeParticleAngle = swipe.Direction switch
            {
                SwipeDirection.Left => 180f,
                SwipeDirection.Right => 0f,
                SwipeDirection.Up => 270f,
                SwipeDirection.Down => 90f,
                _ => 0f
            };

            var main = _swipeParticles.main;
            main.startRotation = swipeParticleAngle * Mathf.Deg2Rad;
            
            _swipeParticles.Play();
        }
    }

    private void UpdateTrailPosition()
    {
        // No longer needed for arrow particles
    }

    private void RemoveExpiredSwipes()
    {
        if (_swipeQueue.Count > 0)
        {
            Swipe firstSwipe = _swipeQueue.Peek();
            firstSwipe.TimeAtFront += Time.deltaTime;

            if (firstSwipe.TimeAtFront > _maxSwipeLifetime)
            {
                _swipeQueue.Dequeue();
                Debug.Log($"Removed expired swipe: {firstSwipe.Direction}");
            }
        }
    }

    private SwipeDirection? GetSwipeDirection(Vector2 delta)
    {
        float absX = Mathf.Abs(delta.x);
        float absY = Mathf.Abs(delta.y);

        if (absX > absY)
        {
            return delta.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;
        }
        else
        {
            return delta.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;
        }
    }

    public Swipe GetSwipe()
    {
        if (_swipeQueue.Count > 0)
        {
            return _swipeQueue.Dequeue();
        }

        return null;
    }
}
