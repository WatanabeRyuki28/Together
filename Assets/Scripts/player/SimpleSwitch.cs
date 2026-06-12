using UnityEngine;

[RequireComponent(typeof(AudioSource))] // ★効果音再生に必須
public class FloorSwitch : MonoBehaviour
{
    [Header("連動させるワープ扉（ペア）")]
    [SerializeField] private WarpDoor targetDoorA;
    [SerializeField] private WarpDoor targetDoorB;

    [Header("踏まれた時のスイッチの色")]
    [SerializeField] private Color pressedColor = Color.gray;

    [Header("Audio Settings (効果音)")]
    [SerializeField] private AudioClip switchOnSound; // ★スイッチが押された時の音

    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource; // ★効果音再生用
    private bool isPressed = false; // 一度押されたら true になり、固定される

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        // AudioSourceの初期設定
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2Dサウンドとしてハッキリ鳴らす
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 既に起動しているなら、これ以降の判定はすべて無視
        if (isPressed) return;

        // プレイヤー（1P・2Pどちらでも）が乗ったかチェック
        if (other.TryGetComponent<PlayerController>(out PlayerController player))
        {
            ActivateSwitchPermanent();
        }
    }

    // 永久起動の処理
    private void ActivateSwitchPermanent()
    {
        isPressed = true;

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

        // 両方の扉を「開く（true）」にする（以降、閉じる命令は送られない）
        if (targetDoorA != null) targetDoorA.SetDoorState(true);
        if (targetDoorB != null) targetDoorB.SetDoorState(true);

        Debug.Log($"{gameObject.name} が起動し、扉が開放状態で固定されました。");
    }
}