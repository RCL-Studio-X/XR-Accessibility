using UnityEngine;
using UnityEngine.SceneManagement;

public static class TransitionHelper
{
    public static string TargetScene { get; private set; }

    public static void LoadSceneWithTransition(string sceneName)
    {
        TargetScene = sceneName;
        SceneManager.LoadScene("TransitionScene");
    }
}