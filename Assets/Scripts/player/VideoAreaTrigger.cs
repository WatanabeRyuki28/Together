using UnityEngine;

public class VideoAreaTrigger : MonoBehaviour
{
    [Header("表示する操作説明パネル")]
    [SerializeField] private GameObject guidePanelObject;

    [Header("表示or非表示")]
    [SerializeField] private bool isVideo = false;

    private bool hasPlayed = false;

    private void Start()
    {
        // 起動時はパネルを非表示
        if (guidePanelObject != null) guidePanelObject.SetActive(false);
    
    }

    void Update()
    {
#if UNITY_EDITOR

        if (!isVideo)guidePanelObject.SetActive(false);
        if (isVideo) guidePanelObject.SetActive(true);


#endif
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        // プレイヤーが触れたか判定
        var player = other.GetComponent<PlayerController>();
        if (player == null) return;


        // すでに表示済み
        if (hasPlayed) return;

        ShowGuide();


    }

    private void ShowGuide()
    {
        hasPlayed = true;

        // パネルを表示
        if (guidePanelObject != null) guidePanelObject.SetActive(true);
        
        
    }
}
