using UnityEngine;

public class BackgroundManager : MonoBehaviour
{
    [SerializeField] private Transform[] backgrounds;

    [SerializeField] private float scrollSpeed = 2f;

    [SerializeField] private float mapWidth = 52f;

    void Update()
    {
        foreach (Transform bg in backgrounds)
        {
            bg.position += Vector3.left * scrollSpeed * Time.deltaTime;

            if (bg.position.x <= -mapWidth)
            {
                float rightMostX = GetRightMostX();

                bg.position = new Vector3(
                    rightMostX + mapWidth,
                    bg.position.y,
                    bg.position.z
                );
            }
        }
    }

    float GetRightMostX()
    {
        float maxX = backgrounds[0].position.x;

        foreach (Transform bg in backgrounds)
        {
            if (bg.position.x > maxX)
            {
                maxX = bg.position.x;
            }
        }

        return maxX;
    }
}