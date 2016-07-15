using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DeckEditorCardTypeListScript : MonoBehaviour
{
    public GameObject buttonPrefab; //used to instantiate deck buttons
    public Color defaultColor;   //normal button color
    public Color highlightColor; //color of highlighted button

    private List<GameObject> buttons;

    // Use this for initialization
    void Start()
    {
        //init
        buttons = new List<GameObject>();

        //purge dev placeholders
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        //load up the contents of the list
        setupDeckButtons(null);
    }

    //adds card buttons to the list.  If the card type is in highlightDeck, that button is a different color
    void setupDeckButtons(XMLDeck highlightDeck)
    {
        //one button for each card type
        foreach (CardData type in CardTypeManagerScript.instance.types.cardTypes)
        {
            //create the button and add it to the list
            GameObject xButton = Instantiate(buttonPrefab);
            xButton.SendMessage("setCard", type);
            xButton.transform.SetParent(this.transform, false);
            buttons.Add(xButton);

            //set its color based on its presence (or lack thereof) in highlightDeck
            Color buttonColor = defaultColor;
            if (highlightDeck != null)
            {
                foreach (XMLDeckEntry e in highlightDeck.contents)
                {
                    if (e.name == type.cardName)
                    {
                        buttonColor = highlightColor;
                        break;
                    }
                }
            }
            xButton.SendMessage("setColor", buttonColor);
        }
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
