using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class SceneAutoOpen
{
    static SceneAutoOpen()
    {
        EditorApplication.delayCall += OpenDefaultSceneIfNoneLoaded;
    }

    private static void OpenDefaultSceneIfNoneLoaded()
    {
        if (!string.IsNullOrEmpty(EditorSceneManager.GetActiveScene().path)) return;
        if (EditorBuildSettings.scenes.Length == 0) return;

        EditorSceneManager.OpenScene(EditorBuildSettings.scenes[0].path);
    }
}
