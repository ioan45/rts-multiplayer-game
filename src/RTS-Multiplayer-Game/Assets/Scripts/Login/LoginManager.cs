using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class LoginManager : MonoBehaviour
{
    public static LoginManager Instance { get; private set; }

    private const float cameraRotationSpeed = 3;  // angles per second

    [SerializeField]
    private GameObject globalCanvas;
    [SerializeField]
    private MainUi mainUiManager;
    [SerializeField]
    private GameObject signInUi;
    [SerializeField]
    private GameObject signUpUi;
    [SerializeField]
    private GameObject creditsUi;
    [SerializeField]
    private GameObject quitUi;
    private Camera mainCamera;
    private bool canRotateCamera;

    private void Awake()
    {
        if (!IsSingletonInstance())
            return;

        mainCamera = CoreUi.Instance.mainCamera;
        canRotateCamera = false;
        globalCanvas.SetActive(true);
        signInUi.SetActive(false);
        signUpUi.SetActive(false);
        creditsUi.SetActive(false);
        quitUi.SetActive(false);
        mainUiManager.transform.parent.gameObject.SetActive(true);
    }

    private async void Start()
    {
        InitBackground();

        var (operationSucceeded, sessionUserData) = await CheckIfSessionExists();
        if (sessionUserData != null)
            SignInManager.Instance.SignInUser(sessionUserData);
        else
        {
            if (!operationSucceeded)
            {
                var mainUiTxt = mainUiManager.MessageText.GetComponentInChildren<TMP_Text>();
                mainUiTxt.text = "Couldn't verify if there is a session being active";
                mainUiTxt.color = Color.red;
                mainUiManager.MessageText.SetActive(true);
            }
            canRotateCamera = true;
            var audioComp = GetComponent<AudioSource>();
            audioComp.loop = true;
            audioComp.volume = CoreUi.Instance.SoundVolume / 100.0f;
            audioComp.Play();
            CoreUi.Instance.LoadingScreen.SetActive(false);
        }
    }

    private void FixedUpdate()
    {
        if (canRotateCamera)
            mainCamera.transform.Rotate(new Vector3(0, cameraRotationSpeed * Time.deltaTime, 0), Space.World);
    }

    private async Task<Tuple<bool, UserData>> CheckIfSessionExists()
    {
        string sessionToken = PlayerPrefs.GetString("UserSessionToken", null);
        if (sessionToken == null)
            return new Tuple<bool, UserData>(true, null);
        
#if USING_LOCAL_SERVERS
        const string requestURL = "http://localhost/check_if_session_exists";
#else
        const string requestURL = "https:// - /check_if_session_exists";
#endif
        WWWForm form = new WWWForm();
        form.AddField("SessionToken", sessionToken);

        UnityWebRequest req = UnityWebRequest.Post(requestURL, form);
        req.disposeUploadHandlerOnDispose = true;
        req.disposeDownloadHandlerOnDispose = true;
        AsyncOperation asyncOp = req.SendWebRequest();
        while (!asyncOp.isDone)
            await Task.Delay(100);

        bool opSucceeded = false;
        UserData data = null;
        if (req.result != UnityWebRequest.Result.Success)
            Debug.Log("OnStartupCheckIfSessionIsActive: Web request failed! " + req.error);
        else
        {
            opSucceeded = true;
            string[] reqResponse = req.downloadHandler.text.Split('\t');
            if (reqResponse[0] == "1" && reqResponse.Length == 8)
            {
                data = new UserData();
                data.username = reqResponse[1];
                data.playerName = reqResponse[2];
                Int32.TryParse(reqResponse[3], out data.gold);
                Int32.TryParse(reqResponse[4], out data.trophies);
                string[] ownedUnitsIds = reqResponse[5].Trim().Split('&');
                string[] ownedUnitsLevels = reqResponse[6].Trim().Split('&');
                string[] deckUnitsIds = reqResponse[7].Trim().Split('&');
                SignInManager.Instance.FillWithOwnedUnits(ref data, ownedUnitsIds, ownedUnitsLevels, deckUnitsIds);
            }
        }

        req.Dispose();
        return new Tuple<bool, UserData>(opSucceeded, data);
    }

    private void InitBackground()
    {
        Vector3 lightPosition = new Vector3(92, 156, -93);
        Vector3 lightRotationAngles = new Vector3(60, 0, 0);
        Vector3 cameraPos = new Vector3(-32, 267, -56);
        Vector3 cameraInitRotation = new Vector3(39, 0, 0);

        Light light = CoreManager.Instance.LightComponent;
        light.transform.position = lightPosition;
        light.transform.rotation = Quaternion.identity;
        light.transform.Rotate(lightRotationAngles);

        mainCamera.transform.position = cameraPos;
        mainCamera.transform.rotation = Quaternion.identity;
        mainCamera.transform.Rotate(cameraInitRotation);
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
