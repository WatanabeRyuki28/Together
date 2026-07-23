using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class StageMenuManager : MonoBehaviour
{
    public static StageMenuManager Instance { get; private set; }

    private enum MenuState
    {
        Closed,        // 閉じている
        FirstMenu,     // 第1段階：「退出する」 or 「閉じる」
        FinalMenu      // 第2段階：「はい」 or 「いいえ」
    }
    private MenuState currentMenuState = MenuState.Closed;

    [Header("1枚目: メインメニューパネル設定")]
    [SerializeField] private GameObject firstmenuPanel;
    [SerializeField] private RectTransform firstMenuCursor; // 第1メニューのカーソル画像
    [SerializeField] private RectTransform exitButtonTransform;  // 「退出する」ボタンの座標
    [SerializeField] private RectTransform closeButtonTransform; // 「閉じる」ボタンの座標
    private int currentFirstIndex = 0; // 0: 退出する, 1: 閉じる

    [Header("2枚目: 確認パネル設定")]
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private RectTransform confirmCursor;   // 第2メニューのカーソル画像
    [SerializeField] private RectTransform yesButtonTransform;   // 「はい」ボタンの座標
    [SerializeField] private RectTransform noButtonTransform;    // 「いいえ」ボタンの座標
    private int currentConfirmIndex = 1; // 0: はい, 1: いいえ (初期位置は「いいえ」)

    [Header("カーソル表示のX軸オフセット微調整用")]
    [SerializeField] private float cursorOffsetX = -120f; // ボタンからどれだけ左にズラすか

    private bool isNavigating = false;

    [Header("画面左上のメニューボタン")]
    [SerializeField] private Button menuOpenButton;

    [Header("退出確認用のUI要素")]
    [SerializeField] private Text yesButtonText;

    [Header("左上のスターUIの親オブジェクト")]
    [SerializeField] private Transform starUIPanel;

    [Header("生成するスターアイコンのプレハブ")]
    [SerializeField] private GameObject starIconPrefab; // 黄色の星プレハブ

    [Header("未獲得時に表示する白スターのプレハブ")]
    [SerializeField] private GameObject missingStarIconPrefab;

    [Header("このステージのインデックス（0から開始）")]
    [SerializeField] private int currentStageStageIndex = 0;

    [Header("ステージ選択画面のシーン名")]
    [SerializeField] private string stageSelectSceneName = "StageSelectScene";

    // Input System用のコントローラー
    private CommunicationUI controls;

    private int readyPlayersCount = 0;
    private bool player0Ready = false;
    private bool player1Ready = false;
    public bool isMenuOpen { get; private set; } = false;
    private bool hasPressedYes = false;
    private GameObject currentSpawnedStar = null;
    private const string ItemIdPrefix = "Stage_";

    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject Image;
    [SerializeField] private Text stageNameText;
    [SerializeField] private Text LoadText;

    [Header("退出同期用の変数（追記）")]
    private bool myExitVote = false;      // 自分が「はい」を押したか
    private bool remoteExitVote = false;  // 相手が「はい」を押したか
    private bool isExitingScene = false;   // 二重遷移防止フラグ

    [SerializeField] private Text exitStatusText;

    [SerializeField] private Button yesButton;
    public bool isIntroPlaying { get; private set; } = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;

        controls = new CommunicationUI();
    }

    private void OnEnable()
    {
        controls.Player.Enable();
        controls.SecondMenu.Disable();
        controls.FinalMenu.Disable();
    }

    private void OnDisable()
    {
        controls.Player.Disable();
        controls.SecondMenu.Disable();
        controls.FinalMenu.Disable();
    }

    private void Start()
    {
        if (firstmenuPanel != null) firstmenuPanel.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.SetActive(false);

        Time.timeScale = 1f;
        if (menuOpenButton != null) menuOpenButton.interactable = true;

        InitializeStageStarDisplay();

        if (stageNameText != null)
        {
            stageNameText.gameObject.SetActive(true);
            LoadText.gameObject.SetActive(true);
            Image.SetActive(true);
            panel.SetActive(true);

            int displayStageNumber = currentStageStageIndex + 1;
            stageNameText.text = $"ステージ {displayStageNumber}";
            LoadText.text = "読み込み中";

            
            StartCoroutine(AnimateStageNameRoutine());
        }
    }

    private void Update()
    {


        if (InputSystem.GetDevice<Gamepad>() != null && Gamepad.current.startButton.wasPressedThisFrame)
        {
            if (currentMenuState == MenuState.Closed)
            {
                // プレイ画面のときだけ、メニューを開く
                ToggleMenu();
                return;
            }
            else if (currentMenuState == MenuState.FirstMenu)
            {
                // もし「1枚目のメニュー」を開いているときにスタートボタンを押したら閉じるようにしたい場合はここに入れる
                ToggleMenu();
                return;
            }
            else if (currentMenuState == MenuState.FinalMenu)
            {
                // 「2枚目の確認パネル」のときは、スタートボタンを押しても何もさせない
                return;
            }
        }

        if (currentMenuState == MenuState.Closed) return;

        if (myExitVote || isExitingScene) return;

        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        // 横移動の処理
        float navigateInput = controls.SecondMenu.Maps.ReadValue<float>();

        if (Mathf.Abs(navigateInput) > 0.5f)
        {
            if (!isNavigating)
            {
                isNavigating = true;
                HandleHorizontalNavigation(navigateInput);
            }
        }
        else
        {
            isNavigating = false;
        }
        // 決定ボタン（Aボタン）
        if (controls.SecondMenu.Submit.triggered)
        {
            StartCoroutine(ExecuteSelectionWithDelay());
            return;
        }
    }
    private IEnumerator ExecuteSelectionWithDelay()
    {
        yield return null; // 1フレーム待つ
        ExecuteSelection();
    }
    // 横移動の数値に基づいてカーソルを切り替える
    private void HandleHorizontalNavigation(float direction)
    {
        if (currentMenuState == MenuState.FirstMenu)
        {
            if (direction < -0.5f && currentFirstIndex == 1)
            {
                currentFirstIndex = 0; // 「退出する」へ
            }
            else if (direction > 0.5f && currentFirstIndex == 0)
            {
                currentFirstIndex = 1; // 「閉じる」へ
            }
        }
        else if (currentMenuState == MenuState.FinalMenu)
        {
            if (direction < -0.5f && currentConfirmIndex == 1)
            {
                currentConfirmIndex = 0; // 「はい」へ
            }
            else if (direction > 0.5f && currentConfirmIndex == 0)
            {
                currentConfirmIndex = 1; // 「いいえ」へ
            }
        }

        UpdateCursorPositions();
    }

    // Aボタンが押されたときの実行処理
    private void ExecuteSelection()
    {
        if (currentMenuState == MenuState.FirstMenu)
        {
            if (currentFirstIndex == 0)
            {
                OpenConfirmation(); // 「退出する」
            }
            else
            {
                ToggleMenu(); // 「閉じる」
            }
        }
        else if (currentMenuState == MenuState.FinalMenu)
        {
            if (currentConfirmIndex == 0)
            {
                PressYesByClick(); // 「はい」
            }
            else
            {
                CancelExit(); // 「いいえ」
            }
        }
    }

    // メニューの開閉処理
    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;

        if (isMenuOpen)
        {
            currentMenuState = MenuState.FirstMenu;
            if (firstmenuPanel != null) firstmenuPanel.SetActive(true);
            if (confirmationPanel != null) confirmationPanel.SetActive(false);

            controls.Player.Disable();
            controls.SecondMenu.Enable();
            controls.FinalMenu.Disable();

            if (menuOpenButton != null) menuOpenButton.interactable = false;

            currentFirstIndex = 0;
            UpdateCursorPositions();
        }
        else
        {
            currentMenuState = MenuState.Closed;
            if (firstmenuPanel != null) firstmenuPanel.SetActive(false);
            if (confirmationPanel != null) confirmationPanel.SetActive(false);

            ResetReadyStatus();

            controls.Player.Enable();
            controls.SecondMenu.Disable();
            controls.FinalMenu.Disable();

            if (menuOpenButton != null) menuOpenButton.interactable = true;
        }
    }

    // 2枚目の確認画面を開く
    public void OpenConfirmation()
    {
        currentMenuState = MenuState.FinalMenu;
        if (firstmenuPanel != null) firstmenuPanel.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.SetActive(true);

        UpdateYesButtonText();

        controls.SecondMenu.Enable();
        //controls.FinalMenu.Enable();  // 2枚目の入力をオンに

        currentConfirmIndex = 1; // 初期位置は安全のため「いいえ」
        UpdateCursorPositions();
    }

    // カーソル位置の更新処理（エラーの起きていた古い変数をすべて排除）
    private void UpdateCursorPositions()
    {
        // 1枚目メインメニューのアクティブ時
        if (currentMenuState == MenuState.FirstMenu && firstmenuPanel != null && firstmenuPanel.activeSelf)
        {
            if (firstMenuCursor != null)
            {
                firstMenuCursor.gameObject.SetActive(true);
                RectTransform targetTransform = (currentFirstIndex == 0) ? exitButtonTransform : closeButtonTransform;
                if (targetTransform != null)
                {
                    // position ではなく anchoredPosition (UIのローカル座標) を使用する
                    Vector2 targetAnchorPos = targetTransform.anchoredPosition;
                    firstMenuCursor.anchoredPosition = new Vector2(targetAnchorPos.x  +10, targetAnchorPos.y -10);
                }
            }
            if (confirmCursor != null) confirmCursor.gameObject.SetActive(false);
        }
        // 2枚目確認パネルのアクティブ時
        else if (currentMenuState == MenuState.FinalMenu && confirmationPanel != null && confirmationPanel.activeSelf)
        {
            if (confirmCursor != null)
            {
                confirmCursor.gameObject.SetActive(true);
                RectTransform targetTransform = (currentConfirmIndex == 0) ? yesButtonTransform : noButtonTransform;
                if (targetTransform != null)
                {
                    // anchoredPosition を使用
                    Vector2 targetAnchorPos = targetTransform.anchoredPosition;
                    confirmCursor.anchoredPosition = new Vector2(targetAnchorPos.x + cursorOffsetX, targetAnchorPos.y);
                }
            }
            if (firstMenuCursor != null) firstMenuCursor.gameObject.SetActive(false);
        }
        else
        {
            if (firstMenuCursor != null) firstMenuCursor.gameObject.SetActive(false);
            if (confirmCursor != null) confirmCursor.gameObject.SetActive(false);
        }
    }

    public async void PressYesByClick()
    {
        if (myExitVote || isExitingScene) return;
   

        myExitVote = true;
        if (yesButton != null)
        {
            yesButton.interactable = false;
        }

        UpdateExitStatusUI();

        if (NetworkManager.Instance != null)
        {
            InGameMoveData exitMsg = new InGameMoveData();
            exitMsg.type = "menu_toggle";
            exitMsg.dataType = "";
            exitMsg.room_id = NetworkManager.Instance.myRoomID;
            exitMsg.char_index = NetworkManager.Instance.myPlayerIndex;

            string json = JsonUtility.ToJson(exitMsg);
            await NetworkManager.Instance.SendMessageAsync(json);

            Debug.Log("退出同意を送信しました。");
        }

        CheckBothPlayersReadyToExit();
    }

    public void ReceiveExitReady(int senderIndex)
    {
        int myIndex = (NetworkManager.Instance != null) ? NetworkManager.Instance.myCharaIndex : -1;

        // 自分自身の送信メッセージがループバックして届いた場合は無視する
        if (senderIndex == myIndex) return;

        remoteExitVote = true; // 相手が「はい」を押した場合のみ立てる

        // UIを更新する 
        UpdateExitStatusUI();

        // 全員揃ったか判定
        CheckBothPlayersReadyToExit();
    }

    public void ReceiveExitCancel(int senderIndex)
    {

        int myIndex = (NetworkManager.Instance != null) ? NetworkManager.Instance.myCharaIndex : -1;

        // 自分自身のメッセージなら無視
        if (senderIndex == myIndex) return;

        remoteExitVote = false;
        UpdateExitStatusUI();
    }

    private void UpdateExitStatusUI()
    {
        if (exitStatusText == null || isExitingScene) return;

        int voteCount = 0;
        if (myExitVote) voteCount++;
        if (remoteExitVote) voteCount++;

        exitStatusText.text = $"はい {voteCount}/2";
    }

    private void CheckBothPlayersReadyToExit()
    {
        if (myExitVote && remoteExitVote && !isExitingScene)
        {
            isExitingScene = true;
            

            // 獲得スターの取り消し処理
            if (SaveManager.Instance != null)
            {
                string targetItemId = ItemIdPrefix + currentStageStageIndex;
                if (SaveManager.Instance.CurrentSaveData?.obtainedItemIds != null)
                {
                    SaveManager.Instance.CurrentSaveData.obtainedItemIds.Remove(targetItemId);
                }
                SaveManager.Instance.LoadGame();
            }

            // 1.5秒後にステージ選択へ
            Invoke("LoadStageSelectScene", 1.5f);
        }
    }

    private void LoadStageSelectScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(stageSelectSceneName);
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

    public async void CancelExit()
    {
        hasPressedYes = false;
        myExitVote = false;
        ResetReadyStatus();

        currentMenuState = MenuState.FirstMenu;
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (firstmenuPanel != null) firstmenuPanel.SetActive(true);

        controls.Player.Disable();
        controls.SecondMenu.Enable();
        //controls.FinalMenu.Disable();

        currentFirstIndex = 0;
        UpdateCursorPositions();

        if (NetworkManager.Instance != null)
        {
            int currentMyIndex = NetworkManager.Instance.myCharaIndex;

            InGameMoveData cancelMsg = new InGameMoveData();
            cancelMsg.type = "menu_exit_cancel";
            cancelMsg.dataType = "";
            cancelMsg.room_id = NetworkManager.Instance.myRoomID;
            cancelMsg.char_index = currentMyIndex;

            string json = JsonUtility.ToJson(cancelMsg);
            await NetworkManager.Instance.SendMessageAsync(json);
            Debug.Log("退出キャンセルを送信しました。");
        }
    }

    private void ResetReadyStatus()
    {
        int myIndex = (NetworkManager.Instance != null) ? NetworkManager.Instance.myCharaIndex : 0;
        if (myIndex == 0) player0Ready = false;
        if (myIndex == 1) player1Ready = false;
        hasPressedYes = false;
        UpdateYesButtonText();
    }

    private void InitializeStageStarDisplay()
    {
        if (starUIPanel == null) return;

        foreach (Transform child in starUIPanel)
        {
            Destroy(child.gameObject);
        }
        currentSpawnedStar = null;

        string targetItemId = ItemIdPrefix + currentStageStageIndex;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.LoadGame();
        }

        if (SaveManager.Instance != null && SaveManager.Instance.HasItem(targetItemId))
        {
            if (starIconPrefab != null)
            {
                currentSpawnedStar = Instantiate(starIconPrefab, starUIPanel);
                ResetUIElementTransform(currentSpawnedStar);
                Debug.Log($"【UI初期化】{targetItemId} は獲得済みのため、黄色の星を表示します。");
            }
        }
        else
        {
            if (missingStarIconPrefab != null)
            {
                currentSpawnedStar = Instantiate(missingStarIconPrefab, starUIPanel);
                ResetUIElementTransform(currentSpawnedStar);
                Debug.Log($"【UI初期化】{targetItemId} は未獲得のため、白星を表示します。");
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
            Debug.Log($"【仮取得】メモリに星 {targetItemId} をキープしました。クリア時に正式保存されます。");
        }
    }

    public void PrepareClearSceneTransition()
    {
        PlayerPrefs.SetInt("CurrentStageIndex", currentStageStageIndex);
        PlayerPrefs.SetString("RetrySceneName", SceneManager.GetActiveScene().name);
        PlayerPrefs.SetInt("NextStageIndex", SceneManager.GetActiveScene().buildIndex + 1);
        PlayerPrefs.Save();
        Debug.Log($"【クリア準備】ステージ {currentStageStageIndex} の情報を引き渡し用に保存しました。");
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

   

    private IEnumerator AnimateStageNameRoutine()
    {

        isIntroPlaying = true;
        yield return new WaitForSecondsRealtime(0.5f);

        int displayStageNumber = currentStageStageIndex + 1;
        string baseText = $"ステージ {displayStageNumber}";
        string lText = "読み込み中";

        float duration = 3f;
        float interval = 0.3f;
        float elapsedTime = 0f;
        int dotCount = 0;

        while (elapsedTime < duration)
        {
            string dots = new string('.', dotCount);
            LoadText.text = lText + dots;

            yield return new WaitForSecondsRealtime(interval);
            elapsedTime += interval;

            dotCount = (dotCount + 1) % 4;
        }

        stageNameText.text = baseText;
        LoadText.text = lText + "...";
        yield return new WaitForSecondsRealtime(0.3f);

        stageNameText.gameObject.SetActive(false);
        LoadText.gameObject.SetActive(false);

        Image.SetActive(false);
        panel.SetActive(false);

      isIntroPlaying = false;
      

        Debug.Log("3秒間のアニメーションが終了したため、テキストを非表示にしました。");
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}