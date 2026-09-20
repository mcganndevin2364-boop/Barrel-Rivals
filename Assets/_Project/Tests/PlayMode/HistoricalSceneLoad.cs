using UnityEngine;
using UnityEngine.SceneManagement;

namespace BarrelRivals.Tests
{
    internal static class HistoricalSceneLoad
    {
        // Historical scenes remain testable without entering the Reins mobile scene list.
        public static AsyncOperation Load(string name)
        {
#if UNITY_EDITOR
            string folder=name=="Arena_Foundation"?"Foundation":"Practice";
            return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/_Project/Generated/"+folder+"/"+name+".unity",new LoadSceneParameters(LoadSceneMode.Single));
#else
            return SceneManager.LoadSceneAsync(name,LoadSceneMode.Single);
#endif
        }
    }
}
