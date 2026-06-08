using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

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

    // ★ 連続移動（スクロール）を快適にするためのタイマー変数
    [Header("ボタンの連続移動スピード（秒）")]
    [SerializeField] private float inputDelay = 0.2f; // 0.2秒ごとに次のボタンへ移動
    private float nextInputTime = 0f;

    private void Start()
    {
        currentSelectedIndex = 0;
        ApplyButtonFocus();
    }

    private void Update()
    {
        float v1 = 0f;
        float v2 = 0f;
        float vDefault = 0f;

        if (HasAxis("Vertical1")) v1 = Input.GetAxisRaw("Vertical1");
        if (HasAxis("Vertical2")) v2 = Input.GetAxisRaw("Vertical2");
        if (HasAxis("Vertical")) vDefault = Input.GetAxisRaw("Vertical");

        float finalVerticalInput = v1 + v2 + vDefault;

        // スティックが一定以上倒されているかチェック
        if (Mathf.Abs(finalVerticalInput) > 0.5f)
        {
            // ★【ここが快適化のキモ】
            // 現在の時間が、次に移動していい時間（nextInputTime）を過ぎていたら移動を許可する
            if (Time.unscaledTime >= nextInputTime)
            {
                if (finalVerticalInput < -0.5f)
                {
                    // 下に入力：インデックスを増やす
                    currentSelectedIndex = (currentSelectedIndex + 1) % TotalButtons;
                    ApplyButtonFocus();
                }
                else if (finalVerticalInput > 0.5f)
                {
                    // 上に入力：インデックスを減らす
                    currentSelectedIndex = (currentSelectedIndex - 1 + TotalButtons) % TotalButtons;
                    ApplyButtonFocus();
                }

                // 次に入力を受け付ける時間を「今の時間 + 0.2秒後」に設定する（押しっぱなしで連打になるのを防ぐ）
                nextInputTime = Time.unscaledTime + inputDelay;
            }
        }
        else
        {
            // スティックが真ん中に戻されたら、いつでも次の入力を受け付けられるようにタイマーを即リセット
            nextInputTime = 0f;
        }

        // 決定ボタンの判定
        if ((HasAxis("Fire1") && Input.GetButtonDown("Fire1")) ||
            (HasAxis("Fire2") && Input.GetButtonDown("Fire2")) ||
            (HasAxis("Submit") && Input.GetButtonDown("Submit")))
        {
            ExecuteCurrentSelectedButton();
        }
    }

    private bool HasAxis(string axisName)
    {
        try
        {
            Input.GetAxisRaw(axisName);
            return true;
        }
        catch (System.ArgumentException)
        {
            return false;
        }
    }

    private void ApplyButtonFocus()
    {
        if (EventSystem.current == null) return;

        switch (currentSelectedIndex)
        {
            case 0:
                if (nextStageButton != null) nextStageButton.Select();
                break;
            case 1:
                if (stageSelectButton != null) stageSelectButton.Select();
                break;
            case 2:
                if (retryButton != null) retryButton.Select();
                break;
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
                button.onClick.Invoke();
            }
        }
    }

    public void OnNextStagePressed()
    {
        SceneManager.LoadScene(nextStageSceneName);
    }

    public void OnBackToSelectPressed()
    {
        SceneManager.LoadScene(stageSelectSceneName);
    }

    public void OnRetryPressed()
    {
        if (PlayerPrefs.HasKey("RetrySceneName"))
        {
            string previousStageName = PlayerPrefs.GetString("RetrySceneName");
            Debug.Log($"[リトライ成功] 前のステージ {previousStageName} に戻ります。");
            SceneManager.LoadScene(previousStageName);
        }
        else
        {
            Debug.LogError("エラー：前のステージ名がセーブデータに存在しません。");
        }
    }
}