using Controllers;
using UnityEngine;
using Zenject;

namespace ZenjectInstallers
{
    public class MainInstaller : MonoInstaller
    {
        [SerializeField]
        private GameObject container;
        
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<ScoreController>().AsSingle();
            Container.BindInterfacesAndSelfTo<AudioController>().FromNewComponentOn(container).AsSingle().NonLazy();
        }
    }
}
