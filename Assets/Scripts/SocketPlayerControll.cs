using UnityEngine;
using System.Threading.Tasks;
using NativeWebSocket;

public class SocketPlayerController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5.0f;

    PlayerManager pm;
    NetworkManager client;
    async Task SendPlayerData(Vector3 pos)
    {
        if (client.ws.State == WebSocketState.Open)
        {
            var playerData = new InGameMoveData
            {
                name_id = pm.myPlayerId,
                position_x = pos.x,
                position_y = pos.y,
            };

            var jsonMsg = JsonUtility.ToJson(playerData);
            await client.ws.SendText(jsonMsg);
        }
    }
    void MovePlayer()
    {
        // 自身がプレイヤーリストに登録された後にチェック
        if (pm.players.ContainsKey(pm.myPlayerId))
        {
            var move = Vector3.zero;

            if (Input.GetKey(KeyCode.W)) move.y += 1;
            if (Input.GetKey(KeyCode.S)) move.y -= 1;
            if (Input.GetKey(KeyCode.A)) move.x -= 1;
            if (Input.GetKey(KeyCode.D)) move.x += 1;

            move *= moveSpeed * Time.deltaTime;

            // 移動処理
            if (move != Vector3.zero)
            {
                var player = pm.players[pm.myPlayerId];
                player.transform.Translate(move);

                // プレイヤー情報をサーバに送信
                _ = SendPlayerData(player.transform.position);
            }
        }
    }

    void Start()
    {
        pm = GameObject.Find("PlayerManager").GetComponent<PlayerManager>();
        client = GameObject.Find("WebSocket").GetComponent<NetworkManager>();
    }

    void Update()
    {
        // 移動処理
        MovePlayer();
    }
}
