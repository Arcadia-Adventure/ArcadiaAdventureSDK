using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InternetManager : MonoBehaviour
{
    
    string m_ReachabilityText;

    private bool isAvailable=true;
    private float timer = 0f;

    
    void Update()
    {
        if (isAvailable)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                timer += Time.deltaTime;
                if (timer > 3)
                {
                    m_ReachabilityText = ":* Not Reachable";
                    isAvailable = false;
                    timer = 0;
                    
                    GameObject obj = Instantiate(Resources.Load<GameObject>("No Internet"));
                    DontDestroyOnLoad(obj);
                }
            }
            else if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork ||
                     Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
            {
                m_ReachabilityText = ":* Connected";
            }
        }
    }
[ContextMenu("OnRetry")]
    public bool OnRetry()
    {
        if (Application.internetReachability == NetworkReachability.ReachableViaCarrierDataNetwork ||
            Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
        {
            isAvailable = true;
            return true;
        }
        else
        {
            return false;
        }
    }
}
