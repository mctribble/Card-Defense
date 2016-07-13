using UnityEngine;
using UnityEngine.UI;
using Vexe.Runtime.Types;

public class ChargeStatusScript : BaseBehaviour
{
    private Text text;
    public Color32 fullColor;     //font color to use when the deck is full
    public Color32 emptyColor;    //font color to use when the deck is empty

    // Use this for initialization
    private void Start()
    {
        text = GetComponent<Text>();
        LevelManagerScript.instance.LevelLoadedEvent += LevelLoadedHandler;
    }

    // event handler for LevelManagerScript.instance.LevelLoadedEvent
    private void LevelLoadedHandler()
    {
        text.raycastTarget = true; //alloww this object to capture mouse events only after the level has loaded
    }

    // Update is called once per frame
    private void Update()
    {
        //skip if level not loaded
        if (!LevelManagerScript.instance.levelLoaded)
            return;

        //interpolate between fullColor and emptyColor depending on how many cards are left out of the full amount
        Color32 lerpColor = Color32.Lerp(emptyColor, fullColor, ((float)DeckManagerScript.instance.cardsLeft / (float)DeckManagerScript.instance.deckSize));

        //convert result to hex for text formatting
        string lerpHex =
            lerpColor.r.ToString("X2") +
            lerpColor.g.ToString("X2") +
            lerpColor.b.ToString("X2") +
            lerpColor.a.ToString("X2");

        text.text = "Charges: " +
            "<color=#" + lerpHex + ">" + DeckManagerScript.instance.curDeckCharges + "</color>/" + DeckManagerScript.instance.maxDeckCharges;
    }
}