using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraCalculation
{
    private const float DEFAULT_ORTOGRAPHIC_SIZE = 13f;
    private readonly Vector3 DEFAULT_CAMERA_POSITION = new Vector3(6.5f, 6.5f, -10);
    private const int MAX_SIZE_PUZZLE = 14;

    public void CameraOrtAndPosition(int size)
    {
        float ort = 0;
        Camera cameraMain = Camera.main;
        Vector3 pos = DEFAULT_CAMERA_POSITION;

        ort = DEFAULT_ORTOGRAPHIC_SIZE - ((MAX_SIZE_PUZZLE - size) * 0.9f);

        pos -= ((MAX_SIZE_PUZZLE) - size) * new Vector3(.5f, .5f, 0f);

        float aspect = cameraMain.aspect;
        if (aspect != 0.75f)
            cameraMain.orthographicSize = (float)System.Math.Round((0.5625f * ort) / (aspect), 1);
        else
            cameraMain.orthographicSize = (float)(size * 6.6f) / 8;
        cameraMain.transform.localPosition = pos;
    }
}
