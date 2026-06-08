using UnityEngine;

public class StarItem : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 触れてきたのがプレイヤー（PlayerControllerを持っているか）をチェック
        PlayerController player = collision.GetComponent<PlayerController>();

        if (player != null)
        {
            // ★合体した StageMenuManager にスター獲得を伝える
            if (StageMenuManager.Instance != null)
            {
                StageMenuManager.Instance.AddStar();
            }

            // スターのオブジェクトを消去
            Destroy(gameObject);
        }
    }
}