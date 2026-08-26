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
    private const float DefaultPillarOffsetY = 0.5f;      // 氷柱のデフォルト生成高さオフセット
    private const float DefaultPillarOffsetX = 0.8f;      // 氷柱のデフォルト生成前方オフセット
    private const int DefaultMaxPillarCount = 3;           // デフォルトの最大氷柱生成数
    private const float DefaultPreviewAlpha = 0.5f;        // プレビュー表示時のデフォルト透明度
    private const float PreviewLineWidth = 0.05f;          // 予測枠線の基本太さ
    private const int LineCornerCount = 4;                  // 枠線の頂点数（四角形）

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
    [SerializeField] private float moveSpeed = 5.0f;                // 基本の移動速度
    [SerializeField] private float pushSpeedMultiplier = 0.5f; // オブジェクト押し出し中の移動速度倍率
    [SerializeField] private float jumpForce = 6.5f;                // ジャンプ時に加える力の強さ

    [Header("射撃（クールタイム）設定")]
    [SerializeField] private float fireRate = 0.3f; // 次の弾を撃つまでに必要な待機時間（秒）
    private float nextFireTime = 0f;                // 次に発射が可能になる時刻の記録用

    [Header("氷柱生成（アイスメーカー）設定")]
    [SerializeField] private GameObject icePillarPrefab; // 生成する氷柱のプレハブ
    [SerializeField] private LayerMask waterLayer;       // 水エリアのレイヤー
    [SerializeField] private float waterCheckDistance = DefaultWaterCheckDistance; // Raycastの長さ
    [SerializeField] private float pillarForwardOffset = DefaultPillarOffsetX; // プレイヤー前方への生成ずらし距離
    [SerializeField] private Vector3 pillarSpawnOffset = new Vector3(0f, DefaultPillarOffsetY, 0f); // 生成位置の上方向オフセット
    [SerializeField] private int maxPillarCount = DefaultMaxPillarCount; // 画面内に存在できる氷柱の最大数
    [SerializeField] private float pillarCoolTime = 1.0f; // ★ 氷柱生成のクールタイム（秒）
    private float nextPillarTime = 0f;                       // ★ 次に氷柱が生成可能になる時刻

    [Header("氷柱生成の予測（プレビュー）設定")]
    [SerializeField] private GameObject previewPillarObject; // プレビュー用の氷柱オブジェクト
    [SerializeField] private LineRenderer previewLineRenderer; // 予測線用のLineRenderer
    [SerializeField] private bool matchPillarWidth = true;    // チェックをいれると柱の幅を基準にする
    [SerializeField] private float previewLineWidthMultiplier = 1.0f; // インスペクターで太さを倍率調整
    [SerializeField] private Color canSpawnColor = new Color(0f, 1f, 1f, DefaultPreviewAlpha);   // 生成可能時の色（水色・半透明）
    [SerializeField] private Color cannotSpawnColor = new Color(1f, 0f, 0f, DefaultPreviewAlpha); // 生成不可時の色（赤色・半透明）

    [Header("効果音（SE）設定")]
    [SerializeField] private AudioClip jumpSound;          // ジャンプ音
    [SerializeField] private AudioClip shootSound;         // 射撃音
    [SerializeField] private AudioClip walkSound;          // 足音（ループ用）
    [SerializeField] private AudioClip createPillarSound; // 氷柱生成音
    [SerializeField] private AudioClip failSound;          // 氷柱生成失敗音

    // 使用するキーをコード側で固定
    private KeyCode leftKey = KeyCode.A;          // 左移動
    private KeyCode rightKey = KeyCode.D;          // 右移動
    private KeyCode jumpKey = KeyCode.Space;      // ジャンプ
    private KeyCode fireKey = KeyCode.Return;     // 攻撃
    private KeyCode pillarKey = KeyCode.E;        // 氷柱生成キー

    [Header("各種参照設定")]
    [SerializeField] private GameObject projectilePrefab; // 発射する弾のプレハブ
    [SerializeField] private Transform firePoint;           // 弾が生成（出現）するポイント
    [SerializeField] private LayerMask groundLayer;        // 地面判定を行う対象レイヤー
    [SerializeField] private LayerMask pushableLayer;      // 押し出し可能なオブジェクトのレイヤー

    public bool CanMove { get; set; } = true;

    private Rigidbody2D rb;
    private Animator anim;                  // アニメーター用
    private SpriteRenderer spriteRenderer; // 左右反転用
    private AudioSource audioSource;       // 効果音再生用
    private SpriteRenderer previewSpriteRenderer; // プレビューオブジェクトのSpriteRenderer

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

        // プレビュー表示用スプライトレンダラーの取得と初期化
        if (previewPillarObject != null)
        {
            previewSpriteRenderer = previewPillarObject.GetComponent<SpriteRenderer>();
            previewPillarObject.SetActive(false);
        }

        // LineRenderer の枠線初期設定
        if (previewLineRenderer != null)
        {
            previewLineRenderer.useWorldSpace = true;
            previewLineRenderer.loop = true;
            previewLineRenderer.positionCount = LineCornerCount;
            previewLineRenderer.enabled = false;
        }
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
            SetPreviewActive(false);
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
            SetPreviewActive(false);
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
            SetPreviewActive(false);
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

            // 氷柱生成判定 (キーボード E キー または ゲームパッド追加ボタン)
            bool keyboardPillar = useKeyboard && Input.GetKeyDown(pillarKey);
            if (keyboardPillar)
            {
                TrySpawnIcePillarOnWater();
            }

            // 氷柱の生成予測（プレビュー）の更新処理
            UpdateIcePillarPreview();

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
        else
        {
            SetPreviewActive(false);
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
    /// プレイヤーの向き（左右）に合わせた Raycast 照射開始位置を計算する
    /// </summary>
    private Vector3 GetRaycastOrigin()
    {
        float forwardDirection = (spriteRenderer != null && spriteRenderer.flipX) ? -1f : 1f;
        return transform.position + new Vector3(forwardDirection * pillarForwardOffset, 0f, 0f);
    }

    /// <summary>
    /// 足元（前寄り）が水エリア（Waterレイヤー）かつ氷属性の時、水面に氷柱を生成する（クールタイム制限あり）
    /// </summary>
    private void TrySpawnIcePillarOnWater()
    {
        // ★ クールタイム中なら何もしない
        if (Time.time < nextPillarTime) return;

        if (element != ElementType.Ice || icePillarPrefab == null) return;

        Vector3 rayOrigin = GetRaycastOrigin();
        RaycastHit2D hit = Physics2D.Raycast(
            rayOrigin,
            Vector2.down,
            waterCheckDistance,
            waterLayer
        );

        if (hit.collider != null)
        {
            // 現在存在する氷柱を取得
            GameObject[] currentPillars = GameObject.FindGameObjectsWithTag("IcePillar");

            // 上限数（3個）以上の場合は一番古い氷柱を破壊
            if (currentPillars.Length >= maxPillarCount)
            {
                Destroy(currentPillars[0]);
            }

            Vector3 spawnPosition = (Vector3)hit.point + pillarSpawnOffset;
            Instantiate(icePillarPrefab, spawnPosition, Quaternion.identity);

            if (createPillarSound != null)
            {
                audioSource.PlayOneShot(createPillarSound);
            }

            // ★ 生成成功時、次回可能時刻を更新（クールタイム設定）
            nextPillarTime = Time.time + pillarCoolTime;
        }
        else
        {
            if (failSound != null)
            {
                audioSource.PlayOneShot(failSound);
            }
        }
    }

    /// <summary>
    /// 氷柱の設置予定位置を四角い予測枠線（LineRenderer）およびプレビュー表示で更新する
    /// </summary>
    private void UpdateIcePillarPreview()
    {
        if (element != ElementType.Ice)
        {
            SetPreviewActive(false);
            return;
        }

        // プレイヤー前方から真下に向かって Raycast を照射
        Vector3 rayOrigin = GetRaycastOrigin();
        RaycastHit2D hit = Physics2D.Raycast(
            rayOrigin,
            Vector2.down,
            waterCheckDistance,
            waterLayer
        );

        if (hit.collider != null)
        {
            Vector3 targetSpawnPosition = (Vector3)hit.point + pillarSpawnOffset;

            // ★ クールタイム消化中かどうかでプレビュー色を判定（待ち時間中は赤、可能なら水色）
            bool canSpawn = Time.time >= nextPillarTime;
            Color currentColor = canSpawn ? canSpawnColor : cannotSpawnColor;

            // 1. スプライトのプレビュー表示
            if (previewPillarObject != null)
            {
                previewPillarObject.SetActive(true);
                previewPillarObject.transform.position = targetSpawnPosition;

                if (icePillarPrefab != null)
                {
                    previewPillarObject.transform.localScale = icePillarPrefab.transform.localScale;

                    SpriteRenderer prefabSr = icePillarPrefab.GetComponent<SpriteRenderer>();
                    if (prefabSr != null && previewSpriteRenderer != null)
                    {
                        previewSpriteRenderer.sprite = prefabSr.sprite;
                    }
                }

                if (previewSpriteRenderer != null)
                {
                    previewSpriteRenderer.color = currentColor;
                }
            }

            // 2. LineRenderer による四角い予測枠線の描画
            if (previewLineRenderer != null)
            {
                previewLineRenderer.enabled = true;
                previewLineRenderer.useWorldSpace = true;
                previewLineRenderer.loop = true;
                previewLineRenderer.positionCount = LineCornerCount;

                // 柱のサイズを取得（PrefabのSprite bounds または scale）
                Vector2 pillarSize = Vector2.one;
                if (icePillarPrefab != null)
                {
                    SpriteRenderer pillarSR = icePillarPrefab.GetComponent<SpriteRenderer>();
                    if (pillarSR != null && pillarSR.sprite != null)
                    {
                        pillarSize = Vector2.Scale(pillarSR.sprite.bounds.size, icePillarPrefab.transform.localScale);
                    }
                    else
                    {
                        pillarSize = icePillarPrefab.transform.localScale;
                    }
                }

                float halfWidth = pillarSize.x * 0.5f;
                float halfHeight = pillarSize.y * 0.5f;

                // 四角形の4頂点を計算
                Vector3 topLeft = targetSpawnPosition + new Vector3(-halfWidth, halfHeight, 0f);
                Vector3 topRight = targetSpawnPosition + new Vector3(halfWidth, halfHeight, 0f);
                Vector3 bottomRight = targetSpawnPosition + new Vector3(halfWidth, -halfHeight, 0f);
                Vector3 bottomLeft = targetSpawnPosition + new Vector3(-halfWidth, -halfHeight, 0f);

                previewLineRenderer.SetPosition(0, topLeft);
                previewLineRenderer.SetPosition(1, topRight);
                previewLineRenderer.SetPosition(2, bottomRight);
                previewLineRenderer.SetPosition(3, bottomLeft);

                float calculatedLineWidth = PreviewLineWidth * previewLineWidthMultiplier;
                previewLineRenderer.startWidth = calculatedLineWidth;
                previewLineRenderer.endWidth = calculatedLineWidth;

                previewLineRenderer.startColor = currentColor;
                previewLineRenderer.endColor = currentColor;
            }
        }
        else
        {
            SetPreviewActive(false);
        }
    }

    /// <summary>
    /// プレビュー表示を一括でON/OFF制御する
    /// </summary>
    private void SetPreviewActive(bool active)
    {
        if (previewPillarObject != null && previewPillarObject.activeSelf != active)
        {
            previewPillarObject.SetActive(active);
        }

        if (previewLineRenderer != null && previewLineRenderer.enabled != active)
        {
            previewLineRenderer.enabled = active;
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
        // 向きに応じた Raycast 照射開始位置（真下へ一直線に伸ばす）
        float forwardDirection = (spriteRenderer != null && spriteRenderer.flipX) ? -1f : 1f;
        Vector3 startPosition = transform.position + new Vector3(forwardDirection * pillarForwardOffset, 0f, 0f);
        Vector3 endPosition = startPosition + Vector3.down * waterCheckDistance;

        // Raycast 判定線の描画（水色）
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(startPosition, endPosition);

        // 生成位置の円を描画（青色）
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(endPosition + pillarSpawnOffset, 0.2f);
    }
}