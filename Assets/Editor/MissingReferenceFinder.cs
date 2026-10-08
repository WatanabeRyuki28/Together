using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MissingReferenceFinder : EditorWindow
{
    private Vector2 scrollPos;
    private List<MissingInfo> missingList = new List<MissingInfo>();
    private bool searchInScene = true;
    private bool searchInPrefabs = true;

    private class MissingInfo
    {
        public string targetName;   // オブジェクト名やアセット名
        public string location;     // シーン名 or アセットパス
        public string description;  // どこが切れているか
        public Object targetObject; // クリックしたときに選択するため
    }

    [MenuItem("CustomEditor/Missing Reference Finder")]
    public static void ShowWindow()
    {
        GetWindow<MissingReferenceFinder>("Missing Finder");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox("シーン内やプレハブの参照切れ（Missing）を検出します。", MessageType.Info);
        EditorGUILayout.Space(5);

        // 検索対象の選択
        searchInScene = EditorGUILayout.Toggle("現在のシーンを検索", searchInScene);
        searchInPrefabs = EditorGUILayout.Toggle("プロジェクト内のプレハブを検索", searchInPrefabs);

        EditorGUILayout.Space(5);

        if (GUILayout.Button("検索開始", GUILayout.Height(35)))
        {
            FindMissingReferences();
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField($"検出結果: {missingList.Count} 件", EditorStyles.boldLabel);

        // 一覧表示エリア
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        foreach (var item in missingList)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(item.targetName, EditorStyles.boldLabel, GUILayout.Width(150));
            EditorGUILayout.LabelField($"場所: {item.location}", GUILayout.MaxWidth(250));

            // オブジェクトを選択するボタン
            if (GUILayout.Button("選択", GUILayout.Width(60), GUILayout.Height(20)))
            {
                if (item.targetObject != null)
                {
                    Selection.activeObject = item.targetObject;
                    EditorGUIUtility.PingObject(item.targetObject);
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField($"詳細: {item.description}", EditorStyles.wordWrappedLabel);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        EditorGUILayout.EndScrollView();
    }

    private void FindMissingReferences()
    {
        missingList.Clear();

        //  現在のシーン内を検索
        if (searchInScene)
        {
            Scene currentScene = SceneManager.GetActiveScene();
            GameObject[] rootObjects = currentScene.GetRootGameObjects();

            foreach (var root in rootObjects)
            {
                // 子要素も含めてすべてのコンポーネントを取得
                Component[] components = root.GetComponentsInChildren<Component>(true);
                foreach (var comp in components)
                {
                    if (comp == null)
                    {
                        // コンポーネント自体がMissing（スクリプトが消えているなど）
                        missingList.Add(new MissingInfo
                        {
                            targetName = "Missing Component",
                            location = currentScene.name,
                            description = "GameObjectにアタッチされているスクリプトが消失しています。",
                            targetObject = root
                        });
                        continue;
                    }

                    // シリアル化されたプロパティを走査して参照切れがないかチェック
                    SerializedObject so = new SerializedObject(comp);
                    SerializedProperty sp = so.GetIterator();

                    while (sp.NextVisible(true))
                    {
                        if (sp.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            if (sp.objectReferenceValue == null && sp.objectReferenceInstanceIDValue != 0)
                            {
                                // 参照すべきアセット/オブジェクトがあるのに見つからない（Missing）状態
                                missingList.Add(new MissingInfo
                                {
                                    targetName = comp.gameObject.name,
                                    location = $"{currentScene.name} > {comp.GetType().Name}",
                                    description = $"変数 '{sp.name}' の参照が切れています (Missing)",
                                    targetObject = comp.gameObject
                                });
                            }
                        }
                    }
                }
            }
        }

        // プロジェクト内のプレハブを検索
        if (searchInPrefabs)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                Component[] components = prefab.GetComponentsInChildren<Component>(true);
                foreach (var comp in components)
                {
                    if (comp == null)
                    {
                        missingList.Add(new MissingInfo
                        {
                            targetName = "Missing Component",
                            location = path,
                            description = "プレハブ内のスクリプトが消失しています。",
                            targetObject = prefab
                        });
                        continue;
                    }

                    SerializedObject so = new SerializedObject(comp);
                    SerializedProperty sp = so.GetIterator();

                    while (sp.NextVisible(true))
                    {
                        if (sp.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            if (sp.objectReferenceValue == null && sp.objectReferenceInstanceIDValue != 0)
                            {
                                missingList.Add(new MissingInfo
                                {
                                    targetName = comp.gameObject.name,
                                    location = $"{path} > {comp.GetType().Name}",
                                    description = $"変数 '{sp.name}' の参照が切れています (Missing)",
                                    targetObject = prefab
                                });
                            }
                        }
                    }
                }
            }
        }
    }
}