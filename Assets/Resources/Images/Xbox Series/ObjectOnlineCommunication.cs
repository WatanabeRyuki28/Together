using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ObjectOnlineCommunication : MonoBehaviour
{
    [SerializeField] private GameObject[] playersPrefab; // 0:赤のPrefab, 1:青のPrefab
    public Dictionary<int, GameObject> players = new Dictionary<int, GameObject>();

    [SerializeField] private GameObject ghostPrefab;
    public Dictionary<int, GameObject> ghostObjects = new Dictionary<int, GameObject>();

    [Header("飛ばすもの（弾など）のPrefabリスト")]
    [SerializeField] private GameObject[] projectilePrefabs; // 0:赤の弾、1:青の弾などを登録

    public Dictionary<int, NetworkIdentity2D> syncObjects = new Dictionary<int, NetworkIdentity2D>();

    // void Start() から IEnumerator Start() に変更
    IEnumerator Start()
    {
        // 1フレームだけ待つことで、カメラ側の初期化を確実に終わらせる
        yield return null;

        // NetworkManager がある場合
        if (NetworkManager.Instance != null)
        {
            int myColorIndex = NetworkManager.Instance.myRealSelectedChar;
            if (myColorIndex == -1) myColorIndex = NetworkManager.Instance.myCharaIndex;
            int opponentColorIndex = (myColorIndex == 0) ? 1 : 0;

            Vector3 myStartPos = Vector3.zero;
            Vector3 opponentStartPos = Vector3.zero;

            string currentSceneName = SceneManager.GetActiveScene().name;

            if (myColorIndex == 0)
            {
                if (currentSceneName == "Stage1"
                        || currentSceneName == "Stage2"
                        || currentSceneName == "Stage3"
                        || currentSceneName == "Stage4"
                        || currentSceneName == "Stage5")
                {
                    myStartPos = new Vector3(-12f, -1.5f, 0f);
                    opponentStartPos = new Vector3(-7f, -1.5f, 0f);
                }
                else if (currentSceneName == "Stage6")
                {
                    myStartPos = new Vector3(-1.5f, -1.5f, 0f);
                    opponentStartPos = new Vector3(1.5f, -1.5f, 0f);
                }

                   
            }
            else
            {
                if (currentSceneName == "Stage1"
                        || currentSceneName == "Stage2"
                        || currentSceneName == "Stage3"
                        || currentSceneName == "Stage4"
                        || currentSceneName == "Stage5")
                {
                    myStartPos = new Vector3(-7f, -1.5f, 0f);
                    opponentStartPos = new Vector3(-12f, -1.5f, 0f);

                }
                else if (currentSceneName == "Stage6")
                {
                    myStartPos = new Vector3(1.5f, -1.5f, 0f);
                    opponentStartPos = new Vector3(-1.5f, -1.5f, 0f);

                }
            }

            CreatePlayer(myColorIndex, myStartPos, true);
            CreatePlayer(opponentColorIndex, opponentStartPos, false);
        }
        // NetworkManager がない場合
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

 /*   public void CreatePlayer(int charaindex, Vector3 pos, bool isLocal)
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
        // 自分が動かすキャラ（ローカルプレイヤー）だった場合
        // ========================================================
        if (!isLocal)
        {
            OffScreenIndicator indicator = FindFirstObjectByType<OffScreenIndicator>(FindObjectsInactive.Include);
            if (indicator != null)
            {
                indicator.gameObject.SetActive(true);
                indicator.SetupTarget(player.transform); // 相手を登録！
                Debug.Log($"[成功] OffScreenIndicator に相手プレイヤー({player.name})をターゲットとして自動登録しました。");
            }
        
        else
            {
                Debug.LogError("[エラー] シーン内に OffScreenIndicator が見つかりませんでした。ヒエラルキーに配置されているか確認してください。");
            }
        }
        else // 自分以外のプレイヤーだった場合
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
*/
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
        // 【修正】ローカルプレイヤー（自分）の場合
        // ========================================================
        if (isLocal)
        {
            // 自分の画面用：もし必要ならカメラ等の処理をここに書く（現状は空でもOK）
        }
        // ========================================================
        // 【修正】リモートプレイヤー（相手）の場合
        // ========================================================
        else
        {
            // 相手キャラの物理演算を止める（Kinematicにする）
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

            // 画面外インジケーターに相手を登録
            OffScreenIndicator indicator = FindFirstObjectByType<OffScreenIndicator>(FindObjectsInactive.Include);
            if (indicator != null)
            {
                indicator.gameObject.SetActive(true);
                indicator.SetupTarget(player.transform); // 相手を登録！
                Debug.Log($"[成功] OffScreenIndicator に相手プレイヤー({player.name})をターゲットとして自動登録しました。");
            }
            else
            {
                Debug.LogError("[エラー] シーン内に OffScreenIndicator が見つかりませんでした。");
            }
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
                    StageMenuManager.Instance.ReceiveExitReady(menuData.char_index);
                    return;
                }
                else if (menuData.type == "menu_exit_ready" && StageMenuManager.Instance != null)
                {
                    StageMenuManager.Instance.ReceiveExitCancel(menuData.char_index);
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

                controller.isGrounded = data.IsGrounded;
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

                if (players != null && players.ContainsKey(bulletType))
                {
                    GameObject remotePlayerObj = players[bulletType];
                    if (remotePlayerObj != null)
                    {
                        PlayerController pc = remotePlayerObj.GetComponent<PlayerController>();

                        // 自分ではないキャラクターの時だけアニメーションを実行
                        if (pc != null && !pc.IsLocalPlayer)
                        {
                            Animator remoteAnim = pc.GetComponent<Animator>();
                            if (remoteAnim != null)
                            {
                                remoteAnim.SetTrigger("Attack"); //  相手のアニメーションを直接再生！
                            }
                        }
                    }
                }
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