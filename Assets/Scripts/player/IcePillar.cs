using System.Collections;
using UnityEngine;

/// <summary>
/// 既存の氷柱にアタッチするスクリプト
/// </summary>
public class IcePillar : MonoBehaviour, IInteractable
{
    private const float GrowDuration = 0.25f; // 生えるスピード

    [Header("Pillar Life Settings")]
    [SerializeField] private float duration = 8.0f;    // 存在時間（秒）
    [SerializeField] private bool meltByFire = true;   // 炎で溶けるか
    [SerializeField] private bool animateGrow = true;   // 下から生える演出をするか

    [Header("Audio & Effect Settings")]
    [SerializeField] private AudioClip meltSound;        // 溶ける時の音
    [SerializeField] private GameObject meltEffectPrefab; // 溶ける時のエフェクト

    private Vector3 targetScale;
    private bool isMelting = false;

    private void Start()
    {
        targetScale = transform.localScale;

        if (animateGrow)
        {
            // 最初はY軸（高さ）を0にして出現開始
            transform.localScale = new Vector3(targetScale.x, 0f, targetScale.z);
            StartCoroutine(GrowRoutine());
        }

        StartCoroutine(TimerRoutine());
    }

    /// <summary>
    /// 下からにょきッと生える演出
    /// </summary>
    private IEnumerator GrowRoutine()
    {
        float elapsed = 0f;
        while (elapsed < GrowDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / GrowDuration;
            float currentY = Mathf.Lerp(0f, targetScale.y, t);
            transform.localScale = new Vector3(targetScale.x, currentY, targetScale.z);
            yield return null;
        }
        transform.localScale = targetScale;
    }

    private IEnumerator TimerRoutine()
    {
        yield return new WaitForSeconds(duration);
        Melt();
    }

    /// <summary>
    /// 炎の弾などが当たった時
    /// </summary>
    public void OnInteract(ElementType type)
    {
        if (isMelting) return;

        if (meltByFire && type == ElementType.Fire)
        {
            Melt();
        }
    }

    private void Melt()
    {
        if (isMelting) return;
        isMelting = true;

        if (meltSound != null)
        {
            AudioSource.PlayClipAtPoint(meltSound, transform.position);
        }

        if (meltEffectPrefab != null)
        {
            Instantiate(meltEffectPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}