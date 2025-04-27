using UnityEngine;

namespace Gameplay
{
    public enum PieceType
    {
        Movable,
        Goal,
        Block
    }

    public class Piece : MonoBehaviour
    {
        public byte CurrentNumber { get; set; }
        public PieceType Type { get; set; }
        
        private TextMesh _numberText;
        private SpriteRenderer _sprite;
        
        private void Awake()
        {
            _numberText = GetComponentInChildren<TextMesh>();
            _sprite = GetComponent<SpriteRenderer>();
        }

        public void UpdateValue()
        {
            if (CurrentNumber > 0)
            {
                CurrentNumber--;
                _numberText.text = CurrentNumber.ToString();
                if (CurrentNumber <= 0)
                {
                    this.gameObject.tag = "Untagged";
                    _numberText.text = "";
                }
            }
        }

        public void Initialize()
        {
            _numberText.text = "" + CurrentNumber;
        }
    }
}