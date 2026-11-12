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

    [Header("上昇設定")]
    [SerializeField] private GameObject WaterBlock;
    [SerializeField] private GameObject WaterBlock2;
    [SerializeField] private float speed = 5.0f;

    [Tooltip("初期位置からどれくらい上まで上昇できるか")]
    [SerializeField] private float maxRaiseHeight = 3.0f;

    private bool shouldRaise = false;
    private float time = 0;
    private float waitTime = 0.5f;

    private float initialY;   // ゲーム開始時のY座標
    private float targetY;    // 現在の目標Y座標

    private void Start()
    {
        if (WaterBlock != null)
        {
            initialY = WaterBlock.transform.position.y;
            targetY = initialY;
        }
    }

    // --- 弾（Projectile）が当たったとき ---
    public void OnInteract(ElementType element)
    {
        Debug.Log($"[OnInteract] 属性: {element}, エリア: {areaType}");

        if (areaType == AreaType.Water && element == ElementType.Ice)
        {
            TryRaiseBlock();
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

                if (player.Element == ElementType.Ice)
                {
                    TryRaiseBlock();
                }
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

    // ブロックを上げる共通処理
    private void TryRaiseBlock()
    {
        if (WaterBlock == null) return;

        float currentBaseY = shouldRaise ? targetY : WaterBlock.transform.position.y;
        float absoluteMaxY = initialY + maxRaiseHeight;

        if (currentBaseY >= absoluteMaxY)
        {
            Debug.Log("[TryRaiseBlock] これ以上上に上がれません（上限に達しています）。");
            return;
        }

        targetY = currentBaseY + 1f;

        if (targetY > absoluteMaxY)
        {
            targetY = absoluteMaxY;
        }

        shouldRaise = true;
        Debug.Log($"[TryRaiseBlock] 目標Y座標を {targetY} に設定しました（上限: {absoluteMaxY}）。");
    }

    void Update()
    {
        if (WaterBlock == null) return;

        if (shouldRaise)
        {
            Vector3 pos1 = WaterBlock.transform.position;
            pos1.y = Mathf.MoveTowards(pos1.y, targetY, speed * Time.deltaTime);
            WaterBlock.transform.position = pos1;

            if (WaterBlock2 != null)
            {
                Vector3 pos2 = WaterBlock2.transform.position;
                pos2.y = Mathf.MoveTowards(pos2.y, targetY, speed * Time.deltaTime);
                WaterBlock2.transform.position = pos2;
            }

            if (Mathf.Approximately(WaterBlock.transform.position.y, targetY))
            {
                WaterBlock.transform.position = new Vector3(pos1.x, targetY, pos1.z);
                if (WaterBlock2 != null)
                {
                    Vector3 finalPos2 = WaterBlock2.transform.position;
                    WaterBlock2.transform.position = new Vector3(finalPos2.x, targetY, finalPos2.z);
                }

                shouldRaise = false;
                Debug.Log("[Update] 目標の高さに到達しました。");
            }
        }

        time += Time.deltaTime;
        if (time >= waitTime)
        {
            if (WaterBlock2 != null)
            {
                bool isActive = WaterBlock2.activeSelf;
                WaterBlock2.SetActive(!isActive);
            }
            time = 0f;
        }
    }
}