using UnityEngine;

public class OffScreenIndicator : MonoBehaviour
{
    [Header("追尾ターゲット（インスペクターから直接登録、または自動登録）")]
    [SerializeField] private Transform targetPlayer;

    [Header("表示するアイコン（SpriteRendererを持つオブジェクト）")]
    [SerializeField] private SpriteRenderer indicatorSprite;

    [Header("ターゲットの色に応じたアイコン画像（0:赤, 1:青）")]
    [SerializeField] private Sprite[] indicatorSprites;

    [Header("画面の端からどれくらい内側に固定するか（ワールド単位）")]
    [SerializeField] private float margin = 0.5f;

    [Header("アイコンの高さをどれくらい底上げするか（ワールド単位）")]
    [SerializeField] private float offsetY = 1.5f;

    [Header("画面外判定の余裕（ワールド単位）")]
    [SerializeField] private float offsetBuffer = 0.1f;

    [Header("アイコンを回転させるか？")]
    [SerializeField] private bool rotateIcon = true;

    [Header("矢印の初期向き調整（右向き画像なら0、上向きなら-90、左向きなら180）")]
    [SerializeField] private float rotationOffset = -90f;

    [Header("左側に行ったときに画像を左右反転させるか？")]
    [SerializeField] private bool flipXOnLeft = false;

    [Header("デバッグ用")]
    [SerializeField] private bool showDebugLog = true;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }

        if (indicatorSprite != null)
        {
            indicatorSprite.sortingOrder = 20;
            indicatorSprite.gameObject.SetActive(false);
        }
        else if (showDebugLog)
        {
            Debug.LogWarning("[OffScreenIndicator] indicatorSprite が未設定です。", this);
        }

        if (targetPlayer == null && showDebugLog)
        {
            Debug.LogWarning("[OffScreenIndicator] targetPlayer が未設定です。SetupTargetを呼ぶかインスペクターで指定してください。", this);
        }
    }

    public void SetupTarget(Transform target, int charaIndex = -1)
    {
        targetPlayer = target;

        if (charaIndex >= 0 && indicatorSprites != null && charaIndex < indicatorSprites.Length)
        {
            if (indicatorSprite != null && indicatorSprites[charaIndex] != null)
            {
                indicatorSprite.sprite = indicatorSprites[charaIndex];
            }
        }

        if (targetPlayer != null)
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main ?? FindFirstObjectByType<Camera>();
            }

            if (showDebugLog)
            {
                Debug.Log($"[OffScreenIndicator] ターゲット '{target.name}' (Index:{charaIndex}) を認識しました。", this);
            }
        }
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main ?? FindFirstObjectByType<Camera>();
        }

        bool hasTarget = targetPlayer != null && targetPlayer.gameObject.activeInHierarchy;

        if (!hasTarget || indicatorSprite == null || mainCamera == null)
        {
            if (indicatorSprite != null && indicatorSprite.gameObject.activeSelf)
            {
                indicatorSprite.gameObject.SetActive(false);
            }

            if (showDebugLog && Time.frameCount % 60 == 0)
            {
                Debug.LogWarning($"[OffScreenIndicator] 早期終了 -> target:{targetPlayer != null}, targetActive:{targetPlayer?.gameObject.activeInHierarchy}, sprite:{indicatorSprite != null}, cam:{mainCamera != null}", this);
            }
            return;
        }

        Vector3 camPos = mainCamera.transform.position;
        float camHalfHeight = mainCamera.orthographicSize;
        float camHalfWidth = camHalfHeight * mainCamera.aspect;

        Vector3 targetPos = targetPlayer.position;

        bool isOffScreenNow =
            targetPos.x < (camPos.x - camHalfWidth - offsetBuffer) ||
            targetPos.x > (camPos.x + camHalfWidth + offsetBuffer) ||
            targetPos.y < (camPos.y - camHalfHeight - offsetBuffer) ||
            targetPos.y > (camPos.y + camHalfHeight + offsetBuffer);

        if (!isOffScreenNow)
        {
            if (indicatorSprite.gameObject.activeSelf)
            {
                indicatorSprite.gameObject.SetActive(false);
            }

            if (showDebugLog && Time.frameCount % 60 == 0)
            {
                Debug.Log($"[OffScreenIndicator] 画面内判定 -> targetPos:{targetPos}, camPos:{camPos}, halfW:{camHalfWidth:F2}, halfH:{camHalfHeight:F2}", this);
            }
            return;
        }

        if (!indicatorSprite.gameObject.activeSelf)
        {
            indicatorSprite.gameObject.SetActive(true);
        }

        float edgeX = Mathf.Max(0.01f, camHalfWidth - margin);
        float edgeY = Mathf.Max(0.01f, camHalfHeight - margin);

        Vector3 originPos = new Vector3(camPos.x, camPos.y + offsetY, 0f);
        Vector3 dir = new Vector3(targetPos.x - originPos.x, targetPos.y - originPos.y, 0f);

        float scaleX = Mathf.Approximately(dir.x, 0f) ? Mathf.Infinity : edgeX / Mathf.Abs(dir.x);
        float scaleY = Mathf.Approximately(dir.y, 0f) ? Mathf.Infinity : edgeY / Mathf.Abs(dir.y);
        float minScale = Mathf.Min(scaleX, scaleY);

        Vector3 indicatorPos = originPos + dir * minScale;
        indicatorPos.x = Mathf.Clamp(indicatorPos.x, originPos.x - edgeX, originPos.x + edgeX);
        indicatorPos.y = Mathf.Clamp(indicatorPos.y, originPos.y - edgeY, originPos.y + edgeY);
        indicatorPos.z = 0f;

        indicatorSprite.transform.position = indicatorPos;

        if (rotateIcon)
        {
            float rotationAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            indicatorSprite.transform.rotation = Quaternion.Euler(0f, 0f, rotationAngle + rotationOffset);
        }
        else
        {
            indicatorSprite.transform.rotation = Quaternion.identity;
        }

        indicatorSprite.flipX = flipXOnLeft && (dir.x < 0f);

        if (showDebugLog && Time.frameCount % 30 == 0)
        {
            Debug.Log($"[OffScreenIndicator] 画面外表示中 -> pos:{indicatorPos}, spriteActive:{indicatorSprite.gameObject.activeInHierarchy}, sprite:{indicatorSprite.sprite?.name}", this);
        }
    }
}