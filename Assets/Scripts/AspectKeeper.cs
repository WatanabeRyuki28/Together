using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AspectKeeper : MonoBehaviour
{
    [SerializeField] private Vector2 targetAspect = new Vector2(16f, 9f); // 固定したい比率

    void Awake()
    {
        Camera cam = GetComponent<Camera>();
        if (cam == null) return;

        float targetRatio = targetAspect.x / targetAspect.y;
        float currentRatio = (float)Screen.width / Screen.height;
        float scaleHeight = currentRatio / targetRatio;

        Rect rect = cam.rect;

        if (scaleHeight < 1.0f)
        {
            // 縦長の場合（上下に黒帯）
            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;
        }
        else
        {
            // 横長の場合（左右に黒帯）
            float scaleWidth = 1.0f / scaleHeight;
            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;
        }

        cam.rect = rect;
    }
}