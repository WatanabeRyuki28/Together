using UnityEngine;

public class OffScreenIndicator : MonoBehaviour
{
    [Header("追尾ターゲット（インスペクターから直接登録）")]
    [SerializeField] private Transform targetPlayer;

    [Header("表示するアイコン（SpriteRendererを持つオブジェクト）")]
    [SerializeField] private SpriteRenderer indicatorSprite;

    [Header("画面の端からどれくらい内側に固定するか（ワールド単位）")]
    [SerializeField] private float margin = 0.5f;

    [Header("アイコンの高さをどれくらい底上げするか（ワールド単位）")]
    [SerializeField] private float offsetY = 1.5f;

    [Header("アイコンを回転させるか？")]
    [SerializeField] private bool rotateIcon = true;

    [Header("矢印の初期向き調整（右向き画像なら0、上向きなら-90、左向きなら180）")]
    [SerializeField] private float rotationOffset = -90f;

    [Header("★左側に行ったときに画像を左右反転させるか？")]
    [SerializeField] private bool flipXOnLeft = false;

    private Camera mainCamera;
    private SpriteRenderer playerSpriteRenderer;

    private void Start()
    {
        mainCamera = Camera.main;

        if (indicatorSprite != null)
        {
            indicatorSprite.gameObject.SetActive(false);
        }

        if (targetPlayer != null)
        {
            playerSpriteRenderer = targetPlayer.GetComponent<SpriteRenderer>();
        }
    }

    // ★追加：生成スクリプトからこれを呼び出してターゲットを登録する
    public void SetupTarget(Transform target)
    {
        targetPlayer = target;
        if (targetPlayer != null)
        {
            playerSpriteRenderer = targetPlayer.GetComponent<SpriteRenderer>();
        }
    }

    private void FixedUpdate()
    {
        if (targetPlayer == null || !targetPlayer.gameObject.activeInHierarchy || indicatorSprite == null || mainCamera == null)
        {
            if (indicatorSprite != null) indicatorSprite.gameObject.SetActive(false);
            return;
        }

        if (playerSpriteRenderer == null || playerSpriteRenderer.transform != targetPlayer)
        {
            playerSpriteRenderer = targetPlayer.GetComponent<SpriteRenderer>();
        }

        Vector3 camPos = mainCamera.transform.position;
        float camHalfHeight = mainCamera.orthographicSize;
        float camHalfWidth = camHalfHeight * mainCamera.aspect;

        float playerHalfWidth = 0f;
        float playerHalfHeight = 0f;
        if (playerSpriteRenderer != null)
        {
            playerHalfWidth = playerSpriteRenderer.bounds.extents.x;
            playerHalfHeight = playerSpriteRenderer.bounds.extents.y;
        }

        // 完全画面外判定
        bool isOffScreenNow = targetPlayer.position.x < (camPos.x - camHalfWidth - playerHalfWidth) ||
                             targetPlayer.position.x > (camPos.x + camHalfWidth + playerHalfWidth) ||
                             targetPlayer.position.y < (camPos.y - camHalfHeight - playerHalfHeight) ||
                             targetPlayer.position.y > (camPos.y + camHalfHeight + playerHalfHeight);

        if (isOffScreenNow)
        {
            indicatorSprite.gameObject.SetActive(true);

            float edgeX = camHalfWidth - margin;
            float edgeY = camHalfHeight - margin;

            // 【位置用】高さを底上げした中心点
            Vector3 centerOffsetPos = camPos;
            centerOffsetPos.y += offsetY;

            Vector3 playerDir = targetPlayer.position - centerOffsetPos;
            playerDir.z = 0f;

            // 交点計算
            float mX = Mathf.Infinity;
            float mY = Mathf.Infinity;

            if (playerDir.x != 0) mX = Mathf.Abs(edgeX / playerDir.x);
            if (playerDir.y != 0) mY = Mathf.Abs(edgeY / playerDir.y);

            float minM = Mathf.Min(mX, mY);

            Vector3 indicatorPos = centerOffsetPos + (playerDir * minM);

            indicatorPos.x = Mathf.Clamp(indicatorPos.x, camPos.x - edgeX, camPos.x + edgeX);
            indicatorPos.y = Mathf.Clamp(indicatorPos.y, camPos.y - edgeY, camPos.y + edgeY);
            indicatorPos.z = 0f;

            indicatorSprite.transform.position = indicatorPos;

            // --- 回転と反転の計算 ---
            Vector3 pureDirection = targetPlayer.position - camPos;
            pureDirection.z = 0f;

            // プレイヤーがカメラより「左側」にいるかどうか
            bool isPlayerOnLeft = pureDirection.x < 0;

            if (rotateIcon)
            {
                float rotationAngle = Mathf.Atan2(pureDirection.y, pureDirection.x) * Mathf.Rad2Deg;

                // 左側にいて、かつ単純な角度計算だと逆を向く画像のための追加補正
                // もし「左に行ったときだけ変」なら、インスペクターで flipXOnLeft をオンにするか、
                // ここの角度に +180 する調整が効きます
                indicatorSprite.transform.rotation = Quaternion.Euler(0f, 0f, rotationAngle + rotationOffset);
            }
            else
            {
                indicatorSprite.transform.rotation = Quaternion.identity;
            }

            // スプライト自体の反転機能（Flip X）を使って無理やり向きを合わせる設定
            if (flipXOnLeft)
            {
                indicatorSprite.flipX = isPlayerOnLeft;
            }
        }
        else
        {
            indicatorSprite.gameObject.SetActive(false);
        }
    }
}