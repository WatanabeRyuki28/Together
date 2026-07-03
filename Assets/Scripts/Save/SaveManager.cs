using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    // 現在のセーブデータ構造のインスタンス
    public SaveData CurrentSaveData { get; private set; } = new SaveData();

    private string filePath;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // シーン遷移しても壊さない

            // 保存先パスを設定
            filePath = Path.Combine(Application.persistentDataPath, "save.json");

            // ゲーム起動時に自動ロード
            LoadGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ゲーム状態をJSONファイルに保存
    public void SaveGame()
    {
        string json = JsonUtility.ToJson(CurrentSaveData, true);
        File.WriteAllText(filePath, json);
        Debug.Log($"ゲームデータを保存しました: {filePath}");
    }

    // JSONファイルからゲーム状態を読み込み
    public void LoadGame()
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            CurrentSaveData = JsonUtility.FromJson<SaveData>(json);
            Debug.Log("ゲームデータをロードしました。");
        }
        else
        {
            CurrentSaveData = new SaveData();
            Debug.Log("セーブファイルが見つからないため、新規データを作成しました。");
        }
    }

    // 特定のアイテムを持っているかチェックする便利な関数
    public bool HasItem(string itemId)
    {
        return CurrentSaveData.obtainedItemIds.Contains(itemId);
    }
}