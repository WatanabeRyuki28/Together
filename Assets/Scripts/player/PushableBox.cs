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

    [Header("効果音設定")]
    [SerializeField] private AudioClip pushSound;

    [Header("エフェクト設定")]
    [SerializeField] private GameObject fireEffect; // マグマで燃えるときのエフェクト

    private Rigidbody2D rb;
    private AudioSource audioSource;
    private float defaultGravityScale; // 水に浮く処理用のデフォルト重力

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = true; // 押している間ループ再生

        defaultGravityScale = rb.gravityScale;
    }

    private void Update()
    {
        HandlePushSound();

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
        currentStatus = newStatus;

        switch (currentStatus)
        {
            case StatusCondition.Normal:
                rb.gravityScale = defaultGravityScale;
                break;

            case StatusCondition.Burning:
                // マグマの上に乗ると燃える仕様
                BurnBox();
                break;

            case StatusCondition.Wet:
                // 水の上で浮く仕様（重力を軽減または反転させて浮力を表現）
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

        // 例: 燃え尽きて消滅させる場合
        // Destroy(gameObject, 1.5f);
    }

    private void ApplyBuoyancy()
    {
        Debug.Log("箱が水に濡れ、水面で浮いています。");
        // 水に浮く表現として重力を小さくする（例: デフォルトの1/4程度にする、または0にする）
        rb.gravityScale = defaultGravityScale * 0.1f;

        // 落下速度に制限をかけるなどすると、ぷかぷか浮くような挙動が作りやすくなります
    }
}