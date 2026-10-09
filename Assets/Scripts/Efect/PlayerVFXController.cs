using UnityEngine;
using UnityEngine.VFX;

public class PlayerVFXController : MonoBehaviour
{
    [Header("Single VFX Settings (Attack / Jump etc.)")]
    [SerializeField] private GameObject effectPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float destroyDelay = 2.0f;

    [Header("Continuous Run VFX Settings")]
    [SerializeField] private VisualEffect runDustVfx;
    [SerializeField] private float runSpawnRate = 300f;
    [SerializeField] private float moveThreshold = 0.1f;
    [SerializeField] private Rigidbody2D rb2d;

    // Input Axis Names
    private const string HorizontalAxis = "Horizontal";
    private const string VerticalAxis = "Vertical";
    private const string Fire1Button = "Fire1";

    // Shader Property IDs
    private static readonly int OnPlayEventID = Shader.PropertyToID("OnPlay");
    private static readonly int SpawnRateID = Shader.PropertyToID("SpawnRate");

    // Constants
    private const float YAngleFlipped = 180f;
    private const float YAngleNormal = 0f;
    private const float ZeroRate = 0f;
    private const float ZeroThreshold = 0f;

    void Update()
    {
        // 1. 走りの土煙エフェクト制御
        HandleRunEffect();

        // 2. 単発エフェクトの入力検知（スペースキー／Fire1）
        if (Input.GetButtonDown(Fire1Button) || Input.GetKeyDown(KeyCode.Space))
        {
            PlayEffect();
        }
    }

    /// <summary>
    /// 移動入力または物理速度に応じて足元のダッシュエフェクトを動的制御する
    /// </summary>
    private void HandleRunEffect()
    {
        if (runDustVfx == null)
        {
            Debug.LogError("[VFX Diagnostic] RunDustVfx が Inspector でアタッチされていません！");
            return;
        }

        // キー入力値の取得（Axis）
        float horizontalInput = Mathf.Abs(Input.GetAxisRaw(HorizontalAxis));
        float verticalInput = Mathf.Abs(Input.GetAxisRaw(VerticalAxis));

        // A / D / 左矢印 / 右矢印 キーの直接入力チェック
        bool isKeyHeld = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D) ||
                         Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow);

        bool hasInput = horizontalInput > moveThreshold || verticalInput > moveThreshold || isKeyHeld;

        // Rigidbody2D 速度の取得
        float currentSpeed = rb2d != null ? rb2d.velocity.magnitude : ZeroThreshold;
        bool hasPhysicsVelocity = currentSpeed > moveThreshold;

        // 診断用ログ（A/Dキーを押した時のみ出力）
        if (isKeyHeld)
        {
            Debug.Log($"[VFX Diagnostic] キー押下検知! hasInput: {hasInput}, currentSpeed: {currentSpeed}, threshold: {moveThreshold}");
        }

        // 入力がある、または物理速度がある場合にエフェクト発生
        if (hasInput || hasPhysicsVelocity)
        {
            runDustVfx.SetFloat(SpawnRateID, runSpawnRate);

            // プレイヤーの向き（Scale X）に合わせてエフェクトの向きを反転
            float yRotation = transform.localScale.x < ZeroThreshold ? YAngleFlipped : YAngleNormal;
            runDustVfx.transform.localRotation = Quaternion.Euler(ZeroThreshold, yRotation, ZeroThreshold);
        }
        else
        {
            runDustVfx.SetFloat(SpawnRateID, ZeroRate);
        }
    }

    /// <summary>
    /// 単発エフェクトを生成して再生する（攻撃・ジャンプなど）
    /// </summary>
    private void PlayEffect()
    {
        if (effectPrefab == null)
        {
            Debug.LogWarning("VFX Prefabが設定されていません。");
            return;
        }

        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

        GameObject vfxInstance = Instantiate(effectPrefab, spawnPosition, spawnRotation);

        if (vfxInstance.TryGetComponent<VisualEffect>(out var vfx))
        {
            vfx.SendEvent(OnPlayEventID);
        }

        Destroy(vfxInstance, destroyDelay);
    }
}