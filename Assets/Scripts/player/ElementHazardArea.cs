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
    [SerializeField] private float upWater;

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

    private void OnTriggerExit2D(Collider2D collision)
    {
        PushableBox box = collision.GetComponent<PushableBox>();
        if (box != null)
        {
            if (areaType == AreaType.Water)
            {
                box.SetStatus(StatusCondition.Normal);
                Debug.Log("箱が水エリアから出たため、Normal状態に戻しました。");
            }
        }
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
                box.SetStatus(StatusCondition.Burning);
            }
            else if (areaType == AreaType.Water)
            {
                box.SetStatus(StatusCondition.Wet);
            }
        }
    }

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

        targetY = currentBaseY + upWater;

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
            float previousY = pos1.y; // 移動前のY座標を保存
            pos1.y = Mathf.MoveTowards(pos1.y, targetY, speed * Time.deltaTime);

            float deltaY = pos1.y - previousY; // 今回の移動量を計算

            WaterBlock.transform.position = pos1;

            if (WaterBlock2 != null)
            {
                Vector3 pos2 = WaterBlock2.transform.position;
                pos2.y = Mathf.MoveTowards(pos2.y, targetY, speed * Time.deltaTime);
                WaterBlock2.transform.position = pos2;
            }

            // 水エリアが上昇中、中にいる Wet 状態の箱も一緒に持ち上げて基準位置も同期する
            Collider2D waterCollider = GetComponent<Collider2D>();
            if (waterCollider != null)
            {
                ContactFilter2D filter = new ContactFilter2D().NoFilter();
                Collider2D[] results = new Collider2D[10];
                int count = waterCollider.OverlapCollider(filter, results);

                for (int i = 0; i < count; i++)
                {
                    PushableBox box = results[i].GetComponent<PushableBox>();
                    if (box != null && box.CurrentStatus == StatusCondition.Wet)
                    {
                        box.transform.position += new Vector3(0, deltaY, 0);
                        box.AddBuoyantBaseY(deltaY);
                    }
                }
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