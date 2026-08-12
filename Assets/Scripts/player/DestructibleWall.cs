using System.Collections;
using UnityEngine;

/// <summary>
/// 炎属性専用の破壊可能な壁
/// </summary>
public class FireDestructibleWall : MonoBehaviour, IInteractable
{
    // --- マジックナンバー排除用の定数定義 ---
    private const float DefaultRespawnDelay = 3.0f; // 復活までの秒数

    [Header("Respawn Settings (再生成設定)")]
    [SerializeField] private bool canRespawn = false;              // 時間経過で復活するか
    [SerializeField] private float respawnDelay = DefaultRespawnDelay; // 復活するまでの秒数

    [Header("Audio Settings (効果音)")]
    [SerializeField] private AudioClip breakSound; // 破壊時の効果音
    [SerializeField] private AudioClip failSound;  // 氷属性などの攻撃を弾いた時の効果音

    [Header("Effect Settings (演出)")]
    [SerializeField] private GameObject breakEffectPrefab; // 破壊時のエフェクトプレハブ

    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private Collider2D[] wallColliders;

    private bool isBreaking = false;
    private Color baseColor;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        wallColliders = GetComponents<Collider2D>();

        // AudioSourceのキャッシュ
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 0f;
    }

    private void Start()
    {
        if (spriteRenderer != null)
        {
            baseColor = spriteRenderer.color;
        }
    }

    /// <summary>
    /// インタラクト時の判定（弾や攻撃が当たった時に呼ばれる）
    /// </summary>
    public void OnInteract(ElementType type)
    {
        if (isBreaking) return;

        // 🔥 炎属性のみ破壊可能
        if (type == ElementType.Fire)
        {
            StartBreakSequence();
        }
        else
        {
            // 氷など他の属性の場合は弾かれる（失敗音）
            PlaySound(failSound);
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

        if (canRespawn)
        {
            StartCoroutine(RespawnRoutine());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 壁を一定時間後に復活させるコルーチン
    /// </summary>
    private IEnumerator RespawnRoutine()
    {
        SetWallActive(false);

        yield return new WaitForSeconds(respawnDelay);

        isBreaking = false;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = baseColor;
        }

        SetWallActive(true);
    }

    /// <summary>
    /// 壁の表示と当たり判定を一括で切り替える
    /// </summary>
    private void SetWallActive(bool active)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = active;
        }

        if (wallColliders != null)
        {
            foreach (var col in wallColliders)
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