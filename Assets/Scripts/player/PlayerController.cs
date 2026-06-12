using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Threading.Tasks;
using NativeWebSocket;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(AudioSource))] // ★AudioSourceを必須にする
public class PlayerController : MonoBehaviour
{
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

    private bool isGrounded; // 現在、地面に接地しているかどうかのフラグ
    private bool isPushing;  // 現在、押し出し対象に接触しているかどうかのフラグ

    NetworkManager client;

    public bool IsLocalPlayer { get; set; } = true;
    private NetworkManager networkManager;

    // 前回の座標を記録
    private Vector2 lastPosition;

    public Vector3 TargetPosition { get; set; }

    [SerializeField] private int projectilePrefabIndex;
    private int projectileCount = 0;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>(); // ★取得

        // AudioSourceの初期設定（3Dサウンドではなく2Dとして手軽にハッキリ鳴らす）
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
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
    }

    void Update()
    {
        // 相手（リモート）のキャラクターの場合
        if (!IsLocalPlayer)
        {
            if (rb != null)
            {
                rb.velocity = Vector2.zero; // 物理干渉による荒ぶりを完全カット
            }

            transform.position = Vector3.Lerp(transform.position, TargetPosition, 0.05f);

            // 相手が歩いている時の足音をミュート（自分の画面で相手の足音が鳴り続けるのを防ぐ）
            if (audioSource.isPlaying && audioSource.clip == walkSound)
            {
                audioSource.Stop();
            }
            return;
        }

        // ────────── 以下は「自分のキャラ（IsLocalPlayer == true）」だけの処理 ──────────

        // 毎フレーム移動処理を呼び出し
        Move();

        // ★【キー直接入力】Wキーが押された瞬間 且つ 地面にいる時
        if (Input.GetKeyDown(jumpKey) && isGrounded) Jump();

        // ★【キー直接入力】スペースキーが押された瞬間
        if (Input.GetKeyDown(fireKey))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Shoot();
        }

        // 毎フレーム最新の状態をAnimatorに送信
        UpdateAnimationParameters();

        // 足音の再生コントロール
        HandleWalkSound();

        if (Vector2.Distance(transform.position, lastPosition) > 0.01f)
        {
            SendPlayerData(transform.position);
            lastPosition = transform.position; // 記録を更新
        }
    }

    private void Move()
    {
        // ★【キー直接入力】A/Dキーの押し状態から移動方向を完全自作（左: -1, なし: 0, 右: 1）
        float moveInput = 0f;
        if (Input.GetKey(leftKey)) moveInput -= 1f;
        if (Input.GetKey(rightKey)) moveInput += 1f;

        // 「箱に触れている」かつ「箱がある方向にキーを入力している」時だけ、本当に押していると判定
        bool isActuallyPushing = isPushing && IsInputtingTowardsBox(moveInput);

        // 押し状態なら速度を下げ、そうでなければ通常の速度を適用
        float currentSpeed = isActuallyPushing ? moveSpeed * pushSpeedMultiplier : moveSpeed;

        // 左右の速度を設定（y軸は現在の物理挙動を維持）
        rb.velocity = new Vector2(moveInput * currentSpeed, rb.velocity.y);

        // 進行方向（入力値）に合わせてスプライトを左右反転する
        if (moveInput > 0.1f)
        {
            spriteRenderer.flipX = false; // 右を向く
        }
        else if (moveInput < -0.1f)
        {
            spriteRenderer.flipX = true;  // 左を向く
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

        // ★ジャンプ音を再生（他の音をぶった切って最優先で鳴らす）
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

        // ★【キー直接入力】現在のA/Dキーの押し状態から弾の方向を計算
        float moveInput = 0f;
        if (Input.GetKey(leftKey)) moveInput -= 1f;
        if (Input.GetKey(rightKey)) moveInput += 1f;

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

        // ★射撃音を再生（移動の足音などと重なっても綺麗に鳴るPlayOneShot）
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

    // ★【追加】足音のループ管理
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
            // 止まった、または空中に浮いたときは、足音が鳴っていたら止める
            if (audioSource.isPlaying && audioSource.clip == walkSound)
            {
                audioSource.Stop();
            }
        }
    }

    // Animatorのパラメーターに物理速度ベースで数値を書き込む処理
    private void UpdateAnimationParameters()
    {
        if (anim == null) return;

        // 現在の Rigidbody2D の「実際の物理的な移動速度」をベースにアニメーションを切り替える
        float currentHorizontalSpeed = Mathf.Abs(rb.velocity.x);
        anim.SetFloat("Speed", currentHorizontalSpeed);

        // Animatorウインドウの設定名に合わせて「isGrounded」から「isGround」に変更
        anim.SetBool("isGround", isGrounded);

        // 物理演算のリアルタイムな縦方向の速度を送る
        anim.SetFloat("yVelocity", rb.velocity.y);
    }

    // 衝突開始時の判定
    private void OnCollisionEnter2D(Collision2D collision) => CheckContact(collision, true);
    // 衝突終了時の判定
    private void OnCollisionExit2D(Collision2D collision) => CheckContact(collision, false);

    // 衝突している相手が床か箱かを確認し、状態を更新する
    // ★【接地判定を強化・マイルドに修正】
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
                    // 判定角度を 0.7f ➡ 0.5f に緩和（坂道やコライダーの継ぎ目対策）
                    float minGroundAngleY = 0.5f;

                    if (contact.normal.y >= minGroundAngleY)
                    {
                        isGrounded = true;
                        break;
                    }
                }
            }
            else // 離れた（Exit）とき
            {
                // 完全に離れた場合のみfalseにする
                isGrounded = false;
            }
        }

        if (isPushableLayer) isPushing = state;
    }

    // ★【追加】接触し続けている間も判定を毎フレームケアする（Enterの取りこぼし対策）
    private void OnCollisionStay2D(Collision2D collision) => CheckContact(collision, true);

    // プレイヤーの位置を送る
    async void SendPlayerData(Vector3 pos)
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
}