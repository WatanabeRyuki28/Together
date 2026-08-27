using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(AudioSource))]
public class PushableBox : MonoBehaviour
{
    // --- 定数定義（マジックナンバー排除） ---
    // 微振動で音が鳴るのを防ぐため、閾値を 0.05f から 0.2f に引き上げ
    private const float MinMoveSpeedThreshold = 0.2f;

    [Header("効果音（SE/BGM）設定")]
    [SerializeField] private AudioClip pushSound;

    private Rigidbody2D rb;
    private AudioSource audioSource;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = true; // 押している間ループ再生
    }

    private void Update()
    {
        HandlePushSound();
    }

    /// <summary>
    /// 箱の移動速度に応じて移動音の再生・停止を制御する
    /// </summary>
    private void HandlePushSound()
    {
        if (pushSound == null) return;

        // 落下などの縦振動を無視し、純粋な「横方向（X軸）の速度」だけ判定
        float currentHorizontalSpeed = Mathf.Abs(rb.velocity.x);

        // 一定以上の横速度で動いている場合のみ再生
        if (currentHorizontalSpeed > MinMoveSpeedThreshold)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.clip = pushSound;
                audioSource.Play();
            }
        }
        else
        {
            // 横移動が止まったら即座に音を停止
            if (audioSource.isPlaying && audioSource.clip == pushSound)
            {
                audioSource.Stop();
            }
        }
    }
}