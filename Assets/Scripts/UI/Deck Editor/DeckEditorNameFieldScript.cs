using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Vexe.Runtime.Types;

public class DeckEditorNameFieldScript : BaseBehaviour
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
