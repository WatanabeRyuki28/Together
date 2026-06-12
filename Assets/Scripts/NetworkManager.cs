using UnityEngine;
using WebSocket = NativeWebSocket.WebSocket;
using System;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using System.IO;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    public WebSocket ws;
    public string myPlayerId;
    public string myRoomID;
    public int myPlayerIndex;
    public int myCharaIndex;
    public int myRealSelectedChar = -1;

    [SerializeField]
    private TextAsset IPtextAsset;

    public string targetIp;
    public int targetPort;
    private float waitIp = 0f;

    void Awake()
    {
        LoadConfig();
    }

    void LoadConfig()
    {
       
        string filePath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "IP.txt");

        // ファイルが存在するかチェック
        if (File.Exists(filePath))
        {
            try
            {
                string jsonText = File.ReadAllText(filePath);
                ServerConfig config = JsonUtility.FromJson<ServerConfig>(jsonText);

                targetIp = config.serverIp;
                targetPort = config.port;
                Debug.Log($"接続先IPをexe真横のファイルから読み込みました: {targetIp}:{targetPort}");
            }
            catch (Exception e)
            {
                Debug.LogError($"ファイルの読み込みに失敗: {e.Message}");
                targetIp = "localhost";
            }
        }
        else
        {
            targetIp = "localhost";
            Debug.LogWarning($"設定ファイルがないためlocalhostにします。探した場所: {filePath}");
        }
    }

    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Update()
    {
        
      

#if !UNITY_WEBGL || UNITY_EDITOR
        if (ws != null)
        {
            ws.DispatchMessageQueue();
        }
#endif
    }

    public async void Connect(string playerID, string roomID)
    {
        myPlayerId = playerID;
        myRoomID = roomID;


        ws = new WebSocket($"ws://{targetIp}:{targetPort}/ws?room_id={roomID}&name_id={playerID}");

        ws.OnOpen += () =>
        {
            print("接続成功");
        };

        ws.OnMessage += (bytes) =>
        {
            var msg = System.Text.Encoding.UTF8.GetString(bytes);
            print("受信メッセージ：" + msg);

            //  現在アクティブなシーンの名前を取得する
            string currentSceneName = SceneManager.GetActiveScene().name;

            //  シーン名に応じて、届いたデータの届け先を完全に仕分ける！
            if (currentSceneName == "SecondScene" || currentSceneName == "TopViewScene")
            {
                var client = FindObjectOfType<TopViewClient>();
                if (client != null)
                {
                    client.HandleMessage(msg);
                }
            }
            else if (currentSceneName == "CharacterSelectScene")
            {

                var charManager = FindObjectOfType<CharacterSelectManager>();
                if (charManager != null)
                {
                    charManager.HandleRemoteMessage(msg);
                }
            }

            else if (currentSceneName == "StageSelectScene")
            {
                var stageManager = FindObjectOfType<StageManager>();
                if (stageManager != null)
                {
                    stageManager.HandleRemoteStageMessage(msg);
                }
            }

             else if (currentSceneName == "TutorialStageScene_Backup")
            {
                var onlineComm = FindObjectOfType<ObjectOnlineCommunication>();
                if (onlineComm != null)
                {
                    onlineComm.HandleWebSocketMessage(msg);
                }
                else
                {
                    Debug.LogWarning("ObjectOnlineCommunication が現在のシーンに見つかりません！");
                }
            }

            else if(currentSceneName == "ClearScene")
            {
                var ClearManager = FindObjectOfType<StageClearManager>();
                if (ClearManager != null)
                {
                    ClearManager.HandleClearMessage(msg);
                }
            }
        };

        await ws.Connect();
    }

    public async Task SendMessageAsync(string jsonMsg)
    {
        if (ws != null && ws.State == NativeWebSocket.WebSocketState.Open)
        {
            await ws.SendText(jsonMsg);
        }
        else
        {
            Debug.LogWarning("接続切断");
        }
    }

    private async void OnApplicationQuit()
    {
        if (ws != null)
        {
            await ws.Close();
            DeleteData();
        }
    }

    public void DeleteData()
    {
        ws = null;

        myPlayerId = string.Empty;
        myRoomID = string.Empty;

        myPlayerIndex = -1;



    }
}