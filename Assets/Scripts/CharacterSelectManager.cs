using System;
using System.Security.Cryptography;
using System.Xml.Linq;
using Unity.Properties;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterSelectManager : MonoBehaviour
{
    [Header("キャラのアイコン（選択肢）")]
    public RectTransform[] characterIcons;

    [Header("自分の選択枠（カーソル画像）")]
    public RectTransform selectionCursor;

    [Header("相手の選択枠（カーソル画像）")]
    public RectTransform remoteSelectionCursor;


    [Header("決定時の専用画像")]
    [SerializeField] private Sprite confirmedSelectionSprite; // 自分の決定時専用画像
    [SerializeField] private Sprite confirmedRemoteSprite;

    private Sprite defaultSelectionSprite;
    private Sprite defaultRemoteSprite;

    private int currentSelectIndex = 0; // 現在選んでいるキャラの番号 

    private int mySelectedChar = -1;     // 自分が確定したキャラ番号 
    private int remoteSelectedChar = -1; // 相手が確定したキャラ番号

    private bool isMySelectionConfirmed = false; // 自分が既に決定ボタンを押したかどうか

    public Text myInfoText;
    public Text otherInfoText;

    private string remoteplayer;

    private bool isInputPressed = false;

    [SerializeField] private CommunicationUI controls;

    void Awake()
    {
        // インスタンスの生成
        controls = new CommunicationUI();
    }

    void OnEnable()
    {
        if (controls != null)
        {
            controls.CharSelect.Enable();

            // ボタンが押された瞬間のイベントを登録する
            controls.CharSelect.Right.started += OnRightPressed;
            controls.CharSelect.Left.started += OnLeftPressed;
            controls.CharSelect.Submit.started += OnSubmitPressed;
        }
    }

    void OnDisable()
    {
        if (controls != null)
        {
            controls.CharSelect.Enable();

            // ボタンが押された瞬間のイベントを登録する
            controls.CharSelect.Right.started += OnRightPressed;
            controls.CharSelect.Left.started += OnLeftPressed;
            controls.CharSelect.Submit.started += OnSubmitPressed;
        }
    }

    private void OnRightPressed(InputAction.CallbackContext context)
    {
        if (isMySelectionConfirmed) return;

        currentSelectIndex++;
        if (currentSelectIndex >= characterIcons.Length) currentSelectIndex = 0;

        UpdateCursorPosition();
        SendCharacterState(currentSelectIndex, false);
    }

    // 左ボタンが1回カチッと押された瞬間に走る処理
    private void OnLeftPressed(InputAction.CallbackContext context)
    {
        if (isMySelectionConfirmed) return;

        currentSelectIndex--;
        if (currentSelectIndex < 0) currentSelectIndex = characterIcons.Length - 1;

        UpdateCursorPosition();
        SendCharacterState(currentSelectIndex, false);
    }

    //決定（Submit）ボタンが1回カチッと押された瞬間に走る処理
    private void OnSubmitPressed(InputAction.CallbackContext context)
    {
        SelectCharacter(currentSelectIndex);
    }
    void Start()
    {

        if (selectionCursor != null && selectionCursor.GetComponent<Image>() != null)
        {
            defaultSelectionSprite = selectionCursor.GetComponent<Image>().sprite;
        }
        if (remoteSelectionCursor != null && remoteSelectionCursor.GetComponent<Image>() != null)
        {
            defaultRemoteSprite = remoteSelectionCursor.GetComponent<Image>().sprite;
        }

        int myIndex = 0;
        var myName = "";

        // 1Pは 0番のキャラ、2Pは 1番のキャラを初期位置にする
        if (NetworkManager.Instance != null)
        {
            myIndex = NetworkManager.Instance.myPlayerIndex;
            myName = NetworkManager.Instance.myPlayerId;

           
           /* // 自分の番号を表示
            if (myInfoText != null && otherInfoText != null)
            {
                if (myIndex == 0)
                {
                    myInfoText.text = myName;
                    otherInfoText.text = "";
                }
                else if (myIndex == 1)
                {
                    myInfoText.text = "";
                    otherInfoText.text = myName;
                }           
                    


            }
            */
            if (myIndex == 1)
            {

                RectTransform temp = selectionCursor;
                selectionCursor = remoteSelectionCursor;
                remoteSelectionCursor = temp;

                Sprite tempDefault = defaultSelectionSprite;
                defaultSelectionSprite = defaultRemoteSprite;
                defaultRemoteSprite = tempDefault;

                Sprite tempConfirmed = confirmedSelectionSprite;
                confirmedSelectionSprite = confirmedRemoteSprite;
                confirmedRemoteSprite = tempConfirmed;


                // 2Pの初期位置を1番にする
                currentSelectIndex = 1;
            }
            else
            {
                // 1Pの初期位置は0番
                currentSelectIndex = 0;
            }



        }
     

        UpdateCursorPosition();

        //  初期状態から相手のカーソルを見せるために最初からTrueにする、またはSetActiveを制御
        if (remoteSelectionCursor != null)
        {
            remoteSelectionCursor.gameObject.SetActive(true); // 常に表示

            int remoteDefault = (myIndex == 0) ? 1 : 0;
            float remoteStartX = 0f;
            float remoteStartY = 0f;

            if (myIndex == 0) // 自分が1P ➔ 相手は2P
            {
                // 相手（2P）の初期位置は1番なので 611
                remoteStartX = (remoteDefault == 0) ? -364f : 636f;
               
            }
            else // 自分が2P ➔ 相手は1P
            {
                // 相手（1P）の初期位置は0番なので -611
                remoteStartX = (remoteDefault == 0) ? -636f : 364f;
               
            }

            remoteSelectionCursor.anchoredPosition = new Vector2(remoteStartX,290);
        }

        SendCharacterState(currentSelectIndex, false);
    }

    void Update()
    {
    
    }

    void UpdateCursorPosition()
    {
        if (selectionCursor == null) return;

        int myIndex = NetworkManager.Instance != null ? NetworkManager.Instance.myPlayerIndex : 0;

        float targetX = 0f;
        float targetY = 0f;

        if (myIndex == 0) // 1P（ホスト）の場合
        {
            // 0番（ほのお）なら -611、1番（こおり）なら 389
            targetX = (currentSelectIndex == 0) ? -636f : 364f;
        }
        else // 2P（ゲスト）の場合
        {
            // 0番（ほのお）なら -389、1番（こおり）なら 611
            targetX = (currentSelectIndex == 0) ? -364f : 636f;

        }

        // 決定した座標をローカル座標（anchoredPosition）としてセットする
        selectionCursor.anchoredPosition = new Vector2(targetX,290);
    }

    // データを送信する共通の関数を作りました
    //  これなら確実に動く！
    async void SendCharacterState(int index, bool isReady)
    {
        if (NetworkManager.Instance == null) return;

        CharSelectData msgData = new CharSelectData();

        // 親クラス（InitResponse）から受け継いだ大事な変数もすべて確実にセット！
        msgData.type = "char_select";
        msgData.name_id = NetworkManager.Instance.myPlayerId;   
        msgData.room_id = NetworkManager.Instance.myRoomID;     
        msgData.index = NetworkManager.Instance.myPlayerIndex;   
        msgData.IsStarted = false;

        // 子クラス（CharSelectData）の固有メンバー
        msgData.char_index = index;
        msgData.is_ready = isReady;

        Debug.Log($"'{msgData.char_index}'");
        string jsonMsg = JsonUtility.ToJson(msgData);
        await NetworkManager.Instance.SendMessageAsync(jsonMsg);
    }

    // キャラクターが確定した時の処理
    void SelectCharacter(int index)
    {
        if (isMySelectionConfirmed) return;

        Debug.Log($"【自分】キャラ {index} 番で決定！サーバーに送信します。");

        mySelectedChar = index;
        isMySelectionConfirmed = true;

        if (selectionCursor != null && selectionCursor.GetComponent<Image>() != null)
        {
            selectionCursor.GetComponent<Image>().sprite = confirmedSelectionSprite;
        }

        //  決定フラグを true にして送信
        SendCharacterState(index, true);

        CheckBothPlayersReady();

        if (NetworkManager.Instance != null)
        {
            NetworkManager.Instance.myRealSelectedChar = index;
        }
    }

    // NetworkManagerからデータを受け取る部分
    public void HandleRemoteMessage(string msg)
    {
        var playerData = JsonUtility.FromJson<CharSelectData>(msg);
        if (playerData == null) return; Debug.Log("何も入ってないよ");

        if (playerData.type == "char_select")
        {
            Debug.Log("typeはあってる");
            // 相手からのデータの場合のみ処理
            if (playerData.name_id != NetworkManager.Instance.myPlayerId)
            {
                //  相手が「動かしただけ」でも「決定した」でも、カーソル位置はリアルタイムに同期する
                if (remoteSelectionCursor != null && characterIcons.Length > playerData.char_index)
                {
                    remoteSelectionCursor.gameObject.SetActive(true);
                    float remoteX = 0f;
                  

                    // 相手のインデックス（1Pか2Pか）で判定
                    if (playerData.index == 0) // 相手が1Pの場合
                    {
                        remoteX = (playerData.char_index == 0) ? -636f :364f;
                    }
                    else // 相手が2Pの場合
                    {
                        remoteX = (playerData.char_index == 0) ? -364f : 636f;
                      
                    }

                    remoteSelectionCursor.anchoredPosition = new Vector2(remoteX, 290f);

                    remoteplayer = playerData.name_id;

                    NetworkManager.Instance.myCharaIndex = playerData.char_index;

                    int myIndex = NetworkManager.Instance.myPlayerIndex;
/*
                    if (myIndex == 0)
                    {
                       
                        otherInfoText.text = remoteplayer;
                    }
                    else if (myIndex == 1)
                    {
                        myInfoText.text = remoteplayer;
                      
                    }*/
                }

                //  相手が「決定（is_ready == true）」した時だけ、選択番号を確定させる
                if (playerData.is_ready)
                {
                    remoteSelectedChar = playerData.char_index;
                    Debug.Log($"【同期】相手がキャラ {remoteSelectedChar} 番で決定しました。");
                    CheckBothPlayersReady();

                    if (remoteSelectionCursor != null && remoteSelectionCursor.GetComponent<Image>() != null)
                    {
                        remoteSelectionCursor.GetComponent<Image>().sprite = confirmedRemoteSprite;
                    }
                }
                else
                {
                    if (remoteSelectionCursor != null && remoteSelectionCursor.GetComponent<Image>() != null)
                    {
                        remoteSelectionCursor.GetComponent<Image>().sprite = defaultRemoteSprite;
                    }
                }
            }
        }

        else
        {
           
            Debug.Log($"typeが違うよ。期待値: char_select ➔ 実際の値: '{playerData.type}'");
        }
    }

    private void CheckBothPlayersReady()
    {
        if (mySelectedChar != -1 && remoteSelectedChar != -1)
        {
            if (mySelectedChar != remoteSelectedChar)
            {
                SceneManager.LoadScene("StageSelectScene");
            }
            else
            {
                Debug.LogWarning("キャラが重複しています！選び直してください。");
                isMySelectionConfirmed = false;
                mySelectedChar = -1;

                if (selectionCursor != null && selectionCursor.GetComponent<Image>() != null)
                    selectionCursor.GetComponent<Image>().sprite = defaultSelectionSprite;

                if (remoteSelectionCursor != null && remoteSelectionCursor.GetComponent<Image>() != null)
                    remoteSelectionCursor.GetComponent<Image>().sprite = defaultRemoteSprite;

                //  被ったので、相手側にも自分が「未確定（移動中）」に戻ったことを通知する
                SendCharacterState(currentSelectIndex, false);
            }
        }
    }
}