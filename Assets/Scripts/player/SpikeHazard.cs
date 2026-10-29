using UnityEngine;

/// <summary>
/// どちらのプレイヤーが触れても即死する棘（トゲ）ギミック
/// </summary>
public class SpikeHazard : MonoBehaviour
{
    private const int FatalDamage = 999; // 確殺用の大きなダメージ値

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // ぶつかったオブジェクトから PlayerController を取得
        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        if (player != null)
        {
            // どちらが触れても死亡（即死ダメージを与える）
            player.TakeDamage(FatalDamage);

            Debug.Log($"{player.playerNumber}P が棘に触れて死亡しました。リトライ処理へ移行します[cite: 3]。");

            // 必要に応じてここでゲームマネージャーのリトライ処理などを呼び出せます
            // 例: GameManager.Instance.RetryGame();
        }
    }
}