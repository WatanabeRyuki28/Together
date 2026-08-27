using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FloorSwitch : MonoBehaviour
{
    // --- 定数定義（マジックナンバー排除） ---
    private const float SoundCoolTime = 0.2f;    // 音の連打を防ぐインターバル時間（秒）
    private const float EffectDestroyDelay = 1.0f; // エフェクト自動削除までの時間（秒）

    [Header("連動させるワープ扉（ペア）")]
    [SerializeField] private WarpDoor targetDoorA;
    [SerializeField] private WarpDoor targetDoorB;

    [Header("スイッチの色設定")]
    [SerializeField] private Color normalColor = Color.white;  // 元の色（離れた時用）
    [SerializeField] private Color pressedColor = Color.gray;   // 踏まれた時の色

    [Header("スイッチの画像設定")]
    [SerializeField] private Sprite normalSprite;  // 通常時（離れた時）の画像
    [SerializeField] private Sprite pressedSprite; // 押された時の画像

    [Header("Audio Settings (効果音)")]
    [SerializeField] private AudioClip switchOnSound; // スイッチが押された時の音

    [Header("プッシュエフェクト")]
    [SerializeField] private GameObject pressEffect;

    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;

    private int currentOverlapCount = 0;
    private float nextSoundTime = 0f; // 音の連続再生を制御するタイマー

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 判定対象（プレイヤーまたは箱）かつトリガー判定用のコライダーでない場合のみ処理
        if (IsValidTriggerObject(other))
        {
            currentOverlapCount++;

            // 0個から1個になった（＝新しく押された）瞬間だけ起動処理を走動させる
            if (currentOverlapCount == 1)
            {
                SetSwitchState(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsValidTriggerObject(other))
        {
            currentOverlapCount--;

            // カウンターがマイナスにならないように安全対策
            if (currentOverlapCount < 0) currentOverlapCount = 0;

            // 乗っているものが完全にゼロになった瞬間、スイッチをオフにする
            if (currentOverlapCount == 0)
            {
                SetSwitchState(false);
            }
        }
    }

    /// <summary>
    /// スイッチを起動できる正規のオブジェクトかどうかを厳格に判定する
    /// </summary>
    private bool IsValidTriggerObject(Collider2D other)
    {
        // 1. 攻撃判定などの Trigger コライダーは除外
        if (other.isTrigger) return false;

        // 2. プレイヤーコンポーネントを所持しているか判定
        if (other.TryGetComponent<PlayerController>(out _)) return true;

        // 3. タグが "Box" かどうか判定
        if (other.CompareTag("Box")) return true;

        return false;
    }

    private void SetSwitchState(bool isPressed)
    {
        // スイッチの色および画像の差し替え処理
        if (spriteRenderer != null)
        {
            spriteRenderer.color = isPressed ? pressedColor : normalColor;
            spriteRenderer.sprite = isPressed ? pressedSprite : normalSprite;
        }

        // スイッチが押された音を鳴らす（クールタイム挟み込みで連打音を防止）
        if (switchOnSound != null && audioSource != null && isPressed)
        {
            if (Time.time >= nextSoundTime)
            {
                audioSource.PlayOneShot(switchOnSound);
                nextSoundTime = Time.time + SoundCoolTime;
            }
        }

        // エフェクト生成処理
        if (pressEffect != null && isPressed)
        {
            GameObject effect = Instantiate(
                pressEffect,
                transform.position,
                Quaternion.identity
            );

            Destroy(effect, EffectDestroyDelay);
        }

        // 扉の状態を連動（trueなら開く、falseなら閉じる）
        if (targetDoorA != null) targetDoorA.SetDoorState(isPressed);
        if (targetDoorB != null) targetDoorB.SetDoorState(isPressed);

        Debug.Log($"{gameObject.name} の状態が変更されました: 押されている={isPressed} | 現在の重なり数={currentOverlapCount}");
    }
}