using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

/// <summary>
/// displays a given card but does nothing else.  For use in UI.
/// </summary>
public class CardPreviewScript : CardScript, IPointerClickHandler
{
    public PlayerCardData data; //card type being previewed
    public Image art;           //reference to card art image

    public override string cardName { get { return data.cardName; } }

    //register to be informed about type reloads
    private IEnumerator Start()
    {
        while (CardTypeManagerScript.instance == null)
            yield return null;

        CardTypeManagerScript.instance.cardTypesReloadedEvent += cardTypesReloaded;
    }

    //override awake() to start face up
    protected override void Awake()
    {
        base.Awake();
        faceDown = false;
    }

    /// <summary>
    /// event handler for card types being reloaded.  Updates display
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
            Debug.LogWarning("The card types were reloaded, but the card type being previewed was not found and could not be updated!");
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
        //Debug.Log("previewing " + c.cardName); //DEBUG ONLY

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

        //load art with WWW, even on PC (yes, really!  I couldn't find an easier way to do this and still let the user access the image files)
        string artPath = "";
        if (Application.platform != RuntimePlatform.WebGLPlayer)
            artPath = "file:///";
        artPath += Application.streamingAssetsPath + "/Art/Card Art/" + data.cardArtName;
        WWW www = new WWW (artPath); //load file
        yield return www; //wait for it to load

        if (www.error == null)
        {
            art.sprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0.5f, 0.5f));
        }
        else
        {
            art.sprite = Resources.Load<Sprite>("Sprites/Error");
            Debug.LogWarning("Could not preview card art: (" + www.error + ")");
        }

        //hide the back image
        cardBack.enabled = false;
    }

    //helper function.  updates the card description text.
    public override void updateDescriptionText()
    {
        description.text = data.getDescription();
    }

    //updates card charge counts
    public void updateChargeText()
    {
        title.text = data.cardName + "\n" + data.cardMaxCharges + "/" + data.cardMaxCharges;
    }

    public override void Hide()
    {
        Debug.LogWarning("CardPreviewScript: Hide() could make sense, but is not yet implemented.");
    }

    public override IEnumerator Discard()
    {
        //just destroy self with no animation
        state = CardState.discarding;
        hand.SendMessage("Discard", this);
        Destroy(gameObject);
        yield break;
    }

    public override void triggerOnDrawnEffects()
    {
        Debug.LogWarning("CardPreviewScript: triggerOnDrawnEffects() does not apply.");
    }

    //ignore mouseovers
    public override void OnPointerEnter(PointerEventData eventData) { }
    public override void OnPointerExit(PointerEventData eventData) { }

    //send a message upwards when we get clicked on, in case anything cares
    public void OnPointerClick(PointerEventData eventData)
    {
        SendMessageUpwards("cardPreviewClicked", this, SendMessageOptions.DontRequireReceiver);
    }
}
