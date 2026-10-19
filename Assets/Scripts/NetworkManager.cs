using UnityEngine;
using WebSocket = NativeWebSocket.WebSocket;
using System;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using System.IO;
using static UnityEditor.PlayerSettings;
using static UnityEditor.Rendering.FilterWindow;
using Unity.Netcode;

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

    private int stageIndex = 0;

    // 受け取り用のイベント
    public event Action<NetMessage> OnInitReceived;
    public event Action<NetMessage> OnLobbyStatusReceived;
    public event Action<NetMessage> OnCharacterSelected;
    public event Action<NetMessage> OnStageSelected;
    public event Action<NetMessage> OnPlayerPosReceived;
    public event Action<NetMessage> OnSpawnProjectile;
    public event Action<NetMessage> OnMenuToggleReceived;
    public event Action<NetMessage> OnMenuRetryReceived;
    public event Action<NetMessage> OnMenuExitCancelReceived;
    public event Action<NetMessage> OnStageClearSelected;




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

   


    public void DispatchMessage(string jsonMsg)
    {
        NetMessage msg = JsonUtility.FromJson<NetMessage>(jsonMsg);

        // type や dataType に応じて、登録されているイベントを一斉に発火（通知）する
        if (msg.type == "init")
        {
            OnInitReceived?.Invoke(msg);
        }
        else if (msg.type == "lobby_status")
        {
            OnLobbyStatusReceived?.Invoke(msg);
        }
        else if (msg.type == "char_select")
        {
            OnCharacterSelected?.Invoke(msg);
        }
        else if (msg.type == "stage_select")
        {
            OnStageSelected?.Invoke(msg);
        }
        else if (msg.dataType == "player")
        {
            OnPlayerPosReceived?.Invoke(msg);
        }
        else if(msg.dataType == "spawn_projectile")
        {
            OnSpawnProjectile?.Invoke(msg);
        }
        else if (msg.type == "menu_toggle")
        {
            OnMenuToggleReceived?.Invoke(msg);
        }
        else if (msg.type == "menu_retry")
        {
            OnMenuRetryReceived?.Invoke(msg);
        }
        else if (msg.type == "menu_exit_cancel")
        {
            OnMenuExitCancelReceived?.Invoke(msg);
        }
        else if (msg.type == "clear_select")
        {
            OnStageClearSelected?.Invoke(msg);
        }

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

            DispatchMessage(msg);
        };

        await ws.Connect();
    }

    // ゲーム開始を送る時
    public async void SendGameStart()
    {
        var data = new NetMessage
        {
            type = "lobby_to_char",
            IsStarted = true,
            name_id = myPlayerId,
            room_id = myRoomID,
            index = myPlayerIndex
        };
        await SendJsonAsync(data);
    }

    //　キャラ選択を送る時
    public async void SendCharSelect(int charIndex, bool isReady)
    {
        var data = new NetMessage
        {
            type = "char_select",
            char_index = charIndex,
            is_ready = isReady, 
            index = myPlayerIndex,
            name_id = myPlayerId,
            room_id = myRoomID
        };
        await SendJsonAsync(data);
    }

    //　ステージ選択を送る時
    public async void SendStageSelect(int stageIndex,bool isReady)
    {
        var data = new NetMessage
        {
            type = "stage_select",
            stage_index = stageIndex,
            is_ready = isReady,
            index = myPlayerIndex,
            name_id = myPlayerId,
            room_id = myRoomID

        };
        await SendJsonAsync(data);
    }

    // プレイヤーの位置・移動データを送る時

    public async void SendPlayerPosition(int charIndex, float x, float y, bool isGrounded, bool isFlipX)
    {
        var data = new NetMessage
        {
            dataType = "player",
            index = myPlayerIndex,
            room_id = myRoomID,
            name_id = myPlayerId,
            char_index = charIndex,
            position_x = x,
            position_y = y,
            IsGrounded = isGrounded,
            is_flip_x = isFlipX
        };
        await SendJsonAsync(data);
    }

    public async void SendSpawnProjectile(Vector3 pos, float dir, int bulletIndex)
    {
        var data = new NetMessage
        {
            dataType = "spawn_projectile",
            room_id =   myRoomID,
            char_index = bulletIndex,
            position_x = pos.x,
            position_y = pos.y,
            id = (int)dir

        };
        await SendJsonAsync(data);
    }

    // メニューの開閉を送る時
    public async void SendMenuToggle(int myIndex)
    {
        var data = new NetMessage
        {
            type = "menu_toggle",
            index = myPlayerIndex,
            room_id = myRoomID,
            char_index = myIndex,
        };
        await SendJsonAsync(data);
    }

    public async void SendMenuRetry(int myIndex)
    {
        var data = new NetMessage
        {
            type = "menu_retry",
            index = myPlayerIndex,
            room_id = myRoomID,
            char_index = myIndex,
        };
        await SendJsonAsync(data);
    }

   

    public async void SendMenuExitCansel(int currentMyIndex)
    {
        var data = new NetMessage
        {
            type = "menu_exit_cancel",
            index = myPlayerIndex,
            room_id = myRoomID,
            char_index = currentMyIndex,
    };
        await SendJsonAsync(data);
    }

    public async void SendStageClear(int clearIndex)
    {
        var data = new NetMessage
        {
            type = "clear_select",
            name_id =myPlayerId,
            room_id = myRoomID,
            index = myPlayerIndex,
            IsStarted = false,
            select_index = clearIndex,
        };
        await SendJsonAsync(data);
    }

    // 内部でJsonに変換して送信する共通ヘルパー
    private async Task SendJsonAsync(object payload)
    {
        string json = JsonUtility.ToJson(payload);
        await SendMessageAsync(json);
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