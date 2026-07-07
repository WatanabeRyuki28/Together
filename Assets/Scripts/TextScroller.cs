
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TextScroller : MonoBehaviour
{
    public enum BlinkType
    {
        Smooth,   // じわじわ明滅（フェードイン・アウト）
        Sharp     // パッ、パッと切り替わる点滅
    }

    [Header("点滅のタイプ（じわじわ or パキパキ）")]
    [SerializeField] private BlinkType blinkType = BlinkType.Smooth;

    [Header("点滅のスピード（数値が大きいほど速い）")]
    [SerializeField] private float blinkSpeed = 2.0f;

    private Graphic uiGraphic;
    private Color originalColor;

    void Start()
    {
        // TextやImageの共通の親クラス（Graphic）を取得
        uiGraphic = GetComponent<Graphic>();

        if (uiGraphic != null)
        {
            originalColor = uiGraphic.color;
        }
        else
        {
            Debug.LogError("このオブジェクトにはTextやImageがついていません！");
        }
    }

    void Update()
    {
        if (uiGraphic == null) return;

        if (blinkType == BlinkType.Smooth)
        {
            // ────────── 1. じわじわ明滅させる処理 ──────────
            // Mathf.Sin を使うことで、時間の経過とともに 0 ～ 1 の間をなめらかに往復します
            float alpha = (Mathf.Sin(Time.time * blinkSpeed) + 1.0f) / 2.0f;

            // 計算した透明度（alpha）をUIの色に適用
            uiGraphic.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
        }
        else if (blinkType == BlinkType.Sharp)
        {
            // ────────── 2. パッパッと切り替える処理 ──────────
            // 一定時間ごとに 0 か 1 かをパキッと切り替えます
            float alpha = (Mathf.Repeat(Time.time * blinkSpeed, 1.0f) < 0.5f) ? 0f : 1f;

            uiGraphic.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
        }
    }

    // スクリプトがオフになったりシーンが切り替わるときは、文字をちゃんと見える状態（不透明）に戻す
    void OnDisable()
    {
        if (uiGraphic != null)
        {
            uiGraphic.color = originalColor;
        }
    }
}