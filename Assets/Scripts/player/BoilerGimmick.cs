using UnityEngine;
using UnityEngine.UI;

public class BoilerGimmick : MonoBehaviour, IInteractable
{
    [Header("チャージ設定")]
    [Tooltip("1秒あたりのチャージ量 (0.25 = 25%)")]
    [SerializeField] private float chargeRate = 0.25f;
    [Tooltip("1秒あたりの自動減少量 (0.25 = 25%)")]
    [SerializeField] private float decreaseRate = 0.25f;

    [Header("動く床の設定")]
    [SerializeField] private Transform movingPlatform;
    [SerializeField] private Vector3 startPosition;
    [SerializeField] private Vector3 endPosition;

    [Header("メーター（UI）の設定")]
    [SerializeField] private Slider gaugeSlider;         // スライダー

    [Header("アニメーションの設定")]
    [SerializeField] private Animator boilerAnimator;    // ボイラーのアニメーター
    [SerializeField] private string isWorkingParam = "IsWorking"; // アニメーターのパラメータ名

    private float currentCharge = 0f;
    private bool isFireNearby = false;
    private bool isHitByFire = false;
    private float fireHitTimer = 0f;
    private bool wasZeroBefore = true; // 0になった瞬間を検知するためのフラグ

    private void Start()
    {
        if (movingPlatform != null)
        {
            startPosition = movingPlatform.localPosition;
        }

        if (boilerAnimator == null)
        {
            boilerAnimator = GetComponent<Animator>();
        }
    }

    private void Update()
    {
        // 炎の攻撃（球など）を受けた場合のタイマー更新
        if (isHitByFire)
        {
            fireHitTimer -= Time.deltaTime;
            if (fireHitTimer <= 0f)
            {
                isHitByFire = false;
            }
        }

        // チャージ判定：炎キャラが領域内にいる、または攻撃がHit中
        bool isActivating = (isFireNearby || isHitByFire);

        if (isActivating)
        {
            currentCharge += chargeRate * Time.deltaTime;
        }
        else
        {
            currentCharge -= decreaseRate * Time.deltaTime;
        }

        // 0から1の間にクランプ
        float previousCharge = currentCharge;
        currentCharge = Mathf.Clamp01(currentCharge);

        // --- メーターが0になった瞬間、または0から変動したときの処理 ---
        if (currentCharge <= 0f && !wasZeroBefore)
        {
            OnChargeReachedZero();
        }
        else if (currentCharge > 0f && wasZeroBefore)
        {
            OnChargeStarted();
        }
        wasZeroBefore = (currentCharge <= 0f);

        // --- 変更点：チャージが0より上（残っている）の間はアニメーションを稼働状態にする ---
        bool hasCharge = currentCharge > 0f;

        UpdatePlatformPosition();
        UpdateGaugeUI();
        UpdateAnimatorState(hasCharge);
    }

    public void OnInteract(ElementType type)
    {
        if (type == ElementType.Fire)
        {
            isHitByFire = true;
            fireHitTimer = 0.5f;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && player.Element == ElementType.Fire)
        {
            isFireNearby = true;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && player.Element == ElementType.Fire)
        {
            isFireNearby = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && player.Element == ElementType.Fire)
        {
            isFireNearby = false;
        }
    }

    private void UpdatePlatformPosition()
    {
        if (movingPlatform == null) return;
        movingPlatform.localPosition = Vector3.Lerp(startPosition, endPosition, currentCharge);
    }

    private void UpdateGaugeUI()
    {
        if (gaugeSlider != null)
        {
            gaugeSlider.value = currentCharge;
        }
    }

    private void UpdateAnimatorState(bool isWorking)
    {
        if (boilerAnimator != null)
        {
            boilerAnimator.SetBool(isWorkingParam, isWorking);
        }
    }

    // メーターが0になった瞬間に呼ばれるメソッド
    private void OnChargeReachedZero()
    {
        Debug.Log("メーターが0になりました！");
        // ここでさらに「0になった瞬間のトリガー」を引くこともできます
        // boilerAnimator.SetTrigger("OnDepleted");
    }

    // チャージが0から増え始めた瞬間に呼ばれるメソッド
    private void OnChargeStarted()
    {
        Debug.Log("チャージが開始されました！");
    }
}