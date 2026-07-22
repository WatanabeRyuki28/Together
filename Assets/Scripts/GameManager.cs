using UnityEngine;

public class GameManager : MonoBehaviour
{
    // シーンをまたいでも重複して生成されないようにするための簡易的な管理
    private static GameManager instance;

    void Awake()
    {
        // すでにインスタンスが存在していれば、新しく作られた方を破壊する
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        // シーンが移行してもこのオブジェクトを破壊しない
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // ゲーム開始時にマウスカーソルを非表示にして、画面中央に固定する
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Ctrlキー（左右どちらでも） と Escキー が同時に押されたか判定
        bool isCtrlPressed = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool isEscDown = Input.GetKeyDown(KeyCode.Escape);

        if (isEscDown)
        {
            QuitGame();
        }
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}