using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Vexe.Runtime.Types;

public class DeckEditorCurrentDeckScript : BaseBehaviour
{
    //color settings
    public Color towerColor;     //tower cards
    public Color upgradeColor;   //upgrade cards
    public Color spellColor;     //spell cards
    public Color highlightColor; //overlaid if this card is in the open deck

    public GameObject currentDeckEntryPrefab; //used to instantiate deck list entries
    public XMLDeck    data; //current deck

    private List<GameObject> deckEntries;
    private DeckEditorFilter filter;

	// Use this for initialization
	void Awake ()
    {
        //init
        deckEntries = new List<GameObject>();

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

    //refreshes the list.  If deck is not null, also sets that as the new deck
    public void refresh(XMLDeck deck)
    {
        if (deck != null)
            data = deck;
        destroyDeckEntries();
        setupDeckEntries();
    }

    //purges the list
    private void destroyDeckEntries()
    {
        foreach (GameObject entry in deckEntries)
            Destroy(entry);
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
            GameObject entry = Instantiate(currentDeckEntryPrefab);
            entry.SendMessage("setData", xEntry);
            entry.transform.SetParent(this.transform, false);
            deckEntries.Add(entry);

            //set its color based on its type
            Color buttonColor;
            switch (CardTypeManagerScript.instance.getCardByName(xEntry.name).cardType)
            {
                case CardType.tower: buttonColor = towerColor; break;
                case CardType.upgrade: buttonColor = upgradeColor; break;
                case CardType.spell: buttonColor = spellColor; break;
                default: Debug.LogWarning("card type list doesnt know what color to use for this card."); buttonColor = Color.black; break;
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
}
