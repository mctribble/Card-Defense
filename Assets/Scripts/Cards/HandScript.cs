using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Vexe.Runtime.Types;

/// <summary>
/// represents a group of cards being shown on the screens
/// </summary>
public abstract class HandScript : BaseBehaviour
{
    //hand status
    public int         startingHandSize; //cards the hand starts with
    public int         maximumHandSize;  //max number of cards the hand can have
    public bool        handHidden;       //whether or not the hand is currently hidden
    [Hide] public CardScript  selectedCard;     //holds return value for selectCard() coroutine

    public int         currentHandSize {get { return cards.Count; } }  //number of cards currently in hand

    //returns the number of cards in the hand that can be discarded
    public int discardableCardCount { get { return cards.Count(go => go != null && go.GetComponent<CardScript>().discardable); } }

    //positioning and timing
    public int   idealGap;      //ideal gap between cards
    public float idleHeightMod; //used to calculate height at which new cards should idle
    public float drawDelay;	    //delay given between drawing multiple cards
    public float discardDelay;  //delay given between discarding multiple cards
    
    //private info
    protected List<CardScript> cards; //stores the number of cards
    protected bool busy;              //if true, we are currently drawing/discarding and we need to wait before starting another such operation

    // Use this for initialization
    // it is a coroutine for animation purposes
    protected virtual IEnumerator Start()
    {
        cards = new List<CardScript>(); //construct array to hold the hand
        busy = false; //we are not busy
        selectedCard = null; //no selection yet

        //wait for the level to load
        while (LevelManagerScript.instance.levelLoaded == false)
            yield return null;

        handHidden = false;

        yield break;
    }

    /// <summary>
    /// [COROUTINE] empties out the hand, waits for the level to be loaded if it isn't already, then draws a new hand
    /// </summary>
    public virtual IEnumerator Reset()
    {
        //explicitly remove everything so we even get rid of cards that are not discardable
        foreach (CardScript c in cards)
            Destroy(c.gameObject);

        //behave as if the hand was just created
        yield return StartCoroutine(Start());
    }

    /// <summary>
    /// adds a card that already exists the hand.
    /// all parameters are optional.
    /// </summary>
    /// <param name="cardToAdd">the card to add</param>
    /// <param name="flipOver">flipOver: card should flip over AFTER moving</param>
    /// <param name="turnToIdentity">turnToIdentity: card should straighten itself while moving</param>
    /// <param name="scaleToIdentity">scaleToIdentity: card should scale itself to (1,1,1) while moving</param>
    public virtual void addCard(CardScript cardToAdd, bool flipOver = true, bool turnToIdentity = true, bool scaleToIdentity = true)
    {
        cards.Add(cardToAdd); //add the card to the hand

        cardToAdd.SendMessage("SetHand", gameObject);     //tell the card which hand owns it

        //if set, tell it to turn upright
        if (turnToIdentity)
            cardToAdd.SendMessage("turnToQuaternion", Quaternion.identity);

        //if set, tell it to scale to its normal size
        if (scaleToIdentity)
            cardToAdd.SendMessage("scaleToVector", Vector3.one);

        //if set, tell it to flip face up after its done moving
        if (flipOver)
            cardToAdd.SendMessage("flipFaceUpWhenIdle");

        //if the hand is currently hidden, tell the new card to hide itself
        if (handHidden)
            cardToAdd.SendMessage("Hide");

        //rearrange cards
        updateCardIdleLocations(); //rearrange the cards
    }

    /// <summary>
    /// adds multiple CardScripts that already exist instead of drawing them
    /// If delay is true, there is a slight pause between each one.
    /// </summary>
    public IEnumerator addCards(CardScript[] cardsToAdd, bool delay = true)
    {
        //wait until we arent busy
        do
        {
            yield return null;
        }
        while (busy);
        busy = true;

        //draw the cards
        foreach (CardScript c in cardsToAdd)
        {
            //draw the new card, being sure not to flip it over yet since we want to flip all of them as a group later
            addCard(c, false);

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
    /// [COROUTINE] discards multiple random cards.
    /// </summary>
    /// <param name="exemption">if not null, this card cannot be discarded</param>
    /// <param name="count">number to discard</param>
    /// <param name="delay">whether or not to pause between discards</param>
    public IEnumerator discardRandomCards(CardScript exemption, int count, bool delay = true)
    {
        busy = true;
        for (uint i = 0; i < count; i++)
        {
            discardRandomCard(exemption);
            if (currentHandSize == 0)
                break;

            if (delay) 
                yield return new WaitForSeconds(discardDelay);
        }
        busy = false;
    }

    /// <summary>
    /// discard a random card from the hand.  exemption card is safe
    /// </summary>
    public void discardRandomCard(CardScript exemption)
    {
        //special case: no discardable cards
        if (discardableCardCount == 0)
            return;

        CardScript[] discardableCards = cards.Where(c => c != null && c.discardable).ToArray(); //get an array of cards we can actually discard to simplify the rest of this code

        //special case: only one card
        if (discardableCards.Length == 1)
        {
            if (discardableCards[0] != exemption)
            {
                discardableCards[0].SendMessage("Discard");
            }
            return;
        }

        //general case: multiple cards
        CardScript target = null;
        while (target == null) //loop because we might randomly pick the exempt card
        {
            //pick a card
            int i = Random.Range(0, discardableCards.Length);
            target = discardableCards[i];

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
    public void Discard(CardScript card)
    {
        bool result = cards.Remove(card);

        //bail and print warning if that card didnt exist
        if (result == false)
        {
            Debug.LogWarning("Attempted to discard something that doesn't exist!");
            return;
        }

        updateCardIdleLocations(); //rearrange the hand
    }

    /// <summary>
    /// hides the hand
    /// </summary>
    public void Hide()
    {
        handHidden = true;
        foreach (CardScript c in cards)
            if (c != null)
                c.SendMessage("Hide");
    }

    /// <summary>
    /// shows the hand
    /// </summary>
    public void Show()
    {
        handHidden = false;
        foreach (CardScript c in cards)
            if (c != null)
                c.SendMessage("Show");
    }

    /// <summary>
    /// returns whether or not this hand is full
    /// </summary>
    public bool isFull { get { return currentHandSize >= maximumHandSize; } }
    
    //wait for all cards in the hand to be ready
    public IEnumerator waitForReady()
    {
        yield return null;

        //waiting twice because sometimes it can slip through in the one-frame gap between the card being drawn and being flipped face up
        do
            yield return null;
        while (cards.Any(c => c.isReady() == false));

        do
            yield return null;
        while (cards.Any(c => c.isReady() == false));
    }
}