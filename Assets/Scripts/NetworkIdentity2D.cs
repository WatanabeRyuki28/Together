using UnityEngine;

public class NetworkIdentity2D : MonoBehaviour
{
    [Header("オブジェクト固有のID（被らない数値を手動設定）")]
    public int objectId;

    // 自分が動かす権利を持っているか
    public bool isOwnedByLocal = false;

    private Rigidbody2D rb;
    private Vector2 lastPosition;
    private float sendInterval = 0.05f;
    private float nextSendTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        lastPosition = transform.position;

        // もし弾（1000以上のID）で、かつ生成時にすでに local 権限が与えられている場合は処理をスキップ
        if (objectId >= 1000 && isOwnedByLocal)
        {
            return;
        }

        // 最初からステージにある箱などは、一律で「ホスト（赤・0番）」が初期管理権を持つ
        if (NetworkManager.Instance != null)
        {
            isOwnedByLocal = (NetworkManager.Instance.myRealSelectedChar == 0);
        }

        // もし自分が権利を持っていない（ゲスト）なら、初期状態では物理演算を完全に止めておく
        if (!isOwnedByLocal && rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.velocity = Vector2.zero;
        }
    }

    void Update()
    {
        // 自分が動かす権利を持っていないなら、位置情報の送信はしない
        if (!isOwnedByLocal) return;

        if (objectId >= 1000) return;

        // 自分が権利を持っている時は、動いたら通信を送る
        if (Time.time >= nextSendTime && Vector2.Distance(transform.position, lastPosition) > 0.01f)
        {
            SendObjectData();
            lastPosition = transform.position;
            nextSendTime = Time.time + sendInterval;
        }
    }

    // プレイヤーが箱にぶつかった（押した）瞬間に呼び出される判定
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // ぶつかってきたのが「自分が操作しているローカルプレイヤー」だった場合
        var player = collision.gameObject.GetComponent<PlayerController>();
        if (player != null && player.IsLocalPlayer)
        {
            // もし自分がまだ権利を持っていなかったら、権利を自分に切り替える！
            if (!isOwnedByLocal)
            {
                isOwnedByLocal = true;

                // 物理演算を通常（Dynamic）に戻して、自分が押して動かせるようにする
                if (rb != null)
                {
                    rb.bodyType = RigidbodyType2D.Dynamic;
                }
                Debug.Log($"[権利獲得] オブジェクト {objectId} の操作権が自分に移りました！");
            }
        }
    }

    // 相手（通信）から座標が届いた時に呼び出されるメソッド
    public void UpdatePositionFromNetwork(Vector3 targetPos)
    {
        // もし相手が動かしているデータが届いたら、自分の操作権は自動でオフにする（権利を譲る）
        if (isOwnedByLocal)
        {
            isOwnedByLocal = false;

            // 相手が動かすので、自分側の物理演算は邪魔しないように止める
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.velocity = Vector2.zero;
            }
        }

        // 届いた座標に移動させる
        transform.position = targetPos;
    }

    private async void SendObjectData()
    {
        if (NetworkManager.Instance == null) return;

        InGameMoveData data = new InGameMoveData();
        data.dataType = "object";
        data.room_id = NetworkManager.Instance.myRoomID;
        data.id = objectId;
        data.position_x = transform.position.x;
        data.position_y = transform.position.y;

        string json = JsonUtility.ToJson(data);
        await NetworkManager.Instance.SendMessageAsync(json);
    }
}