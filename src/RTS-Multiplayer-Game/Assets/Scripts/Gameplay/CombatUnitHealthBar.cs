using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class CombatUnitHealthBar : NetworkBehaviour
{
    private CombatUnitBehaviour associatedUnit;
    private Camera associatedCamera;
    
    private NetworkVariable<ulong> assocUnitNetworkId;
    private NetworkVariable<float> barFillAmount;
    private NetworkVariable<bool> graphicsAreEnabled;

    private Transform graphicsHolder;
    private Image fillImage;
    private float distanceFromUnit;
    private Vector3 posOffsetFromUnit;
    private IEnumerator activeGetterCoroutine;

    void Awake()
    {
        graphicsHolder = transform.Find("Graphics");
        graphicsHolder.gameObject.SetActive(false); 
        assocUnitNetworkId = new NetworkVariable<ulong>(ulong.MaxValue);  // Just for clients. The server will reinitialize it with a proper value.
        barFillAmount = new NetworkVariable<float>(1.0f);
        graphicsAreEnabled = new NetworkVariable<bool>(false);  // Graphics starts hidden. They become visible on first HP update.
        distanceFromUnit = 40.0f;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsClient)
        {
            associatedCamera = CoreUi.Instance.mainCamera;
            
            if (assocUnitNetworkId.Value != ulong.MaxValue)
                StartUnitGetterCoroutine();
            assocUnitNetworkId.OnValueChanged += OnAssocMinionNetworkIdChange;

            fillImage = graphicsHolder.Find("Fill").GetComponent<Image>();
            fillImage.fillAmount = barFillAmount.Value;
            barFillAmount.OnValueChanged += OnNewBarFillAmount;
            
            graphicsHolder.gameObject.SetActive(graphicsAreEnabled.Value);
            graphicsAreEnabled.OnValueChanged += OnGraphicsVisibilityChange;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (activeGetterCoroutine != null)
            StopCoroutine(activeGetterCoroutine);
        base.OnNetworkDespawn();
    }

    public void Init(CombatUnitBehaviour assocUnit)
    {
        // Executed by server. This should be executed when the associated unit is spawned over the network.
        
        associatedUnit = assocUnit;
        assocUnitNetworkId = new NetworkVariable<ulong>(associatedUnit.NetworkObjectId);
        associatedUnit.CurrentBasicStats.HealthPoints.onValueChanged += OnHealthChange;
        associatedUnit.CurrentBasicStats.HealthPoints.onValueChanged += OnMinionDeath;
        this.enabled = false;  // This component will be inactive on Server, yet the OnChange callbacks will run.
    }

    void LateUpdate()
    {
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        if (associatedUnit == null || associatedCamera == null)
            return;
        posOffsetFromUnit = associatedCamera.transform.up * distanceFromUnit;
        transform.position = associatedCamera.WorldToScreenPoint(associatedUnit.transform.position + posOffsetFromUnit);
    }

    public void OnMinionDeath(float prevHp, float newHp)
    {
        // Executed by server.
        // The health bar is also destroyed on clients because Destroy() here calls Despawn() which calls Destroy() on clients.

        if (newHp <= 0)
        {
            associatedUnit.CurrentBasicStats.HealthPoints.onValueChanged -= OnHealthChange;
            associatedUnit.CurrentBasicStats.HealthPoints.onValueChanged -= OnMinionDeath;
            Destroy(this.gameObject);
        }
    }

    public void OnHealthChange(float prevHp, float newHp)
    {
        // Executed by server.

        float maxUnitHp = associatedUnit.CurrentBasicStats.HealthPointsMax.Value;
        barFillAmount.Value = newHp / maxUnitHp;

        if (graphicsAreEnabled.Value == false)
            graphicsAreEnabled.Value = true;
    }

    public void OnNewBarFillAmount(float previousValue, float newValue)
    {
        // Executed by clients.

        fillImage.fillAmount = newValue;
    }

    private void OnGraphicsVisibilityChange(bool previousValue, bool newValue)
    {
        // Executed by clients.

        if (newValue == true)
            UpdatePosition();
        graphicsHolder.gameObject.SetActive(newValue);
    }

    private void OnAssocMinionNetworkIdChange(ulong previousValue, ulong newValue)
    {
        // Executed by clients.

        StartUnitGetterCoroutine();
    }

    private void StartUnitGetterCoroutine()
    {
        if (activeGetterCoroutine != null)
            StopCoroutine(activeGetterCoroutine);
        activeGetterCoroutine = AskForAssocUnit();
        StartCoroutine(activeGetterCoroutine);
    }

    IEnumerator AskForAssocUnit()
    {
        // Even though this healthbar is created when its assoc. unit is spawned, the spawned network objects
        // dictionary in the NetworkSpawnManager of the clients may not be updated with the associated
        // minion at the initialization time of this healthbar, so a repeated check of the dictionary is done.

        while (!NetworkManager.SpawnManager.SpawnedObjects.ContainsKey(assocUnitNetworkId.Value))
            yield return new WaitForSeconds(0.1f);
        associatedUnit = NetworkManager.SpawnManager.SpawnedObjects[assocUnitNetworkId.Value].GetComponent<CombatUnitBehaviour>();
    }
}
