using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class KeyboardManager : MonoBehaviour
{
    
    [SerializeField] Volume globalVolume;
    [SerializeField] GameObject playCavas;
    [SerializeField] GameObject menuCanvas;
    private DepthOfField depthOfField;


    public InputField NameInputField;
    public InputField RoomInputField;
    public Text NameText;
    public Text RoomText;

    public Text KeyboardNameText;
    public Text KeyboardRoomText;

    
    public InputField Name;

    const int limit = 10;

    public GameObject Hiragana;
    public GameObject Katakana;

    [Header("キーボードのボタン設定")]
    [SerializeField] private int gridCols = 13; // 横13列
    [SerializeField] private int gridRows = 5;  // 縦5行
    private Button[,] keyGrid;

    private int currentX = 11; // 初期位置は「あ」の列 
    private int currentY = 0;  // 初期位置は1行目

    [SerializeField] private Transform rightFixedColumn;  // 左側の「あ/ア/a/A」が入っている列オブジェクト
    [SerializeField] private Transform leftFixedColumn;

    [Header("自動配置の設定")]
    [SerializeField] private Transform hiraganaPanel; // ひらがなのPanel
    [SerializeField] private Transform katakanaPanel; // カタカナのPanel

    [SerializeField] private RectTransform selectionCursor;

    public 

    void Start()
    {
        if (NameInputField != null)
        {
            StartCoroutine(RefocusInputField());
        }

        globalVolume.profile.TryGet(out depthOfField);
        if (depthOfField == null)
            Debug.LogError("DepthOfField is not found in the global volume");

        InitializeKeyboardGrid();
    }


    void Update()
    {
        NameCheck();
        RoomCheck();

        if (menuCanvas.activeSelf) // キーボードが開いている時だけ入力を受け付ける
        {
            HandleHardwareInput();
        }
    }


    private void InitializeKeyboardGrid()
    {
        // 縦5行 × 横12列 の配列を確保
        keyGrid = new Button[gridCols, gridRows];

        // 右側の固定キーを登録
        RegisterColumn(12, rightFixedColumn);

        // 現在アクティブな50音を中央に登録
        Transform currentCenterPanel = Hiragana.activeSelf ? hiraganaPanel : katakanaPanel;
        if (currentCenterPanel != null)
        {
            int colOffset = 11; // 50音は2列目からスタート
            foreach (Transform rowTransform in currentCenterPanel)
            {
                if (!rowTransform.gameObject.activeSelf) continue;

                RegisterColumn(colOffset, rowTransform);
                colOffset--;
            }
        }

        // 右側の固定キーを登録 
        RegisterColumn(0, leftFixedColumn);

        if (keyGrid[12, 3] != null)
        {
            keyGrid[12, 2] = keyGrid[12, 3]; // 空欄に決定ボタンをセット
            keyGrid[12, 4] = keyGrid[12, 3]; // 決定の下半分にもセット
        }

        currentX = 11;
        currentY = 0;
        UpdateCursorSelection();
    }

    // 1つの列の中にあるボタンを、指定したX座標に縦に並べる共通メソッドb
    private void RegisterColumn(int targetX, Transform columnTransform)
    {
        if (columnTransform == null) return;

        int rowIndex = 0;
        foreach (Transform buttonTransform in columnTransform)
        {
            Button btn = buttonTransform.GetComponent<Button>();
            if (btn != null)
            {

                if (targetX >= 0 && targetX < gridCols && rowIndex < gridRows)
                {
                    keyGrid[targetX, rowIndex] = btn;
                }
            }
            rowIndex++;
        }
    }

    // 十字キー・決定キーの入力監視
    private void HandleHardwareInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow)) MoveCursor(0, -1);
        if (Input.GetKeyDown(KeyCode.DownArrow)) MoveCursor(0, 1);
        if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveCursor(-1, 0);
        if (Input.GetKeyDown(KeyCode.RightArrow)) MoveCursor(1, 0);

        // 決定キー
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            Button currentButton = keyGrid[currentX, currentY];
            if (currentButton != null && currentButton.interactable)
            {
                currentButton.onClick.Invoke(); // ボタンのクリックイベントを発火
            }
        }
    }

    private void MoveCursor(int moveX, int moveY)
    {
        int startX = currentX;
        int startY = currentY;

        int nextX = currentX;
        int nextY = currentY;

        // ボタンがあるマス、または画面端に到達するまでループ
        while (true)
        {
            nextX += moveX;
            nextY += moveY;

            // 画面外に出そうならストップ
            if (nextX < 0 || nextX >= gridCols || nextY < 0 || nextY >= gridRows)
            {
                // 端で行き止まった場合は移動前の位置を維持、またはループさせる
                nextX = Mathf.Clamp(nextX, 0, gridCols - 1);
                nextY = Mathf.Clamp(nextY, 0, gridRows - 1);
                break;
            }

            // 移動先にボタンが存在すればそこでストップ
            if (keyGrid[nextX, nextY] != null)
            {
                break;
            }

            // 空欄（や行の空白など）なら、whileループでそのまま同じ方向にもう1マス進む
        }

        currentX = nextX;
        currentY = nextY;

        UpdateCursorSelection();
    }

    // 現在の選択ボタンを視覚的に選択状態にする 
    private void UpdateCursorSelection()
    {
        Button currentButton = keyGrid[currentX, currentY];

        if (currentButton != null)
        {
            // 1. Unity標準の選択状態にする（これでボタン自体の色変化なども共存可能）
            currentButton.Select();

            // 2. カーソル画像を選択されたボタンの場所に移動させる
            if (selectionCursor != null)
            {
                // アクティブ（表示）にする
                selectionCursor.gameObject.SetActive(true);

                // 【ロジックの肝】選択されたボタンの RectTransform を取得
                RectTransform buttonRect = currentButton.GetComponent<RectTransform>();

                // ボタンの世界座標（または親基準の座標）をカーソルにそのままコピーする
                selectionCursor.position = buttonRect.position;

                // （おまけ）もしボタンのサイズに合わせてカーソルのサイズも自動で変えたい場合
                // 決定ボタンなどの大きいボタンにも綺麗に枠がフィットするようになります
                selectionCursor.sizeDelta = buttonRect.sizeDelta;
            }
        }
        else
        {
            // もし選択された場所が null（空欄）なら、カーソルを非表示にする
            if (selectionCursor != null)
            {
                selectionCursor.gameObject.SetActive(false);
            }
        }
    }

    private void NameCheck()
    {
        if (NameInputField.text.Length > limit)
        {
            NameInputField.text = NameInputField.text[..10];

        }
        else
        {

            int leftNum = NameInputField.text.Length;

            NameText.text = leftNum.ToString() + "/10";
            KeyboardNameText.text = leftNum.ToString() + "/10";

        }
    }
    private void RoomCheck()
    {
        if (RoomInputField.text.Length > limit)
        {
            RoomInputField.text = RoomInputField.text[..10];

        }
        else
        {

            int leftNum = RoomInputField.text.Length;

            RoomText.text = leftNum.ToString() + "/10";
            KeyboardRoomText.text = leftNum.ToString() + "/10";


        }

    }
  


    public void InputCharacter(string character)
    {
        if (NameInputField == null) return;

        // 文字の追加・削除
        if (character == "Delete")
        {
            if (NameInputField.text.Length > 0)
            {
                NameInputField.text = NameInputField.text.Substring(0, NameInputField.text.Length - 1);
            }
        }
        else
        {
            NameInputField.text += character;
        }

        // ボタンが押された瞬間に、即座にフォーカスを奪い返す
        NameInputField.ActivateInputField();

        // さらに、念のため次のフレームでもフォーカスを固定する（2段構え）
        StartCoroutine(RefocusInputField());
    }

    private IEnumerator RefocusInputField()
    {
        // 2フレーム待ってUnityの選択処理が完全に落ち着くのを待つ
        yield return null;
        yield return null;

        if (NameInputField != null)
        {
            NameInputField.ActivateInputField();
            NameInputField.MoveTextEnd(false);
        }
    }

    public void InputTextEnter()
    {
        menuCanvas.SetActive(false);
        SwitchDepthOfField(false);

        Name.text = NameInputField.text;
    }

   



    public void OpenKeyboard()
    {
        if (menuCanvas)
        {

            menuCanvas.SetActive(true);
            SwitchDepthOfField(true);
        }
    }

    public void SwitchDepthOfField(bool _switch)
    {
        if (_switch)
        {
            depthOfField.active = true;
        }
        else
        {
            depthOfField.active = false;
        }
    }

    public void HiraganaCall()
    {
        Hiragana.SetActive(true);
        Katakana.SetActive(false);
        InitializeKeyboardGrid();
    }

    public void KatakanaCall()
    {
        Katakana.SetActive(true);
        Hiragana.SetActive(false);
        InitializeKeyboardGrid();
    }
}