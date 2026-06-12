using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System;
using System.Threading.Tasks;

public class StageClearManager : MonoBehaviour
{
    [Header("ステージクリアの背景画像（Image）")]
    [SerializeField] private Image clearBackImage;

    [Header("並んでいる3つのボタン（上から順に登録）")]
    [SerializeField] private Button nextStageButton;     // ④ 次のステージ
    [SerializeField] private Button stageSelectButton;   // ⑤ ステージ選択に戻る
    [SerializeField] private Button retryButton;         // ⑥ リトライ

    [Header("各アクションの遷移先シーン名")]
    [SerializeField] private string nextStageSceneName = "Stage2";
    [SerializeField] private string stageSelectSceneName = "Title";

    private int currentSelectedIndex = 0;
    private const int TotalButtons = 3;

    [Header("ボタンの連続移動スピード（秒）")]
    [SerializeField] private float inputDelay = 0.2f;
    private float nextInputTime = 0f;

    private void Start()
    {
        CheckHost();
        currentSelectedIndex = 0;
        ApplyButtonFocus();
    }

    private void Update()
    {
        if (IsHost())
        {
            float v1 = 0f; float v2 = 0f; float vDefault = 0f;
            if (HasAxis("Vertical1")) v1 = Input.GetAxisRaw("Vertical1");
            if (HasAxis("Vertical2")) v2 = Input.GetAxisRaw("Vertical2");
            if (HasAxis("Vertical")) vDefault = Input.GetAxisRaw("Vertical");

            float finalVerticalInput = v1 + v2 + vDefault;

            if (Mathf.Abs(finalVerticalInput) > 0.5f)
            {
                if (Time.unscaledTime >= nextInputTime)
                {
                    if (finalVerticalInput < -0.5f)
                    {
                        currentSelectedIndex = (currentSelectedIndex + 1) % TotalButtons;
                        ApplyButtonFocus();
                    }
                    else if (finalVerticalInput > 0.5f)
                    {
                        currentSelectedIndex = (currentSelectedIndex - 1 + TotalButtons) % TotalButtons;
                        ApplyButtonFocus();
                    }
                    nextInputTime = Time.unscaledTime + inputDelay;
                }
            }
            else
            {
                nextInputTime = 0f;
            }

            if ((HasAxis("Fire1") && Input.GetButtonDown("Fire1")) ||
                (HasAxis("Fire2") && Input.GetButtonDown("Fire2")) ||
                (HasAxis("Submit") && Input.GetButtonDown("Submit")))
            {
                Debug.Log("【入力ログ】決定入力検知。ボタンを実行します。");
                ExecuteCurrentSelectedButton();
            }
        }
    }

    private bool HasAxis(string axisName)
    {
        try { Input.GetAxisRaw(axisName); return true; }
        catch (System.ArgumentException) { return false; }
    }

    private void ApplyButtonFocus()
    {
        if (EventSystem.current == null) return;
        switch (currentSelectedIndex)
        {
            case 0: if (nextStageButton != null) nextStageButton.Select(); break;
            case 1: if (stageSelectButton != null) stageSelectButton.Select(); break;
            case 2: if (retryButton != null) retryButton.Select(); break;
        }
    }

    private void ExecuteCurrentSelectedButton()
    {
        if (EventSystem.current == null) return;
        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
        if (currentSelected != null)
        {
            Button button = currentSelected.GetComponent<Button>();
            if (button != null && button.onClick != null)
            {
                Debug.Log($"【UIログ】ボタン「{currentSelected.name}」のOnClickを呼び出します。");
                button.onClick.Invoke();
            }
        }
    }

    public async void OnNextStagePressed()
    {
        if (!IsHost()) return;
        Debug.Log("【進行ログ】OnNextStagePressed が呼ばれました。");
        int clearindex = 0;
        await SendStageClear(clearindex);
    }

    public async void OnBackToSelectPressed()
    {
        if (!IsHost()) return;
        Debug.Log("【進行ログ】OnBackToSelectPressed が呼ばれました。");
        int clearindex = 1;
        await SendStageClear(clearindex);
    }

    public async void OnRetryPressed()
    {
        if (!IsHost()) return;
        Debug.Log("【進行ログ】OnRetryPressed が呼ばれました。");
        int clearindex = 2;
        await SendStageClear(clearindex);
    }

    private async Task SendStageClear(int clearindex)
    {
        if (NetworkManager.Instance == null)
        {
            Debug.LogError("【エラー】NetworkManager のインスタンスが存在しません！");
            return;
        }

        ClearSelectData msgData = new ClearSelectData();
        msgData.type = "clear_select";
        msgData.name_id = NetworkManager.Instance.myPlayerId;
        msgData.room_id = NetworkManager.Instance.myRoomID;
        msgData.index = NetworkManager.Instance.myPlayerIndex;
        msgData.IsStarted = false;
        msgData.select_index = clearindex;

        string jsonMsg = JsonUtility.ToJson(msgData);
        Debug.Log($"【通信ログ】サーバーへ送信要求を出します: {jsonMsg}");
        await NetworkManager.Instance.SendMessageAsync(jsonMsg);
    }

    public void HandleClearMessage(string msg)
    {
        Debug.Log($"【通信ログ】NetworkManagerからパケットが届きました: {msg}");
        var clearData = JsonUtility.FromJson<ClearSelectData>(msg);
        if (clearData == null) return;

        if (clearData.type == "clear_select")
        {
            Debug.Log($"【同期ログ】全員同時にシーン {clearData.select_index} へ遷移します。");
            LoadScene(clearData.select_index);
        }
    }

    void LoadScene(int clearIndex)
    {
        if (clearIndex == 0) SceneManager.LoadScene(nextStageSceneName);
        else if (clearIndex == 1) SceneManager.LoadScene(stageSelectSceneName);
        else if (clearIndex == 2)
        {
            string previousStageName = PlayerPrefs.GetString("RetrySceneName");
            SceneManager.LoadScene(previousStageName);
        }
    }

    private bool IsHost() => NetworkManager.Instance != null && NetworkManager.Instance.myPlayerIndex == 0;

    private void CheckHost()
    {
        if (NetworkManager.Instance == null) return;
        if (NetworkManager.Instance.myPlayerIndex == 0) Debug.Log("あなたはホストです。");
        else Debug.Log("あなたはゲストです。ホストがステージを選ぶのを待っています。");
    }
}