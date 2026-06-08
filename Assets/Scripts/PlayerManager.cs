using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] GameObject playerPrefab; // プレイヤーを表すPrefab
    public Dictionary<string, GameObject> players = new Dictionary<string, GameObject>(); // プレイヤーの一覧
    public string myPlayerId; // 自分のプレイヤーID
    public string myRoomID;
    public int myPlayerIndex;
    public void CreatePlayer(string id, Vector3 pos ,int index)
    {
        // プレイヤーオブジェクト生成
        var canvas = GameObject.Find("Canvas");
        var player = Instantiate(playerPrefab, pos, Quaternion.identity, canvas.transform);

        // 色指定
        if (index == 0)
        {
            // 自分自身
            player.GetComponent<Image>().color = Color.red;
        }
        else
        {
            // 自分以外
            player.GetComponent<Image>().color = Color.blue;
        }

        // リスト追加
        players[id] = player;
    }
    public void UpdatePlayer(InGameMoveData pd)
    {
        // 位置情報更新
        var player = players[pd.name_id];
        player.transform.position = new Vector3(pd.position_x, pd.position_y, 0);
    }
    public void HandleWebSocketMessage(string msg)
    {
        var playerData = JsonUtility.FromJson<InGameMoveData>(msg);

        if (!players.ContainsKey(playerData.name_id))
        {
            // リストに存在しなければ登録
            CreatePlayer(playerData.name_id, Vector3.zero,playerData.index);
        }
        else
        {
            // 存在すれば位置を移動
            UpdatePlayer(playerData);
        }
    }
}
