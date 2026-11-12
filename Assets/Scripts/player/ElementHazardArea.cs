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
    [SerializeField] GameObject WaterBlock;
    [SerializeField] GameObject WaterBlock2;
    [SerializeField] private float speed = 5.0f;

    [Tooltip("初期位置からどれくらい上まで上昇できるか")]
    [SerializeField] private float maxRaiseHeight = 3.0f; // 

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
        CheckPlayerSafety(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        CheckPlayerSafety(collision);
    }

    private void CheckPlayerSafety(Collider2D collisionCol)
    {
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

                // プレイヤー（Iceキャラ）が直接触れたときも動かしたい場合
                if (player.Element == ElementType.Ice)
                {
                    TryRaiseBlock();
                }
            }
            else if(areaType == AreaType.Spike)
            {
                if (player.Element == ElementType.Ice) isDeadly = true;
                if (player.Element == ElementType.Fire) isDeadly = true;
            }

            if (isDeadly)
            {
                player.TakeDamage(fatalDamage);
            }
        }
    }

    // ブロックを上げる共通処理
    private void TryRaiseBlock()
    {
        if (WaterBlock == null) return;

        // 現在地、または現在の目標位置がすでに上限に達しているかチェック
        float currentBaseY = shouldRaise ? targetY : WaterBlock.transform.position.y;
        float absoluteMaxY = initialY + maxRaiseHeight;

        // すでに上限に達している場合は何もしない
        if (currentBaseY >= absoluteMaxY)
        {
            Debug.Log("[TryRaiseBlock] これ以上上に上がれません（上限に達しています）。");
            return;
        }

        // 次の目標位置を計算
        targetY = currentBaseY + 1f;

        // もし次の目標が上限を超える場合は、上限ピッタリに丸める
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

        // 上昇中の処理
        if (shouldRaise)
        {
          
            Vector3 pos1 = WaterBlock.transform.position;
            pos1.y = Mathf.MoveTowards(pos1.y, targetY, speed * Time.deltaTime);
            WaterBlock.transform.position = pos1;

      
           
                Vector3 pos2 = WaterBlock2.transform.position;
                pos2.y = Mathf.MoveTowards(pos2.y, targetY, speed * Time.deltaTime);
                WaterBlock2.transform.position = pos2;
            

            // 目標の高さに到達したかチェック
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

        // WaterBlock2 の点滅処理
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