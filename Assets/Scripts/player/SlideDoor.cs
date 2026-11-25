using Unity.VisualScripting;
using System.Collections;
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

    // エフェクト
    [SerializeField] DustCloudCheck dust1;
    [SerializeField] DustCloudCheck dust2;

    [SerializeField] private GameObject shutterDie;
    [SerializeField] private float shutterMoveDuration = 0.25f;
    [SerializeField] private float dustDelay = 0.5f;


    

    private Coroutine dustCoroutine;
    private Coroutine shutterMoveCoroutine;
    private static readonly Vector2 OpenShutterLocalPos = new Vector2(0.0f, 0.5533f);
    private static readonly Vector2 ClosedShutterLocalPos = new Vector2(0.0f, -1.14f);

    private bool isClosingSequence = false;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        doorCollider = GetComponent<BoxCollider2D>();
        audioSource = GetComponent<AudioSource>();
    

        // AudioSourceの初期設定
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2Dサウンドとしてハッキリ鳴らす

        DustCloudCheck[] dusts = GetComponentsInChildren<DustCloudCheck>();

        if (dusts.Length > 0) dust1 = dusts[0];
        if (dusts.Length > 1) dust2 = dusts[1];


        // ゲーム開始時の初期化（最初は閉じているので壁にする）
        UpdateColliderState(instant: true);

     
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
        UpdateColliderState(instant: false);

        // ★開閉音の再生
        if (isOpen)
        {
            PlaySound(openSound);
        }
        else
        {
            StartCoroutine(CloseDoorRoutine());

        }

    }

       

    // SetDoorState(false) の中で呼ぶ想定の例
    private IEnumerator CloseDoorRoutine()
    {
        isClosingSequence = true;

        // 1. 閉まる最中は押し出さないよう Trigger にする
        doorCollider.isTrigger = true;
        Vector2 checkSize = doorCollider.size * 1.2f;
        doorCollider.offset = Vector2.zero;

        // 2. シャッターを閉じる位置まで移動
        yield return StartCoroutine(MoveShutterDieCoroutine(ClosedShutterLocalPos, shutterMoveDuration));

        // 3. 閉まりきった瞬間に範囲内のキャラをチェックして即死させる
        Vector2 checkCenter = (Vector2)transform.position + doorCollider.offset;
        Collider2D[] hits = Physics2D.OverlapBoxAll(checkCenter, doorCollider.size, 0f);
        foreach (Collider2D hit in hits)
        {
            if (hit.TryGetComponent<PlayerController>(out PlayerController player))
            {
                // ここにキャラの死亡処理を書く（例: player.Die();）
                Debug.Log($"{player.name} がシャ ッターに挟まれて即死しました！");
                player.Die();
            }
        }

        // 4. 通常の壁コライダーに戻す
        doorCollider.isTrigger = false;
        isClosingSequence = false;
    }

    public void DustCloudCheck(bool isClose)
    {
        if (dust1 != null&&isClose)
        {
            if (dustCoroutine != null)
            {
                StopCoroutine(dustCoroutine);
            }

            // 新しくコルーチンをスタートして記憶する
            dustCoroutine = StartCoroutine(DelayDustCoroutine());

        }
    }

    private IEnumerator DelayDustCoroutine()
    {
        // 1秒間待つ
        yield return new WaitForSeconds(0.5f);

        dust1.DustCloud();
        dust2.DustCloud();


        // 終わったら変数を空にする
        dustCoroutine = null;
    }

    // 扉の開閉状態に応じてコライダーの性質を切り替える処理
    private void UpdateColliderState(bool instant)
    {
        if (doorCollider == null) return;

        Vector2 targetShutterLocalPos;

        if (isOpen)
        {
            // 扉が開いている
            doorCollider.isTrigger = true;
            Debug.Log($"{gameObject.name} が開いたため、通り抜け可能になりました。");

            // サイズを変更する
            doorCollider.size = new Vector2(2.16f, 0.8019378f);

            // 中心位置を変更する
            doorCollider.offset = new Vector2(0.0f, 0.8590311f);

            targetShutterLocalPos = OpenShutterLocalPos;
        }
        else
        {
            // 扉が閉じている
            doorCollider.isTrigger = false;
            Debug.Log($"{gameObject.name} が閉じたため、通行不可の壁になりました。");

            // サイズを変更する
            doorCollider.size = new Vector2(2.16f, 2.52f);

            // 中心位置を変更する
            doorCollider.offset = new Vector2(0.0f, 0.0f);

            targetShutterLocalPos = ClosedShutterLocalPos;
        }

        if (shutterDie == null) return;

        if (shutterMoveCoroutine != null)
        {
            StopCoroutine(shutterMoveCoroutine);
            shutterMoveCoroutine = null;
        }

        if (instant || shutterMoveDuration <= 0f)
        {
            shutterDie.transform.localPosition = targetShutterLocalPos;
        }
        else
        {
            shutterMoveCoroutine = StartCoroutine(MoveShutterDieCoroutine(targetShutterLocalPos, shutterMoveDuration));
        }
    }

    private IEnumerator MoveShutterDieCoroutine(Vector2 targetLocalPos, float duration)
    {
        Vector2 startLocalPos = shutterDie.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // 最初と最後がなめらかになるイージング（等速にしたい場合は t のままにする）
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            shutterDie.transform.localPosition = Vector2.Lerp(startLocalPos, targetLocalPos, smoothT);
            yield return null;
        }

        shutterDie.transform.localPosition = targetLocalPos;
        shutterMoveCoroutine = null;
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