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

    private float currentCharge = 0f;
    private bool isFireNearby = false;
    private bool isHitByFire = false;
    private float fireHitTimer = 0f;

    private void Start()
    {
        if (movingPlatform != null)
        {
            startPosition = movingPlatform.localPosition;
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
        if (isFireNearby || isHitByFire)
        {
            currentCharge += chargeRate * Time.deltaTime;
        }
        else
        {
            currentCharge -= decreaseRate * Time.deltaTime;
        }

        currentCharge = Mathf.Clamp01(currentCharge);

        UpdatePlatformPosition();
        UpdateGaugeUI();
    }

    public void OnInteract(ElementType type)
    {
        if (type == ElementType.Fire)
        {
            isHitByFire = true;
            fireHitTimer = 0.5f;
        }
    }

    // プレイヤーが入った時にフラグをONにする
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && player.Element == ElementType.Fire)
        {
            isFireNearby = true;
        }
    }

    // プレイヤーが領域内にいる間はONを保持
    private void OnTriggerStay2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && player.Element == ElementType.Fire)
        {
            isFireNearby = true;
        }
    }

    // プレイヤーが出た時にフラグをOFFにする
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
}