using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    private const float RightDirection = 1f;
    private const float LeftDirection = -1f;
    private const float DefaultHitEffectLifeTime = 1f;
    private const float RotationAngleThresholdMin = 170f;
    private const float RotationAngleThresholdMax = 190f;

    private static readonly int OnPlayEventID = Shader.PropertyToID("OnPlay");

    [Header("Projectile Settings")]
    [SerializeField] private float speed = 10.0f;
    [SerializeField] private float lifeTime = 1.5f;
    [SerializeField] private ElementType projectileType;

    [Header("軌道設定（放物線にする場合）")]
    [Tooltip("チェックを入れると重力を受けて放物線を描きます（水の球などに便利）")]
    [SerializeField] private bool useParabola = false;     // 放物線軌道を使うか
    [SerializeField] private float gravityScale = 1.5f;    // 重力の強さ
    [SerializeField] private float upwardForce = 3.0f;     // 打ち出すときの上方向への初速

    [Header("Collision Settings")]
    [SerializeField] private LayerMask collisionLayers;

    [Header("HITエフェクト")]
    [SerializeField] private GameObject hitEffect;

    private Rigidbody2D rb;
    private float moveDirection = RightDirection;
    private bool isInitialized = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (useParabola)
        {
            rb.gravityScale = gravityScale; // 重力を有効にする
        }
        else
        {
            rb.gravityScale = 0f; // 直線的な場合は重力を切る
        }
    }

    private void Start()
    {
        if (!isInitialized)
        {
            float currentRotationZ = transform.eulerAngles.z;
            if (currentRotationZ > RotationAngleThresholdMin && currentRotationZ < RotationAngleThresholdMax)
            {
                moveDirection = LeftDirection;
            }
            else
            {
                moveDirection = RightDirection;
            }
            isInitialized = true;
            ApplyInitialVelocity();
        }

        Destroy(gameObject, lifeTime);
    }

    public void Initialize(float direction, ElementType playerElement)
    {
        moveDirection = direction;
        projectileType = playerElement;
        isInitialized = true;

        ApplyInitialVelocity();
    }

    /// <summary>
    /// 発射時の初速を計算して適用する
    /// </summary>
    private void ApplyInitialVelocity()
    {
        if (rb == null) return;

        if (useParabola)
        {
            // 放物線：横方向のスピードに加え、上方向への初速（upwardForce）を加算する
            rb.velocity = new Vector2(speed * moveDirection, upwardForce);
        }
        else
        {
            // 直線：重力なしでまっすぐ飛ぶ
            rb.velocity = new Vector2(speed * moveDirection, 0f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Vector3 hitPoint = other.ClosestPoint(transform.position);
        SpawnHitEffect(hitPoint);

        PlayerController hitPlayer = other.GetComponent<PlayerController>();
        if (hitPlayer != null)
        {
            if (projectileType == ElementType.Fire)
            {
                hitPlayer.ApplyStatus(StatusCondition.Burning);
            }
            else if (projectileType == ElementType.Ice)
            {
                hitPlayer.ApplyStatus(StatusCondition.Wet);
            }

            hitPlayer.TakeDamage(1);
            Destroy(gameObject);
            return;
        }

        IInteractable target = other.GetComponent<IInteractable>();
        if (target != null)
        {
            target.OnInteract(projectileType);
            Destroy(gameObject);
            return;
        }

        if (((1 << other.gameObject.layer) & collisionLayers) != 0)
        {
            Destroy(gameObject);
        }
    }

    private void SpawnHitEffect(Vector3 position)
    {
        if (hitEffect == null) return;

        GameObject effectInstance = Instantiate(
            hitEffect,
            position,
            Quaternion.identity
        );

        if (effectInstance.TryGetComponent<VisualEffect>(out var vfx))
        {
            vfx.SendEvent(OnPlayEventID);
        }

        Destroy(effectInstance, DefaultHitEffectLifeTime);
    }
}