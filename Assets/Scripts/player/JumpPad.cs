using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class JumpPad : MonoBehaviour
{
    [Header("ジャンプの設定")]
    [Tooltip("通常ジャンプの倍の高さに合わせるため、必要に応じて数値を調整してください")]
    [SerializeField] private float jumpForce = 16.0f; // 例: 通常より高い跳躍力

    [Header("Audio Settings (効果音)")]
    [SerializeField] private AudioClip launchSound;

    private Animator animator;
    private AudioSource audioSource;

    private static readonly int LaunchTrigger = Animator.StringToHash("Launch");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // プレイヤーがトリガーに侵入したとき
        if (other.TryGetComponent<PlayerController>(out PlayerController player))
        {
            // ★【改善】プレイヤーの「足元（コライダーの下端のY座標）」を取得する
            float playerBottomY = other.bounds.min.y;

            // ★【改善】ジャンプ台の基準位置（または上面のY座標）よりも足元が「上」にいるときだけ発動
            // これにより、横から歩いてきたときは足元がジャンプ台より下にあるため反応しなくなります
            if (playerBottomY >= transform.position.y)
            {
                player.isGrounded = true;
                LaunchPlayer(other.gameObject, player);
            }
        }
    }

    private void LaunchPlayer(GameObject playerObj, PlayerController player)
    {
        if (playerObj.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
        {
            // 上方向への速度をリセットしてから跳躍力を加えることで、落下中でも安定して高く跳ねる
            rb.velocity = new Vector2(rb.velocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            player.isGrounded = false;

            // 1Pと2Pでアニメーションを分岐
            if (playerObj.TryGetComponent<Animator>(out Animator playerAnim))
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

            // ジャンプ台自身の演出
            animator.SetTrigger(LaunchTrigger);
            if (launchSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(launchSound);
            }
        }
    }
}