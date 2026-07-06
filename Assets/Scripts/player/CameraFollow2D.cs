using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    private Transform targetPlayer;
    private Rigidbody2D playerRb;
    private PlayerController playerController; // ★接地状態をチェックするために追加

    [Header("滑らかさ (数値が小さいほどキビキビ動く)")]
    [SerializeField] private float smoothTimeX = 0.2f;
    [Header("段差を上がった時のカメラ移動の遅れ・滑らかさ")]
    [SerializeField] private float smoothTimeY = 0.4f; // ★少し大きめ(0.4〜0.6)にするとフワッと遅れて上がります
    private Vector3 currentVelocity;

    [Header("横方向（X軸）の前方予測（Look-Ahead）")]
    [SerializeField] private float lookAheadFactorX = 2.0f;
    [SerializeField] private float lookAheadMoveSpeedX = 5.0f;
    private float currentLookAheadX;

    // ★縦方向の前方予測は「ジャンプで動かさない」仕様にするため不要になったので削除

    [Header("ステージの境界線（限界値）")]
    [SerializeField] private float minX = -15.0f;
    [SerializeField] private float maxX = 50.0f;
    [SerializeField] private float minY = 0.0f;
    [SerializeField] private float maxY = 15.0f; // ★段差の上に上がれるようにインスペクターで15などに広げてください！

    private float lastGroundedY; // 最後に地面にいた時のプレイヤーのY座標
    private Vector3 debugFinalTarget;

    public void SetupTarget(Transform player)
    {
        targetPlayer = player;
        if (targetPlayer != null)
        {
            playerRb = targetPlayer.GetComponent<Rigidbody2D>();
            playerController = targetPlayer.GetComponent<PlayerController>(); // ★コンポーネント取得

            // 初期位置のY座標を記録
            lastGroundedY = targetPlayer.position.y;
        }

        if (targetPlayer != null)
        {
            float startX = Mathf.Clamp(targetPlayer.position.x, minX, maxX);
            float startY = Mathf.Clamp(lastGroundedY, minY, maxY);
            transform.position = new Vector3(startX, startY, transform.position.z);
        }
    }


    public void AssignPlayer(int playerIndex, Transform playerTransform)
    {
        if (playerIndex == 0)
        {
            player1 = playerTransform;
            if (playerTransform != null) p1Rb = playerTransform.GetComponent<Rigidbody2D>();
            Debug.Log($"[Camera] 1Pの参照とRigidbody2Dを割り当てました: {playerTransform.name}");
        }
        else if (playerIndex == 1)
        {
            player2 = playerTransform;
            if (playerTransform != null) p2Rb = playerTransform.GetComponent<Rigidbody2D>();
            Debug.Log($"[Camera] 2Pの参照とRigidbody2Dを割り当てました: {playerTransform.name}");
        }

        // プレイヤーが登録された直後にカメラの初期目標位置をリセットする
        Vector3 initialTarget = GetPlayersCenterPosition();
        float startX = Mathf.Clamp(initialTarget.x, minX, maxX);
        float startY = Mathf.Clamp(initialTarget.y, minY, maxY);
        cameraTargetPos = new Vector3(startX, startY, transform.position.z);
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

        // --- 1. 横方向（X軸）の目標位置計算 ---
        float targetX = targetPlayer.position.x;

        // --- 2. 横方向の前方予測（Look-Ahead） ---
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

        // --- ★3. 縦方向（Y軸）の目標位置計算（ここがキモ！） ---
        // プレイヤーが地面に着いている（またはPlayerControllerがない）場合だけ、カメラの目標高さを更新する
        if (playerController == null || playerController.isGrounded)
        {
            lastGroundedY = targetPlayer.position.y;
        }

        // カメラが目指す高さは、最後に着地していた床の高さ
        float targetY = lastGroundedY;

        // --- 4. 目標座標の合成とクランプ ---
        Vector3 finalCameraTarget = new Vector3(targetX + currentLookAheadX, targetY, transform.position.z);

        finalCameraTarget.x = Mathf.Clamp(finalCameraTarget.x, minX, maxX);
        finalCameraTarget.y = Mathf.Clamp(finalCameraTarget.y, minY, maxY);

        debugFinalTarget = finalCameraTarget;
        debugFinalTarget.z = 0f;

        // --- 5. スムーズ移動（XとYで追従の速度を変える） ---
        float posX = Mathf.SmoothDamp(transform.position.x, finalCameraTarget.x, ref currentVelocity.x, smoothTimeX, Mathf.Infinity, Time.fixedDeltaTime);
        // Y軸は smoothTimeY を使って、段差を上がった後に遅れてフワッと追いつかせる
        float posY = Mathf.SmoothDamp(transform.position.y, finalCameraTarget.y, ref currentVelocity.y, smoothTimeY, Mathf.Infinity, Time.fixedDeltaTime);

        transform.position = new Vector3(posX, posY, transform.position.z);
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

        if (Application.isPlaying && targetPlayer != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(debugFinalTarget, 0.4f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(targetPlayer.position, debugFinalTarget);
        }
    }
}