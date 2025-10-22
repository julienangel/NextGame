using System;
using UnityEngine;

public class BorderInfo : MonoBehaviour
{
    public bool IsLeftBorder { get; private set; }
    public bool IsRightBorder { get; private set; }
    public bool IsTopBorder { get; private set; }
    public bool IsBottomBorder { get; private set; }
    
    private static readonly int RoundedCornersID = Shader.PropertyToID("_RoundedCorners");
    private Material _material;

    private void Awake()
    {
        // Initialize shader property ID
        _ = RoundedCornersID;
    }

    public void SetBorders(bool left, bool right, bool top, bool bottom)
    {
        IsLeftBorder = left;
        IsRightBorder = right;
        IsTopBorder = top;
        IsBottomBorder = bottom;
        
        if (_material == null)
        {
            _material = GetComponent<SpriteRenderer>()?.material;
        }
        if (_material != null)
        {
            var roundValue = 15f;
            
            _material.SetVector(RoundedCornersID, new Vector4(
                (top && right) ? roundValue : 0,   // x: top right
                (bottom && right) ? roundValue : 0, // y: bottom right
                (top && left) ? roundValue : 0,     // z: top left
                (bottom && left) ? roundValue : 0   // w: bottom left
            ));
        }
    }
}