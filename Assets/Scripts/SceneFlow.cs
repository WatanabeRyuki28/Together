using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New SceneFlow", menuName = "Scenes/SceneFlow", order = 1)]
public class SceneFlow : ScriptableObject
{
    [SerializeField]
    private List<SceneReference> scenes = new();

    public IReadOnlyList<SceneReference> Scenes => scenes;

    public SceneReference GetNextScene(SceneReference currentScene)
    {
        int index = scenes.IndexOf(currentScene);
        if (index >= 0 && index < scenes.Count - 1)
        {
            return scenes[index + 1];
        }
        return null;
    }

    public SceneReference GetPreviousScene(SceneReference currentScene)
    {
        int index = scenes.IndexOf(currentScene);
        if (index > 0)
        {
            return scenes[index - 1];
        }
        return null;
    }

    public SceneReference GetSceneAt(int index)
    {
        if (index >= 0 && index < scenes.Count)
        {
            return scenes[index];
        }
        return null;
    }
}