using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Vexe.Runtime.Types;
using System.Linq;

/// <summary>
/// do-nothing struct containing static rules for deck building
/// </summary>
public class DeckRules
{
    public const ushort MAX_CARDS_OF_SAME_TYPE = 10;
    public const ushort MAX_CARDS_IN_DECK = 60;
    public const ushort MIN_CARDS_IN_DECK = 30;

    private DeckRules() { } //hide constructor because this is just a container for constants
}

/// <summary>
/// used to show the available decks in the deck editor
/// </summary>
public class DeckEditorDeckListScript : BaseBehaviour
{
    public GameObject buttonPrefab; //used to instantiate deck buttons
    public Color defaultColor;   //normal button color
    public Color moddedColor;    //color of button for modded decks
    public Color highlightColor; //color of highlighted button
    public Color menuColor;      //color of menu buttons

    private List<MenuButtonScript> buttons;

	// Use this for initialization
	IEnumerator Start ()
    {
        //wait for card type manager to be ready
        while ( (CardTypeManagerScript.instance == null) || (CardTypeManagerScript.instance.areTypesLoaded() == false) ) 
            yield return null;

        //init
        buttons = new List<MenuButtonScript>();

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
            MenuButtonScript xButton = Instantiate(buttonPrefab).GetComponent<MenuButtonScript>();
            xButton.setDeck(xDeck);

            //set button color
            Color targetColor;

            if (xDeck.isModded())
                targetColor = moddedColor;
            else
                targetColor = defaultColor;
            if (xDeck == highlightDeck)
                targetColor = Color.Lerp(targetColor, highlightColor, 0.5f);

            xButton.setColor(targetColor);
            xButton.transform.SetParent(this.transform, false);
            buttons.Add(xButton);
        }

        //another button for making a new deck
        MenuButtonScript ndButton = Instantiate(buttonPrefab).GetComponent<MenuButtonScript>();
        ndButton.setButtonText("New Deck");
        ndButton.setColor(menuColor);
        ndButton.transform.SetParent(this.transform, false);
        buttons.Add(ndButton);
    }

    //purges the buttons from the list
    void destroyDeckButtons()
    {
        foreach (MenuButtonScript button in buttons)
            Destroy(button.gameObject);
        buttons.Clear();
    }

    //refreshes the list, highlighting the current deck
    void refresh(XMLDeck currentDeck)
    {
        //remove all deck buttons that no longer have a corresponding deck
        foreach (MenuButtonScript toRemove in buttons.FindAll(mb => (mb.buttonType == MenuButtonType.deck) && 
                                                                    (DeckManagerScript.instance.playerDecks.decks.Contains(mb.xDeck) == false)))
        {
            buttons.Remove(toRemove);
            Destroy(toRemove.gameObject);
        }

        //create buttons for decks that do not have a corresponding button
        foreach (XMLDeck newDeck in DeckManagerScript.instance.playerDecks.decks)
        {
            if (buttons.Any(mb => mb.xDeck == newDeck) == false)
            {
                MenuButtonScript xButton = Instantiate(buttonPrefab).GetComponent<MenuButtonScript>();
                xButton.SendMessage("setDeck", newDeck);

                //set button color
                Color targetColor;

                if (newDeck.isModded())
                    targetColor = moddedColor;
                else
                    targetColor = defaultColor;

                if (newDeck == currentDeck)
                    targetColor = Color.Lerp(targetColor, highlightColor, 0.5f);

                xButton.setColor(targetColor);
                xButton.transform.SetParent(this.transform, false);
                buttons.Add(xButton);
            }
        }
    }
}
