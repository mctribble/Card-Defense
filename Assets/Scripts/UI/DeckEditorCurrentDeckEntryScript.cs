using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class DeckEditorCurrentDeckEntryScript : MonoBehaviour, IPointerEnterHandler
{
    //child object references
    public Text   cardNameText;  
    public Text   cardCountText;

    //XMLDeckEntry reference
    private XMLDeckEntry data;

    //sets values of the card
    public void setData (XMLDeckEntry newData)
    {
        data = newData;
        cardNameText.text = data.name;
        cardCountText.text = data.count.ToString();
    }

    //called by buttons when + or - gets clicked
    public void TextButtonSelected (string text)
    {
        if (text == "+")
        {
            //player wants to add a copy of this card.
            if (data.count != DeckRules.MAX_CARDS_OF_SAME_TYPE) //if we are not at the max...
            {
                //add the card and pass a note up the tree
                data.count++;
                SendMessageUpwards("deckEntryUpdated", data);
            }
        }
        else if (text == "-")
        {
            //player wants to remove a copy of this card.  Take it out and pass a note up the tree
            data.count--;
            SendMessageUpwards("deckEntryUpdated", data);

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
        SendMessageUpwards("PreviewCard", data);   
    }

}
