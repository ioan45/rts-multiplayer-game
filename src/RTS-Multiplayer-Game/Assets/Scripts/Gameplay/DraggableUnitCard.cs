using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class DraggableUnitCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField]
    private int handCardIndex;

    private Camera cam;
    private Image cardIcon;
    private GameObject cardGraphics;
    private CombatUnitSpawner unitSpawner;
    private GraphicRaycaster raycaster;
    private Transform parentCanvas;
    private Transform parentAfterDrag;
    private Vector3 positionBeforeDrag;
    private PointerEventData pointerData;
    private List<RaycastResult> raycastResults;
    private int spawnAreasLayerMask;
    private bool isInsideGrid;

    private void Awake()
    {
        // The server doesn't use this UI feature.
        if (NetworkManager.Singleton.IsServer)
        {
            this.gameObject.SetActive(false);
            return;   
        }

        cam = CoreUi.Instance.mainCamera;
        raycaster = GetComponentInParent<GraphicRaycaster>();
        cardGraphics = transform.GetChild(0).gameObject;
        cardIcon = cardGraphics.transform.GetChild(0).GetChild(0).GetComponent<Image>();
        parentCanvas = GetComponentInParent<Canvas>().transform;
        pointerData = new PointerEventData(EventSystem.current);
        raycastResults = new List<RaycastResult>();
        spawnAreasLayerMask = 1 << LayerMask.NameToLayer("SpawnArea");
        isInsideGrid = true;
    }

    private void Start()
    {
        unitSpawner = CombatUnitSpawner.Instance;
        // The graphics are initialized here because the unit spawner initialization is done in its Awake()
        // (since this object and the unit spawner are both in-scene placed objects, their Awake() calls order is unknown
        // and Start() comes after all Awake calls).
        UpdateCardGraphics();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        parentAfterDrag = transform.parent;
        positionBeforeDrag = transform.position;
        transform.SetParent(parentCanvas);
        transform.SetAsLastSibling();

        foreach (var area in unitSpawner.UsedAreas)
            area.gameObject.SetActive(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        var mousePos = Mouse.current.position.ReadValue();
        transform.position = new Vector3(mousePos.x, mousePos.y, transform.position.z);

        pointerData.position = mousePos;
        raycastResults.Clear();
        raycaster.Raycast(pointerData, raycastResults);
        if (isInsideGrid && raycastResults.Count == 3)  // the draggable card consists of 3 discoverable ui elements under the mouse pointer
        {
            cardGraphics.SetActive(false);
            isInsideGrid = false;
        }
        else if (!isInsideGrid && raycastResults.Count > 0)
        {
            cardGraphics.SetActive(true);
            isInsideGrid = true;
            unitSpawner.GhostUnit.SetActive(false);
        }

        if (!isInsideGrid)
        {
            Ray ray = cam.ScreenPointToRay(mousePos);
            RaycastHit raycastHit;
            if (Physics.Raycast(ray, out raycastHit, Mathf.Infinity, spawnAreasLayerMask))
            {
                Vector3 ghostPos = raycastHit.point;
                ghostPos.y = 50.0f;
                unitSpawner.GhostUnit.transform.position = ghostPos;
                if (!unitSpawner.GhostUnit.activeSelf)
                    unitSpawner.GhostUnit.SetActive(true);
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        uint associatedUnitId = unitSpawner.InHandUnitsIds[handCardIndex];
        float associatedUnitCost = UserData.SignedInUserData.ownedUnitsData[associatedUnitId].basicData.energyCost;
        
        if (unitSpawner.GhostUnit.activeSelf && associatedUnitCost <= unitSpawner.CurrentEnergy)
        {
            var unitSpawnPos = unitSpawner.GhostUnit.transform.position;
            unitSpawnPos.y = UserData.SignedInUserData.ownedUnitsData[associatedUnitId].basicData.ySpawnCoord;
            unitSpawner.SpawnCombatUnit(handCardIndex, unitSpawnPos);
            UpdateCardGraphics();
        }
        unitSpawner.GhostUnit.SetActive(false);

        foreach (var area in unitSpawner.UsedAreas)
            area.gameObject.SetActive(false);

        transform.SetParent(parentAfterDrag);
        transform.position = positionBeforeDrag;
        cardGraphics.SetActive(true);
        isInsideGrid = true;
    }

    private void UpdateCardGraphics()
    {
        uint associatedUnitId = unitSpawner.InHandUnitsIds[handCardIndex];
        cardIcon.sprite = UserData.SignedInUserData.ownedUnitsData[associatedUnitId].basicData.unitIcon;
    }
}
