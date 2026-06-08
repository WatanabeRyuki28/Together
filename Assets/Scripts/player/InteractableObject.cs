using UnityEngine;

public class InteractableObject : MonoBehaviour, IInteractable
{
    public void OnInteract(ElementType type)
    {
        if (type == ElementType.Fire)
        {
            // ”R‚¦‚éFÁ–Å
            Destroy(gameObject);
        }
        else if (type == ElementType.Ice)
        {
            // “€‚éF•¨—‚ğ~‚ß‚ÄF‚ğ•Ï‚¦‚é
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb) rb.bodyType = RigidbodyType2D.Static;
            GetComponent<SpriteRenderer>().color = Color.cyan;
        }
    }
}