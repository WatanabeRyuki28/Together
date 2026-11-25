using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class JumpPad : MonoBehaviour
{
    private const float DefaultJumpForce = 16.0f;
    private const float DefaultLaunchCooldown = 0.12f;
    private const float DefaultAboveTopMargin = -0.25f;
    private const float DefaultMaxFallVelocityY = 0.5f;
    private const float BottomComparisonEpsilon = 0.002f;
    private const float ApproachDropMargin = 0.05f;
    private const float MinDescendingVelocityY = -1.0f;
    private const float GroundPassThroughMargin = 0.25f;
    private const float ResetCooldownTime = -999f;

    [Header("ジャンプの設定")]
    [Tooltip("通常ジャンプの倍の高さに合わせるため、必要に応じて数値を調整してください")]
    [SerializeField] private float jumpForce = DefaultJumpForce;

    [Tooltip("連続発動を防ぐクールタイム（秒）")]
    [SerializeField] private float launchCooldown = DefaultLaunchCooldown;

    [Header("すり抜け・判定条件")]
    [Tooltip("横や下から飛んできた場合も高さ制限なしで発動させたい場合はオン")]
    [SerializeField] private bool allowInMidAir = false;

    [Tooltip("トリガーコライダー上端（bounds.max.y）に加える高さマージン。地面歩行時の誤爆防止用")]
    [SerializeField] private float aboveTopMargin = DefaultAboveTopMargin;

    [Tooltip("このY速度以下なら上から乗った/落下中とみなす（地面歩行すり抜け用）")]
    [SerializeField] private float maxFallVelocityY = DefaultMaxFallVelocityY;

    [Header("デバッグログ")]
    [SerializeField] private bool enableDebugLog = true;

    [Header("Audio Settings (効果音)")]
    [SerializeField] private AudioClip launchSound;

    private Animator padAnimator;
    private AudioSource padAudioSource;
    private Collider2D triggerCollider;
    private float lastLaunchTime = ResetCooldownTime;
    private string lastHeightFailSignature = string.Empty;

    private readonly Dictionary<int, float> previousBottomByCollider = new Dictionary<int, float>();

    private static readonly int LaunchTrigger = Animator.StringToHash("Launch");

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();

        // 親（Jump）側に Animator / AudioSource があれば優先し、なければ自分（TopTrigger）から取得
        padAnimator = transform.parent != null ? transform.parent.GetComponent<Animator>() : null;
        if (padAnimator == null)
        {
            padAnimator = GetComponent<Animator>();
        }

        padAudioSource = transform.parent != null ? transform.parent.GetComponent<AudioSource>() : null;
        if (padAudioSource == null)
        {
            padAudioSource = GetComponent<AudioSource>();
        }

        if (padAudioSource != null)
        {
            padAudioSource.playOnAwake = false;
            padAudioSource.spatialBlend = 0f;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        lastHeightFailSignature = string.Empty;
        LogInfo($"OnTriggerEnter2D: other={other.name}");
        TryLaunch(other);
        RecordPreviousBottom(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryLaunch(other);
        RecordPreviousBottom(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        lastHeightFailSignature = string.Empty;
        if (other != null)
        {
            previousBottomByCollider.Remove(other.GetInstanceID());
        }
    }

    private void RecordPreviousBottom(Collider2D col)
    {
        if (col != null)
        {
            previousBottomByCollider[col.GetInstanceID()] = col.bounds.min.y;
        }
    }

    private void TryLaunch(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        if (Time.time - lastLaunchTime < launchCooldown)
        {
            return;
        }

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            LogInfo($"PlayerControllerが見つかりません: other={other.name}");
            return;
        }

        Rigidbody2D rb = ResolveRigidbody(player.gameObject);
        if (rb == null)
        {
            LogInfo($"Rigidbody2Dが見つかりません: player={player.name}");
            return;
        }

        if (!allowInMidAir && !CanLaunchFromAbove(other, rb))
        {
            return;
        }

        lastHeightFailSignature = string.Empty;
        lastLaunchTime = Time.time;
        LaunchPlayer(player, rb);
    }

    private bool CanLaunchFromAbove(Collider2D playerCollider, Rigidbody2D playerRb)
    {
        float referenceTopY = triggerCollider != null ? triggerCollider.bounds.max.y : transform.position.y;
        float referenceBottomY = triggerCollider != null ? triggerCollider.bounds.min.y : transform.position.y;
        float requiredBottomY = referenceTopY + aboveTopMargin;
        float bottomY = playerCollider.bounds.min.y;
        float topY = playerCollider.bounds.max.y;
        float centerY = playerCollider.bounds.center.y;

        // プレイヤーの上端がトリガー下端より低い場合は、完全に下側を通過中なので弾く
        if (topY < referenceBottomY || centerY < referenceBottomY)
        {
            return false;
        }

        // 地面スレスレ（トリガー下端付近よりさらに下）を横切っているときは弾く
        if (bottomY < referenceBottomY - GroundPassThroughMargin)
        {
            return false;
        }

        if (playerRb.velocity.y > maxFallVelocityY)
        {
            LogInfo($"Y速度で不発: velY={playerRb.velocity.y:F3}, max={maxFallVelocityY:F3}");
            return false;
        }

        // しっかり落下している流れ＆直前の足元位置より下がっているときだけ踏み込みを優先
        bool hasPrevBottom = previousBottomByCollider.TryGetValue(playerCollider.GetInstanceID(), out float prevBottomY);
        bool descendingIntoPad = hasPrevBottom &&
                                 (prevBottomY + ApproachDropMargin > bottomY) &&
                                 (playerRb.velocity.y < MinDescendingVelocityY) &&
                                 (centerY >= referenceBottomY);

        if (descendingIntoPad)
        {
            return true;
        }

        if (bottomY + BottomComparisonEpsilon < requiredBottomY)
        {
            string signature = $"{playerCollider.name}:{bottomY:F3}:{requiredBottomY:F3}";
            if (signature != lastHeightFailSignature)
            {
                lastHeightFailSignature = signature;
                LogInfo(
                    $"高さ不足で不発: col={playerCollider.name}, bottomY={bottomY:F3}, " +
                    $"required={requiredBottomY:F3} (top={referenceTopY:F3}, velY={playerRb.velocity.y:F3})"
                );
            }
            return false;
        }

        return true;
    }

    private Rigidbody2D ResolveRigidbody(GameObject playerObj)
    {
        if (playerObj.TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
        {
            return rb;
        }
        return playerObj.GetComponentInParent<Rigidbody2D>();
    }

    private Animator ResolvePlayerAnimator(GameObject playerObj)
    {
        if (playerObj.TryGetComponent<Animator>(out Animator anim))
        {
            return anim;
        }
        return playerObj.GetComponentInChildren<Animator>();
    }

    private void LaunchPlayer(PlayerController player, Rigidbody2D rb)
    {
        GameObject playerObj = player.gameObject;

        player.isGrounded = false;
        rb.velocity = new Vector2(rb.velocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        Animator playerAnim = ResolvePlayerAnimator(playerObj);
        if (playerAnim != null)
        {
            if (player.playerNumber == 1)
            {
                playerAnim.Play("1P_jump", 0, 0f);
                LogInfo("1Pのジャンプアニメーションを再生しました。");
            }
            else if (player.playerNumber == 2)
            {
                playerAnim.Play("2P_jump", 0, 0f);
                LogInfo("2Pのジャンプアニメーションを再生しました。");
            }
            else
            {
                Debug.LogWarning($"{playerObj.name} の playerNumber が 1 または 2 ではありません（現在の値: {player.playerNumber}）");
            }
        }

        if (padAnimator != null && padAnimator.runtimeAnimatorController != null)
        {
            padAnimator.SetTrigger(LaunchTrigger);
        }

        if (launchSound != null && padAudioSource != null)
        {
            padAudioSource.PlayOneShot(launchSound);
        }
    }

    private void LogInfo(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[JumpPad] {message}");
        }
    }
}