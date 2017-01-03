using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using Vexe.Runtime.Types;

public class PlayerHandScript : HandScript
{
    public Image       deckImage;        //image that serves as the spawn point for new cards
    public GameObject  playerCardPrefab; //prefab used to spawn a new player card
    public GameObject  previewCardPrefab;//prefab used to spawn a new card preview

    [Hide] public static PlayerHandScript instance; //singleton reference

    protected override IEnumerator Start()
    {
        yield return base.Start();

        instance = this;
        while (DeckManagerScript.instance.cardsLeft == 0)
            yield return null;

        //register for the roundOver event
        LevelManagerScript.instance.RoundOverEvent += roundOver;

        //draw starting hand
        yield return drawToHandSize(startingHandSize);

        //generate the "gather power" token 
        drawToken("Gather Power", true, true, true, true);
    }

    //called whenever a new round starts (event registered in Start())
    private void roundOver() { StartCoroutine(roundOverCoroutine()); }
    private IEnumerator roundOverCoroutine()
    {
        //wait to be done drawing
        do
            yield return null;
        while (busy);

        //if we don't have a "gather power" token, make a new one
        if (cards.Any(cs => cs != null && cs.cardName == "Gather Power") == false)
            drawToken("Gather Power");
    }

    /// <summary>
    /// takes a card name as a string, creates a new token for it, and then draws that. parameters are the same as drawCard
    /// </summary>
    /// <param name="tokenName">name of the token to create</param>
    /// <see cref="drawCard(bool, bool, bool, bool, PlayerCard?)"/>
    public void drawToken(string tokenName, bool flipOver = true, bool turnToIdentity = true, bool scaleToIdentity = true, bool ignoreHandCap = false)
    {
        //create card
        PlayerCard token = new PlayerCard();
        token.data = CardTypeManagerScript.instance.getCardByName(tokenName);
        token.charges = token.data.cardMaxCharges;

        //force the card to be a token, even if it isnt normally considered one
        token.data.isToken = true;

        //draw it
        drawCard(flipOver, turnToIdentity, scaleToIdentity, token, ignoreHandCap);
    }

    /// <summary>
    /// draws a card from the deck.  The card spawns face down at the same position, rotation, and scale as the spawn point image.
    /// all parameters are optional.
    /// </summary>
    /// <param name="flipOver">flipOver: card should flip over AFTER moving</param>
    /// <param name="turnToIdentity">turnToIdentity: card should straighten itself while moving</param>
    /// <param name="scaleToIdentity">scaleToIdentity: card should scale itself to (1,1,1) while moving</param>
    /// <param name="cardToDraw">if this is not null, then the given PlayerCard is drawn instead of fetching one from the deck.  This parameter is ignored in enemy hands since they draw WaveData's instead.</param>
    /// <param name="ignoreHandCap">if this is true, the hand can draw even if it is full</param>
    public void drawCard(bool flipOver = true, bool turnToIdentity = true, bool scaleToIdentity = true, PlayerCard? cardToDraw = null, bool ignoreHandCap = false)
    {
        //bail if reached max
        if ((currentHandSize == maximumHandSize) && (ignoreHandCap == false))
        {
            Debug.Log("Can't draw: hand is full.");
            return;
        }

        //bail if the relevant deck out of cards
        if (DeckManagerScript.instance.cardsLeft == 0)
        {
            Debug.Log("Can't draw: out of cards.");
            return;
        }

        //instantiate card prefab
        CardScript newCard;
        newCard = Instantiate(playerCardPrefab).GetComponent<CardScript>();

        newCard.transform.SetParent(transform.root); //declare the new card a child of the UI object at the root of this tree

        //position card to match up with the deck image we are spawning at
        RectTransform spawnT = deckImage.rectTransform;
        newCard.GetComponent<RectTransform>().position = spawnT.position;
        newCard.GetComponent<RectTransform>().rotation = spawnT.rotation;
        newCard.GetComponent<RectTransform>().localScale = spawnT.localScale;

        //add the card to the hand
        addCard(newCard, flipOver, turnToIdentity, scaleToIdentity);

        newCard.SendMessage("SetDeckImage", deckImage); //also tell the card where the deck image is so it knows where to go if it returns there

        //send the card the data that defines it
        if (cardToDraw == null)
            newCard.SendMessage("SetCard", DeckManagerScript.instance.Draw()); //fetch the card from the deck
        else
            newCard.SendMessage("SetCard", cardToDraw.Value); //we were given the PlayerCard already, so just pass that

        newCard.SendMessage("triggerOnDrawnEffects"); //trigger effects
    }

    /// <summary>
    /// [COROUTINE] draws until the given number of cards are in the hand or unable to draw more
    /// </summary>
    public IEnumerator drawToHandSize(int target)
    {
        int drawCount = target - currentHandSize;
        yield return drawCards(drawCount);
    }

    /// <summary>
    /// draws multiple cards.  If delay is true, there is a slight pause between each one
    /// </summary>
    public IEnumerator drawCards(int drawCount, bool delay = true)
    {
        //draw the cards
        while (drawCount > 0)
        {
            drawCount--;
            drawCard(false); //dont flip them over just yet: we want to do them all at once since they were drawn as a group

            if (delay)
                yield return new WaitForSeconds(drawDelay);
        }

        //if the deck is empty, we may not be able to draw any cards.  bail if the hand is still empty at this point.
        if (currentHandSize == 0)
            yield break;

        //wait for all cards to be idle
        while (cards.All(c => (c.state == CardState.idle) || (c.state == CardState.discarding)))
            yield return null;

        //flip the entire hand face up at once
        foreach (CardScript c in cards)
            c.SendMessage("flipFaceUp");

        yield break;
    }

    /// <summary>
    /// draws multiple specific PlayerCard objects that already exist instead of taking them from the deck.  
    /// If delay is true, there is a slight pause between each one.
    /// </summary>
    public IEnumerator drawCards(PlayerCard[] cardsToDraw, bool delay = true)
    {
        //wait until we arent busy
        do
        {
            yield return null;
        }
        while (busy);
        busy = true;

        //draw the cards
        foreach (PlayerCard c in cardsToDraw)
        {
            //draw the new card, being sure not to flip it over yet since we want to flip all of them as a group later
            drawCard(false, true, true, c);

            if (delay)
                yield return new WaitForSeconds(drawDelay);
        }

        //wait for all cards to be idle
        foreach (CardScript c in cards)
        {
            if (c == null) continue;
            CardScript waitCard = c;
            yield return StartCoroutine(waitCard.waitForIdleOrDiscarding());
        }

        //flip the entire hand face up at once
        foreach (CardScript c in cards)
            if (c != null)
                c.SendMessage("flipFaceUp");

        busy = false;
        yield break;
    }

    /// <summary>
    /// attempts to remove d charges from random cards in the hand, discarding any that hit 0 charges.  
    /// returns how much damage was actually dealt
    /// </summary>
    public int Damage(int d)
    {
        int alreadyDealt = 0;

        while ((currentHandSize > 0) && (alreadyDealt < d))
        {
            //pick a random card in the hand
            CardScript toDamage = cards[ Random.Range(0, currentHandSize) ];
            PlayerCardScript scriptRef = toDamage.GetComponent<PlayerCardScript>();

            //deal damage to it
            int toDeal = Mathf.Min (scriptRef.card.charges, (d - alreadyDealt) );
            scriptRef.card.charges -= toDeal;
            scriptRef.updateChargeText();

            //spawn combat text to show the damage
            Vector3 pos = scriptRef.combatTextPosition;
            MessageHandlerScript.instance.spawnPlayerDamageText(pos, toDeal);

            //track it
            alreadyDealt += toDeal;

            if (scriptRef.card.charges == 0)
                toDamage.SendMessage("Discard"); //discard the card if it is out of charges
            else
                scriptRef.updateChargeText(); //otherwise, tell the card to update its header text
        }

        return alreadyDealt;
    }

    /// <summary>
    /// prompts the user to choose a card in this hand with the given message.  
    /// all cards except for exception are copied to the selection hand to serve as options
    /// </summary>
    /// <param name="exception">GameObject to exclude from the list of valid choices</param>
    /// <param name="prompt">message to show the player during the selection (not yet supported)</param>
    /// <returns></returns>
    public IEnumerator selectCard(GameObject exception, string prompt)
    {
        selectedCard = null;

        //make CardPreviewScript objects for each card in hand except for exception
        Dictionary<CardScript, GameObject> previewCards = new Dictionary<CardScript, GameObject>();
        foreach (CardScript c in cards)
        {
            //skip exception card and null cards
            if ((c == exception) || (c == null))
                continue;

            //make previews
            GameObject previewCard = (GameObject)Instantiate(previewCardPrefab, SelectionHandScript.instance.transform); //spawn object
            previewCard.SendMessage("PreviewCard", c.GetComponent<PlayerCardScript>().card.data);        //send it the info
            previewCard.transform.localPosition = (c.transform.localPosition);                           //spawn previews over their corresponding PlayerCardScripts
            previewCards.Add(previewCard.GetComponent<CardPreviewScript>(), c.gameObject);               //add it to the selection hand
            c.transform.localPosition -= new Vector3(0.0f, 1000.0f, 0.0f);                              //blink the actual card off screen
        }

        //set the hand as hidden, even though it was just teleported off-screen, just to make sure the cards dont come back on screen
        Hide();

        //send the new previewCards to the selectionHand and wait for a result
        yield return StartCoroutine(SelectionHandScript.instance.addCards(previewCards.Keys.ToArray(), false));
        yield return StartCoroutine(SelectionHandScript.instance.selectCard(null, prompt));

        //get a script reference back to the card in this hand that corresponds to the chosen previewCard
        GameObject selectedGameObject = null;
        if (previewCards.TryGetValue(SelectionHandScript.instance.selectedCard, out selectedGameObject))
            selectedCard = selectedGameObject.GetComponent<CardScript>();
        else
            Debug.LogError("invalid card selected!");

        //put the playerCardScripts back to where the previews are
        foreach (KeyValuePair<CardScript, GameObject> entry in previewCards)
            entry.Value.transform.localPosition = entry.Key.transform.localPosition;

        //get rid of the preview cards
        yield return StartCoroutine(SelectionHandScript.instance.discardRandomCards(null, 999, false));

        //send the player cards back to their normal positions
        Show();

        yield break;
    }

    //[DEV] creates buttons in the inspector to manipulate the hand
    [Show] private void devDraw() { drawCard(); }
    [Show] private void devDiscard() { discardRandomCard(null); }
    [Show] private void devRecycle()
    {
        int cardCount = currentHandSize;
        StartCoroutine(discardRandomCards(null, cardCount, false));
        StartCoroutine(drawCards(cardCount, false));
    }
}
