using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Vexe.Runtime.Types;

/// <summary>
/// provides static utilities to show dialog boxes or floating combat text to the player.  
/// Some dialogs return a result, but because of the nature of coroutines the results must be retrieved "manually" from the responseToLastPrompt field.
/// </summary>
public class MessageHandlerScript : BaseBehaviour
{
    //object references
    private bool shouldShowRefs() { return !Application.isPlaying; }
    [VisibleWhen("shouldShowRefs")] public GameObject messageBox;
    [VisibleWhen("shouldShowRefs")] public Text       messageText;
    [VisibleWhen("shouldShowRefs")] public GameObject buttonA;
    [VisibleWhen("shouldShowRefs")] public GameObject buttonB;
    [VisibleWhen("shouldShowRefs")] public GameObject buttonC;
    [VisibleWhen("shouldShowRefs")] public GameObject buttonD;
    [VisibleWhen("shouldShowRefs")] public GameObject combatTextPrefab;

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
        instance = this;
        messageBox.SetActive(false);
        messageText.text = "No Message.";
        buttonA.SetActive(false);
        buttonB.SetActive(false);
        buttonC.SetActive(false);
        buttonD.SetActive(false);
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
        instance.messageText.text = message;
        instance.buttonA.SetActive(true);
        instance.buttonA.SendMessage("setButtonText", "OK");
        instance.buttonB.SetActive(false);
        instance.buttonC.SetActive(false);
        instance.buttonD.SetActive(false);
        responseToLastPrompt = null;
        instance.gameObject.transform.SetAsLastSibling();

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
        instance.messageText.text = prompt;
        instance.buttonA.SetActive(true);
        instance.buttonA.SendMessage("setButtonText", "Yes");
        instance.buttonB.SetActive(true);
        instance.buttonB.SendMessage("setButtonText", "No");
        instance.buttonC.SetActive(false);
        instance.buttonD.SetActive(false);
        responseToLastPrompt = null;
        instance.gameObject.transform.SetAsLastSibling();

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
