using UnityEngine;

/// <summary>
/// 水の弾（攻撃）が当たったときのみ水位が上昇する貯水ギミック（接触では反応しない）
/// </summary>
public class WaterStorageGimmick : MonoBehaviour, IInteractable
{
    [Header("水位上昇の設定")]
    [SerializeField] private float waterRaiseAmount = 2.0f; // 水位が上がる高さや量
    [SerializeField] private float raiseSpeed = 1.0f;       // 水位が上昇するスピード
    [SerializeField] private GameObject waterObject;        // 実際に動かす水のオブジェクト

    [Header("制限設定")]
    [SerializeField] private bool canRaiseMultipleTimes = false; // 何回も水位を上げられるようにするかどうか
    private bool hasRaised = false;

    private Vector3 targetPosition;
    private bool shouldRaise = false;

    private void Start()
    {
        if (waterObject != null)
        {
            // 初期位置から目標の水位位置を計算
            targetPosition = waterObject.transform.position + new Vector3(0f, waterRaiseAmount, 0f);
        }
    }

    private void Update()
    {
        // 起動フラグが立ったら、水オブジェクトを滑らかに上昇させる
        if (shouldRaise && waterObject != null)
        {
            waterObject.transform.position = Vector3.MoveTowards(
                waterObject.transform.position,
                targetPosition,
                raiseSpeed * Time.deltaTime
            );
        }
    }

    /// <summary>
    /// 水の弾（Projectile）などの攻撃が当たったときにのみ呼ばれる
    /// </summary>
    public void OnInteract(ElementType element)
    {
        if (!canRaiseMultipleTimes && hasRaised) return;

        // 水の攻撃（ElementType.Ice）が当たった場合のみ起動する
        if (element == ElementType.Ice)
        {
            hasRaised = true;
            shouldRaise = true;

            Debug.Log("水に攻撃が当たったため、水位が上昇します！");
        }
    }
}