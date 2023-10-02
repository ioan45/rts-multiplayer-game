using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ListUnitFrame : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [field:SerializeField]
    public CombatUnitBasicData BasicUnitData { get; private set; }
    // If the unit is owned, this won't be null.
    public OwnedCombatUnitData OwnedUnitData { get; private set; }
    // If the unit is selected in deck, this won't be null.
    public DeckUnitFrame DeckFrame { get; set; }
    public Image ContentImage { get; private set; }

    private Transform parentCanvas;
    private Transform parentAfterDrag;
    private Vector3 positionBeforeDrag;

    private void Awake()
    {
        parentCanvas = GetComponentInParent<Canvas>().transform;
    }

    public void InitAsOwnedUnit(OwnedCombatUnitData ownedData)
    {
        OwnedUnitData = ownedData;
        ContentImage = transform.GetChild(0).GetChild(0).GetComponent<Image>();
        ContentImage.sprite = BasicUnitData.unitIcon;
    }

    public void InitAsLockedUnit()
    {
        ContentImage = transform.GetChild(0).GetChild(0).GetComponent<Image>();
        ContentImage.sprite = BasicUnitData.unitIconGreyscale;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (OwnedUnitData == null)
            return;

        parentAfterDrag = transform.parent;
        positionBeforeDrag = transform.position;
        transform.SetParent(parentCanvas);
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (OwnedUnitData == null)
            return;

        var mousePos = Mouse.current.position.ReadValue();
        transform.position = new Vector3(mousePos.x, mousePos.y, transform.position.z);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (OwnedUnitData == null)
            return;

        transform.SetParent(parentAfterDrag);
        transform.position = positionBeforeDrag;

        GraphicRaycaster raycaster = GetComponentInParent<GraphicRaycaster>();
        List<RaycastResult> raycastResults = new List<RaycastResult>();
        PointerEventData pointerData = new PointerEventData(EventSystem.current);

        pointerData.position = Mouse.current.position.ReadValue();
        raycaster.Raycast(pointerData, raycastResults);

        foreach (var result in raycastResults)
        {
            DeckUnitFrame deckFrameDroppedOn = result.gameObject.GetComponent<DeckUnitFrame>();
            if (deckFrameDroppedOn != null)
            {
                // (Mutual referencing if the unit is in deck: DeckUnitFrame <--> ListUnitFrame)
                // If the unit is already selected in deck, swap positions (references and content) with the unit dropped on
                if (DeckFrame != null)
                    DeckFrame.SwapListUnitFrameRefs(deckFrameDroppedOn);
                else
                    deckFrameDroppedOn.UpdateListUnitFrameRef(this);
                break;
            }
        }
    }

    public void OnPointerClick(PointerEventData pointerEventData)
    {
        if (OwnedUnitData == null)
            return;
        
        DeckPanelsManager.Instance.ShowInfoPanel(this);
    }
}
