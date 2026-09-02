using System.Collections;
using UnityEngine;

/// <summary>
/// 炎属性専用の破壊可能な壁（オンライン同期対応）
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
    private NetworkIdentity2D networkIdentity; // ★ 追加: ネットワーク同期用コンポーネント

    private bool isBreaking = false;
    private Color baseColor;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        wallColliders = GetComponents<Collider2D>();
        networkIdentity = GetComponent<NetworkIdentity2D>(); // ★ 追加: NetworkIdentity2D の取得

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

            // ★ 追加: オンライン通信中であれば他プレイヤーへ通知を送信
            SendObjectSyncEvent();
        }
        else
        {
            // 氷など他の属性の場合は弾かれる（失敗音）
            PlaySound(failSound);
        }
    }

    /// <summary>
    /// ★ 追加: 外部（他プレイヤー）からネットワーク経由で破壊イベントを受け取った際に呼ばれる
    /// </summary>
    public void OnBreakFromNetwork()
    {
        if (isBreaking) return;
        StartBreakSequence();
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
    /// オブジェクトの破壊・同期データをサーバー/対戦相手へ送信する
    /// </summary>
    private async void SendObjectSyncEvent()
    {
        if (NetworkManager.Instance == null || networkIdentity == null) return;

        int myRealColor = NetworkManager.Instance.myRealSelectedChar;
        if (myRealColor == -1) myRealColor = NetworkManager.Instance.myCharaIndex;

        InGameMoveData moveData = new InGameMoveData
        {
            type = "in_game_move",
            dataType = "object",
            room_id = NetworkManager.Instance.myRoomID,
            char_index = myRealColor,
            id = networkIdentity.objectId, // オブジェクト固有のID
            position_x = transform.position.x,
            position_y = transform.position.y
        };

        string json = JsonUtility.ToJson(moveData);

        // ★ SendWebSocketMessage を削除し、既存の SendMessageAsync を直接呼び出し
        await NetworkManager.Instance.SendMessageAsync(json);
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