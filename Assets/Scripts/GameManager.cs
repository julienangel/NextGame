using System;
using Unity.Collections;
using UnityEngine;
using Utils;
using Zenject;

public class GameManager : MonoBehaviour
{
    [Header("Windows")] 
    [SerializeField] private GameObject menuWindow;
    [SerializeField] private GameObject inGameWindow;

    [Inject] [ReadOnly] private ObjectPooler _objectPooler;

    private void Awake()
    {
        Screen.SetResolution(720, 1280, false);
    }

    private void OnEnable()
    {
        _objectPooler.CreatePool();
    }
}
