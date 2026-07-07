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
    // [SerializeField] private string nextStageSceneName = "Stage2";
    [SerializeField] private string stageSelectSceneName = "Title";

    private int currentSelectedIndex = 0;
    private const int TotalButtons = 3;

    [Header("ボタンの連続移動スピード（秒）")]
    [SerializeField] private float inputDelay = 0.2f;
    private float nextInputTime = 0f;

    [SerializeField] private GameObject GestPanel;

    private CommunicationUI controls;

    void Awake()
    {
        // インスタンスの生成
        controls = new CommunicationUI();
    }

    void OnEnable()
    {
        // ステージ選択シーン用の操作マップ「StageSelect」を有効化
        if (controls != null)
        {
            controls.GameClear.Enable();
        }
    }

    void OnDisable()
    {
        // シーンを抜ける時は安全のために操作をオフにする
        if (controls != null)
        {
            controls.GameClear.Disable();
        }
    }

    private void Start()
    {
        CheckHost();
        currentSelectedIndex = 0;
        ApplyButtonFocus();
        GestPanel.SetActive(false);
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
            else if (controls.GameClear.Sumbit.triggered)
            {
                ExecuteCurrentSelectedButton();
            }

            #region 前の処理
            /*
            float v1 = 0f; float v2 = 0f; float vDefault = 0f;
            if (HasAxis("Vertical1")) v1 = Input.GetAxisRaw("Vertical1");
            if (HasAxis("Vertical2")) v2 = Input.GetAxisRaw("Vertical2");
            if (HasAxis("Vertical")) vDefault = Input.GetAxisRaw("Vertical");

             float finalVerticalInput = v1 + v2 + vDefault;


            if (Mathf.Abs(finalVerticalInput) > 0.5f)
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
                if (Time.unscaledTime >= nextInputTime)
                {

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
            }*/
            #endregion 

        }
        else if (!IsHost())
        {
            GestPanel.SetActive(true);
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
        if (clearIndex == 0)
        {
            int nextSceneIndex = PlayerPrefs.GetInt("NextStageIndex", 3);

            // 次のインデックスが、ビルド設定されている総シーン数より少なければロード
            if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                Debug.Log($"【シーン遷移】保存された次のステージ（BuildIndex: {nextSceneIndex}）をロードします。");
                SceneManager.LoadScene(nextSceneIndex);
            }
            else
            {
                Debug.LogWarning("【警告】すべてのステージをクリアしました！ステージ選択に戻ります。");
                SceneManager.LoadScene(stageSelectSceneName);
            }
        }
        else if (clearIndex == 1) SceneManager.LoadScene(stageSelectSceneName);
        else if (clearIndex == 2)
        {
            string previousStageName = PlayerPrefs.GetString("Retryusing UnityEngine;\r\nusing UnityEngine.UI;\r\nusing UnityEngine.SceneManagement;\r\nusing UnityEngine.EventSystems;\r\nusing System;\r\nusing System.Threading.Tasks;\r\nusing UnityEditor.ShaderGraph;\r\n\r\npublic class StageClearManager : MonoBehaviour\r\n{\r\n    [Header(\"ステージクリアの背景画像（Image）\")]\r\n    [SerializeField] private Image clearBackImage;\r\n\r\n    [Header(\"並んでいる3つのボタン（上から順に登録）\")]\r\n    [SerializeField] private Button nextStageButton;     // ④ 次のステージ\r\n    [SerializeField] private Button stageSelectButton;   // ⑤ ステージ選択に戻る\r\n    [SerializeField] private Button retryButton;         // ⑥ リトライ\r\n\r\n    [Header(\"各アクションの遷移先シーン名\")]\r\n    // [SerializeField] private string nextStageSceneName = \"Stage2\";\r\n    [SerializeField] private string stageSelectSceneName = \"Title\";\r\n\r\n    private int currentSelectedIndex = 0;\r\n    private const int TotalButtons = 3;\r\n\r\n    [Header(\"ボタンの連続移動スピード（秒）\")]\r\n    [SerializeField] private float inputDelay = 0.2f;\r\n    private float nextInputTime = 0f;\r\n\r\n\r\n    private CommunicationUI controls;\r\n\r\n    void Awake()\r\n    {\r\n        // インスタンスの生成\r\n        controls = new CommunicationUI();\r\n    }\r\n\r\n    void OnEnable()\r\n    {\r\n        // ステージ選択シーン用の操作マップ「StageSelect」を有効化\r\n        if (controls != null)\r\n        {\r\n            controls.GameClear.Enable();\r\n        }\r\n    }\r\n\r\n    void OnDisable()\r\n    {\r\n        // シーンを抜ける時は安全のために操作をオフにする\r\n        if (controls != null)\r\n        {\r\n            controls.GameClear.Disable();\r\n        }\r\n    }\r\n\r\n    private void Start()\r\n    {\r\n        CheckHost();\r\n        currentSelectedIndex = 0;\r\n        ApplyButtonFocus();\r\n    }\r\n\r\n    private void Update()\r\n    {\r\n        if (IsHost())\r\n        {\r\n            float v1 = 0f; float v2 = 0f; float vDefault = 0f;\r\n            if (HasAxis(\"Vertical1\")) v1 = Input.GetAxisRaw(\"Vertical1\");\r\n            if (HasAxis(\"Vertical2\")) v2 = Input.GetAxisRaw(\"Vertical2\");\r\n            if (HasAxis(\"Vertical\")) vDefault = Input.GetAxisRaw(\"Vertical\");\r\n\r\n            float finalVerticalInput = v1 + v2 + vDefault;\r\n\r\n            /*if (controls.GameClear.Up.triggered)\r\n            {\r\n            }\r\n            else if (controls.GameClear.Down.triggered)\r\n            {\r\n            }*/\r\n            if (Mathf.Abs(finalVerticalInput) > 0.5f)\r\n            {\r\n                if (Time.unscaledTime >= nextInputTime)\r\n                {\r\n                    if (controls.GameClear.Down.triggered)\r\n                    {\r\n                        currentSelectedIndex = (currentSelectedIndex + 1) % TotalButtons;\r\n                        ApplyButtonFocus();\r\n                    }\r\n                    else if (controls.GameClear.Up.triggered)\r\n                    {\r\n                        currentSelectedIndex = (currentSelectedIndex - 1 + TotalButtons) % TotalButtons;\r\n                        ApplyButtonFocus();\r\n                    }\r\n                    if (finalVerticalInput < -0.5f)\r\n                    {\r\n                        currentSelectedIndex = (currentSelectedIndex + 1) % TotalButtons;\r\n                        ApplyButtonFocus();\r\n                    }\r\n                    else if (finalVerticalInput > 0.5f)\r\n                    {\r\n                        currentSelectedIndex = (currentSelectedIndex - 1 + TotalButtons) % TotalButtons;\r\n                        ApplyButtonFocus();\r\n                    }\r\n                    nextInputTime = Time.unscaledTime + inputDelay;\r\n                }\r\n            }\r\n            else\r\n            {\r\n                nextInputTime = 0f;\r\n            }\r\n\r\n            if ((HasAxis(\"Fire1\") && Input.GetButtonDown(\"Fire1\")) ||\r\n                (HasAxis(\"Fire2\") && Input.GetButtonDown(\"Fire2\")) ||\r\n                (HasAxis(\"Submit\") && Input.GetButtonDown(\"Submit\")))\r\n            {\r\n                Debug.Log(\"【入力ログ】決定入力検知。ボタンを実行します。\");\r\n                ExecuteCurrentSelectedButton();\r\n            }\r\n        }\r\n    }\r\n\r\n    private bool HasAxis(string axisName)\r\n    {\r\n        try { Input.GetAxisRaw(axisName); return true; }\r\n        catch (System.ArgumentException) { return false; }\r\n    }\r\n\r\n    private void ApplyButtonFocus()\r\n    {\r\n        if (EventSystem.current == null) return;\r\n        switch (currentSelectedIndex)\r\n        {\r\n            case 0: if (nextStageButton != null) nextStageButton.Select(); break;\r\n            case 1: if (stageSelectButton != null) stageSelectButton.Select(); break;\r\n            case 2: if (retryButton != null) retryButton.Select(); break;\r\n        }\r\n    }\r\n\r\n    private void ExecuteCurrentSelectedButton()\r\n    {\r\n        if (EventSystem.current == null) return;\r\n        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;\r\n        if (currentSelected != null)\r\n        {\r\n            Button button = currentSelected.GetComponent<Button>();\r\n            if (button != null && button.onClick != null)\r\n            {\r\n                Debug.Log($\"【UIログ】ボタン「{currentSelected.name}」のOnClickを呼び出します。\");\r\n                button.onClick.Invoke();\r\n            }\r\n        }\r\n    }\r\n\r\n    public async void OnNextStagePressed()\r\n    {\r\n        if (!IsHost()) return;\r\n        Debug.Log(\"【進行ログ】OnNextStagePressed が呼ばれました。\");\r\n        int clearindex = 0;\r\n        await SendStageClear(clearindex);\r\n    }\r\n\r\n    public async void OnBackToSelectPressed()\r\n    {\r\n        if (!IsHost()) return;\r\n        Debug.Log(\"【進行ログ】OnBackToSelectPressed が呼ばれました。\");\r\n        int clearindex = 1;\r\n        await SendStageClear(clearindex);\r\n    }\r\n\r\n    public async void OnRetryPressed()\r\n    {\r\n        if (!IsHost()) return;\r\n        Debug.Log(\"【進行ログ】OnRetryPressed が呼ばれました。\");\r\n        int clearindex = 2;\r\n        await SendStageClear(clearindex);\r\n    }\r\n\r\n    private async Task SendStageClear(int clearindex)\r\n    {\r\n        if (NetworkManager.Instance == null)\r\n        {\r\n            Debug.LogError(\"【エラー】NetworkManager のインスタンスが存在しません！\");\r\n            return;\r\n        }\r\n\r\n        ClearSelectData msgData = new ClearSelectData();\r\n        msgData.type = \"clear_select\";\r\n        msgData.name_id = NetworkManager.Instance.myPlayerId;\r\n        msgData.room_id = NetworkManager.Instance.myRoomID;\r\n        msgData.index = NetworkManager.Instance.myPlayerIndex;\r\n        msgData.IsStarted = false;\r\n        msgData.select_index = clearindex;\r\n\r\n        string jsonMsg = JsonUtility.ToJson(msgData);\r\n        Debug.Log($\"【通信ログ】サーバーへ送信要求を出します: {jsonMsg}\");\r\n        await NetworkManager.Instance.SendMessageAsync(jsonMsg);\r\n    }\r\n\r\n    public void HandleClearMessage(string msg)\r\n    {\r\n        Debug.Log($\"【通信ログ】NetworkManagerからパケットが届きました: {msg}\");\r\n        var clearData = JsonUtility.FromJson<ClearSelectData>(msg);\r\n        if (clearData == null) return;\r\n\r\n        if (clearData.type == \"clear_select\")\r\n        {\r\n            Debug.Log($\"【同期ログ】全員同時にシーン {clearData.select_index} へ遷移します。\");\r\n            LoadScene(clearData.select_index);\r\n        }\r\n    }\r\n\r\n    void LoadScene(int clearIndex)\r\n    {\r\n        if (clearIndex == 0)\r\n        {\r\n            int nextSceneIndex = PlayerPrefs.GetInt(\"NextStageIndex\", 3);\r\n\r\n            // 次のインデックスが、ビルド設定されている総シーン数より少なければロード\r\n            if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)\r\n            {\r\n                Debug.Log($\"【シーン遷移】保存された次のステージ（BuildIndex: {nextSceneIndex}）をロードします。\");\r\n                SceneManager.LoadScene(nextSceneIndex);\r\n            }\r\n            else\r\n            {\r\n                Debug.LogWarning(\"【警告】すべてのステージをクリアしました！ステージ選択に戻ります。\");\r\n                SceneManager.LoadScene(stageSelectSceneName);\r\n            }\r\n        }\r\n        else if (clearIndex == 1) SceneManager.LoadScene(stageSelectSceneName);\r\n        else if (clearIndex == 2)\r\n        {\r\n            string previousStageName = PlayerPrefs.GetString(\"RetrySceneName\");\r\n            SceneManager.LoadScene(previousStageName);\r\n        }\r\n    }\r\n\r\n    private bool IsHost() => NetworkManager.Instance != null && NetworkManager.Instance.myPlayerIndex == 0;\r\n\r\n    private void CheckHost()\r\n    {\r\n        if (NetworkManager.Instance == null) return;\r\n        if (NetworkManager.Instance.myPlayerIndex == 0) Debug.Log(\"あなたはホストです。\");\r\n        else Debug.Log(\"あなたはゲストです。ホストがステージを選ぶのを待っています。\");\r\n    }\r\n}SceneName");
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