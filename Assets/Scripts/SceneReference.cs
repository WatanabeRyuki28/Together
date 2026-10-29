using UnityEngine;

[CreateAssetMenu(fileName = "New SceneReference", menuName = "Scenes/SceneReference")]
public class SceneReference : ScriptableObject
{
    [SerializeField, HideInInspector] private string sceneName;
#if UNITY_EDITOR
    [SerializeField] private UnityEditor.SceneAsset sceneAsset;

    private void OnValidate()
    {
        if (sceneAsset != null)
            sceneName = sceneAsset.name;
    }
#endif

    public string SceneName => sceneName;
}