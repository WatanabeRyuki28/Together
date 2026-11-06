using UnityEngine;

/// <summary>
/// 属性によって通行できたり、入ると死亡したり、箱の状態が変化したりするエリア（マグマ / 水）
/// </summary>
public class ElementHazardArea : MonoBehaviour
{
    public enum AreaType
    {
        Magma, // マグマ（炎キャラは安全、水キャラが通ると死亡、箱は燃える）
        Water  // 水（水キャラは安全、炎キャラが通ると死亡、箱は浮く）
    }

    [Header("エリアの設定")]
    [SerializeField] private AreaType areaType = AreaType.Magma;
    [SerializeField] private int fatalDamage = 999;              // 即死させるためのダメージ値

    private void OnTriggerEnter2D(Collider2D collision)
    {
        HandleHazardInteraction(collision);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        HandleHazardInteraction(collision);
    }

    private void HandleHazardInteraction(Collider2D collision)
    {
        // 1. プレイヤーの判定
        PlayerController player = collision.GetComponent<PlayerController>();
        if (player != null)
        {
            bool isDeadly = false;

            if (areaType == AreaType.Magma)
            {
                // マグマ：水キャラ（Ice等）が通ったら死ぬ[cite: 1]
                if (player.Element != ElementType.Fire)
                {
                    isDeadly = true;
                    Debug.Log($"{player.playerNumber}Pがマグマに触れて死亡しました[cite: 1]。");
                }
            }
            else if (areaType == AreaType.Water)
            {
                // 水：炎キャラ（Fire）が通ったら死ぬ[cite: 2]
                if (player.Element != ElementType.Ice)
                {
                    isDeadly = true;
                    Debug.Log($"{player.playerNumber}Pが水に入って死亡しました[cite: 2]。");
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
        PushableBox box = collision.GetComponent<PushableBox>();
        if (box != null)
        {
            if (areaType == AreaType.Magma)
            {
                // マグマの上に乗ると燃える（移動させることができない）[cite: 1]
                box.SetStatus(StatusCondition.Burning);
            }
            else if (areaType == AreaType.Water)
            {
                // 水の上で浮く（水面を活用して上に乗せられる）[cite: 2]
                box.SetStatus(StatusCondition.Wet);
            }
        }
    }
}