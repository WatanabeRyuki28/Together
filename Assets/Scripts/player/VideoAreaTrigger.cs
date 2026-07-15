using UnityEngine;
using UnityEngine.Video;

public class VideoAreaTrigger : MonoBehaviour
{
    [Header("表示するRawImageオブジェクト")]
    [SerializeField] private GameObject videoUIObject;

    [Header("VideoPlayerコンポーネント")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("一度だけ再生するかどうか")]
    [SerializeField] private bool playOnlyOnce = true;

    private bool hasPlayed = false;

    private void Start()
    {
        // 事前に非表示・停止であることを確認
        if (videoUIObject != null) videoUIObject.SetActive(false);
        if (videoPlayer != null) videoPlayer.Stop();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // プレイヤーが触れたか判定 (PlayerControllerを持っているか、またはTagがPlayerか)
        var player = other.GetComponent<PlayerController>();
        if (player == null) return;

        // すでに再生済みで、一度きりの設定なら無視
        if (playOnlyOnce && hasPlayed) return;

        // 動画再生処理
        PlayVideo();
    }

    private void PlayVideo()
    {
        hasPlayed = true;

        if (videoUIObject != null && videoPlayer != null)
        {
            // UIを表示して動画を再生
            videoUIObject.SetActive(true);
            videoPlayer.Play();

            // 動画が終了した時のイベントを登録
            videoPlayer.loopPointReached += OnVideoEnd;
        }
    }

    // 動画が最後まで再生し終わったら呼ばれる
    private void OnVideoEnd(VideoPlayer vp)
    {
        // イベントの登録解除
        videoPlayer.loopPointReached -= OnVideoEnd;

        // UIを非表示にして停止する
        if (videoUIObject != null)
        {
            videoUIObject.SetActive(false);
        }
    }
}