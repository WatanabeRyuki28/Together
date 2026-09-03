using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // コルーチンを使うために必須
using System.Collections.Generic;

public class GoalArea : MonoBehaviour
{
    [Header("クリアに必要なプレイヤー人数")]
    [SerializeField] private int requiredPlayersToClear = 2;

    [Header("次に進むステージ（クリアシーン名）")]
    [SerializeField] private string nextStageSceneName = "ClearScene";

    [Header("花火演出スクリプトの参照")]
    [SerializeField] private FireWork fireWork; // 花火スクリプトをセットする

    [Header("BGM / 効果音設定")]
    [SerializeField] private AudioSource audioSource; // 再生用のAudioSource（指定がない場合は自オブジェクトから自動取得）
    [SerializeField] private AudioClip clearJingle;    // クリア時に鳴らすファンファーレやBGM
    [SerializeField] private bool stopBgmOnGoal = true;  // ゴール時に元のBGMを停止するかどうか

    // 二重カウントを防ぐため、エリア内にいる一意のプレイヤーをリストで管理
    private HashSet<PlayerController> playersInGoal = new HashSet<PlayerController>();

    // 二重クリア処理防止フラグ
    private bool isClearing = false;

    private void Awake()
    {
        // AudioSourceが指定されていなければ自身のコンポーネントを取得
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isClearing) return; // すでにクリア処理中なら何もしない

        PlayerController player = collision.GetComponent<PlayerController>();

        if (player != null)
        {
            if (playersInGoal.Add(player))
            {
                Debug.Log($"{player.name} がゴールエリアに入りました。");

                if (playersInGoal.Count >= requiredPlayersToClear)
                {
                    string currentSceneName = SceneManager.GetActiveScene().name;

                    // Stage 1 クリア
                    if (currentSceneName == "Stage1")
                    {
                        StageManager.ClearStage(0);
                    }

                    if (currentSceneName == "Stage2")
                    {
                        StageManager.ClearStage(1);
                    }

                    if (currentSceneName == "Stage3")
                    {
                        StageManager.ClearStage(2);
                    }
                    if (currentSceneName == "Stage4")
                    {
                        StageManager.ClearStage(3);
                    }
                    if (currentSceneName == "Stage5")
                    {
                        StageManager.ClearStage(4);
                    }
                    if (currentSceneName == "Stage6")
                    {
                        StageManager.ClearStage(5);
                    }


                    // クリアコルーチンを開始
                    StartCoroutine(ClearStageRoutine());
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (isClearing) return; // クリア処理に入っていたら退出カウントしない

        PlayerController player = collision.GetComponent<PlayerController>();

        if (player != null)
        {
            if (playersInGoal.Remove(player))
            {
                Debug.Log($"{player.name} がゴールエリアから出ました。");
            }
        }
    }

    private IEnumerator ClearStageRoutine()
    {
        isClearing = true;
        Debug.Log("全員到達！キャラを停止して花火演出を開始します。");

        // --- BGM・サウンド再生処理 ---
        PlayClearSound();

        PlayerController[] allPlayers = FindObjectsOfType<PlayerController>();
        foreach (var player in allPlayers)
        {
            if (player != null)
            {
                // 1. 操作不能にする
                player.CanMove = false;

                // 2. 慣性（移動速度）を強制的に 0 にしてその場でピタッと止める
                Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.velocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }

                // 3. アニメーション（走るモーション等）もアイドル（棒立ち）に戻す処理
                Animator anim = player.GetComponent<Animator>();
                if (anim != null)
                {
                    // もしアニメーターで Speed などのパラメータを使っていれば 0 にリセット
                    anim.SetFloat("Speed", 0f);
                    anim.SetBool("IsMoving", false);
                }
            }
        }

        // 花火演出を実行し、すべての打ち上げが終わるまで待機
        if (fireWork != null)
        {
            yield return StartCoroutine(fireWork.HanabiShot());
        }
        else
        {
            Debug.LogWarning("FireWork の参照がありません。演出をスキップします。");
        }

        yield return new WaitForSeconds(0.5f);

        Debug.Log("花火演出終了。ステージ情報を保存してクリアシーンへ移行します。");

        // ステージ情報の保存処理
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

        // 星の獲得データを正式セーブ
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

    /// <summary>
    /// クリア時のサウンドを再生する処理
    /// </summary>
    private void PlayClearSound()
    {
        // StageManagerやSoundManager等の全体BGMを管理するシングルトンがあればここで停止することも可能
        if (stopBgmOnGoal && audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // ジングル（ファンファーレ/BGM）が設定されている場合
        if (clearJingle != null)
        {
            if (audioSource != null)
            {
                // PlayOneShotで重ねて再生（あるいは audioSource.clip = clearJingle; audioSource.Play();）
                audioSource.PlayOneShot(clearJingle);
            }
            else
            {
                // AudioSourceが指定されていない場合は一時的な3D/2Dサウンドとして再生
                AudioSource.PlayClipAtPoint(clearJingle, Camera.main != null ? Camera.main.transform.position : transform.position);
            }
        }
    }
}