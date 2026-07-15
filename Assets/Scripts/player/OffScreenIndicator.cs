using UnityEngine;

public class OffScreenIndicator : MonoBehaviour
{
    [Header("追尾ターゲット（インスペクターから直接登録、または自動登録）")]
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

        // 初期状態ではアイコンを隠しておく
        if (indicatorSprite != null)
        {
            indicatorSprite.gameObject.SetActive(false);
        }

        if (targetPlayer != null)
        {
            playerSpriteRenderer = targetPlayer.GetComponent<SpriteRenderer>();
        }
    }

    // 生成スクリプトからこれを呼び出してターゲットを登録する
    public void SetupTarget(Transform target)
    {
        targetPlayer = target;
        if (targetPlayer != null)
        {
            playerSpriteRenderer = targetPlayer.GetComponent<SpriteRenderer>();
            // ターゲットが設定されたら、メインカメラを再取得して準備を確実にする
            if (mainCamera == null) mainCamera = Camera.main;

            Debug.Log($"[OffScreenIndicator] ターゲット '{target.name}' を正常に認識しました。");
        }
    }

    private void FixedUpdate()
    {
        // ターゲットがいない、または非アクティブならインジケーターを消して終了
        if (targetPlayer == null || !targetPlayer.gameObject.activeInHierarchy || indicatorSprite == null || mainCamera == null)
        {
            if (indicatorSprite != null && indicatorSprite.gameObject.activeSelf)
            {
                indicatorSprite.gameObject.SetActive(false);
            }
            return;
        }

        Vector3 camPos = mainCamera.transform.position;
        float camHalfHeight = mainCamera.orthographicSize;
        float camHalfWidth = camHalfHeight * mainCamera.aspect;

        // ★改善：プレイヤーのサイズ自動取得を廃止し、中心点の座標だけでシンプルに判定する
        // わずかに画面の内側に入ったら消えるように、マージン分（0.1fなど）だけ余裕を持たせます
        float offsetBuffer = 0.1f;

        bool isOffScreenNow = targetPlayer.position.x < (camPos.x - camHalfWidth - offsetBuffer) ||
                             targetPlayer.position.x > (camPos.x + camHalfWidth + offsetBuffer) ||
                             targetPlayer.position.y < (camPos.y - camHalfHeight - offsetBuffer) ||
                             targetPlayer.position.y > (camPos.y + camHalfHeight + offsetBuffer);

        if (isOffScreenNow)
        {
            // 画面外のときだけアクティブにする
            if (!indicatorSprite.gameObject.activeSelf)
            {
                indicatorSprite.gameObject.SetActive(true);
                Debug.Log($"[OffScreenIndicator] ターゲットが画面外に出たため、アイコンを呼び出しました！位置: {targetPlayer.position}");
            }

            float edgeX = camHalfWidth - margin;
            float edgeY = camHalfHeight - margin;

            // 【位置・回転の共通基準】高さを底上げした中心点
            Vector3 centerOffsetPos = camPos;
            centerOffsetPos.y += offsetY;

            // 共通の基準点からターゲットへのベクトル
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
            bool isPlayerOnLeft = playerDir.x < 0;

            if (rotateIcon)
            {
                float rotationAngle = Mathf.Atan2(playerDir.y, playerDir.x) * Mathf.Rad2Deg;
                indicatorSprite.transform.rotation = Quaternion.Euler(0f, 0f, rotationAngle + rotationOffset);
            }
            else
            {
                indicatorSprite.transform.rotation = Quaternion.identity;
            }

            if (flipXOnLeft)
            {
                indicatorSprite.flipX = isPlayerOnLeft;
            }
        }
        else
        {
            // 画面内にいるときは非アクティブにする
            if (indicatorSprite.gameObject.activeSelf)
            {
                indicatorSprite.gameObject.SetActive(false);
                Debug.Log("[OffScreenIndicator] ターゲットが画面内に戻ったため、アイコンを非表示にしました。");
            }
        }
    }
}