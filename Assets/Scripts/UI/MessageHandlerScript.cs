using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Vexe.Runtime.Types;

//provides static utilities to show dialog boxes to the player and return a result
public class MessageHandlerScript : MonoBehaviour
{
    //object references
    [Inline] public GameObject messageBox;
    [Inline] public Text       messageText;
    [Inline] public GameObject buttonA;
    [Inline] public GameObject buttonB;
    [Inline] public GameObject buttonC;
    [Inline] public GameObject buttonD;

    [Show] public static string responseToLastPrompt;
    public static MessageHandlerScript instance;

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
	private void TextButtonSelected(string response) { responseToLastPrompt = response; messageBox.SetActive(false); Debug.Log(response); }

    //handles messages whose only valid response is "OK".  does not return until the box is answered
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

        //pause the game
        float oldTimeScale = Time.timeScale;
        Time.timeScale = 0.0f;

        //wait for the box to be closed
        while (responseToLastPrompt == null)
            yield return null;

        //allow the game to continue
        Time.timeScale = oldTimeScale;
    }

    //handles messages that can be responded to with "Yes" and "No".  Yields until the prompt is answered.
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

        //pause the game
        float oldTimeScale = Time.timeScale;
        Time.timeScale = 0.0f;

        //wait for the box to be closed
        while (responseToLastPrompt == null)
            yield return null;

        //allow the game to continue
        Time.timeScale = oldTimeScale;
    }

    //helper: calls ShowAndYield but returns without waiting for a response.  Useful for places (like Update()) where the engine does not allow you to yield
    public static void ShowNoYield(string message) { instance.StartCoroutine(ShowAndYield(message)); }

    //helper: like ShowNoYield, but reroutes the message to the log if we are running a debug build
    public static void Warning(string message)
    {
        if (Debug.isDebugBuild)
            Debug.LogWarning(message);
        else
            MessageHandlerScript.ShowNoYield("WARNING: " + message);
    }

    //helper: like ShowNoYield, but duplicates to the log if we are running a debug build
    public static void Error(string message)
    {
        if (Debug.isDebugBuild)
            Debug.LogError(message);

        MessageHandlerScript.ShowNoYield("ERROR: " + message);
    }
}
