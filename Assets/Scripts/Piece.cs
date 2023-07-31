using UnityEngine;

public class Piece : MonoBehaviour
{
    [Header("Properties")] 
    [SerializeField] private TextMesh numberText;
    
    public int NumberValue { get; private set; }

    private void OnEnable()
    {
        numberText.text = NumberValue <= 0 ? string.Empty : NumberValue.ToString();
    }

    public void OnMove()
    {
        if (NumberValue > 0)
        {
            NumberValue--;
            numberText.text = NumberValue <= 0 ? string.Empty : NumberValue.ToString();
        }
    }
}
