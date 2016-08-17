using System.Collections;
using UnityEngine;
using Vexe.Runtime.Types;

public class HandScript : BaseBehaviour
{
    public int        startingHandSize; //cards the player starts with
    public int        maximumHandSize;  //max number of cards the player can have
    public Vector2    spawnLocation;    //where new cards appear
    public GameObject cardPrefab;       //prefab used to spawn a new card
    public int        idealGap;         //ideal gap between cards
    public float      idleHeightMod;    //used to calculate height at which new cards should idle
    public float      drawDelay;	    //delay given between drawing multiple cards
    public float      discardDelay;     //delay given between discarding multiple cards
    public bool       handHidden;       //whether or not the hand is hidden

    private GameObject[] cards;  //stores the number of cards
    private int currentHandSize; //number of cards currently in hand
    private bool discarding;     //if true, multiple cards are currently being discarded and we need to wait before drawing

    // Use this for initialization
    // it is a coroutine for animation purposes
    private IEnumerator Start()
    {
        //wait for the level to load
        while (LevelManagerScript.instance.levelLoaded == false)
            yield return null;

        cards = new GameObject[maximumHandSize]; //construct array to hold the hand
        currentHandSize = 0; //no cards yet
        discarding = false; //we are not discarding cards

        //draw starting hand
        handHidden = false;
        yield return drawToHandSize(startingHandSize);

        yield break;
    }

    // Update is called once per frame
    private void Update()
    {
    }

    //draws a card from the deck, and flips it face up if flipOver is true
    private void drawCard(bool flipOver = true)
    {
        //bail if reached max
        if (currentHandSize == maximumHandSize)
        {
            Debug.Log("Can't draw: hand is full.");
            return;
        }

        //bail if deck out of cards
        if (DeckManagerScript.instance.cardsLeft == 0)
        {
            Debug.Log("Can't draw: out of cards.");
            return;
        }

        //card setup
        cards[currentHandSize] = Instantiate(cardPrefab);                                   //instantiate card prefab
        cards[currentHandSize].transform.SetParent(transform.root);                         //declare the card a child of the UI object at the root of this tree
        cards[currentHandSize].GetComponent<RectTransform>().localPosition = spawnLocation; //position card
        cards[currentHandSize].GetComponent<RectTransform>().localScale = Vector3.one;      //reset scale because changing parent changes it
        cards[currentHandSize].SendMessage("SetHand", gameObject);                          //tell the card which hand owns it
        cards[currentHandSize].SendMessage("SetCard", DeckManagerScript.instance.Draw());	//send the card the data that defines it

        //if set, tell it to flip face up after its done moving
        if (flipOver)
            cards[currentHandSize].SendMessage("flipWhenIdle");

        //if the hand is currently hidden, tell the new card to hide itself
        if (handHidden)
            cards[currentHandSize].SendMessage("Hide");

        currentHandSize++;  //increment card count

        //rearrange cards
        updateCardIdleLocations(); //rearrange the cards
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
        { yield return null; }
        while (discarding);

        //draw the cards
        while (drawCount > 0)
        {
            yield return new WaitForSeconds(drawDelay);
            drawCount--;
            drawCard(false); //dont flip them over just yet: we want to do them all at once since they were drawn as a group
        }

        //wait for the last card to be idle, since it will be the last one to finish moving around
        CardScript waitCard = cards[currentHandSize - 1].GetComponent<CardScript>();
        yield return StartCoroutine(waitCard.waitForIdle());

        //flip the entire hand face up at once
        foreach (GameObject c in cards)
            if (c != null)
                c.SendMessage("faceUp");

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
        int cardWidth  = (int)cards [0].GetComponent<RectTransform> ().rect.width;
        int cardHeight = (int)cards[0].GetComponent<RectTransform>().rect.height;

        //figure out how far away the cards need to be from each other
        int cardDistance = cardWidth + idealGap;

        //calculate the midpoint of the hand to offset it properly
        int lastCardPos  = (cardDistance * (currentHandSize - 1));
        int handMidpoint = lastCardPos / 2;

        //allow cards to overlap if they dont fit on screen by using a different distance formula
        int screenWidth  = (int)transform.root.gameObject.GetComponent<RectTransform> ().rect.width;
        if ((lastCardPos + cardDistance) > screenWidth)
        {
            //recalculate variables
            cardDistance = (screenWidth - (cardWidth/2)) / currentHandSize;
            lastCardPos  = (cardDistance * (currentHandSize - 1));
            handMidpoint = lastCardPos / 2;
        }

        //calculate card idle height
        int screenHeight = (int)transform.root.gameObject.GetComponent<RectTransform>().rect.height;
        float idleHeight = (screenHeight / -2) + (cardHeight * idleHeightMod);

        //calculate positions and send them to the cards
        for (int c = 0; c < currentHandSize; c++)
        {
            cards[c].SendMessage("SetIdleLocation", new Vector2((cardDistance * c) - handMidpoint, idleHeight));
            cards[c].transform.SetSiblingIndex(c); //update draw order also
        }
    }

    //attempts to remove d charges from random cards in the hand, discarding any that hit 0 charges.  returns how much damage was actually dealt
    public int Damage(int d)
    {
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
}