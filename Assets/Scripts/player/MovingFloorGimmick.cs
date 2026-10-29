using UnityEngine;

/// <summary>
/// 炎キャラクターのみが動かすことができ、パネルに乗っている間だけ床が動くギミック
/// </summary>
public class MovingFloorGimmick : MonoBehaviour
{
    [Header("移動設定")]
    [SerializeField] private Transform movableFloor;     // 実際に動かす床のトランスフォーム
    [SerializeField] private Vector3 moveOffset = new Vector3(5f, 0f, 0f); // パネル起動時の移動オフセット（相対距離）
    [SerializeField] private float moveSpeed = 3.0f;     // 床が動く速度

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool isPlayerOnPanel = false;

    private void Start()
    {
        if (movableFloor != null)
        {
            startPosition = movableFloor.position;
            targetPosition = startPosition + moveOffset;
        }
    }

    private void Update()
    {
        if (movableFloor == null) return;

        // 炎キャラがパネルに乗っている間は目標地点へ、それ以外は初期位置へ滑らかに戻る
        Vector3 destination = isPlayerOnPanel ? targetPosition : startPosition;
        movableFloor.position = Vector3.MoveTowards(
            movableFloor.position,
            destination,
            moveSpeed * Time.deltaTime
        );
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        if (player != null)
        {
            // 炎キャラクター（Fire）のみがパネルを動かせる
            if (player.Element == ElementType.Fire)
            {
                isPlayerOnPanel = true;
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        if (player != null)
        {
            if (player.Element == ElementType.Fire)
            {
                isPlayerOnPanel = false;
            }
        }
    }
}