using UnityEngine;

public class DestructibleWall : MonoBehaviour, IInteractable
{
    [Header("Wall Settings")]
    [SerializeField] private ElementType breakableBy; // どの属性で壊れるか
    [SerializeField] private bool needsBoth = false;   // 両方の属性が必要か

    private bool hitByFire = false;
    private bool hitByIce = false;

    public void OnInteract(ElementType type)
    {
        if (needsBoth)
        {
            // 両方の属性が必要な場合の処理
            if (type == ElementType.Fire) hitByFire = true;
            if (type == ElementType.Ice) hitByIce = true;

            // 見た目で「あと一歩」感を出す（片方当たったら半透明にする等）
            UpdateVisuals();

            if (hitByFire && hitByIce)
            {
                BreakWall();
            }
        }
        else
        {
            // 特定の属性だけで壊れる場合
            if (type == breakableBy)
            {
                BreakWall();
            }
        }
    }

    private void UpdateVisuals()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // 片方当たったら少し暗くするなど
            sr.color = new Color(0.5f, 0.5f, 0.5f, 1.0f);
        }
    }

    private void BreakWall()
    {
        Debug.Log($"{gameObject.name} を破壊しました！");
        // パーティクルを出すならここで生成
        Destroy(gameObject);
    }
}