using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SendEvents : MonoBehaviour
{
    public UnityEvent sendPHP;
    public UnityEvent sendUIGlobal;
    public UnityEvent sendFinish;

    public void InvokeEvent()
    {
        if (sendPHP != null)
        {
            sendPHP.Invoke();
        }
    }

    public void InvokeUIGlobal()
    {
        if (sendUIGlobal != null)
        {
            sendUIGlobal.Invoke();
        }
    }

    public void InvokeFinish()
    {
        if (sendFinish != null)
        {
            sendFinish.Invoke();
        }
    }
}
