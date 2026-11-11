using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEditor.PlayerSettings;
using static UnityEditor.Rendering.FilterWindow;

public class WaterUp : MonoBehaviour
{
    [SerializeField] GameObject WaterBlock;
    [SerializeField] GameObject WaterBlock2;
    [SerializeField] float speed = 5.0f;

    private float time = 0;
    private float waitTime = 0.5f;

    private float targetY;


    [Header("§ŒÀÝ’è")]
    [SerializeField] private bool canRaiseMultipleTimes = false; // ‰½‰ñ‚à…ˆÊ‚ðã‚°‚ç‚ê‚é‚æ‚¤‚É‚·‚é‚©‚Ç‚¤‚©
   

    public ElementType Element => projectileType;


    private ElementType projectileType;
    void Start()
    {
        
        if (WaterBlock != null)
        {
            targetY = WaterBlock.transform.position.y;
        }
    }

    void Update()
    {
        if (WaterBlock == null) return;

    
            targetY = WaterBlock.transform.position.y + 1f;
        


        Vector2 currentPos = WaterBlock.transform.position;

    
        currentPos.x = WaterBlock.transform.position.x;
        currentPos.y = Mathf.MoveTowards(currentPos.y, targetY, speed * Time.deltaTime);

        WaterBlock.transform.position = currentPos;


        time += Time.deltaTime;

        if (time >= waitTime)
        {
          
            bool isActive = WaterBlock2.activeSelf;
            WaterBlock2.SetActive(!isActive);

        
            time = 0f;
        }

    }


   
}
