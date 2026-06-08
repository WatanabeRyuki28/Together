using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitalManager : MonoBehaviour
{
    public void ClickNextScene()
    {
        SceneManager.LoadScene("SecondScene");
    }
}
