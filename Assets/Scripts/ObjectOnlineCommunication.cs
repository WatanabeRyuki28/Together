using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectOnlineCommunication : MonoBehaviour
{
    [SerializeField] private GameObject[] playersPrefab; // 0:赤のPrefab, 1:青のPrefab
    public Dictionary<int, GameObject> players = new Dictionary<int, GameObject>();

    [SerializeField] private GameObject ghostPrefab;
    public Dictionary<int, GameObject> ghostObjects = new Dictionary<int, GameObject>();

    [Header("飛ばすもの（弾など）のPrefabリスト")]
    [SerializeField] private GameObject[] projectilePrefabs; // 0:赤の弾、1:青の弾などを登録

    public Dictionary<int, NetworkIdentity2D> syncObjects = new Dictionary<int, NetworkIdentity2D>();

    void Start()
    {
        if (NetworkManager.Instance != null)
        {
            int myColorIndex = NetworkManager.Instance.myRealSelectedChar;
            if (myColorIndex == -1) myColorIndex = NetworkManager.Instance.myCharaIndex;
            int opponentColorIndex = (myColorIndex == 0) ? 1 : 0;

            Vector3 myStartPos = Vector3.zero;
            Vector3 opponentStartPos = Vector3.zero;

            // 選んだ色（0:赤、1:青）によって初期位置を完全に固定する
            if (myColorIndex == 0)
            {
                myStartPos = Vector3.zero;                  // 赤は中央
                opponentStartPos = new Vector3(2f, 0f, 0f);  // 青は右
            }
            else
            {
                myStartPos = new Vector3(2f, 0f, 0f);       // 青は右
                opponentStartPos = Vector3.zero;            // 赤は中央
            }

            // キャラクターの種類（0か1）をそのまま鍵にして生成
            CreatePlayer(myColorIndex, myStartPos, true);
            CreatePlayer(opponentColorIndex, opponentStartPos, false);
        }

       
        // ステージ内に最初から配置されている NetworkIdentity2D（箱や扉など）をすべて自動探索して登録！
        NetworkIdentity2D[] sceneObjects = FindObjectsOfType<NetworkIdentity2D>();
        foreach (var obj in sceneObjects)
        {
            // すでに登録されていなければ辞書に入れる
            if (!syncObjects.ContainsKey(obj.objectId))
            {
                syncObjects[obj.objectId] = obj;
            }
        }
    }

    public void CreatePlayer(int charaindex, Vector3 pos, bool isLocal)
    {
        var player = Instantiate(playersPrefab[charaindex], pos, Quaternion.identity);

        // キャラクターの種類（0:赤、1:青）をキーにして辞書に保存
        players[charaindex] = player;

        var controller = player.GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.IsLocalPlayer = isLocal;
        }

        if (!isLocal)
        {
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.velocity = Vector2.zero;
            }

            // ゴーストを生成して辞書に保存
            if (ghostPrefab != null)
            {
                var ghost = Instantiate(ghostPrefab, pos, Quaternion.identity);
                ghost.name = $"Ghost_Player_{charaindex}";
                ghostObjects[charaindex] = ghost;
            }
        }
    }

    public void HandleWebSocketMessage(string msg)
    {
        var data = JsonUtility.FromJson<InGameMoveData>(msg);

        if (data.dataType == "player")
        {
            HandlePlayerSync(data);
        }
        else if (data.dataType == "object" || data.dataType == "spawn_projectile")
        {
            // ★弾生成イベント(spawn_projectile)も位置同期(object)も、同じオブジェクト同期関数で処理する
            HandleObjectSync(data);
        }
    }

    private void HandlePlayerSync(InGameMoveData data)
    {
        if (NetworkManager.Instance == null) return;

        // 自分の選んだキャラ情報
        int myRealColor = NetworkManager.Instance.myRealSelectedChar;
        if (myRealColor == -1) myRealColor = NetworkManager.Instance.myCharaIndex;

        // 届いたデータの色が、自分のキャラと同じなら自分のデータなので無視
        if (data.char_index == myRealColor) return;

        // データの部屋IDが自分と違う場合も無視
        if (data.room_id != NetworkManager.Instance.myRoomID) return;

        Vector3 targetPos = new Vector3(data.position_x, data.position_y, 0);

        // 1. まず届いた生データ（ゴーストオブジェクト）をその座標に「瞬間移動」させて可視化する
        if (ghostObjects.ContainsKey(data.char_index))
        {
            ghostObjects[data.char_index].transform.position = targetPos;
        }

        // 2. 本物のキャラクターには、そのゴーストを追尾させるために座標を教える
        if (players.ContainsKey(data.char_index))
        {
            var controller = players[data.char_index].GetComponent<PlayerController>();
            if (controller != null)
            {
               

                // ゴーストの座標を目標地点として設定
                controller.TargetPosition = targetPos;
            }
        }
    }

    private void HandleObjectSync(InGameMoveData data)
    {
        if (NetworkManager.Instance == null) return;
        if (data.room_id != NetworkManager.Instance.myRoomID) return;

        Vector3 targetPos = new Vector3(data.position_x, data.position_y, 0);

        if (data.dataType == "spawn_projectile")
        {
            // データの char_index（撃った人の色）から、出すべき弾のプレハブを決める
            int bulletType = data.char_index;

            if (projectilePrefabs != null && bulletType < projectilePrefabs.Length && projectilePrefabs[bulletType] != null)
            {
                // 送られてきた idを取り出す
                float direction = data.id;
                Quaternion spawnRotation = (direction == -1f) ? Quaternion.identity : Quaternion.Euler(0, 0, 180f);

                // 相手の画面に弾を生成
                GameObject spawnedProjectile = Instantiate(projectilePrefabs[bulletType], targetPos, spawnRotation);

                // 相手の画面の弾にも速度を与えて勝手に飛ばす
                Projectile projectileScript = spawnedProjectile.GetComponent<Projectile>();
                if (projectileScript != null)
                {
                    // 属性は bulletType が 0 なら Fire、1 なら Ice 
                    ElementType bulletElement = (bulletType == 0) ? ElementType.Fire : ElementType.Ice;
                    projectileScript.Initialize(direction, bulletElement);
                   
                }

                Debug.Log("[イベント生成完了] 相手が撃った弾をローカルで発射しました。");
            }
            return; // 弾の処理はここで完全に終了！
        }


        // 既存のオブジェクト、もしくは上で新しく生成された弾の座標を更新する
        if (syncObjects.ContainsKey(data.id))
        {
            var targetObj = syncObjects[data.id];
            targetObj.UpdatePositionFromNetwork(targetPos);
        }
    }
   
}