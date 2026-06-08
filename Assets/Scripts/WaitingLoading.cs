using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaitingLoading : MonoBehaviour
{

    private float waitTime = 2;

    public GameObject LoadPanel;
    public GameObject CharSelectPanel;
    // Start is called before the first frame update
    void Start()
    {
        waitTime = 2;
        LoadPanel.SetActive(true);
        CharSelectPanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        waitTime -= Time.deltaTime;

        if (waitTime < 0)
        {
            LoadPanel.SetActive(false);
            CharSelectPanel.SetActive(true);

        }
    }
}
