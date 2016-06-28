using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class DeckStatusTextScript : MonoBehaviour {

	Text text;
    public Color32 fullColor;     //font color to use when the deck is full
    public Color32 emptyColor;    //font color to use when the deck is empty

	// Use this for initialization
	void Awake () {
		text = GetComponent<Text> ();
	}
	
	// Update is called once per frame
	void Update ()
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

        text.text = "Deck:\n" + 
            "<color=#" + lerpHex + ">" + DeckManagerScript.instance.cardsLeft + "</color>/" + DeckManagerScript.instance.deckSize;
	}

}
