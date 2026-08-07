#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class EditorUtils : MonoBehaviour
{
    #region Fast Open Scenes

    [MenuItem("Editor Utils/Open Scene/Root &1")]
    public static void OpenSceneRoot()
    {
        OpenScene(StaticValue.SCENE_ROOT);
    }

    [MenuItem("Editor Utils/Open Scene/Login &2")]
    public static void OpenSceneLogin()
    {
        OpenScene(StaticValue.SCENE_LOGIN);
    }

    [MenuItem("Editor Utils/Open Scene/MainGame &3")]
    public static void OpenSceneMainGame()
    {
        OpenScene(StaticValue.SCENE_MAINGAME);
    }

    private static void OpenScene(string sceneName)
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene("Assets/_Assets/Scenes/" + sceneName + ".unity");
        }
    }

    #endregion
}
#endif
