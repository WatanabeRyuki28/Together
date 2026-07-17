using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic; // Listを使うために追加

public class GoalArea : MonoBehaviour
{
    [Header("クリアに必要なプレイヤー人数")]
    [SerializeField] private int requiredPlayersToClear = 2;

    [Header("次に進むステージ（クリアシーン名）")]
    [SerializeField] private string nextStageSceneName = "ClearScene";

    // 二重カウントを防ぐため、エリア内にいる一意のプレイヤーをリストで管理
    private HashSet<PlayerController> playersInGoal = new HashSet<PlayerController>();

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerController player = collision.GetComponent<PlayerController>();

        if (player != null)
        {
            // すでに登録済みのプレイヤーでなければ追加
            if (playersInGoal.Add(player))
            {
                Debug.Log($"{player.name} がゴールエリアに入りました。");

                if (playersInGoal.Count >= requiredPlayersToClear)
                {
                    ClearStage();
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        PlayerController player = collision.GetComponent<PlayerController>();

        if (player != null)
        {
            // リストから削除（存在していた場合のみカウントが減る）
            if (playersInGoal.Remove(player))
            {
                Debug.Log($"{player.name} がゴールエリアから出ました。");
            }
        }
    }

    private void ClearStage()
    {
        Debug.Log("全員到達！現在のステージ情報を引き渡し用データとして保存し、クリアシーンへ移行します。");

        // クリア確定時に初めて全員の動きを止める
        foreach (var player in playersInGoal)
        {
            if (player != null)
            {
                player.CanMove = false;
            }
        }

        // -------------------------------------------------------------
        // ★ステージ情報の保存処理
        // -------------------------------------------------------------
        if (StageMenuManager.Instance != null)
        {
            StageMenuManager.Instance.PrepareClearSceneTransition();
        }
        else
        {
            Debug.LogWarning("StageMenuManager のインスタンスが見つからないため、個別に遷移情報を保存します。");
            string currentStageName = SceneManager.GetActiveScene().name;
            PlayerPrefs.SetString("RetrySceneName", currentStageName);

            int nextStageIndex = SceneManager.GetActiveScene().buildIndex + 1;
            PlayerPrefs.SetInt("NextStageIndex", nextStageIndex);
            PlayerPrefs.Save();
        }

        // -------------------------------------------------------------
        // ★星の獲得データを正式セーブ
        // -------------------------------------------------------------
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
            Debug.Log("【セーブ】ステージクリアに伴い、星の獲得データを正式に保存しました。");
        }
        else
        {
            Debug.LogWarning("SaveManager のインスタンスが見つからないため、クリア時の正式保存がスキップされました。");
        }

        // シーン遷移
        SceneManager.LoadScene(nextStageSceneName);
    }
}