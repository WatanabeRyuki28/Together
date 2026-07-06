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

    // ★修正：void Start() から IEnumerator Start() に変更
    IEnumerator Start()
    {
        // 1フレームだけ待つことで、カメラ側の初期化を確実に終わらせる
        yield return null;

        // --- 修正：NetworkManager がある場合（オンラインプレイ時） ---
        if (NetworkManager.Instance != null)
        {
            int myColorIndex = NetworkManager.Instance.myRealSelectedChar;
            if (myColorIndex == -1) myColorIndex = NetworkManager.Instance.myCharaIndex;
            int opponentColorIndex = (myColorIndex == 0) ? 1 : 0;

            Vector3 myStartPos = Vector3.zero;
            Vector3 opponentStartPos = Vector3.zero;

            if (myColorIndex == 0)
            {
                myStartPos = Vector3.zero;
                opponentStartPos = new Vector3(2f, -1.5f, 0f);
            }
            else
            {
                myStartPos = new Vector3(2f, -1.5f, 0f);
                opponentStartPos = Vector3.zero;
            }

            CreatePlayer(myColorIndex, myStartPos, true);
            CreatePlayer(opponentColorIndex, opponentStartPos, false);
        }
        // --- ★追加：NetworkManager がない場合（デバッグ・テストプレイ時） ---
        else
        {
            Debug.LogWarning("[警告] NetworkManager が見つかりません。テスト用として赤スライムをローカルプレイヤーとして生成します。");
            // とりあえず「0:赤のPrefab」を自分(true)として生成する
            CreatePlayer(0, Vector3.zero, true);
        }

        // ステージ内のオブジェクト探索
        NetworkIdentity2D[] sceneObjects = FindObjectsOfType<NetworkIdentity2D>();
        foreach (var obj in sceneObjects)
        {
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

        // ========================================================
        // 自分が動かすキャラ（ローカルプレイヤー）だった場合、カメラに追従させる
        // ========================================================
        if (isLocal)
        {
            CameraFollow2D cameraScript = FindObjectOfType<CameraFollow2D>();
            if (cameraScript != null)
            {
                cameraScript.SetupTarget(player.transform);
                Debug.Log($"[カメラ連携完了] 生成されたローカルプレイヤー(Index:{charaindex})をカメラターゲットに設定しました。");
            }
            else
            {
                Debug.LogError("[カメラ連携失敗] シーン内に CameraFollow2D が見つかりません！");
            }
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

        CameraFollow2D cameraFollow = FindFirstObjectByType<CameraFollow2D>();
        if (cameraFollow != null)
        {
            // カメラ側の player1, player2 の割り当てルールに合わせる
            // ローカル（自分）なら myPlayerIndex、リモート（相手）ならその逆を割り当てる
            int assignedCameraIndex = 0;
            if (isLocal)
            {
                assignedCameraIndex = NetworkManager.Instance.myPlayerIndex;
            }
            else
            {
                assignedCameraIndex = (NetworkManager.Instance.myPlayerIndex == 0) ? 1 : 0;
            }

            // カメラに生成したプレイヤーのTransformを直接登録！
            cameraFollow.AssignPlayer(assignedCameraIndex, player.transform);
        }
    }

    public void HandleWebSocketMessage(string msg)
    {
        if (msg.Contains("\"type\":\"menu_toggle\"") || msg.Contains("\"type\":\"menu_exit_ready\""))
        {
            try
            {
                InGameMoveData menuData = JsonUtility.FromJson<InGameMoveData>(msg);

                if (menuData.type == "menu_toggle" && StageMenuManager.Instance != null)
                {
                    StageMenuManager.Instance.ReceiveMenuToggle(menuData.position_x, menuData.char_index);
                    return;
                }
                else if (menuData.type == "menu_exit_ready" && StageMenuManager.Instance != null)
                {
                    StageMenuManager.Instance.ReceiveExitReady(menuData.char_index);
                    return;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"メニューデータの振り分けでエラーが発生: {e.Message}");
            }
        }

        var data = JsonUtility.FromJson<InGameMoveData>(msg);

        if (data.dataType == "player")
        {
            HandlePlayerSync(data);
        }
        else if (data.dataType == "object" || data.dataType == "spawn_projectile")
        {
            HandleObjectSync(data);
        }
    }

    private void HandlePlayerSync(InGameMoveData data)
    {
        if (NetworkManager.Instance == null) return;

        int myRealColor = NetworkManager.Instance.myRealSelectedChar;
        if (myRealColor == -1) myRealColor = NetworkManager.Instance.myCharaIndex;

        if (data.char_index == myRealColor) return;
        if (data.room_id != NetworkManager.Instance.myRoomID) return;

        Vector3 targetPos = new Vector3(data.position_x, data.position_y, 0);

        if (ghostObjects.ContainsKey(data.char_index))
        {
            ghostObjects[data.char_index].transform.position = targetPos;
        }

        if (players.ContainsKey(data.char_index))
        {
            GameObject remotePlayerObj = players[data.char_index];

            var controller = remotePlayerObj.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.TargetPosition = targetPos;
            }

            var remoteSprite = remotePlayerObj.GetComponent<SpriteRenderer>();
            if (remoteSprite != null)
            {
                remoteSprite.flipX = data.is_flip_x;
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
            int bulletType = data.char_index;

            if (projectilePrefabs != null && bulletType < projectilePrefabs.Length && projectilePrefabs[bulletType] != null)
            {
                float direction = data.id;
                Quaternion spawnRotation = (direction == -1f) ? Quaternion.identity : Quaternion.Euler(0, 0, 180f);

                GameObject spawnedProjectile = Instantiate(projectilePrefabs[bulletType], targetPos, spawnRotation);

                Projectile projectileScript = spawnedProjectile.GetComponent<Projectile>();
                if (projectileScript != null)
                {
                    ElementType bulletElement = (bulletType == 0) ? ElementType.Fire : ElementType.Ice;
                    projectileScript.Initialize(direction, bulletElement);
                }

                Debug.Log("[イベント生成完了] 相手が撃った弾をローカルで発射しました。");
            }
            return;
        }

        if (syncObjects.ContainsKey(data.id))
        {
            var targetObj = syncObjects[data.id];
            targetObj.UpdatePositionFromNetwork(targetPos);
        }
    }
}