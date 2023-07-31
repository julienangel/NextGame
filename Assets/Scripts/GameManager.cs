using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Windows")] 
    [SerializeField] private GameObject menuWindow;
    [SerializeField] private GameObject inGameWindow;

    void Awake()
    {
        Screen.SetResolution(720, 1280, false);
    }
}
