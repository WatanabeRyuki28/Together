using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class JumpPad : MonoBehaviour
{
    [Header("ジャンプの設定")]
    [SerializeField] private float jumpForce = 12.0f;

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
        if (other.TryGetComponent<PlayerController>(out PlayerController player))
        {
            player.isGrounded = true;
            LaunchPlayer(other.gameObject, player);
        }
    }

    private void LaunchPlayer(GameObject playerObj, PlayerController player)
    {
        if (playerObj.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
        {
            rb.velocity = new Vector2(rb.velocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            player.isGrounded = false;

            // ★【方法A】PlayerControllerの番号を読み取ってアニメーションを分岐
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