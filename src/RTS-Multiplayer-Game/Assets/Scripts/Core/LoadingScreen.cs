using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingScreen : MonoBehaviour
{
    [SerializeField]
    private List<RectTransform> parallaxLayers;
    [SerializeField]
    private List<float> parallaxLayersSpeeds;
    private float screenWidth;

    private void Awake()
    {
        screenWidth = 1920.0f;
        foreach (var layer in parallaxLayers)
            layer.anchoredPosition = new Vector2(0, 0);
    }

    private void Update()
    {
        for (int i = parallaxLayers.Count - 1; i >= 0; --i)
        {
            Vector2 newLayerPos = parallaxLayers[i].anchoredPosition;
            newLayerPos.x -= parallaxLayersSpeeds[i] * Time.deltaTime;
            if (newLayerPos.x <= -screenWidth)
                newLayerPos.x += screenWidth;
            parallaxLayers[i].anchoredPosition = newLayerPos;
        }
    }
}
