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
    }


    void Update()
    {
        NameCheck();
        RoomCheck();
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
    }

    public void KatakanaCall()
    {
        Katakana.SetActive(true);
        Hiragana.SetActive(false);
    }
}