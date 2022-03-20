using UnityEngine;
using View;

namespace Controllers
{
    public class UiManager : MonoBehaviour
    {
        [SerializeField] private ShuffleWidget shuffleWidget;

        private void Awake()
        {
            GridManager.OnShuffleItems += OnShuffleItemsHandler;
        }

        private void OnDestroy()
        {
            GridManager.OnShuffleItems -= OnShuffleItemsHandler;
        }

        private void OnShuffleItemsHandler()
        {
            Instantiate(shuffleWidget);
        }
    }
}