using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using Unity.VisualScripting;

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


    private string remotePlayer;

    private CommunicationUI controls;

    void Start()
    {
        Init(true, false);
        HostText.SetActive(false);
        GestText.SetActive(false);
        Startflag = false;
        controls = new CommunicationUI();

        controls.UI.Enable();
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

        // 4. GameStart（Xボタン）の処理
        if (controls.UI.GameStart.triggered)
        {
            if (LobbyPanel != null && LobbyPanel.activeSelf && Startflag == true)
            {
                Debug.Log("-> SendPlayerDataを実行します");
                SendPlayerData();
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

    public void HandleMessage(string msg)
    {
        // 共通のレスポンス構造をチェック
        var res = JsonUtility.FromJson<InitResponse>(msg);

        if (res.type == "init")
        {
           
            // 自分の情報をNetworkManager側に保存してもらう
            NetworkManager.Instance.myPlayerId = res.name_id;
            NetworkManager.Instance.myRoomID = res.room_id;
            NetworkManager.Instance.myPlayerIndex = res.index;

            Debug.Log($"<color=cyan>【システム】接続完了。自分のID: {res.name_id}, 入室順: {res.index}</color>");
            UpdateLobbyUI();
           
            return;

        }
        else if (res.type == "lobby_status")
        {
            // 自分以外のプレイヤーなら、対戦相手として名前を登録する
            if (res.name_id != NetworkManager.Instance.myPlayerId)
            {
                remotePlayer = res.name_id;
              
                UpdateLobbyUI();
            }
            return;
        }
        else
        {
            HandleWebSocketMessage(msg);
        }


    }

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

        // 通信開始をNetworkManagerに依頼する
        NetworkManager.Instance.Connect(playerNameInput, roomIdInput);
    }

    public async void SendPlayerData()
    {
        var initResponse = new InitResponse
        {
            type = "lobby_to_char", 
            name_id = NetworkManager.Instance.myPlayerId,
            room_id = NetworkManager.Instance.myRoomID,
            index = NetworkManager.Instance.myPlayerIndex,
            IsStarted = true,       // 開始フラグ
        };

        var jsonMsg = JsonUtility.ToJson(initResponse);
       
        await NetworkManager.Instance.SendMessageAsync(jsonMsg);
        SceneManager.LoadScene("CharacterSelectScene");
    }

    private void Init(bool IP, bool LP)
    {
        InputPanel.SetActive(IP);
        LobbyPanel.SetActive(LP);
    }

    private void UpdateLobbyUI()
    {
        MatchNowflag = false;

        int myIndex = NetworkManager.Instance.myPlayerIndex;
        string myId = NetworkManager.Instance.myPlayerId;

        

        if (myIndex == 0)
        {
            P1Text.text = myId;
            P2Text.text = string.IsNullOrEmpty(remotePlayer) ? "待機中..." : remotePlayer;
        }
        else if (myIndex == 1)
        {
            P1Text.text = string.IsNullOrEmpty(remotePlayer) ? "待機中..." : remotePlayer;
            P2Text.text = myId;
        }

        CheckStartButtonCondition();
    }

    private void HandleWebSocketMessage(string msg)
    {
        var playerData = JsonUtility.FromJson<InGameMoveData>(msg);

        if (playerData.IsStarted)
        {
            // ここで次のシーンへ！NetworkManagerは生き残ります
            SceneManager.LoadScene("CharacterSelectScene");
            UIInit();
            return;
        }

        if (playerData.name_id != NetworkManager.Instance.myPlayerId)
        {
            remotePlayer = playerData.name_id;
            UpdateLobbyUI();
        }
      
    }

    private void CheckStartButtonCondition()
    {
        bool isP1Ready = !string.IsNullOrEmpty(P1Text.text) && P1Text.text != "待機中...";
        bool isP2Ready = !string.IsNullOrEmpty(P2Text.text) && P2Text.text != "待機中...";

        // 自分がホスト（Index 0）かつ、両方準備できたらボタン表示
        if (NetworkManager.Instance.myPlayerIndex == 0 && isP1Ready && isP2Ready)
        {
            HostText.SetActive(true);
            GestText.SetActive(false);
            Startflag = true;
        }
        else
        {
            HostText.SetActive(false);
            GestText.SetActive(true);
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
        remotePlayer = string.Empty;

        P1Text.text = string.Empty;
        P2Text.text = string.Empty;
        HostText.SetActive(false);
        GestText.SetActive(false);

        Init(true, false);
    }
}