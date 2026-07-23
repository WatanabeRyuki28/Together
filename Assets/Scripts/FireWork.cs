using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireWork : MonoBehaviour
{
    [SerializeField] GameObject[] myArray = new GameObject[5];
    [SerializeField] GameObject hanabi;

    public IEnumerator HanabiShot()
    {
        foreach (GameObject point in myArray)
        {
            Instantiate(hanabi, point.transform.position, Quaternion.identity);
            yield return new WaitForSeconds(0.5f);
        }
    }
}