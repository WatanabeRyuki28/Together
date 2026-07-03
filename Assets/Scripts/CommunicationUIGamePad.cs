using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CommunicationUIGamePad : MonoBehaviour
{
    CommunicationUI controls;


    [SerializeField] private GameObject[] uiButtons; // 操作したいボタンの配列
    private int currentSelectedIndex = 0;           // 現在選択されているボタンの番号

    private Vector2 moveInput;
    private bool isInputReset = true; // スティックが中央に戻ったかどうかの判定フラグ

    [SerializeField] private RectTransform cursorImage;

    [SerializeField] private float cursorOffsetOfX = -100f;

    [Header("長押しの設定")]
    [SerializeField] private float holdDelay = 0.4f;    // 連続移動が始まるまでの時間（秒）
    [SerializeField] private float repeatRate = 0.15f;  // 連続移動する間隔（秒）

    private Coroutine movementCoroutine;


    private Vector2 currentInput;
    private float moveTimer = 0f;    // 長押し時間を計るタイマー
    private bool isHolding = false;  // 絶賛長押し中かどうかのフラグ

    [Header("干渉防止用")]
    [SerializeField] private GameObject keyboardMenuCanvas;


    private void OnEnable()
    {
        // 通信画面が開いた時だけ、新しくcontrolsを作ってUIマップだけを起動する
        controls = new CommunicationUI();
        controls.UI.Enable();

        // 決定イベントを登録
        controls.UI.Submit.started += OnSubmit;

        UpdateCursorPosition();
        isHolding = false;
        moveTimer = 0f;
    }

    private void OnDisable()
    {
        // 通信画面が閉じる時は、完全にすべてをシャットダウンしてクリアする
        if (controls != null)
        {
            controls.UI.Submit.started -= OnSubmit;
            controls.UI.Disable();
            controls = null; // 無効化
        }

        isHolding = false;
        moveTimer = 0f;
        currentInput = Vector2.zero;
    }
    private void OnMoveInput(InputAction.CallbackContext context)
    {
        if (uiButtons.Length == 0) return;

        Vector2 moveInput = context.ReadValue<Vector2>();

        Debug.Log($"【入力検知】 X: {currentInput.x} / Y: {currentInput.y}");

        // 上下の入力を判定 (UIシステムは上がプラス、リストのインデックスは下がプラスなので反転)
        if (moveInput.y > 0.4f)
        {
            ChangeSelection(-1); // 上へ
        }
        else if (moveInput.y < -0.4f)
        {
            ChangeSelection(1);  // 下へ
        }
    }


    private void OnMoveCancel(InputAction.CallbackContext context)
    {
        StopMovement();
    }

    private void StopMovement()
    {
        currentInput = Vector2.zero;
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }
    }

    private void Update()
    {
        if (uiButtons.Length == 0 || !gameObject.activeInHierarchy || controls == null) return;

       
        if (keyboardMenuCanvas != null && keyboardMenuCanvas.activeSelf)
        {
            isHolding = false;
            moveTimer = 0f;
            return;
        }

        // これより下は通常時の処理
        currentInput = controls.UI.Move.ReadValue<Vector2>();

        int moveY = 0;
        if (currentInput.y > 0.3f) moveY = -1;
        else if (currentInput.y < -0.3f) moveY = 1;

        if (moveY != 0)
        {
            if (!isHolding)
            {
                ChangeSelection(moveY);
                isHolding = true;
                moveTimer = holdDelay;
            }
            else
            {
                moveTimer -= Time.deltaTime;
                if (moveTimer <= 0f)
                {
                    ChangeSelection(moveY);
                    moveTimer = repeatRate;
                }
            }
        }
        else
        {
            isHolding = false;
            moveTimer = 0f;
        }
    }

    // 決定ボタンが押されたとき
    public void OnSubmit(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            Debug.Log($"{uiButtons[currentSelectedIndex].name} が決定されました！");
            // ここに選択中のボタンの実行処理を書く
        }

        if (uiButtons.Length == 0 || uiButtons[currentSelectedIndex] == null) return;

        // 現在カーソルが合っているボタンの「Button」コンポーネントを取得
        Button targetButton = uiButtons[currentSelectedIndex].GetComponent<Button>();

        if (targetButton != null)
        {
            // 該当するボタンの「On Click()」に登録されている処理を、コードから実行する！
            targetButton.onClick.Invoke();
            Debug.Log($"{uiButtons[currentSelectedIndex].name} を決定ボタンで実行しました！");
        }
    }

    // 選択を切り替えるメソッド
    private void ChangeSelection(int direction)
    {
        currentSelectedIndex += direction;
        if (currentSelectedIndex < 0) currentSelectedIndex = uiButtons.Length - 1;
        if (currentSelectedIndex >= uiButtons.Length) currentSelectedIndex = 0;

        //  選択が切り替わったらカーソルの位置を更新する
        UpdateCursorPosition();
    }

  

    private void UpdateCursorPosition()
    {
        if (uiButtons.Length == 0 || uiButtons[currentSelectedIndex] == null || cursorImage == null) return;

        // 選択中のボタンの RectTransform を取得
        RectTransform buttonRect = uiButtons[currentSelectedIndex].GetComponent<RectTransform>();

        if (buttonRect != null)
        {
            // ボタンの現在位置（ローカル座標）を取得
            Vector3 targetPosition = buttonRect.anchoredPosition;

            // カーソルをボタンの少し左側（X軸をマイナス）にずらして配置する
            targetPosition.x += cursorOffsetOfX;

            // カーソルの位置を決定
            cursorImage.anchoredPosition = targetPosition;
        }

        Debug.Log($"現在選択中: {uiButtons[currentSelectedIndex].name}");
    }
}
