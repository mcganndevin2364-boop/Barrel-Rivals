using UnityEditor;
namespace BarrelRacing.Editor
{
    public static class TestSceneGenerator
    {
        [MenuItem("Barrel Rivals/Setup Foundation Scene")]
        public static void GenerateScene()
        {
            if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                BarrelRivals.Editor.FoundationBuilder.Generate();
        }
    }
}
