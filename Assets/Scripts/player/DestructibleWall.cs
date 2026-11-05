using UnityEngine;

/// <summary>
/// 属性ごとの破壊可能な壁（燃えている壁/草の壁、オンライン同期対応・復活なし）
/// </summary>
public class DestructibleWall : MonoBehaviour, IInteractable
{
    public enum WallType
    {
        BurningWall, // 燃えている壁（水の球でのみ破壊可能・直接接触は不可）
        GrassWall    // 草の壁（炎キャラの直接接触・炎の球で破壊可能）
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
    /// インタラクト時の判定（球/弾などの攻撃が当たった時に呼ばれる）
    /// </summary>
    public void OnInteract(ElementType type)
    {
        // 弾（球）が当たった場合はどちらの壁も属性チェックして破壊を試みる
        TryBreakWall(type, isDirectContact: false);
    }

    /// <summary>
    /// プレイヤー本体が物理衝突（通常のコライダー）で触れた時の処理
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckPlayerCollision(collision.gameObject);
    }

    /// <summary>
    /// プレイヤー本体がトリガーコライダーで触れた時の処理
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        CheckPlayerCollision(other.gameObject);
    }

    /// <summary>
    /// 接触したオブジェクトがプレイヤーか判定し、対応する属性なら壁を壊す
    /// </summary>
    private void CheckPlayerCollision(GameObject targetObj)
    {
        if (isBreaking) return;

        PlayerController player = targetObj.GetComponent<PlayerController>();
        if (player != null)
        {
            // プレイヤー本体の直接接触として処理
            TryBreakWall(player.Element, isDirectContact: true);
        }
    }

    /// <summary>
    /// 属性と接触方法に応じて壁を破壊するか判定し、実行する
    /// </summary>
    private void TryBreakWall(ElementType type, bool isDirectContact)
    {
        if (isBreaking) return;

        bool canBreak = false;

        if (wallType == WallType.BurningWall)
        {
            // 燃えている壁：水属性（Ice）の「球（弾）」が当たった時のみ破壊（直接接触は無効）
            if (type == ElementType.Ice && !isDirectContact)
            {
                canBreak = true;
            }
        }
        else if (wallType == WallType.GrassWall)
        {
            // 草の壁：炎属性（Fire）であれば「直接接触」でも「球（弾）」でも破壊可能
            if (type == ElementType.Fire)
            {
                canBreak = true;
            }
        }

        if (canBreak)
        {
            StartBreakSequence();

            // オンライン通信中であれば他プレイヤーへ通知を送信
            SendObjectSyncEvent();
        }
        else if (!isDirectContact)
        {
            // 弾（球）が当たったが属性が合わなかった場合は失敗音を鳴らす
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