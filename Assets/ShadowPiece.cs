using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowPiece : MonoBehaviour {

    public SpriteRenderer sprite;

	// Use this for initialization
	void Start () {
        GetComponent<SpriteRenderer>().color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, 0.5f);
	}
}
