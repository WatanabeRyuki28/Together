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

    private Button[] uiButtons;

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

    [Header("カーソル設定")]
    [SerializeField] private RectTransform cursorImage;
    [Tooltip("カーソルの移動速度（大きいほどキレよく動き、1.0で完全同期）")]
    [SerializeField] private float cursorMoveSpeed = 0.4f;
    private Vector2 cursorTargetPosition;

    void Awake()
    {
        controls = new CommunicationUI();

        uiButtons = new Button[] { nextStageButton, stageSelectButton, retryButton };
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

        // 最初に対象ボタンの目標座標を計算する
        ApplyButtonFocus();

        // 最初だけは滑らかに移動せず、一瞬で初期位置にカーソルを合わせる
        if (cursorImage != null)
        {
            cursorImage.anchoredPosition = cursorTargetPosition;
        }

        if (GestPanel != null) GestPanel.SetActive(false);

        // 星の表示切り替え
        UpdateClearStarDisplay();
    }

    private void UpdateClearStarDisplay()
    {
        if (clearStarImage == null || obtainedStarSprite == null || missingStarSprite == null)
        {
            Debug.LogWarning("【警告】星のUIまたはSpriteの参照がインスペクターで設定されていません。");
            return;
        }

        int currentStageIndex = PlayerPrefs.GetInt(CurrentStageKey, 0);
        string targetItemId = ItemIdPrefix + currentStageIndex;

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
            // 時間ベースの入力遅延（チャタリング・2個飛ばし防止）
            if (Time.time >= nextInputTime)
            {
                if (controls.GameClear.Down.triggered)
                {
                    currentSelectedIndex = (currentSelectedIndex + 1) % TotalButtons;
                    ApplyButtonFocus();
                    nextInputTime = Time.time + inputDelay; // 次の入力可能時間までロック
                }
                else if (controls.GameClear.Up.triggered)
                {
                    currentSelectedIndex = (currentSelectedIndex - 1 + TotalButtons) % TotalButtons;
                    ApplyButtonFocus();
                    nextInputTime = Time.time + inputDelay; // 次の入力可能時間までロック
                }
            }

            // 決定はディレイに関係なくいつでも実行可能にする
            if (controls.GameClear.Submit.triggered)
            {
                ExecuteCurrentSelectedButton();
            }
        }
        else if (!IsHost())
        {
            if (GestPanel != null) GestPanel.SetActive(true);
        }

        UpdateCursorPosition();
    }


    private void ApplyButtonFocus()
    {
        if (uiButtons == null || uiButtons.Length == 0) return;

        Button targetButton = uiButtons[currentSelectedIndex];
        if (targetButton == null) return;

        // 【ここが重要】ワールド座標からの変換をやめ、親子関係のローカル座標で位置を計算する
        RectTransform buttonRect = targetButton.GetComponent<RectTransform>();
        if (buttonRect != null && cursorImage != null)
        {
            RectTransform cursorParent = cursorImage.parent as RectTransform;
            if (cursorParent != null)
            {
                // カーソルの親を基準にしたボタンの「ローカル座標」を正しく取得
                Vector3 buttonLocalPos = cursorParent.InverseTransformPoint(buttonRect.position);

                // X軸にオフセットを適用して目標位置を設定
                cursorTargetPosition = new Vector2(buttonLocalPos.x , buttonLocalPos.y);
            }
        }

        
    }

    private void ExecuteCurrentSelectedButton()
    {
        if (uiButtons == null || uiButtons.Length == 0) return;

        Button targetButton = uiButtons[currentSelectedIndex];
        if (targetButton != null && targetButton.interactable && targetButton.onClick != null)
        {
            Debug.Log($"【UIログ】ボタン「{targetButton.name}」のOnClickを呼び出します。");
            targetButton.onClick.Invoke();
        }
    }

    public async void OnNextStagePressed()
    {
        if (!IsHost()) return;

        // 送信する前に0.5秒待つ場合
        await Task.Delay(500);
        int clearindex = 0;

        await SendStageClear(clearindex);
    }

    public async void OnBackToSelectPressed()
    {
        if (!IsHost()) return;
        // 送信する前に0.5秒待つ場合
        await Task.Delay(500);
        int clearindex = 1;
        await SendStageClear(clearindex);
    }

    public async void OnRetryPressed()
    {
        if (!IsHost()) return;

        // 送信する前に0.5秒待つ場合
        await Task.Delay(500);
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

    private void UpdateCursorPosition()
    {
        if (cursorImage == null) return;

        // 速度をインスペクターからいじれるように cursorMoveSpeed に変更（デフォルト0.4f）
        cursorImage.anchoredPosition = Vector2.Lerp(
            cursorImage.anchoredPosition,
            cursorTargetPosition,
            cursorMoveSpeed
        );
    }
}