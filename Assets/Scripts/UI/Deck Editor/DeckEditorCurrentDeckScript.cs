using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Vexe.Runtime.Types;
using System.Linq;

/// <summary>
/// shows contents of the current deck in the deck editor
/// </summary>
public class DeckEditorCurrentDeckScript : BaseBehaviour
{
    //color settings
    public Color towerColor;     //tower cards
    public Color upgradeColor;   //upgrade cards
    public Color spellColor;     //spell cards
    public Color highlightColor; //overlaid if this card is in the open deck

    public GameObject currentDeckEntryPrefab; //used to instantiate deck list entries
    public XMLDeck    data; //current deck

    private List<DeckEditorCurrentDeckEntryScript> deckEntries;
    private DeckEditorFilter filter;

	// Use this for initialization
	void Awake ()
    {
        //init
        deckEntries = new List<DeckEditorCurrentDeckEntryScript>();

        //purge dev placeholders
        foreach (Transform child in transform)
            Destroy(child.gameObject);
    }

    //sets the deck
    public void SetDeck(XMLDeck deck)
    {
        data = deck;
        destroyDeckEntries();
        setupDeckEntries();
    }

    //sets the filter
    public void filterChanged(DeckEditorFilter newFilter)
    {
        filter = newFilter;
    }

    //purges the list
    private void destroyDeckEntries()
    {
        foreach (DeckEditorCurrentDeckEntryScript entry in deckEntries)
            Destroy(entry.gameObject);
    }

    //populates the list
    private void setupDeckEntries()
    {
        //if a filter is set, use the sorted list instead of the full list
        List<XMLDeckEntry> listToSearch = data.contents;
        if (filter != null)
            listToSearch = filter.sortXMLDeckEntries(listToSearch);

        foreach (XMLDeckEntry xEntry in listToSearch)
        {
            //create the entry and add it to the list
            DeckEditorCurrentDeckEntryScript entry = Instantiate(currentDeckEntryPrefab).GetComponent<DeckEditorCurrentDeckEntryScript>();
            entry.setData(xEntry);
            entry.transform.SetParent(this.transform, false);
            deckEntries.Add(entry);

            //set its color based on its type
            Color buttonColor;
            switch (CardTypeManagerScript.instance.getCardByName(xEntry.name).cardType)
            {
                case PlayerCardType.tower:   buttonColor = towerColor;   break;
                case PlayerCardType.upgrade: buttonColor = upgradeColor; break;
                case PlayerCardType.spell:   buttonColor = spellColor;   break;

                default:
                    Debug.LogWarning("current deck list doesnt know what color to use for this card. (" + xEntry.name + ")");
                    buttonColor = Color.black;
                    break;
            }

            //highlight if it does not match the current filter
            if (filter != null)
            {
                if (filter.match(xEntry) == false)
                    buttonColor = Color.Lerp(buttonColor, highlightColor, 0.5f);
            }
            entry.SendMessage("setColor", buttonColor);
        }
    }

    //refreshes the list.  If deck is not null, also sets that as the new deck
    public void refresh(XMLDeck deck)
    {
        if (deck != null)
            data = deck;

        //update existing entries as needed and keep a list of which ones no longer exist
        List<DeckEditorCurrentDeckEntryScript> toRemove = new List<DeckEditorCurrentDeckEntryScript>();
        foreach (DeckEditorCurrentDeckEntryScript entry in deckEntries)
        {
            XMLDeckEntry xde = data.contents.FirstOrDefault(x => x.name == entry.cardName);
            if (xde != null)
            {
                //this entry still corresponds to a card that is in the deck.  Update it

                //update count
                if (entry.cardCount != xde.count)
                    entry.cardCount = xde.count;

                //figure out what color it should be
                Color buttonColor;
                switch (CardTypeManagerScript.instance.getCardByName(xde.name).cardType)
                {
                    case PlayerCardType.tower: buttonColor = towerColor; break;
                    case PlayerCardType.upgrade: buttonColor = upgradeColor; break;
                    case PlayerCardType.spell: buttonColor = spellColor; break;

                    default:
                        Debug.LogWarning("current deck list doesnt know what color to use for this card. (" + xde.name + ")");
                        buttonColor = Color.black;
                        break;
                }

                //highlight if it does not match the current filter
                if (filter != null)
                {
                    if (filter.match(xde) == false)
                        buttonColor = Color.Lerp(buttonColor, highlightColor, 0.5f);
                }

                //update color
                entry.setColor(buttonColor);
            }
            else
            {
                //the card for this entry is no longer in the deck
                toRemove.Add(entry);
            }
        }

        //get rid of the ones that no longer exist
        foreach (DeckEditorCurrentDeckEntryScript e in toRemove)
        {
            deckEntries.Remove(e);
            Destroy(e.gameObject);
        }

        //create new entries for cards that dont have them yet
        foreach (XMLDeckEntry xEntry in data.contents)
        {
            if (deckEntries.Any(de => de.cardName == xEntry.name) == false)
            {
                //create the entry and add it to the list
                DeckEditorCurrentDeckEntryScript entry = Instantiate(currentDeckEntryPrefab).GetComponent<DeckEditorCurrentDeckEntryScript>();
                entry.setData(xEntry);
                entry.transform.SetParent(this.transform, false);
                deckEntries.Add(entry);

                //set its color based on its type
                Color buttonColor;
                switch (CardTypeManagerScript.instance.getCardByName(xEntry.name).cardType)
                {
                    case PlayerCardType.tower: buttonColor = towerColor; break;
                    case PlayerCardType.upgrade: buttonColor = upgradeColor; break;
                    case PlayerCardType.spell: buttonColor = spellColor; break;

                    default:
                        Debug.LogWarning("current deck list doesnt know what color to use for this card. (" + xEntry.name + ")");
                        buttonColor = Color.black;
                        break;
                }

                //highlight if it does not match the current filter
                if (filter != null)
                {
                    if (filter.match(xEntry) == false)
                        buttonColor = Color.Lerp(buttonColor, highlightColor, 0.5f);
                }
                entry.setColor(buttonColor);
            }
        }
    }
}
