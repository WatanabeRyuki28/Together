using UnityEngine;

/// <summary>
/// 属性ごとの破壊可能な壁（燃えている壁/草の壁、オンライン同期対応・復活なし）
/// </summary>
public class DestructibleWall : MonoBehaviour, IInteractable
{
    public enum WallType
    {
        BurningWall, // 10: 燃えている壁（水キャラ・水属性でのみ破壊可能）[cite: 3]
        GrassWall    // 11: 草の壁（炎キャラ・炎属性でのみ破壊可能）[cite: 3]
    }

    // --- マジックナンバー排除用の定数定義 ---
    private const int UnassignedCharacterIndex = -1; // 未割り当てキャラインデックスのデフォルト値
    private const float SpatialBlend2D = 0f;          // 2Dサウンド用空間ブレンド値

    [Header("Wall Settings (壁の設定)")]
    [SerializeField] private WallType wallType = WallType.BurningWall; // インスペクターで切り替え

    [Header("Audio Settings (効果音)")]
    [SerializeField] private AudioClip breakSound; // 破壊時の効果音
    [SerializeField] private AudioClip failSound;  // 属性が合わずに弾いた時の効果音

    [Header("Effect Settings (演出)")]
    [SerializeField] private GameObject breakEffectPrefab; // 破壊時のエフェクトプレハブ

    private AudioSource audioSource;
    private NetworkIdentity2D networkIdentity;

    private bool isBreaking = false;

    private void Awake()
    {
        networkIdentity = GetComponent<NetworkIdentity2D>();

        // AudioSourceのキャッシュ
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = SpatialBlend2D;
    }

    /// <summary>
    /// インタラクト時の判定（弾や攻撃が当たった時に呼ばれる）
    /// </summary>
    public void OnInteract(ElementType type)
    {
        if (isBreaking) return;

        bool canBreak = false;

        // 壁の種類に応じて破壊できる属性を判定
        if (wallType == WallType.BurningWall && type == ElementType.Ice)
        {
            canBreak = true; // 燃えている壁は水で壊せる[cite: 3]
        }
        else if (wallType == WallType.GrassWall && type == ElementType.Fire)
        {
            canBreak = true; // 草の壁は炎で壊せる[cite: 3]
        }

        if (canBreak)
        {
            StartBreakSequence();

            // オンライン通信中であれば他プレイヤーへ通知を送信
            SendObjectSyncEvent();
        }
        else
        {
            // 属性が合わない場合は弾かれる（失敗音）
            PlaySound(failSound);
        }
    }

    /// <summary>
    /// 外部（他プレイヤー）からネットワーク経由で破壊イベントを受け取った際に呼ばれる
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

        // 復活処理を行わず、即座にオブジェクトを完全に削除
        Destroy(gameObject);
    }

    /// <summary>
    /// オブジェクトの破壊・同期データをサーバー/対戦相手へ送信する
    /// </summary>
    private async void SendObjectSyncEvent()
    {
        if (NetworkManager.Instance == null || networkIdentity == null) return;

        int myRealColor = NetworkManager.Instance.myRealSelectedChar;
        if (myRealColor == UnassignedCharacterIndex)
        {
            myRealColor = NetworkManager.Instance.myCharaIndex;
        }

        NetMessage moveData = new NetMessage
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

        await NetworkManager.Instance.SendMessageAsync(json);
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