using UnityEngine;
using UnityEngine.UI;

public class LimitCheck : MonoBehaviour
{
  
    public InputField NameInputField;
    public InputField RoomInputField;
    public Text NameText;
    public Text RoomText;

    int limit = 10;

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

        }
    }
}