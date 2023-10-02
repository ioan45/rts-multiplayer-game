using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class DeckUnitFrame : MonoBehaviour
{
    // Reference to the list frame attached to this deck slot.
    public ListUnitFrame ListFrame { get; private set; }

    private Image contentImage;

    private void Awake()
    {
        contentImage = transform.GetChild(0).GetChild(0).GetComponent<Image>();
    }

    private void Start()
    {
        UpdateFrameContent();
    }

    public void UpdateListUnitFrameRef(ListUnitFrame newRef)
    {
        if (newRef != null)
        {
            // Update the mutual references
            if (ListFrame != null)
                ListFrame.DeckFrame = null;
            ListFrame = newRef;
            newRef.DeckFrame = this;

            // Update content
            UpdateFrameContent();

            DeckPanelsManager.Instance.DeckModified = true;
        }
    }

    public void SwapListUnitFrameRefs(DeckUnitFrame other)
    {
        if (other != null)
        {
            // Swap the ListUnitFrame references.
            var tmp = other.ListFrame;
            other.ListFrame = this.ListFrame;
            this.ListFrame = tmp;

            // Update the DeckUnitFrame references inside the swapped ListUnitFrame references.
            this.ListFrame.DeckFrame = this;
            other.ListFrame.DeckFrame = other;

            // Update content for both DeckUnitFrame objects.
            other.UpdateFrameContent();
            this.UpdateFrameContent();

            DeckPanelsManager.Instance.DeckModified = true;
        }
    }

    private void UpdateFrameContent()
    {
        if (this.contentImage != null && ListFrame != null && ListFrame.ContentImage != null)
            this.contentImage.sprite = ListFrame.ContentImage.sprite;
    }
}
