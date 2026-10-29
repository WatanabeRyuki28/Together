using UnityEngine;

public class StarParticleFly : MonoBehaviour
{
    private Transform uiTarget;      // 吸い込み先のUI
    private Vector3 startPosition;   // 出発地
    private float journeyTime = 0f;  // 経過時間
    private float duration = 0.7f;   // 移動にかかる時間（秒）
    private float arcHeight = 1.5f;  // 放物線の高さ
    private bool isFlying = false;
    private Vector3 initialScale;    // 当初の大きさ

    void Start()
    {
        initialScale = transform.localScale;

        // 【自動検索】もしUIターゲットが未設定なら、シーン内から "UITarget" という名前のオブジェクトを自動で探す
        if (uiTarget == null)
        {
            GameObject targetObj = GameObject.FindWithTag("Sterf"); // または GameObject.Find("UIのオブジェクト名")
            if (targetObj != null)
            {
                uiTarget = targetObj.transform;
            }
            else
            {
                // 見つからない場合は仮で画面の左上方向へ飛ばす
                Debug.LogWarning("UIターゲットが見つかりません。");
            }
        }

        startPosition = transform.position;
        isFlying = true;
    }

    // 外部から明示的にターゲットを渡したい時用
    public void Initialize(Transform target)
    {
        uiTarget = target;
        startPosition = transform.position;
        journeyTime = 0f;
        isFlying = true;
    }

    void Update()
    {
        if (!isFlying) return;

        // もし途中でターゲットが消えた場合の安全対策
        if (uiTarget == null)
        {
            Destroy(gameObject);
            return;
        }

        journeyTime += Time.deltaTime;
        float linearT = journeyTime / duration; // 0から1への進捗

        // 目的地に到着した場合
        if (linearT >= 1f)
        {
            transform.position = uiTarget.position;
            Destroy(gameObject); // 消去
            return;
        }

        // 1. 出発地からゴールへの直線的な位置を計算
        Vector3 currentTargetPos = uiTarget.position;
        Vector3 currentPos = Vector3.Lerp(startPosition, currentTargetPos, linearT);

        // 2. 放物線（Y軸に上方向のカーブ）を加える
        float heightOffset = Mathf.Sin(linearT * Mathf.PI) * arcHeight;
        currentPos.y += heightOffset;

        // 位置を更新
        transform.position = currentPos;

        // 3. 【演出】ゴールに近づく（残り20%の時間）につれて、キュッと縮みながら吸い込まれるようにする
        if (linearT > 0.8f)
        {
            float shrinkT = (linearT - 0.8f) / 0.2f; // 0〜1に正規化
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, shrinkT);
        }
    }
}