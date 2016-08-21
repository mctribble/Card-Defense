using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Vexe.Runtime.Types;

//do-nothing struct containing static rules for deck building
public class DeckRules
{
    public const ushort MAX_CARDS_OF_SAME_TYPE = 10;
    public const ushort MAX_CARDS_IN_DECK = 60;
    public const ushort MIN_CARDS_IN_DECK = 30;

    private DeckRules() { } //hide constructor because this is just a container for constants
}

public class DeckEditorDeckListScript : BaseBehaviour
{
    public GameObject buttonPrefab; //used to instantiate deck buttons
    public Color defaultColor;   //normal button color
    public Color moddedColor;    //color of button for modded decks
    public Color highlightColor; //color of highlighted button
    public Color menuColor;      //color of menu buttons

    private List<GameObject> buttons;

	// Use this for initialization
	void Start ()
    {
        //init
        buttons = new List<GameObject>();

        //purge dev placeholders
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        //load up the contents of the list
        setupDeckButtons(null);
	}

    //adds deck buttons to the list.  If highlightDeck is in the list, that button is a different color
    void setupDeckButtons(XMLDeck highlightDeck)
    {
        //one button for each player deck
        foreach (XMLDeck xDeck in DeckManagerScript.instance.playerDecks.decks)
        {
            GameObject xButton = Instantiate(buttonPrefab);
            xButton.SendMessage("setDeck", xDeck);

            //set button color
            Color targetColor;

            if (xDeck.isModded())
                targetColor = moddedColor;
            else
                targetColor = defaultColor;
            if (xDeck == highlightDeck)
                targetColor = Color.Lerp(targetColor, highlightColor, 0.5f);

            xButton.SendMessage("setColor", targetColor);
            xButton.transform.SetParent(this.transform, false);
            buttons.Add(xButton);
        }

        //another button for making a new deck
        GameObject ndButton = Instantiate(buttonPrefab);
        ndButton.SendMessage("setButtonText", "New Deck");
        ndButton.SendMessage("setColor", menuColor);
        ndButton.transform.SetParent(this.transform, false);
        buttons.Add(ndButton);
    }

    //purges the buttons from the list
    void destroyDeckButtons()
    {
        foreach (GameObject button in buttons)
            Destroy(button);
        buttons.Clear();
    }

    //refreshes the list, highlighting the current deck
    void refresh(XMLDeck currentDeck)
    {
        destroyDeckButtons();
        setupDeckButtons(currentDeck);
    }
}
