using UnityEngine;

/// <summary>
/// 属性によって通行できたり、入ると死亡したりするエリア（マグマ / 水）
/// </summary>
public class ElementHazardArea : MonoBehaviour
{
    public enum AreaType
    {
        Magma, // 3: マグマ（炎キャラは安全、水キャラが通ると死ぬ）
        Water  // 4: 水（水キャラは安全、炎キャラが通ると死ぬ）[cite: 4]
    }

    [Header("エリアの設定")]
    [SerializeField] private AreaType areaType = AreaType.Magma; // インスペクターで切り替え
    [SerializeField] private int fatalDamage = 999;             // 即死させるためのダメージ値

    private void OnTriggerEnter2D(Collider2D collision)
    {
        CheckPlayerSafety(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        CheckPlayerSafety(collision);
    }

    private void CheckPlayerSafety(Collider2D collision)
    {
        PlayerController player = collision.GetComponent<PlayerController>();
        if (player == null) return;

        bool isDeadly = false;

        if (areaType == AreaType.Magma)
        {
            // マグマ：水キャラ（Ice）が通ったら死ぬ[cite: 4]
            if (player.Element == ElementType.Ice)
            {
                isDeadly = true;
                Debug.Log($"{player.playerNumber}P（水キャラ）がマグマに触れて死亡しました[cite: 4]。");
            }
        }
        else if (areaType == AreaType.Water)
        {
            // 水：炎キャラ（Fire）が通ったら死ぬ[cite: 4]
            if (player.Element == ElementType.Fire)
            {
                isDeadly = true;
                Debug.Log($"{player.playerNumber}P（炎キャラ）が水に入って死亡しました[cite: 4]。");
            }
        }

        // 死亡条件を満たしている場合はダメージ（即死）を与える
        if (isDeadly)
        {
            player.TakeDamage(fatalDamage);
        }
    }
}