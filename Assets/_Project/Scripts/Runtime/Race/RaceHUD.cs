using UnityEngine;
using UnityEngine.UI;

namespace BarrelRacing.Runtime.Race
{
    public sealed class RaceHUD : MonoBehaviour
    {
        [SerializeField] private Text _timerText;
        [SerializeField] private Text _scoreText;
        [SerializeField] private Text _speedometerText;

        public void UpdateTimer(float time)
        {
            if (_timerText) _timerText.text = $"{time:F2}s";
        }

        public void UpdateScore(int score)
        {
            if (_scoreText) _scoreText.text = $"SCORE: {score}";
        }

        public void UpdateSpeed(float speedMps)
        {
            if (_speedometerText) _speedometerText.text = $"{(speedMps * 2.23694f):F0} MPH";
        }
    }
}
