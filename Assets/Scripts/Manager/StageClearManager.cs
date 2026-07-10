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

    [Header("クリア画面の星のUI設定")]
    [SerializeField] private Image clearStarImage;       // クリア画面中央にある星のImageコンポーネント
    [SerializeField] private Sprite obtainedStarSprite;   // 獲得した時の星の画像（黄色の星）
    [SerializeField] private Sprite missingStarSprite;    // 獲得できなかった時の星の画像（白またはグレーの星）

    [Header("各アクションの遷移先シーン名")]
    [SerializeField] private string stageSelectSceneName = "Title";

    private int currentSelectedIndex = 0;
    private const int TotalButtons = 3;

    [Header("ボタンの連続移動スピード（秒）")]
    [SerializeField] private float inputDelay = 0.2f;
    private float nextInputTime = 0f;

    [SerializeField] private GameObject GestPanel;

    private CommunicationUI controls;

    // マジックナンバー回避のためのキー定数
    private const string ItemIdPrefix = "Stage_";
    private const string CurrentStageKey = "CurrentStageIndex";

    void Awake()
    {
        controls = new CommunicationUI();
    }

    void OnEnable()
    {
        if (controls != null) controls.GameClear.Enable();
    }

    void OnDisable()
    {
        if (controls != null) controls.GameClear.Disable();
    }

    private void Start()
    {
        CheckHost();
        currentSelectedIndex = 0;
        ApplyButtonFocus();
        if (GestPanel != null) GestPanel.SetActive(false);

        // 星の表示切り替え
        UpdateClearStarDisplay();
    }

    /// <summary>
    /// PlayerPrefsから現在のステージ番号を取得し、SaveManagerから星の獲得状況を判定する
    /// </summary>
    private void UpdateClearStarDisplay()
    {
        if (clearStarImage == null || obtainedStarSprite == null || missingStarSprite == null)
        {
            Debug.LogWarning("【警告】星のUIまたはSpriteの参照がインスペクターで設定されていません。");
            return;
        }

        // 直前のステージが保存した「ステージ番号」を読み出す（デフォルトは0）
        int currentStageIndex = PlayerPrefs.GetInt(CurrentStageKey, 0);
        string targetItemId = ItemIdPrefix + currentStageIndex;

        // SaveManagerのメモリ上にこのステージの星が存在するかチェック
        if (SaveManager.Instance != null && SaveManager.Instance.HasItem(targetItemId))
        {
            clearStarImage.sprite = obtainedStarSprite;
            Debug.Log($"【クリアUI】ステージ {currentStageIndex} の {targetItemId} を獲得しているため、黄色の星を表示します。");
        }
        else
        {
            clearStarImage.sprite = missingStarSprite;
            Debug.Log($"【クリアUI】ステージ {currentStageIndex} の {targetItemId} は未獲得のため、白色の星を表示します。");
        }
    }

    private void Update()
    {
        if (IsHost())
        {
            if (controls.GameClear.Down.triggered)
            {
                currentSelectedIndex = (currentSelectedIndex + 1) % TotalButtons;
                ApplyButtonFocus();
            }
            else if (controls.GameClear.Up.triggered)
            {
                currentSelectedIndex = (currentSelectedIndex - 1 + TotalButtons) % TotalButtons;
                ApplyButtonFocus();
            }
            else if (controls.GameClear.Submit.triggered)
            {
                ExecuteCurrentSelectedButton();
            }
        }
        else if (!IsHost())
        {
            if (GestPanel != null) GestPanel.SetActive(true);
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
        int clearindex = 0;
        await SendStageClear(clearindex);
    }

    public async void OnBackToSelectPressed()
    {
        if (!IsHost()) return;
        int clearindex = 1;
        await SendStageClear(clearindex);
    }

    public async void OnRetryPressed()
    {
        if (!IsHost()) return;
        int clearindex = 2;
        await SendStageClear(clearindex);
    }

    private async Task SendStageClear(int clearindex)
    {
        if (NetworkManager.Instance == null) return;

        ClearSelectData msgData = new ClearSelectData();
        msgData.type = "clear_select";
        msgData.name_id = NetworkManager.Instance.myPlayerId;
        msgData.room_id = NetworkManager.Instance.myRoomID;
        msgData.index = NetworkManager.Instance.myPlayerIndex;
        msgData.IsStarted = false;
        msgData.select_index = clearindex;

        string jsonMsg = JsonUtility.ToJson(msgData);
        await NetworkManager.Instance.SendMessageAsync(jsonMsg);
    }

    public void HandleClearMessage(string msg)
    {
        var clearData = JsonUtility.FromJson<ClearSelectData>(msg);
        if (clearData == null) return;

        if (clearData.type == "clear_select")
        {
            LoadScene(clearData.select_index);
        }
    }

    void LoadScene(int clearIndex)
    {
        if (clearIndex == 0)
        {
            int nextSceneIndex = PlayerPrefs.GetInt("NextStageIndex", 3);

            if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextSceneIndex);
            }
            else
            {
                SceneManager.LoadScene(stageSelectSceneName);
            }
        }
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
        else Debug.Log("あなたはゲストです。");
    }
}