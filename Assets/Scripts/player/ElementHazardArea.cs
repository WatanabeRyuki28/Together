using UnityEngine;

public class ElementHazardArea : MonoBehaviour, IInteractable
{
    public enum AreaType
    {
        Magma,
        Water,
        Spike
    }

    [Header("エリアの設定")]
    [SerializeField] private AreaType areaType = AreaType.Magma;
    [SerializeField] private int fatalDamage = 999;

   
    private WhichWater whichWater;


    void Start()
    {
        whichWater = FindFirstObjectByType<WhichWater>();
    }

    void Update()
    {

    }

    // --- 弾（Projectile）が当たったとき ---
    public void OnInteract(ElementType element)
    {
        Debug.Log($"[OnInteract] 属性: {element}, エリア: {areaType}");

        if (areaType == AreaType.Water && element == ElementType.Ice)
        {
            whichWater.TryRaiseBlock();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckTargetSafety(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        CheckTargetSafety(collision);
    }

    private void CheckTargetSafety(Collider2D collisionCol)
    {
        // 1. プレイヤーの判定
        PlayerController player = collisionCol.GetComponent<PlayerController>();
        if (player != null)
        {
            bool isDeadly = false;

            if (areaType == AreaType.Magma)
            {
                if (player.Element == ElementType.Ice) isDeadly = true;
            }
            else if (areaType == AreaType.Water)
            {
                if (player.Element == ElementType.Fire) isDeadly = true;

                
            }
            else if (areaType == AreaType.Spike)
            {
                if (player.Element == ElementType.Ice) isDeadly = true;
                if (player.Element == ElementType.Fire) isDeadly = true;
            }

            if (isDeadly)
            {
                player.TakeDamage(fatalDamage);
            }
            return;
        }

        // 2. プッシュ可能な箱（PushableBox）の判定
        PushableBox box = collisionCol.GetComponent<PushableBox>();
        if (box != null)
        {
            if (areaType == AreaType.Magma)
            {
                // マグマに触れたら燃やす（Burning状態にしてリスポーン発動）
                box.SetStatus(StatusCondition.Burning);
            }
            else if (areaType == AreaType.Water)
            {
                // 水に触れたら濡らす（Wet状態にしてぷかぷか浮遊発動）
                box.SetStatus(StatusCondition.Wet);
            }
        }
    }

    

}