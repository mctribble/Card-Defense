using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DeckEditorCurrentDeckScript : MonoBehaviour
{
    public GameObject currentDeckEntryPrefab; //used to instantiate deck list entries
    public XMLDeck    data; //current deck

    private List<GameObject> deckEntries;

	// Use this for initialization
	void Start ()
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
        foreach (XMLDeckEntry xEntry in data.contents)
        {
            GameObject entry = Instantiate(currentDeckEntryPrefab);
            entry.SendMessage("setData", xEntry);
            entry.transform.SetParent(this.transform, false);
            deckEntries.Add(entry);
        }
    }
}
