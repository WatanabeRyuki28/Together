using UnityEngine;

public class FloorSwitch : MonoBehaviour
{
    [Header("連動させるワープ扉（ペア）")]
    [SerializeField] private WarpDoor targetDoorA;
    [SerializeField] private WarpDoor targetDoorB;

    [Header("踏まれた時のスイッチの色")]
    [SerializeField] private Color pressedColor = Color.gray;

    private SpriteRenderer spriteRenderer;
    private bool isPressed = false; // 一度押されたら true になり、固定される

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
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

        // 両方の扉を「開く（true）」にする（以降、閉じる命令は送られない）
        if (targetDoorA != null) targetDoorA.SetDoorState(true);
        if (targetDoorB != null) targetDoorB.SetDoorState(true);

        Debug.Log($"{gameObject.name} が起動し、扉が開放状態で固定されました。");
    }
}