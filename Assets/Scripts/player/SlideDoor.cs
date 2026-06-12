using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(BoxCollider2D))] // 壁判定（および将来のワープ検知）に必須
[RequireComponent(typeof(AudioSource))]   // ★効果音再生に必須
public class WarpDoor : MonoBehaviour
{
    // --- 【ワープ機能用】将来使う場合はコメントアウトを外してください ---
    // [Header("繋がっている相方の扉")]
    // [SerializeField] private WarpDoor companionDoor;

    // [Header("ワープ後の出現オフセット")]
    // [SerializeField] private Vector2 exitOffset = new Vector2(0f, 0f);

    // [Header("再ワープまでの待機時間")]
    // [SerializeField] private float warpCooldown = 0.5f; 

    // private static float nextWarpAvailableTime = 0f;
    // -----------------------------------------------------------------

    [Header("Audio Settings (効果音)")]
    [SerializeField] private AudioClip openSound;   // 扉が開いた時の音
    //[SerializeField] private AudioClip closeSound;  // 扉が閉じた時の音
    //[SerializeField] private AudioClip warpSound;   // ワープした時の音

    private Animator animator;
    private BoxCollider2D doorCollider; // 扉のコライダー
    private AudioSource audioSource;    // 効果音再生用
    private bool isOpen = false;

    // アニメーターのパラメーター名（IsOpen）のハッシュ値
    private static readonly int IsOpenParam = Animator.StringToHash("IsOpen");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        doorCollider = GetComponent<BoxCollider2D>();
        audioSource = GetComponent<AudioSource>();

        // AudioSourceの初期設定
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2Dサウンドとしてハッキリ鳴らす

        // ゲーム開始時の初期化（最初は閉じているので壁にする）
        UpdateColliderState();
    }

    // スッチから呼ばれる、扉の開閉状態を設定するメソッド
    public void SetDoorState(bool open)
    {
        // 状態に変化がない場合は音を鳴らさない（毎フレーム呼ばれた際のエラー・音の重複防止）
        if (isOpen == open) return;

        isOpen = open;

        // アニメーターの「IsOpen」パラメーターを更新してアニメーションを切り替える
        animator.SetBool(IsOpenParam, open);

        // 扉の状態に合わせて、当たり判定の性質（壁か、通り抜け可能か）を切り替える
        UpdateColliderState();

        // ★開閉音の再生
        if (isOpen)
        {
            PlaySound(openSound);
        }
        else
        {
            //PlaySound(closeSound);
        }
    }

    // 扉の開閉状態に応じてコライダーの性質を切り替える処理
    private void UpdateColliderState()
    {
        if (doorCollider == null) return;

        if (isOpen)
        {
            // 扉が開いている：通り抜け可能にする（Is Trigger を ON にする）
            doorCollider.isTrigger = true;
            Debug.Log($"{gameObject.name} が開いたため、通り抜け可能になりました。");
        }
        else
        {
            // 扉が閉じている：物理的な壁にする（Is Trigger を OFF にする）
            doorCollider.isTrigger = false;
            Debug.Log($"{gameObject.name} が閉じたため、通行不可の壁になりました。");
        }
    }

    // 効果音を鳴らす共通メソッド
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // --- 【ワープ判定・処理】将来使う場合はここのコメントアウトを外してください ---
    // private void OnTriggerEnter2D(Collider2D other)
    // {
    //     // 扉が開いている 且つ 触れたのがプレイヤー 且つ クールタイムが終了している時だけワープ
    //     if (isOpen && Time.time >= nextWarpAvailableTime && other.TryGetComponent<PlayerController>(out PlayerController player))
    //     {
    //         WarpPlayer(player.gameObject);
    //     }
    // }

    // private void WarpPlayer(GameObject playerObj)
    // {
    //     if (companionDoor == null) return;

    //     // 次にワープができる時刻を「現在時刻 + クールタイム」に設定
    //     nextWarpAvailableTime = Time.time + warpCooldown;

    //     // ワープ先の座標を計算
    //     Vector3 warpTargetPosition = companionDoor.transform.position + (Vector3)companionDoor.exitOffset;
    //     warpTargetPosition.z = playerObj.transform.position.z;

    //     // 物理速度をリセットして座標を書き換え
    //     if (playerObj.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
    //     {
    //         rb.velocity = Vector2.zero;
    //     }

    //     playerObj.transform.position = warpTargetPosition;

    //     // ★ワープ音を再生
    //     PlaySound(warpSound);

    //     Debug.Log($"{playerObj.name} がワープしました。");
    // }
    // ---------------------------------------------------------------------------
}