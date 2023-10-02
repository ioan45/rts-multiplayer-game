using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// Helper class for executing async functions on application quit.
// Async functions must be added/removed before starting the quitting process.
public class AsyncCleanupManager : MonoBehaviour
{
    public static AsyncCleanupManager Instance { get; private set; }

    private Dictionary<string, System.Action> cleanupCallbacks;
    private int callbacksToComplete;
    private int callbacksCompleted;

    private void Awake()
    {
        if (!IsSingletonInstance())
            return;
        
        cleanupCallbacks = new Dictionary<string, System.Action>();
        callbacksToComplete = 0;
        callbacksCompleted = 0;
        Application.wantsToQuit += OnAppWantsToQuit;

        CoreManager.Instance.SignalComponentInitialized();
    }

    public void AddAsyncCleanup(string callbackId, System.Func<Task> cleanupCallback)
    {
        cleanupCallbacks.TryAdd(callbackId, async () => {
            await cleanupCallback();
            SignalCleanupComplete();
        });
        ++callbacksToComplete;
    }

    public void RemoveAsyncCleanup(string callbackId)
    {
        bool callbackFound = cleanupCallbacks.Remove(callbackId);
        if (callbackFound)
            --callbacksToComplete;
    }

    private bool OnAppWantsToQuit()
    {
        // This method stops the current quitting attempt and calls all the subscribed async functions.
        // Also, it prevents itself from being called again.
        // If there are no subscribers, the current quitting attempt will continue.

        Application.wantsToQuit -= OnAppWantsToQuit;

        if (cleanupCallbacks.Count == 0)
            return true;

        foreach (var callback in cleanupCallbacks.Values)
            callback();
        return false;
    }

    private void SignalCleanupComplete()
    {
        // Once all the subscribed async functions completed their execution, a new attempt to quit is made.

        ++callbacksCompleted;
        if (callbacksCompleted == callbacksToComplete)
            Application.Quit();
    }

    private bool IsSingletonInstance()
    {
        if (Instance != null)
        {
            Destroy(this.gameObject);
            return false;
        }
        else
        {
            Instance = this;
            return true;
        }
    }
}
