using UnityEngine;
using System;

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

    private const int UnassignedCharacterIndex = -1;
    private const float SpatialBlend2D = 0f;

    [Header("Wall Settings (壁の設定)")]
    [SerializeField] private WallType wallType = WallType.BurningWall;

    [Header("Audio Settings (効果音)")]
    [SerializeField] private AudioClip breakSound;
    [SerializeField] private AudioClip failSound;

    [Header("Effect Settings (演出)")]
    [SerializeField] private GameObject breakEffectPrefab;

    private AudioSource audioSource;
    private NetworkIdentity2D networkIdentity;

    public bool isBreaking = false;
    public BoxCollider2D box;

    private void Awake()
    {
        networkIdentity = GetComponent<NetworkIdentity2D>();
        box = GetComponent<BoxCollider2D>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = SpatialBlend2D;
    }

    void Update()
    {
        if (isBreaking)
        {
            float shrinkAmount = 0.5f * Time.deltaTime;
            float newX = Mathf.Max(0f, transform.localScale.x - shrinkAmount);
            float newY = Mathf.Max(0f, transform.localScale.y - shrinkAmount);
            transform.localScale = new Vector3(newX, newY, transform.localScale.z);
        }
    }

    /// <summary>
    /// 弾（球）などの攻撃が当たった時に呼ばれる
    /// </summary>
    public void OnInteract(ElementType type)
    {
        TryBreakWall(type, isDirectContact: false);
    }

    /// <summary>
    /// プレイヤーが直接体当たりした時に、PlayerController側から呼ぶメソッド
    /// </summary>
    public void OnPlayerDirectTouch(ElementType playerElement)
    {
        TryBreakWall(playerElement, isDirectContact: true);
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
            // 燃えている壁：水属性（Ice）の「球（弾）」のみ（直接接触無効）
            if (type == ElementType.Ice && !isDirectContact)
            {
                canBreak = true;
            }
        }
        else if (wallType == WallType.GrassWall)
        {
            // 草の壁：炎属性（Fire）なら直接接触でも球でもOK
            if (type == ElementType.Fire)
            {
                canBreak = true;
            }
        }

        if (canBreak)
        {
            StartBreakSequence();

            // 自分が壊した（あるいはローカルで検知した）とき、相手へネットワーク通知を送る
            SendObjectSyncEvent();
        }
        else if (!isDirectContact)
        {
            PlaySound(failSound);
        }
    }

    /// <summary>
    /// ネットワーク経由で他プレイヤーから破壊イベントを受け取った時
    /// </summary>
    public void OnBreakFromNetwork()
    {

        if (isBreaking) return;
        StartBreakSequence();
    }

    private void StartBreakSequence()
    {
        isBreaking = true;

        PlayerController[] attachedPlayers = GetComponentsInChildren<PlayerController>();
        foreach (PlayerController player in attachedPlayers)
        {
            player.transform.SetParent(null);
        }

        if (breakSound != null)
        {
            AudioSource.PlayClipAtPoint(breakSound, transform.position);
        }

        SpawnBreakEffect();

        if (box != null)
        {
            box.isTrigger = true;
        }

        Destroy(gameObject, 2.0f);
    }

    private async void SendObjectSyncEvent()
    {
        if (NetworkManager.Instance == null || networkIdentity == null) return;

        Debug.Log($"[送信] 壁の破壊イベントを送信します。ID: {networkIdentity.objectId}");

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
            id = networkIdentity.objectId,
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