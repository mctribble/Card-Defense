using UnityEngine;
using System.Collections;
using Vexe.Runtime.Types;
using UnityEngine.UI;

/// <summary>
/// displays card count statistics in the deck editor
/// </summary>
public class DeckEditorCardCountScript : BaseBehaviour
{
    private Text text;

    //init
    private void Awake()
    {
        text = gameObject.GetComponent<Text>();
    }

    //called when the deck updates
    public void refresh(XMLDeck openDeck)
    {
        int cardCount = 0;
        if (openDeck != null)
            cardCount = openDeck.cardCount;

        text.text = "min/cur/max\n";

        if (cardCount < DeckRules.MIN_CARDS_IN_DECK)
            text.text += "<Color=red> ";
        else
            text.text += "<Color=green> ";
        text.text += DeckRules.MIN_CARDS_IN_DECK + " </Color>/ ";

        if ((DeckRules.MIN_CARDS_IN_DECK <= cardCount) && (cardCount <= DeckRules.MAX_CARDS_IN_DECK))
            text.text += "<Color=green> ";
        else
            text.text += "<Color=red> ";
        text.text += cardCount + " </Color>/ ";

        if (cardCount > DeckRules.MAX_CARDS_IN_DECK)
            text.text += "<Color=red> ";
        else
            text.text += "<Color=black> ";
        text.text += DeckRules.MAX_CARDS_IN_DECK + " </Color>";
    }
}