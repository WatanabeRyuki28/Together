using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class StageExitManager : MonoBehaviour
{
    public static StageExitManager Instance { get; private set; }

    [Header("UI参照設定")]
    [SerializeField] private Button yesButton;       // yesButtonTransformのButtonコンポーネント
    [SerializeField] private Text exitStatusText;    // 「0/2」などを表示するテキストUI
    [SerializeField] private string stageSelectSceneName = "StageSelectScene"; // 遷移先のシーン名

    private bool myExitVote = false;     // 自分が「はい」を押したか
    private bool remoteExitVote = false;  // 相手が「はい」を押したか
    private bool isExitingScene = false;  // 二重遷移防止フラグ

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        // ボタンのクリックイベントを登録
        if (yesButton != null)
        {
            yesButton.onClick.AddListener(OnYesExitButtonClick);
        }
        UpdateExitStatusUI();
    }

    /// <summary>
    /// はい（yesButtonTransform）が押された時の処理
    /// </summary>
    public void OnYesExitButtonClick()
    {
        if (myExitVote || isExitingScene) return;

        myExitVote = true;
        UpdateExitStatusUI();

        // 相手に自分が退出を希望したことを送信
        SendExitVote(true);

        // すでに相手も押していれば退出処理へ
        CheckBothPlayersReadyToExit();
    }

    /// <summary>
    /// 相手から投票データが届いた時に呼び出される関数
    /// </summary>
    public void ReceiveRemoteExitVote(bool isReady)
    {
        remoteExitVote = isReady;
        UpdateExitStatusUI();

        CheckBothPlayersReadyToExit();
    }

    private void UpdateExitStatusUI()
    {
        if (exitStatusText == null || isExitingScene) return;

        int voteCount = 0;
        if (myExitVote) voteCount++;
        if (remoteExitVote) voteCount++;

        exitStatusText.text = $"{voteCount}/2";
    }

    private void CheckBothPlayersReadyToExit()
    {
        if (myExitVote && remoteExitVote && !isExitingScene)
        {
            isExitingScene = true;
            StartCoroutine(ExitStageRoutine());
        }
    }

    private IEnumerator ExitStageRoutine()
    {
        if (exitStatusText != null)
        {
            exitStatusText.text = "ステージを退出します...";
        }

        yield return new WaitForSecondsRealtime(1.5f);

        // ステージセレクトへ戻る
        SceneManager.LoadScene(stageSelectSceneName);
    }

    private async void SendExitVote(bool isReady)
    {
        if (NetworkManager.Instance == null) return;

        StageExitSelectData exitData = new StageExitSelectData();
        exitData.type = "stage_exit"; // 識別用タイプ名
        exitData.room_id = NetworkManager.Instance.myRoomID;
        exitData.index = NetworkManager.Instance.myPlayerIndex;
        exitData.is_ready = isReady;

        string json = JsonUtility.ToJson(exitData);
        await NetworkManager.Instance.SendMessageAsync(json);
    }
}