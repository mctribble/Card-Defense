using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class DeckStatusTextScript : MonoBehaviour {

	Text text;

	// Use this for initialization
	void Awake () {
		text = GetComponent<Text> ();
	}
	
	// Update is called once per frame
	void Update () {
		//skip if level not loaded
		if (!LevelManagerScript.instance.levelLoaded)
			return;

		text.text = "Deck:\n" + DeckManagerScript.instance.cardsLeft + "/" + DeckManagerScript.instance.deckSize;
	}

}
