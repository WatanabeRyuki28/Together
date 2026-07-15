using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class StageMenuManager : MonoBehaviour
{
    public static StageMenuManager Instance { get; private set; }


    [Header("1枚目: メインメニューパネル設定")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button exitButton;       // 「ステージ退出」ボタン
    [SerializeField] private Button closeButton;      // 「閉じる」ボタン
    [SerializeField] private RectTransform mainMenuCursor; // メインメニュー用カーソルのRectTransform
    [SerializeField] private float mainMenuCursorOffsetX = -130f; // カーソルをボタンの左にずらす距離

    [Header("2枚目: 確認パネル設定")]
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private Button yesButton;        // 「はい」ボタン
    [SerializeField] private Button noButton;         // 「いいえ」ボタン
    [SerializeField] private RectTransform confirmCursor;   // 確認パネル用カーソルのRectTransform
    [SerializeField] private float confirmCursorOffsetX = -100f; // カーソルをボタンの左にずらす距離

    private int currentMenuIndex = 0;       // 1枚目メニュー用 
    private int currentConfirmIndex = 1;    // 2枚目確認画面用 
    private bool moveInputPressed = false;  // スティックの連打防止用フラグ

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
    [SerializeField] private string stageSelectSceneName = "StageSelect";

    [Header("メニュー内にあるボタン（上から順に登録）")]
    [SerializeField] private Button[] menuButtons; // 例：0番に閉じる、1番にステージ退出

    [Header("確認画面のボタン（0番:はい、1番:いいえ）")]
    [SerializeField] private Button[] confirmationButtons;

    private int currentSelectedIndex = 0;
    private int currentConfirmationIndex = 1;
    private float nextInputTime = 0f;
    private const float inputDelay = 0.2f;

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


    private void Awake()
    {
        // 次のステージへ遷移した際、古いステージのインスタンスの残骸を確実に破棄し、
        // 常に新ステージのManagerが正しく新しいInstanceとして上書きされるようにする
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
        }
        Instance = this;

        controls = new CommunicationUI();
    }

    private void OnEnable()
    {
        // ゲーム開始時のみ有効、メニュー関連はすべて無効
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
        if (menuPanel != null) menuPanel.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.SetActive(false);

        Time.timeScale = 1f;
        if (menuOpenButton != null) menuOpenButton.interactable = true;

        // 新しいステージに合わせて星表示をクリーンアップ＆リセットする
        InitializeStageStarDisplay();



        if (stageNameText != null)
        {
            // まずテキストオブジェクトを確実に表示する
            stageNameText.gameObject.SetActive(true);
            Image.SetActive(true);
            panel.SetActive(true);

            int displayStageNumber = currentStageStageIndex + 1;
            stageNameText.text = $"ステージ {displayStageNumber}";

            StartCoroutine(AnimateStageNameRoutine());

           
        }

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
                // FinalMenuのアクションから値を取得
                float horizontalInput = controls.FinalMenu.Maps.ReadValue<float>();

                if (confirmationButtons != null && confirmationButtons.Length > 0)
                {
                    HandleConfirmationNavigationGamepad(horizontalInput);
                }

                // Aボタンで決定
                if (controls.FinalMenu.Submit.triggered)
                {
                    if (currentConfirmationIndex == 0)
                    {
                        PressYesByClick();
                    }
                    else
                    {
                        CancelExit();
                        SendMenuToggleAction("cancel");
                    }
                }


            }
            else
            {
                // SecondMenuのアクションから値を取得
                float horizontalInput = controls.SecondMenu.Maps.ReadValue<float>();

                if (menuButtons != null && menuButtons.Length > 0)
                {
                    HandleMenuNavigationGamepad(horizontalInput);
                }

                // Aボタンで決定
                if (controls.SecondMenu.Submit.triggered)
                {
                    if (currentSelectedIndex == 0)
                    {
                        ToggleMenu();
                        SendMenuToggleAction("toggle");
                    }
                    else if (currentSelectedIndex == 1)
                    {
                        OpenConfirmation();
                    }
                }


            }
        }
    }

    // 2枚目の左右選択
    private void HandleMenuNavigationGamepad(float horizontalInput)
    {
        if (Mathf.Abs(horizontalInput) > 0.5f)
        {
            if (!moveInputPressed)
            {
                moveInputPressed = true;
                int total = menuButtons.Length;

                if (horizontalInput > 0.5f) currentMenuIndex = (currentMenuIndex + 1) % total;
                else if (horizontalInput < -0.5f) currentMenuIndex = (currentMenuIndex - 1 + total) % total;

                UpdateCursorPositions();

            }
        }
        else
        {
            if (Mathf.Abs(horizontalInput) < 0.1f)
            {
                moveInputPressed = false;
            }
        }
    }

    // 3枚目の左右選択
    private void HandleConfirmationNavigationGamepad(float horizontalInput)
    {
        if (Mathf.Abs(horizontalInput) > 0.5f)
        {
            if (!moveInputPressed)
            {
                moveInputPressed = true;

                // 左入力で「はい(0)」、右入力で「いいえ(1)」
                if (horizontalInput < -0.5f) currentConfirmIndex = 0;
                else if (horizontalInput > 0.5f) currentConfirmIndex = 1;

                UpdateCursorPositions();
            }
        }
        else if (Mathf.Abs(horizontalInput) < 0.1f)
        {
            moveInputPressed = false;
        }
    }

    private void UpdateCursorPositions()
    {
        // 1枚目メインメニューのカーソル制御
        if (isMenuOpen && !confirmationPanel.activeSelf)
        {
            if (mainMenuCursor != null && menuButtons != null && menuButtons.Length > currentMenuIndex)
            {
                mainMenuCursor.gameObject.SetActive(true);
                Button targetButton = menuButtons[currentMenuIndex];
                if (targetButton != null)
                {
                    Vector3 targetPos = targetButton.GetComponent<RectTransform>().position;
                    mainMenuCursor.position = new Vector3(targetPos.x + mainMenuCursorOffsetX, targetPos.y, targetPos.z);
                }
            }
            if (confirmCursor != null) confirmCursor.gameObject.SetActive(false);
        }
        // 2枚目確認パネルのカーソル制御
        else if (isMenuOpen && confirmationPanel.activeSelf)
        {
            if (confirmCursor != null && confirmationButtons != null && confirmationButtons.Length > currentConfirmIndex)
            {
                confirmCursor.gameObject.SetActive(true);
                Button targetButton = confirmationButtons[currentConfirmIndex];
                if (targetButton != null)
                {
                    Vector3 targetPos = targetButton.GetComponent<RectTransform>().position;
                    confirmCursor.position = new Vector3(targetPos.x + confirmCursorOffsetX, targetPos.y, targetPos.z);
                }
            }
            if (mainMenuCursor != null) mainMenuCursor.gameObject.SetActive(false);
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

            controls.Player.Disable();
            controls.SecondMenu.Enable();
            controls.FinalMenu.Disable();

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

            controls.Player.Enable();
            controls.SecondMenu.Disable();
            controls.FinalMenu.Disable();


            if (menuOpenButton != null) menuOpenButton.interactable = true;

            if (mainMenuCursor != null) mainMenuCursor.gameObject.SetActive(false);
            if (confirmCursor != null) confirmCursor.gameObject.SetActive(false);
        }
    }

    private void ToggleMenuLocal(bool open)
    {
        isMenuOpen = open;
        if (menuPanel != null) menuPanel.SetActive(isMenuOpen);

        if (isMenuOpen)
        {

            controls.Player.Disable();
            controls.SecondMenu.Enable();
            controls.FinalMenu.Disable();

            if (menuOpenButton != null) menuOpenButton.interactable = false;
            SetMenuButtonsInteractable(true);
            currentSelectedIndex = 0;
            ApplyMenuButtonFocus();
        }
        else
        {

            controls.Player.Enable();
            controls.SecondMenu.Disable();
            controls.FinalMenu.Disable();

            if (menuOpenButton != null) menuOpenButton.interactable = true;
            if (confirmationPanel != null) confirmationPanel.SetActive(false);
        }
    }

    public void OpenConfirmation()
    {
        confirmationPanel.SetActive(true);
        UpdateYesButtonText();

        controls.SecondMenu.Disable();
        controls.FinalMenu.Enable();

        currentConfirmIndex = 1; // 初期位置は安全のため「いいえ」
        UpdateCursorPositions(); // カーソル更新

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


    // 退出が確定した際にメモリを完全にクリアし、元の状態に巻き戻してから遷移する
    private void SetPlayerReady(int index)
    {
        if (index == 0) player0Ready = true;
        if (index == 1) player1Ready = true;

        UpdateYesButtonText();

        if (player0Ready && player1Ready)
        {
            Debug.Log("両プレイヤーの同意を確認。ステージ選択に戻ります。");

            // 途中でやめるため、キープ中の星のIDをメモリ（List）から破棄し、ファイルをリロードして巻き戻す

            if (SaveManager.Instance != null)
            {
                string targetItemId = ItemIdPrefix + currentStageStageIndex;

                if (SaveManager.Instance.CurrentSaveData?.obtainedItemIds != null)
                {
                    SaveManager.Instance.CurrentSaveData.obtainedItemIds.Remove(targetItemId);
                }

                // セーブファイルから前回の確定セーブ状態をロードし直し、同期をリセットする
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

    public void CancelExit()
    {
        confirmationPanel.SetActive(false);
        hasPressedYes = false;

        int myIndex = (NetworkManager.Instance != null) ? NetworkManager.Instance.myCharaIndex : 0;
        if (myIndex == 0) player0Ready = false;
        if (myIndex == 1) player1Ready = false;
        UpdateYesButtonText();

        if (exitButton != null) exitButton.interactable = true;

        SetMenuButtonsInteractable(true);

        controls.Player.Disable();
        controls.SecondMenu.Enable();
        controls.FinalMenu.Disable();

        currentMenuIndex = 0; // メインに戻ったら初期位置を戻す
        UpdateCursorPositions(); // カーソル更新
    }

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


    private void InitializeStageStarDisplay()
    {
        if (starUIPanel == null) return;

        // -------------------------------------------------------------
        // ★追加：前のステージの星のアイコンUIオブジェクトが残っていたら、すべて全削除してまっさらにする
        // -------------------------------------------------------------
        foreach (Transform child in starUIPanel)
        {
            Destroy(child.gameObject);
        }
        currentSpawnedStar = null;
        // -------------------------------------------------------------

        // 今回の新しいステージに対応するIDを生成
        string targetItemId = ItemIdPrefix + currentStageStageIndex;

        // 念のため最新の正規セーブ状態をロードしてメモリに同期
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.LoadGame();
        }

        // 今回のステージで既に星を獲得しているかチェックしてUIを生成
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

    // 星を取った時点ではファイル保存(SaveGame)は走らせず、メモリ(List)に一時キープのみにする
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

            // メモリ上のリストに仮追加（この時点ではまだセーブファイルに書き込まない）
            SaveManager.Instance.AddItem(targetItemId);

            Debug.Log($"【仮取得】メモリに星 {targetItemId} をキープしました。クリア時に正式保存されます。");
        }
        else
        {
            Debug.LogWarning("SaveManager のインスタンスが見つからないため、獲得情報をキープできませんでした。");
        }

        Debug.Log($"ステージアイテムを取得！黄色のスターアイコンを左上に追加しました。(Stage_{currentStageStageIndex})");
    }

    /// <summary>
    /// ★追加：独立した1つのクリアシーンへ安全に情報を引き渡すため、
    /// 現在のステージインデックスをPlayerPrefsに保存する
    /// </summary>
    public void PrepareClearSceneTransition()
    {
        // 1つのクリアシーンが「どのステージの結果を表示すべきか」を判断するためのメモ
        PlayerPrefs.SetInt("CurrentStageIndex", currentStageStageIndex);

        // （参考）リトライ用やネクストステージ用の設定もここで一緒に担保しておくと安全です
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


    private IEnumerator HideStageNameAfterDelay(float delay)
    {
        // ゲーム中の一時停止（Time.timeScale = 0）の影響を受けずに、現実世界の時間で3秒待つ
        yield return new WaitForSeconds(delay);

        if (stageNameText != null)
        {
            stageNameText.gameObject.SetActive(false); // 3秒経ったら非表示にする
            Image.SetActive(false);
            panel.SetActive(false);
        }
    }

    // テキストアニメーション
    private IEnumerator AnimateStageNameRoutine()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        
        int displayStageNumber = currentStageStageIndex + 1;
        string baseText = $"ステージ {displayStageNumber}"; // ベースとなる文字

        float duration = 3f;      // 全体で表示しておく時間
        float interval = 0.3f;    // 「.」が増える文字更新の間隔
        float elapsedTime = 0f;   // 経過時間
        int dotCount = 0;         // 現在のドットの数 

        // 3秒間が経過するまでループを繰り返す
        while (elapsedTime < duration)
        {
            // ドットの数に応じたテキストを作る

            string dots = new string('.', dotCount);
            stageNameText.text = baseText + dots;

            // 0.3秒待つ
            yield return new WaitForSecondsRealtime(interval);
            elapsedTime += interval;


            dotCount = (dotCount + 1) % 4;
        }

        stageNameText.text = baseText + "...";
        yield return new WaitForSecondsRealtime(0.3f);

        // 3秒経ったらテキストオブジェクトごと非表示にする
        stageNameText.gameObject.SetActive(false);
        Image.SetActive(false);
        panel.SetActive(false);
        Debug.Log("3秒間のアニメーションが終了したため、テキストを非表示にしました。");
    }


    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}