using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // ボタンの制御用


public class StageManager : MonoBehaviour
{

    public GameObject confirmButton; // 最初は非表示か、ホストの時だけ出したいボタン

    private int currentStageIndex = -1; // 現在選んでいるステージ番号（-1は未選択）

    void Start()
    {
        CheckHost();

        // 起動時はまだステージを選んでいないので、決定ボタンを非表示にする
        if (confirmButton != null)
        {
            confirmButton.SetActive(false);
        }
    }

    void Update()
    {

    }

   
    public void SelectStage(int stageIndex)
    {
        // ホスト（1P）以外はボタンを押されても無視する
        if (NetworkManager.Instance == null || NetworkManager.Instance.myPlayerIndex != 0)
        {
            Debug.LogWarning("ホスト以外のプレイヤーはステージを選択できません。");
            return;
        }

        currentStageIndex = stageIndex;
        Debug.Log($"ホストがステージ {currentStageIndex} を選択しました（仮決定）。");

        // 【ホスト側】ステージが選ばれたので、画面に「決定ボタン」を出現させる！
        if (confirmButton != null)
        {
            confirmButton.SetActive(true);
        }

       
        SendStageSelectNotification(currentStageIndex, false);


    }

    
    // 決定ボタン用
    public void ConfirmStageSelection()
    {
        if (NetworkManager.Instance == null || NetworkManager.Instance.myPlayerIndex != 0) return;

        // 何も選んでないのに押されたら防ぐ
        if (currentStageIndex == -1) return;

        Debug.Log($"ホストがステージ {currentStageIndex} で本決定しました！ゲームを開始します。");

        // 相手に「確定した」と通知を送る
        SendStageSelectNotification(currentStageIndex, true);

        // ホストの画面を即遷移させる
        LoadTargetScene(currentStageIndex);
    }

    // サーバー送信処理（共通）
    private async void SendStageSelectNotification(int stageIndex, bool isReady)
    {
        if (NetworkManager.Instance == null) return;

        StageSelectData msgData = new StageSelectData();
        msgData.type = "stage_select";
        msgData.name_id = NetworkManager.Instance.myPlayerId;
        msgData.room_id = NetworkManager.Instance.myRoomID;
        msgData.index = NetworkManager.Instance.myPlayerIndex;
        msgData.IsStarted = isReady; 

        msgData.stage_index = stageIndex;
        msgData.stage_ready = isReady; // 決定フラグ

        string jsonMsg = JsonUtility.ToJson(msgData);
        await NetworkManager.Instance.SendMessageAsync(jsonMsg);
    }

    // 2P（ゲスト）側のメッセージ受信処理
    public void HandleRemoteStageMessage(string msg)
    {
        var stageData = JsonUtility.FromJson<StageSelectData>(msg);
        if (stageData == null) return;

        if (stageData.type == "stage_select")
        {
            // ホストが選んでいる最中のとき
            if (!stageData.stage_ready)
            {
                Debug.Log($"【同期】ホストがステージ {stageData.stage_index} を選択中（仮）...");

                
            }
            // ホストが決定ボタンを押したとき
            else
            {
                Debug.Log($"【同期】ホストがステージ {stageData.stage_index} で確定しました。遷移します。");
                LoadTargetScene(stageData.stage_index);
            }
        }
    }

    // シーン遷移用の共通関数
    private void LoadTargetScene(int stageIndex)
    {
        if (stageIndex == 0)
        {
            SceneManager.LoadScene("TutorialStageScene_Backup");
        }
        else if (stageIndex == 1)
        {
            SceneManager.LoadScene("Stage1Scene");
        }
        else if (stageIndex == 1)
        {
            SceneManager.LoadScene("Stage2Scene");
        }
    }

   


    private void CheckHost() 
    {
        if (NetworkManager.Instance == null) return;


        if (NetworkManager.Instance.myPlayerIndex == 0)

        {

            Debug.Log("あなたはホストです。ステージ選択が可能です。");

        }

        else

        {

            Debug.Log("あなたはゲストです。ホストがステージを選ぶのを待っています。");

        }
    }
}