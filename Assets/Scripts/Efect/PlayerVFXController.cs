using UnityEngine;
using UnityEngine.VFX;

public class PlayerVFXController : MonoBehaviour
{
    [Header("VFX Settings")]
    [SerializeField] private GameObject landEffectPrefab; // 着地用VFXプレハブ
    [SerializeField] private Transform spawnPoint;       // 足元のSpawnPoint
    [SerializeField] private float destroyDelay = 1.0f;    // 自動削除までの時間

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb2d;

    // 着地判定用の変数
    private bool wasInAir = false;
    private static readonly int OnPlayEventID = Shader.PropertyToID("OnPlay");

    void Start()
    {
        if (rb2d == null)
        {
            rb2d = GetComponent<Rigidbody2D>();
        }
    }

    void Update()
    {
        if (rb2d == null) return;

        // Y軸の移動速度を取得
        float yVelocity = rb2d.velocity.y;

        // 下方向に落下中（空中）のフラグを立てる
        if (yVelocity < -0.1f)
        {
            wasInAir = true;
        }
        // 空中にいて、かつY速度がほぼ0になった瞬間（＝地面に着地した瞬間）
        else if (wasInAir && Mathf.Abs(yVelocity) <= 0.01f)
        {
            PlayLandEffect();
            wasInAir = false; // フラグをリセット
        }
    }

    /// <summary>
    /// 着地エフェクトを生成して再生する
    /// </summary>
    private void PlayLandEffect()
    {
        if (landEffectPrefab == null)
        {
            Debug.LogWarning("Land VFX Prefabが設定されていません。");
            return;
        }

        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

        GameObject vfxInstance = Instantiate(landEffectPrefab, spawnPosition, spawnRotation);

        if (vfxInstance.TryGetComponent<VisualEffect>(out var vfx))
        {
            vfx.SendEvent(OnPlayEventID);
        }

        Destroy(vfxInstance, destroyDelay);
    }
}