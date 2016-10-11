using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Vexe.Runtime.Types;

//filters/sorts card lists to be used by the various editor elements.
public class DeckEditorFilter
{
    public string    searchString; //null if no active search.  otherwise contains the text to be found
    public CardType? type;         //only show cards of this type.  show all card types if null.

    public enum SortingRule { name, charges, type };
    public SortingRule sortBy; //how to sort the lists

    public bool baseCards;
    public bool moddedCards;

    //returns true if the card matches the filter
    public bool match(CardData c)
    {
        //search in the card description uses indexOf because contains() does not support case-insensitive searching 
        //(extended discussion of this topic can be found at http://stackoverflow.com/questions/444798/case-insensitive-containsstring/444818#444818)
        if (searchString != null)
            if (c.getDescription().IndexOf(searchString, System.StringComparison.OrdinalIgnoreCase) < 0) //TODO: optimize this?  currently causes TONS of string operations
                return false;

        if (type != null)
            if (c.cardType != type)
                return false;

        if (baseCards == false)
            if (c.isModded == false)
                return false;

        if (moddedCards == false)
            if (c.isModded == true)
                return false;

        return true;
    }

    //match() overload that takes XMLDeckEntry instead
    public bool match(XMLDeckEntry xEntry) { return match(CardTypeManagerScript.instance.getCardByName(xEntry.name)); }

    //filters the CardData list according to the current filter rules and returns it as a new list
    public List<CardData> filterCardData(List<CardData> unfiltered)
    {
        List<CardData> filtered = new List<CardData>(unfiltered.Count); //create a new list of the same size as the unfiltered list to guarantee we can fit the results without resizing

        //fill the filtered list with everything in the unfiltered list that matches the filter
        foreach (CardData data in unfiltered)
            if (match(data))
                filtered.Add(data);

        return filtered; //return the result
    }

    //sorts the CardData list according to the current sorting rules and returns it as a new list
    public List<CardData> sortCardData(List<CardData> unsorted)
    {
        List<CardData> sorted = new List<CardData>(unsorted); //clone the unsorted list

        //sort using an IComparer chosen from the sort type
        switch(sortBy)
        {
            case SortingRule.name:    sorted.Sort(new CardNameComparer());    break;
            case SortingRule.type:    sorted.Sort(new CardTypeComparer());    break;
            case SortingRule.charges: sorted.Sort(new CardChargesComparer()); break;
            default:
                MessageHandlerScript.Error("sortCardData doesnt know how to handle this sorting rule");
                break;
        }

        return sorted; //return the sorted list
    }

    //sorts the XMLDeckEntry list according to the current sorting rules and returns it as a new list
    public List<XMLDeckEntry> sortXMLDeckEntries(List<XMLDeckEntry> unsorted)
    {
        List<XMLDeckEntry> sorted = new List<XMLDeckEntry>(unsorted); //clone the unsorted list

        //sort using an IComparer chosen from the sort type
        switch (sortBy)
        {
            case SortingRule.name: sorted.Sort(new CardNameComparer()); break;
            case SortingRule.type: sorted.Sort(new CardTypeComparer()); break;
            case SortingRule.charges: sorted.Sort(new CardChargesComparer()); break;
            default:
                MessageHandlerScript.Error("sortCardData doesnt know how to handle this sorting rule");
                break;
        }

        return sorted; //return the sorted list
    }

    //sorts and filters the CardData list according to the current filter and sorting rules and returns it as a new list
    public List<CardData> filterAndSortCardData(List<CardData> raw)
    {
        return sortCardData(filterCardData(raw)); //filters and then sorts the list and returns it
    }
}

//comparer used to sort card lists by name
class CardNameComparer : IComparer<CardData>, IComparer<XMLDeckEntry>
{
    public int Compare(CardData a, CardData b) { return string.Compare(a.cardName, b.cardName); }
    public int Compare(XMLDeckEntry a, XMLDeckEntry b) { return string.Compare(a.name, b.name); }
}

//comparer used to sort card lists by type
class CardTypeComparer : IComparer<CardData>, IComparer<XMLDeckEntry>
{
    public int Compare(CardData a, CardData b)
    {
        if (a.cardType != b.cardType)
            return a.cardType - b.cardType;
        else
            return string.Compare(a.cardName, b.cardName);
    }
    public int Compare(XMLDeckEntry a, XMLDeckEntry b) { return Compare(CardTypeManagerScript.instance.getCardByName(a.name), CardTypeManagerScript.instance.getCardByName(b.name)); }
}

//comparer used to sort card lists by charges
class CardChargesComparer : IComparer<CardData>, IComparer<XMLDeckEntry>
{
    public int Compare(CardData a, CardData b) { return b.cardMaxCharges - a.cardMaxCharges; }
    public int Compare(XMLDeckEntry a, XMLDeckEntry b) { return Compare(CardTypeManagerScript.instance.getCardByName(a.name), CardTypeManagerScript.instance.getCardByName(b.name)); }
}

//this is the powerhouse of the deck editor.  it handles messages to/from all of the interface elements and also performs all of the actual deck editing.
public class DeckEditorMainScript : BaseBehaviour
{
    public GameObject cardPreview;          //reference to the object responsible for previewing cards
    public XMLDeck    openDeck;             //the XMLDeck currently being edited
    public DeckEditorFilter filter; //settings used to filter interface elements

    private bool unsavedChanges; //whether or not there are changes that have not been written to disk
    private bool newDeck;        //if true, this deck is not currently in the deck collection and must be added to it in order to save changes

    //creates a clickable button in the UNITY inspector that test previews all card types and warns about any that dont fit on the card
    [Show] public void testCardSizes() { StartCoroutine(testCardSizesCoroutine()); }
    private IEnumerator testCardSizesCoroutine()
    {
        Debug.Log("Testing...");
        foreach (CardData c in CardTypeManagerScript.instance.types.cardTypes)
        {
            cardPreview.SendMessage("PreviewCard", c);
            yield return new WaitForSeconds(0.1f);

            //title
            Text t = cardPreview.GetComponent<CardPreviewScript>().title;
            if (t.cachedTextGenerator.characterCountVisible < t.text.Length)
                Debug.LogWarning("Card title for " + c.cardName + " does not fit on the card!");

            //description
            t = cardPreview.GetComponent<CardPreviewScript>().description;
            if (t.cachedTextGenerator.characterCountVisible < t.text.Length)
                Debug.LogWarning("Card description for " + c.cardName + " does not fit on the card!");
        }
        Debug.Log("Done.");
        yield break;
    }

    //init
    public IEnumerator Start()
    {
        //wait for card type manager to be ready
        while ((CardTypeManagerScript.instance == null) || (CardTypeManagerScript.instance.areTypesLoaded() == false))
            yield return null;

        unsavedChanges = false;                 //there are no changes since we just opened the editor
        XMLDeck openDeck = new XMLDeck();       //create a new deck
        newDeck = true;                         //flag the deck as new

        //default search filter settings
        filter = new DeckEditorFilter();
        filter.searchString = null;
        filter.type = null;
        filter.sortBy = DeckEditorFilter.SortingRule.name;
        filter.baseCards = true;
        filter.moddedCards = true;

        BroadcastMessage("filterChanged", filter); //report the new filter settings to children
        BroadcastMessage("refresh", openDeck);     //update interfaces
    }

    //something in the editor wants to preview the given card, but doesnt know how to reach the card preview, so it sent it here instead.
    //this just passes the message along to the intended destination
    public void PreviewXMLDeckEntry(XMLDeckEntry xC) { cardPreview.SendMessage("PreviewXMLDeckEntry", xC); }
    public void PreviewCard(CardData c) { cardPreview.SendMessage("PreviewCard", c); }

    //handles buttons in the deck list
    public void DeckSelected(XMLDeck selectedDeck)
    {
        //user wants to load a different deck
        saveChanges();                         //save any unsaved changes
        openDeck = selectedDeck;               //change decks
        newDeck = false;                       //this deck was loaded from disk, so it is not new
        BroadcastMessage("refresh", openDeck); //update the menus to reflect it
    }

    //called when an existing deck entry changes
    public void deckEntryUpdated (XMLDeckEntry updatedEntry)
    {
        //first, we have to find the entry that changed
        XMLDeckEntry oldEntry = null;
        foreach (XMLDeckEntry curEntry in openDeck.contents)
        {
            if (curEntry.name == updatedEntry.name)
            {
                oldEntry = curEntry;
                break;
            }
        }

        //if the entry was not in the deck, bail
        if (oldEntry == null)
        {
            MessageHandlerScript.Error("updated an entry that is not in the deck!");
            return;
        }

        //if the count is now zero, we remove it from the list.  Otherwise, we update it.  
        //We dont have to refresh the children this time because the current deck list is the only one that needs to change 
        //and it is the one that told us about said change to begin with
        if (updatedEntry.count == 0)
            openDeck.contents.Remove(oldEntry);
        else
            oldEntry.count = updatedEntry.count;

        unsavedChanges = true; //mark the deck as having changed
        BroadcastMessage("refresh", openDeck); //update the menus to reflect it
    }

    //handles button clicks from the card type list
    public void CardSelected(CardData c)
    {
        //check the open deck and ignore the message if that card is already in the deck
        foreach (XMLDeckEntry entry in openDeck.contents)
            if (entry.name == c.cardName)
                return;

        //otherwise, we add the card to the deck
        XMLDeckEntry newEntry = new XMLDeckEntry(c.cardName, 1);
        openDeck.contents.Add(newEntry);
        unsavedChanges = true;
        BroadcastMessage("refresh", openDeck);
    }

    //handles all buttons in the editor that only have text attached.
    public void TextButtonSelected(string text)
    {
        //text contains the text of the button that was clicked, so we use a case statement to differentiate between buttons
        switch (text)
        {
            case "-": //this is a stray message actually intended for DeckEditorCurrentDeckScript, so we can ignore it
                break;

            case "+": //this is a stray message actually intended for DeckEditorCurrentDeckScript, so we can ignore it
                break;

            case "New Deck":                            //user wants to make a new deck
                saveChanges();                          //save any unsaved changes
                openDeck = new XMLDeck();               //make a new deck
                newDeck = true;                         //flag this deck as new so that any changes to it get saved as a new deck
                BroadcastMessage("refresh", openDeck);  //inform the lists
                break;

            case "Close Editor":                //user wants to close the editor
                saveChanges();                  //save any unsaved changes
                SceneManager.LoadScene("Game"); //go back to the game scene
                break;

            default: //button has not been implemented.  Print warning.
                MessageHandlerScript.Error("DeckEditorMainScript doesn't know how to respond to this button");
                break;
        }
    }

    //called when the deck name changes
    public void DeckRenamed(string newDeckName)
    {
        //change the name
        openDeck.name = newDeckName;

        //force a save immediately
        unsavedChanges = true;
        saveChanges();

        //and refresh the interface
        BroadcastMessage("refresh", openDeck);
    }

    //called when filter type changes
    public void filterTypeChanged(int newSetting)
    {
        switch (newSetting)
        {
            case 0: filter.type = null; break;
            case 1: filter.type = CardType.tower; break;
            case 2: filter.type = CardType.spell; break;
            case 3: filter.type = CardType.upgrade; break;
            default: MessageHandlerScript.Error("unknown filter type"); break;
        }
        BroadcastMessage("filterChanged", filter); //report the new filter settings to children
        BroadcastMessage("refresh", openDeck);     //update interfaces
    }

    //called when filter sort changes
    public void filterSortChanged(int newSetting)
    {
        switch(newSetting)
        {
            case 0: filter.sortBy = DeckEditorFilter.SortingRule.name; break;
            case 1: filter.sortBy = DeckEditorFilter.SortingRule.charges; break;
            case 2: filter.sortBy = DeckEditorFilter.SortingRule.type; break;
            default: MessageHandlerScript.Error("unknown sort type"); break;
        }
        BroadcastMessage("filterChanged", filter); //report the new filter settings to children
        BroadcastMessage("refresh", openDeck);     //update interfaces
    }

    //called when filter search changes
    public void filterSearchChanged(string newSetting)
    {
        if (newSetting == "")
            filter.searchString = null;
        else
            filter.searchString = newSetting;

        BroadcastMessage("filterChanged", filter); //report the new filter settings to children
        BroadcastMessage("refresh", openDeck);     //update interfaces
    }

    //called when filter check boxes change
    public void filterToggleBaseChanged(bool newSetting) { filter.baseCards   = newSetting; BroadcastMessage("filterChanged", filter); BroadcastMessage("refresh", openDeck); }
    public void filterToggleModChanged (bool newSetting) { filter.moddedCards = newSetting; BroadcastMessage("filterChanged", filter); BroadcastMessage("refresh", openDeck); }

    //saves the deck collection, if there are unsaved changes
    private void saveChanges()
    {
        //bail early if there are no changes to save
        if (unsavedChanges == false)
            return;

        unsavedChanges = false; 

        //if the deck is empty, it should not be in the collection
        if (openDeck.contents.Count == 0)
        {
            if (newDeck)
                return; //the deck is already missing from the collection, so we are done
            else
                DeckManagerScript.instance.playerDecks.decks.Remove(openDeck); //the deck is currently in the collection.  Take it out.
        }

        //if this is a new deck, add it to the collection
        if (newDeck)
        {
            DeckManagerScript.instance.playerDecks.decks.Add(openDeck);
            newDeck = false;
        }

        //save the collection
        DeckManagerScript.instance.savePlayerDecks();
    }
}