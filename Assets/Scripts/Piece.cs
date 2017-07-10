using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Piece : MonoBehaviour {
    
    public TextMesh numberText;

    [HideInInspector]
    public SpriteRenderer sprite;
    
    public int number;

    private BoardManager board;

	// Use this for initialization
	void Start () {
        numberText = GetComponentInChildren<TextMesh>();
        sprite = GetComponent<SpriteRenderer>();
        board = GameManager.GetInstance().board;
    }

    public void UpdateValue()
    {
        if (number > 0)
        {
            number--;
            numberText.text = "" + number;
            if (number <= 0)
            {
                this.gameObject.tag = "Untagged";
                numberText.text = "";
                board.DecrementNumberCount();
            }
        }
    }

    public void Initialize()
    {
        numberText.text = "" + number;
    }
}
