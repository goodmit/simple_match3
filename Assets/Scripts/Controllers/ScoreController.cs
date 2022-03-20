using System;
using Controllers.API;
using UnityEngine;

namespace Controllers
{
    public class ScoreController : IInitializable
    {
        public event Action<int> OnScoresChanged;
        
        public void Initialize()
        {
            //Scores = PlayerPrefs.GetInt("scores");
        }

        public int Scores { get; private set; }

        public void AddScores(int count)
        {
            Scores += 10 + (count - 3) * 5;
            OnScoresChanged?.Invoke(Scores);
        }

        public void SaveScores()
        {
            PlayerPrefs.SetInt("scores", Scores);
        }
        
    }
}