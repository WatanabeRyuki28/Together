using UnityEngine;
using UnityEngine.UIElements;

public class CameraController : MonoBehaviour
{
    [Header("プレイヤーの設定")]

    [SerializeField] private string targetName1 = "player1(Clone)";
    [SerializeField] private string targetName2 = "player2(Clone)";
    private GameObject player1;
    private GameObject player2;

    [Header("カメラの追尾スピード（なめらかさ）")]
    [Range(0.01f, 1f)][SerializeField] private float smoothSpeed = 0.125f;

    [Header("これ以上落ちたらカメラ追尾を諦めるY座標のしきい値")]
    [SerializeField] private float fallThreshold = -20f; // ステージの床より少し下くらい

    private void LateUpdate()
    {
        player1 = GameObject.Find(targetName1);
        player2 = GameObject.Find(targetName2);
        if (player1 == null || player2 == null) return;

        // 1Pと2Pがそれぞれ正常な位置（穴に落ちていない状態）にいるかチェック
        bool p1IsAlive = player1.transform.position.y > fallThreshold;
        bool p2IsAlive = player2.transform.position.y > fallThreshold;

        Vector3 targetPosition = transform.position;

        if (p1IsAlive && p2IsAlive)
        {
            // 【通常時】2人とも画面内にいるなら、2人のちょうど真ん中をターゲットにする
            targetPosition = (player1.transform.position + player2.transform.position) / 2f;
        }
        else if (p1IsAlive)
        {
            // 2Pだけが落ちたなら、1Pだけを追いかける
            targetPosition = player1.transform.position;
        }
        else if (p2IsAlive)
        {
            // 1Pだけが落ちたなら、2Pだけを追いかける
            targetPosition = player2.transform.position;
        }

        // カメラのZ位置（-10など）は元の値を維持する
        targetPosition.z = transform.position.z;

        // 計算した目標位置に向かって、カメラをなめらかに移動させる
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, smoothSpeed);
        transform.position = smoothedPosition;
    }
}