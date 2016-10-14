using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Vexe.Runtime.Types;

//used to indicate hand ownership
public enum HandFaction { player, enemy, neutral };

public class HandScript : BaseBehaviour
{
    //static references to the player hand and the enemy hand so other objects can easily access them
    [Hide] public static HandScript playerHand;
    [Hide] public static HandScript enemyHand;

    //references
    public Image       deckImage;        //image that serves as the spawn point for new cards
    public GameObject  playerCardPrefab; //prefab used to spawn a new  playercard
    public GameObject  enemyCardPrefab;  //prefab used to spawn a new enemy card

    //hand status
    public HandFaction handOwner;        //indicates ownership of the hand
    public int         startingHandSize; //cards the player starts with
    public int         maximumHandSize;  //max number of cards the player can have
    public int         currentHandSize;  //number of cards currently in hand
    public bool        handHidden;       //whether or not the hand is hidden

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

        cards = new GameObject[maximumHandSize]; //construct array to hold the hand
        currentHandSize = 0; //no cards yet
        discarding = false; //we are not discarding cards

        //draw starting hand
        handHidden = false;
        yield return drawToHandSize(startingHandSize);

        yield break;
    }

    //[DEV] creates buttons in the inspector to manipulate the hand
    [Show] private void devDraw() { drawCard(); }
    [Show] private void devDiscard() { discardRandomCard(null); }
    [Show] private void devRecycle()
    {
        int cardCount = currentHandSize;
        StartCoroutine(discardRandomCards(null, cardCount));
        StartCoroutine(drawCards(cardCount));
    }

    //draws a card from the deck.  The card spawns face down at the same position, rotation, and scale as the spawn point image.
    //flipOver: card should flip over AFTER moving
    //turnToIdentity: card should straighten itself while moving
    //scaleToIdentity: card should scale itself to (1,1,1) while moving
    //drawSurvivorWave: (enemy hands only) sets up the new card with a survivor wave instead of a wave from the deck
    public void drawCard(bool flipOver = true, bool turnToIdentity = true, bool scaleToIdentity = true, bool drawSurvivorWave = false)
    {
        //unsupported for neutral hands
        if (handOwner == HandFaction.neutral)
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
        if (handOwner == HandFaction.player)
            cards[currentHandSize] = Instantiate(playerCardPrefab);
        else if (handOwner == HandFaction.enemy)
            cards[currentHandSize] = Instantiate(enemyCardPrefab);

        cards[currentHandSize].transform.SetParent(transform.root); //declare the new card a child of the UI object at the root of this tree

        //position card to match up with the deck image we are spawning at
        RectTransform spawnT = deckImage.rectTransform;
        cards[currentHandSize].GetComponent<RectTransform>().position   = spawnT.position;
        cards[currentHandSize].GetComponent<RectTransform>().rotation   = spawnT.rotation;
        cards[currentHandSize].GetComponent<RectTransform>().localScale = spawnT.localScale;

        cards[currentHandSize].SendMessage("SetHand", gameObject);     //tell the card which hand owns it
        if (handOwner == HandFaction.player)
            cards[currentHandSize].SendMessage("SetDeckImage", deckImage); //also tell the card where the deck image is so it knows where to go if it returns there

        //send the card the data that defines it
        if (handOwner == HandFaction.player)
        {
            cards[currentHandSize].SendMessage("SetCard", DeckManagerScript.instance.Draw());
        }
        else if (handOwner == HandFaction.enemy)
        {
            if (drawSurvivorWave == false)
                cards[currentHandSize].SendMessage("SetWave", LevelManagerScript.instance.DrawEnemy());
            else
                cards[currentHandSize].SendMessage("SurvivorWave");
        }
        else
        {
            MessageHandlerScript.Error("don't know how to draw for a hand with this owner");
        }

        //if set, tell it to turn upright
        if (turnToIdentity)
            cards[currentHandSize].SendMessage("turnToQuaternion", Quaternion.identity);

        //if set, tell it to scale to its normal size
        if (scaleToIdentity)
            cards[currentHandSize].SendMessage("scaleToVector", Vector3.one);

        //if set, tell it to flip face up after its done moving
        if (flipOver)
            cards[currentHandSize].SendMessage("flipWhenIdle");

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

    //draws until the target card count
    public IEnumerator drawToHandSize(int target)
    {
        int drawCount = target - currentHandSize;
        yield return drawCards(drawCount);
    }

    //draws multiple cards
    public IEnumerator drawCards(int drawCount)
    {
        //wait to make sure we dont attempt to discard and draw at the same time (i. e., cards like Recycle that cause bothd iscarding and drawing simultaneously)
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
            yield return new WaitForSeconds(drawDelay);
        }

        //if the deck is empty, we may not be able to draw any cards.  bail if the hand is still empty at this point.
        if (currentHandSize == 0)
            yield break;

        //wait for the last card to be idle, since it will be the last one to finish moving around
        if (handOwner == HandFaction.player)
        {
            CardScript waitCard = cards[currentHandSize - 1].GetComponent<CardScript>();
            yield return StartCoroutine(waitCard.waitForIdle());
        }
        else
        {
            EnemyCardScript waitCard = cards[currentHandSize - 1].GetComponent<EnemyCardScript>();
            yield return StartCoroutine(waitCard.waitForIdle());
        }

        //flip the entire hand face up at once
        foreach (GameObject c in cards)
            if (c != null)
                c.SendMessage("flipFaceUp");

        yield break;
    }

    //helper: calls the normal version repeatedly
    public IEnumerator discardRandomCards(GameObject exemption, int count)
    {
        discarding = true;
        for (uint i = 0; i < count; i++)
        {
            discardRandomCard(exemption);
            if (currentHandSize == 0)
                break;
            yield return new WaitForSeconds(discardDelay);
        }
        discarding = false;
    } 
    
    //discard a random card from the hand.  exemption card is safe
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

    //to be called whenever the number of cards changes: updates all the cards to tell them where they should be
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

    //removes a card from the hand (the card is NOT destroyed; the card does that part)
    private void Discard(GameObject card)
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

    //hides the hand
    private void Hide()
    {
        handHidden = true;
        foreach (GameObject c in cards)
            if (c != null)
                c.SendMessage("Hide");
    }

    //shows the hand
    private void Show()
    {
        handHidden = false;
        foreach (GameObject c in cards)
            if (c != null)
                c.SendMessage("Show");
    }

    //(player hands only) attempts to remove d charges from random cards in the hand, discarding any that hit 0 charges.  returns how much damage was actually dealt
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
            CardScript scriptRef = toDamage.GetComponent<CardScript>();

            //deal damage to it
            int toDeal = Mathf.Min (scriptRef.card.charges, (d - alreadyDealt) );
            scriptRef.card.charges -= toDeal;
            alreadyDealt += toDeal;

            if (scriptRef.card.charges == 0)
                toDamage.SendMessage("Discard"); //discard the card if it is out of charges
            else
                scriptRef.updateChargeText(); //otherwise, tell the card to update its header text
        }

        return alreadyDealt;
    }

    //(enemy hands only) returns a list of all WaveData objects associated with cards in the hand
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

    //(enemy hands only) returns total spawn count of all waves in the hand
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

    //(enemy hands only) returns total remaining health of all waves in the hand
    public int totalRemainingHealth
    {
        get
        {
            return IncomingWaves.Sum(x => x.totalRemainingHealth);
        }
    }

    //(enemy hands only) returns longest spawn time among all cards in the hand
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

    public bool isFull { get { return currentHandSize >= maximumHandSize; } } //helper: returns whether or not this hand is full

    //(enemy hands only) discards the card associated with the given wave
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

    public void updateEnemyCards()             { foreach (GameObject ec in cards) if (ec != null) ec.SendMessage("updateWaveStats"); }    //(enemy hands only) instructs all cards in the hand to refresh themselves
    public void applyWaveEffect(IEffectWave e) { foreach (GameObject ec in cards) if (ec != null) ec.SendMessage("applyWaveEffect", e); } //(enemy hands only) applies the wave effect to all cards in the hand
    public void UpdateWaveStats()              { foreach (GameObject ec in cards) if (ec != null) ec.SendMessage("updateWaveStats"); }    //(enemy hands only) updates wave stats for all cards in the hand


}