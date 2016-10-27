﻿using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Vexe.Runtime.Types;

/// <summary>
/// what kind of information is associated with the button
/// </summary>
public enum MenuButtonType
{
    level, 
    deck,
    card,
    text
}

/// <summary>
/// a versatile menu button that sends a message back to its parent when clicked
/// </summary>
public class MenuButtonScript : BaseBehaviour
{
    public Text           buttonText; //text of this button
    public MenuButtonType buttonType; //enum that represents how this menu button is being used

    //each of these may be null based on button type
    public FileInfo levelFile;  //level file attached to this button, if any
    public XMLDeck  xDeck;      //deck attached to this button, if any
    public CardData card;       //card type attached to this button, if any

    /// <summary>
    /// the button is set up to correspond to the given level
    /// </summary>
    private void setLevel(FileInfo file)
    {
        levelFile = file;   //set file name
        buttonText.text = file.Name;    //set button text
        buttonText.text = buttonText.text.Remove(buttonText.text.Length - 4); //remove the '.xml' from the button text
        buttonType = MenuButtonType.level;
    }

    /// <summary>
    /// the button is set up to correspond to the given XMLDeck
    /// </summary>
    private void setDeck(XMLDeck newXDeck)
    {
        xDeck = newXDeck; //set deck
        buttonText.text = xDeck.name;
        buttonType = MenuButtonType.deck;
    }

    /// <summary>
    /// the button is set up to correspond to the given CardData
    /// </summary>
    private void setCard(CardData newCard)
    {
        card = newCard;
        buttonText.text = newCard.cardName;
        buttonType = MenuButtonType.card;
    }

    /// <summary>
    /// sets up the button by setting the text directly (note: only use on text buttons, as the other types set the text automatically)
    /// </summary>
    private void setButtonText(string text)
    {
        buttonText.text = text;
        buttonType = MenuButtonType.text;
    }

    /// <summary>
    /// sets the color for this button
    /// </summary>
    private void setColor(Color c)
    {
        GetComponent<Image>().color = c;
    }

    /// <summary>
    /// reports back to the parent object in a slightly different way for each button type
    /// </summary>
    private void buttonClicked()
    {
        switch (buttonType)
        {
            case MenuButtonType.level:
                SendMessageUpwards("LevelSelected", levelFile);
                break;
            case MenuButtonType.deck:
                SendMessageUpwards("DeckSelected", xDeck);
                break;
            case MenuButtonType.card:
                SendMessageUpwards("CardSelected", card);
                break;
            case MenuButtonType.text:
                SendMessageUpwards("TextButtonSelected", buttonText.text);
                break;
            default:
                MessageHandlerScript.Error("MenuButtonScript cant handle this button type!");
                break;
        }
    }
}