﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// displays a given card but does nothing else.  For use in deck editor.
/// </summary>
public class CardPreviewScript : MonoBehaviour
{
    //data
    public PlayerCardData data; //card type being previewed

    //object references
    public Image art;         //reference to card art image
    public Text  title;       //reference to card name text
    public Text  description; //reference to card description text
    public Image cardBack;    //reference to card back image

    //register to be informed about type reloads
    private IEnumerator Start()
    {
        while (CardTypeManagerScript.instance == null)
            yield return null;

        CardTypeManagerScript.instance.cardTypesReloadedEvent += cardTypesReloaded;
    }

    /// <summary>
    /// event handler for card types being reloaded
    /// </summary>
    private void cardTypesReloaded(CardTypeCollection newTypes)
    {
        //bail if we arent showing anything
        if (data == null)
            return;

        //if we are, then find it in the list and reload it
        StartCoroutine(PreviewCard(newTypes.cardTypes.Find(c => c.cardName == data.cardName)));

        //if data is now null, throw a warning
        if (data == null)
            MessageHandlerScript.Warning("The card types were reloaded, but the card type being previewed was not found and could not be updated!");
    }

    /// <summary>
    /// fetches data on the given deck entry and then previews that card type
    /// </summary>
    private void PreviewXMLDeckEntry(XMLDeckEntry xC)
    {
        PlayerCardData c = CardTypeManagerScript.instance.getCardByName(xC.name);
        StartCoroutine("PreviewCard", c);
    }

    /// <summary>
    /// [COROUTINE] saves card definition data and updates components to display it
    /// </summary>
    private IEnumerator PreviewCard(PlayerCardData c)
    {
        //if null, show card back instead
        if (c == null)
        {
            cardBack.enabled = true;
            yield break;
        }

        //save the data
        data = c;

        //update card text
        updateChargeText();
        updateDescriptionText();

        //load art with WWW (yes, really!  I couldn't find an easier way to do this and still let the user access the image files)
        WWW www = new WWW ("file:///" + Application.dataPath + "/StreamingAssets/Art/Card Art/" + data.cardArtName); //load file
        yield return www; //wait for it to load

        if (www.error == null)
            art.sprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0.5f, 0.5f));
        else
            art.sprite = Resources.Load<Sprite>("Sprites/Error");

        //hide the back image
        cardBack.enabled = false;
    }

    //helper function.  updates the card description text.
    private void updateDescriptionText()
    {
        description.text = data.getDescription();
    }

    //updates card charge counts
    public void updateChargeText()
    {
        title.text = data.cardName + "\n" + data.cardMaxCharges + "/" + data.cardMaxCharges;
    }
}
