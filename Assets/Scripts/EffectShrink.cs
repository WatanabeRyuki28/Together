using UnityEngine;

public class EffectShrink : MonoBehaviour
{
    private float duration = 2.0f; // エフェクトが消えるまでの時間（StarItem側のDestroy時間と合わせる）
    private Vector3 startScale;
    private float elapsed = 0f;

    void Start()
    {
        // 生成されたときの大きさを基準にする
        startScale = transform.localScale;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;

        // 時間経過とともに少しずつゼロに向かって縮小させる
        transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
    }
}