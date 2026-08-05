using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    [SerializeField] private AudioSource audioSource;

    [Header("BGM素材")]
    [SerializeField] private AudioClip titleBgm; // TitleBGMをセット
    [SerializeField] private AudioClip gameBgm;  // GameBGMをセット

    [Header("TitleBGMを鳴らすシーン名")]
    [SerializeField] private string[] titleBgmScenes = { "SecondScene", "CharacterSelectScene", "StageSelectScene" };

    private void Awake()
    {
        // 🌟 重複防止処理を強化
        if (Instance != null && Instance != this)
        {
            // すでに古いBGMManager（曲を再生中のもの）が存在する場合、
            // 新しい方の AudioSource を即座に停止して削除する（音が重複するのを防ぐ）
            if (audioSource != null)
            {
                audioSource.Stop();
            }
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        // 最初のシーン起動時のBGM判定
        TryPlayBGM(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryPlayBGM(scene.name);
    }

    private void TryPlayBGM(string sceneName)
    {
        // ① タイトル・メニュー系シーンの場合 ➔ titleBgm を再生
        // (SecondScene / CharacterSelectScene / StageSelectScene など)
        if (IsTitleBgmScene(sceneName))
        {
            PlayClip(titleBgm);
        }
        // ② ゲームプレイ中・クリアシーン等の場合 ➔ gameBgm を再生
        // (Stage1〜8 / ClearScene など)
        else
        {
            PlayClip(gameBgm);
        }
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip == null) return;

        // すでに同じ曲が再生中なら、曲を止めずに継続して流し続ける
        if (audioSource.isPlaying && audioSource.clip == clip)
        {
            return;
        }

        // 違う曲になった場合のみ切り替えて再生
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.Play();
    }

    private bool IsTitleBgmScene(string sceneName)
    {
        foreach (string name in titleBgmScenes)
        {
            if (sceneName == name) return true;
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