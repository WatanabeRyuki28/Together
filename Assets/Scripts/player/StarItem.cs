using System.Runtime.InteropServices;
using UnityEngine;

public class StarItem : MonoBehaviour
{
    //[追加]
    [Header("Audio Settings (効果音)")]
    [SerializeField] private AudioClip sterSound;  // starを取った時の音

    [Header("取得エフェクト")]
    [SerializeField] private GameObject getEffect;

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

            // エフェクト生成
            if (getEffect != null)
            {
                GameObject effect = Instantiate(
                    getEffect,
                    transform.position,
                    Quaternion.identity
                );

                // 1秒後にエフェクトを削除
                Destroy(effect, 1f);
            }

            // スターのオブジェクトを消去
            Destroy(gameObject);
        }
    }
}