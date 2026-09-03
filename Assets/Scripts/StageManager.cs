using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StageManager : MonoBehaviour
{
    [SerializeField] private RectTransform[] stageButtons;
    [SerializeField] private RectTransform homeButton;

    [Header("ホストの選択枠（カーソル画像や太枠）")]
    [SerializeField] private RectTransform selectionCursor;

    [Header("アイテム表示用の設定")]
    [SerializeField] private SpriteRenderer[] itemIcons;

    [SerializeField] private Sprite obtainedStarSprite;
    [SerializeField] private Sprite missingStarSprite;

    private const string StarObjectName = "StarItem";
    private const string ItemIdPrefix = "Stage_";

    private int currentStageIndex = 0;     // 0〜9: ステージ1〜10
    private bool isHomeSelected = false;    // true: ホームへもどるを選択中
    private int maxUnlockedStageIndex = 0; // 解放されている最大ステージ（0 = Stage1のみ）

    [SerializeField] private GameObject[] offPanel;
    [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    [SerializeField] private GameObject gestPanel;

    private CommunicationUI controls;

    void Awake()
    {
        controls = new CommunicationUI();
    }

    void OnEnable()
    {
        if (controls != null) controls.StageSelect.Enable();
    }

    void OnDisable()
    {
        if (controls != null) controls.StageSelect.Disable();
    }

    void Start()
    {
        CheckHost();

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.LoadGame();
        }

        int clearedCount = GetClearedStageCountFromSave();
        maxUnlockedStageIndex = Mathf.Clamp(clearedCount, 0, stageButtons.Length - 1);

        currentStageIndex = 0; // 初期位置は Stage 1
        isHomeSelected = false;

        UpdateCursorPosition();
        UpdateItemIconsDisplay();

        if (IsHost())
        {
            SendStageSelectNotification(currentStageIndex, isHomeSelected, false);
        }
    }

    void Update()
    {
        if (!IsHost()) return;

        bool isMoved = false;

        if (isHomeSelected)
        {
            // 下キーでステージ1（または上段）へ戻る
            if (controls.StageSelect.Down.triggered)
            {
                isHomeSelected = false;
                currentStageIndex = 0; // Stage 1へ
                isMoved = true;
            }
        }
        else
        {
            // 横移動（右）
            if (controls.StageSelect.Right.triggered)
            {
                int nextIndex = currentStageIndex + 1;
                if (nextIndex >= stageButtons.Length) nextIndex = 0;

                if (IsStageUnlocked(nextIndex))
                {
                    currentStageIndex = nextIndex;
                    isMoved = true;
                }
            }
            // 横移動（左）
            else if (controls.StageSelect.Left.triggered)
            {
                int nextIndex = currentStageIndex - 1;
                if (nextIndex < 0) nextIndex = stageButtons.Length - 1;

                while (!IsStageUnlocked(nextIndex) && nextIndex > 0)
                {
                    nextIndex--;
                }

                currentStageIndex = nextIndex;
                isMoved = true;
            }
            // 縦移動（上）
            else if (controls.StageSelect.Up.triggered)
            {
                if (currentStageIndex < 5) // 上段（Stage 1〜5）にいる時
                {
                    isHomeSelected = true;
                    isMoved = true;
                }
                else // 下段（Stage 6〜10）にいる時
                {
                    int nextIndex = currentStageIndex - 5;
                    if (IsStageUnlocked(nextIndex))
                    {
                        currentStageIndex = nextIndex;
                        isMoved = true;
                    }
                }
            }
            // 縦移動（下）
            else if (controls.StageSelect.Down.triggered)
            {
                if (currentStageIndex < 5)
                {
                    int nextIndex = currentStageIndex + 5;
                    if (nextIndex < stageButtons.Length && IsStageUnlocked(nextIndex))
                    {
                        currentStageIndex = nextIndex;
                        isMoved = true;
                    }
                }
            }
        }

        if (isMoved)
        {
            UpdateCursorPosition();
            SendStageSelectNotification(currentStageIndex, isHomeSelected, false);
        }

        if (controls.StageSelect.Submit.triggered)
        {
            ConfirmStageSelection();
        }
    }

    private bool IsStageUnlocked(int index)
    {
        return index <= maxUnlockedStageIndex;
    }

    private int GetClearedStageCountFromSave()
    {
        if (SaveManager.Instance != null)
        {
            // SaveManagerのStageClearCountプロパティから値を取得する
            return SaveManager.Instance.StageClearCount;
        }
        return 0;

    }

    public static void ClearStage(int clearedStageIndex)
    {
        if (SaveManager.Instance == null) return;

        // 現在のクリア数より大きいステージをクリアした場合のみセーブデータを更新
        // （例: Stage1クリア（index=0）の時、クリア数が0なら1にカウントアップ）
        int nextClearCount = clearedStageIndex + 1;

        if (SaveManager.Instance.StageClearCount < nextClearCount)
        {
            SaveManager.Instance.SetStageClearCount(nextClearCount);
            Debug.Log($"ステージ {clearedStageIndex + 1} をクリア！ 解放数を {nextClearCount} に更新しました。");
        }
    }

    void UpdateCursorPosition()
    {
        if (selectionCursor == null) return;

        selectionCursor.gameObject.SetActive(true);

        if (isHomeSelected)
        {
            selectionCursor.sizeDelta = new Vector2(320f, 125f);
            if (homeButton != null) selectionCursor.position = homeButton.position;
        }
        else
        {
            if (stageButtons != null && currentStageIndex >= 0 && currentStageIndex < stageButtons.Length)
            {
                selectionCursor.sizeDelta = new Vector2(280f, 340f);
                selectionCursor.position = stageButtons[currentStageIndex].position;
            }
        }
    }

    private void UpdateItemIconsDisplay()
    {
        if (stageButtons == null) return;

        itemIcons = new SpriteRenderer[stageButtons.Length];

        for (int i = 0; i < stageButtons.Length; i++)
        {
            if (stageButtons[i] == null) continue;

            bool isUnlocked = IsStageUnlocked(i);

            if (offPanel != null && i < offPanel.Length && offPanel[i] != null)
            {
                offPanel[i].SetActive(!isUnlocked);
            }

            Transform starTransform = stageButtons[i].Find(StarObjectName) ?? FindChildRecursive(stageButtons[i], StarObjectName);

            if (starTransform != null && starTransform.TryGetComponent<SpriteRenderer>(out SpriteRenderer starRenderer))
            {
                itemIcons[i] = starRenderer;
                starRenderer.enabled = true; 

                string targetItemId = ItemIdPrefix + i;

            
                if (!isUnlocked)
                {
                    if (missingStarSprite != null) starRenderer.sprite = missingStarSprite;
                }
                else if (SaveManager.Instance != null && SaveManager.Instance.HasItem(targetItemId))
                {
                    if (obtainedStarSprite != null) starRenderer.sprite = obtainedStarSprite;
                }
                else
                {
                    if (missingStarSprite != null) starRenderer.sprite = missingStarSprite;
                }
                starRenderer.color = Color.white;
            }
        }
    }
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

    public void ConfirmStageSelection()
    {
        if (!IsHost()) return;

        if (isHomeSelected)
        {
            Debug.Log("【ホスト】ホームへもどるを選択しました。");
            SendStageSelectNotification(currentStageIndex, true, true);
            LoadHomeScene();
        }
        else
        {
            if (!IsStageUnlocked(currentStageIndex)) return;

            Debug.Log($"【ホスト】ステージ {currentStageIndex + 1} で決定しました。");
            SendStageSelectNotification(currentStageIndex, false, true);
            LoadTargetScene(currentStageIndex);
        }
    }

    private async void SendStageSelectNotification(int stageIndex, bool isHome, bool isReady)
    {
        if (NetworkManager.Instance == null) return;

        StageSelectData msgData = new StageSelectData();
        msgData.type = "stage_select";
        msgData.name_id = NetworkManager.Instance.myPlayerId;
        msgData.room_id = NetworkManager.Instance.myRoomID;
        msgData.index = NetworkManager.Instance.myPlayerIndex;
        msgData.IsStarted = isReady;

        // ホーム選択時は -1 を送信して識別
        msgData.stage_index = isHome ? -1 : stageIndex;
        msgData.stage_ready = isReady;

        string jsonMsg = JsonUtility.ToJson(msgData);
        await NetworkManager.Instance.SendMessageAsync(jsonMsg);
    }

    public void HandleRemoteStageMessage(string msg)
    {
        var stageData = JsonUtility.FromJson<StageSelectData>(msg);
        if (stageData == null) return;

        if (stageData.type == "stage_select" && stageData.name_id != NetworkManager.Instance.myPlayerId)
        {
            if (!stageData.stage_ready)
            {
                if (stageData.stage_index == -1)
                {
                    isHomeSelected = true;
                }
                else
                {
                    isHomeSelected = false;
                    currentStageIndex = stageData.stage_index;
                }
                UpdateCursorPosition();
            }
            else
            {
                if (stageData.stage_index == -1) LoadHomeScene();
                else LoadTargetScene(stageData.stage_index);
            }
        }
    }

    private void LoadHomeScene()
    {
        if (controls != null) controls.StageSelect.Disable();
        NetworkManager.Instance.DeleteData();
        SceneManager.LoadScene("SecondScene"); 
    }

    private void LoadTargetScene(int stageIndex)
    {
        if (controls != null) controls.StageSelect.Disable();

        string sceneName = "Stage" + (stageIndex + 1);
        SceneManager.LoadScene(sceneName);
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