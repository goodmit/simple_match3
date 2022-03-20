using Controllers;
using TMPro;
using UnityEngine;
using Zenject;

namespace View
{
    public class ScoresPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        
        [Inject] private ScoreController _scoreController;

        private void Start()
        {
            _scoreController.OnScoresChanged += OnScoresChangedHandler;
            OnScoresChangedHandler(_scoreController.Scores);
        }

        private void OnDestroy()
        {
            _scoreController.OnScoresChanged -= OnScoresChangedHandler;
        }

        private void OnScoresChangedHandler(int scores)
        {
            label.text = $"Scores: {scores}";
        }
    }
}