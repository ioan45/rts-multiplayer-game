using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainUiManager : MonoBehaviour
{
    [SerializeField]
    private GameObject menuUi;
    [SerializeField]
    private GameObject inWorldGameUi;
    [SerializeField]
    private Button menuButton;
    [SerializeField]
    private Button changeCameraButton;
    [SerializeField]
    private Slider ownHpBar;
    [SerializeField]
    private Slider enemyHpBar;
    [SerializeField]
    private Slider energyBar;
    [SerializeField]
    private TMP_Text ownBarHpText;
    [SerializeField]
    private TMP_Text ownBarNameText;
    [SerializeField]
    private TMP_Text enemyBarHpText;
    [SerializeField]
    private TMP_Text enemyBarNameText;
    [SerializeField]
    private TMP_Text energyBarValueText;
    [SerializeField]
    private TMP_Text gameTimeText;
    [SerializeField]
    private Image gameTimeBackground;

    private int currentEnergy;
    private PlayerUnitBehaviour ownPlayerUnit;
    private PlayerUnitBehaviour enemyPlayerUnit;

    private void Awake()
    {
        menuButton.onClick.AddListener(OnMenuButtonPress);
        menuButton.onClick.AddListener(() => CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.BUTTON_CLICK));
        changeCameraButton.onClick.AddListener(OnChangeCameraButton);
        changeCameraButton.onClick.AddListener(() => CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.BUTTON_CLICK));
    }

    private void OnEnable()
    {
        inWorldGameUi.SetActive(true);
    }

    private void Start()
    {
        ownBarNameText.text = UserData.SignedInUserData.playerName;
        ownHpBar.value = 1.0f;
        ownBarHpText.text = ((int)PlayersManager.Instance.Player1CombatUnitObj.CurrentBasicStats.HealthPointsMax.Value).ToString(); 
        enemyHpBar.value = 1.0f;
        enemyBarHpText.text = ((int)PlayersManager.Instance.Player1CombatUnitObj.CurrentBasicStats.HealthPointsMax.Value).ToString(); 

        energyBar.value = CombatUnitSpawner.Instance.CurrentEnergy / 10.0f;
        currentEnergy = (int)CombatUnitSpawner.Instance.CurrentEnergy;
        energyBarValueText.text = currentEnergy.ToString();

        GameplayManager.Instance.GameTimeRemained.OnValueChanged += UpdateGameTimeBackground;
        GameplayManager.Instance.GameTimeRemained.OnValueChanged += (prevValue, newValue) => {
            if ((int)newValue != (int)prevValue)
                UpdateGameTimeText((int)newValue);
        };
        UpdateGameTimeText((int)GameplayManager.Instance.GameTimeRemained.Value);

        if (PlayersManager.Instance.OwnGameplayData != null)
            OnPlayerGameplayDataReceived();
        else
            PlayersManager.Instance.onDataReceived += OnPlayerGameplayDataReceived;
    }

    private void Update()
    {
        float energy = CombatUnitSpawner.Instance.CurrentEnergy;
        // Update energy bar.
        energyBar.value = energy / 10.0f;

        // Update energy amount text.
        if ((int)energy != currentEnergy)
        {
            currentEnergy = (int)energy;
            energyBarValueText.text = currentEnergy.ToString();
        }
    }

    public void OnMenuButtonPress()
    {
        menuUi.SetActive(true);
    }

    public void OnChangeCameraButton()
    {
        GameplayManager.Instance.ApplyNextCameraConfig();
    }

    private void UpdateGameTimeText(int seconds)
    {
        int minutes = seconds / 60;
        seconds %= 60;
        gameTimeText.text = $"{minutes}:{(seconds < 10 ? "0" : "")}{seconds}";
    }

    private void UpdateGameTimeBackground(float _, float gameTimeRemained)
    {
        if (gameTimeRemained <= 60.0f)
        {
            gameTimeBackground.color = Color.red;
            GameplayManager.Instance.GameTimeRemained.OnValueChanged -= UpdateGameTimeBackground;
        }
    }

    private void OnPlayerGameplayDataReceived()
    {
        enemyBarNameText.text = PlayersManager.Instance.EnemyPlayerName;

        var player1Obj = PlayersManager.Instance.Player1CombatUnitObj;
        var player2Obj = PlayersManager.Instance.Player2CombatUnitObj;
        ownPlayerUnit = PlayersManager.Instance.OwnGameplayData.playerNumber == 1 ? player1Obj : player2Obj;
        enemyPlayerUnit = PlayersManager.Instance.OwnGameplayData.playerNumber == 1 ? player2Obj : player1Obj;
        
        ownPlayerUnit.HealthPoints.OnValueChanged += OnOwnPlayerUnitHpChange;
        enemyPlayerUnit.HealthPoints.OnValueChanged += OnEnemyPlayerUnitHpChange;
    }

    private void OnOwnPlayerUnitHpChange(float prevValue, float newValue)
    {
        ownBarHpText.text = ((int)newValue).ToString();
        ownHpBar.value = 1 / (ownPlayerUnit.CurrentBasicStats.HealthPointsMax.Value / newValue);
    }

    private void OnEnemyPlayerUnitHpChange(float prevValue, float newValue)
    {
        enemyBarHpText.text = ((int)newValue).ToString();
        enemyHpBar.value = 1 / (enemyPlayerUnit.CurrentBasicStats.HealthPointsMax.Value / newValue);
    }

    private void OnDisable()
    {
        inWorldGameUi.SetActive(false);
    }
}
