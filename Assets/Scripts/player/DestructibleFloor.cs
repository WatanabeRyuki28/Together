using System.Collections;
using UnityEngine;

public class DestructibleFloor : MonoBehaviour
{
    // --- マジックナンバー排除用の定数定義 ---
    private const float HalfColorMultiplier = 0.5f; // 片方乗った時のスプライト減色率
    private const float DefaultRespawnDelay = 3.0f;  // デフォルトの復活待機時間（秒）

    [Header("Floor Settings")]
    [SerializeField] private ElementType breakableBy; // どの属性で壊れるか
    [SerializeField] private bool needsBoth = false;   // 両方の属性で乗る必要があるか（協力用）

    [Header("Slippery Settings (滑る床の設定)")]
    [SerializeField] private PhysicsMaterial2D slipperyMaterial; // 摩擦0に設定したPhysicsMaterial2Dをアサイン

    [Header("Respawn Settings (再生成設定)")]
    [SerializeField] private bool canRespawn = true;             // 時間経過で復活するか
    [SerializeField] private float respawnDelay = DefaultRespawnDelay; // 壊れてから復活するまでの時間（秒）

    [Header("Audio Settings (効果音)")]
    [SerializeField] private AudioClip breakSound;       // 床が壊れた時の音
    [SerializeField] private AudioClip layerHitSound;    // 【協力用】片方乗った時の音
    [SerializeField] private AudioClip failSound;        // 属性が違って壊れなかった時の音

    [Header("Effect Settings (演出)")]
    [SerializeField] private GameObject breakEffectPrefab; // 共通のパーティクルプレハブ

    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private Collider2D[] floorColliders; // 床のコライダー配列

    private bool hitByFire = false;
    private bool hitByIce = false;
    private bool isBreaking = false;

    private Color baseColor;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        floorColliders = GetComponents<Collider2D>();

        // AudioSourceをあらかじめ取得・追加しておく
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 0f; // 2D音響設定
    }

    private void Start()
    {
        if (spriteRenderer != null)
        {
            baseColor = spriteRenderer.color;
        }

        // インスペクターで SlipperyMaterial が設定されていればコライダーに適用する
        ApplySlipperyMaterial();
    }

    /// <summary>
    /// 床のコライダーに滑る材質を適用する
    /// </summary>
    /// <summary>
    /// 床の物理コライダーに滑る材質を適用する
    /// </summary>
    private void ApplySlipperyMaterial()
    {
        if (slipperyMaterial == null || floorColliders == null) return;

        foreach (var col in floorColliders)
        {
            // nullチェック 兼 Triggerでない（実体のある）コライダーにのみマテリアルをセット
            if (col != null && !col.isTrigger)
            {
                col.sharedMaterial = slipperyMaterial;
            }
        }
    }

    // 💡 Trigger判定（Is Triggerにチェックが入っている場合）
    private void OnTriggerEnter2D(Collider2D collision)
    {
        HandleStepOn(collision.gameObject);
    }

    // 💡 物理Collision判定（固い床の場合）
    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleStepOn(collision.gameObject);
    }

    private void HandleStepOn(GameObject steppedObject)
    {
        if (isBreaking) return;

        // ★ PlayerControllerからプロパティ（Element）を取得
        if (steppedObject.TryGetComponent<PlayerController>(out var player))
        {
            ElementType playerType = player.Element;
            TryBreak(playerType);
        }
    }

    private void TryBreak(ElementType type)
    {
        if (needsBoth)
        {
            // すでに両方揃っている場合は処理しない
            if (hitByFire && hitByIce) return;

            // すでに踏んだ属性と同じなら無視
            if (type == ElementType.Fire && hitByFire) return;
            if (type == ElementType.Ice && hitByIce) return;

            if (type == ElementType.Fire) hitByFire = true;
            if (type == ElementType.Ice) hitByIce = true;

            UpdateVisuals();

            // 両方の属性が揃ったら破壊シークエンスへ
            if (hitByFire && hitByIce)
            {
                StartBreakSequence();
            }
            else
            {
                PlaySound(layerHitSound);
            }
        }
        else
        {
            if (type == breakableBy)
            {
                StartBreakSequence();
            }
            else
            {
                PlaySound(failSound);
            }
        }
    }

    private void UpdateVisuals()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = baseColor * HalfColorMultiplier;
        }
    }

    private void StartBreakSequence()
    {
        isBreaking = true;

        if (breakSound != null)
        {
            AudioSource.PlayClipAtPoint(breakSound, transform.position);
        }

        SpawnBreakEffect();

        // --------------------------------------------------
        // ★【破壊・リスポーンギミックの無効化】
        // 床が消えたり削除されたりしないように処理をコメントアウト
        // --------------------------------------------------
        /*
        if (canRespawn)
        {
            StartCoroutine(RespawnRoutine());
        }
        else
        {
            Destroy(gameObject);
        }
        */
    }

    /// <summary>
    /// 床の表示と当たり判定の有効/無効を一括切り替え
    /// </summary>
    private void SetFloorActive(bool active)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = active;
        }

        if (floorColliders != null)
        {
            foreach (var col in floorColliders)
            {
                if (col != null)
                {
                    col.enabled = active;
                }
            }
        }
    }

    private void SpawnBreakEffect()
    {
        if (breakEffectPrefab == null) return;

        Instantiate(
            breakEffectPrefab,
            transform.position,
            Quaternion.identity
        );
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip);
    }
}