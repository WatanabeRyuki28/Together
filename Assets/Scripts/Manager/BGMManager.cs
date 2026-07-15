using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip bgm2;

    // BGMを共通にしたいシーン名の一覧（BGM2を流し続けたいロビー画面など）
    [SerializeField] private string[] sameBgmScenes = { "SecondScene", "CharacterSelectScene", "StageSelectScene" };

    // ★【追加】このシーンに入ったら、BGMManager自体を削除して完全にリセットするシーン名の一覧
    // （ここに「ステージ」や「クリア画面」の正確なシーン名を設定します）
    [SerializeField] private string[] destroyMeScenes = { "StageScene", "ClearScene" };

    private void Awake()
    {
        // シングルトンのパターンの実装（重複防止）
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // シーン遷移してもオブジェクトを破壊しない

        // 最初のシーン移動のイベント登録
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        // 起動時に現在のシーン名が退場シーンなら自爆、そうでなければ再生を試みる
        string currentScene = SceneManager.GetActiveScene().name;
        if (ShouldDestroy(currentScene))
        {
            SelfDestroy();
            return;
        }

        TryPlayBGM(currentScene);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ★【追加】もし退場させたいシーンに入った場合は、BGMManagerを完全に消滅させる
        if (ShouldDestroy(scene.name))
        {
            Debug.Log($"【退場】{scene.name}に入ったため、BGMManagerを削除してリセットします。");
            SelfDestroy();
            return;
        }

        TryPlayBGM(scene.name);
    }

    private void TryPlayBGM(string sceneName)
    {
        Debug.Log("現在のシーン名: " + sceneName);

        if (IsSameBgmScene(sceneName))
        {
            Debug.Log("【合格】シーン名がリストに一致しました！再生処理に入ります。");

            if (audioSource.isPlaying && audioSource.clip == bgm2)
            {
                Debug.Log("すでに同じBGMが再生中なので、そのまま流し続けます。");
                return;
            }

            audioSource.clip = bgm2;
            audioSource.loop = true;
            audioSource.Play();
            Debug.Log("【再生】BGM2の再生コマンドを実行しました！");
        }
        else
        {
            Debug.Log("【不合格】リストにないシーンなのでBGMを止めます。");
            audioSource.Stop();
        }
    }

    private bool IsSameBgmScene(string sceneName)
    {
        foreach (string name in sameBgmScenes)
        {
            if (sceneName == name)
            {
                return true;
            }
        }
        return false;
    }

    // ★【追加】退場シーンかどうかのチェック判定
    private bool ShouldDestroy(string sceneName)
    {
        foreach (string name in destroyMeScenes)
        {
            if (sceneName == name)
            {
                return true;
            }
        }
        return false;
    }

    // ★【追加】安全にイベント接続を解除して自爆するためのメソッド
    private void SelfDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}