using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(AudioSource))]
public class WarpDoor : MonoBehaviour
{
    [Header("Audio Settings (効果音)")]
    [SerializeField] private AudioClip openSound;

    private Animator animator;
    private BoxCollider2D doorCollider;
    private AudioSource audioSource;
    private bool isOpen = false;

    private static readonly int IsOpenParam = Animator.StringToHash("IsOpen");

    [Header("エフェクト")]
    [SerializeField] private DustCloudCheck dust1;
    [SerializeField] private DustCloudCheck dust2;
    [SerializeField] private GameObject shutterDie;

    private Coroutine dustCoroutine;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        doorCollider = GetComponent<BoxCollider2D>();
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        DustCloudCheck[] dusts = GetComponentsInChildren<DustCloudCheck>();
        if (dusts.Length > 0 && dust1 == null) dust1 = dusts[0];
        if (dusts.Length > 1 && dust2 == null) dust2 = dusts[1];

        UpdateColliderState();
    }

    public void SetDoorState(bool open)
    {
        if (isOpen == open) return;

        isOpen = open;

        if (animator != null)
        {
            animator.SetBool(IsOpenParam, open);
        }

        UpdateColliderState();

        if (isOpen)
        {
            PlaySound(openSound);
        }
    }

    public void DustCloudCheck(bool isClose)
    {
        if (isClose && (dust1 != null || dust2 != null))
        {
            if (dustCoroutine != null)
            {
                StopCoroutine(dustCoroutine);
            }

            dustCoroutine = StartCoroutine(DelayDustCoroutine());
        }
    }

    private IEnumerator DelayDustCoroutine()
    {
        yield return new WaitForSeconds(0.5f);

        if (dust1 != null) dust1.DustCloud();
        if (dust2 != null) dust2.DustCloud();

        dustCoroutine = null;
    }

    private void UpdateColliderState()
    {
        if (doorCollider == null) return;

        if (isOpen)
        {
            doorCollider.isTrigger = true;
            doorCollider.size = new Vector2(2.16f, 0.8019378f);
            doorCollider.offset = new Vector2(0.0f, 0.8590311f);

            if (shutterDie != null)
            {
                shutterDie.transform.localPosition = new Vector2(0.0f, 0.5533f);
            }
            Debug.Log($"{gameObject.name} が開いたため、通り抜け可能になりました。");
        }
        else
        {
            doorCollider.isTrigger = false;
            doorCollider.size = new Vector2(2.16f, 2.52f);
            doorCollider.offset = new Vector2(0.0f, 0.0f);

            if (shutterDie != null)
            {
                shutterDie.transform.localPosition = new Vector2(0.0f, -1.14f);
            }
            Debug.Log($"{gameObject.name} が閉じたため、通行不可の壁になりました。");
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}