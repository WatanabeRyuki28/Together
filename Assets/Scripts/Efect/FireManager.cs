using System.Collections;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public class FireManager : MonoBehaviour
{
    // Start is called before the first frame update

    private DestructibleWall fire;

    void Start()
    {
        Invoke("DestroyParticle", 1.0f);

        fire = GetComponent<DestructibleWall>();
    }

    void Update()
    {
        if (fire.isBreaking)
        {
            if (transform.localScale.x >= 0.0f) { transform.localScale -= Vector3.right * 0.01f; }
            if (transform.localScale.y >= 0.0f) { transform.localScale -= Vector3.up * 0.01f; }

        }
    }

    void DestroyParticle()
    {
        Destroy(gameObject);
    }
}
