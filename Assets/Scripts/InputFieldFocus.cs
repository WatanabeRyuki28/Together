using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class InputFieldFocus : MonoBehaviour
{
    [Header("画面ぼやける用のも")]
    [SerializeField] Volume globalVolume;
    [Header("Canvas")]
    [SerializeField] GameObject playCavas;       // 通信時のUI 
    [SerializeField] GameObject menuCanvas;     // キーボード用のUI     
    private DepthOfField depthOfField;

    public InputField NameInputField;
    public InputField RoomInputField;
    private InputField currentTargetInputField;

    public Text NameText;
    public Text RoomText;

    public Text KeyboardNameText;
    public Text KeyboardRoomText;

    public InputField finalNameInputField;
    public InputField finalRoomInputField;

    const int limit = 10;

    public GameObject Hiragana;
    public GameObject Katakana;
    public GameObject englishS;
    public GameObject englishB;

    [Header("キーボードのボタン設定")]
    [SerializeField] private int gridCols = 13; // 横13列
    [SerializeField] private int gridRows = 5;  // 縦5行
    private Button[,] keyGrid;

    private int currentX = 11; // 初期位置は「あ」の列 
    private int currentY = 0;  // 初期位置は1行目

    [SerializeField] private Transform rightFixedColumn;
    [SerializeField] private Transform leftFixedColumn;

    [Header("自動配置の設定")]
    [SerializeField] private Transform hiraganaPanel;
    [SerializeField] private Transform katakanaPanel;
    [SerializeField] private Transform englishSmallPanel;
    [SerializeField] private Transform englishBigPanel;

    [SerializeField] private RectTransform selectionCursor;

    [Header("常に表示する入力バーの設定")]
    [SerializeField] private Text displayedNameText;
    [SerializeField] private Text displayedRoomText;
    [SerializeField] private RectTransform inputCaret;
    [SerializeField] private float blinkInterval = 0.5f;

    CommunicationUI controls;

    private bool isKeyboardInputReset = true;

    [Header("長押しの設定")]
    [SerializeField] private float holdDelay = 0.4f;
    [SerializeField] private float repeatRate = 0.12f;

    [SerializeField] private float charHoldDelay = 0.5f;
    [SerializeField] private float charRepeatRate = 0.1f;

    private float charHoldTimer = 0f;
    private bool isCharHolding = false;
    private bool wasSubmitPressedLastFrame = false;

    private Coroutine keyboardMoveCoroutine;
    private Vector2 currentKeyboardInput;

    private float moveTimer = 0f;
    private bool isKeyboardHolding = false;

    [SerializeField] private GameObject N;
    [SerializeField] private GameObject R;

    public GameObject ChangeLaugauge;


    [SerializeField] private GameObject lobbyPanel;


    void Start()
    {
        globalVolume.profile.TryGet(out depthOfField);
        if (depthOfField == null)
            Debug.LogError("DepthOfField is not found in the global volume");

        InitializeKeyboardGrid();
        StartCoroutine(BlinkCaret());

        currentX = 11;
        currentY = 0;
    }

    void Update()
    {
        NameCheck();
        RoomCheck();

        if (menuCanvas.activeSelf)
        {
            UpdateCaretPosition();


            if (controls != null && controls.Keyboard.Back.IsPressed())
            {
                InputTextEnter();
            }

        }
    }

    // 決定ボタンが押された瞬間の処理
    private void OnKeyboardSubmit(InputAction.CallbackContext context)
    {

        if (!context.performed) return;
        if (keyGrid == null) return;

        Button currentButton = keyGrid[currentX, currentY];
        if (currentButton == null || !currentButton.interactable) return;

        TriggerCurrentButtonOnClick(currentButton);

    }

    private void TriggerCurrentButtonOnClick(Button button)
    {
        if (button != null && button.onClick != null)
        {
            button.onClick.Invoke();
        }
    }

    private void OnKeyboardMove(InputAction.CallbackContext context)
    {
        if (this == null) return;

        currentKeyboardInput = context.ReadValue<Vector2>();

        if (keyboardMoveCoroutine == null)
        {
            keyboardMoveCoroutine = StartCoroutine(KeepKeyboardMovingRoutine());
        }
    }

    private void OnKeyboardMoveCancel(InputAction.CallbackContext context)
    {
        currentKeyboardInput = Vector2.zero;
        if (keyboardMoveCoroutine != null)
        {
            StopCoroutine(keyboardMoveCoroutine);
            keyboardMoveCoroutine = null;
        }
    }

   

    // 決定ボタンが離された瞬間の処理
    private void OnKeyboardSubmitCancel(InputAction.CallbackContext context)
    {
        wasSubmitPressedLastFrame = false;
        charHoldTimer = 0f;
    }
    private IEnumerator KeepKeyboardMovingRoutine()
    {
        // 最初の1回目の移動
        int moveX = 0;
        int moveY = 0;
        float kand = 0.3f;

        if (Mathf.Abs(currentKeyboardInput.x) > Mathf.Abs(currentKeyboardInput.y))
        {
            if (currentKeyboardInput.x > kand) moveX = 1;
            else if (currentKeyboardInput.x < -kand) moveX = -1;
        }
        else
        {
            if (currentKeyboardInput.y > kand) moveY = -1;
            else if (currentKeyboardInput.y < -kand) moveY = 1;
        }

        if (moveX != 0 || moveY != 0)
        {
            MoveCursor(moveX, moveY);
        }

        // 1回目に入力した後の「長押し判定」までのタメ（0.4秒待機）
        yield return new WaitForSeconds(holdDelay);

        // 以降、ボタンが離される（currentKeyboardInputがゼロになる）まで連続移動
        while (currentKeyboardInput != Vector2.zero)
        {
            if (!menuCanvas.activeSelf) yield break;

            moveX = 0;
            moveY = 0;

            if (Mathf.Abs(currentKeyboardInput.x) > Mathf.Abs(currentKeyboardInput.y))
            {
                if (currentKeyboardInput.x > kand) moveX = 1;
                else if (currentKeyboardInput.x < -kand) moveX = -1;
            }
            else
            {
                if (currentKeyboardInput.y > kand) moveY = -1;
                else if (currentKeyboardInput.y < -kand) moveY = 1;
            }

            if (moveX != 0 || moveY != 0)
            {
                MoveCursor(moveX, moveY);
            }

            // 連続移動の間隔（0.12秒待記）
            yield return new WaitForSeconds(repeatRate);
        }

        // 入力が完全になくなったらコルーチン参照をクリア
        keyboardMoveCoroutine = null;
    }

    private void InitializeKeyboardGrid()
    {
        keyGrid = new Button[gridCols, gridRows];

        if (rightFixedColumn != null) rightFixedColumn.gameObject.SetActive(true);
        if (leftFixedColumn != null) leftFixedColumn.gameObject.SetActive(true);

        RegisterColumn(12, rightFixedColumn);

        Transform currentCenterPanel = null;
        if (Hiragana != null && Hiragana.activeSelf) currentCenterPanel = hiraganaPanel;
        else if (Katakana != null && Katakana.activeSelf) currentCenterPanel = katakanaPanel;
        else if (englishS != null && englishS.activeSelf) currentCenterPanel = englishSmallPanel;
        else if (englishB != null && englishB.activeSelf) currentCenterPanel = englishBigPanel;

        if (currentCenterPanel != null)
        {
            int colOffset = 11;
            foreach (Transform rowTransform in currentCenterPanel)
            {
                if (!rowTransform.gameObject.activeSelf) continue;
                RegisterColumn(colOffset, rowTransform);
                colOffset--;
            }
        }

        RegisterColumn(0, leftFixedColumn);
        UpdateCursorSelection();
        UpdateTargetPanelVisibility();
    }

    private void RegisterColumn(int targetX, Transform columnTransform)
    {
        if (columnTransform == null) return;

        List<Button> buttonsInCol = new List<Button>();
        foreach (Transform buttonTransform in columnTransform)
        {
            Button btn = buttonTransform.GetComponent<Button>();
            if (btn != null) buttonsInCol.Add(btn);
        }

        if (buttonsInCol.Count == 0) return;

        for (int rowIndex = 0; rowIndex < gridRows; rowIndex++)
        {
            int buttonIndex = rowIndex;
            if (buttonIndex >= buttonsInCol.Count)
            {
                buttonIndex = buttonsInCol.Count - 1;
            }

            if (targetX >= 0 && targetX < gridCols)
            {
                keyGrid[targetX, rowIndex] = buttonsInCol[buttonIndex];
            }
        }
    }

   

    private void UpdateCaretPosition()
    {
        if (inputCaret == null) return;

        if (currentTargetInputField == NameInputField && displayedNameText != null)
        {
            float textWidthN = displayedNameText.preferredWidth;
            Vector3 caretPosN = displayedNameText.rectTransform.localPosition;
            caretPosN.x += textWidthN;
            inputCaret.localPosition = caretPosN;
        }
        else if (currentTargetInputField == RoomInputField && displayedRoomText != null)
        {
            float textWidthR = displayedRoomText.preferredWidth;
            Vector3 caretPosR = displayedRoomText.rectTransform.localPosition;
            caretPosR.x += textWidthR;
            inputCaret.localPosition = caretPosR;
        }
    }

    private IEnumerator BlinkCaret()
    {
        while (true)
        {
            if (inputCaret != null)
            {
                inputCaret.gameObject.SetActive(!inputCaret.gameObject.activeSelf);
            }
            yield return new WaitForSeconds(blinkInterval);
        }
    }

    private void MoveCursor(int moveX, int moveY)
    {
        Button startButton = keyGrid[currentX, currentY];

        int nextX = currentX;
        int nextY = currentY;

        while (true)
        {
            nextX += moveX;
            nextY += moveY;

            if (nextX < 0 || nextX >= gridCols || nextY < 0 || nextY >= gridRows)
            {
                nextX = Mathf.Clamp(nextX, 0, gridCols - 1);
                nextY = Mathf.Clamp(nextY, 0, gridRows - 1);
                break;
            }

            if (keyGrid[nextX, nextY] != null)
            {
                if (keyGrid[nextX, nextY] != startButton)
                {
                    break;
                }
            }
        }

        currentX = nextX;
        currentY = nextY;

        UpdateCursorSelection();
    }

    private void UpdateCursorSelection()
    {
        Button currentButton = keyGrid[currentX, currentY];

        if (currentButton != null)
        {
            currentButton.Select();

            if (selectionCursor != null)
            {
                selectionCursor.gameObject.SetActive(true);
                RectTransform buttonRect = currentButton.GetComponent<RectTransform>();
                selectionCursor.position = buttonRect.position;
                selectionCursor.sizeDelta = buttonRect.sizeDelta;
            }
        }
        else
        {
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
        if (currentTargetInputField == null) return;

        if (character == "Delete")
        {
            if (currentTargetInputField.text.Length > 0)
            {
                currentTargetInputField.text = currentTargetInputField.text.Substring(0, currentTargetInputField.text.Length - 1);
            }
        }
        else if (character == "Transform")
        {
            if (currentTargetInputField.text.Length > 0)
            {
                string currentText = currentTargetInputField.text;
                char lastChar = currentText[currentText.Length - 1];

                if (textTransformTable.ContainsKey(lastChar))
                {
                    string baseText = currentText.Substring(0, currentText.Length - 1);
                    currentTargetInputField.text = baseText + textTransformTable[lastChar];
                }
            }
        }
        else
        {
            if (currentTargetInputField.text.Length < limit)
            {
                currentTargetInputField.text += character;
            }
        }

        if (currentTargetInputField == NameInputField && displayedNameText != null)
        {
            displayedNameText.text = currentTargetInputField.text;
        }
        else if (currentTargetInputField == RoomInputField && displayedRoomText != null)
        {
            displayedRoomText.text = currentTargetInputField.text;
        }

        UpdateCursorSelection();
    }

    private readonly Dictionary<char, char> textTransformTable = new Dictionary<char, char>()
{
// --- ひらがな ---
// か行
{'か', 'が'}, {'が', 'か'}, // 「か」は小文字がないので通常と濁点を往復
{'き', 'ぎ'}, {'ぎ', 'き'}, {'く', 'ぐ'}, {'ぐ', 'く'},
{'け', 'げ'}, {'げ', 'け'}, {'こ', 'ご'}, {'ご', 'こ'},
// さ行・た行も同様に往復
{'さ', 'ざ'}, {'ざ', 'さ'}, {'し', 'じ'}, {'じ', 'し'}, {'す', 'ず'}, {'ず', 'す'}, {'せ', 'ぜ'}, {'ぜ', 'せ'}, {'そ', 'ぞ'}, {'ぞ', 'そ'},
{'た', 'だ'}, {'だ', 'た'}, {'ち', 'ぢ'}, {'ぢ', 'ち'}, {'つ', 'づ'}, {'づ', 'っ'}, {'っ', 'つ'}, // 「つ」は小文字（っ）も含めてサイクル
{'て', 'で'}, {'で', 'て'}, {'と', 'ど'}, {'ど', 'と'},
// は行
{'は', 'ば'}, {'ば', 'ぱ'}, {'ぱ', 'は'},
{'ひ', 'び'}, {'び', 'ぴ'}, {'ぴ', 'ひ'},
{'ふ', 'ぶ'}, {'ぶ', 'ぷ'}, {'ぷ', 'ふ'},
{'へ', 'べ'}, {'べ', 'ぺ'}, {'ぺ', 'へ'},
{'ほ', 'ぼ'}, {'ぼ', 'ぽ'}, {'ぽ', 'ほ'},
// 小文字がある文字
{'あ', 'ぁ'}, {'ぁ', 'あ'},
{'い', 'ぃ'}, {'ぃ', 'い'},
{'う', 'ぅ'}, {'ぅ', 'う'},
{'え', 'ぇ'}, {'ぇ', 'え'},
{'お', 'ぉ'}, {'ぉ', 'お'},
{'や', 'ゃ'}, {'ゃ', 'や'},
{'ゆ', 'ゅ'}, {'ゅ', 'ゆ'},
{'よ', 'ょ'}, {'ょ', 'よ'},
{'わ', 'ゎ'}, {'ゎ', 'わ'},

// --- カタカナ
{'カ', 'ガ'}, {'ガ', 'カ'},
{'キ', 'ギ'}, {'ギ', 'キ'}, {'ク', 'グ'}, {'グ', 'ク'},
{'ケ', 'ゲ'}, {'ゲ', 'ケ'}, {'コ', 'ゴ'}, {'ゴ', 'コ'},
{'サ', 'ザ'}, {'ザ', 'サ'},
{'シ', 'ジ'}, {'ジ', 'シ'}, {'ス', 'ズ'}, {'ズ', 'ス'},
{'セ', 'ゼ'}, {'ゼ', 'セ'}, {'ソ', 'ゾ'}, {'ゾ', 'ソ'},
{'タ', 'ダ'}, {'ダ', 'タ'},
{'チ', 'ヂ'}, {'ヂ', 'チ'}, {'ツ', 'ヅ'}, {'ヅ', 'ッ'},{'ッ', 'ツ'},
{'テ', 'デ'}, {'デ', 'テ'}, {'ト', 'ド'}, {'ド', 'ト'},
{'ハ', 'バ'}, {'バ', 'パ'}, {'パ', 'ハ'},
{'ヒ', 'ビ'}, {'ビ', 'ピ'}, {'ピ', 'ヒ'},
{'フ', 'ブ'}, {'ブ', 'プ'}, {'プ', 'フ'},
{'ヘ', 'ベ'}, {'ベ', 'ペ'}, {'ペ', 'ヘ'},
{'ホ', 'ボ'}, {'ボ', 'ポ'}, {'ポ', 'ホ'},
{'ア', 'ァ'}, {'ァ', 'ア'}, {'イ', 'ィ'}, {'ィ', 'イ'},
{'ウ', 'ゥ'}, {'ゥ', 'ウ'}, {'エ', 'ェ'}, {'ェ', 'エ'},
{'オ', 'ォ'}, {'ォ', 'オ'},
{'ヤ', 'ャ'}, {'ャ', 'ヤ'},
{'ユ', 'ュ'}, {'ュ', 'ユ'},
{'ヨ', 'ョ'}, {'ョ', 'ヨ'},
{'ワ', 'ヮ'}, {'ヮ', 'ワ'},

// 小文字から大文字へ
{'a', 'A'}, {'b', 'B'}, {'c', 'C'}, {'d', 'D'}, {'e', 'E'},
{'f', 'F'}, {'g', 'G'}, {'h', 'H'}, {'i', 'I'}, {'j', 'J'},
{'k', 'K'}, {'l', 'L'}, {'m', 'M'}, {'n', 'N'}, {'o', 'O'},
{'p', 'P'}, {'q', 'Q'}, {'r', 'R'}, {'s', 'S'}, {'t', 'T'},
{'u', 'U'}, {'v', 'V'}, {'w', 'W'}, {'x', 'X'}, {'y', 'Y'}, {'z', 'Z'},

// 大文字から小文字へ
{'A', 'a'}, {'B', 'b'}, {'C', 'c'}, {'D', 'd'}, {'E', 'e'},
{'F', 'f'}, {'G', 'g'}, {'H', 'h'}, {'I', 'i'}, {'J', 'j'},
{'K', 'k'}, {'L', 'l'}, {'M', 'm'}, {'N', 'n'}, {'O', 'o'},
{'P', 'p'}, {'Q', 'q'}, {'R', 'r'}, {'S', 's'}, {'T', 't'},
{'U', 'u'}, {'V', 'v'}, {'W', 'w'}, {'X', 'x'}, {'Y', 'y'}, {'Z', 'z'}

};


    public void InputTextEnter()
    {
        if (menuCanvas != null) menuCanvas.SetActive(false);
        SwitchDepthOfField(false);

        if (currentTargetInputField == NameInputField && finalNameInputField != null)
        {
            finalNameInputField.text = NameInputField.text;
            finalNameInputField.ForceLabelUpdate();
        }
        else if (currentTargetInputField == RoomInputField && finalRoomInputField != null)
        {
            finalRoomInputField.text = RoomInputField.text;
            finalRoomInputField.ForceLabelUpdate();
        }

        if (N != null) N.SetActive(false);
        if (R != null) R.SetActive(false);

        CleanupControls();

      
        
        currentKeyboardInput = Vector2.zero;
        if (keyboardMoveCoroutine != null)
        {
            StopCoroutine(keyboardMoveCoroutine);
            keyboardMoveCoroutine = null;
        }
    }
    private void CleanupControls()
    {
        if (controls != null)
        {
            // まずイベントを確実に解除
            controls.Keyboard.Move.started -= OnKeyboardMove;
            controls.Keyboard.Move.canceled -= OnKeyboardMoveCancel;
            controls.Keyboard.Submit.performed -= OnKeyboardSubmit;

            controls.Keyboard.Disable();

            // コルーチン側へ安全に変数を引き渡すために一時変数へ退避
            CommunicationUI oldControls = controls;
            controls = null;

            StartCoroutine(SafeEnableUI(oldControls));
        }
    }
    private IEnumerator SafeEnableUI(CommunicationUI targetControls)
    {
        // 1フレーム待つことで、キーボードを閉じた「Aボタン」の押しっぱなしが
        // 裏画面のボタンを勝手に押してしまうのを完全に防ぎます
        yield return null;

        if (targetControls != null)
        {
            targetControls.UI.Enable();
            Debug.Log("裏画面のUI（Yボタンなど）が正常に復活しました！");
        }
    }

    public void OpenNameKeyboard()
    {
        if (lobbyPanel != null && lobbyPanel.activeSelf) return;

        if (menuCanvas)
        {

      

            currentTargetInputField = NameInputField;

            currentX = 11;
            currentY = 0;

            //  先にパネルを「ひらがな」に切り替える
            Hiragana.SetActive(true);
            Katakana.SetActive(false);
            if (englishS != null) englishS.SetActive(false);
            if (englishB != null) englishB.SetActive(false);
            ChangeLaugauge.SetActive(true);

            // 最後にキーボードを開始
            KeyboardPadStart();
        }
    }

    public void OpenRoomKeyboard()
    {
        if (lobbyPanel != null && lobbyPanel.activeSelf) return;

        if (menuCanvas)
        {
            currentTargetInputField = RoomInputField;

            currentX = 11;
            currentY = 0;

            // 先にパネルを「アルファベット大文字」に切り替える
            Hiragana.SetActive(false);
            Katakana.SetActive(false);
            if (englishS != null) englishS.SetActive(false);
            if (englishB != null) englishB.SetActive(true);
            ChangeLaugauge.SetActive(false);

            // 最後にキーボードを開始
            KeyboardPadStart();
        }
    }

    public void KeyboardPadStart()
    {
        isKeyboardInputReset = true;
        menuCanvas.SetActive(true);

        

        InitializeKeyboardGrid();
        if (controls == null)
        {
            controls = new CommunicationUI();
        }

        controls.UI.Disable();
        controls.Keyboard.Enable();

        controls.UI.Disable();
        controls.Keyboard.Move.started += OnKeyboardMove;
        controls.Keyboard.Move.canceled += OnKeyboardMoveCancel;

        controls.Keyboard.Submit.performed += OnKeyboardSubmit;

        SwitchDepthOfField(true);
    }

    public void SwitchDepthOfField(bool _switch)
    {
        if (depthOfField == null) return;
        depthOfField.active = _switch;
    }

    public void HiraganaCall()
    {
        Hiragana.SetActive(true);
        Katakana.SetActive(false);
        if (englishS != null) englishS.SetActive(false);
        if (englishB != null) englishB.SetActive(false);
        InitializeKeyboardGrid();
    }

    public void KatakanaCall()
    {
        Katakana.SetActive(true);
        Hiragana.SetActive(false);
        if (englishS != null) englishS.SetActive(false);
        if (englishB != null) englishB.SetActive(false);
        InitializeKeyboardGrid();
    }
    public void EnglishSmallCall()
    {
        if (englishS == null) return;
        Hiragana.SetActive(false);
        Katakana.SetActive(false);
        englishS.SetActive(true);
        if (englishB != null) englishB.SetActive(false);
        InitializeKeyboardGrid();
    }

    public void EnglishBigCall()
    {
        if (englishB == null) return;
        Hiragana.SetActive(false);
        Katakana.SetActive(false);
        if (englishS != null) englishS.SetActive(false);
        englishB.SetActive(true);
        InitializeKeyboardGrid();
    }

    private void UpdateTargetPanelVisibility()
    {
        if (currentTargetInputField == NameInputField)
        {
            if (N != null) N.SetActive(true);
            if (R != null) R.SetActive(false);
        }
        else if (currentTargetInputField == RoomInputField)
        {
            if (R != null) R.SetActive(true);
            if (N != null) N.SetActive(false);
        }
    }
    private void OnDisable()
    {
        // オブジェクトが非アクティブになったら、コルーチンとイベントを安全に解放する
        if (keyboardMoveCoroutine != null)
        {
            StopCoroutine(keyboardMoveCoroutine);
            keyboardMoveCoroutine = null;
        }

        if (controls != null)
        {
            controls.Keyboard.Move.started -= OnKeyboardMove;
            controls.Keyboard.Move.canceled -= OnKeyboardMoveCancel;



            controls.Keyboard.Submit.performed  -= OnKeyboardSubmit;
            controls.Keyboard.Submit.started    -= OnKeyboardSubmitCancel;
            controls.Keyboard.Submit.canceled   -= OnKeyboardSubmitCancel;

            controls.Keyboard.Disable();
            controls = null;
        }
        currentKeyboardInput = Vector2.zero;
    }
    private void OnDestroy()
    {
        // 念のため OnDisable と同じ安全化処理を通す
        if (keyboardMoveCoroutine != null)
        {
            StopCoroutine(keyboardMoveCoroutine);
            keyboardMoveCoroutine = null;
        }

        if (controls != null)
        {
            controls.Keyboard.Move.started -= OnKeyboardMove;
            controls.Keyboard.Move.canceled -= OnKeyboardMoveCancel;

            controls.Keyboard.Submit.performed -= OnKeyboardSubmit;
            controls.Keyboard.Submit.started -= OnKeyboardSubmitCancel;
            controls.Keyboard.Submit.canceled -= OnKeyboardSubmitCancel;

            controls.Keyboard.Disable();
            controls = null;
        }
    }

    private void OnEnable()
    {
        // 画面が戻ってきた（アクティブになった）瞬間に、
        // 内部に記憶されている文字を、表示用テキストとInputFieldに強制的に再反映させる

        if (NameInputField != null)
        {
            // 1. 入力フィールド本体に文字を戻す
            NameInputField.ForceLabelUpdate();

            // 2. もし「常に表示する用」のTextを使っていれば、そこにも文字を戻す
            if (displayedNameText != null)
            {
                displayedNameText.text = NameInputField.text;
            }
        }

        if (RoomInputField != null)
        {
            // 1. 入力フィールド本体に文字を戻す
            RoomInputField.ForceLabelUpdate();

            // 2. もし「常に表示する用」のTextを使っていれば、そこにも文字を戻す
            if (displayedRoomText != null)
            {
                displayedRoomText.text = RoomInputField.text;
            }
        }

        // 文字数カウントの表示もここで強制更新（NameCheck / RoomCheckを呼ぶ）
        NameCheck();
        RoomCheck();
    }
}