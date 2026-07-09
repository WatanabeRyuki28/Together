using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System;

public class StageMenuManager : MonoBehaviour
{
    public static StageMenuManager Instance { get; private set; }

    [Header("UIパネルの設定")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject confirmationPanel;

    [Header("画面左上のメニューボタン")]
    [SerializeField] private Button menuOpenButton;

    [Header("退出確認用のUI要素")]
    [SerializeField] private Button exitButton; // 「はい」ボタン
    [SerializeField] private Text yesButtonText;
    [SerializeField] private Button noButton;   // 「いいえ」ボタンの参照

    [Header("左上のスターUIの親オブジェクト")]
    [SerializeField] private Transform starUIPanel;

    [Header("生成するスターアイコンのプレハブ")]
    [SerializeField] private GameObject starIconPrefab; // 黄色の星プレハブ

    [Header("未獲得時に表示する白スターのプレハブ")]
    [SerializeField] private GameObject missingStarIconPrefab;

    [Header("このステージのインデックス（0から開始）")]
    [SerializeField] private int currentStageStageIndex = 0;

    [Header("ステージ選択画面のシーン名")]
    [SerializeField] private string stageSelectSceneName = "StageSelect";

    [Header("メニュー内にあるボタン（上から順に登録）")]
    [SerializeField] private Button[] menuButtons; // 例：0番に閉じる、1番にステージ退出

    [Header("確認画面のボタン（0番:はい、1番:いいえ）")]
    [SerializeField] private Button[] confirmationButtons;

    private int currentSelectedIndex = 0;
    private int currentConfirmationIndex = 0;
    private float nextInputTime = 0f;
    private const float inputDelay = 0.2f;

    // A/Dキーでの操作に固定
    private KeyCode menuLeftKey = KeyCode.A;
    private KeyCode menuRightKey = KeyCode.D;

    private int readyPlayersCount = 0;

    private bool player0Ready = false;
    private bool player1Ready = false;
    public bool isMenuOpen { get; private set; } = false;

    private bool hasPressedYes = false;

    private GameObject currentSpawnedStar = null;

    // マジックナンバー回避のためのキー定数
    private const string ItemIdPrefix = "Stage_";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.SetActive(false);

        Time.timeScale = 1f;
        if (menuOpenButton != null) menuOpenButton.interactable = true;

        InitializeStageStarDisplay();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (confirmationPanel.activeSelf)
            {
                CancelExit();
                SendMenuToggleAction("cancel");
            }
            else
            {
                ToggleMenu();
                SendMenuToggleAction("toggle");
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.Y))
        {
            if (!isMenuOpen)
            {
                ToggleMenu();
                SendMenuToggleAction("toggle");
            }
            return;
        }

        if (isMenuOpen)
        {
            if (confirmationPanel.activeSelf)
            {
                if (confirmationButtons != null && confirmationButtons.Length > 0)
                {
                    HandleConfirmationNavigation();
                }
            }
            else
            {
                if (menuButtons != null && menuButtons.Length > 0)
                {
                    HandleMenuNavigation();
                }
            }
        }
    }

    private void InitializeStageStarDisplay()
    {
        if (starUIPanel == null) return;

        string targetItemId = ItemIdPrefix + currentStageStageIndex;

        if (SaveManager.Instance != null && SaveManager.Instance.HasItem(targetItemId))
        {
            if (starIconPrefab != null)
            {
                currentSpawnedStar = Instantiate(starIconPrefab, starUIPanel);
                ResetUIElementTransform(currentSpawnedStar);
            }
        }
        else
        {
            if (missingStarIconPrefab != null)
            {
                currentSpawnedStar = Instantiate(missingStarIconPrefab, starUIPanel);
                ResetUIElementTransform(currentSpawnedStar);
            }
        }
    }

    public void AddStar()
    {
        if (starUIPanel == null || starIconPrefab == null) return;

        if (currentSpawnedStar != null)
        {
            Destroy(currentSpawnedStar);
        }

        currentSpawnedStar = Instantiate(starIconPrefab, starUIPanel);
        ResetUIElementTransform(currentSpawnedStar);

        if (SaveManager.Instance != null)
        {
            string targetItemId = ItemIdPrefix + currentStageStageIndex;

            SaveManager.Instance.AddItem(targetItemId);
            SaveManager.Instance.SaveGame();

            Debug.Log($"【セーブ】ステージアイテム {targetItemId} の獲得情報を保存しました。");
        }
        else
        {
            Debug.LogWarning("SaveManager のインスタンスが見つからないため、獲得情報を保存できませんでした。");
        }

        Debug.Log($"ステージアイテムを取得！黄色のスターアイコンを左上に追加しました。(Stage_{currentStageStageIndex})");
    }

    private void ResetUIElementTransform(GameObject targetObj)
    {
        if (targetObj == null) return;

        targetObj.transform.localScale = Vector3.one;

        RectTransform rect = targetObj.GetComponent<RectTransform>();
        if (rect != null)
        {
            Vector3 localPos = rect.localPosition;
            localPos.z = 0f;
            rect.localPosition = localPos;
        }
    }

    private void HandleMenuNavigation()
    {
        float inputVal = 0f;
        if (Input.GetKey(menuLeftKey)) inputVal += 1f;
        if (Input.GetKey(menuRightKey)) inputVal -= 1f;

        if (Mathf.Abs(inputVal) > 0.5f)
        {
            if (Time.unscaledTime >= nextInputTime)
            {
                int total = menuButtons.Length;
                if (inputVal < -0.5f) currentSelectedIndex = (currentSelectedIndex + 1) % total;
                else if (inputVal > 0.5f) currentSelectedIndex = (currentSelectedIndex - 1 + total) % total;

                ApplyMenuButtonFocus();
                nextInputTime = Time.unscaledTime + inputDelay;
            }
        }
        else
        {
            nextInputTime = 0f;
        }
    }

    private void HandleConfirmationNavigation()
    {
        float horizontalInput = 0f;
        if (Input.GetKey(menuLeftKey)) horizontalInput -= 1f;
        if (Input.GetKey(menuRightKey)) horizontalInput += 1f;

        if (Mathf.Abs(horizontalInput) > 0.5f)
        {
            if (Time.unscaledTime >= nextInputTime)
            {
                if (horizontalInput > 0.5f)
                {
                    currentConfirmationIndex = 1;
                }
                else if (horizontalInput < -0.5f)
                {
                    currentConfirmationIndex = 0;
                }

                ApplyConfirmationButtonFocus();
                nextInputTime = Time.unscaledTime + inputDelay;
            }
        }
        else
        {
            nextInputTime = 0f;
        }
    }

    private void ApplyMenuButtonFocus()
    {
        if (EventSystem.current == null || menuButtons == null || menuButtons.Length == 0) return;
        if (currentSelectedIndex < 0 || currentSelectedIndex >= menuButtons.Length) currentSelectedIndex = 0;

        if (menuButtons[currentSelectedIndex] != null)
        {
            menuButtons[currentSelectedIndex].Select();
        }
    }

    private void ApplyConfirmationButtonFocus()
    {
        if (EventSystem.current == null || confirmationButtons == null || confirmationButtons.Length == 0) return;
        if (currentConfirmationIndex < 0 || currentConfirmationIndex >= confirmationButtons.Length) currentConfirmationIndex = 0;

        if (confirmationButtons[currentConfirmationIndex] != null)
        {
            confirmationButtons[currentConfirmationIndex].Select();
        }
    }

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        menuPanel.SetActive(isMenuOpen);

        if (isMenuOpen)
        {
            Time.timeScale = 0f;
            Debug.Log("ゲームを一時停止しました。");

            if (menuOpenButton != null) menuOpenButton.interactable = false;

            // メニューが開いた時は元のボタン群を触れるようにしておく
            SetMenuButtonsInteractable(true);

            currentSelectedIndex = 0;
            ApplyMenuButtonFocus();
        }
        else
        {
            Time.timeScale = 1f;
            Debug.Log("ゲームを再開しました。");

            if (menuOpenButton != null) menuOpenButton.interactable = true;
        }
    }

    private void ToggleMenuLocal(bool open)
    {
        isMenuOpen = open;
        if (menuPanel != null) menuPanel.SetActive(isMenuOpen);

        if (isMenuOpen)
        {
            if (menuOpenButton != null) menuOpenButton.interactable = false;
            SetMenuButtonsInteractable(true);
            currentSelectedIndex = 0;
            ApplyMenuButtonFocus();
        }
        else
        {
            if (menuOpenButton != null) menuOpenButton.interactable = true;
            if (confirmationPanel != null) confirmationPanel.SetActive(false);
        }
    }

    // ★修正：確認画面を開いた時、背後のメニューボタン群を押せなくする
    public void OpenConfirmation()
    {
        confirmationPanel.SetActive(true);
        UpdateYesButtonText();
        currentConfirmationIndex = 0;
        ApplyConfirmationButtonFocus();

        // メインメニューのボタンを一括で非活性化
        SetMenuButtonsInteractable(false);
    }

    public async void PressYesByClick()
    {
        if (hasPressedYes) return;
        hasPressedYes = true;

        if (exitButton != null) exitButton.interactable = false;

        int myIndex = (NetworkManager.Instance != null) ? NetworkManager.Instance.myCharaIndex : 0;
        SetPlayerReady(myIndex);

        if (NetworkManager.Instance != null)
        {
            InGameMoveData exitMsg = new InGameMoveData();
            exitMsg.type = "menu_exit_ready";
            exitMsg.dataType = "";
            exitMsg.room_id = NetworkManager.Instance.myRoomID;
            exitMsg.char_index = myIndex;

            string json = JsonUtility.ToJson(exitMsg);
            await NetworkManager.Instance.SendMessageAsync(json);

            Debug.Log("退出同意を送信しました。");
        }
    }

    public void ReceiveExitReady(int senderIndex)
    {
        Debug.Log($"【同期】インデックス {senderIndex} のプレイヤーから退出同意を受信しました。");
        SetPlayerReady(senderIndex);
    }

    private void SetPlayerReady(int index)
    {
        if (index == 0) player0Ready = true;
        if (index == 1) player1Ready = true;

        UpdateYesButtonText();

        if (player0Ready && player1Ready)
        {
            Debug.Log("両プレイヤーの同意を確認。ステージ選択に戻ります。");

            if (SaveManager.Instance != null)
            {
                string targetItemId = ItemIdPrefix + currentStageStageIndex;

                if (SaveManager.Instance.CurrentSaveData?.obtainedItemIds != null)
                {
                    SaveManager.Instance.CurrentSaveData.obtainedItemIds.Remove(targetItemId);
                }

                SaveManager.Instance.LoadGame();

                Debug.Log($"【退出リセット】途中でやめたため、{targetItemId} の獲得をキャンセルしてデータを巻き戻しました。");
            }

            player0Ready = false;
            player1Ready = false;

            Time.timeScale = 1f;
            SceneManager.LoadScene(stageSelectSceneName);
        }
    }

    private async void SendMenuToggleAction(string actionType)
    {
        if (NetworkManager.Instance != null)
        {
            InGameMoveData menuMsg = new InGameMoveData();
            menuMsg.type = "menu_toggle";
            menuMsg.dataType = "";
            menuMsg.room_id = NetworkManager.Instance.myRoomID;

            int myRealChara = NetworkManager.Instance.myRealSelectedChar;
            if (myRealChara == -1) myRealChara = NetworkManager.Instance.myCharaIndex;
            menuMsg.char_index = myRealChara;

            menuMsg.position_x = (actionType == "toggle") ? 1f : 2f;

            string json = JsonUtility.ToJson(menuMsg);
            await NetworkManager.Instance.SendMessageAsync(json);
        }
    }

    public void ReceiveMenuToggle(float actionCode, int senderCharIndex)
    {
        Debug.Log($"【同期受信】ReceiveMenuToggle: Code={actionCode}, Sender={senderCharIndex}");

        if (actionCode == 1f)
        {
            ToggleMenuLocal(true);
            hasPressedYes = false;
            if (exitButton != null) exitButton.interactable = true;
        }
        else if (actionCode == 2f)
        {
            ToggleMenuLocal(false);
        }
        else if (actionCode == 3f)
        {
            if (confirmationPanel != null) confirmationPanel.SetActive(false);
            hasPressedYes = false;
            if (exitButton != null) exitButton.interactable = true;

            // ★修正：ネットワーク同期経由でのキャンセル時も、メインメニューのボタンを復活させる
            SetMenuButtonsInteractable(true);
            ApplyMenuButtonFocus();
        }
    }

    private void UpdateYesButtonText()
    {
        if (yesButtonText != null)
        {
            int count = 0;
            if (player0Ready) count++;
            if (player1Ready) count++;

            yesButtonText.text = $"はい {count}/2";
        }
    }

    // ★修正：「いいえ」またはキャンセルで戻った時、背後のメニューボタン群を再び押せるようにする
    public void CancelExit()
    {
        confirmationPanel.SetActive(false);
        currentConfirmationIndex = 0;
        hasPressedYes = false;

        int myIndex = (NetworkManager.Instance != null) ? NetworkManager.Instance.myCharaIndex : 0;
        if (myIndex == 0) player0Ready = false;
        if (myIndex == 1) player1Ready = false;
        UpdateYesButtonText();

        if (exitButton != null) exitButton.interactable = true;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);

        // メインメニューのボタンを一括で活性化
        SetMenuButtonsInteractable(true);

        ApplyMenuButtonFocus();
    }

    // ★追加：menuButtonsの配列に登録されたボタンの有効・無効を一括で切り替えるヘルパー関数
    private void SetMenuButtonsInteractable(bool interactable)
    {
        if (menuButtons != null)
        {
            foreach (var btn in menuButtons)
            {
                if (btn != null)
                {
                    btn.interactable = interactable;
                }
            }
        }
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}