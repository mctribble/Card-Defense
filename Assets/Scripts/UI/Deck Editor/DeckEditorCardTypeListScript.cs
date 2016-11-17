﻿using System.Collections.Generic;
using UnityEngine;
using Vexe.Runtime.Types;

/// <summary>
/// list of card types in the deck editor
/// </summary>
public class DeckEditorCardTypeListScript : BaseBehaviour
{
    public GameObject entryPrefab; //used to instantiate deck buttons

    //color settings
    public Color towerColor;     //tower cards
    public Color upgradeColor;   //upgrade cards
    public Color spellColor;     //spell cards
    public Color highlightColor; //overlaid if this card is in the open deck

    private List<GameObject> buttons;
    private DeckEditorFilter filter;

    // Use this for initialization
    private void Start()
    {
        //init
        buttons = new List<GameObject>();

        //purge dev placeholders
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        //register to be informed about type reloads
        CardTypeManagerScript.instance.cardTypesReloadedEvent += cardTypesReloaded;

        //load up the contents of the list
        setupTypeEntries(null);
    }

    //called when card types are reloaded for some reason (probably from the inspector)
    private void cardTypesReloaded(CardTypeCollection newTypes)
    {
        destroyTypeEntries();
        setupTypeEntries(GetComponentInParent<DeckEditorMainScript>().openDeck);
    }

    //adds card entries to the list.  If the card type is in highlightDeck, that button is a different color
    private void setupTypeEntries(XMLDeck highlightDeck)
    {
        //if a filter is set, operate on a list sorted and filtered according to the current settings instead of the full list
        List<PlayerCardData> listToShow = CardTypeManagerScript.instance.types.cardTypes;
        if (filter != null)
            listToShow = filter.filterAndSortCardData(listToShow);

        //one entry for each card type
        foreach (PlayerCardData type in listToShow)
        {
            //skip tokens
            if (type.isToken)
                continue;

            //create the button and add it to the list
            GameObject xButton = Instantiate(entryPrefab);
            xButton.SendMessage("setCard", type);
            xButton.transform.SetParent(this.transform, false);
            buttons.Add(xButton);

            //set its color based on its type
            Color buttonColor;
            switch (type.cardType)
            {
                case PlayerCardType.tower:   buttonColor = towerColor;   break;
                case PlayerCardType.upgrade: buttonColor = upgradeColor; break;
                case PlayerCardType.spell:   buttonColor = spellColor;   break;
                default:               Debug.LogWarning("card type list doesnt know what color to use for this card."); buttonColor = Color.black; break;
            }

            //highlight if it is in the deck
            if (highlightDeck != null)
            {
                foreach (XMLDeckEntry e in highlightDeck.contents)
                {
                    if (e.name == type.cardName)
                    {
                        buttonColor = Color.Lerp(buttonColor, highlightColor, 0.5f);
                        break;
                    }
                }
            }
            xButton.SendMessage("setColor", buttonColor);
        }
    }

    //purges the buttons from the list
    private void destroyTypeEntries()
    {
        foreach (GameObject button in buttons)
            Destroy(button);
        buttons.Clear();
    }

    //refreshes the list, highlighting the current deck
    public void refresh(XMLDeck currentDeck)
    {
        destroyTypeEntries();
        setupTypeEntries(currentDeck);
    }

    //saves new filter settings
    public void filterChanged(DeckEditorFilter newFilter)
    {
        filter = newFilter;
    }
}