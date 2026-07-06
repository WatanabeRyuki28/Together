using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    // 取得したアイテムのID（例: "Stage1_ItemA" など）を保存するリスト
    public List<string> obtainedItemIds = new List<string>();
}