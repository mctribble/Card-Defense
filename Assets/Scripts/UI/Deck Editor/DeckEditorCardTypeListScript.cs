using System.Collections.Generic;
using System.Linq;
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

    private List<DeckEditorCardTypeEntryScript> buttons;
    private DeckEditorFilter filter;

    // Use this for initialization
    private void Start()
    {
        //init
        buttons = new List<DeckEditorCardTypeEntryScript>();

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
            DeckEditorCardTypeEntryScript xButton = Instantiate(entryPrefab).GetComponent<DeckEditorCardTypeEntryScript>();
            xButton.setCard(type);
            xButton.transform.SetParent(this.transform, false);
            buttons.Add(xButton);

            //set its color based on its type
            Color buttonColor;
            switch (type.cardType)
            {
                case PlayerCardType.tower:   buttonColor = towerColor;   break;
                case PlayerCardType.upgrade: buttonColor = upgradeColor; break;
                case PlayerCardType.spell:   buttonColor = spellColor;   break;
                default: Debug.LogWarning("card type list doesnt know what color to use for this card."); buttonColor = Color.black; break;
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
            xButton.setColor(buttonColor);
        }
    }

    //purges the buttons from the list
    private void destroyTypeEntries()
    {
        foreach (DeckEditorCardTypeEntryScript button in buttons)
            Destroy(button.gameObject);
        buttons.Clear();
    }

    //refreshes the list, highlighting the current deck
    public void refresh(XMLDeck currentDeck)
    {
        //if a filter is set, operate on a list sorted and filtered according to the current settings instead of the full list
        List<PlayerCardData> listToShow = CardTypeManagerScript.instance.types.cardTypes;
        if (filter != null)
            listToShow = filter.filterAndSortCardData(listToShow);

        //dont include tokens
        listToShow.RemoveAll(pcd => pcd.isToken);

        //create/remove entries as needed so that we have as many as we need to show.  data will be set in a second pass.
        while (buttons.Count < listToShow.Count)
        {
            DeckEditorCardTypeEntryScript newButton = Instantiate(entryPrefab).GetComponent<DeckEditorCardTypeEntryScript>();
            newButton.transform.SetParent(this.transform, false);
            buttons.Add(newButton);
        }
        while (buttons.Count > listToShow.Count)
        {
            DeckEditorCardTypeEntryScript toRemove = buttons[0];
            buttons.Remove(toRemove);
            Destroy(toRemove.gameObject);
        }

        //update entries
        for (int i = 0; i < buttons.Count; i++)
        {
            //ensure it has the right data
            if (buttons[i].type != listToShow[i])
                buttons[i].setCard(listToShow[i]);

            //set its color based on its type
            Color buttonColor;
            switch (buttons[i].type.cardType)
            {
                case PlayerCardType.tower: buttonColor = towerColor; break;
                case PlayerCardType.upgrade: buttonColor = upgradeColor; break;
                case PlayerCardType.spell: buttonColor = spellColor; break;
                default: Debug.LogWarning("card type list doesnt know what color to use for this card."); buttonColor = Color.black; break;
            }

            //highlight if it is in the deck
            if (currentDeck != null)
                if (currentDeck.contents.Any(xde => xde.name == buttons[i].type.cardName))
                    buttonColor = Color.Lerp(buttonColor, highlightColor, 0.5f);

            buttons[i].setColor(buttonColor);
        }
    }

    //saves new filter settings
    public void filterChanged(DeckEditorFilter newFilter)
    {
        filter = newFilter;
    }
}