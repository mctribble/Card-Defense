using UnityEngine;
using UnityEngine.UI;
using Vexe.Runtime.Types;

/// <summary>
/// shows status of the deck in the status bar
/// </summary>
public class DeckStatusTextScript : BaseBehaviour
{
    //references
    public Text text;
    public Image gaugeBG;
    public Image gaugeFG;

    //colors
    public Color32 fullColor;  //font color to use when the deck is full
    public Color32 emptyColor; //font color to use when the deck is empty

    // Use this for initialization
    private void Start()
    {
        LevelManagerScript.instance.LevelLoadedEvent += LevelLoadedHandler;
    }

    // event handler for LevelManagerScript.instance.LevelLoadedEvent
    private void LevelLoadedHandler()
    {
        //allow this object to capture mouse events only after the level has loaded
        text.raycastTarget = true;
        gaugeBG.raycastTarget = true;
        gaugeFG.raycastTarget = true;
    }

    // Update is called once per frame
    private void Update()
    {
        //skip if level not loaded
        if (!LevelManagerScript.instance.levelLoaded)
            return;

        //interpolate between fullColor and emptyColor depending on how many cards are left out of the full amount
        float fillRatio = (float)DeckManagerScript.instance.cardsLeft / (float)DeckManagerScript.instance.deckSize;
        Color lerpColor = Color.Lerp(emptyColor, fullColor, fillRatio);

        //update gauge
        gaugeFG.fillAmount = fillRatio;
        gaugeFG.color = lerpColor; 
    }
}