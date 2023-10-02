using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Mail;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

public class SignUpManager : MonoBehaviour
{
    public static SignUpManager Instance { get; private set; }

    [SerializeField]
    private GameObject signUpUi;
    [SerializeField]
    private MainUi mainUiManager;
    [SerializeField]
    private TMP_InputField usernameInput;
    [SerializeField]
    private TMP_InputField passwordInput;
    [SerializeField]
    private TMP_InputField playerNameInput;
    [SerializeField]
    private TMP_InputField emailInput;
    [SerializeField]
    private GameObject messageText;
    [SerializeField]
    private Button submitButton;
    private List<uint> startDeck;  // list of units IDs

    private void Awake()
    {
        if (!IsSingletonInstance())
            return;

        startDeck = new List<uint>(){ 1, 2, 3, 4, 5, 6, 7, 8 };
        submitButton.onClick.AddListener(OnSubmitButtonPress);
        submitButton.onClick.AddListener(() => CoreUi.Instance.PlaySoundEffect(CoreUi.SoundEffect.BUTTON_CLICK));
    }

    private async void OnSubmitButtonPress()
    {
        submitButton.interactable = false;
        messageText.SetActive(false);

        bool isInputValid = ValidateInput();
        if (isInputValid)
        {
            bool succeeded = await SignUpUser();
            if (succeeded)
            {
                signUpUi.SetActive(false);
                mainUiManager.transform.parent.gameObject.SetActive(true);
                submitButton.interactable = true;
                return;
            }
        }

        messageText.SetActive(true);
        submitButton.interactable = true;
    }

    private async Task<bool> SignUpUser()
    {
        string startDeckEncoding = "";
        for (int i = 0; i < startDeck.Count - 1; ++i)
            startDeckEncoding += (startDeck[i].ToString() + "&");
        startDeckEncoding += startDeck[startDeck.Count - 1].ToString();

#if USING_LOCAL_SERVERS
        const string requestURL = "http://localhost/sign_up_user";
#else
        const string requestURL = "https:// - /sign_up_user";
#endif
        WWWForm form = new WWWForm();
        form.AddField("Username", usernameInput.text);
        form.AddField("Password", passwordInput.text);
        form.AddField("Player_name", playerNameInput.text);
        form.AddField("Email", emailInput.text);
        form.AddField("Start_deck", startDeckEncoding);
        form.AddField("Initial_gold", 3500);
        form.AddField("Initial_trophies", 0);

        UnityWebRequest req = UnityWebRequest.Post(requestURL, form);
        req.disposeUploadHandlerOnDispose = true;
        req.disposeDownloadHandlerOnDispose = true;
        AsyncOperation asyncOp = req.SendWebRequest();
        while (!asyncOp.isDone)
            await Task.Delay(100);

        bool succeeded = false;
        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("SignUp: Web request failed! " + req.error);
            messageText.GetComponentInChildren<TMP_Text>().text = "Oops, something went wrong. Try again later.";
        }
        else
        {
            string[] reqResponse = req.downloadHandler.text.Split('\t');
            if (reqResponse[0] == "1" && reqResponse.Length == 1)
            {
                var mainUiTxt = mainUiManager.MessageText.GetComponentInChildren<TMP_Text>();
                mainUiTxt.text = "Account created";
                mainUiTxt.color = Color.green;
                mainUiManager.MessageText.SetActive(true);
                mainUiManager.transform.parent.gameObject.SetActive(true);
                succeeded = true;
            }
            else if (reqResponse[0] == "2" && reqResponse.Length > 1)
                messageText.GetComponentInChildren<TMP_Text>().text = reqResponse[1];
            else
            {
                string error = (reqResponse[0] == "0" && reqResponse.Length > 1 ? reqResponse[1] : reqResponse[0]);
                Debug.Log($"SignUp error: {error}");
                messageText.GetComponentInChildren<TMP_Text>().text = "Oops, something went wrong. Try again later.";
            }
        }

        req.Dispose();
        return succeeded;
    }

    private bool ValidateInput()
    {
        Regex alphanumericRegex = new Regex("^[a-zA-Z0-9]*$");
        string invalidMessage = null;
        
        if (usernameInput.text.Length < 3)
            invalidMessage = "Username is too short";
        if (usernameInput.text.Length > 25)
            invalidMessage = "Username is too long";
        if (!alphanumericRegex.IsMatch(usernameInput.text))
            invalidMessage = "Username contains invalid characters";
        if (passwordInput.text.Length < 10)
            invalidMessage = "Password is too short";
        if (passwordInput.text.Length > 40)
            invalidMessage = "Password is too long";
        if (Encoding.UTF8.GetByteCount(passwordInput.text) != passwordInput.text.Length)  // password must contain only one byte chars
            invalidMessage = "Password contains invalid characters";
        if (playerNameInput.text.Length < 3)
            invalidMessage = "Player name is too short";
        if (playerNameInput.text.Length > 25)
            invalidMessage = "Player name is too long";
        if (playerNameInput.text.Trim() != playerNameInput.text)
            invalidMessage = "Player name starts or ends with white space characters";
        if (Encoding.UTF8.GetByteCount(playerNameInput.text) != playerNameInput.text.Length)  // player name must contain only one byte chars
            invalidMessage = "Player name contains invalid characters";
        if (!IsEmailAddressValid(emailInput.text))
            invalidMessage = "The given email address is invalid.";

        if (invalidMessage == null)
            return true;
        
        TMP_Text messageTextObj = messageText.GetComponentInChildren<TMP_Text>();
        messageTextObj.text = invalidMessage;
        return false;
    }

    private bool IsEmailAddressValid(string email)
    {
        if (email.Length == 0)
            return false;
        try
        {
            MailAddress temp = new MailAddress(email);
            return true;
        }
        catch (System.FormatException)
        {
            return false;
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
