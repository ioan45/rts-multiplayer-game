using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

public class DeckPanelsManager : MonoBehaviour, IMainMenuPanel
{
    public static DeckPanelsManager Instance { get; private set; }
    public bool DeckModified { get; set; }

    [SerializeField]
    private GameObject deckPanels;
    [SerializeField]
    private GameObject infoPanel;

    [SerializeField]
    private TMP_Text goldIndicator;

    [SerializeField]
    private Button saveDeckButton;

    [SerializeField]
    private TMP_Text unitTitle;
    [SerializeField]
    private TMP_Text currentLevelText;
    [SerializeField]
    private Button levelUpButton;
    [SerializeField]
    private TMP_Text goldRequiredText;
    [SerializeField]
    private List<TMP_Text> infoSlots;
    
    [SerializeField]
    private List<DeckUnitFrame> deckFrames;
    [SerializeField]
    private List<ListUnitFrame> listFrames;

    private ListUnitFrame unitOnInfoPanel;
    private Animator showPanelController;

    private void Awake()
    {
        if (!IsSingletonInstance())
            return;
        
        DeckModified = false;
        unitOnInfoPanel = null;
        showPanelController = deckPanels.GetComponentInChildren<Animator>();

        goldIndicator.text = $"Gold : {UserData.SignedInUserData.gold}";
        saveDeckButton.onClick.AddListener(OnSaveDeckButtonPress);
        saveDeckButton.onClick.AddListener(() => CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.BUTTON_CLICK));
        levelUpButton.onClick.AddListener(OnLevelUpButtonPress);
        levelUpButton.onClick.AddListener(() => CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.BUTTON_CLICK));
        infoPanel.SetActive(false);
        deckPanels.SetActive(false);
        InitUnitFrames();
        
        MainMenuManager.Instance.SignalComponentInitialized();
    }

    private void InitUnitFrames()
    {
        // Init ListUnitFrames and DeckUnitFrames
        foreach (ListUnitFrame frame in listFrames)
        {
            OwnedCombatUnitData ownedUnitData;
            if (!UserData.SignedInUserData.ownedUnitsData.TryGetValue(frame.BasicUnitData.unitId, out ownedUnitData))
                frame.InitAsLockedUnit();
            else
            {
                frame.InitAsOwnedUnit(ownedUnitData);
                uint deckPosition;
                if (UserData.SignedInUserData.deckUnitsData.TryGetValue(frame.BasicUnitData.unitId, out deckPosition))
                    deckFrames[(int)deckPosition - 1].UpdateListUnitFrameRef(frame);
            }
        }
    }

    public void ShowPanel()
    {
        deckPanels.SetActive(true);
        showPanelController.SetTrigger("ShowPanel");
    }

    public void HidePanel()
    {
        deckPanels.SetActive(false);
    }

    public void ShowInfoPanel(ListUnitFrame unitFrame)
    {
        if (unitOnInfoPanel == unitFrame || unitFrame == null)
            return;

        OwnedCombatUnitData ownedUnitData = unitFrame.OwnedUnitData;
        CombatUnitBasicData basicUnitData = ownedUnitData.basicData;

        unitTitle.text = basicUnitData.unitName;
        currentLevelText.text = $"(Level {ownedUnitData.unitLevel})";
        int goldRequired = CombatUnitBasicStats.GetLevelBasedLevelUpGold(ownedUnitData.unitLevel);
        if (ownedUnitData.unitLevel < basicUnitData.maxLevel)
            goldRequiredText.text = $"{goldRequired} G";
        else
            goldRequiredText.text = "MAX";
        if (ownedUnitData.unitLevel < basicUnitData.maxLevel && UserData.SignedInUserData.gold >= goldRequired)
            levelUpButton.interactable = true;
        else
            levelUpButton.interactable = false;
        infoSlots[0].text = $"HP : {CombatUnitBasicStats.GetLevelBasedMaxHp(basicUnitData.baseMaxHp, ownedUnitData.unitLevel)}";
        infoSlots[1].text = $"ATK : {CombatUnitBasicStats.GetLevelBasedAtkDmg(basicUnitData.baseAttackDamage, ownedUnitData.unitLevel)}";
        infoSlots[2].text = $"ATK SPD : {basicUnitData.baseAttackSpeed}/s";
        infoSlots[3].text = $"MS : {basicUnitData.baseMovementSpeed}";
        infoSlots[4].text = $"ATK RNG : {basicUnitData.baseAttackRange}";

        infoPanel.SetActive(true);
        unitOnInfoPanel = unitFrame;
    }

    public async void OnLevelUpButtonPress()
    {
        int requiredGold = CombatUnitBasicStats.GetLevelBasedLevelUpGold(unitOnInfoPanel.OwnedUnitData.unitLevel);
        if (UserData.SignedInUserData.gold >= requiredGold)
        {
            levelUpButton.interactable = false;
            
            await PostNewUnitLevel(unitOnInfoPanel.BasicUnitData.unitId, unitOnInfoPanel.OwnedUnitData.unitLevel + 1, requiredGold);
            ++unitOnInfoPanel.OwnedUnitData.unitLevel;
            UserData.SignedInUserData.gold -= requiredGold;
            goldIndicator.text = $"Gold : {UserData.SignedInUserData.gold}";

            // Refresh the unit info panel.
            var tmp = unitOnInfoPanel;
            unitOnInfoPanel = null;
            ShowInfoPanel(tmp);
        }
    }

    private async Task PostNewUnitLevel(uint unitId, uint newUnitLevel, int goldUsed)
    {
#if USING_LOCAL_SERVERS
        const string requestURL = "http://localhost/level_up_unit";
#else
        const string requestURL = "https:// - /level_up_unit";
#endif
        WWWForm form = new WWWForm();
        form.AddField("SessionToken", UserData.SignedInUserData.loginSessionToken);
        form.AddField("Username", UserData.SignedInUserData.username);
        form.AddField("UnitId", unitId.ToString());
        form.AddField("ToLevel", newUnitLevel.ToString());
        form.AddField("GoldUsed", goldUsed.ToString());

        UnityWebRequest req = UnityWebRequest.Post(requestURL, form);
        req.disposeUploadHandlerOnDispose = true;
        req.disposeDownloadHandlerOnDispose = true;
        AsyncOperation asyncOp = req.SendWebRequest();
        while (!asyncOp.isDone)
            await Task.Delay(100);

        if (req.result != UnityWebRequest.Result.Success)
            Debug.Log("NewUnitLevel: Web request failed! " + req.error);
        else
        {
            string[] reqResponse = req.downloadHandler.text.Split('\t');
            if (reqResponse[0] != "1")
            {
                if (reqResponse.Length > 1)
                    Debug.Log("NewUnitLevel error: " + reqResponse[1]);
                else
                    Debug.Log("NewUnitLevel error: Unknown error.");
            }
        }

        req.Dispose();
    }

    public async void OnSaveDeckButtonPress()
    {
        if (DeckModified)
        {
            saveDeckButton.interactable = false;
            DeckModified = false;
            await SaveDeck();
            UpdateInternalDeck();
            saveDeckButton.interactable = true;
        }
    }

    private async Task SaveDeck()
    {
        string deckEncoding = "";
        for (int i = 0; i < deckFrames.Count - 1; ++i)
            deckEncoding += (deckFrames[i].ListFrame.BasicUnitData.unitId.ToString() + '&');
        deckEncoding += deckFrames[deckFrames.Count - 1].ListFrame.BasicUnitData.unitId.ToString();

#if USING_LOCAL_SERVERS
        const string requestURL = "http://localhost/save_deck";
#else
        const string requestURL = "https:// - /save_deck";
#endif
        WWWForm form = new WWWForm();
        form.AddField("SessionToken", UserData.SignedInUserData.loginSessionToken);
        form.AddField("Username", UserData.SignedInUserData.username);
        form.AddField("Deck", deckEncoding);

        UnityWebRequest req = UnityWebRequest.Post(requestURL, form);
        req.disposeUploadHandlerOnDispose = true;
        req.disposeDownloadHandlerOnDispose = true;
        AsyncOperation asyncOp = req.SendWebRequest();
        while (!asyncOp.isDone)
            await Task.Delay(100);

        if (req.result != UnityWebRequest.Result.Success)
            Debug.Log("SaveDeck: Web request failed! " + req.error);
        else
        {
            string[] reqResponse = req.downloadHandler.text.Split('\t');
            if (reqResponse[0] != "1")
            {
                if (reqResponse.Length > 1)
                    Debug.Log("SaveDeck error: " + reqResponse[1]);
                else
                    Debug.Log("SaveDeck error: Unknown error.");
            }
        }

        req.Dispose();
    }

    private void UpdateInternalDeck()
    {
        UserData.SignedInUserData.deckUnitsData = new Dictionary<uint, uint>();
        for (int i = deckFrames.Count - 1; i >= 0; --i)
            UserData.SignedInUserData.deckUnitsData.Add(deckFrames[i].ListFrame.BasicUnitData.unitId, (uint)i + 1);
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
