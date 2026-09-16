using System;
using System.Collections;
using UnityEngine;

namespace BarrelRacing.Runtime.Race
{
    public sealed class ScoreRevealDirector : MonoBehaviour
    {
        public IEnumerator PlayScoreRevealRoutine(RunScoringSystem.RunResult result, Action onFinished)
        {
            yield return new WaitForSeconds(0.4f);
            // Scratch-ticket reveal sequence
            yield return new WaitForSeconds(0.8f);
            onFinished?.Invoke();
        }
    }
}
