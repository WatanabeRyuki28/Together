using UnityEngine;

[RequireComponent(typeof(Animator))]
public class JumpPad : MonoBehaviour
{
    [Header("ジャンプの設定")]
    [SerializeField] private float jumpForce = 12.0f; // 跳ね上げる強さ（通常のジャンプより高め）

    private Animator animator;

    // アニメーターのトリガー名（インスペクターでのスペルミス防止）
    private static readonly int LaunchTrigger = Animator.StringToHash("Launch");

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    // トリガー判定（プレイヤーが上に乗った瞬間の検知）
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 触れたオブジェクトがプレイヤーかどうかをチェック
        if (other.TryGetComponent<PlayerController>(out PlayerController player))
        {
            LaunchPlayer(other.gameObject);
        }
    }

    // プレイヤーを跳ね上げる処理
    private void LaunchPlayer(GameObject playerObj)
    {
        if (playerObj.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
        {
            // 落下中の勢いを一度リセットし、常に一定の高さまで飛べるようにする
            rb.velocity = new Vector2(rb.velocity.x, 0f);

            // 上方向へ瞬間的な力を加える（Impulseモード）
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

            // ジャンプ台がビヨーンと動くアニメーションを再生
            animator.SetTrigger(LaunchTrigger);

            Debug.Log($"{playerObj.name} がジャンプ台で大ジャンプしました！強さ: {jumpForce}");
        }
    }
}