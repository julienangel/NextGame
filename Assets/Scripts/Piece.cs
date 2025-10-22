using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Piece : MonoBehaviour {
    
    public TextMeshProUGUI numberText;
    [SerializeField] private SpriteRenderer _scribbleOverlay;
    
    public SpriteRenderer sprite;
    
    public int number;

    private BoardManager board;

	// Use this for initialization
	void Start () {
        numberText = GetComponentInChildren<TextMeshProUGUI>();
        //sprite = GetComponent<SpriteRenderer>();
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
                //_scribbleOverlay.enabled = true;
                sprite.enabled = false;
                board.DecrementNumberCount();
            }
        }
    }

    public void Initialize()
    {
        numberText.text = "" + number;
        sprite.enabled = true;
        //_scribbleOverlay.enabled = false;
    }
}
