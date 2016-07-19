using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Vexe.Runtime.Types;

public class DeckEditorCurrentDeckEntryScript : BaseBehaviour, IPointerEnterHandler
{
    //child object references
    public Text   cardNameText;
    public Text   cardCountText;
    public Image  background;

    //XMLDeckEntry reference
    private XMLDeckEntry data;

    //sets values of the card
    public void setData (XMLDeckEntry newData)
    {
        data = newData;
        cardNameText.text = data.name;
        cardCountText.text = data.count.ToString();

        //if we are below the max, print count in white.  Otherwise, print count in red
        if (data.count <= DeckRules.MAX_CARDS_OF_SAME_TYPE)
            background.color = Color.white;
        else
            background.color = Color.red;
    }

    //sets the background color for this entry
    public void setColor(Color newColor)
    {
        background.color = newColor;
    }

    //called by buttons when + or - gets clicked
    public void TextButtonSelected (string text)
    {
        if (text == "+")
        {
            //player wants to add a copy of this card.
            data.count++;
            cardCountText.text = data.count.ToString();
            SendMessageUpwards("deckEntryUpdated", data);

            //if we are below the max, print count in white.  Otherwise, print count in red
            if (data.count <= DeckRules.MAX_CARDS_OF_SAME_TYPE)
                cardCountText.color = Color.white;
            else
                cardCountText.color = Color.red;
        }
        else if (text == "-")
        {
            //player wants to remove a copy of this card.  Take it out and pass a note up the tree
            data.count--;
            cardCountText.text = data.count.ToString();
            SendMessageUpwards("deckEntryUpdated", data);

            //if we are below the max, print count in white.  Otherwise, print count in red
            if (data.count <= DeckRules.MAX_CARDS_OF_SAME_TYPE)
                cardCountText.color = Color.white;
            else
                cardCountText.color = Color.red;

            //and if that was the last copy, remove this entry from the list entirely
            if (data.count == 0)
                Destroy(gameObject);
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