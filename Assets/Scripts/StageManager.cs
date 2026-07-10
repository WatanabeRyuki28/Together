using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StageManager : MonoBehaviour
{
    [SerializeField] private RectTransform[] stageButtons;

    [Header("ホストの選択枠（カーソル画像や太枠）")]
    [SerializeField] private RectTransform selectionCursor;

    public GameObject confirmButton; // ホストの画面にだけ出る「開始」ボタン

    [Header("アイテム表示用の設定")]
    // ★ Image[] から SpriteRenderer[] に修正
    [SerializeField] private SpriteRenderer[] itemIcons;

    // ★追加：星の画像をインスペクターから登録できるようにする
    [SerializeField] private Sprite obtainedStarSprite; // 獲得済みの星（黄色の星など）
    [SerializeField] private Sprite missingStarSprite;  // 未獲得の星（白い星など）

    // ★修正：ヒエラルキーのオブジェクト名に合わせて「StarItem」に変更
    private const string StarObjectName = "StarItem";

    // マジックナンバーを避けるため、アイテムIDのベース名を定義
    private const string ItemIdPrefix = "Stage_";

    private int currentStageIndex = 0; // 現在選んでいるステージ番号

    [SerializeField] private GameObject gestPanel;

    private CommunicationUI controls;

    void Awake()
    {
        // インスタンスの生成
        controls = new CommunicationUI();
    }

    void OnEnable()
    {
        // ステージ選択シーン用の操作マップ「StageSelect」を有効化
        if (controls != null)
        {
            controls.StageSelect.Enable();
        }
    }

    void OnDisable()
    {
        // シーンを抜ける時は安全のために操作をオフにする
        if (controls != null)
        {
            controls.StageSelect.Disable();
        }
    }

    void Start()
    {
        CheckHost();

        // 起動時は開始ボタンを非表示にする
        if (confirmButton != null)
        {
            confirmButton.SetActive(false);
        }

        // 初期カーソル位置の更新
        UpdateCursorPosition();

        // ★セーブファイルから前回の確定セーブ状態をロードし直し、同期をリセットする
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.LoadGame();
        }

        // 各ステージのアイテム獲得状況をロードしてUIに反映する
        UpdateItemIconsDisplay();

        // ホストなら初期位置をゲストに共有
        if (IsHost())
        {
            SendStageSelectNotification(currentStageIndex, false);
        }
    }

    void Update()
    {
        // ホスト以外はキーボード操作を受け付けない！
        if (!IsHost()) return;

        bool isMoved = false;

        // グリッド状（2行×5列）のキーボード移動処理
        // 横移動（右）
        if (controls.StageSelect.Right.triggered)
        {
            currentStageIndex++;
            if (currentStageIndex >= stageButtons.Length) currentStageIndex = 0;
            isMoved = true;
        }
        // 横移動（左）
        else if (controls.StageSelect.Left.triggered)
        {
            currentStageIndex--;
            if (currentStageIndex < 0) currentStageIndex = stageButtons.Length - 1;
            isMoved = true;
        }
        // 縦移動（下）
        else if (controls.StageSelect.Down.triggered)
        {
            if (currentStageIndex < 5) // 上の段（0〜4）にいるとき
            {
                currentStageIndex += 5;
                if (currentStageIndex >= stageButtons.Length) currentStageIndex = stageButtons.Length - 1;
                isMoved = true;
            }
        }
        // 縦移動（上）
        else if (controls.StageSelect.Up.triggered)
        {
            if (currentStageIndex >= 5) // 下の段（5〜9）にいるとき
            {
                currentStageIndex -= 5;
                isMoved = true;
            }
        }

        // カーソルが動いたら、位置を更新してゲストにも即座にパケットを送信する
        if (isMoved)
        {
            UpdateCursorPosition();

            // ホストの画面にも「開始」ボタンを出してあげる
            if (confirmButton != null) confirmButton.SetActive(true);

            // ゲストへ同期送信
            SendStageSelectNotification(currentStageIndex, false);
        }

        // 決定ボタンで本決定（Aボタン、またはEnter/Space/Zキーなど）
        if (controls.StageSelect.Submit.triggered)
        {
            ConfirmStageSelection();
        }
    }

    // 自画面のカーソルの位置を更新する
    void UpdateCursorPosition()
    {
        if (stageButtons == null || stageButtons.Length == 0 || selectionCursor == null) return;

        if (currentStageIndex >= 0 && currentStageIndex < stageButtons.Length)
        {
            selectionCursor.gameObject.SetActive(true);
            selectionCursor.position = stageButtons[currentStageIndex].position;
        }
    }

    // ★修正：SpriteRendererのSpriteを切り替えるように処理を最適化
    private void UpdateItemIconsDisplay()
    {
        if (stageButtons == null || SaveManager.Instance == null) return;

        // ボタンの数に合わせて配列を自動で用意する
        itemIcons = new SpriteRenderer[stageButtons.Length];

        for (int i = 0; i < stageButtons.Length; i++)
        {
            if (stageButtons[i] == null) continue;

            // まずボタンの直下から探す
            Transform starTransform = stageButtons[i].Find(StarObjectName);

            // 階層が奥深い場合は再帰探索で探す
            if (starTransform == null)
            {
                starTransform = FindChildRecursive(stageButtons[i], StarObjectName);
            }

            // ★ TryGetComponent<SpriteRenderer> で星のスプライトを取得
            if (starTransform != null && starTransform.TryGetComponent<SpriteRenderer>(out SpriteRenderer starRenderer))
            {
                itemIcons[i] = starRenderer;

                // 各ステージ固有のアイテムIDを生成（例: "Stage_0", "Stage_1"...）
                string targetItemId = ItemIdPrefix + i;

                // セーブデータに入っているかチェック
                if (SaveManager.Instance.HasItem(targetItemId))
                {
                    // 取得済み：黄色の星の画像に切り替える
                    if (obtainedStarSprite != null)
                    {
                        starRenderer.sprite = obtainedStarSprite;
                    }
                    starRenderer.color = Color.white; // カラーを通常に戻す
                }
                else
                {
                    // 未取得：白い星の画像に切り替える
                    if (missingStarSprite != null)
                    {
                        starRenderer.sprite = missingStarSprite;
                        starRenderer.color = Color.white;
                    }
                    else
                    {
                        // もしインスペクターに「白い星」が未登録なら、暫定処置として半透明のグレーにする
                        starRenderer.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
                    }
                }
            }
            else
            {
                Debug.LogWarning($"{stageButtons[i].name} の中に名前が '{StarObjectName}' の SpriteRenderer コンポーネントが見つかりません。");
            }
        }
    }

    // ★追加：ボタンの奥深い子階層から名前でオブジェクトを検索するヘルパー関数
    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindChildRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }

    // 決定ボタンが押された（または決定キーが叩かれた）時の本決定処理
    public void ConfirmStageSelection()
    {
        if (!IsHost()) return;
        if (currentStageIndex == -1) return;

        Debug.Log($"【ホスト】ステージ {currentStageIndex + 1} で本決定しました！ゲームを開始します。");

        // 相手に「本決定（stage_ready = true）」として通知を送る
        SendStageSelectNotification(currentStageIndex, true);

        // ホスト自身の画面を遷移させる
        LoadTargetScene(currentStageIndex);
    }

    // サーバーへの送信処理（キャラ選択のロジックと完全に統一）
    private async void SendStageSelectNotification(int stageIndex, bool isReady)
    {
        if (NetworkManager.Instance == null) return;

        StageSelectData msgData = new StageSelectData();
        msgData.type = "stage_select";
        msgData.name_id = NetworkManager.Instance.myPlayerId;
        msgData.room_id = NetworkManager.Instance.myRoomID;
        msgData.index = NetworkManager.Instance.myPlayerIndex;
        msgData.IsStarted = isReady;

        msgData.stage_index = stageIndex;
        msgData.stage_ready = isReady;

        string jsonMsg = JsonUtility.ToJson(msgData);
        await NetworkManager.Instance.SendMessageAsync(jsonMsg);
    }

    // ゲスト側のメッセージ受信処理
    public void HandleRemoteStageMessage(string msg)
    {
        var stageData = JsonUtility.FromJson<StageSelectData>(msg);
        if (stageData == null) return;

        if (stageData.type == "stage_select")
        {
            // ホストからのデータである場合のみ処理する
            if (stageData.name_id != NetworkManager.Instance.myPlayerId)
            {
                // ホストが選択中の場合、ゲスト側のカーソル位置をリアルタイム同期
                if (!stageData.stage_ready)
                {
                    Debug.Log($"【同期】ホストがステージ {stageData.stage_index + 1} を選択中...");

                    currentStageIndex = stageData.stage_index;
                    UpdateCursorPosition(); // ゲスト画面のカーソルをホストと同じ位置に動かす
                }
                // ホストが本決定のパケットを送ってきた場合、ゲストも道連れでシーン遷移
                else
                {
                    Debug.Log($"【同期】ホストがステージ {stageData.stage_index + 1} で確定しました。遷移します。");
                    LoadTargetScene(stageData.stage_index);
                }
            }
        }
    }

    // シーン遷移用の関数
    private void LoadTargetScene(int stageIndex)
    {


        if (stageIndex == -1) SceneManager.LoadScene("TutorialStageScene_Backup");
        else if (stageIndex == 0) SceneManager.LoadScene("Stage1");
        else if (stageIndex == 1) SceneManager.LoadScene("Stage2");
        else if (stageIndex == 2) SceneManager.LoadScene("Stage3");


        if (controls != null)
        {
            controls.StageSelect.Disable();
        }

            if (stageIndex == -1) SceneManager.LoadScene("TutorialStageScene_Backup"); 
        else if (stageIndex == 0) SceneManager.LoadScene("Stage1");               
        else if (stageIndex == 1) SceneManager.LoadScene("Stage2");               
        else if (stageIndex == 2) SceneManager.LoadScene("Stage3");              
       


        if (stageIndex == -1) SceneManager.LoadScene("TutorialStageScene_Backup");

        else if (stageIndex == 0) SceneManager.LoadScene("Stage1");
        else if (stageIndex == 1) SceneManager.LoadScene("Stage2");
        else if (stageIndex == 2) SceneManager.LoadScene("Stage3");

    }
  


    private bool IsHost() => NetworkManager.Instance != null && NetworkManager.Instance.myPlayerIndex == 0;

    private void CheckHost()
    {
        if (NetworkManager.Instance == null) return;

        if (IsHost())
        {
            if (gestPanel != null) gestPanel.SetActive(false);
            Debug.Log("あなたはホストです。キーボードでステージ選択が可能です。");
        }
        else
        {
            if (gestPanel != null) gestPanel.SetActive(true);
            Debug.Log("あなたはゲストです。ホストのカーソル同期を待機しています。");
        }
    }
}