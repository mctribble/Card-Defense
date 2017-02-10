using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using Vexe.Runtime.Types;
using System.Linq;

public class EnemyHandScript : HandScript
{
    public Image       deckImage;        //image that serves as the spawn point for new cards
    public GameObject  enemyCardPrefab;  //prefab used to spawn a new enemy card

    [Hide] public static EnemyHandScript instance; //singleton instance

    protected override IEnumerator Start()
    {
        yield return base.Start();
        instance = this;

        while (LevelManagerScript.instance.wavesInDeck == 0)
            yield return null;

        //draw starting hand
        yield return drawToHandSize(startingHandSize);
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
    /// <param name="ignoreHandCap">if this is true, the hand can draw even if it is full</param>
    public void drawCard(bool flipOver = true, bool turnToIdentity = true, bool scaleToIdentity = true, bool drawSurvivorWave = false, bool ignoreHandCap = false)
    {
        //bail if reached max
        if ((currentHandSize == maximumHandSize) && (ignoreHandCap == false))
        {
            Debug.Log("Can't draw: hand is full.");
            return;
        }

        //bail if the relevant deck out of cards
        if (LevelManagerScript.instance.wavesInDeck == 0)
        {
            if (drawSurvivorWave == false)  //an empty enemy deck is fine if we're drawing a survivor wave instead of from the deck
            {
                if (LevelManagerScript.instance.endurance == false) //it is also fine in endurance
                {
                    Debug.Log("Can't draw: enemy deck empty.");
                    return;
                }
            }
        }

        //instantiate card prefab
        CardScript newCard;
        newCard = Instantiate(enemyCardPrefab).GetComponent<CardScript>();

        newCard.transform.SetParent(transform.root); //declare the new card a child of the UI object at the root of this tree

        //position card to match up with the deck image we are spawning at
        RectTransform spawnT = deckImage.rectTransform;
        newCard.GetComponent<RectTransform>().position = spawnT.position;
        newCard.GetComponent<RectTransform>().rotation = spawnT.rotation;
        newCard.GetComponent<RectTransform>().localScale = spawnT.localScale;

        //add the card to the hand
        addCard(newCard, flipOver, turnToIdentity, scaleToIdentity);

        //send the card the data that defines it
        if (drawSurvivorWave == false)
            newCard.SendMessage("SetWave", LevelManagerScript.instance.DrawEnemy());
        else
            newCard.SendMessage("SurvivorWave");

        LevelManagerScript.instance.UpdateWaveStats();
        
        newCard.SendMessage("triggerOnDrawnEffects"); //trigger effects
    }

    /// <summary>
    /// draws a new card created from the given wave
    /// </summary>
    public void drawCard(WaveData wave)
    {
        //instantiate card prefab
        CardScript newCard;
        newCard = Instantiate(enemyCardPrefab).GetComponent<CardScript>();

        newCard.transform.SetParent(transform.root); //declare the new card a child of the UI object at the root of this tree

        //position card to match up with the deck image we are spawning at
        RectTransform spawnT = deckImage.rectTransform;
        newCard.GetComponent<RectTransform>().position = spawnT.position;
        newCard.GetComponent<RectTransform>().rotation = spawnT.rotation;
        newCard.GetComponent<RectTransform>().localScale = spawnT.localScale;

        //add the card to the hand
        addCard(newCard);

        //send it the data
        newCard.SendMessage("SetWave", wave);

        LevelManagerScript.instance.UpdateWaveStats();

        newCard.SendMessage("triggerOnDrawnEffects"); //trigger effects
    }

    //we neeed to update wave stats when cards are added to this hand
    public override void addCard(CardScript cardToAdd, bool flipOver = true, bool turnToIdentity = true, bool scaleToIdentity = true)
    {
        base.addCard(cardToAdd, flipOver, turnToIdentity, scaleToIdentity);
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
        //draw the cards
        while (drawCount > 0)
        {
            drawCount--;
            drawCard(false); //dont flip them over just yet: we want to do them all at once since they were drawn as a group

            if (delay)
                yield return new WaitForSeconds(drawDelay);
        }

        //if the deck is empty, we may not be able to draw any cards.  bail if the hand is still empty at this point
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
    /// draws multiple waves that already exist, such as from conjureEnemyCard
    /// </summary>
    public IEnumerator drawCards(List<WaveData> waves, bool delay = true)
    {
        //draw the cards
        foreach(WaveData w in waves)
        {
            drawCard(w);

            if (delay)
                yield return new WaitForSeconds(drawDelay);
        }

        //wait for all cards to be idle
        while (cards.All(c => (c.state == CardState.idle) || (c.state == CardState.discarding)))
            yield return null;

        //flip the entire hand face up at once
        foreach (CardScript c in cards)
            c.SendMessage("flipFaceUp");

        yield break;
    }

    /// <summary>
    /// (enemy hands only) returns a list of all WaveData objects associated with cards in the hand
    /// </summary>
    public List<WaveData> IncomingWaves
    {
        get
        {
            List<WaveData> result = new List<WaveData>();
            foreach (CardScript ec in cards)
                if (ec != null)
                    result.Add(ec.GetComponent<EnemyCardScript>().wave);
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
            foreach (WaveData ew in IncomingWaves)
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
    /// (enemy hands only) discards the card associated with the given wave
    /// </summary>
    public void discardWave(WaveData toDiscard)
    {
        //find the card to discard
        CardScript card = null;
        foreach (CardScript c in cards)
            if (c != null)
                if (c.GetComponent<EnemyCardScript>().wave == toDiscard)
                    card = c;

        //and discard it
        card.SendMessage("Discard");
    }

    public void updateEnemyCards()             { foreach (CardScript c in cards) if (c != null) c.SendMessage("updateWaveStats"); }    //instructs all cards in the hand to refresh themselves
    public void applyWaveEffect(IEffectWave e) { foreach (CardScript c in cards) if (c != null) c.SendMessage("applyWaveEffect", e); } //applies the wave effect to all cards in the hand
    public void UpdateWaveStats()              { foreach (CardScript c in cards) if (c != null) c.SendMessage("updateWaveStats"); }    //updates wave stats for all cards in the hand

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
