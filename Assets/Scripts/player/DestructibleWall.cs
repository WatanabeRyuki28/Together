using UnityEngine;

public class DestructibleWall : MonoBehaviour, IInteractable
{
    [Header("Wall Settings")]
    [SerializeField] private ElementType breakableBy; // どの属性で壊れるか
    [SerializeField] private bool needsBoth = false;   // 両方の属性が必要か

    [Header("Audio Settings (効果音)")]
    [SerializeField] private AudioClip breakSound;       // 壁が壊れた時の音
    [SerializeField] private AudioClip layerHitSound;    // 【協力用】片方当たった時の音（ピキーンなど）
    [SerializeField] private AudioClip failSound;        // 属性が違って効かなかった時の音（カンッなど）

    [Header("Effect Settings (演出)")]
    [SerializeField] private GameObject breakEffectPrefab; // ★作成したパーティクルのプレハブ

    private bool hitByFire = false;
    private bool hitByIce = false;

    // ========================================================
    // ★【エラー解消】省略せずにすべて記述した OnInteract メソッド
    // ========================================================
    public void OnInteract(ElementType type)
    {
        if (needsBoth)
        {
            // すでに両方当たっている、または同じ属性に連続で当たった場合は無視
            if (hitByFire && hitByIce) return;
            if (type == ElementType.Fire && hitByFire) return;
            if (type == ElementType.Ice && hitByIce) return;

            if (type == ElementType.Fire) hitByFire = true;
            if (type == ElementType.Ice) hitByIce = true;

            // 見た目で「あと一歩」感を出す（片方当たったら半透明にする等）
            UpdateVisuals();

            if (hitByFire && hitByIce)
            {
                BreakWall();
            }
            else
            {
                // まだ片方だけしか当たっていないなら、受付音を鳴らす
                PlaySound(layerHitSound);
            }
        }
        else
        {
            // 特定の属性だけで壊れる場合
            if (type == breakableBy)
            {
                BreakWall();
            }
            else
            {
                // 属性が間違っているなら、弾かれ音を鳴らす
                PlaySound(failSound);
            }
        }
    }

    private void UpdateVisuals()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // 片方当たったら少し暗くするなど
            sr.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
        }
    }

    private void BreakWall()
    {
        Debug.Log($"{gameObject.name} を破壊しました！");

        if (breakSound != null)
        {
            AudioSource.PlayClipAtPoint(breakSound, transform.position);
        }

        // 壁の位置に破片パーティクルを生成する
        if (breakEffectPrefab != null)
        {
            GameObject effect = Instantiate(breakEffectPrefab, transform.position, Quaternion.identity);

            // ========================================================
            // ★【追加】壁の色をパーティクルにコピーする処理
            // ========================================================
            SpriteRenderer wallSr = GetComponent<SpriteRenderer>();
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();

            if (wallSr != null && ps != null)
            {
                // パーティクルの「メイン設定（色や寿命など）」にアクセス
                var mainModule = ps.main;

                // 壁のSpriteRendererの色を、破片の初期色（Start Color）にそっくりそのまま代入
                mainModule.startColor = wallSr.color;
            }
            // ========================================================

            // 1秒後に自動でヒエラルキーから削除する
            Destroy(effect, 1.0f);
        }

        // 最後に壁本体を消去
        Destroy(gameObject);
    }

    // 壁がまだ存在している間に鳴らす通常SE用メソッド
    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;

        AudioSource source = GetComponent<AudioSource>();
        if (source == null)
        {
            source = gameObject.AddComponent<AudioSource>();
        }

        source.spatialBlend = 0f; // 2Dサウンドとして綺麗に鳴らす
        source.PlayOneShot(clip);
    }
}