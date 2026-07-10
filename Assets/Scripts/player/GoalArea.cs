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
        Debug.Log("全員到達！現在のステージ情報を引き渡し用データとして保存し、クリアシーンへ移行します。");

        // -------------------------------------------------------------
        // ★追加：星の獲得状況がクリア画面で混ざらないよう、現在のステージ番号を保存
        // -------------------------------------------------------------
        if (StageMenuManager.Instance != null)
        {
            // StageMenuManagerに現在のステージインデックス（CurrentStageIndex）や
            // リトライ用、次のステージ用のPlayerPrefsへのメモをまとめてやらせる
            StageMenuManager.Instance.PrepareClearSceneTransition();
        }
        else
        {
            // 万が一 StageMenuManager がない場合のフォールバック（従来の保存処理）
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
            SaveManager.Instance.SaveGame(); // メモリ上のキープ状態をファイルに本保存
            Debug.Log("【セーブ】ステージクリアに伴い、星の獲得データを正式に保存しました。");
        }
        else
        {
            Debug.LogWarning("SaveManager のインスタンスが見つからないため、クリア時の正式保存がスキップされました。");
        }
        // -------------------------------------------------------------

        // 満を持してクリアシーン（ClearScene）へ遷移
        SceneManager.LoadScene(nextStageSceneName);
    }
}