using UnityEngine;

public class NetworkIdentity2D : MonoBehaviour
{
    [Header("オブジェクト固有のID")]
    public int objectId;

    [Header("自分が動かす権利を持っているか")]
    public bool isOwnedByLocal = false;

    private Rigidbody2D rb;
    private Vector2 lastPosition;
    private float sendInterval = 0.05f;
    private float nextSendTime;

    // ネットワークから最後に受け取った目標座標（補間用）
    private Vector3 targetPosition;
    private bool isReceivingNetworkData = false;

    void OnEnable()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnObjectPosReceived += HandleObjectPosReceived;
        }
    }

    void OnDisable()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.OnObjectPosReceived -= HandleObjectPosReceived;
        }
    }

    // サーバーから何かしらのオブジェクト位置が届いたとき、自分のIDと一致していれば更新
    private void HandleObjectPosReceived(NetMessage msg)
    {
        // 届いたメッセージの id が、このオブジェクトの objectId と一致するか？
        if (msg.id == objectId)
        {
            UpdatePositionFromNetwork(new Vector3(msg.position_x, msg.position_y, 0f));
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        lastPosition = transform.position;
        targetPosition = transform.position;

        // 弾などの特殊オブジェクト
        if (objectId >= 1000 && isOwnedByLocal) return;

        // 初期状態の所有権（デフォルトはホスト：char == 0）
        if (NetworkManager.Instance != null)
        {
            isOwnedByLocal = (NetworkManager.Instance.myRealSelectedChar == 0);
        }

        UpdateRigidbodyState();
    }

    void Update()
    {
        // 自分が権利を持っている場合：自分の位置をネットワークに送信
        if (isOwnedByLocal)
        {
            if (objectId >= 1000) return;

            if (Time.time >= nextSendTime && Vector2.Distance(transform.position, lastPosition) > 0.005f)
            {
                SendPositionToNetwork();
                lastPosition = transform.position;
                nextSendTime = Time.time + sendInterval;
            }
        }
        //  権利を持っていない場合：受け取った目標座標へ滑らかに近づける（カクつき防止）
        else if (isReceivingNetworkData)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 15f);
        }
    }

    // プレイヤーが箱に触れている間、権利を維持・あるいは奪い取る
    private void OnCollisionStay2D(Collision2D collision)
    {
        var player = collision.gameObject.GetComponent<PlayerController>();
        if (player != null && player.IsLocalPlayer)
        {
            // 自分がまだ権利を持っていなければ、権利を奪取する通信をサーバーに送る等が必要
            if (!isOwnedByLocal)
            {
                ClaimOwnership();
            }
        }
    }

    // 権利を自分に書き換えるメソッド
    public void ClaimOwnership()
    {
        isOwnedByLocal = true;
        isReceivingNetworkData = false;
        UpdateRigidbodyState();

        Debug.Log($"[権利獲得] オブジェクト {objectId} の操作権が自分に移りました！");
    }

    // 物理演算の状態を切り替える
    private void UpdateRigidbodyState()
    {
        if (rb != null)
        {
            if (isOwnedByLocal)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
            }
            else
            {
                // 非権利者は物理演算を切る（またはKinematicにして位置の受信に専念させる）
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.velocity = Vector2.zero;
            }
        }
    }

    // ネットワークから座標が届いたとき
    public void UpdatePositionFromNetwork(Vector3 newPos)
    {
        // もし自分が「権利者」としてバリバリ動かしている最中に、
        // 古いネットワークデータが届いて自分の動きが邪魔されないようにガードする
        if (isOwnedByLocal)
        {
            return;
        }

        targetPosition = newPos;
        isReceivingNetworkData = true;
    }

    private void SendPositionToNetwork()
    {
        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.SendObjectPosition(objectId, transform.position.x, transform.position.y);
        }
    }
}