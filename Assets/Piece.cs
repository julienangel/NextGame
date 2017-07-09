using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Piece : MonoBehaviour {

    [HideInInspector]
    public TextMesh numberText;

    [HideInInspector]
    public SpriteRenderer sprite;

    [HideInInspector]
    public int number;

	// Use this for initialization
	void Start () {
        numberText = GetComponentInChildren<TextMesh>();
        sprite = GetComponent<SpriteRenderer>();
        numberText.text = "" + number;
	}
}
