using System;
using System.Threading.Tasks;
using UnityEngine;

public class WebRequestLoop
{
    private Func<Task> request;
    private int nextReqDelay;  // in milliseconds
    private bool isLoopRunning;
    private bool stopLoopRequested;

    public event System.Action onRequestComplete;

    public WebRequestLoop(Func<Task> requestFunction, int nextRequestDelay)
    {
        request = requestFunction;
        nextReqDelay = nextRequestDelay;
        isLoopRunning = false;
        stopLoopRequested = false;
    }

    public void StartRequestLoop()
    {
        // This is set here to false for cases when the stop request is cancelled.
        // A stop request can be cancelled while the loop still hasn't completed execution after the stop request was issued.
        stopLoopRequested = false;
        
        if (!isLoopRunning)
            RunLoop();
    }

    public void StopRequestLoop()
    {
        if (!isLoopRunning)
            return;
        stopLoopRequested = true;
    }

    public void SetNewReqDelay(int delay)
        => nextReqDelay = delay;

    private async void RunLoop()
    {
        isLoopRunning = true;
        while (!stopLoopRequested)
        {
            await request();
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
#endif
            onRequestComplete?.Invoke();

            if (!stopLoopRequested)
                await Task.Delay(nextReqDelay);
#if UNITY_EDITOR
            if (!Application.isPlaying)
                return;
#endif
        }
        isLoopRunning = false;
    }
}
