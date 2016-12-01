using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Vexe.Runtime.Types;

/// <summary>
/// individual entry of the list of card types in the deck editor
/// </summary>
public class DeckEditorCardTypeEntryScript : BaseBehaviour, IPointerEnterHandler
{
    public Text cardNameText; //label reference
    public Image background;  //background reference

    private PlayerCardData data; //card type

    public PlayerCardData type { get { return data; } }

    //sets the card associated with this entry
    public void setCard(PlayerCardData newData)
    {
        data = newData;
        cardNameText.text = data.cardName;
    }

    //sets the background color for this entry
    public void setColor(Color newColor)
    {
        background.color = newColor;
    }

    //called by button when + gets clicked
    public void TextButtonSelected(string text)
    {
        if (text == "+")
            SendMessageUpwards("CardSelected", data); //report what card was picked
        else
            Debug.LogWarning("DeckEditorCardTypeEntryScript doesnt know how to handle this button");
    }

    //preview card on mouse over
    public void OnPointerEnter(PointerEventData eventData)
    {
        SendMessageUpwards("PreviewCard", data);
    }
}
