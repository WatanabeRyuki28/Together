using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    // --- 定数定義（マジックナンバーの排除） ---
    private const float RightDirection = 1f;
    private const float LeftDirection = -1f;
    private const float DefaultHitEffectLifeTime = 1f; // ヒットエフェクト消滅までの時間
    private const float RotationAngleThresholdMin = 170f;
    private const float RotationAngleThresholdMax = 190f;

    [Header("Projectile Settings")]
    [SerializeField] private float speed = 10.0f;
    [SerializeField] private float lifeTime = 1.5f; // 1.5秒で自動消滅
    [SerializeField] private ElementType projectileType;

    [Header("Collision Settings")]
    // インスペクターで「壁(Wall)」「床(Ground)」「箱(Pushable)」にチェックを入れる
    [SerializeField] private LayerMask collisionLayers;

    [Header("HITエフェクト")]
    [SerializeField] private GameObject hitEffect;

    private Rigidbody2D rb;
    private float moveDirection = RightDirection; // 飛ぶ方向（1なら右、-1なら左）
    private bool isInitialized = false; // 初期化が完了したかのフラグ

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // 重力の影響を無効化
        rb.gravityScale = 0f;
    }

    private void Start()
    {
        // すでに Initialize で方向が決まっているなら、以下の自動判定は完全にスルーする
        if (!isInitialized)
        {
            // ネットワーク経由で相手の画面に直接生成された時だけの救済措置
            float currentRotationZ = transform.eulerAngles.z;
            if (currentRotationZ > RotationAngleThresholdMin && currentRotationZ < RotationAngleThresholdMax)
            {
                moveDirection = LeftDirection;
            }
            else
            {
                moveDirection = RightDirection;
            }
            isInitialized = true;
        }

        // 確定した正しい方向（moveDirection）へ物理速度を設定する
        if (rb != null)
        {
            rb.velocity = new Vector2(speed * moveDirection, 0f);
        }

        // 指定した秒数後に自分を削除
        Destroy(gameObject, lifeTime);
    }

    // プレイヤーから「向き」と「属性」を受け取って、勢いよく飛ばす処理（ローカルプレイヤー用）
    public void Initialize(float direction, ElementType playerElement)
    {
        moveDirection = direction;
        projectileType = playerElement; // プレイヤーの属性（FireやIce）を自動コピー
        isInitialized = true; // ここで先に初期化フラグを立てる

        // 向いている方向（右 or 左）に物理速度をセットする
        if (rb != null)
        {
            rb.velocity = new Vector2(speed * moveDirection, 0f);
        }
    }

    private void FixedUpdate()
    {
        // 万が一、衝突などで弾の速度が落ちたり変わったりしないよう、消えるまで速度を一定に維持する
        if (isInitialized && rb != null)
        {
            rb.velocity = new Vector2(speed * moveDirection, 0f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 接触位置（着弾点）を計算
        Vector3 hitPoint = other.ClosestPoint(transform.position);

        // エフェクト生成
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(
                hitEffect,
                hitPoint,
                Quaternion.identity
            );

            // 定数で設定した時間後にエフェクトを削除
            Destroy(effect, DefaultHitEffectLifeTime);
        }

        // 1. ピンポイント生成に対応した WaterSurface 等のコンポーネントかチェック
        if (other.TryGetComponent<WaterSurface>(out var waterSurface))
        {
            waterSurface.OnInteractAtPoint(projectileType, hitPoint);
            Destroy(gameObject);
            return;
        }

        // 2. 通常の IInteractable ギミックに当たった場合
        IInteractable target = other.GetComponent<IInteractable>();
        if (target != null)
        {
            target.OnInteract(projectileType); // 属性を伝達
            Destroy(gameObject);              // 即座に消滅
            return;
        }

        // 3. プレイヤーや壁などに当たった場合
        // LayerMaskに含まれるレイヤー（壁や床など）に接触したか判定
        if (((1 << other.gameObject.layer) & collisionLayers) != 0)
        {
            Destroy(gameObject); // 即座に消滅
        }
    }
}