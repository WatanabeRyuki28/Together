using System.Collections;
using System.Collections.Generic;
using TMP_Ruby;
using TMPro;
using UnityEngine;

public class TMPSample : MonoBehaviour
{
    public TextMeshProRuby m_contents;
    public TextMeshProRuby m_explanation;
    public TextMeshProRuby m_index;
  


    void Update()
    {
        m_contents.Text = "<r=でんきねずみ>電気鼠</r>";

    }


    private void OnGUI()
    {
        if (GUILayout.Button("TextMeshProRuby.Text で設定"))
        {
           
        }

        if (GUILayout.Button("TMP_Text.SetTextAndExpandRuby で設定"))
        {
          //  m_text.SetTextAndExpandRuby("<r=でんきねずみ>電気鼠</r>");
        }

        if (GUILayout.Button("タグ展開後のテキストを取得"))
        {
            // 電気鼠<space=-3.00em><voffset=1em>でんきねずみ</voffset>
            var text = TMProRubyUtil.GetExpandText("<r=でんきねずみ>電気鼠</r>");
            print(text);
        }
    }
}
