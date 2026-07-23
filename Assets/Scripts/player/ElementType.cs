using UnityEngine;

/// <summary>
/// プレイヤー、弾、ギミック（床や箱など）共通で使用する属性タイプ
/// </summary>
public enum ElementType
{
    None = 0, // 属性なし（デフォルト・中立）
    Fire = 1, // 炎属性
    Ice = 2  // 氷属性
}