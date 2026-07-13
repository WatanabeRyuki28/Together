using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip bgm2;

    // BGMを共通にしたいシーン名の一覧
    [SerializeField] private string[] sameBgmScenes = { "SecondScene", "CharacterSelectScene", "StageSelectScene" };

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
        // 起動時に現在のシーンが対象ならBGMを再生
        TryPlayBGM(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryPlayBGM(scene.name);
    }

    private void TryPlayBGM(string sceneName)
    {
        Debug.Log("現在のシーン名: " + sceneName);

        if (IsSameBgmScene(sceneName))
        {
            Debug.Log("【合格】シーン名がリストに一致しました！再生処理に入ります。"); // 👈追加

            if (audioSource.isPlaying && audioSource.clip == bgm2)
            {
                Debug.Log("すでに同じBGMが再生中なので、そのまま流し続けます。"); // 👈追加
                return;
            }

            audioSource.clip = bgm2;
            audioSource.loop = true;
            audioSource.Play();
            Debug.Log("【再生】BGM2の再生コマンドを実行しました！"); // 👈追加
        }
        else
        {
            Debug.Log("【不合格】リストにないシーンなのでBGMを止めます。"); // 👈追加
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

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}