using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.UIElements;
using DG.Tweening;
using UnityEngine.AI;
using System.Security.Cryptography;
using TMPro;
using System.ComponentModel;
using System.Threading;
using Unity.VisualScripting;



// van chatGPT:
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher instance;

    private readonly Queue<Action> actionQueue = new Queue<Action>();
    private readonly object queueLock = new object();

    // Property to get the singleton instance
    public static UnityMainThreadDispatcher Instance
    {
        get
        {
            if (instance == null)
            {
                // Create a new GameObject to attach the script to
                GameObject go = new GameObject("UnityMainThreadDispatcher");
                instance = go.AddComponent<UnityMainThreadDispatcher>();
            }
            return instance;
        }
    }

    private void Update()
    {
        // Execute actions queued on the main thread
        lock (queueLock)
        {
            while (actionQueue.Count > 0)
            {
                Action action = actionQueue.Dequeue();
                action.Invoke();
            }
        }
    }

    // Enqueue an action to be executed on the main thread
    public void Enqueue(Action action)
    {
        lock (queueLock)
        {
            actionQueue.Enqueue(action);
        }
    }

    private void OnDestroy()
    {
        instance = null;
    }
}
