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
    [SerializeField] private string[] sameBgmScenes = { "SecondScene", "CharacterSelectScene", "StageSelectScene" };

    private void Awake()
    {
        // 重複防止（すでにBGMManagerが存在していれば新しい方を消す）
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryPlayBGM(scene.name);
    }

    private void TryPlayBGM(string sceneName)
    {
        // ① タイトル系シーン（TitleBGM）か判定
        if (IsSameBgmScene(sceneName))
        {
            PlayClip(titleBgm);
        }
        // ② それ以外（Stage1〜8 や ClearScene）はすべて GameBGM にする
        else
        {
            PlayClip(gameBgm);
        }
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip == null) return;

        // すでに同じ曲が再生中ならそのまま流し続ける（途切れ防止）
        if (audioSource.isPlaying && audioSource.clip == clip)
        {
            return;
        }

        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.Play();
    }

    private bool IsSameBgmScene(string sceneName)
    {
        foreach (string name in sameBgmScenes)
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