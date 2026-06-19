using System.Collections;
using UnityEngine;

public class DestructibleWall : MonoBehaviour, IInteractable
{
    [Header("Wall Settings")]
    [SerializeField] private ElementType breakableBy; // どの属性で壊れるか
    [SerializeField] private bool needsBoth = false;   // 両方の属性が必要か

    [Header("Audio Settings (効果音)")]
    [SerializeField] private AudioClip breakSound;       // 壁が壊れた時の音
    [SerializeField] private AudioClip layerHitSound;    // 【協力用】片方当たった時の音
    [SerializeField] private AudioClip failSound;        // 属性が違って効かなかった時の音

    [Header("Effect Settings (演出)")]
    [SerializeField] private GameObject breakEffectPrefab; // 共通のパーティクルプレハブ

    private SpriteRenderer spriteRenderer;
    private bool hitByFire = false;
    private bool hitByIce = false;
    private bool isBreaking = false;

    // 待機アニメーションのループ制御用
    private Coroutine idleCoroutine;
    private Vector3 basePosition;
    private Color baseColor;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        basePosition = transform.position;
        if (spriteRenderer != null)
        {
            baseColor = spriteRenderer.color;
        }

        // 待機中の動き（どちらも異なる振動）
        if (needsBoth)
        {
            idleCoroutine = StartCoroutine(IdleIceShiver());
        }
        else if (breakableBy == ElementType.Fire)
        {
            idleCoroutine = StartCoroutine(IdleFireShiver()); // 炎：超高速振動
        }
        else
        {
            idleCoroutine = StartCoroutine(IdleIceShiver());  // 氷：小刻みな硬質振動
        }
    }

    // 🔥 待機中（炎）：激しい微振動
    private IEnumerator IdleFireShiver()
    {
        while (!isBreaking)
        {
            float shiverAmount = 0.025f;
            transform.position = basePosition + new Vector3(
                Random.Range(-shiverAmount, shiverAmount),
                Random.Range(-shiverAmount, shiverAmount),
                0f
            );
            yield return null;
        }
    }

    // ❄️ 待機中（氷）：小刻みな振動
    private IEnumerator IdleIceShiver()
    {
        while (!isBreaking)
        {
            float shiverAmount = 0.015f;
            transform.position = basePosition + new Vector3(
                Random.Range(-shiverAmount, shiverAmount),
                Random.Range(-shiverAmount, shiverAmount),
                0f
            );
            yield return new WaitForSeconds(0.05f);
        }
    }

    public void OnInteract(ElementType type)
    {
        if (isBreaking) return;

        if (needsBoth)
        {
            if (hitByFire && hitByIce) return;
            if (type == ElementType.Fire && hitByFire) return;
            if (type == ElementType.Ice && hitByIce) return;

            if (type == ElementType.Fire) hitByFire = true;
            if (type == ElementType.Ice) hitByIce = true;

            UpdateVisuals();

            if (hitByFire && hitByIce)
            {
                StartBreakSequence(ElementType.Fire);
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
                StartBreakSequence(type);
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
            baseColor = new Color(baseColor.r * 0.5f, baseColor.g * 0.5f, baseColor.b * 0.5f, 1.0f);
            spriteRenderer.color = baseColor;
        }
    }

    private void StartBreakSequence(ElementType targetType)
    {
        isBreaking = true;

        if (idleCoroutine != null)
        {
            StopCoroutine(idleCoroutine);
        }

        transform.position = basePosition;
        if (spriteRenderer != null) spriteRenderer.color = baseColor;

        if (breakSound != null)
        {
            AudioSource.PlayClipAtPoint(breakSound, transform.position);
        }

        if (targetType == ElementType.Fire)
        {
            StartCoroutine(AnimateFireShatter()); // 炎：木っ端微塵に砕け散る
        }
        else if (targetType == ElementType.Ice)
        {
            StartCoroutine(AnimateIceShatterGlassEffect()); // 氷：パリーン！とガラスのように割れる
        }
    }

    // 🔥 破壊時（炎）：木っ端微塵に砕け散る
    private IEnumerator AnimateFireShatter()
    {
        if (TryGetComponent<Collider2D>(out Collider2D col)) col.enabled = false;

        float elapsed = 0f;
        float shakeDuration = 0.25f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float shakeAmount = 0.08f;
            transform.position = basePosition + new Vector3(
                Random.Range(-shakeAmount, shakeAmount),
                Random.Range(-shakeAmount, shakeAmount),
                0f
            );
            yield return null;
        }

        // 炎：速度 5.0f、寿命 0.25秒（小さく激しく散る）
        SpawnPrefabEffect(baseColor, 40, 5.0f, 0.25f);
        Destroy(gameObject);
    }

    // ❄️ 破壊時（氷）：パリーン！と一瞬でヒビが入り、鋭く弾け飛ぶ演出
    private IEnumerator AnimateIceShatterGlassEffect()
    {
        if (TryGetComponent<Collider2D>(out Collider2D col)) col.enabled = false;
        Vector3 originalScale = transform.localScale;

        float elapsed = 0f;
        float glassStiffDuration = 0.1f;

        transform.localScale = new Vector3(originalScale.x * 1.1f, originalScale.y * 0.9f, originalScale.z);

        while (elapsed < glassStiffDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // ★【調整】氷：速度 7.0f（鋭さ維持）に対し、寿命を「0.15秒」と超短縮！
        // これにより、勢いよく弾け出た瞬間にパッと消えるため、広がりすぎず小気味良いサイズになります。
        SpawnPrefabEffect(baseColor, 45, 7.0f, 0.15f);

        Destroy(gameObject);
    }

    // エフェクト生成（速度と寿命をコントロール）
    private void SpawnPrefabEffect(Color particleColor, int count, float speed, float lifetime)
    {
        if (breakEffectPrefab == null) return;

        GameObject effect = Instantiate(breakEffectPrefab, basePosition, Quaternion.identity);
        effect.transform.localScale = transform.localScale;

        ParticleSystem ps = effect.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            var mainModule = ps.main;
            mainModule.startColor = particleColor;
            mainModule.startSpeed = speed;       // 吹き飛ぶ速さ
            mainModule.startLifetime = lifetime; // ★【追加】飛び散る距離を抑えるために寿命を制限

            var emission = ps.emission;
            ParticleSystem.Burst burst = emission.GetBurst(0);
            float wallSizeFactor = transform.localScale.x * transform.localScale.y;
            burst.count = new ParticleSystem.MinMaxCurve(count * wallSizeFactor);
            emission.SetBurst(0, burst);
        }

        Destroy(effect, 1.0f);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        AudioSource source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        source.spatialBlend = 0f;
        source.PlayOneShot(clip);
    }
}