using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System;

public class TopViewClient : MonoBehaviour
{
    [SerializeField] InputField inputPlayerName;
    [SerializeField] InputField inputRoomId;

    public GameObject InputPanel;
    public GameObject LobbyPanel;

    bool MatchNowflag;
    bool Startflag;

    public Text P1Text;
    public Text P2Text;
    public GameObject HostText;
    public GameObject GestText;

    [SerializeField] private GameObject WaitingForGuestText;

    private CommunicationUI controls;

    void Start()
    {
        Init(true, false);
        HostText.SetActive(false);
        GestText.SetActive(false);
        WaitingForGuestText.SetActive(false);

        Startflag = false;
        controls = new CommunicationUI();
        controls.UI.Enable();

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnInitReceived -= HandleInit;
            NetworkManager.Instance.OnInitReceived += HandleInit;

            NetworkManager.Instance.OnLobbyStatusReceived -= HandleLobbyStatus;
            NetworkManager.Instance.OnLobbyStatusReceived += HandleLobbyStatus;

            Debug.Log("<color=green>【TopViewClient】NetworkManagerのイベントを登録しました</color>");
        }
        else
        {
            Debug.LogError("【TopViewClient】NetworkManager.Instanceが見つかりません！");
        }
    }

    void Update()
    {
        GameObject keyboardMenu = GameObject.Find("CanvasKeyborad");
        if (keyboardMenu != null && keyboardMenu.activeSelf)
        {
            return;
        }

        if (controls.UI.Match.triggered)
        {
            if (InputPanel != null && InputPanel.activeSelf && !LobbyPanel.activeSelf)
            {
                Debug.Log("-> PushJoinButtonを実行します");
                PushJoinButton();
            }
        }

        if (controls.UI.Cansel.triggered)
        {
            if (LobbyPanel != null && LobbyPanel.activeSelf)
            {
                Debug.Log("-> DeleteDataButtonを実行します");
                DeleteDataButton();
            }
        }

        // GameStart（Xボタン）の処理

        if (controls.UI.GameStart.triggered)
        {
            if (LobbyPanel != null && LobbyPanel.activeSelf && Startflag == true)
            {
                Debug.Log("-> SendPlayerDataを実行します");
                SendGameStartMessage();
            }
        }
    }

    private void OnDestroy()
    {
        if (controls != null)
        {
            controls.UI.Disable();
            controls = null;
        }
    }

    private void OnEnable()
    {
      
    }

    private void OnDisable()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnInitReceived -= HandleInit;
            NetworkManager.Instance.OnLobbyStatusReceived -= HandleLobbyStatus;
        }
    }

    // 自分の初期情報（init）を受け取ったとき
    private void HandleInit(NetMessage msg)
    {
        NetworkManager.Instance.myPlayerId = msg.name_id;
        NetworkManager.Instance.myRoomID = msg.room_id;
        NetworkManager.Instance.myPlayerIndex = msg.index;

        Debug.Log($"<color=cyan>【システム】接続完了。自分のID: {msg.name_id}, 入室順: {msg.index}</color>");

        // 自分のインデックス（0 または 1）の場所に名前を反映
        UpdatePlayerUI(msg.index, msg.name_id);
    }

    // サーバーからロビーの状態（lobby_status）を受け取ったとき
    private void HandleLobbyStatus(NetMessage msg)
    {
        Debug.Log($"<color=red>★HandleLobbyStatusが呼ばれたよ！ 名前: {msg.name_id}, インデックス: {msg.index}, IsStarted: {msg.IsStarted}</color>");

        // ★★★ ここを追加：ゲーム開始の合図が来たら最優先でシーンを切り替える ★★★
        if (msg.IsStarted)
        {
            Debug.Log("<color=green>【シーン移行】ゲーム開始シグナルを受信しました。キャラクターセレクトへ移動します</color>");
            SceneManager.LoadScene("CharacterSelectScene");
            return;
        }

        if (msg.index == 0) P1Text.text = msg.name_id;
        else if (msg.index == 1) P2Text.text = msg.name_id;

        CheckStartButtonCondition();
    }

    #region 送信
    public void SendGameStartMessage()
    {
        NetworkManager.Instance.SendGameStart();
        SceneManager.LoadScene("CharacterSelectScene");
    }
    #endregion

    public void PushJoinButton()
    {
        var playerNameInput = inputPlayerName.text;
        var roomIdInput = inputRoomId.text;

        Debug.Log($"接続試行: Name={playerNameInput}, Room={roomIdInput}");

        if (string.IsNullOrEmpty(roomIdInput) || string.IsNullOrEmpty(playerNameInput))
        {
            print("ルームID、プレイヤー名は必須です");
            return;
        }

        Init(false, true);

        // ボタンを押した瞬間に自分の名前をP1へ仮置きし、相方を「待機中...」にする
        P1Text.text = playerNameInput;
        P2Text.text = "待機中...";

        // 通信開始をNetworkManagerに依頼する
        NetworkManager.Instance.Connect(playerNameInput, roomIdInput);
    }

    private void Init(bool IP, bool LP)
    {
        InputPanel.SetActive(IP);
        LobbyPanel.SetActive(LP);
    }

    // インデックスごとにテキストを割り当て、スタートボタンの条件をチェックする
    private void UpdatePlayerUI(int index, string playerName)
    {
        if (index == 0)
        {
            P1Text.text = playerName;
        }
        else if (index == 1)
        {
            P2Text.text = playerName;
        }

        CheckStartButtonCondition();
    }

    private void CheckStartButtonCondition()
    {
        bool isP1Ready = !string.IsNullOrEmpty(P1Text.text) && P1Text.text != "待機中...";
        bool isP2Ready = !string.IsNullOrEmpty(P2Text.text) && P2Text.text != "待機中...";

        int myIndex = NetworkManager.Instance.myPlayerIndex;

        // 自分がホスト（P1）の場合
        if (myIndex == 0)
        {
            GestText.SetActive(false);

            if (isP1Ready && isP2Ready)
            {
                HostText.SetActive(true);
                if (WaitingForGuestText != null) WaitingForGuestText.SetActive(false);
                Startflag = true;
            }
            else
            {
                HostText.SetActive(false);
                if (WaitingForGuestText != null) WaitingForGuestText.SetActive(true);
                Startflag = false;
            }
        }
        // 自分がゲスト（P2）の場合
        else
        {
            HostText.SetActive(false);
            if (WaitingForGuestText != null) WaitingForGuestText.SetActive(false);
            GestText.SetActive(true);
            Startflag = false;
        }
    }

    public async void DeleteDataButton()
    {
        if (NetworkManager.Instance.ws != null)
        {
            Debug.Log("サーバー接続を切断中...");
            await NetworkManager.Instance.ws.Close();
            Debug.Log("サーバー接続完了");
        }
        MatchNowflag = true;
        NetworkManager.Instance.DeleteData();

        UIInit();
    }

    private void UIInit()
    {
        P1Text.text = string.Empty;
        P2Text.text = string.Empty;
        HostText.SetActive(false);
        GestText.SetActive(false);

        Init(true, false);
    }
}