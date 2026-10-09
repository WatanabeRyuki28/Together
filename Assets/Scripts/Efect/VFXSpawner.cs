using UnityEngine;
using UnityEngine.VFX;

public class VFXSpawner : MonoBehaviour
{
    [Header("VFX Prefabs")]
    [SerializeField] private GameObject explosionVfxPrefab;

    // パフォーマンス向上のためイベント名をPropertyIDとしてキャッシュ
    private static readonly int OnPlayEventID = Shader.PropertyToID("OnPlay");

    /// <summary>
    /// 指定位置にエフェクトを生成して再生
    /// </summary>
    /// <param name="spawnPosition">生成するワールド座標</param>
    /// <param name="destroyDelay">自動破棄されるまでの時間（秒）</param>
    public void SpawnExplosion(Vector3 spawnPosition, float destroyDelay = 2.0f)
    {
        if (explosionVfxPrefab == null)
        {
            Debug.LogWarning("Explosion VFX Prefabがアタッチされていません。");
            return;
        }

        // 1. 指定位置にインスタンス化
        GameObject vfxInstance = Instantiate(explosionVfxPrefab, spawnPosition, Quaternion.identity);

        // 2. VisualEffectコンポーネントを取得して再生
        if (vfxInstance.TryGetComponent<VisualEffect>(out var vfx))
        {
            vfx.SendEvent(OnPlayEventID);
        }

        // 3. 寿命に合わせて自動破棄（メモリリーク防止）
        Destroy(vfxInstance, destroyDelay);
    }
}