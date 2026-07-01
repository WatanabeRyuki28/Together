using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [Header("追従対象（インスペクターから直接登録）")]
    [SerializeField] private Transform player1;
    [SerializeField] private Transform player2;

    private Rigidbody2D p1Rb;
    private Rigidbody2D p2Rb;

    [Header("滑らかさ (数値が小さいほどキビキビ動く)")]
    [SerializeField] private float smoothTime = 0.2f;
    private Vector3 currentVelocity;

    [Header("デッドゾーンのサイズ (中心からの半径)")]
    [SerializeField] private float deadZoneWidth = 1.5f;
    [SerializeField] private float deadZoneHeight = 1.5f;

    [Header("★工夫2：前方予測（Look-Ahead）の強さ")]
    [SerializeField] private float lookAheadFactor = 2.0f; // ★戻らなくしたため、ここの数値を大きめ（1.5〜3.0等）にするのがオススメです
    [SerializeField] private float lookAheadMoveSpeed = 5.0f; // 先行位置へ追いつく速度
    private float currentLookAheadX;

    [Header("ステージの境界線（限界値）")]
    [SerializeField] private float minX = -15.0f;
    [SerializeField] private float maxX = 50.0f;
    [SerializeField] private float minY = 0.0f;
    [SerializeField] private float maxY = 0.0f;

    private Vector3 cameraTargetPos;
    private bool wasBothAlive = true;

    void Start()
    {
        RefreshRigidbodyReferences();

        Vector3 initialTarget = GetPlayersCenterPosition();
        float startX = Mathf.Clamp(initialTarget.x, minX, maxX);
        float startY = Mathf.Clamp(initialTarget.y, minY, maxY);

        cameraTargetPos = new Vector3(startX, startY, transform.position.z);
        transform.position = cameraTargetPos;
    }

    void FixedUpdate()
    {
        if (player1 == null && player2 == null) return;

        bool isBothAliveNow = (player1 != null && player2 != null);
        Vector3 centerPosition = GetPlayersCenterPosition();

        float targetX = cameraTargetPos.x;
        float targetY = cameraTargetPos.y;

        if (wasBothAlive && !isBothAliveNow)
        {
            cameraTargetPos = new Vector3(transform.position.x, transform.position.y, transform.position.z);
            wasBothAlive = false;
        }
        else
        {
            // --- デッドゾーンの計算 ---
            if (centerPosition.x > cameraTargetPos.x + deadZoneWidth)
            {
                targetX = centerPosition.x - deadZoneWidth;
            }
            else if (centerPosition.x < cameraTargetPos.x - deadZoneWidth)
            {
                targetX = centerPosition.x + deadZoneWidth;
            }

            if (centerPosition.y > cameraTargetPos.y + deadZoneHeight)
            {
                targetY = centerPosition.y - deadZoneHeight;
            }
            else if (centerPosition.y < cameraTargetPos.y - deadZoneHeight)
            {
                targetY = centerPosition.y + deadZoneHeight;
            }

            targetX = Mathf.Clamp(targetX, minX, maxX);
            targetY = Mathf.Clamp(targetY, minY, maxY);

            cameraTargetPos = new Vector3(targetX, targetY, transform.position.z);
        }

        if (isBothAliveNow)
        {
            wasBothAlive = true;
        }

        // --- ★修正：戻らない前方予測の計算 ---
        float averageVelocityX = GetPlayersAverageVelocityX();

        // プレイヤーが一定以上の速度で動いている時だけ、その方向（右か左か）へ目標値を更新する
        // 立ち止まった（速度がほぼ0）ときは targetLeadX を更新せず、前回の値をそのままキープする
        float targetLeadX = currentLookAheadX;
        if (averageVelocityX > 0.1f)
        {
            targetLeadX = lookAheadFactor; // 右に進んでいる時は右に固定
        }
        else if (averageVelocityX < -0.1f)
        {
            targetLeadX = -lookAheadFactor; // 左に進んでいる時は左に固定
        }

        // 現在の先行量を目標値に向けて滑らかに近づける（止まっても0に戻らない）
        currentLookAheadX = Mathf.MoveTowards(currentLookAheadX, targetLeadX, lookAheadMoveSpeed * Time.fixedDeltaTime);

        // 最終的な目標座標の計算
        Vector3 finalCameraTarget = cameraTargetPos;
        finalCameraTarget.x += currentLookAheadX;
        finalCameraTarget.x = Mathf.Clamp(finalCameraTarget.x, minX, maxX);

        // スムーズに追従
        transform.position = Vector3.SmoothDamp(transform.position, finalCameraTarget, ref currentVelocity, smoothTime, Mathf.Infinity, Time.fixedDeltaTime);
    }

    private float GetPlayersAverageVelocityX()
    {
        if (player1 == null && player2 == null) return 0f;
        if (player1 == null) return (p2Rb != null) ? p2Rb.velocity.x : 0f;
        if (player2 == null) return (p1Rb != null) ? p1Rb.velocity.x : 0f;
        return (p1Rb.velocity.x + p2Rb.velocity.x) / 2f;
    }

    private Vector3 GetPlayersCenterPosition()
    {
        if (player1 == null && player2 == null) return Vector3.zero;
        if (player1 == null) return player2.position;
        if (player2 == null) return player1.position;
        return (player1.position + player2.position) / 2f;
    }

    private void RefreshRigidbodyReferences()
    {
        if (player1 != null) p1Rb = player1.GetComponent<Rigidbody2D>();
        if (player2 != null) p2Rb = player2.GetComponent<Rigidbody2D>();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Vector3 center = (Application.isPlaying) ? cameraTargetPos : transform.position;
        center.z = 0;
        Gizmos.DrawWireCube(center, new Vector3(deadZoneWidth * 2, deadZoneHeight * 2, 0));

        Gizmos.color = Color.red;
        Vector3 minPoint = new Vector3(minX, minY, 0);
        Vector3 maxPoint = new Vector3(maxX, maxY, 0);
        Vector3 clampCenter = (minPoint + maxPoint) / 2f;
        Vector3 clampSize = new Vector3(Mathf.Abs(maxX - minX), Mathf.Abs(maxY - minY), 0);
        if (clampSize.x < 0.1f) clampSize.x = 0.1f;
        if (clampSize.y < 0.1f) clampSize.y = 0.1f;
        Gizmos.DrawWireCube(clampCenter, clampSize);

        if (Application.isPlaying && (player1 != null || player2 != null))
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(GetPlayersCenterPosition(), 0.3f);
        }
    }
}