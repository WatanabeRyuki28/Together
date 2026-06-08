using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class TopViewClient : MonoBehaviour
{
    [SerializeField] InputField inputPlayerName;
    [SerializeField] InputField inputRoomId;

    public GameObject InputPanel;
    public GameObject LobbyPanel;

    public Text P1Text;
    public Text P2Text;
    public GameObject StartButton;

    private string remotePlayer;

    void Start()
    {
        Init(true, false);
        StartButton.SetActive(false);

   
    }

    private void OnDestroy()
    {
       
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
            StartButton.SetActive(true);
        }
        else
        {
            StartButton.SetActive(false);
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

        NetworkManager.Instance.DeleteData();

        UIInit();
        
    }

    private void UIInit()
    {
        if (inputPlayerName != null) inputPlayerName.text = string.Empty;
        if (inputRoomId != null) inputRoomId.text = string.Empty;

        remotePlayer = string.Empty;

        P1Text.text = string.Empty;
        P2Text.text = string.Empty;
        StartButton.SetActive(false);

        Init(true, false);
    }
}