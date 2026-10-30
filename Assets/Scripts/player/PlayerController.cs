using UnityEngine;
using UnityEngine.EventSystems;
using System.Threading.Tasks;
using NativeWebSocket;

// 状態の定義（燃えている、濡れている、通常）
public enum StatusCondition
{
    Normal,
    Burning, // 炎で燃えている
    Wet      // 水で濡れている
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(AudioSource))]
public class PlayerController : MonoBehaviour
{
    // --- 定数定義（マジックナンバー排除） ---
    private const float MinMoveInputThreshold = 0.01f;
    private const float MinFlipInputThreshold = 0.1f;
    private const float StickDeadZone = 0.2f;
    private const float MinGroundAngleY = 0.5f;
    private const float BoxCheckDistance = 1.0f;
    private const float PositionSyncThreshold = 0.01f;
    private const float RemoteSpeedThreshold = 0.2f;
    private const float RemoteLerpFactor = 0.05f;
    private const float FrictionSlipperyThreshold = 0.01f;

    [Header("操作方法の設定 (チェックONで有効)")]
    [SerializeField] private bool useKeyboard = true;
    [SerializeField] private bool useGamepad = false;

    [Header("プレイヤー番号の設定")]
    [Tooltip("1Pなら1、2Pなら2を設定してください")]
    public int playerNumber = 1;

    [Header("キャラクターの属性")]
    [SerializeField] private ElementType element;
    public ElementType Element => element;

    [Header("ステータス設定（HP・死亡）")]
    [SerializeField] private int maxHp = 1;
    private int currentHp;
    private bool isDead = false;
    public bool IsDead => isDead;

    [Header("現在の状態異常")]
    [SerializeField] private StatusCondition currentStatus = StatusCondition.Normal;
    public StatusCondition CurrentStatus => currentStatus;

    [Header("移動・ジャンプ設定")]
    [SerializeField] private float moveSpeed = 5.0f;
    [SerializeField] private float pushSpeedMultiplier = 0.5f;
    [SerializeField] private float jumpForce = 6.5f;

    [Header("射撃（クールタイム）設定")]
    [SerializeField] private float fireRate = 0.3f;
    private float nextFireTime = 0f;

    [Header("効果音（SE）設定")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip walkSound;

    private KeyCode leftKey = KeyCode.A;
    private KeyCode rightKey = KeyCode.D;
    private KeyCode jumpKey = KeyCode.Space;
    private KeyCode fireKey = KeyCode.Return;

    [Header("各種参照設定")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask pushableLayer;

    public bool CanMove { get; set; } = true;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;

    public bool isGrounded { get; set; }
    private bool isOnSlipperyFloor;
    private bool isPushing;

    public bool IsLocalPlayer { get; set; } = true;
    private NetworkManager networkManager;

    private Vector2 lastPosition;
    public Vector3 TargetPosition { get; set; }

    [SerializeField] private int projectilePrefabIndex;
    private CommunicationUI controls;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

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
        currentHp = maxHp; // HPの初期化

        if (NetworkManager.Instance != null)
        {
            networkManager = NetworkManager.Instance;
        }
        else
        {
            Debug.LogWarning("[警告] NetworkManager が見つかりません。");
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
        }
    }

    void Update()
    {
        if (isDead) return; // 死亡時は入力を一切受け付けない

        if (!IsLocalPlayer)
        {
            if (rb != null) rb.velocity = Vector2.zero;

            Vector3 previousPosition = transform.position;
            transform.position = Vector3.Lerp(transform.position, TargetPosition, RemoteLerpFactor);

            Vector2 simulatedVelocity = (transform.position - previousPosition) / Time.deltaTime;
            UpdateAnimationParametersForRemote(simulatedVelocity, this.isGrounded);

            if (audioSource != null && audioSource.isPlaying && audioSource.clip == walkSound)
            {
                audioSource.Stop();
            }
            return;
        }

        if (StageMenuManager.Instance != null && (StageMenuManager.Instance.isIntroPlaying || StageMenuManager.Instance.isMenuOpen))
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            UpdateAnimationParameters(rb.velocity);

            if (audioSource != null && audioSource.isPlaying && audioSource.clip == walkSound)
            {
                audioSource.Stop();
            }
            return;
        }

        if (CanMove)
        {
            Move();

            bool keyboardJump = useKeyboard && Input.GetKeyDown(jumpKey);
            bool gamepadJump = useGamepad && controls != null && controls.Player.Jump.triggered;

            if ((keyboardJump || gamepadJump) && isGrounded)
            {
                Jump();
            }

            bool keyboardFire = useKeyboard && Input.GetKeyDown(fireKey);
            bool gamepadFire = useGamepad && controls != null && controls.Player.Tama.triggered;

            if (keyboardFire || gamepadFire)
            {
                if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                {
                    Shoot();
                }
            }

            bool keyboardMenu = useKeyboard && Input.GetKeyDown(KeyCode.Escape);
            bool gamepadMenu = useGamepad && controls != null && controls.Player.Menu.triggered;

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
            if (Mathf.Abs(gamepadInput) < StickDeadZone) gamepadInput = 0f;
            if (moveInput == 0f) moveInput = gamepadInput;
        }

        moveInput = Mathf.Clamp(moveInput, -1f, 1f);

        if (Mathf.Abs(moveInput) < MinMoveInputThreshold)
        {
            if (!isOnSlipperyFloor) rb.velocity = new Vector2(0f, rb.velocity.y);
        }
        else
        {
            bool isActuallyPushing = isPushing && IsInputtingTowardsBox(moveInput);
            float currentSpeed = isActuallyPushing ? moveSpeed * pushSpeedMultiplier : moveSpeed;
            rb.velocity = new Vector2(moveInput * currentSpeed, rb.velocity.y);
        }

        if (moveInput > MinFlipInputThreshold) spriteRenderer.flipX = false;
        else if (moveInput < -MinFlipInputThreshold) spriteRenderer.flipX = true;
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

        if (jumpSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(jumpSound);
        }
    }

    private void Shoot()
    {
        if (Time.time < nextFireTime) return;
        if (projectilePrefab == null || firePoint == null) return;

        if (anim != null) anim.SetTrigger("Attack");

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

        float direction = (moveInput < -MinFlipInputThreshold) ? -1f : (moveInput > MinFlipInputThreshold ? 1f : (spriteRenderer.flipX ? -1f : 1f));

        Quaternion spawnRotation = (direction == -1f) ? Quaternion.identity : Quaternion.Euler(0, 0, 180f);
        GameObject projectileObj = Instantiate(projectilePrefab, firePoint.position, spawnRotation);

        Projectile projectileScript = projectileObj.GetComponent<Projectile>();
        if (projectileScript != null)
        {
            projectileScript.Initialize(direction, element);
        }

        if (shootSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(shootSound);
        }

        int myBulletIndex = (element == ElementType.Fire) ? 0 : 1;
        SendSpawnProjectileEvent(firePoint.position, direction, myBulletIndex);

        nextFireTime = Time.time + fireRate;
    }

    private void HandleWalkSound()
    {
        if (walkSound == null || audioSource == null) return;

        if (isGrounded && Mathf.Abs(rb.velocity.x) > RemoteSpeedThreshold)
        {
            if (!audioSource.isPlaying || audioSource.clip != walkSound)
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckContact(collision, true);

        // プレイヤー同士が接触したときの処理（燃やす・濡らす）
        PlayerController otherPlayer = collision.gameObject.GetComponent<PlayerController>();
        if (otherPlayer != null)
        {
            if (this.element == ElementType.Fire)
            {
                otherPlayer.ApplyStatus(StatusCondition.Burning); // 触れたら燃やす
            }
            else if (this.element == ElementType.Ice) // ※水属性がIceの場合
            {
                otherPlayer.ApplyStatus(StatusCondition.Wet);      // 触れたら濡らす
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision) => CheckContact(collision, false);
    private void OnCollisionStay2D(Collision2D collision) => CheckContact(collision, true);

    /// <summary>
    /// 外部や接触から状態異常（燃焼・濡れ）を付与される
    /// </summary>
    public void ApplyStatus(StatusCondition newStatus)
    {
        currentStatus = newStatus;
        Debug.Log($"{playerNumber}P の状態が変化: {newStatus}");
        // 必要に応じてここで色の変化やパーティクル再生を呼び出せます
    }

    /// <summary>
    /// ダメージを受ける処理（弾のヒット時など）
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHp -= damage;
        Debug.Log($"{playerNumber}P がダメージを受けました！ 残りHP: {currentHp}");

        if (currentHp <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 死亡処理
    /// </summary>
    private void Die()
    {
        isDead = true;
        CanMove = false;
        rb.velocity = Vector2.zero;

        if (anim != null)
        {
            anim.SetTrigger("Die");
        }

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        Debug.Log($"{playerNumber}P が死亡しました。");
    }

    private void CheckContact(Collision2D collision, bool state)
    {
        int layer = collision.gameObject.layer;

        bool isGroundLayer = ((1 << layer) & groundLayer) != 0;
        bool isPushableLayer = ((1 << layer) & pushableLayer) != 0;

        if (isGroundLayer)
        {
            if (state)
            {
                if (collision.collider.sharedMaterial != null && collision.collider.sharedMaterial.friction <= FrictionSlipperyThreshold)
                {
                    isOnSlipperyFloor = true;
                }

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
                isOnSlipperyFloor = false;

                if (IsLocalPlayer)
                {
                    SendPlayerData(transform.position, false);
                }
            }
        }

        if (isPushableLayer) isPushing = state;
    }

    
    #region  送信

    private async void SendPlayerData(Vector3 pos, bool groundedStatus)
    {
        if (networkManager == null) return;

        int myRealChara = networkManager.myRealSelectedChar;
        if (myRealChara == -1)
        {
            myRealChara = (Element == ElementType.Fire) ? 0 : 1;
        }

        networkManager.SendPlayerPosition(
            myRealChara,
            pos.x,
            pos.y,
            groundedStatus,
            spriteRenderer != null && spriteRenderer.flipX
        );

    }


    private async void SendSpawnProjectileEvent(Vector3 pos, float dir, int bulletIndex)
    {
        NetworkManager.Instance.SendSpawnProjectile(pos, dir, bulletIndex);
    }

    #endregion

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
}