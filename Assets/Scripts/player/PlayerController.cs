using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Threading.Tasks;
using NativeWebSocket;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(AudioSource))]
public class PlayerController : MonoBehaviour
{
    [Header("操作方法の設定 (チェックONで有効)")]
    [SerializeField] private bool useKeyboard = true;  // キーボードを使うか
    [SerializeField] private bool useGamepad = false;  // ゲームパッドを使うか

    // 【方法Aのための追加】インスペクターから1Pか2Pかを設定する変数
    [Header("プレイヤー番号の設定")]
    [Tooltip("1Pなら1、2Pなら2を設定してください")]
    public int playerNumber = 1;

    [Header("キャラクターの属性")]
    [SerializeField] private ElementType element; // 属性設定：Fire（炎）または Ice（氷）
    public ElementType Element => element;       // 他のクラスから属性を確認するための公開プロパティ

    [Header("移動・ジャンプ設定")]
    [SerializeField] private float moveSpeed = 5.0f;           // 基本の移動速度
    [SerializeField] private float pushSpeedMultiplier = 0.5f; // オブジェクト押し出し中の移動速度倍率（例: 0.5なら速度半分）
    [SerializeField] private float jumpForce = 6.5f;           // ジャンプ時に加える力の強さ

    [Header("射撃（クールタイム）設定")]
    [SerializeField] private float fireRate = 0.3f; // 次の弾を撃つまでに必要な待機時間（秒）
    private float nextFireTime = 0f;                // 次に発射が可能になる時刻の記録用

    [Header("効果音（SE）設定")]
    [SerializeField] private AudioClip jumpSound;    // ジャンプ音
    [SerializeField] private AudioClip shootSound;   // 射撃音
    [SerializeField] private AudioClip walkSound;    // 足音（ループ用）

    // ★【Input Manager完全排除】使用するキーをコード側で固定
    private KeyCode leftKey = KeyCode.A;       // 左移動
    private KeyCode rightKey = KeyCode.D;      // 右移動
    private KeyCode jumpKey = KeyCode.Space;       // ジャンプ
    private KeyCode fireKey = KeyCode.Return;   // ★【変更】攻撃をスペースキーに固定（マウス左クリックなら KeyCode.Mouse0）

    [Header("各種参照設定")]
    [SerializeField] private GameObject projectilePrefab; // 発射する弾のプレハブ
    [SerializeField] private Transform firePoint;          // 弾が生成（出現）するポイント
    [SerializeField] private LayerMask groundLayer;       // 地面判定を行う対象レイヤー
    [SerializeField] private LayerMask pushableLayer;      // 押し出し可能なオブジェクトのレイヤー

    public bool CanMove { get; set; } = true;

    private Rigidbody2D rb;
    private Animator anim;                 // アニメーター用
    private SpriteRenderer spriteRenderer; // 左右反転用
    private AudioSource audioSource;       // ★効果音再生用

    public bool isGrounded { get; set; }
    private bool isPushing;

    NetworkManager client;

    public bool IsLocalPlayer { get; set; } = true;
    private NetworkManager networkManager;

    // 前回の座標を記録
    private Vector2 lastPosition;

    public Vector3 TargetPosition { get; set; }

    [SerializeField] private int projectilePrefabIndex;
    private int projectileCount = 0;

    private CommunicationUI controls;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>(); // 取得

        // AudioSourceの初期設定（3Dサウンドではなく2Dとして手軽にハッキリ鳴らす）
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        controls = new CommunicationUI();
    }

    void OnEnable()
    {
        // 自分のキャラ（ローカルプレイヤー）の時だけ入力を有効にする
        if (IsLocalPlayer && controls != null)
        {
            controls.Player.Enable(); // アクションマップ名「Player」を有効化
        }
    }

    void OnDisable()
    {
        if (controls != null)
        {
            controls.Player.Disable();
        }
    }

    void Start()
    {
        if (NetworkManager.Instance != null)
        {
            networkManager = NetworkManager.Instance;
        }
        else
        {
            Debug.LogError("NetworkManagerが見つかりません！");
        }

        lastPosition = transform.position;

        CameraFollow2D cameraFollow = FindFirstObjectByType<CameraFollow2D>();

        if (cameraFollow != null)
        {
            // 自分が操作するキャラ（IsLocalPlayer == true）なら 0番（プレイヤー1）
            // 通信相手のキャラ（IsLocalPlayer == false）なら 1番（プレイヤー2）にする
            int cameraIndex = IsLocalPlayer ? 0 : 1;

            cameraFollow.AssignPlayer(cameraIndex, this.transform);
            string playerType = IsLocalPlayer ? "【自分（ローカル）】" : "【相手（リモート）】";
            Debug.Log($"[カメラ登録ログ] オブジェクト名: {gameObject.name} | 属性: {playerType} ➔ カメラ番号 {cameraIndex} 番に登録しました！");
        
        }
    }

    void Update()
    {
        // 相手（リモート）のキャラクターの場合
        if (!IsLocalPlayer)
        {

            Debug.Log($"現在の接地状態: {isGrounded}");

            if (rb != null)
            {
                rb.velocity = Vector2.zero; // 物理干渉による荒ぶりを完全カット
            }

            // 移動する前の座標を一時保存（アニメーションの速度計算用）
            Vector3 previousPosition = transform.position;

            // 線形補間で位置を同期
            transform.position = Vector3.Lerp(transform.position, TargetPosition, 0.05f);

            // 実際の移動距離から、相手の擬似的な速度を計算して Animator に反映する
            Vector2 simulatedVelocity = (transform.position - previousPosition) / Time.deltaTime;

            // 相手のアニメーション更新を呼び出す
            UpdateAnimationParametersForRemote(simulatedVelocity, this.isGrounded);

            // 相手が歩いている時の足音をミュート
            if (audioSource.isPlaying && audioSource.clip == walkSound)
            {
                audioSource.Stop();
            }
            return; 
        }

        if (StageMenuManager.Instance != null && StageMenuManager.Instance.isMenuOpen)
        {
            // 移動速度をゼロにしてその場に立ち止まらせる
            rb.velocity = new Vector2(0f, rb.velocity.y);
            UpdateAnimationParameters(rb.velocity);

            // 足音を止める
            if (audioSource.isPlaying && audioSource.clip == walkSound)
            {
                audioSource.Stop();
            }
            return; // これ以降の操作入力を一切無視する
        }


        if (CanMove)
        {
            // 毎フレーム移動処理を呼び出し
            Move();

            // ジャンプ判定 (キーボード or ゲームパッド)
            bool keyboardJump = useKeyboard && Input.GetKeyDown(jumpKey);
            bool gamepadJump = useGamepad && controls.Player.Jump.triggered;

            if ((keyboardJump || gamepadJump) && isGrounded)
            {
                Jump();
            }

            // 攻撃判定 (キーボード or ゲームパッド)
            bool keyboardFire = useKeyboard && Input.GetKeyDown(fireKey);
            bool gamepadFire = useGamepad && controls.Player.Tama.triggered;

            if (keyboardFire || gamepadFire)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                    return;
                }

                Shoot();
            }

            bool keyboardMenu = useKeyboard && Input.GetKeyDown(KeyCode.Escape); // キーボードのEscキーなど
            bool gamepadMenu = useGamepad && controls.Player.Menu.triggered;    

            if (keyboardMenu || gamepadMenu)
            {
                if (StageMenuManager.Instance != null)
                {
                    StageMenuManager.Instance.ToggleMenu();
                }
            }
        }

        // 毎フレーム最新の状態をAnimatorに送信
        UpdateAnimationParameters(rb.velocity);
        // 足音の再生コントロール
        HandleWalkSound();

        bool groundStateChanged = isGrounded != anim.GetBool("isGround");

        if (Vector2.Distance(transform.position, lastPosition) > 0.01f)
        {
            SendPlayerData(transform.position, isGrounded);
            lastPosition = transform.position; // 記録を更新
        }
    }

    private void Move()
    {
        float moveInput = 0f;

        // キーボードの入力を加算
        if (useKeyboard)
        {
            if (Input.GetKey(leftKey)) moveInput -= 1f;
            if (Input.GetKey(rightKey)) moveInput += 1f;
        }

        // ゲームパッドの入力を加算 (両方ONならスティックもキーボードも両方効きます)
        if (useGamepad && controls != null)
        {
            float gamepadInput = controls.Player.Move.ReadValue<float>();

            // デッドゾーン（遊び）の設定: スティックの傾きが 0.2 未満なら完全に 0 にする
            if (Mathf.Abs(gamepadInput) < 0.2f)
            {
                gamepadInput = 0f;
            }

            // キーボード入力がなければ、ゲームパッドの入力をそのまま採用する
            if (moveInput == 0f)
            {
                moveInput = gamepadInput;
            }
        }

        moveInput = Mathf.Clamp(moveInput, -1f, 1f);

        // 「箱に触れている」かつ「箱がある方向にキーを入力している」時だけ、本当に押していると判定
        bool isActuallyPushing = isPushing && IsInputtingTowardsBox(moveInput);

        // 押し状態なら速度を下げ、そうでなければ通常の速度を適用
        float currentSpeed = isActuallyPushing ? moveSpeed * pushSpeedMultiplier : moveSpeed;

        // 左右の速度を設定（y軸は現在の物理挙動を維持）
        rb.velocity = new Vector2(moveInput * currentSpeed, rb.velocity.y);


        if (Mathf.Abs(moveInput) < 0.01f)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }
        else
        {
            rb.velocity = new Vector2(moveInput * currentSpeed, rb.velocity.y);
        }

        if (moveInput > 0.1f)
        {
            spriteRenderer.flipX = false;
        }
        else if (moveInput < -0.1f)
        {
            spriteRenderer.flipX = true;
        }
    }

    // 入力した方向に箱があるかどうかを確認する判定（Raycastを使用）
    private bool IsInputtingTowardsBox(float moveInput)
    {
        if (moveInput == 0) return false;

        float checkDistance = 0.5f; // キャラクターから前方どれくらいの距離まで確認するか
        RaycastHit2D hit = Physics2D.Raycast(transform.position, new Vector2(moveInput, 0), checkDistance, pushableLayer);

        return hit.collider != null; // 当たったものがあればtrueを返す
    }

    private void Jump()
    {
        // ジャンプの瞬間に縦方向の速度をリセット
        rb.velocity = new Vector2(rb.velocity.x, 0);
        // 上方向に瞬間的な力を加える
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        // ジャンプ音を再生（他の音をぶった切って最優先で鳴らす）
        if (jumpSound != null)
        {
            audioSource.PlayOneShot(jumpSound);
        }
    }

    private void Shoot()
    {
        // クールタイム判定
        if (Time.time < nextFireTime) return;

        // プレハブや発射地点が未設定ならエラー防止のため中断
        if (projectilePrefab == null || firePoint == null) return;

        anim.SetTrigger("Attack");

        float moveInput = 0f;
        if (useKeyboard)
        {
            if (Input.GetKey(leftKey)) moveInput -= 1f;
            if (Input.GetKey(rightKey)) moveInput += 1f;
        }
        if (useGamepad && controls != null)
        {
            moveInput += controls.Player.Move.ReadValue<float>();
        }

        float direction = 1f; // 基本は右向き

        if (moveInput < -0.1f)
        {
            direction = -1f; // 左キーが押されていれば確実に左向き
        }
        else if (moveInput > 0.1f)
        {
            direction = 1f;  // 右キーが押されていれば確実に右向き
        }
        else
        {
            // キーが押されていない時は、現在のキャラの反転状態（見た目）に合わせる
            direction = spriteRenderer.flipX ? -1f : 1f;
        }

        // 弾の画像（プレハブ）の向きの決定（バック撃ち対策済み）
        Quaternion spawnRotation = (direction == -1f) ? Quaternion.identity : Quaternion.Euler(0, 0, 180f);

        // 決定した正しい位置と角度で、自分の画面に弾を生成
        GameObject projectileObj = Instantiate(projectilePrefab, firePoint.position, spawnRotation);

        // 弾スクリプト（Projectile）へ「確定した方向」と「属性」を送信して初期化
        Projectile projectileScript = projectileObj.GetComponent<Projectile>();
        if (projectileScript != null)
        {
            projectileScript.Initialize(direction, element);
        }

        // 射撃音を再生（移動の足音などと重なっても綺麗に鳴るPlayOneShot）
        if (shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        int myBulletIndex = (element == ElementType.Fire) ? 0 : 1;

        // 弾の座標、向きを相手に送る
        SendSpawnProjectileEvent(firePoint.position, direction, myBulletIndex);

        // 次に撃てる時刻を更新
        nextFireTime = Time.time + fireRate;
    }

    // 足音のループ管理
    private void HandleWalkSound()
    {
        if (walkSound == null) return;

        // 「地面にいて」「左右の移動速度が一定以上（動いている）」とき
        if (isGrounded && Mathf.Abs(rb.velocity.x) > 0.2f)
        {
            // まだ足音が鳴っていないなら再生を始める
            if (!audioSource.isPlaying)
            {
                audioSource.clip = walkSound;
                audioSource.loop = true; // ループを有効化
                audioSource.Play();
            }
        }
        else
        {
            // 止まった、あるいは空中に浮いたときは、足音が鳴っていたら止める
            if (audioSource.isPlaying && audioSource.clip == walkSound)
            {
                audioSource.Stop();
            }
        }
    }

   

    // 衝突開始時の判定
    private void OnCollisionEnter2D(Collision2D collision) => CheckContact(collision, true);
    // 衝突終了時の判定
    private void OnCollisionExit2D(Collision2D collision) => CheckContact(collision, false);

    // 衝突している相手が床か箱かを確認し、状態を更新する
    private void CheckContact(Collision2D collision, bool state)
    {
        int layer = collision.gameObject.layer;

        bool isGroundLayer = ((1 << layer) & groundLayer) != 0;
        bool isPushableLayer = ((1 << layer) & pushableLayer) != 0;

        if (isGroundLayer)
        {
            if (state) // 接触した（Enter / Stay）とき
            {
                foreach (ContactPoint2D contact in collision.contacts)
                {
                    float minGroundAngleY = 0.5f;

                    if (contact.normal.y >= minGroundAngleY)
                    {
                        isGrounded = true;

                        if (IsLocalPlayer)
                        {
                            SendPlayerData(transform.position, true);
                        }
                        break;
                    }
                }
            }
            else // 離れた（Exit）とき
            {
                isGrounded = false;

                if (IsLocalPlayer)
                {
                    SendPlayerData(transform.position, false);
                }
            }
        }

        if (isPushableLayer) isPushing = state;
    }

    // 接触し続けている間も判定を毎フレームケアする（Enterの取りこぼし対策）
    private void OnCollisionStay2D(Collision2D collision) => CheckContact(collision, true);

    // プレイヤーの位置を送る
    async void SendPlayerData(Vector3 pos, bool groundedStatus)
    {
        if (networkManager == null) return;

        InGameMoveData playerData = new InGameMoveData();

        playerData.dataType = "player";
        playerData.room_id = networkManager.myRoomID;

        int myRealChara = networkManager.myRealSelectedChar;

        if (myRealChara == -1)
        {
            myRealChara = (Element == ElementType.Fire) ? 0 : 1;
        }
        playerData.char_index = myRealChara;

        playerData.position_x = pos.x;
        playerData.position_y = pos.y;

        playerData.is_flip_x = spriteRenderer.flipX;

        playerData.IsGrounded = groundedStatus;

        var jsonMsg = JsonUtility.ToJson(playerData);
        await networkManager.SendMessageAsync(jsonMsg);
    }

    private async void SendSpawnProjectileEvent(Vector3 pos, float dir, int bulletIndex)
    {
        if (networkManager == null) return;

        InGameMoveData spawnData = new InGameMoveData();
        spawnData.dataType = "spawn_projectile";
        spawnData.room_id = networkManager.myRoomID;

        spawnData.char_index = bulletIndex;

        spawnData.position_x = pos.x;
        spawnData.position_y = pos.y;
        spawnData.id = (int)dir;

        string json = JsonUtility.ToJson(spawnData);
        await networkManager.SendMessageAsync(json);
    }

    private void UpdateAnimationParameters(Vector2 velocity)
    {
        if (anim == null) return;

        float currentHorizontalSpeed = Mathf.Abs(velocity.x);
        anim.SetFloat("Speed", currentHorizontalSpeed);
        anim.SetBool("isGround", isGrounded);
        anim.SetFloat("yVelocity", velocity.y);
    }

    // リモートプレイヤー用のアニメーション更新
    private void UpdateAnimationParametersForRemote(Vector2 simulatedVelocity, bool remoteIsGrounded)
    {
        if (anim == null) return;

        float currentHorizontalSpeed = Mathf.Abs(simulatedVelocity.x);

        // Lerp移動による細かなガタつき（微小な移動速度）をカット
        if (currentHorizontalSpeed < 0.2f) currentHorizontalSpeed = 0f;

        anim.SetFloat("Speed", currentHorizontalSpeed);

        // 通信から直接受け取った正確な接地状態をアニメーターに流し込む
        anim.SetBool("isGround", remoteIsGrounded);

        anim.SetFloat("yVelocity", simulatedVelocity.y);
    }
}