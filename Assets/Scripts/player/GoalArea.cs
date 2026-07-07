using UnityEngine;
using UnityEngine.SceneManagement;

public class GoalArea : MonoBehaviour
{
    [Header("クリアに必要なプレイヤー人数")]
    [SerializeField] private int requiredPlayersToClear = 2;

    [Header("次に進むステージ（クリアシーン名）")]
    [SerializeField] private string nextStageSceneName = "ClearScene";

    private int currentPlayersInGoal = 0;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerController player = collision.GetComponent<PlayerController>();

        if (player != null)
        {
            currentPlayersInGoal++;
            player.CanMove = false;

            if (currentPlayersInGoal >= requiredPlayersToClear)
            {
                ClearStage();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        PlayerController player = collision.GetComponent<PlayerController>();

        if (player != null)
        {
            currentPlayersInGoal--;
            player.CanMove = true;

            if (currentPlayersInGoal < 0)
            {
                currentPlayersInGoal = 0;
            }
        }
    }

    private void ClearStage()
    {
        Debug.Log("全員到達！現在のステージ名を保存してクリアシーンへ移行します。");

        // -------------------------------------------------------------
        // ★追加：ステージクリア確定のタイミングで、星の獲得データを正式セーブ
        // -------------------------------------------------------------
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame(); // メモリ上のキープ状態をファイルに本保存
            Debug.Log("【セーブ】ステージクリアに伴い、星の獲得データを正式に保存しました。");
        }
        else
        {
            Debug.LogWarning("SaveManager のインスタンスが見つからないため、クリア時の正式保存がスキップされました。");
        }
        // -------------------------------------------------------------

        // シーンが切り替わる前に、現在のステージ名（"Stage1"など）を「RetrySceneName」という名前でメモリにセーブします
        string currentStageName = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetString("RetrySceneName", currentStageName);

        int nextStageIndex = SceneManager.GetActiveScene().buildIndex + 1;
        PlayerPrefs.SetInt("NextStageIndex", nextStageIndex);
        PlayerPrefs.Save(); // 確実に保存

        // 満を持してクリアシーン（ClearScene）へ遷移
        SceneManager.LoadScene(nextStageSceneName);
    }
}