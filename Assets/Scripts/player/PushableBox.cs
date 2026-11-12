using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(AudioSource))]
public class PushableBox : MonoBehaviour
{
    // --- 定数定義 ---
    private const float MinMoveSpeedThreshold = 0.2f;

    [Header("状態の設定")]
    [SerializeField] private StatusCondition currentStatus = StatusCondition.Normal;
    public StatusCondition CurrentStatus => currentStatus;

    [Header("水面ぷかぷか設定")]
    [Tooltip("ぷかぷか揺れる上下の幅（高さ）")]
    [SerializeField] private float floatAmplitude = 0.1f;
    [Tooltip("ぷかぷか揺れる速さ（周期）")]
    [SerializeField] private float floatFrequency = 3.0f;

    [Header("リスポーン設定")]
    [SerializeField] private Vector3 respawnPosition;       // リスポーン地点（初期位置を自動記憶）
    [SerializeField] private float respawnDelay = 1.5f;     // 燃えてからリスポーンするまでの時間

    [Header("効果音設定")]
    [SerializeField] private AudioClip pushSound;

    [Header("エフェクト設定")]
    [SerializeField] private GameObject fireEffect;         // マグマで燃えるときのエフェクト

    private Rigidbody2D rb;
    private AudioSource audioSource;
    private float defaultGravityScale;                      // デフォルトの重力

    // ぷかぷか浮遊用の基準Y座標を記憶する変数
    private float buoyantBaseY;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = true; // 押している間ループ再生

        defaultGravityScale = rb.gravityScale;
    }

    private void Start()
    {
        // ゲーム開始時の位置を初期リスポーン地点として自動記憶
        respawnPosition = transform.position;
    }

    private void Update()
    {
        HandlePushSound();

        // 水に濡れている（Wet）状態のときはぷかぷかさせる
        if (currentStatus == StatusCondition.Wet)
        {
            ApplyFloatingMotion();
        }

        // デバッグ用（Lキーで強制的に燃やすテストなど）
        if (Input.GetKey(KeyCode.L) && currentStatus != StatusCondition.Burning)
        {
            SetStatus(StatusCondition.Burning);
        }
    }

    /// <summary>
    /// 箱の移動速度に応じて移動音の再生・停止を制御する
    /// </summary>
    private void HandlePushSound()
    {
        if (pushSound == null) return;

        float currentHorizontalSpeed = Mathf.Abs(rb.velocity.x);

        if (currentHorizontalSpeed > MinMoveSpeedThreshold)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.clip = pushSound;
                audioSource.Play();
            }
        }
        else
        {
            if (audioSource.isPlaying && audioSource.clip == pushSound)
            {
                audioSource.Stop();
            }
        }
    }

    /// <summary>
    /// 箱の状態を変更し、それぞれの状態に応じた処理を行う
    /// </summary>
    public void SetStatus(StatusCondition newStatus)
    {
        if (currentStatus == newStatus) return;

        // Wet状態に入る瞬間の高さを基準位置として記録する
        if (newStatus == StatusCondition.Wet)
        {
            buoyantBaseY = transform.position.y;
        }

        currentStatus = newStatus;

        switch (currentStatus)
        {
            case StatusCondition.Normal:
                rb.gravityScale = defaultGravityScale;
                break;

            case StatusCondition.Burning:
                BurnBox();
                break;

            case StatusCondition.Wet:
                ApplyBuoyancy();
                break;
        }
    }

    private void BurnBox()
    {
        if (fireEffect != null)
        {
            Instantiate(fireEffect, transform.position, fireEffect.transform.rotation);
        }
        Debug.Log("箱がマグマで燃え上がりました[cite: 1]。");

        // 燃えている間は物理挙動を一時停止
        rb.velocity = Vector2.zero;
        rb.simulated = false;

        // 指定秒数後にリスポーン処理を実行
        Invoke(nameof(RespawnBox), respawnDelay);
    }

    private void RespawnBox()
    {
        // 位置を初期位置（または設定されたリスポーン地点）に戻す
        transform.position = respawnPosition;

        // 状態を Normal（通常）に戻す
        currentStatus = StatusCondition.Normal;
        rb.gravityScale = defaultGravityScale;

        // 物理挙動を再開
        rb.simulated = true;

        Debug.Log("箱がリスポーンしました。");
    }

    private void ApplyBuoyancy()
    {
        Debug.Log("箱が水に濡れ、水面で浮いています。");
        // 重力を完全にオフにして沈まないようにする
        rb.gravityScale = 0f;
        rb.velocity = new Vector2(rb.velocity.x, 0f);

        // 水に触れた瞬間の高さを基準にする
        buoyantBaseY = transform.position.y;
    }

    /// <summary>
    /// 水面で上下にふんわりと揺れる（ぷかぷかさせる）処理（めり込み防止版）
    /// </summary>
    private void ApplyFloatingMotion()
    {
        // サイン波を使って目指すべきY座標を計算
        float targetY = buoyantBaseY + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        
        // Rigidbody2DのMovePositionを使って移動させることで、壁へのめり込みや引っかかりを防ぐ
        float newY = Mathf.MoveTowards(transform.position.y, targetY, Time.deltaTime * 5f);
        rb.MovePosition(new Vector2(transform.position.x, newY));

        // 浮いている間は縦方向の物理速度をリセット
        rb.velocity = new Vector2(rb.velocity.x, 0f);
    }
}