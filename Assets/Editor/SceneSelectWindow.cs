using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;
using System.Linq;

public class SceneSelectWindow : EditorWindow
{
    private string searchFolder = "Assets/Scenes"; // 検索するフォルダ
    private string searchKeyword = "";           // 検索キーワード
    private Vector2 scrollPos;                   // スクロール位置
    private List<SceneInfo> sceneInfoList = new List<SceneInfo>();

    private string pendingAddPath = null;
    private string pendingRemovePath = null;

    // シーンの情報を保持するクラス
    private class SceneInfo
    {
        public string path;
        public string name;
        public bool isBuildTarget; // Build Settingsに入っているか
        public int buildIndex;     // Build Settingsでのインデックス (-1なら未登録)
    }

    [MenuItem("CustomEditor/Scene")]
    public static void ShowWindow()
    {
        GetWindow<SceneSelectWindow>("Scene Select");
    }

    private void OnEnable()
    {
        RefreshSceneList();
    }

    // シーン情報を再取得・更新する
    private void RefreshSceneList()
    {
        sceneInfoList.Clear();

        if (!Directory.Exists(searchFolder)) return;

        // Build Settingsに登録されているシーンを取得
        var buildScenes = EditorBuildSettings.scenes;

        // 指定フォルダ内の全シーンを検索
        string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { searchFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string sceneName = Path.GetFileNameWithoutExtension(path);

            // 検索キーワードフィルター
            if (!string.IsNullOrEmpty(searchKeyword) && !sceneName.ToLower().Contains(searchKeyword.ToLower()))
            {
                continue;
            }

            // Build Settingsに入っているかチェック
            int buildIndex = -1;
            for (int i = 0; i < buildScenes.Length; i++)
            {
                if (buildScenes[i].path == path)
                {
                    buildIndex = i;
                    break;
                }
            }

            sceneInfoList.Add(new SceneInfo
            {
                path = path,
                name = sceneName,
                isBuildTarget = (buildIndex != -1),
                buildIndex = buildIndex
            });
        }
    }

    private void OnGUI()
    {
        if (!string.IsNullOrEmpty(pendingAddPath))
        {
            AddSceneToBuildSettings(pendingAddPath);
            pendingAddPath = null;
            RefreshSceneList();
        }
        if (!string.IsNullOrEmpty(pendingRemovePath))
        {
            RemoveSceneFromBuildSettings(pendingRemovePath);
            pendingRemovePath = null;
            RefreshSceneList();
        }

        EditorGUILayout.Space(5);

        // フォルダ選択・検索エリア
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("シーンフォルダ", GUILayout.Width(85));
        searchFolder = EditorGUILayout.TextField(searchFolder);
        if (GUILayout.Button("選択", GUILayout.Width(60)))
        {
            string selected = EditorUtility.OpenFolderPanel("Select Scene Folder", "Assets", "");
            if (!string.IsNullOrEmpty(selected))
            {
                if (selected.StartsWith(Application.dataPath))
                {
                    searchFolder = "Assets" + selected.Substring(Application.dataPath.Length);
                    RefreshSceneList();
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("検索", GUILayout.Width(85));
        string newKeyword = EditorGUILayout.TextField(searchKeyword);
        if (newKeyword != searchKeyword)
        {
            searchKeyword = newKeyword;
            RefreshSceneList();
        }
        if (GUILayout.Button("クリア", GUILayout.Width(60)))
        {
            searchKeyword = "";
            RefreshSceneList();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // リロードボタン
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("シーンを更新"))
        {
            RefreshSceneList();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // シーン一覧表示エリア
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        if (sceneInfoList.Count == 0)
        {
            EditorGUILayout.HelpBox($"指定されたフォルダ ({searchFolder}) にシーンが見つかりませんでした。", MessageType.Info);
        }

        foreach (var scene in sceneInfoList)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            // Build Settingsに入っているなら緑色のバーを表示
            Rect barRect = EditorGUILayout.GetControlRect(GUILayout.Width(6), GUILayout.Height(35));
            if (scene.isBuildTarget)
            {
                EditorGUI.DrawRect(barRect, Color.green);
            }
            else
            {
                EditorGUI.DrawRect(barRect, new Color(0.3f, 0.3f, 0.3f));
            }

            // Unity公式のシーンアイコンを取得して表示
            GUIContent icon = EditorGUIUtility.IconContent("SceneAsset Icon");
            if (icon != null)
            {
                GUILayout.Label(icon, GUILayout.Width(35), GUILayout.Height(35));
            }

            // インデックスとシーン名
            string titleText = scene.isBuildTarget ? $"#{scene.buildIndex}   {scene.name}" : $"[未登録]   {scene.name}";
            EditorGUILayout.LabelField(titleText, EditorStyles.boldLabel, GUILayout.Height(35));

            // Add / Remove ボタン（直接実行せず、変数にパスを代入する形に修正）
            if (scene.isBuildTarget)
            {
                GUI.enabled = false;
                GUILayout.Button("追加", GUILayout.Width(55), GUILayout.Height(18));
                GUI.enabled = true;

                if (GUILayout.Button("除外", GUILayout.Width(65), GUILayout.Height(18)))
                {
                    pendingRemovePath = scene.path; // ループ外で処理するために予約
                }
            }
            else
            {
                if (GUILayout.Button("追加", GUILayout.Width(55), GUILayout.Height(18)))
                {
                    pendingAddPath = scene.path; // ループ外で処理するために予約
                }
                GUI.enabled = false;
                GUILayout.Button("除外", GUILayout.Width(65), GUILayout.Height(18));
                GUI.enabled = true;
            }

            EditorGUILayout.EndHorizontal();

            // Open ボタン
            if (GUILayout.Button("開く", GUILayout.Height(22)))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(scene.path);
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        EditorGUILayout.EndScrollView();

        // 最下段の共通ボタン
        EditorGUILayout.Space(5);
        if (GUILayout.Button("ビルド設定を開く", GUILayout.Height(30)))
        {
            var buildWindowType = System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor");
            if (buildWindowType != null)
            {
                EditorWindow.GetWindow(buildWindowType);
            }
        }
    }

    private void AddSceneToBuildSettings(string path)
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Any(s => s.path == path))
        {
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }

    private void RemoveSceneFromBuildSettings(string path)
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        scenes.RemoveAll(s => s.path == path);
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}