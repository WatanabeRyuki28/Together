using UnityEngine;

public class VideoAreaTrigger : MonoBehaviour
{
    [Header("表示する操作説明パネル（動画を入れたUIオブジェクト）")]
    [SerializeField] private GameObject guidePanelObject;

    [Header("表示切り替えを行う『看板』オブジェクト")]
    [SerializeField] private GameObject kanbanObject;

    [Header("一度だけ表示するかどうか")]
    [SerializeField] private bool playOnlyOnce = true;

    private bool hasPlayed = false;

    private void Start()
    {
        // 起動時はパネルと看板を非表示にしておく
        if (guidePanelObject != null) guidePanelObject.SetActive(false);
        if (kanbanObject != null) kanbanObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // プレイヤーが触れたか判定
        var player = other.GetComponent<PlayerController>();
        if (player == null) return;

        // すでに表示済みで、一度きりの設定なら無視
        if (playOnlyOnce && hasPlayed) return;

        ShowGuide();
    }

    private void ShowGuide()
    {
        hasPlayed = true;

        // パネルを表示（SetActive(true) されることで VideoPlayer が自動再生されます）
        if (guidePanelObject != null)
        {
            guidePanelObject.SetActive(true);
        }

        // 看板を表示
        if (kanbanObject != null)
        {
            kanbanObject.SetActive(true);
        }
    }
}