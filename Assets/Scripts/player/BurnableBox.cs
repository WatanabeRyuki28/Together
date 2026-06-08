using UnityEngine;

// どんなオブジェクトにも貼れる汎用スクリプト
public class ElementReceiver : MonoBehaviour, IInteractable
{
    [Header("Behavior Settings")]
    [SerializeField] private bool canBurn = true;   // 炎で消滅するか
    [SerializeField] private bool canFreeze = true; // 氷で固まるか

    [Header("Visual Effects (Optional)")]
    [SerializeField] private Color freezeColor = new Color(0.5f, 0.8f, 1f);

    public void OnInteract(ElementType type)
    {
        switch (type)
        {
            case ElementType.Fire:
                if (canBurn) PerformBurn();
                break;

            case ElementType.Ice:
                if (canFreeze) PerformFreeze();
                break;
        }
    }

    private void PerformBurn()
    {
        // 燃える演出（必要ならここにエフェクト生成を入れる）
        Destroy(gameObject);
    }

    private void PerformFreeze()
    {
        // 物理を止める
        if (TryGetComponent<Rigidbody2D>(out Rigidbody2D rb))
        {
            rb.bodyType = RigidbodyType2D.Static;
        }

        // 色を変える
        if (TryGetComponent<SpriteRenderer>(out SpriteRenderer sr))
        {
            sr.color = freezeColor;
        }
    }
}