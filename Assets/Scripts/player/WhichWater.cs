using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ElementHazardArea;

public class WhichWater : MonoBehaviour
{

    [Header("上昇設定")]
    [SerializeField] private GameObject WaterBlock;
    [SerializeField] private GameObject WaterBlock2;



    [SerializeField] private float speed = 5.0f;

    [Tooltip("初期位置からどれくらい上まで上昇できるか")]
    [SerializeField] private float maxRaiseHeight = 3.0f;
    [SerializeField] private float upWater;

    private bool shouldRaise = false;
    private float time = 0;
    private float waitTime = 0.5f;

    private float initialY;   // ゲーム開始時のY座標

    private float targetY;    // 現在の目標Y座標
    // Start is called before the first frame update
    void Start()
    {
        if (WaterBlock != null)
        {
            initialY = WaterBlock.transform.position.y;
            targetY = initialY;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (WaterBlock == null) return;

        if (shouldRaise)
        {

            if (WaterBlock != null)
            {
                Vector3 pos1 = WaterBlock.transform.position;
                pos1.y = Mathf.MoveTowards(pos1.y, targetY, speed * Time.deltaTime);
                WaterBlock.transform.position = pos1;

            }

            if (WaterBlock2 != null)
            {
                Vector3 pos2 = WaterBlock2.transform.position;
                pos2.y = Mathf.MoveTowards(pos2.y, targetY, speed * Time.deltaTime);
                WaterBlock2.transform.position = pos2;
            }

            if (Mathf.Approximately(WaterBlock.transform.position.y, targetY))
            {
                if (WaterBlock != null)
                {
                    Vector3 pos1 = WaterBlock.transform.position;
                    WaterBlock.transform.position = new Vector3(pos1.x, targetY, pos1.z);
                }
                if (WaterBlock2 != null)
                {
                    Vector3 finalPos2 = WaterBlock2.transform.position;
                    WaterBlock2.transform.position = new Vector3(finalPos2.x, targetY, finalPos2.z);
                }

                shouldRaise = false;
                Debug.Log("[Update] 目標の高さに到達しました。");
            }
        }

        if (WaterBlock != null && WaterBlock2 != null)
        {
            time += Time.deltaTime;
            if (time >= waitTime)
            {
                bool nextState = !WaterBlock.activeSelf;
                WaterBlock.SetActive(nextState);
                WaterBlock2.SetActive(!nextState);

                time = 0f;
            }
        }
    }

    // 水を上げる処理
    public void TryRaiseBlock()
    {
        if (WaterBlock == null) return;

        float currentBaseY = shouldRaise ? targetY : WaterBlock.transform.position.y;
        float absoluteMaxY = initialY + maxRaiseHeight;

        if (currentBaseY >= absoluteMaxY)
        {
            Debug.Log("[TryRaiseBlock] これ以上上に上がれません（上限に達しています）。");
            return;
        }

        targetY = currentBaseY + upWater;

        if (targetY > absoluteMaxY)
        {
            targetY = absoluteMaxY;
        }

        shouldRaise = true;
        Debug.Log($"[TryRaiseBlock] 目標Y座標を {targetY} に設定しました（上限: {absoluteMaxY}）。");
    }
}
