using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class CombatUnitSpawner : NetworkBehaviour
{
    public static CombatUnitSpawner Instance { get; private set; }

    [field:SerializeField] 
    public List<GameObject> Player1SpawnAreas { get; private set; }
    [field:SerializeField] 
    public List<GameObject> Player2SpawnAreas { get; private set; }
    [field:SerializeField]
    public List<GameObject> UsedAreas { get; private set; }
    [field:SerializeField]
    public GameObject GhostUnit { get; private set; }
    public List<uint> InHandUnitsIds { get; private set; }
    public float CurrentEnergy { get; private set; }
    public float EnergyIncreaseRate { get; set; }  // energy amount per second

    [SerializeField]
    private List<GameObject> unitsPrefabsList;
    [SerializeField]
    private GameObject healthBarPrefab;
    [SerializeField]
    private Transform inWorldUiCanvas;
    private Dictionary<uint, GameObject> unitsPrefabsTable;
    private Queue<uint> inDeckUnitsIds;
    private float maxEnergy;
    private System.Random rng;

    private void Awake()
    {
        if (!IsSingletonInstance())
            return;

        if (NetworkManager.Singleton.IsServer)
        {
            unitsPrefabsTable = new Dictionary<uint, GameObject>();
            foreach (GameObject prefab in unitsPrefabsList)
                unitsPrefabsTable.Add(prefab.GetComponent<CombatUnitBehaviour>().UnitBasicData.unitId, prefab);
        }
        else
        {
            GhostUnit.SetActive(false);

            maxEnergy = 10.0f;
            CurrentEnergy = 3.0f;
            EnergyIncreaseRate = 0.5f;

            // Init unit cards (in-hand and in-deck).
            rng = new System.Random();
            InHandUnitsIds = new List<uint>();
            inDeckUnitsIds = new Queue<uint>();
            var tmpList = new List<uint>();
            
            foreach (uint id in UserData.SignedInUserData.deckUnitsData.Keys)
                tmpList.Add(id);
            ShuffleList(tmpList);

            for (int i = 0; i < tmpList.Count; ++i)
                if (i < UserData.SignedInUserData.deckUnitsData.Count / 2)
                    InHandUnitsIds.Add(tmpList[i]);
                else
                    inDeckUnitsIds.Enqueue(tmpList[i]);
            
            NetworkManager.Singleton.SceneManager.OnLoadComplete += OnGameplaySceneLoadedOnClient;
        }
    }

    private void Start()
    {
        if (NetworkManager.Singleton.IsClient)
            GameplayManager.Instance.GameTimeRemained.OnValueChanged += OnGameTimeRemainedChange;
    }

    private void Update()
    {   if (IsClient)
            CurrentEnergy = Mathf.Min(maxEnergy, CurrentEnergy + EnergyIncreaseRate * Time.deltaTime);
    }

    public void SpawnCombatUnit(int handIndex, Vector3 position)
    {
        if (handIndex >= InHandUnitsIds.Count)
            return;
        
        uint unitId = InHandUnitsIds[handIndex];
        uint unitLevel = UserData.SignedInUserData.ownedUnitsData[unitId].unitLevel;
        float unitCost = UserData.SignedInUserData.ownedUnitsData[unitId].basicData.energyCost;
        SpawnCombatUnitServerRpc(unitId, unitLevel, position, new ServerRpcParams());
        CurrentEnergy -= unitCost;
        inDeckUnitsIds.Enqueue(unitId);
        InHandUnitsIds[handIndex] = inDeckUnitsIds.Dequeue();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnCombatUnitServerRpc(uint unitId, uint unitLevel, Vector3 position, ServerRpcParams args)
    {
        GameObject unitPrefab = unitsPrefabsTable[unitId];
        CombatUnitBehaviour unitInstance = Instantiate(unitPrefab, position, Quaternion.identity).GetComponent<CombatUnitBehaviour>();
        unitInstance.ownerPlayerNumber = (args.Receive.SenderClientId == PlayersManager.Instance.Player1Data.clientData.networkId) ? 1U : 2U;
        unitInstance.CurrentBasicStats = new CombatUnitBasicStats(unitLevel, unitInstance.UnitBasicData);
        unitInstance.onNetworkSpawn += SpawnUnitHealthBar;
        unitInstance.GetComponent<NetworkObject>().Spawn(true);
    }

    private void SpawnUnitHealthBar(CombatUnitBehaviour assocUnit)
    {
        CombatUnitHealthBar healthBarObj = Instantiate(healthBarPrefab).GetComponent<CombatUnitHealthBar>();
        healthBarObj.Init(assocUnit);
        healthBarObj.GetComponent<NetworkObject>().Spawn(true);
        healthBarObj.GetComponent<NetworkObject>().TrySetParent(inWorldUiCanvas);
    }

    private void OnGameplaySceneLoadedOnClient(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        if (sceneName == "Gameplay")
            PlayersManager.Instance.onDataReceived += OnPlayerGameplayDataReceived;
    }

    private void OnPlayerGameplayDataReceived()
    {
        GameplayData ownData = PlayersManager.Instance.OwnGameplayData;

        // Init the ghost unit used as unit placeholder.
        GhostUnit.transform.forward = ownData.unitFacingDirOnSpawn;

        // Init spawn areas.
        UsedAreas = (ownData.playerNumber == 1 ? Player1SpawnAreas : Player2SpawnAreas);
        foreach (GameObject area in Player1SpawnAreas)
            area.SetActive(false);
        foreach (GameObject area in Player2SpawnAreas)
            area.SetActive(false);
    }

    private void OnGameTimeRemainedChange(float prevValue, float newValue)
    {
        // For the last 60s of the match, the energy increase rate is doubled.
        if (newValue <= 60.0f)
        {
            EnergyIncreaseRate *= 2;
            GameplayManager.Instance.GameTimeRemained.OnValueChanged -= OnGameTimeRemainedChange;
        }
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; --i)
        {
            int newPos = rng.Next(i + 1);
            T tmp = list[newPos];
            list[newPos] = list[i];  
            list[i] = tmp;
        }
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
