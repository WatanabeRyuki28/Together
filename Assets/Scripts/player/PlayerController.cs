using UnityEngine;
using UnityEngine.EventSystems;
using System.Threading.Tasks;
using NativeWebSocket;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(AudioSource))]
public class PlayerController : MonoBehaviour
{
    // --- 定数定義（マジックナンバー排除） ---
    private const float MinMoveInputThreshold = 0.01f;
    private const float MinFlipInputThreshold = 0.1f;
    private const float StickDeadZone = 0.2f;
    private const float MinGroundAngleY = 0.5f;
    private const float BoxCheckDistance = 0.5f;
    private const float PositionSyncThreshold = 0.01f;
    private const float RemoteSpeedThreshold = 0.2f;
    private const float RemoteLerpFactor = 0.05f;
    private const float DefaultWaterCheckDistance = 1.5f; // 足元の水エリア検知距離

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
    [SerializeField] private float pushSpeedMultiplier = 0.5f; // オブジェクト押し出し中の移動速度倍率
    [SerializeField] private float jumpForce = 6.5f;           // ジャンプ時に加える力の強さ

    [Header("射撃（クールタイム）設定")]
    [SerializeField] private float fireRate = 0.3f; // 次の弾を撃つまでに必要な待機時間（秒）
    private float nextFireTime = 0f;                // 次に発射が可能になる時刻の記録用

    [Header("氷柱生成（アイスメーカー）設定")]
    [SerializeField] private GameObject icePillarPrefab; // 生成する氷柱のプレハブ
    [SerializeField] private LayerMask waterLayer;       // 水エリアのレイヤー
    [SerializeField] private float waterCheckDistance = DefaultWaterCheckDistance; // Raycastの長さ

    [Header("効果音（SE）設定")]
    [SerializeField] private AudioClip jumpSound;        // ジャンプ音
    [SerializeField] private AudioClip shootSound;       // 射撃音
    [SerializeField] private AudioClip walkSound;        // 足音（ループ用）
    [SerializeField] private AudioClip createPillarSound; // 氷柱生成音
    [SerializeField] private AudioClip failSound;         // 氷柱生成失敗音

    // ★【Input Manager完全排除】使用するキーをコード側で固定
    private KeyCode leftKey = KeyCode.A;         // 左移動
    private KeyCode rightKey = KeyCode.D;        // 右移動
    private KeyCode jumpKey = KeyCode.Space;     // ジャンプ
    private KeyCode fireKey = KeyCode.Return;    // 攻撃
    private KeyCode pillarKey = KeyCode.E;       // ★氷柱生成キー

    [Header("各種参照設定")]
    [SerializeField] private GameObject projectilePrefab; // 発射する弾のプレハブ
    [SerializeField] private Transform firePoint;          // 弾が生成（出現）するポイント
    [SerializeField] private LayerMask groundLayer;        // 地面判定を行う対象レイヤー
    [SerializeField] private LayerMask pushableLayer;      // 押し出し可能なオブジェクトのレイヤー

    public bool CanMove { get; set; } = true;

    private Rigidbody2D rb;
    private Animator anim;                 // アニメーター用
    private SpriteRenderer spriteRenderer; // 左右反転用
    private AudioSource audioSource;       // 効果音再生用

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
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        controls = new CommunicationUI();
    }

    void OnEnable()
    {
        if (IsLocalPlayer && controls != null)
        {
            controls.Player.Enable();
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

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = IsLocalPlayer ? 51 : 50;
        }
        if (cameraFollow != null)
        {
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
                rb.velocity = Vector2.zero;
            }

            Vector3 previousPosition = transform.position;
            transform.position = Vector3.Lerp(transform.position, TargetPosition, RemoteLerpFactor);

            Vector2 simulatedVelocity = (transform.position - previousPosition) / Time.deltaTime;
            UpdateAnimationParametersForRemote(simulatedVelocity, this.isGrounded);

            if (audioSource.isPlaying && audioSource.clip == walkSound)
            {
                audioSource.Stop();
            }
            return;
        }

        if (StageMenuManager.Instance != null && StageMenuManager.Instance.isIntroPlaying)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            UpdateAnimationParameters(rb.velocity);

            if (audioSource.isPlaying && audioSource.clip == walkSound)
            {
                audioSource.Stop();
            }
            return;
        }

        if (StageMenuManager.Instance != null && StageMenuManager.Instance.isMenuOpen)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            UpdateAnimationParameters(rb.velocity);

            if (audioSource.isPlaying && audioSource.clip == walkSound)
            {
                audioSource.Stop();
            }
            return;
        }

        if (CanMove)
        {
            Move();

            // ジャンプ判定
            bool keyboardJump = useKeyboard && Input.GetKeyDown(jumpKey);
            bool gamepadJump = useGamepad && controls.Player.Jump.triggered;

            if ((keyboardJump || gamepadJump) && isGrounded)
            {
                Jump();
            }

            // 攻撃判定
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

            // ★ 氷柱生成判定 (キーボード E キー または ゲームパッド追加ボタン)
            bool keyboardPillar = useKeyboard && Input.GetKeyDown(pillarKey);
            // ゲームパッドにアクション追加済みの場合は || controls.Player.Pillar.triggered などを併用可能
            if (keyboardPillar)
            {
                TrySpawnIcePillarOnWater();
            }

            // メニュー判定
            bool keyboardMenu = useKeyboard && Input.GetKeyDown(KeyCode.Escape);
            bool gamepadMenu = useGamepad && controls.Player.Menu.triggered;

            if (keyboardMenu || gamepadMenu)
            {
                if (StageMenuManager.Instance != null)
                {
                    StageMenuManager.Instance.ToggleMenu();
                }
            }
        }

        UpdateAnimationParameters(rb.velocity);
        HandleWalkSound();

        if (Vector2.Distance(transform.position, lastPosition) > PositionSyncThreshold)
        {
            SendPlayerData(transform.position, isGrounded);
            lastPosition = transform.position;
        }
    }

    private void Move()
    {
        float moveInput = 0f;

        if (useKeyboard)
        {
            if (Input.GetKey(leftKey)) moveInput -= 1f;
            if (Input.GetKey(rightKey)) moveInput += 1f;
        }

        if (useGamepad && controls != null)
        {
            float gamepadInput = controls.Player.Move.ReadValue<float>();

            if (Mathf.Abs(gamepadInput) < StickDeadZone)
            {
                gamepadInput = 0f;
            }

            if (moveInput == 0f)
            {
                moveInput = gamepadInput;
            }
        }

        moveInput = Mathf.Clamp(moveInput, -1f, 1f);

        if (Mathf.Abs(moveInput) < MinMoveInputThreshold)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }
        else
        {
            bool isActuallyPushing = isPushing && IsInputtingTowardsBox(moveInput);
            float currentSpeed = isActuallyPushing ? moveSpeed * pushSpeedMultiplier : moveSpeed;
            rb.velocity = new Vector2(moveInput * currentSpeed, rb.velocity.y);
        }

        if (moveInput > MinFlipInputThreshold)
        {
            spriteRenderer.flipX = false;
        }
        else if (moveInput < -MinFlipInputThreshold)
        {
            spriteRenderer.flipX = true;
        }
    }

    private bool IsInputtingTowardsBox(float moveInput)
    {
        if (moveInput == 0) return false;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, new Vector2(moveInput, 0), BoxCheckDistance, pushableLayer);
        return hit.collider != null;
    }

    private void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        if (jumpSound != null)
        {
            audioSource.PlayOneShot(jumpSound);
        }
    }

    private void Shoot()
    {
        if (Time.time < nextFireTime) return;
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

        float direction = 1f;

        if (moveInput < -MinFlipInputThreshold)
        {
            direction = -1f;
        }
        else if (moveInput > MinFlipInputThreshold)
        {
            direction = 1f;
        }
        else
        {
            direction = spriteRenderer.flipX ? -1f : 1f;
        }

        Quaternion spawnRotation = (direction == -1f) ? Quaternion.identity : Quaternion.Euler(0, 0, 180f);
        GameObject projectileObj = Instantiate(projectilePrefab, firePoint.position, spawnRotation);

        Projectile projectileScript = projectileObj.GetComponent<Projectile>();
        if (projectileScript != null)
        {
            projectileScript.Initialize(direction, element);
        }

        if (shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        int myBulletIndex = (element == ElementType.Fire) ? 0 : 1;
        SendSpawnProjectileEvent(firePoint.position, direction, myBulletIndex);

        nextFireTime = Time.time + fireRate;
    }

    /// <summary>
    /// 足元が水エリア（Waterレイヤー）かつ氷属性の時、水面に氷柱を生成する
    /// </summary>
    private void TrySpawnIcePillarOnWater()
    {
        // 氷属性でない場合、または氷柱プレハブが未設定の場合は処理しない
        if (element != ElementType.Ice || icePillarPrefab == null) return;

        // プレイヤーの足元に向かって Raycast を照射
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            Vector2.down,
            waterCheckDistance,
            waterLayer
        );

        // 水エリアを検知した場合
        if (hit.collider != null)
        {
            Vector3 spawnPosition = hit.point;
            Instantiate(icePillarPrefab, spawnPosition, Quaternion.identity);

            if (createPillarSound != null)
            {
                audioSource.PlayOneShot(createPillarSound);
            }
        }
        else
        {
            // 水の上ではない場所で押した時の失敗SE（任意）
            if (failSound != null)
            {
                audioSource.PlayOneShot(failSound);
            }
        }
    }

    private void HandleWalkSound()
    {
        if (walkSound == null) return;

        if (isGrounded && Mathf.Abs(rb.velocity.x) > RemoteSpeedThreshold)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.clip = walkSound;
                audioSource.loop = true;
                audioSource.Play();
            }
        }
        else
        {
            if (audioSource.isPlaying && audioSource.clip == walkSound)
            {
                audioSource.Stop();
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision) => CheckContact(collision, true);
    private void OnCollisionExit2D(Collision2D collision) => CheckContact(collision, false);

    private void CheckContact(Collision2D collision, bool state)
    {
        int layer = collision.gameObject.layer;

        bool isGroundLayer = ((1 << layer) & groundLayer) != 0;
        bool isPushableLayer = ((1 << layer) & pushableLayer) != 0;

        if (isGroundLayer)
        {
            if (state)
            {
                foreach (ContactPoint2D contact in collision.contacts)
                {
                    if (contact.normal.y >= MinGroundAngleY)
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
            else
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

    private void OnCollisionStay2D(Collision2D collision) => CheckContact(collision, true);

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

    private void UpdateAnimationParametersForRemote(Vector2 simulatedVelocity, bool remoteIsGrounded)
    {
        if (anim == null) return;

        float currentHorizontalSpeed = Mathf.Abs(simulatedVelocity.x);

        if (currentHorizontalSpeed < RemoteSpeedThreshold) currentHorizontalSpeed = 0f;

        anim.SetFloat("Speed", currentHorizontalSpeed);
        anim.SetBool("isGround", remoteIsGrounded);
        anim.SetFloat("yVelocity", simulatedVelocity.y);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * waterCheckDistance);
    }
}