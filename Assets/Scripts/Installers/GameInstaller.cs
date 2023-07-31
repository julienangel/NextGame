using UnityEngine;
using Utils;
using Zenject;

namespace Installers
{
    public class GameInstaller : MonoInstaller
    {
        [Header("Prefabs")]
        [SerializeField] private ObjectPooler objectPooler;
        
        public override void InstallBindings()
        {
            Container.Bind<BoardManager>().AsSingle().NonLazy();
            Container.Bind<ObjectPooler>().FromInstance(objectPooler);
        }
    }
}