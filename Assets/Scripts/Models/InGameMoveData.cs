using System;

[Serializable]
public class NetMessage
{
    // 共通・メタ情報
    public string type;
    public string dataType;
    public string name_id;
    public string room_id;
    public int index;
    public int id;
    public bool IsStarted;

    // キャラ選択用
    public int char_index;
    public bool is_ready;

    // ステージ選択用
    public int stage_index;
    public bool stage_ready;

    // InGame移動・同期用
    public float position_x;
    public float position_y;
    public bool is_flip_x;
    public bool IsGrounded;

    // クリア選択用
    public int select_index;
}