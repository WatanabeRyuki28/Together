using UnityEngine;

public class VideoAreaTrigger : MonoBehaviour
{
    [Header("表示する操作説明パネル")]
    [SerializeField] private GameObject guidePanelObject;

    [Header("表示or非表示")]
    [SerializeField] private bool isVideo = false;

    private bool hasPlayed = false;

    private string cha ="";

    [SerializeField] private LayerMask targetLayerFire;
    [SerializeField] private LayerMask targetLayerIce;  

    private void Start()
    {
        // 起動時はパネルを非表示
        if (guidePanelObject != null) guidePanelObject.SetActive(false);
    
    }

    void Update()
    {
#if UNITY_EDITOR

      /*  if (!isVideo)guidePanelObject.SetActive(false);
        if (isVideo) guidePanelObject.SetActive(true);*/


#endif
    }
    private void OnTriggerEnter2D(Collider2D other)
    {

        PlayerStatus playerStatus = other.GetComponent<PlayerStatus>();
        if (playerStatus == null) return; // プレイヤーでなければ何もしない

       
        Debug.Log("当たった");

        bool isCorrectPlayer = false;

        if (playerStatus.char_index == 0)
        {
            // 火のキャラの場合、相手のレイヤーが 「Player_Fire」 レイヤーに含まれているか判定
            if (((1 << other.gameObject.layer) & targetLayerFire) != 0)
            {
                isCorrectPlayer = true;
                Debug.Log("レイヤー判定一致: 炎プレイヤー");
            }
        }
        else
        {
            // 氷のキャラの場合、相手のレイヤーが 「Player_Ice」 レイヤーに含まれているか判定
            if (((1 << other.gameObject.layer) & targetLayerIce) != 0)
            {
                isCorrectPlayer = true;
                Debug.Log("レイヤー判定一致: 氷プレイヤー");
            }
        }

        // 正しいプレイヤーであればガイドを表示
        if (isCorrectPlayer)
        {
            // すでに表示済みなら何もしない
          

            ShowGuide();

            if (hasPlayed) return;
        }
        else
        {
            Debug.Log($"レイヤー不一致。相手のレイヤー番号: {other.gameObject.layer}");
        }

    }

    private void ShowGuide()
    {
        hasPlayed = true;

        // パネルを表示
        if (guidePanelObject != null) guidePanelObject.SetActive(true);
        
        
    }
}
