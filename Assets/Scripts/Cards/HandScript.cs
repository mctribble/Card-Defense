using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Vexe.Runtime.Types;

/// <summary>
/// used to indicate hand ownership 
/// </summary>
public enum HandFaction { player, enemy, selection };

/// <summary>
/// represents a group of cards being shown on the screens
/// </summary>
public class HandScript : BaseBehaviour
{
    //static references to hands of various types so other objects can easily access them
    [Hide] public static HandScript playerHand;
    [Hide] public static HandScript enemyHand;
    [Hide] public static HandScript selectionHand;

    //references
    public Image       deckImage;        //image that serves as the spawn point for new cards
    public GameObject  playerCardPrefab; //prefab used to spawn a new player card
    public GameObject  enemyCardPrefab;  //prefab used to spawn a new enemy card
    public GameObject  previewCardPrefab;//prefab used to spawn a new card preview

    //hand status
    public HandFaction handOwner;        //indicates ownership of the hand
    public int         startingHandSize; //cards the player starts with
    public int         maximumHandSize;  //max number of cards the player can have
    public int         currentHandSize;  //number of cards currently in hand
    public bool        handHidden;       //whether or not the hand is hidden
    public CardScript  selectedCard;     //holds return value for selectCard() coroutine

    //positioning and timing
    public int   idealGap;      //ideal gap between cards
    public float idleHeightMod; //used to calculate height at which new cards should idle
    public float drawDelay;	    //delay given between drawing multiple cards
    public float discardDelay;  //delay given between discarding multiple cards
    
    //private info
    private GameObject[] cards;  //stores the number of cards
    private bool discarding;     //if true, multiple cards are currently being discarded and we need to wait before drawing

    // Use this for initialization
    // it is a coroutine for animation purposes
    private IEnumerator Start()
    {
        cards = new GameObject[maximumHandSize]; //construct array to hold the hand
        currentHandSize = 0; //no cards yet
        discarding = false; //we are not discarding cards
        selectedCard = null; //no selection yet

        //wait for the level to load
        while (LevelManagerScript.instance.levelLoaded == false)
            yield return null;

        //set faction and wait for the appropriate manager
        if (handOwner == HandFaction.player)
        {
            HandScript.playerHand = this;
            while (DeckManagerScript.instance.cardsLeft == 0)
                yield return null;
        }
        else if (handOwner == HandFaction.enemy)
        {
            HandScript.enemyHand = this;
            while (LevelManagerScript.instance.wavesInDeck == 0)
                yield return null;
        }
        else if (handOwner == HandFaction.selection)
        {
            HandScript.selectionHand = this;
        }

        //draw starting hand
        handHidden = false;
        yield return drawToHandSize(startingHandSize);

        yield break;
    }

    /// <summary>
    /// [COROUTINE] empties out the hand, waits for the level to be loaded if it isn't already, then draws a new hand
    /// </summary>
    private IEnumerator Reset()
    {
        handHidden = false;
        yield return StartCoroutine(discardRandomCards(null, currentHandSize));

        while (LevelManagerScript.instance.levelLoaded == false)
            yield return null;

        yield return drawToHandSize(startingHandSize);
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

    /// <summary>
    /// draws a card from the deck.  The card spawns face down at the same position, rotation, and scale as the spawn point image.
    /// all parameters are optional.
    /// </summary>
    /// <param name="flipOver">flipOver: card should flip over AFTER moving</param>
    /// <param name="turnToIdentity">turnToIdentity: card should straighten itself while moving</param>
    /// <param name="scaleToIdentity">scaleToIdentity: card should scale itself to (1,1,1) while moving</param>
    /// <param name="drawSurvivorWave">drawSurvivorWave: (enemy hands only) sets up the new card with a survivor wave instead of a wave from the deck</param>
    /// <param name="cardToDraw">if this is not null, then the given PlayerCard is drawn instead of fetching one from the deck.  This parameter is ignored in enemy hands since they draw WaveData's instead.</param>
    public void drawCard(bool flipOver = true, bool turnToIdentity = true, bool scaleToIdentity = true, bool drawSurvivorWave = false, PlayerCard? cardToDraw = null)
    {
        //unsupported for selection hands, since those are used as a sort of menu
        if (handOwner == HandFaction.selection)
            throw new System.NotImplementedException();

        //bail if reached max
        if (currentHandSize == maximumHandSize)
        {
            Debug.Log("Can't draw: hand is full.");
            return;
        }

        //bail if the relevant deck out of cards
        if ((handOwner == HandFaction.player) && (DeckManagerScript.instance.cardsLeft == 0))
        {
            Debug.Log("Can't draw: out of cards.");
            return;
        }
        else if ((handOwner == HandFaction.enemy) && (LevelManagerScript.instance.wavesInDeck == 0))
        {
            if (drawSurvivorWave == false) //an empty enemy deck is fine if we're drawing a survivor wave instead of from the deck
            {
                Debug.Log("Can't draw: enemy deck empty.");
                return;
            }
        }

        //instantiate card prefab
        GameObject newCard;
        if (handOwner == HandFaction.player)
            newCard = Instantiate(playerCardPrefab);
        else if (handOwner == HandFaction.enemy)
            newCard = Instantiate(enemyCardPrefab);
        else
        {
            newCard = null;
            Debug.LogWarning("tried to draw from the selection hand!");
            return;
        }

        newCard.transform.SetParent(transform.root); //declare the new card a child of the UI object at the root of this tree

        //position card to match up with the deck image we are spawning at
        RectTransform spawnT = deckImage.rectTransform;
        newCard.GetComponent<RectTransform>().position = spawnT.position;
        newCard.GetComponent<RectTransform>().rotation = spawnT.rotation;
        newCard.GetComponent<RectTransform>().localScale = spawnT.localScale;

        //add the card to the hand
        addCard(newCard.GetComponent<CardScript>(), flipOver, turnToIdentity, scaleToIdentity);

        if (handOwner == HandFaction.player)
            newCard.SendMessage("SetDeckImage", deckImage); //also tell the card where the deck image is so it knows where to go if it returns there

        //send the card the data that defines it
        if (handOwner == HandFaction.player)
        {
            if (cardToDraw == null)
                newCard.SendMessage("SetCard", DeckManagerScript.instance.Draw()); //fetch the card from the deck
            else
                newCard.SendMessage("SetCard", cardToDraw.Value); //we were given the PlayerCard already, so just pass that
        }
        else if (handOwner == HandFaction.enemy)
        {
            if (drawSurvivorWave == false)
                newCard.SendMessage("SetWave", LevelManagerScript.instance.DrawEnemy());
            else
                newCard.SendMessage("SurvivorWave");

            LevelManagerScript.instance.UpdateWaveStats();
        }
        else
        {
            MessageHandlerScript.Error("don't know how to draw for a hand with this owner");
        }

        newCard.SendMessage("triggerOnDrawnEffects"); //trigger effects
        
    }

    /// <summary>
    /// adds a card that already exists the hand.
    /// all parameters are optional.
    /// </summary>
    /// <param name="cardToAdd">the card to add</param>
    /// <param name="flipOver">flipOver: card should flip over AFTER moving</param>
    /// <param name="turnToIdentity">turnToIdentity: card should straighten itself while moving</param>
    /// <param name="scaleToIdentity">scaleToIdentity: card should scale itself to (1,1,1) while moving</param>
    /// <param name="drawSurvivorWave">drawSurvivorWave: (enemy hands only) sets up the new card with a survivor wave instead of a wave from the deck</param>
    public void addCard(CardScript cardToAdd, bool flipOver = true, bool turnToIdentity = true, bool scaleToIdentity = true)
    {
        cards[currentHandSize] = cardToAdd.gameObject; //add the card to the hand

        cards[currentHandSize].SendMessage("SetHand", gameObject);     //tell the card which hand owns it

        //if set, tell it to turn upright
        if (turnToIdentity)
            cards[currentHandSize].SendMessage("turnToQuaternion", Quaternion.identity);

        //if set, tell it to scale to its normal size
        if (scaleToIdentity)
            cards[currentHandSize].SendMessage("scaleToVector", Vector3.one);

        //if set, tell it to flip face up after its done moving
        if (flipOver)
            cards[currentHandSize].SendMessage("flipFaceUpWhenIdle");

        //if the hand is currently hidden, tell the new card to hide itself
        if (handHidden)
            cards[currentHandSize].SendMessage("Hide");

        currentHandSize++;  //increment card count

        //rearrange cards
        updateCardIdleLocations(); //rearrange the cards

        //if this is an enemy hand, also update the wave stats
        if (handOwner == HandFaction.enemy)
            LevelManagerScript.instance.UpdateWaveStats();
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
        //wait to make sure we dont attempt to discard and draw at the same time (i. e., cards like Recycle that cause both discarding and drawing simultaneously)
        do
        {
            yield return null;
        }
        while (discarding);

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
        if (handOwner == HandFaction.player)
        {
            foreach (GameObject go in cards)
            {
                if (go == null) continue;
                CardScript waitCard = go.GetComponent<CardScript>();
                yield return StartCoroutine(waitCard.waitForIdleOrDiscarding());
            }
        }
        else
        {
            foreach (GameObject go in cards)
            {
                if (go == null) continue;
                EnemyCardScript waitCard = go.GetComponent<EnemyCardScript>();
                yield return StartCoroutine(waitCard.waitForIdleOrDiscarding());
            }
        }

        //flip the entire hand face up at once
        foreach (GameObject c in cards)
            if (c != null)
                c.SendMessage("flipFaceUp");

        yield break;
    }

    /// <summary>
    /// draws multiple specific PlayerCard objects that already exist instead of taking them from the deck.  
    /// If delay is true, there is a slight pause between each one.
    /// </summary>
    public IEnumerator drawCards(PlayerCard[] cardsToDraw, bool delay = true)
    {
        //wait to make sure we dont attempt to discard and draw at the same time (i. e., cards like Recycle that cause both discarding and drawing simultaneously)
        do
        {
            yield return null;
        }
        while (discarding);

        //draw the cards
        foreach(PlayerCard c in cardsToDraw)
        {
            //draw the new card, being sure not to flip it over yet since we want to flip all of them as a group later
            drawCard(false, true, true, false, c); 

            if (delay)
                yield return new WaitForSeconds(drawDelay);
        }

        //wait for all cards to be idle
        foreach (GameObject go in cards)
        {
            if (go == null) continue;
            CardScript waitCard = go.GetComponent<CardScript>();
            yield return StartCoroutine(waitCard.waitForIdleOrDiscarding());
        }

        //flip the entire hand face up at once
        foreach (GameObject c in cards)
            if (c != null)
                c.SendMessage("flipFaceUp");

        yield break;
    }

    /// <summary>
    /// adds multiple CardScript GameObjects that already exist instead of drawing them
    /// If delay is true, there is a slight pause between each one.
    /// </summary>
    public IEnumerator addCards(CardScript[] cardsToAdd, bool delay = true)
    {
        //wait to make sure we dont attempt to discard and draw at the same time (i. e., cards like Recycle that cause both discarding and drawing simultaneously)
        do
        {
            yield return null;
        }
        while (discarding);

        //draw the cards
        foreach (CardScript c in cardsToAdd)
        {
            //draw the new card, being sure not to flip it over yet since we want to flip all of them as a group later
            addCard(c, false);

            if (delay)
                yield return new WaitForSeconds(drawDelay);
        }

        //wait for all cards to be idle
        foreach (GameObject go in cards)
        {
            if (go == null) continue;
            CardScript waitCard = go.GetComponent<CardScript>();
            yield return StartCoroutine(waitCard.waitForIdleOrDiscarding());
        }

        //flip the entire hand face up at once
        foreach (GameObject c in cards)
            if (c != null)
                c.SendMessage("flipFaceUp");

        yield break;
    }

    /// <summary>
    /// [COROUTINE] discards multiple random cards.
    /// </summary>
    /// <param name="exemption">if not null, this card cannot be discarded</param>
    /// <param name="count">number to discard</param>
    /// <param name="delay">whether or not to pause between discards</param>
    public IEnumerator discardRandomCards(GameObject exemption, int count, bool delay = true)
    {
        discarding = true;
        for (uint i = 0; i < count; i++)
        {
            discardRandomCard(exemption);
            if (currentHandSize == 0)
                break;

            if (delay) 
                yield return new WaitForSeconds(discardDelay);
        }
        discarding = false;
    }

    /// <summary>
    /// discard a random card from the hand.  exemption card is safe
    /// </summary>
    public void discardRandomCard(GameObject exemption)
    {
        //special case: no cards
        if (currentHandSize == 0)
            return;

        //special case: only one card
        if (currentHandSize == 1)
        {
            if (cards[0] != exemption)
            {
                cards[0].SendMessage("Discard");
            }
            return;
        }

        //general case: multiple cards
        GameObject target = null;
        while (target == null) //loop because we might randomly pick the exempt card
        {
            //pick a card
            int i = Random.Range(0, currentHandSize);
            target = cards[i];

            //if we picked the exempt card, reset and try again
            if (target == exemption)
            {
                target = null;
                continue;
            }

            //discard it
            target.SendMessage("Discard");
        }
    }

    /// <summary>
    /// to be called whenever the number of cards changes: updates all the cards to tell them where they should be
    /// </summary>
    private void updateCardIdleLocations()
    {
        //bail if no cards
        if (currentHandSize == 0)
            return;

        //retrieve card dims
        int cardWidth  = (int)cards[0].GetComponent<RectTransform>().rect.width;
        int cardHeight = (int)cards[0].GetComponent<RectTransform>().rect.height;

        //figure out how much horizontal space we have to work with and where it is relative to the canvas
        RectTransform handRectTrans = transform.GetComponent<RectTransform>();
        int handRegionWidth  = (int)handRectTrans.rect.width;
        int handRegionMidpoint = (int)(transform.position.x - transform.root.position.x);

        //figure out how far away the cards need to be from each other
        int cardDistance = cardWidth + idealGap;

        //calculate the midpoint of the hand to offset it properly
        int lastCardPos  = (cardDistance * (currentHandSize - 1));
        int handMidpoint = lastCardPos / 2;

        //allow cards to overlap if they dont fit in the space by using a different distance formula
        if ((lastCardPos + cardDistance) > handRegionWidth)
        {
            //recalculate variables
            cardDistance = (handRegionWidth - (cardWidth/2)) / currentHandSize;
            lastCardPos  = (cardDistance * (currentHandSize - 1));
            handMidpoint = lastCardPos / 2;
        }

        //calculate card idle height relative to the root instead of local position because nested layout elements can mess with the local one
        float globalHandHeight = transform.GetComponent<RectTransform>().position.y ;
        float localHandHeight = transform.root.InverseTransformPoint(0, globalHandHeight, 0).y;
        float idleHeight = localHandHeight + (cardHeight * idleHeightMod);

        //calculate positions and send them to the cards
        for (int c = 0; c < currentHandSize; c++)
        {
            cards[c].SendMessage("SetIdleLocation", new Vector2((cardDistance * c) - handMidpoint + handRegionMidpoint, idleHeight));
            cards[c].transform.SetAsLastSibling(); //update draw order also
        }
    }

    /// <summary>
    /// removes a card from the hand (the card is NOT destroyed; the card does that part)
    /// </summary>
    public void Discard(GameObject card)
    {
        //locate the card to remove
        int index = -1;
        for (int c = 0; c < currentHandSize; c++)
        {
            if (cards[c] == card)
            {
                index = c;
                break;
            }
        }

        //bail and print warning if that card didnt exist
        if (index == -1)
        {
            MessageHandlerScript.Warning("Attempted to discard something that doesn't exist!");
            return;
        }

        //move the others down the list
        for (int c = index; c < currentHandSize - 1; c++)
            cards[c] = cards[c+1];
        cards[maximumHandSize-1] = null; //the last slot is now guaranteed to be empty

        currentHandSize--; //decrement card count
        updateCardIdleLocations(); //rearrange the hand
    }

    /// <summary>
    /// hides the hand
    /// </summary>
    private void Hide()
    {
        handHidden = true;
        foreach (GameObject c in cards)
            if (c != null)
                c.SendMessage("Hide");
    }

    /// <summary>
    /// shows the hand
    /// </summary>
    private void Show()
    {
        handHidden = false;
        foreach (GameObject c in cards)
            if (c != null)
                c.SendMessage("Show");
    }

    /// <summary>
    /// (player hands only) attempts to remove d charges from random cards in the hand, discarding any that hit 0 charges.  
    /// returns how much damage was actually dealt
    /// </summary>
    public int Damage(int d)
    {
        //currently only compatible with player hands
        if (handOwner != HandFaction.player)
            throw new System.InvalidOperationException("Cannot damage hands other than the player hand");

        int alreadyDealt = 0;

        while ( (currentHandSize > 0) && (alreadyDealt < d) )
        {
            //pick a random card in the hand
            GameObject toDamage = cards[ Random.Range(0, currentHandSize) ];
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
    /// (enemy hands only) returns a list of all WaveData objects associated with cards in the hand
    /// </summary>
    public List<WaveData> IncomingWaves
    {
        get
        {
            List<WaveData> result = new List<WaveData>();
            foreach (GameObject ec in cards)
                if (ec != null)
                    result.Add( ec.GetComponent<EnemyCardScript>().wave );
            return result;
        }
    }

    /// <summary>
    /// (enemy hands only) returns total spawn count of all waves in the hand
    /// </summary>
    public int spawnCount
    {
        get
        {
            int result = 0;
            foreach(WaveData ew in IncomingWaves)
                    result += ew.spawnCount;
            return result;
        }
    }

    /// <summary>
    /// (enemy hands only) returns total remaining health of all waves in the hand
    /// </summary>
    public int totalRemainingHealth
    {
        get
        {
            try
            {
                return IncomingWaves.Sum(x => x.totalRemainingHealth);
            }
            catch (System.OverflowException)
            {
                Debug.LogWarning("totalRemainingHealth overflow!");
                return int.MaxValue;
            }
        }
    }

    /// <summary>
    /// (enemy hands only) returns longest spawn time among all cards in the hand
    /// </summary>
    public float longestTime
    {
        get
        {
            float result = 0.0f;
            foreach (WaveData ew in IncomingWaves)
                result = Mathf.Max(result, ew.time);
            return result;
        }
    }

    /// <summary>
    /// returns whether or not this hand is full
    /// </summary>
    public bool isFull { get { return currentHandSize >= maximumHandSize; } }

    /// <summary>
    /// (enemy hands only) discards the card associated with the given wave
    /// </summary>
    public void discardWave(WaveData toDiscard)
    {
        //only valid on enemy hands
        if (handOwner != HandFaction.enemy)
            throw new System.InvalidOperationException();

        //find the card to discard
        GameObject card = null;
        foreach (GameObject i in cards)
            if (i != null)
                if (i.GetComponent<EnemyCardScript>().wave == toDiscard)
                    card = i;

        //and discard it
        card.SendMessage("Discard");
    }

    /// <summary>
    /// prompts the user to choose a card in this hand with the given message.  
    /// if this is the selection hand, then options are the contents of the hand.
    /// if this is not the selection hand, then all cards except for exception are copied to the selection hand to serve as options
    /// </summary>
    /// <param name="exception">GameObject to exclude from the list of valid choices</param>
    /// <param name="prompt">message to show the player during the selection</param>
    /// <returns></returns>
    public IEnumerator selectCard(GameObject exception, string prompt)
    {
        selectedCard = null;

        switch (handOwner)
        {
            case HandFaction.player:
                //make CardPreviewScript objects for each card in hand except for exception
                Dictionary<CardScript, GameObject> previewCards = new Dictionary<CardScript, GameObject>();
                foreach (GameObject go in cards)
                {
                    //skip exception card and null cards
                    if ( (go == exception) || (go == null) )
                        continue;

                    //make previews
                    GameObject previewCard = (GameObject)Instantiate(previewCardPrefab,selectionHand.transform); //spawn object
                    previewCard.SendMessage("PreviewCard", go.GetComponent<PlayerCardScript>().card.data);       //send it the info
                    previewCard.transform.localPosition = (go.transform.localPosition);                          //spawn previews over their corresponding PlayerCardScripts
                    previewCards.Add(previewCard.GetComponent<CardPreviewScript>(), go);                         //add it to the selection hand
                    go.transform.localPosition -= new Vector3(0.0f, 1000.0f, 0.0f);                             //blink the actual card off screen
                }

                //set the hand as hidden, even though it was just teleported off-screen, just to make sure the cards dont come back on screen
                Hide();

                //send the new previewCards to the selectionHand and wait for a result
                yield return StartCoroutine(selectionHand.addCards(previewCards.Keys.ToArray(), false));
                yield return StartCoroutine(selectionHand.selectCard(null, prompt));

                //get a script reference back to the card in this hand that corresponds to the chosen previewCard
                GameObject selectedGameObject = null;
                if (previewCards.TryGetValue(selectionHand.selectedCard, out selectedGameObject))
                    selectedCard = selectedGameObject.GetComponent<CardScript>();
                else
                    Debug.LogError("invalid card selected!");

                //put the playerCardScripts back to where the previews are
                foreach (KeyValuePair<CardScript, GameObject> entry in previewCards)
                    entry.Value.transform.localPosition = entry.Key.transform.localPosition;

                //get rid of the preview cards
                yield return StartCoroutine(selectionHand.discardRandomCards(null, 999, false)); 

                //send the player cards back to their normal positions
                Show();

                yield break;
            
            case HandFaction.enemy:
                //send selectionHand previews and delegate
                Debug.LogWarning("HandScript.selectCard() does not work on enemy hands yet because enemy cards cannot be previewed");
                yield break;

            case HandFaction.selection:
                //if the hand is empty, bail now
                if (currentHandSize == 0)
                    yield break;

                //if the hand has only one card, just act as if that was the selection and return immediately
                if (currentHandSize == 1)
                {
                    selectedCard = cards[0].GetComponent<CardScript>();
                    yield break;
                }

                //wait for a selection to be made and return it
                while (selectedCard == null)
                    yield return null;
                yield break;

        }

        //if we made it here, the above code didnt actually select a card and something is probably wrong
        Debug.LogWarning("HandScript.selectCard() seems to have a problem");
        yield break; 
    }

    //wait for all cards in the hand to be ready
    public IEnumerator waitForReady()
    {
        yield return null;
        foreach (GameObject go in cards)
        {
            if (go == null)
                continue;

            //waiting twice because sometimes it can slip through in the one-frame gap between the card being drawn and being flipped face up
            yield return StartCoroutine(go.GetComponent<CardScript>().waitForReady());
            yield return StartCoroutine(go.GetComponent<CardScript>().waitForReady());
        }
    }

    //if a cardPreviewScript in this hand gets clicked on, store it as the selected card for the selectCard() coroutine
    private void cardPreviewClicked(CardScript card) { selectedCard = card; }

    public void updateEnemyCards()             { foreach (GameObject ec in cards) if (ec != null) ec.SendMessage("updateWaveStats"); }    //(enemy hands only) instructs all cards in the hand to refresh themselves
    public void applyWaveEffect(IEffectWave e) { foreach (GameObject ec in cards) if (ec != null) ec.SendMessage("applyWaveEffect", e); } //(enemy hands only) applies the wave effect to all cards in the hand
    public void UpdateWaveStats()              { foreach (GameObject ec in cards) if (ec != null) ec.SendMessage("updateWaveStats"); }    //(enemy hands only) updates wave stats for all cards in the hand
}