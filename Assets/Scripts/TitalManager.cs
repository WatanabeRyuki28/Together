using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitalManager : MonoBehaviour
{

    private CommunicationUI controls;

    void Awake()
    {
        // インスタンスの生成
        controls = new CommunicationUI();
    }

    void OnEnable()
    {
        // アクションマップの有効化
        // タイトルやステージ選択など、使うマップ名（例：StageSelect または独自のマップ）を指定してください
        if (controls != null)
        {
            controls.Tital.Enable();
        }
    }

    void OnDisable()
    {
        // シーンを抜ける時は安全のために操作をオフにする
        if (controls != null)
        {
            controls.Tital.Disable();
        }
    }

    void Update()
    {
        // ゲームパッドやキーボードの決定ボタン（Submit）が押された瞬間を検知
        if (controls != null && controls.Tital.Submit != null && controls.Tital.Submit.triggered)
        {
            Debug.Log("【タイトル入力】ゲームパッド/キーボードによる決定入力を検知しました。");
            ClickNextScene();
        }

    }
    public void ClickNextScene()
    {
        SceneManager.LoadScene("SecondScene");
    }
}
