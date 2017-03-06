using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Vexe.Runtime.Types;
using UnityEngine.EventSystems;

/// <summary>
/// provides static utilities to show dialog boxes or floating combat text to the player.  
/// Some dialogs return a result, but because of the nature of coroutines the results must be retrieved "manually" from the responseToLastPrompt field.
/// </summary>
public class MessageHandlerScript : BaseBehaviour
{
    //object references
    private bool shouldShowRefs() { return !Application.isPlaying; }
    [VisibleWhen("shouldShowRefs")] public GameObject    messageBox;                //game object containing everything used to show dialogs
    [VisibleWhen("shouldShowRefs")] public GameObject    volumeControls;            //parent game object for all volume controls
    [VisibleWhen("shouldShowRefs")] public Slider        SFXSliderComponent;        //slider for SFX volume
    [VisibleWhen("shouldShowRefs")] public Slider        MusicSliderComponent;      //slider for music volume
    [VisibleWhen("shouldShowRefs")] public Toggle        SFXMuteToggleComponent;    //toggle for muting SFX
    [VisibleWhen("shouldShowRefs")] public Toggle        MusicMuteToggleComponent;  //toggle for muting music
    [VisibleWhen("shouldShowRefs")] public AudioSource[] musicSources;              //audio sources to update when music volume changes
    [VisibleWhen("shouldShowRefs")] public AudioClip     SFXTestClip;               //clip to play when SFX volume changes
    [VisibleWhen("shouldShowRefs")] public Text          messageText;               //text object on the dialog box used to show the message itself
    [VisibleWhen("shouldShowRefs")] public GameObject    buttonA;                   //button used to allow the player to answer prompts
    [VisibleWhen("shouldShowRefs")] public GameObject    buttonB;                   //button used to allow the player to answer prompts
    [VisibleWhen("shouldShowRefs")] public GameObject    buttonC;                   //button used to allow the player to answer prompts
    [VisibleWhen("shouldShowRefs")] public GameObject    buttonD;                   //button used to allow the player to answer prompts
    [VisibleWhen("shouldShowRefs")] public GameObject    combatTextPrefab;          //prefab used to spawn floating combat text

    /// <summary>
    /// text of the button selected on the last prompt.  null if there was no previous prompt
    /// </summary>
    [Show] public static string responseToLastPrompt;

    //singleton instance
    public static MessageHandlerScript instance;

    //indicates whether or not a message is being displayed
    [Show] public bool messageBeingShown { get { return messageBox.activeInHierarchy; } }

    //init
    private void Start()
    {
        messageBox.SetActive(false);
        messageText.text = "No Message.";
        buttonA.SetActive(false);
        buttonB.SetActive(false);
        buttonC.SetActive(false);
        buttonD.SetActive(false);

        //load volume settings, if present, or use defaults if not
        SFXSliderComponent.value   = PlayerPrefs.GetFloat("SFX Volume",   0.75f);
        MusicSliderComponent.value = PlayerPrefs.GetFloat("Music Volume", 0.75f);
        SFXMuteToggleComponent.isOn   = (PlayerPrefs.GetInt("SFX Mute",   0) == 1);
        MusicMuteToggleComponent.isOn = (PlayerPrefs.GetInt("Music Mute", 0) == 1);

        instance = this;
    }

    //keep on top if a message is being shown
    private void Update()
    {
        if (messageBox.activeInHierarchy)
            transform.SetAsLastSibling();
    }

    //result handler
    private void TextButtonSelected(string response) { responseToLastPrompt = response; messageBox.SetActive(false); }

    /// <summary>
    /// [COROUTINE] handles messages whose only valid response is "OK".  
    /// pauses the game and does not return until the box is answered.
    /// the response can be retrieved from MessageHandlerScript.responseToLastPrompt
    /// </summary>
    /// <param name="message">the message to show</param>
    public static IEnumerator ShowAndYield(string message)
    {
        //set up the box
        instance.messageBox.SetActive(true);
        instance.volumeControls.SetActive(false);
        instance.messageText.text = message;
        instance.buttonA.SetActive(true);
        instance.buttonA.SendMessage("setButtonText", "OK");
        instance.buttonB.SetActive(false);
        instance.buttonC.SetActive(false);
        instance.buttonD.SetActive(false);
        responseToLastPrompt = null;

        //pause the game
        float oldTimeScale = Time.timeScale;
        Time.timeScale = 0.0f;

        //wait for the box to be closed
        while (responseToLastPrompt == null)
            yield return null;

        //allow the game to continue
        Time.timeScale = oldTimeScale;
    }

    /// <summary>
    /// [COROUTINE] handles messages that can be responded to with "Yes" and "No".  
    /// Pauses and yields until the prompt is answered.
    /// the response can be retrieved from MessageHandlerScript.responseToLastPrompt
    /// </summary>
    /// <param name="prompt">the prompt to show</param>
    public static IEnumerator PromptYesNo(string prompt)
    {
        //set up the box
        instance.messageBox.SetActive(true);
        instance.volumeControls.SetActive(false);
        instance.messageText.text = prompt;
        instance.buttonA.SetActive(true);
        instance.buttonA.SendMessage("setButtonText", "Yes");
        instance.buttonB.SetActive(true);
        instance.buttonB.SendMessage("setButtonText", "No");
        instance.buttonC.SetActive(false);
        instance.buttonD.SetActive(false);
        responseToLastPrompt = null;

        //pause the game
        float oldTimeScale = Time.timeScale;
        Time.timeScale = 0.0f;

        //wait for the box to be closed
        while (responseToLastPrompt == null)
            yield return null;

        //allow the game to continue
        Time.timeScale = oldTimeScale;
    }

    /// <summary>
    /// [COROUTINE] handles the Esc key during a mission.  Presents volume controls.
    /// Pauses and yields until the prompt is answered.
    /// the response can be retrieved from MessageHandlerScript.responseToLastPrompt
    /// </summary>
    public static IEnumerator ShowPauseMenu()
    {
        //set up the box
        instance.messageBox.SetActive(true);
        instance.volumeControls.SetActive(true);
        instance.messageText.text = "PAUSED";
        instance.buttonA.SetActive(true);
        instance.buttonA.SendMessage("setButtonText", "Continue");
        instance.buttonB.SetActive(true);
        instance.buttonB.SendMessage("setButtonText", "Quit Level");

        if (Application.platform == RuntimePlatform.WebGLPlayer || Application.isEditor)
            instance.buttonC.SetActive(false);
        else
        {
            instance.buttonC.SetActive(true);
            instance.buttonC.SendMessage("setButtonText", "Quit Game");
        }

        instance.buttonD.SetActive(false);
        responseToLastPrompt = null;

        //pause the game
        float oldTimeScale = Time.timeScale;
        Time.timeScale = 0.0f;

        //wait for the box to be closed
        while (responseToLastPrompt == null)
            yield return null;

        //allow the game to continue
        Time.timeScale = oldTimeScale;

        //save the new settings
        PlayerPrefs.SetFloat("SFX Volume",   instance.SFXVolume);
        PlayerPrefs.SetFloat("Music Volume", instance.MusicVolume);
        PlayerPrefs.SetInt("SFX Mute",   instance.SFXMute   ? 1 : 0);
        PlayerPrefs.SetInt("Music Mute", instance.MusicMute ? 1 : 0);
        PlayerPrefs.Save(); //technically this is unnecessary, but we have few values to save and this makes sure they get retained if the game crashes for some reason
    }

    /// <summary>
    /// [COROUTINE] Presents volume controls.
    /// Pauses and yields until the prompt is answered.
    /// the response can be retrieved from MessageHandlerScript.responseToLastPrompt
    /// </summary>
    public static IEnumerator ShowSettingsMenu()
    {
        //set up the box
        instance.messageBox.SetActive(true);
        instance.volumeControls.SetActive(true);
        instance.messageText.text = "You can also access these by pressing Esc during gameplay";
        instance.buttonA.SetActive(true);
        instance.buttonA.SendMessage("setButtonText", "Continue");
        instance.buttonB.SetActive(false);
        instance.buttonC.SetActive(false);
        instance.buttonD.SetActive(false);
        responseToLastPrompt = null;

        //pause the game
        float oldTimeScale = Time.timeScale;
        Time.timeScale = 0.0f;

        //wait for the box to be closed
        while (responseToLastPrompt == null)
            yield return null;

        //allow the game to continue
        Time.timeScale = oldTimeScale;

        //save the new settings
        PlayerPrefs.SetFloat("SFX Volume", instance.SFXVolume);
        PlayerPrefs.SetFloat("Music Volume", instance.MusicVolume);
        PlayerPrefs.SetInt("SFX Mute", instance.SFXMute ? 1 : 0);
        PlayerPrefs.SetInt("Music Mute", instance.MusicMute ? 1 : 0);
        PlayerPrefs.Save(); //technically this is unnecessary, but we have few values to save and this makes sure they get retained if the game crashes for some reason
    }

    //volume controls
    private float SFXVolume, MusicVolume;
    private bool  SFXMute, MusicMute;

    public void SFXSlider(BaseEventData eventData)
    {
        SFXVolume = SFXSliderComponent.value; //save the new setting

        //play a clip so the player can hear it at the new volume
        Camera.main.GetComponent<AudioSource>().clip = SFXTestClip;
        Camera.main.GetComponent<AudioSource>().volume = MessageHandlerScript.instance.SFXVolumeSetting;
        Camera.main.GetComponent<AudioSource>().Play();
    }

    public void MusicSlider(float setting)
    {
        MusicVolume = setting;
        foreach (AudioSource ms in musicSources)
            if (ms != null)
                ms.volume = MusicVolumeSetting;
    }

    public void SFXMuteToggle(bool setting)
    {
        SFXMute = setting; //save the new setting

        //play a clip so the player can hear it at the new volume
        if (instance != null) //skip if Start() hasnt finished yet, since that means this change was from loading the old setting at startup
        {
            Camera.main.GetComponent<AudioSource>().clip = SFXTestClip;
            Camera.main.GetComponent<AudioSource>().volume = MessageHandlerScript.instance.SFXVolumeSetting;
            Camera.main.GetComponent<AudioSource>().Play();
        }
    }

    public void MusicMuteToggle(bool setting)
    {
        MusicMute = setting;
        foreach (AudioSource ms in musicSources)
            if (ms != null)
                ms.volume = MusicVolumeSetting;
    }

    public float SFXVolumeSetting
    {
        get
        {
            if (SFXMute)
                return 0.0f;
            else
                return SFXVolume;
        }
    }

    public float MusicVolumeSetting
    {
        get
        {
            if (MusicMute)
                return 0.0f;
            else
                return MusicVolume;
        }
    }

    /// <summary>
    /// calls ShowAndYield but returns without waiting for a response.  Useful for places (like Update()) where the engine does not allow you to yield
    /// </summary>
    /// <param name="message"></param>
    public static void ShowNoYield(string message) { instance.StartCoroutine(ShowAndYield(message)); }

    /// <summary>
    /// spawns floating combat text to represent an enemy having damaged the player
    /// </summary>
    /// <param name="enemyWorldPosition">world position to spawn the text at</param>
    /// <param name="damageDealt">amount of damage to show</param>
    public void spawnPlayerDamageText (Vector2 enemyWorldPosition, int damageDealt)
    {
        GameObject combatText = (GameObject)Instantiate(combatTextPrefab, enemyWorldPosition, Quaternion.identity);
        combatText.transform.SetParent(PathManagerScript.instance.transform.parent, true);
        combatText.SendMessage("damageText", (-damageDealt).ToString());
    }
}
