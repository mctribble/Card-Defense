using System.Collections;
using UnityEngine;

public class HandScript : MonoBehaviour
{
    public int          startingHandSize;   //cards the player starts with
    public int          maximumHandSize;    //max number of cards the player can have
    public Vector2      spawnLocation;      //where new cards appear
    public GameObject   cardPrefab;         //prefab used to spawn a new card
    public int          idealGap;           //ideal gap between cards
    public float        idleHeightMod;      //used to calculate height at which new cards should idle
    public float        initialDrawDelay;	//delay given between drawing each new card at the start of the level
    public bool         handHidden;         //whether or not the hand is hidden

    private GameObject[] cards;     //stores the number of cards
    private int currentHandSize;    //number of cards currently in hand

    // Use this for initialization
    // it is a coroutine for animation purposes
    private IEnumerator Start()
    {
        //wait for the level to load
        while (LevelManagerScript.instance.levelLoaded == false)
            yield return null;

        cards = new GameObject[maximumHandSize]; //construct array to hold the hand
        currentHandSize = 0; //no cards yet

        //draw starting hand
        handHidden = false;
        for (int i = 0; i < startingHandSize; i++)
        {
            yield return new WaitForSeconds(initialDrawDelay);
            drawCard();
        }

        yield break;
    }

    // Update is called once per frame
    private void Update()
    {
    }

    //draws a card from the deck
    private void drawCard()
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
        cards[currentHandSize] = (GameObject)Instantiate(cardPrefab);                       //instantiate card prefab
        cards[currentHandSize].transform.SetParent(transform.root);                         //declare the card a child of the UI object at the root of this tree
        cards[currentHandSize].GetComponent<RectTransform>().localPosition = spawnLocation; //position card
        cards[currentHandSize].GetComponent<RectTransform>().localScale = Vector3.one;      //reset scale because changing parent changes it
        cards[currentHandSize].SendMessage("SetHand", gameObject);                          //tell the card which hand owns it

        cards[currentHandSize].SendMessage("SetCard", DeckManagerScript.instance.Draw());	//send the card the data that defines it

        //if the hand is currently hidden, tell the new card to hide itself
        if (handHidden)
            cards[currentHandSize].SendMessage("Hide");

        currentHandSize++;  //increment card count

        //rearrange cards
        updateCardIdleLocations(); //rearrange the cards
    }

    //draws until the target card count
    private IEnumerator drawToHandSize(int target)
    {
        while ((currentHandSize < target) && (currentHandSize < maximumHandSize) && (DeckManagerScript.instance.cardsLeft > 0))
        {
            yield return new WaitForSeconds(1);
            drawCard();
        }
    }

    //discard a random card from the hand.  exemption card is safe
    private void discardRandom(GameObject exemption)
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
            Debug.Log("Attempted to discard something that doesn't exist!");
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