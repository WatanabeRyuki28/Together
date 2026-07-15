using System.Runtime.InteropServices;
using UnityEngine;

public class StarItem : MonoBehaviour
{
    //[追加]
    [Header("Audio Settings (効果音)")]
    [SerializeField] private AudioClip sterSound;  // starを取った時の音

    private AudioSource audioSource;

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

            //[追加] スターをとったら音を鳴らす
            if (sterSound != null)
            {
                AudioSource.PlayClipAtPoint(sterSound, transform.position);
            }

            // スターのオブジェクトを消去
            Destroy(gameObject);
        }
    }
}