using UnityEngine;
using UnityEngine.UI;

namespace BarrelRacing.Runtime.Race
{
    public sealed class RaceHUD : MonoBehaviour
    {
        [SerializeField] private Text _roundText;
        [SerializeField] private Text _turnIndicatorText;
        [SerializeField] private Text _timerText;
        [SerializeField] private Text _penaltyText;
        [SerializeField] private Text _speedometerText;
        [SerializeField] private Text _averagesScoreboardText;

        public void UpdateRoundInfo(int round, bool isPlayerTurn)
        {
            if (_roundText) _roundText.text = $"ROUND {round} / 3";
            if (_turnIndicatorText) _turnIndicatorText.text = isPlayerTurn ? "YOUR RUN" : "OPPONENT'S RUN";
        }

        public void UpdateLiveTimer(float currentElapsedSeconds)
        {
            if (_timerText) _timerText.text = $"{currentElapsedSeconds:F2}s";
        }

        public void UpdatePenalty(int knockedBarrels)
        {
            if (_penaltyText)
            {
                if (knockedBarrels > 0)
                {
                    float penaltySeconds = knockedBarrels * BarrelCollisionResolver.KNOCK_PENALTY_SECONDS;
                    _penaltyText.text = $"+{penaltySeconds:F1}s PENALTY ({knockedBarrels} Knock{(knockedBarrels > 1 ? "s" : "")})";
                    _penaltyText.color = Color.red;
                }
                else
                {
                    _penaltyText.text = "NO PENALTIES";
                    _penaltyText.color = Color.green;
                }
            }
        }

        public void UpdateSpeedometer(float speedMps)
        {
            if (_speedometerText) _speedometerText.text = $"{(speedMps * 2.23694f):F0} MPH";
        }

        public void UpdateScoreboard(float playerAvg, float opponentAvg)
        {
            if (_averagesScoreboardText)
            {
                _averagesScoreboardText.text = $"AVG TIME — You: {playerAvg:F2}s | Rival: {opponentAvg:F2}s";
            }
        }
    }
}
