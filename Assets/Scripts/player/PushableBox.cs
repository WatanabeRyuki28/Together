using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(AudioSource))]
public class PushableBox : MonoBehaviour
{
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
    [SerializeField] private Vector3 respawnPosition;
    [SerializeField] private float respawnDelay = 1.5f;

    [Header("効果音設定")]
    [SerializeField] private AudioClip pushSound;

    [Header("エフェクト設定")]
    [SerializeField] private GameObject fireEffect;

    private Rigidbody2D rb;
    private AudioSource audioSource;
    private float defaultGravityScale;
    private float buoyantBaseY;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = true;

        defaultGravityScale = rb.gravityScale;
    }

    private void Start()
    {
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

        // デバッグ用（Lキーで強制燃焼）
        if (Input.GetKey(KeyCode.L) && currentStatus != StatusCondition.Burning)
        {
            SetStatus(StatusCondition.Burning);
        }
    }

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

    public void SetStatus(StatusCondition newStatus)
    {
        // すでに同じ状態なら処理しない（ただしWetに入る瞬間は高さを再取得するため通す）
        if (currentStatus == newStatus && newStatus != StatusCondition.Wet) return;

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

        rb.velocity = Vector2.zero;
        rb.simulated = false;
        Invoke(nameof(RespawnBox), respawnDelay);
    }

    private void RespawnBox()
    {
        transform.position = respawnPosition;
        currentStatus = StatusCondition.Normal;
        rb.gravityScale = defaultGravityScale;
        rb.velocity = Vector2.zero;
        rb.simulated = true;

        Debug.Log("箱がリスポーンしました。");
    }

    private void ApplyBuoyancy()
    {
        Debug.Log("箱が水に濡れ、水面で浮いています。");
        rb.gravityScale = 0f;
        rb.velocity = new Vector2(rb.velocity.x, 0f);
        buoyantBaseY = transform.position.y;
    }

    private void ApplyFloatingMotion()
    {
        float targetY = buoyantBaseY + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        float newY = Mathf.MoveTowards(transform.position.y, targetY, Time.deltaTime * 5f);
        rb.MovePosition(new Vector2(transform.position.x, newY));
        rb.velocity = new Vector2(rb.velocity.x, 0f);
    }

    /// <summary>
    /// 水ブロックが上昇した際、浮遊の基準Y座標も一緒に持ち上げる（上昇ギミック連動用）
    /// </summary>
    public void AddBuoyantBaseY(float amount)
    {
        buoyantBaseY += amount;
    }

    // 水エリアのトリガーから完全に出たとき
    private void OnTriggerExit2D(Collider2D collision)
    {
        ElementHazardArea hazard = collision.GetComponent<ElementHazardArea>();
        if (hazard != null)
        {
            if (currentStatus == StatusCondition.Wet)
            {
                SetStatus(StatusCondition.Normal);
                Debug.Log("水エリアから出たため、Normal状態に戻りました。");
            }
        }
    }
}