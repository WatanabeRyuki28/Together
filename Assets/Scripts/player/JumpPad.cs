using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class JumpPad : MonoBehaviour
{
    [Header("ジャンプの設定")]
    [Tooltip("通常ジャンプの倍の高さに合わせるため、必要に応じて数値を調整してください")]
    [SerializeField] private float jumpForce = 16.0f;

    [Tooltip("連続発動を防ぐクールタイム（秒）")]
    [SerializeField] private float launchCooldown = 0.15f;

    [Tooltip("上面判定の許容誤差（少し下から触れても拾いたい場合は正の値に）")]
    [SerializeField] private float surfaceTolerance = 0.05f;

    [Header("Audio Settings (効果音)")]
    [SerializeField] private AudioClip launchSound;

    private Animator animator;
    private AudioSource audioSource;
    private Collider2D padCollider;
    private float lastLaunchTime = -999f;

    private static readonly int LaunchTrigger = Animator.StringToHash("Launch");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        padCollider = GetComponent<Collider2D>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryLaunchFromCollider(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryLaunchFromCollider(other);
    }

    private void TryLaunchFromCollider(Collider2D other)
    {
        // クールタイム中は再発動しない
        if (Time.time - lastLaunchTime < launchCooldown)
        {
            return;
        }

        // 子コライダーに当たった場合も考慮して親からも PlayerController を探す
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        // ジャンプ台の上面Y座標を取得（Collider2Dがあれば上面、なければtransform.position.y）
        float padTopY = padCollider != null ? padCollider.bounds.max.y : transform.position.y;
        float playerBottomY = other.bounds.min.y;

        // プレイヤーの足元がジャンプ台の上面付近以上にある場合のみ発動
        if (playerBottomY >= padTopY - surfaceTolerance)
        {
            lastLaunchTime = Time.time;
            LaunchPlayer(player);
        }
    }

    private void LaunchPlayer(PlayerController player)
    {
        GameObject playerObj = player.gameObject;

        // Rigidbody2D や Animator が親/子にあるケースにも対応
        Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();
        if (rb == null) rb = playerObj.GetComponentInParent<Rigidbody2D>();

        if (rb != null)
        {
            player.isGrounded = false;

            // 上方向への速度をリセットしてから跳躍力を加える
            rb.velocity = new Vector2(rb.velocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

            Animator playerAnim = playerObj.GetComponent<Animator>();
            if (playerAnim == null) playerAnim = playerObj.GetComponentInChildren<Animator>();

            if (playerAnim != null)
            {
                if (player.playerNumber == 1)
                {
                    playerAnim.Play("1P_jump", 0, 0f);
                    Debug.Log("1Pのジャンプアニメーションを再生しました。");
                }
                else if (player.playerNumber == 2)
                {
                    playerAnim.Play("2P_jump", 0, 0f);
                    Debug.Log("2Pのジャンプアニメーションを再生しました。");
                }
                else
                {
                    Debug.LogWarning($"{playerObj.name} の playerNumber が 1 または 2 ではありません（現在の値: {player.playerNumber}）");
                }
            }

            if (animator != null)
            {
                animator.SetTrigger(LaunchTrigger);
            }

            if (launchSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(launchSound);
            }
        }
    }
}