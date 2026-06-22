using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FloorSwitch : MonoBehaviour
{
    [Header("連動させるワープ扉（ペア）")]
    [SerializeField] private WarpDoor targetDoorA;
    [SerializeField] private WarpDoor targetDoorB;

    [Header("スイッチの色設定")]
    [SerializeField] private Color pressedColor = Color.gray;   // 踏まれた時の色

    [Header("Audio Settings (効果音)")]
    [SerializeField] private AudioClip switchOnSound;  // スイッチが押された時の音

    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;

    //[追加]　isPressedを廃止して乗っているかの判定
    private int currentOverlapCount = 0;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        //[追加]　プレイヤーのほかに、箱にも対応
        if (other.TryGetComponent<PlayerController>(out PlayerController player) || other.CompareTag("Box"))
        {
            currentOverlapCount++;

            // 0個から1個になった（＝新しく押された）瞬間だけ起動処理を走らせる
            if (currentOverlapCount == 1)
            {
                SetSwitchState(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        //[追加]　離れたかのチェック
        if (other.TryGetComponent<PlayerController>(out PlayerController player) || other.CompareTag("Box"))
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

    // ★【状態切り替えの一括処理】
    private void SetSwitchState(bool isPressed)
    {
        // スイッチの色を変更（踏まれて沈んだ状態のまま固定）

        if (spriteRenderer != null)

        {

            spriteRenderer.color = pressedColor;

        }

        // ★スイッチが押された音を鳴らす

        if (switchOnSound != null && audioSource != null)

        {

            audioSource.PlayOneShot(switchOnSound);

        }

        // 3. 扉の状態を連動（trueなら開く、falseなら閉じる）
        if (targetDoorA != null) targetDoorA.SetDoorState(isPressed);
        if (targetDoorB != null) targetDoorB.SetDoorState(isPressed);

        Debug.Log($"{gameObject.name} の状態が変更されました: 押されている={isPressed}");
    }
}