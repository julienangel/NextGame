using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FadeScenes : MonoBehaviour {

    public Image fadeImage;

    int time = 1;

    void Start()
    {
        fadeImage.CrossFadeAlpha(0, time, true);
    }

    public IEnumerator PlayFade()
    {
        fadeImage.raycastTarget = true;
        fadeImage.CrossFadeAlpha(1.1f, time, true);
        yield return new WaitForSeconds(time);
        fadeImage.CrossFadeAlpha(-0.1f, time , true);
        yield return new WaitForSeconds(time);
        Desativate();
    }

    public void Desativate()
    {
        fadeImage.raycastTarget = false;
    }
}
