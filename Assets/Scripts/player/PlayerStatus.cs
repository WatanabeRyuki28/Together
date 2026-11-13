using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    public int char_index; // 0なら火、1なら氷など

    void Start()
    {
        // ゲーム開始時（生成された時）に、NetworkManagerから選んだキャラ番号をもらう
        if (NetworkManager.Instance != null)
        {
            char_index = NetworkManager.Instance.myRealSelectedChar;
            Debug.Log($"【PlayerStatus】自分のキャラ番号をセットしました: {char_index}");
        }
    }
}