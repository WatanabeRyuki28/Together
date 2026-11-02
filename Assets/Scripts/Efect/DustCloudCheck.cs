using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DustCloudCheck : MonoBehaviour
{
    [SerializeField] GameObject dustCloud;

    
    public void DustCloud()
    {
        Debug.Log("çªâååƒÇŒÇÍÇΩÅI");
        Instantiate(dustCloud,transform.position,dustCloud.transform.rotation);
    }
}
