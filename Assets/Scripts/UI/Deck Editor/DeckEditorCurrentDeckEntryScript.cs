using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Vexe.Runtime.Types;

/// <summary>
/// list item for entries in the current deck list of the deck editor
/// </summary>
public class DeckEditorCurrentDeckEntryScript : BaseBehaviour, IPointerEnterHandler
{
    //child object references
    public Text   cardNameText;
    public Text   cardCountText;
    public Image  background;

    public Color normalColor; //color to use normally
    public Color overColor;   //color to use when there are too many

    //XMLDeckEntry reference
    private XMLDeckEntry data;

    //accessors
    public string cardName
    {
        get
        {
            return data.name;
        }
        set
        {
            data.name = value;
            cardNameText.text = value;
        }
    }
    public int cardCount
    {
        get
        {
            return data.count;
        }
        set
        {
            data.count = value;
            cardCountText.text = value.ToString();

            //set color based on card count
            if (value <= DeckRules.MAX_CARDS_OF_SAME_TYPE)
                cardCountText.color = normalColor;
            else
                cardCountText.color = overColor;
        }
    }

    //sets values of the card
    public void setData (XMLDeckEntry newData)
    {
        data = newData;
        cardNameText.text = data.name;
        cardCountText.text = data.count.ToString();

        //set color based on card count
        if (data.count <= DeckRules.MAX_CARDS_OF_SAME_TYPE)
            cardCountText.color = normalColor;
        else
            cardCountText.color = overColor;
    }

    //sets the background color for this entry
    public void setColor(Color newColor)
    {
        background.color = newColor;
    }

    //called by buttons when + or - gets clicked
    public void TextButtonSelected (string text)
    {
        //Debug.Log("currentDeckEntryTextButtonSelected"); //DEBUG ONLY

        if (text == "+")
        {
            //player wants to add a copy of this card.
            data.count++;
            cardCountText.text = data.count.ToString();
            SendMessageUpwards("deckEntryUpdated", data);

            //set color based on card count
            if (data.count <= DeckRules.MAX_CARDS_OF_SAME_TYPE)
                cardCountText.color = normalColor;
            else
                cardCountText.color = overColor;
        }
        else if (text == "-")
        {
            //player wants to remove a copy of this card.  Take it out and pass a note up the tree
            data.count--;
            cardCountText.text = data.count.ToString();
            SendMessageUpwards("deckEntryUpdated", data);

            //set color based on card count
            if (data.count <= DeckRules.MAX_CARDS_OF_SAME_TYPE)
                cardCountText.color = normalColor;
            else
                cardCountText.color = overColor;

            //and if that was the last copy, remove this entry from the list entirely
            if (data.count == 0)
            {
                Destroy(gameObject);

            }
        }
        else
        {
            Debug.LogWarning("DeckEditorCurrentDeckEntryScript doesnt know how to handle this button");
        }
    }

    //preview card on mouse over
    public void OnPointerEnter(PointerEventData eventData)
    {
        SendMessageUpwards("PreviewXMLDeckEntry", data);
    }
}