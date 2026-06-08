using System;
[Serializable]
public class InitResponse
{
    public string type;
    public string name_id;  
    public string room_id;     // 所属するルームID
    public int  index;
    public bool IsStarted;
}

[Serializable]
public class CharSelectData : InitResponse
{
    public int char_index;     // 選んだキャラの番号
    public bool is_ready;      // 決定したか
}

[Serializable]
public class StageSelectData : InitResponse
{ 
    public int stage_index;   // どこのステージを選んだのか
    public bool stage_ready;  // 確定ボタンを押したかどうか？
}

[Serializable]
public class InGameMoveData : InitResponse
{
    public string dataType;
    public int char_index;

    public int id;      // オブジェクト用の番号

    public float position_x;   // X座標
    public float position_y;   // Y座標

   
}


