using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    private Transform targetPlayer;
    private Rigidbody2D playerRb;

    [Header("滑らかさ (数値が小さいほどキビキビ動く)")]
    [SerializeField] private float smoothTimeX = 0.2f;
    [SerializeField] private float smoothTimeY = 0.2f;
    private Vector3 currentVelocity;

    [Header("横方向（X軸）の前方予測（Look-Ahead）")]
    [SerializeField] private float lookAheadFactorX = 2.0f;
    [SerializeField] private float lookAheadMoveSpeedX = 5.0f;
    private float currentLookAheadX;

    [Header("縦方向（Y軸）の前方予測（Look-Ahead）")]
    [SerializeField] private float lookAheadFactorY = 1.0f;
    [SerializeField] private float lookAheadMoveSpeedY = 3.0f;
    private float currentLookAheadY;

    [Header("ステージの境界線（限界値）")]
    [SerializeField] private float minX = -15.0f;
    [SerializeField] private float maxX = 50.0f;
    [SerializeField] private float minY = 0.0f;
    [SerializeField] private float maxY = 0.0f;

    // 前方予測を反映した最終目標位置を Gizmos で描くために保持する変数
    private Vector3 debugFinalTarget;

    public void SetupTarget(Transform player)
    {
        targetPlayer = player;
        if (targetPlayer != null)
        {
            playerRb = targetPlayer.GetComponent<Rigidbody2D>();
        }

        if (targetPlayer != null)
        {
            float startX = Mathf.Clamp(targetPlayer.position.x, minX, maxX);
            float startY = Mathf.Clamp(targetPlayer.position.y, minY, maxY);
            transform.position = new Vector3(startX, startY, transform.position.z);
        }
    }

    void Start()
    {
        if (targetPlayer != null)
        {
            SetupTarget(targetPlayer);
        }
    }

    void FixedUpdate()
    {
        if (targetPlayer == null) return;

        float targetX = targetPlayer.position.x;
        float targetY = targetPlayer.position.y;

        // --- 前方予測（横方向・X軸） ---
        float velX = (playerRb != null) ? playerRb.velocity.x : 0f;
        float targetLeadX = currentLookAheadX;
        if (velX > 0.1f)
        {
            targetLeadX = lookAheadFactorX;
        }
        else if (velX < -0.1f)
        {
            targetLeadX = -lookAheadFactorX;
        }
        currentLookAheadX = Mathf.MoveTowards(currentLookAheadX, targetLeadX, lookAheadMoveSpeedX * Time.fixedDeltaTime);

        // --- 前方予測（縦方向・Y軸） ---
        float velY = (playerRb != null) ? playerRb.velocity.y : 0f;
        float targetLeadY = currentLookAheadY;
        if (velY > 0.1f)
        {
            targetLeadY = lookAheadFactorY;
        }
        else if (velY < -0.1f)
        {
            targetLeadY = -lookAheadFactorY;
        }
        currentLookAheadY = Mathf.MoveTowards(currentLookAheadY, targetLeadY, lookAheadMoveSpeedY * Time.fixedDeltaTime);

        // --- 目標座標の合成とクランプ ---
        Vector3 finalCameraTarget = new Vector3(targetX + currentLookAheadX, targetY + currentLookAheadY, transform.position.z);

        finalCameraTarget.x = Mathf.Clamp(finalCameraTarget.x, minX, maxX);
        finalCameraTarget.y = Mathf.Clamp(finalCameraTarget.y, minY, maxY);

        // デバッグ用に保存（Z軸は0にしておく）
        debugFinalTarget = finalCameraTarget;
        debugFinalTarget.z = 0f;

        // --- スムーズ移動 ---
        float posX = Mathf.SmoothDamp(transform.position.x, finalCameraTarget.x, ref currentVelocity.x, smoothTimeX, Mathf.Infinity, Time.fixedDeltaTime);
        float posY = Mathf.SmoothDamp(transform.position.y, finalCameraTarget.y, ref currentVelocity.y, smoothTimeY, Mathf.Infinity, Time.fixedDeltaTime);

        transform.position = new Vector3(posX, posY, transform.position.z);
    }

    // ★追加：Sceneビューにデバッグ用の線や印を描画する
    void OnDrawGizmos()
    {
        // 1. ステージの境界線（赤色の枠線）を描画
        Gizmos.color = Color.red;
        Vector3 minPoint = new Vector3(minX, minY, 0);
        Vector3 maxPoint = new Vector3(maxX, maxY, 0);

        Vector3 clampCenter = (minPoint + maxPoint) / 2f;
        Vector3 clampSize = new Vector3(Mathf.Abs(maxX - minX), Mathf.Abs(maxY - minY), 0.1f);

        // 縦や横の幅が0だと線が見えなくなるので、最低限の厚みを持たせる
        if (clampSize.x < 0.2f) clampSize.x = 0.2f;
        if (clampSize.y < 0.2f) clampSize.y = 0.2f;

        // 境界線の四角形を描く
        Gizmos.DrawWireCube(clampCenter, clampSize);

        // 2. ゲーム実行中、カメラが現在「目指している目標地点」を青い球で描画
        if (Application.isPlaying && targetPlayer != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(debugFinalTarget, 0.4f);

            // プレイヤーからカメラ目標地点へのガイド線
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(targetPlayer.position, debugFinalTarget);
        }
    }
}