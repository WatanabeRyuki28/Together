using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    // 現在のセーブデータ構造のインスタンス
    public SaveData CurrentSaveData { get; private set; } = new SaveData();

    private string filePath;


    [SerializeField] private int stageClearCount = 0;

    
    public int StageClearCount
    {
        get { return stageClearCount; }
    }

   
    public void SetStageClearCount(int count)
    {
        stageClearCount = count;
        SaveGame();
    }

    void Start()
    {
        ResetSaveData();
    }
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
        // リストがヌルだった場合の安全対策を含める
        if (CurrentSaveData == null || CurrentSaveData.obtainedItemIds == null) return false;
        return CurrentSaveData.obtainedItemIds.Contains(itemId);
    }

    // -------------------------------------------------------------
    // ★追加：アイテムIDをセーブデータに追加する関数
    // -------------------------------------------------------------
    public void AddItem(string itemId)
    {
        if (CurrentSaveData == null || CurrentSaveData.obtainedItemIds == null) return;

        // 二重で追加されないようにチェックしてから保存する
        if (!CurrentSaveData.obtainedItemIds.Contains(itemId))
        {
            CurrentSaveData.obtainedItemIds.Add(itemId);
            Debug.Log($"セーブデータにアイテムを追加しました: {itemId}");
        }
    }

    public void ResetSaveData()
    {
        // データを消す
        CurrentSaveData = new SaveData();

        // もし保存されたファイルが存在するなら削除する
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log($"セーブファイルを削除し、初期化しました: {filePath}");
        }
        else
        {
            Debug.Log("セーブファイルが存在しないため、メモリ内のみ初期化しました。");
        }

    }

}