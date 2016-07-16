using UnityEngine;
using UnityEngine.SceneManagement;

//this is the powerhouse of the deck editor.  it handles messages to/from all of the interface elements and also performs all of the actual deck editing.
public class DeckEditorMainScript : MonoBehaviour
{
    public GameObject cardPreview;    //reference to the object responsible for previewing cards
    public XMLDeck    openDeck;       //the XMLDeck currently being edited

    private bool unsavedChanges; //whether or not there are changes that have not been written to disk
    private bool newDeck;        //if true, this deck is not currently in the deck collection and must be added to it in order to save changes

    //init
    public void Start()
    {
        unsavedChanges = false;                 //there are no changes since we just opened the editor
        XMLDeck openDeck = new XMLDeck();       //create a new deck
        newDeck = true;                         //flag the deck as new
        BroadcastMessage("refresh", openDeck);  //update interfaces
    }

    //something in the editor wants to preview the given card, but doesnt know how to reach the card preview, so it sent it here instead.
    //this just passes the message along to the intended destination
    public void PreviewXMLDeckEntry(XMLDeckEntry xC) { cardPreview.SendMessage("PreviewXMLDeckEntry", xC); }

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
            Debug.LogError("updated an entry that is not in the deck!");
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
                Debug.LogWarning("DeckEditorMainScript doesn't know how to respond to this button");
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