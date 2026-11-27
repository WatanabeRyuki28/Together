using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FloorSwitch : MonoBehaviour
{
    // --- 定数定義（マジックナンバー排除） ---
    private const float SoundCoolTime = 0.2f;       // 音の連打を防ぐインターバル時間（秒）
    private const float EffectDestroyDelay = 1.0f;  // エフェクト自動削除までの時間（秒）

    [Header("連動させるワープ扉（複数可）")]
    [SerializeField] private List<WarpDoor> targetDoors = new List<WarpDoor>();

    [Header("スイッチの色設定")]
    [SerializeField] private Color normalColor = Color.white; // 元の色（離れた時用）
    [SerializeField] private Color pressedColor = Color.gray;  // 踏まれた時の色

    [Header("スイッチの画像設定")]
    [SerializeField] private Sprite normalSprite;  // 通常時（離れた時）の画像
    [SerializeField] private Sprite pressedSprite; // 押された時の画像

    [Header("Audio Settings (効果音)")]
    [SerializeField] private AudioClip switchOnSound; // スイッチが押された時の音

    [Header("プッシュエフェクト")]
    [SerializeField] private GameObject pressEffect;

    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private Animator anim;

    private int currentOverlapCount = 0;
    private float nextSoundTime = 0f; // 音の連続再生を制御するタイマー

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        anim = GetComponent<Animator>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsValidTriggerObject(other))
        {
            currentOverlapCount++;

            // 0個から1個になった（＝新しく押された）瞬間だけ起動処理を実行
            if (currentOverlapCount == 1)
            {
                SetSwitchState(true);
                NotifyDoorsDustCloud(false);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsValidTriggerObject(other))
        {
            currentOverlapCount--;

            if (currentOverlapCount < 0)
            {
                currentOverlapCount = 0;
            }

            // 乗っているものが完全にゼロになった瞬間、スイッチをオフにする
            if (currentOverlapCount == 0)
            {
                SetSwitchState(false);
                NotifyDoorsDustCloud(true);
            }
        }
    }

    /// <summary>
    /// スイッチを起動できる正規のオブジェクトかどうかを判定する
    /// </summary>
    private bool IsValidTriggerObject(Collider2D other)
    {
        if (other.isTrigger) return false;
        if (other.TryGetComponent<PlayerController>(out _)) return true;
        if (other.CompareTag("Box")) return true;

        return false;
    }

    private void SetSwitchState(bool isPressed)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = isPressed ? pressedColor : normalColor;
            spriteRenderer.sprite = isPressed ? pressedSprite : normalSprite;
        }

        if (anim != null)
        {
            anim.SetBool("Push", isPressed);
        }

        if (switchOnSound != null && audioSource != null && isPressed)
        {
            if (Time.time >= nextSoundTime)
            {
                audioSource.PlayOneShot(switchOnSound);
                nextSoundTime = Time.time + SoundCoolTime;
            }
        }

        if (pressEffect != null && isPressed)
        {
            GameObject effect = Instantiate(
                pressEffect,
                transform.position,
                Quaternion.identity
            );

            Destroy(effect, EffectDestroyDelay);
        }

        foreach (WarpDoor door in targetDoors)
        {
            if (door != null)
            {
                door.SetDoorState(isPressed);
            }
        }

        Debug.Log($"{gameObject.name} の状態が変更されました: 押されている={isPressed} | 現在の重なり数={currentOverlapCount}");
    }

    private void NotifyDoorsDustCloud(bool isClosing)
    {
        foreach (WarpDoor door in targetDoors)
        {
            if (door != null)
            {
                door.DustCloudCheck(isClosing);
            }
        }
    }
}