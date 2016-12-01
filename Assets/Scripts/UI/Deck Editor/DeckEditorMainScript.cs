using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Vexe.Runtime.Types;

/// <summary>
/// filters/sorts card lists to be used by the various editor elements.
/// </summary>
public class DeckEditorFilter
{
    public string    searchString; //null if no active search.  otherwise contains the text to be found
    public PlayerCardType? type;         //only show cards of this type.  show all card types if null.

    public enum SortingRule { name, charges, type };
    public SortingRule sortBy; //how to sort the lists

    public bool baseCards;
    public bool moddedCards;

    //returns true if the card matches the filter
    public bool match(PlayerCardData c)
    {
        //type filter
        if (type != null)
            if (c.cardType != type)
                return false;

        //base game cards checkbox
        if (baseCards == false)
            if (c.isModded == false)
                return false;

        //modded cards checkbox
        if (moddedCards == false)
            if (c.isModded == true)
                return false;

        //search in the card name/description uses indexOf because contains() does not support case-insensitive searching 
        //(extended discussion of this topic can be found at http://stackoverflow.com/questions/444798/case-insensitive-containsstring/444818#444818)
        //TODO: optimize these?  currently causes TONS of string operations
        if (searchString != null) //if there is something in the search box
            if (c.getDescription().IndexOf(searchString, System.StringComparison.OrdinalIgnoreCase) < 0) //and it is not in the card name
                if (c.getDescription().IndexOf(searchString, System.StringComparison.OrdinalIgnoreCase) < 0)  //or description
                    return false; //then it is not a match

        //we haven't found a reason to exclude it, so it matches
        return true;
    }

    //match() overload that takes XMLDeckEntry instead
    public bool match(XMLDeckEntry xEntry) { return match(CardTypeManagerScript.instance.getCardByName(xEntry.name)); }

    //filters the PlayerCardData list according to the current filter rules and returns it as a new list
    public List<PlayerCardData> filterCardData(List<PlayerCardData> unfiltered)
    {
        List<PlayerCardData> filtered = new List<PlayerCardData>(unfiltered.Count); //create a new list of the same size as the unfiltered list to guarantee we can fit the results without resizing

        //fill the filtered list with everything in the unfiltered list that matches the filter
        foreach (PlayerCardData data in unfiltered)
            if (match(data))
                filtered.Add(data);

        return filtered; //return the result
    }

    //sorts the PlayerCardData list according to the current sorting rules and returns it as a new list
    public List<PlayerCardData> sortCardData(List<PlayerCardData> unsorted)
    {
        List<PlayerCardData> sorted = new List<PlayerCardData>(unsorted); //clone the unsorted list

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

    //sorts and filters the PlayerCardData list according to the current filter and sorting rules and returns it as a new list
    public List<PlayerCardData> filterAndSortCardData(List<PlayerCardData> raw)
    {
        return sortCardData(filterCardData(raw)); //filters and then sorts the list and returns it
    }
}

/// <summary>
/// comparer used to sort card lists by name
/// </summary>
class CardNameComparer : IComparer<PlayerCardData>, IComparer<XMLDeckEntry>
{
    public int Compare(PlayerCardData a, PlayerCardData b) { return string.Compare(a.cardName, b.cardName); }
    public int Compare(XMLDeckEntry a, XMLDeckEntry b) { return string.Compare(a.name, b.name); }
}

/// <summary>
/// comparer used to sort card lists by type
/// </summary>
class CardTypeComparer : IComparer<PlayerCardData>, IComparer<XMLDeckEntry>
{
    public int Compare(PlayerCardData a, PlayerCardData b)
    {
        if (a.cardType != b.cardType)
            return a.cardType - b.cardType;
        else
            return string.Compare(a.cardName, b.cardName);
    }
    public int Compare(XMLDeckEntry a, XMLDeckEntry b) { return Compare(CardTypeManagerScript.instance.getCardByName(a.name), CardTypeManagerScript.instance.getCardByName(b.name)); }
}

/// <summary>
/// comparer used to sort card lists by charges
/// </summary>
class CardChargesComparer : IComparer<PlayerCardData>, IComparer<XMLDeckEntry>
{
    public int Compare(PlayerCardData a, PlayerCardData b) { return b.cardMaxCharges - a.cardMaxCharges; }
    public int Compare(XMLDeckEntry a, XMLDeckEntry b) { return Compare(CardTypeManagerScript.instance.getCardByName(a.name), CardTypeManagerScript.instance.getCardByName(b.name)); }
}

/// <summary>
/// this is the powerhouse of the deck editor.  it handles messages to/from all of the interface elements and also performs all of the actual deck editing.
/// </summary>
public class DeckEditorMainScript : BaseBehaviour
{
    public GameObject cardPreview;          //reference to the object responsible for previewing cards
    public XMLDeck    openDeck;             //the XMLDeck currently being edited
    public DeckEditorFilter filter; //settings used to filter interface elements

    private bool unsavedChanges; //whether or not there are changes that have not been written to disk
    private bool newDeck;        //if true, this deck is not currently in the deck collection and must be added to it in order to save changes

    /// <summary>
    /// [COROUTINE] dev-only function creates a clickable button in the UNITY inspector that test previews all card types and warns about any that dont fit on the card
    /// </summary>
    [Show] public void testCardSizes() { StartCoroutine(testCardSizesCoroutine()); }
    private IEnumerator testCardSizesCoroutine()
    {
        Debug.Log("Testing...");
        foreach (PlayerCardData c in CardTypeManagerScript.instance.types.cardTypes)
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
    public void PreviewCard(PlayerCardData c) { cardPreview.SendMessage("PreviewCard", c); }

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
        //Debug.Log("deckEntryUpdated"); //DEBUG ONLY

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
    public void CardSelected(PlayerCardData c)
    {
        //Debug.Log("CardSelected"); //DEBUG ONLY

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
        //Debug.Log("TextButtonSelected"); //DEBUG ONLY

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

            case "Save and return to menu":     //user wants to close the editor
                saveChanges();                  //save any unsaved changes
                SceneManager.LoadScene("Game"); //go back to the game scene
                break;

            case "Generate Random Deck":                                    //user wants to get a randomly created deck
                unsavedChanges = true;                                      //there are no unsaved changes
                openDeck = DeckManagerScript.instance.generateRandomDeck(); //make a new deck
                newDeck = true;                                             //flag it as new so that any changes to it get saved as a new deck
                BroadcastMessage("refresh", openDeck);                      //show the new deck
                break;

            default: //button has not been implemented.  Print warning.
                MessageHandlerScript.Error("DeckEditorMainScript doesn't know how to respond to this button");
                break;
        }
    }

    //called when the deck name changes
    public void DeckRenamed(string newDeckName)
    {
        //Debug.Log("deckRenamed"); //DEBUG ONLY

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
        //Debug.Log("filterTypeChanged"); //DEBUG ONLY

        switch (newSetting)
        {
            case 0: filter.type = null; break;
            case 1: filter.type = PlayerCardType.tower; break;
            case 2: filter.type = PlayerCardType.spell; break;
            case 3: filter.type = PlayerCardType.upgrade; break;
            default: MessageHandlerScript.Error("unknown filter type"); break;
        }
        BroadcastMessage("filterChanged", filter); //report the new filter settings to children
        BroadcastMessage("refresh", openDeck);     //update interfaces
    }

    //called when filter sort changes
    public void filterSortChanged(int newSetting)
    {
        //Debug.Log("filterSortChanged"); //DEBUG ONLY

        switch (newSetting)
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
        //Debug.Log("filterSearchChanged"); //DEBUG ONLY

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
        //Debug.Log("saveChanges"); //DEBUG ONLY

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