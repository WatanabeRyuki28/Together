using UnityEngine;

/// <summary>
/// 水キャラクターのみが触れて水位を上げることができる貯水ギミック
/// </summary>
public class WaterStorageGimmick : MonoBehaviour
{
    private bool isActivated = false;

    [Header("水位上昇の設定")]
    [SerializeField] private float waterRaiseAmount = 2.0f; // 水位が上がる高さや量
    [SerializeField] private float raiseSpeed = 1.0f;       // 水位が上昇するスピード
    [SerializeField] private GameObject waterObject;        // 実際に動かす水のオブジェクト

    private Vector3 targetPosition;
    private bool shouldRaise = false;

    private void Start()
    {
        if (waterObject != null)
        {
            // 目標の水位位置を計算
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

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isActivated) return;

        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        if (player != null)
        {
            // 水キャラクター（Ice または Water）のみが触れることができる
            if (player.Element == ElementType.Ice)
            {
                ActivateWaterStorage(player);
            }
        }
    }

    private void ActivateWaterStorage(PlayerController player)
    {
        isActivated = true;
        shouldRaise = true;

        Debug.Log($"{player.playerNumber}P（水キャラ）が貯水ギミックに触れました。水位が上昇します。");

        // オンライン対戦などで他のプレイヤーにも状態を同期させたい場合はここでネットワーク送信処理を記述できます
    }
}