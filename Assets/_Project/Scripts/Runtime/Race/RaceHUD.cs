using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BarrelRacing.Runtime.Race
{
    [DisallowMultipleComponent]
    public sealed class RaceHUD : MonoBehaviour
    {
        [Header("Timing & Score")]
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private TextMeshProUGUI _splitDeltaText;
        [SerializeField] private TextMeshProUGUI _playerScoreText;
        [SerializeField] private TextMeshProUGUI _opponentScoreText;

        [Header("Drift & Whip")]
        [SerializeField] private Image _driftGaugeFill;
        [SerializeField] private Image[] _whipChargePips;
        [SerializeField] private TextMeshProUGUI _rhythmFeedbackText;
        [SerializeField] private CanvasGroup _rhythmFeedbackCanvasGroup;

        [Header("Combo & Speed")]
        [SerializeField] private GameObject _comboRootObject;
        [SerializeField] private TextMeshProUGUI _comboCountText;
        [SerializeField] private TextMeshProUGUI _speedMpsText;
        [SerializeField] private TextMeshProUGUI _countdownText;
        [SerializeField] private CanvasGroup _countdownCanvasGroup;
        [SerializeField] private GameObject _barrelKnockAlert;
        [SerializeField] private GameObject _adrenalineBanner;

        private float _rhythmFadeTimer = 0f;

        private void Update()
        {
            if (_rhythmFadeTimer > 0f)
            {
                _rhythmFadeTimer -= Time.deltaTime;
                if (_rhythmFeedbackCanvasGroup != null) _rhythmFeedbackCanvasGroup.alpha = Mathf.Clamp01(_rhythmFadeTimer / 0.35f);
            }
        }

        public void UpdateRaceTimer(float elapsed) { if (_timerText != null) _timerText.text = $"{elapsed:F2}s"; }
        public void UpdateSplitDelta(float delta) { if (_splitDeltaText != null) _splitDeltaText.text = delta >= 0 ? $"+{delta:F2}s" : $"{delta:F2}s"; }
        public void UpdateMatchInfo(int run, int maxRuns, int p1, int p2) { if (_playerScoreText != null) _playerScoreText.text = $"{p1}"; if (_opponentScoreText != null) _opponentScoreText.text = $"{p2}"; }
        public void UpdateDriftProgress(float progress, DriftTier tier) { if (_driftGaugeFill != null) _driftGaugeFill.fillAmount = progress; }
        public void UpdateWhipCharges(int count, float cdNorm) { if (_whipChargePips != null) for (int i = 0; i < _whipChargePips.Length; i++) if (_whipChargePips[i] != null) _whipChargePips[i].enabled = i < count; }
        public void ShowRhythmFeedback(WhipTimingGrade g) { if (_rhythmFeedbackText != null) { _rhythmFeedbackText.text = g.ToString().ToUpper(); _rhythmFadeTimer = 0.85f; if (_rhythmFeedbackCanvasGroup != null) _rhythmFeedbackCanvasGroup.alpha = 1f; } }
        public void UpdateComboStreak(int count, float mult, float decay) { if (_comboRootObject != null) _comboRootObject.SetActive(count > 0); if (_comboCountText != null) _comboCountText.text = $"{count}x ({mult:F1}x)"; }
        public void UpdateSpeedometer(float speed, float max) { if (_speedMpsText != null) _speedMpsText.text = $"{Mathf.RoundToInt(speed * 2.23694f)} MPH"; }
        public void ShowCountdown(int c) { if (_countdownCanvasGroup != null) _countdownCanvasGroup.alpha = 1f; if (_countdownText != null) _countdownText.text = c > 0 ? $"{c}" : "GO!"; }
        public void HideCountdown() { if (_countdownCanvasGroup != null) _countdownCanvasGroup.alpha = 0f; }
        public void ShowBarrelKnockPenalty(int pts) { if (_barrelKnockAlert != null) _barrelKnockAlert.SetActive(true); }
        public void SetAdrenalineSurgeActive(bool a) { if (_adrenalineBanner != null) _adrenalineBanner.SetActive(a); }
    }
}
