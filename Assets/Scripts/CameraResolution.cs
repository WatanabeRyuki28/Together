using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraResolution : MonoBehaviour
{
    // 開発時の基準解像度（16:9）
    private const float developWidth = 1920f;
    private const float developHeight = 1080f;

    void Awake()
    {
        Camera mainCamera = GetComponent<Camera>();

        // 開発時のアスペクト比と現在の画面のアスペクト比を計算
        float targetAspect = developWidth / developHeight;
        float currentAspect = (float)Screen.width / Screen.height;

        // アスペクト比の比率から、最適な Size を割り出して設定
        float scale = targetAspect / currentAspect;
        mainCamera.orthographicSize = (developHeight / 2f / 100f) * scale;
    }
}