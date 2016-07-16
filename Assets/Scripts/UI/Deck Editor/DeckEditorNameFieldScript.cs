using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class DeckEditorNameFieldScript : MonoBehaviour
{
    public InputField inputField;

    //called when the deck changes.  Fill the text field with the name of said deck
    public void refresh(XMLDeck deck)
    {
        if (deck == null)
            inputField.text = "";
        else
            inputField.text = deck.name;
    }
}
