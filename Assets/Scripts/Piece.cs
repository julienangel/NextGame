using UnityEngine;
using Utils;

[RequireComponent(typeof(SpriteRenderer))]
public class Piece : MonoBehaviour, IPooledObject
{
    [Header("Properties")] 
    [SerializeField] private TextMesh numberText;
    
    public int NumberValue { get; private set; }
    public PieceType PieceType { get; private set; }

    private static readonly Color PlayableColor = new Color(163,117,255,255);
    private static readonly Color FinishColor = new Color(255,200,177,255);

    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void OnMove()
    {
        if (NumberValue > 0 && PieceType != PieceType.Finish)
        {
            NumberValue--;
            numberText.text = NumberValue <= 0 ? string.Empty : NumberValue.ToString();
        }
    }

    public void SetPiece(int number, PieceType pieceType)
    {
        NumberValue = number;
        PieceType = pieceType;
        
        numberText.text = NumberValue <= 0 ? string.Empty : NumberValue.ToString();
        _spriteRenderer.color = PieceType == PieceType.Playable ? PlayableColor : FinishColor;
    }

    public void OnObjectSpawn()
    {
        return;
    }
}
