using UnityEngine;
using UnityEngine.Video;

public class VideoAreaTrigger : MonoBehaviour
{
    [Header("表示する操作説明パネル（動画を入れた親オブジェクト）")]
    [SerializeField] private GameObject guidePanelObject;

    [Header("VideoPlayerコンポーネント")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("表示したいコントローラー画像（看板付き）のプレハブ")]
    [SerializeField] private GameObject controllerImagePrefab;

    [Header("一度だけ再生するかどうか")]
    [SerializeField] private bool playOnlyOnce = true;

    private bool hasPlayed = false;
    private GameObject spawnedControllerInstance = null; // 生成したプレハブを保持する変数

    private void Start()
    {
        // 起動時はパネルを非表示、動画を停止しておく
        if (guidePanelObject != null) guidePanelObject.SetActive(false);
        if (videoPlayer != null) videoPlayer.Stop();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // プレイヤーが触れたか判定
        var player = other.GetComponent<PlayerController>();
        if (player == null) return;

        // すでに再生済みで、一度きりの設定なら無視
        if (playOnlyOnce && hasPlayed) return;

        PlayVideo();
    }

    private void PlayVideo()
    {
        hasPlayed = true;

        if (guidePanelObject != null && videoPlayer != null)
        {
            // パネルを表示
            guidePanelObject.SetActive(true);

            // 看板付きコントローラー画像のプレハブを生成し、パネルの子要素にする
            if (controllerImagePrefab != null)
            {
                spawnedControllerInstance = Instantiate(controllerImagePrefab, guidePanelObject.transform);

                // 位置の微調整（動画の下に表示されるように座標を調整する）
                RectTransform rectTransform = spawnedControllerInstance.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = new Vector2(0f, -250f); // 好みの高さに調整してください
                }
            }

            // 動画を再生
            videoPlayer.Play();

            // 動画が最後まで流れたら OnVideoEnd を呼ぶようにイベントを登録
            videoPlayer.loopPointReached += OnVideoEnd;
        }
    }

    // 動画が最後まで再生し終わったら自動で呼ばれる
    private void OnVideoEnd(VideoPlayer vp)
    {
        videoPlayer.loopPointReached -= OnVideoEnd;

        // 生成した看板付きコントローラー画像を削除する
        if (spawnedControllerInstance != null)
        {
            Destroy(spawnedControllerInstance);
            spawnedControllerInstance = null;
        }

        // パネルを非表示にする
        if (guidePanelObject != null)
        {
            guidePanelObject.SetActive(false);
        }
    }
}