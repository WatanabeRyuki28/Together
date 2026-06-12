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
    [SerializeField] private GameObject starIconPrefab;

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
    }

    private void Update()
    {
        // Escapeキーが押されたとき（ここは変更なし：いつでも開閉可能）
        /*if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (confirmationPanel.activeSelf) CancelExit();
            else ToggleMenu();
            return;
        }

        // ★【修正】Yキーが押されたとき
        // メニューが完全に閉じている（isMenuOpen == false）ときだけメニューを開く
        if (Input.GetKeyDown(KeyCode.Y))
        {
            if (!isMenuOpen)
            {
                ToggleMenu();
            }
            return;
        }

        // メニューが開いている時だけ入力を受け付ける
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
        }*/

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (confirmationPanel.activeSelf)
            {
                CancelExit();
                // 確認画面のキャンセルも同期させる
                SendMenuToggleAction("cancel");
            }
            else
            {
                ToggleMenu();
                // 通常メニューの開閉
                SendMenuToggleAction("toggle");
            }
            return;
        }

        // Yキーが押されたとき
        if (Input.GetKeyDown(KeyCode.Y))
        {
            if (!isMenuOpen)
            {
                ToggleMenu();
                SendMenuToggleAction("toggle");
            }
            return;
        }

        // メニューが開いている時だけ入力を受け付ける（ここは変更なし）
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

    // 通常メニュー用（閉じる／退出）の選択処理（A/Dで上下に動く）
    private void HandleMenuNavigation()
    {
        float inputVal = 0f;
        if (Input.GetKey(menuLeftKey)) inputVal += 1f; // Aで上へ
        if (Input.GetKey(menuRightKey)) inputVal -= 1f; // Dで下へ

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

        // ❌ ここにあった手動のSpaceキー判定（Input.GetKeyDown）を削除しました
    }

    // 確認画面（はい/いいえ）用の左右選択処理
    private void HandleConfirmationNavigation()
    {
        float horizontalInput = 0f;
        if (Input.GetKey(menuLeftKey)) horizontalInput -= 1f; // Aキーで左（はい）
        if (Input.GetKey(menuRightKey)) horizontalInput += 1f; // Dキーで右（いいえ）

        if (Mathf.Abs(horizontalInput) > 0.5f)
        {
            if (Time.unscaledTime >= nextInputTime)
            {
                if (horizontalInput > 0.5f)
                {
                    // 右に入力：いいえ（1番）を選択
                    currentConfirmationIndex = 1;
                }
                else if (horizontalInput < -0.5f)
                {
                    // 左に入力：はい（0番）を選択
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
            currentSelectedIndex = 0;
            ApplyMenuButtonFocus();
        }
        else
        {
            if (menuOpenButton != null) menuOpenButton.interactable = true;
            if (confirmationPanel != null) confirmationPanel.SetActive(false);
        }
    }

    public void OpenConfirmation()
    {
        confirmationPanel.SetActive(true);
        

        UpdateYesButtonText();

        currentConfirmationIndex = 0;
        ApplyConfirmationButtonFocus();
    }

    public async void PressYesByClick()
    {
        // すでに一度押しているなら、処理を完全にブロックする 
        if (hasPressedYes) return;

        // ロックをかける 
        hasPressedYes = true;

        // 連打防止
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

            Debug.Log(" 退出同意を送信しました。");
        }
    }

   

    public void ReceiveExitReady(int senderIndex)
    {
        Debug.Log($"【同期】インデックス {senderIndex} のプレイヤーから退出同意を受信しました。");
        //  届いたインデックスの同意フラグをONにする
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
            menuMsg.char_index = myRealChara; // 誰が操作したかを乗せる

            menuMsg.position_x = (actionType == "toggle") ? 1f : 2f;

            string json = JsonUtility.ToJson(menuMsg);
            await NetworkManager.Instance.SendMessageAsync(json);
        }
    }

    // サーバーから戻ってきたパケットを元に、実際に画面を切り替える関数
    public void ReceiveMenuToggle(float actionCode, int senderCharIndex)
    {
        Debug.Log($"【同期受信】ReceiveMenuToggle: Code={actionCode}, Sender={senderCharIndex}");

        if (actionCode == 1f) // メニューを開く
        {
            ToggleMenuLocal(true);

            // 相手が開いたことによるリセット処理
            hasPressedYes = false;
            if (exitButton != null) exitButton.interactable = true;
        }
        else if (actionCode == 2f) // メメインメニューを閉じる
        {
            ToggleMenuLocal(false);
        }
        else if (actionCode == 3f) // 確認画面のみキャンセル
        {
            if (confirmationPanel != null) confirmationPanel.SetActive(false);
            hasPressedYes = false;
            if (exitButton != null) exitButton.interactable = true;
            ApplyMenuButtonFocus();
        }
    }

 
   

    public void AddStar()
    {
        if (starUIPanel == null || starIconPrefab == null) return;
        Instantiate(starIconPrefab, starUIPanel);
        Debug.Log("スターアイコンを左上に追加しました。");
    }

    private void UpdateYesButtonText()
    {
        if (yesButtonText != null)
        {
            // 同意している人数を数える
            int count = 0;
            if (player0Ready) count++;
            if (player1Ready) count++;

            yesButtonText.text = $"はい {count}/2";
        }
    }

   
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

        ApplyMenuButtonFocus();
    }
    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}