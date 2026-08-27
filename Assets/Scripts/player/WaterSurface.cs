using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 水エリア・設置型仕掛け用スクリプト。
/// ピンポイント生成、広範囲生成、オブジェクトの回転対応、および同時存在上限数制御。
/// </summary>
public class WaterSurface : MonoBehaviour, IInteractable
{
    // --- 定数定義（マジックナンバーの排除） ---
    private const float DefaultSoundVolume = 1.0f;
    private const float DividerForCenterCalculation = 2.0f;
    private const int DefaultMaxPillarCount = 3;
    private const float DefaultSpawnRotationZ = 90.0f; // デフォルトの回転角度（壁からの押し出し用）

    public enum SpawnMode
    {
        PointHit,          // 弾が当たったピンポイント位置に生成
        WideArea,          // 指定範囲（横方向）に複数生成
        UseObjectRotation  // 設定された角度（壁や天井）に合わせて生成
    }

    [Header("Generation Mode")]
    [SerializeField] private SpawnMode spawnMode = SpawnMode.PointHit; // 生成モード

    [Header("Pillar Settings")]
    [SerializeField] private GameObject icePillarPrefab; // 作成済みの氷柱プレハブ
    [SerializeField] private Vector3 spawnOffset = Vector3.zero; // 位置調整用オフセット
    [SerializeField] private Vector3 spawnRotationEuler = new Vector3(0f, 0f, DefaultSpawnRotationZ); // 生成時の回転角度指定
    [SerializeField] private int maxPillarCount = DefaultMaxPillarCount; // 同時に存在できる氷柱の最大数

    [Header("Wide Area Settings (広範囲生成用)")]
    [SerializeField] private int pillarCount = 3;        // 広範囲時に出す本数
    [SerializeField] private float pillarSpacing = 1.5f; // 柱同士の間隔

    [Header("Audio Settings")]
    [SerializeField] private AudioClip freezeSound;
    [SerializeField] private float soundVolume = DefaultSoundVolume;

    // 生成した氷柱を古い順に管理するキュー
    private readonly Queue<GameObject> activePillars = new Queue<GameObject>();

    /// <summary>
    /// 通常の OnInteract（広範囲生成、設置型生成、またはエリア中心生成用）
    /// </summary>
    public void OnInteract(ElementType type)
    {
        if (type != ElementType.Ice || icePillarPrefab == null) return;

        switch (spawnMode)
        {
            case SpawnMode.WideArea:
                CreateWideAreaPillars(transform.position);
                break;
            case SpawnMode.UseObjectRotation:
                CreateRotatedPillar(transform.position);
                break;
            case SpawnMode.PointHit:
            default:
                CreateSinglePillar(transform.position + spawnOffset);
                break;
        }
    }

    /// <summary>
    /// ピンポイント生成用（弾の着弾座標を受け取る）
    /// </summary>
    public void OnInteractAtPoint(ElementType type, Vector3 hitPoint)
    {
        if (type != ElementType.Ice || icePillarPrefab == null) return;

        switch (spawnMode)
        {
            case SpawnMode.PointHit:
                CreateSinglePillar(hitPoint + spawnOffset);
                break;
            case SpawnMode.UseObjectRotation:
                // 着弾点（hitPoint）を直接渡す
                CreateRotatedPillar(hitPoint);
                break;
            case SpawnMode.WideArea:
            default:
                CreateWideAreaPillars(hitPoint);
                break;
        }
    }

    /// <summary>
    /// 1本だけ生成（UseObjectRotationが有効な場合は設定された角度を維持）
    /// </summary>
    private void CreateSinglePillar(Vector3 position)
    {
        Quaternion rotation = (spawnMode == SpawnMode.UseObjectRotation)
            ? Quaternion.Euler(spawnRotationEuler)
            : Quaternion.identity;

        SpawnAndManagePillar(position, rotation);
        PlaySound(position);
    }

    /// <summary>
    /// 設定されたオイラー角に合わせて回転させて生成
    /// </summary>
    private void CreateRotatedPillar(Vector3 basePosition)
    {
        Quaternion rotation = Quaternion.Euler(spawnRotationEuler);

        // 指定した角度の向きに合わせてオフセットを加算
        Vector3 spawnPosition = basePosition + (rotation * spawnOffset);

        SpawnAndManagePillar(spawnPosition, rotation);
        PlaySound(spawnPosition);
    }

    /// <summary>
    /// 横方向に複数本まとめて生成（広範囲用）
    /// </summary>
    private void CreateWideAreaPillars(Vector3 centerPosition)
    {
        float startX = -((pillarCount - 1) * pillarSpacing) / DividerForCenterCalculation;

        for (int i = 0; i < pillarCount; i++)
        {
            float offsetX = startX + (i * pillarSpacing);
            Vector3 spawnPos = centerPosition + spawnOffset + new Vector3(offsetX, 0f, 0f);

            SpawnAndManagePillar(spawnPos, Quaternion.identity);
        }

        PlaySound(centerPosition);
    }

    /// <summary>
    /// 氷柱を実体化し、上限数（3個）を超えた場合は古いものから破棄する共通メソッド
    /// </summary>
    private void SpawnAndManagePillar(Vector3 position, Quaternion rotation)
    {
        // 新しい氷柱を生成してキューに追加
        GameObject newPillar = Instantiate(icePillarPrefab, position, rotation);
        activePillars.Enqueue(newPillar);

        // 上限数を超えていたら一番古い氷柱（Queueの先頭）を破棄する
        while (activePillars.Count > maxPillarCount)
        {
            GameObject oldestPillar = activePillars.Dequeue();

            if (oldestPillar != null)
            {
                Destroy(oldestPillar);
            }
        }
    }

    private void PlaySound(Vector3 position)
    {
        if (freezeSound != null)
        {
            AudioSource.PlayClipAtPoint(freezeSound, position, soundVolume);
        }
    }
}