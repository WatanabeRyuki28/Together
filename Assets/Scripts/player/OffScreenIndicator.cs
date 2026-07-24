using UnityEngine;

public class OffScreenIndicator : MonoBehaviour
{
    [Header("追尾ターゲット（インスペクターから直接登録、または自動登録）")]
    [SerializeField] private Transform targetPlayer;

    [Header("表示するアイコン（SpriteRendererを持つオブジェクト）")]
    [SerializeField] private SpriteRenderer indicatorSprite;

    // ★追加: 0:赤アイコン, 1:青アイコン などを割り当てる配列
    [Header("ターゲットの色に応じたアイコン画像（0:赤, 1:青）")]
    [SerializeField] private Sprite[] indicatorSprites;

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
            indicatorSprite.sortingOrder = 20;
            indicatorSprite.gameObject.SetActive(false);
        }

        if (targetPlayer != null)
        {
            playerSpriteRenderer = targetPlayer.GetComponent<SpriteRenderer>();
        }
    }

    // ★修正: charaIndex を追加で受け取るように変更
    public void SetupTarget(Transform target, int charaIndex = -1)
    {
        targetPlayer = target;

        // ★相手のキャラ番号（0:赤, 1:青）に合わせてアイコン画像を差し替える
        if (charaIndex >= 0 && indicatorSprites != null && charaIndex < indicatorSprites.Length)
        {
            if (indicatorSprite != null && indicatorSprites[charaIndex] != null)
            {
                indicatorSprite.sprite = indicatorSprites[charaIndex];
            }
        }

        if (targetPlayer != null)
        {
            playerSpriteRenderer = targetPlayer.GetComponent<SpriteRenderer>();
            if (mainCamera == null) mainCamera = Camera.main;

            Debug.Log($"[OffScreenIndicator] ターゲット '{target.name}' (Index:{charaIndex}) を正常に認識しました。");
        }
    }

    private void FixedUpdate()
    {
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

        float offsetBuffer = 0.1f;

        bool isOffScreenNow = targetPlayer.position.x < (camPos.x - camHalfWidth - offsetBuffer) ||
                             targetPlayer.position.x > (camPos.x + camHalfWidth + offsetBuffer) ||
                             targetPlayer.position.y < (camPos.y - camHalfHeight - offsetBuffer) ||
                             targetPlayer.position.y > (camPos.y + camHalfHeight + offsetBuffer);

        if (isOffScreenNow)
        {
            if (!indicatorSprite.gameObject.activeSelf)
            {
                indicatorSprite.gameObject.SetActive(true);
            }

            float edgeX = camHalfWidth - margin;
            float edgeY = camHalfHeight - margin;

            Vector3 centerOffsetPos = camPos;
            centerOffsetPos.y += offsetY;

            Vector3 playerDir = targetPlayer.position - centerOffsetPos;
            playerDir.z = 0f;

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
            if (indicatorSprite.gameObject.activeSelf)
            {
                indicatorSprite.gameObject.SetActive(false);
            }
        }
    }
}