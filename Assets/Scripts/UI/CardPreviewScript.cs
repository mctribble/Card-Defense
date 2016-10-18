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

    //fetches data on the deck entry and then previews that card type
    private void PreviewXMLDeckEntry(XMLDeckEntry xC)
    {
        CardData c = CardTypeManagerScript.instance.getCardByName(xC.name);
        StartCoroutine("PreviewCard", c);
    }

    //saves card definition data and updates components as necessary
    private IEnumerator PreviewCard(CardData c)
    {
        //save the data
        data = c;

        //update card text
        updateChargeText();
        updateDescriptionText();

        //load art with WWW (yes, really!  I couldn't find an easier way to do this and still let the user access the image files)
        WWW www = new WWW ("file:///" + Application.dataPath + "/StreamingAssets/Art/Card Art/" + data.cardArtName); //load file
        yield return www; //wait for it to load
        art.sprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0.5f, 0.5f));
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
