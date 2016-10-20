using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class CardPreviewScript : MonoBehaviour
{
    //data
    public CardData data; //card type being previewed

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

    //event handler for card types being reloaded
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

    //fetches data on the deck entry and then previews that card type
    private void PreviewXMLDeckEntry(XMLDeckEntry xC)
    {
        CardData c = CardTypeManagerScript.instance.getCardByName(xC.name);
        StartCoroutine("PreviewCard", c);
    }

    //saves card definition data and updates components as necessary
    private IEnumerator PreviewCard(CardData c)
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
        art.sprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0.5f, 0.5f));

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
