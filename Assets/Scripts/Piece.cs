using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Serialization;

public class Piece : MonoBehaviour {
    
    public TextMeshProUGUI numberText;
    [SerializeField] private SpriteRenderer _scribbleOverlay;
    
    [FormerlySerializedAs("sprite")] public SpriteRenderer _spriteRenderer;
    
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
                _spriteRenderer.DOFade(0.0f, 0.2f);
                board.DecrementNumberCount();
            }
        }
    }

    public void Initialize()
    {
        numberText.text = "" + number;
        //_spriteRenderer.enabled = true;
        _spriteRenderer.DOFade(1, 0f);
        //_scribbleOverlay.enabled = false;
    }
}
