using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Reconnect : MonoBehaviour
{
    public void Retry()
    {
        InternetManager instance = FindObjectOfType<InternetManager>();
        if (instance != null)
        {
            if (instance.OnRetry())
            {
                Destroy(this.gameObject);
            }
        }
    }

}
