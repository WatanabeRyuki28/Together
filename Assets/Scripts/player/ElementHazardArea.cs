using UnityEngine;

/// <summary>
/// 属性によって通行できたり、入ると死亡したり、箱の状態が変化したりするエリア（マグマ / 水）
/// </summary>
public class ElementHazardArea : MonoBehaviour
{
    public enum AreaType
    {
        Magma, // 3: マグマ（炎キャラは安全、水キャラが通ると死ぬ）[cite: 1]
        Water  // 4: 水（水キャラは安全、炎キャラが通ると死ぬ）[cite: 2]
    }

    [Header("エリアの設定")]
    [SerializeField] private AreaType areaType = AreaType.Magma;
    [SerializeField] private int fatalDamage = 999;              // 即死させるためのダメージ値

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckPlayerSafety(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        CheckPlayerSafety(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        CheckPlayerSafety(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        CheckPlayerSafety(collision);
    }

    private void CheckPlayerSafety(Collider2D collisionCol)
    {
        // 1. プレイヤーの判定
        PlayerController player = collisionCol.GetComponent<PlayerController>();
        if (player != null)
        {
            bool isDeadly = false;

            if (areaType == AreaType.Magma)
            {
                // マグマ：水キャラ（Ice）が通ったら死ぬ[cite: 1]
                if (player.Element == ElementType.Ice)
                {
                    isDeadly = true;
                    Debug.Log($"{player.playerNumber}P（水キャラ）がマグマに触れて死亡しました[cite: 1]。");
                }
            }
            else if (areaType == AreaType.Water)
            {
                // 水：炎キャラ（Fire）が通ったら死ぬ[cite: 2]
                if (player.Element == ElementType.Fire)
                {
                    isDeadly = true;
                    Debug.Log($"{player.playerNumber}P（炎キャラ）が水に入って死亡しました[cite: 2]。");
                }
            }

            // 死亡条件を満たしている場合はダメージ（即死）を与える
            if (isDeadly)
            {
                player.TakeDamage(fatalDamage);
            }
            return;
        }

        // 2. 箱（PushableBox）の判定
        PushableBox box = collisionCol.GetComponent<PushableBox>();
        if (box != null)
        {
            if (areaType == AreaType.Magma)
            {
                // マグマの上に乗ると燃える[cite: 1]
                box.SetStatus(StatusCondition.Burning);
            }
            else if (areaType == AreaType.Water)
            {
                // 水の上で浮く[cite: 2]
                box.SetStatus(StatusCondition.Wet);
            }
        }
    }
}