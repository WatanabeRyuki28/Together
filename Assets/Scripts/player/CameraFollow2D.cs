using System;
using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    // ========================================================
    // ★古い targetPlayer は廃止。自分（player1）として固定管理します
    // ========================================================
    private Transform player1;
    private Rigidbody2D playerRb;
    private PlayerController playerController;

    [Header("カメラの追従速度 (数値が小さいほどキビキビ動く)")]
    [SerializeField] private float smoothTimeX = 0.2f;
    [SerializeField] private float smoothTimeY = 0.35f;

    [Header("横方向（X軸）の前方予測（Look-Ahead）")]
    [SerializeField] private float lookAheadFactorX = 2.0f;
    [SerializeField] private float lookAheadMoveSpeedX = 5.0f;

    [Header("縦方向（Y軸）の視界・オフセット調整")]
    [SerializeField] private float offsetY_Up = 2.0f;      // 通常・上昇時のカメラの高さ
    [SerializeField] private float offsetY_Down = -1.0f;   // 崖を降りる時のカメラの高さ（マイナス値で下を広く映す）
    [SerializeField] private float offsetChangeSpeed = 2.0f; // 視界が上下に切り替わる時の滑らかさ（小さいほどじわじわ動く）

    [Header("落下（崖降り）の判定基準")]
    [SerializeField] private float fallThresholdY = 1.5f;   // 元の床からどれくらい落ちたら崖降り判定にするか

    [Header("ステージの境界線（限界値）")]
    [SerializeField] private float minX = -15.0f;
    [SerializeField] private float maxX = 50.0f;
    [SerializeField] private float minY = 0.0f;
    [SerializeField] private float maxY = 100.0f;

    // 内部計算用変数
    private Vector3 currentVelocity;
    private float currentOffsetY;
    private float currentLookAheadX;
    private float lastFollowedY;
    private Vector3 debugFinalTarget;

    void FixedUpdate()
    {
        // 生成されるまでは動かない（エラーを防ぐ）
        if (player1 == null) return;

        //  横方向（X軸）の目標位置計算
        float targetX = player1.position.x;

        //  横方向の前方予測（Look-Ahead）
        float velX = (playerRb != null) ? playerRb.velocity.x : 0f;
        float velY = (playerRb != null) ? playerRb.velocity.y : 0f;

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

        // 縦方向（Y軸）の目標位置計算（ジャンプ時固定・下降時滑らかシフト）
        float targetOffsetY_Goal = offsetY_Up;

        // 本当の崖から下に落ちているかどうかの判定
        bool isTrulyFallingDown = (velY < -0.05f) && (player1.position.y < lastFollowedY - fallThresholdY);

        if (playerController != null && playerController.isGrounded)
        {
            // 地面に完全に着地している時
            lastFollowedY = player1.position.y;
            targetOffsetY_Goal = offsetY_Up;
        }
        else if (isTrulyFallingDown)
        {
            // 本当の崖から下に落ちている時
            lastFollowedY = player1.position.y;
            targetOffsetY_Goal = offsetY_Down;
        }
        else
        {
            // 通常のジャンプ（上昇中、または元の床より少し下までの空中）
            targetOffsetY_Goal = offsetY_Up;
        }

        // カメラ位置のオフセット（ズレ）自体をじわじわと補間
        currentOffsetY = Mathf.Lerp(currentOffsetY, targetOffsetY_Goal, offsetChangeSpeed * Time.fixedDeltaTime);

        // カメラが目指す最終的な高さ
        float targetY = lastFollowedY + currentOffsetY;

        //  目標座標の合成とクランプ
        Vector3 finalCameraTarget = new Vector3(targetX + currentLookAheadX, targetY, transform.position.z);
        finalCameraTarget.x = Mathf.Clamp(finalCameraTarget.x, minX, maxX);
        finalCameraTarget.y = Mathf.Clamp(finalCameraTarget.y, minY, maxY);

        debugFinalTarget = finalCameraTarget;
        debugFinalTarget.z = 0f;

        // スムーズ移動の実行
        float posX = Mathf.SmoothDamp(transform.position.x, finalCameraTarget.x, ref currentVelocity.x, smoothTimeX, Mathf.Infinity, Time.fixedDeltaTime);
        float posY = Mathf.SmoothDamp(transform.position.y, finalCameraTarget.y, ref currentVelocity.y, smoothTimeY, Mathf.Infinity, Time.fixedDeltaTime);

        transform.position = new Vector3(posX, posY, transform.position.z);
    }

    // プレイヤー生成時に各PlayerControllerのStartから呼ばれる窓口

    public void AssignPlayer(int assignedCameraIndex, Transform playerTransform)
    {
        if (playerTransform == null) return;

        //  0番が来た時だけガチッとロックオンする
        if (assignedCameraIndex == 0)
        {
            player1 = playerTransform;
            playerRb = player1.GetComponent<Rigidbody2D>();
            playerController = player1.GetComponent<PlayerController>();

            // 生成された位置にカメラをパッと瞬時に合わせる初期化
            lastFollowedY = player1.position.y;
            currentOffsetY = offsetY_Up;
            float startX = Mathf.Clamp(player1.position.x, minX, maxX);
            float startY = Mathf.Clamp(player1.position.y + offsetY_Up, minY, maxY);
            transform.position = new Vector3(startX, startY, transform.position.z);

            Debug.Log("[CameraFollow2D] 【自分】をカメラの追従ターゲットに設定しました！");
        }
        else if (assignedCameraIndex == 1)
        {
            // 1番が来たらログだけ出してスルーする
            Debug.Log("[CameraFollow2D] 相手プレイヤーの登録を受け取りましたが、追従はせず無視します。");
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 minPoint = new Vector3(minX, minY, 0);
        Vector3 maxPoint = new Vector3(maxX, maxY, 0);

        Vector3 clampCenter = (minPoint + maxPoint) / 2f;
        Vector3 clampSize = new Vector3(Mathf.Abs(maxX - minX), Mathf.Abs(maxY - minY), 0.1f);

        if (clampSize.x < 0.2f) clampSize.x = 0.2f;
        if (clampSize.y < 0.2f) clampSize.y = 0.2f;

        Gizmos.DrawWireCube(clampCenter, clampSize);

        if (Application.isPlaying && player1 != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(debugFinalTarget, 0.4f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(player1.position, debugFinalTarget);
        }
    }
}