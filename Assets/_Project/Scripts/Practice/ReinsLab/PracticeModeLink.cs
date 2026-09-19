using UnityEngine;
using UnityEngine.SceneManagement;

namespace BarrelRivals.Practice
{
    public sealed class PracticeModeLink : MonoBehaviour
    {
        public void OpenReinsLab() => SceneManager.LoadScene(ReinsLabController.SceneName);
    }
}
