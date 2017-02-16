using System;
using System.Collections;
using UnityEngine;
using Vexe.Runtime.Types;

/// <summary>
/// all self effects take place instantly, and target the card which contains them.  This base effect handles behavior common to them all
/// </summary>
public abstract class BaseEffectSelf : BaseEffect, IEffectSelf
{
    [Hide] public override TargetingType targetingType { get { return TargetingType.none; } } //this effect doesnt need a target
    [Hide] public override EffectType effectType       { get { return EffectType.self; } }    //this is a discard effect

    public abstract void trigger(ref PlayerCard card, GameObject card_gameObject);
}

//adds x charges to the card
public class EffectAddCharges : BaseEffectSelf
{
    [Hide] public override string Name
    {
        get
        {
            if (strength < 0)
                return "Costs " + (strength - 1) * -1 + " charges to cast";

            if (strength == 0)
                return null;

            if (strength == 1.0f)
                return "Does not cost a charge to cast";

            return "Card gains " + (strength - 1) + " charges when cast.";
        }
    } 

    [Show] public override string XMLName { get { return "addCharges"; } } //name used to refer to this effect in XML

    public override void trigger(ref PlayerCard card, GameObject card_gameObject)
    {
        card.charges += Mathf.RoundToInt(strength);
    }
}

//discards x chosen cards from the hand in addition to this one.  They return to the bottom of the deck without damage
public class EffectDiscardChosen : BaseEffectSelf
{
    [Hide] public override string Name
    {
        get
        {
            if (strength == 1)
                return "Choose a card to discard";
            else
                return "Choose up to " + strength + " cards to discard";
        }
    } 
    [Show] public override string XMLName { get { return "discardChosenCard"; } }

    //this effect relies on user input, so it starts a coroutine
    public override void trigger(ref PlayerCard card, GameObject card_gameObject)
    {
        SelectionHandScript.instance.StartCoroutine(effectCoroutine(card_gameObject));
    }

    //asks player which cards to discard, then discards them
    private IEnumerator effectCoroutine(GameObject exception)
    {
        int count = Mathf.Min( (Mathf.RoundToInt(strength)), (PlayerHandScript.instance.currentHandSize-1) ); //number of cards to actually discard

        for (int i = 0; i < count; i++)
        {
            //wait for everything in the hand to stop moving around, in case other effects on the same card do something, like drawing/discarding cards, to affect how this should behave
            yield return PlayerHandScript.instance.StartCoroutine(PlayerHandScript.instance.waitForReady());

            //have player select any card in the player hand except this one
            yield return PlayerHandScript.instance.StartCoroutine(PlayerHandScript.instance.selectCard(exception, "Discard what? (" + (count - i) + " to go)"));
            CardScript selected = PlayerHandScript.instance.selectedCard;

            //discard the chosen card
            if (selected == null)
                Debug.LogWarning("HandScript failed to prompt user to pick a card!");
            else
                selected.StartCoroutine(selected.Discard());
        }
    }
}

//discards x random cards from the hand in addition to this one.  They return to the bottom of the deck without damage
public class EffectDiscardRandom : BaseEffectSelf
{
    [Hide] public override string Name { get { return "Discard " + strength + " random card" + ((strength == 1.0f) ? "": "s") ; } } //returns name and strength
    [Show] public override string XMLName { get { return "discardRandomCard"; } } //name used to refer to this effect in XML

    public override void trigger(ref PlayerCard card, GameObject card_gameObject)
    {
        HandScript handRef = PlayerHandScript.instance;
        handRef.StartCoroutine(handRef.discardRandomCards(card_gameObject.GetComponent<CardScript>(), Mathf.FloorToInt(strength)));
    }
}

//discards up to x random cards from the hand IN ADDITION TO this one, then draws a card for each card discarded EXCEPT FOR this one (therefore, if player has 7 cards and the effect replaces up to 7, their hand will be emptied and they will draw six)
public class EffectReplaceRandomCard : BaseEffectSelf
{
    [Hide] public override string Name { get { return "Discard up to " + strength + " cards at random, and draw new ones to replace them"; } } //returns name and strength
    [Show] public override string XMLName { get { return "replaceRandomCard"; } } //name used to refer to this effect in XML

    public override void trigger(ref PlayerCard card, GameObject card_gameObject) { PlayerHandScript.instance.StartCoroutine(effectCoroutine(card, card_gameObject)); }
    public IEnumerator effectCoroutine(PlayerCard card, GameObject card_gameObject)
    {
        int toReplace = Mathf.Min( Mathf.RoundToInt(strength), PlayerHandScript.instance.discardableCardCount-1);  //how many cards are being replaced

        //if we are replacing the entire hand, do them all at once with no delay
        bool applyDelay = true;
        if (toReplace == PlayerHandScript.instance.currentHandSize - 1)
            applyDelay = false;

        yield return PlayerHandScript.instance.StartCoroutine(PlayerHandScript.instance.discardRandomCards(card_gameObject.GetComponent<CardScript>(), toReplace, applyDelay)); //discard toReplace random cards that are NOT this one (this card will be discarded regardless, since it was just played)
        yield return null;
        yield return PlayerHandScript.instance.StartCoroutine(PlayerHandScript.instance.drawCards(toReplace, applyDelay)); //draw new cards to replace them
    }
}

//discards up to x random cards from the hand IN ADDITION TO this one, then draws spell card for each card discarded EXCEPT FOR this one (therefore, if player has 7 cards and the effect replaces up to 7, their hand will be emptied and they will draw six)
public class EffectReplaceRandomCardWithSpell : BaseEffectSelf
{
    [Hide] public override string Name { get { return "Discard up to " + strength + " cards at random, and draw spells to replace them"; } } //returns name and strength
    [Show] public override string XMLName { get { return "replaceRandomCardWithSpell"; } } //name used to refer to this effect in XML

    public override void trigger(ref PlayerCard card, GameObject card_gameObject) { PlayerHandScript.instance.StartCoroutine(effectCoroutine(card, card_gameObject)); }
    public IEnumerator effectCoroutine(PlayerCard card, GameObject card_gameObject)
    {
        int toReplace = Mathf.Min( Mathf.RoundToInt(strength), PlayerHandScript.instance.discardableCardCount-1);  //how many cards are being replaced

        //if we are replacing the entire hand, do them all at once with no delay
        bool applyDelay = true;
        if (toReplace == PlayerHandScript.instance.currentHandSize - 1)
            applyDelay = false;

        yield return PlayerHandScript.instance.StartCoroutine(PlayerHandScript.instance.discardRandomCards(card_gameObject.GetComponent<CardScript>(), toReplace, applyDelay)); //discard toReplace random cards that are NOT this one (this card will be discarded regardless, since it was just played)
        yield return null; //wait a frame for things to happen

        //draw new cards to replace them:
        PlayerCard[] cards = new PlayerCard[toReplace];

        //fill the array with the Cards we want to draw
        for (int i = 0; i < toReplace; i++)
        {
            //attempt to draw from the deck
            PlayerCard? drawn = DeckManagerScript.instance.DrawCardType(PlayerCardType.spell);

            if (drawn != null)
            {
                //the draw succeeded, so we can use it directly
                cards[i] = drawn.Value;
            }
            else
            {
                //the draw failed, so make a new card from thin air using the "Improvised Spell" token.
                PlayerCard newCard = new PlayerCard();
                newCard.data = CardTypeManagerScript.instance.getCardByName("Improvised Spell");
                newCard.charges = newCard.data.cardMaxCharges;
                cards[i] = newCard;
            }

        }

        //tell the hand to draw these specific Cards.
        PlayerHandScript.instance.StartCoroutine(PlayerHandScript.instance.drawCards(cards));
    }
}

//discards up to x random cards from the hand IN ADDITION TO this one, then draws tower cards for each card discarded EXCEPT FOR this one (therefore, if player has 7 cards and the effect replaces up to 7, their hand will be emptied and they will draw six)
public class EffectReplaceRandomCardWithTower : BaseEffectSelf
{
    [Hide] public override string Name { get { return "Discard up to " + strength + " cards at random, and draw towers to replace them"; } } //returns name and strength
    [Show] public override string XMLName { get { return "replaceRandomCardWithTower"; } } //name used to refer to this effect in XML

    public override void trigger(ref PlayerCard card, GameObject card_gameObject) { PlayerHandScript.instance.StartCoroutine(effectCoroutine(card, card_gameObject)); }
    public IEnumerator effectCoroutine(PlayerCard card, GameObject card_gameObject)
    {
        int toReplace = Mathf.Min( Mathf.RoundToInt(strength), PlayerHandScript.instance.discardableCardCount-1);  //how many cards are being replaced

        //if we are replacing the entire hand, do them all at once with no delay
        bool applyDelay = true;
        if (toReplace == PlayerHandScript.instance.currentHandSize - 1)
            applyDelay = false;

        yield return PlayerHandScript.instance.StartCoroutine(PlayerHandScript.instance.discardRandomCards(card_gameObject.GetComponent<CardScript>(), toReplace, applyDelay)); //discard toReplace random cards that are NOT this one (this card will be discarded regardless, since it was just played)
        yield return null; //wait a frame for things to happen

        //draw new cards to replace them:
        PlayerCard[] cards = new PlayerCard[toReplace];

        //fill the array with the Cards we want to draw
        for (int i = 0; i < toReplace; i++)
        {
            //attempt to draw from the deck
            PlayerCard? drawn = DeckManagerScript.instance.DrawCardType(PlayerCardType.tower);

            if (drawn != null)
            {
                //the draw succeeded, so we can use it directly
                cards[i] = drawn.Value;
            }
            else
            {
                //the draw failed, so make a new card from thin air using the "Improvised Spell" token.
                PlayerCard newCard = new PlayerCard();
                newCard.data = CardTypeManagerScript.instance.getCardByName("Improvised Tower");
                newCard.charges = newCard.data.cardMaxCharges;
                cards[i] = newCard;
            }

        }

        //tell the hand to draw these specific Cards.
        PlayerHandScript.instance.StartCoroutine(PlayerHandScript.instance.drawCards(cards));
    }
}

//discards up to x random cards from the hand IN ADDITION TO this one, then draws upgrade cards for each card discarded EXCEPT FOR this one (therefore, if player has 7 cards and the effect replaces up to 7, their hand will be emptied and they will draw six)
public class EffectReplaceRandomCardWithUpgrade : BaseEffectSelf
{
    [Hide] public override string Name { get { return "Discard up to " + strength + " cards at random, and draw upgrades to replace them"; } } //returns name and strength
    [Show] public override string XMLName { get { return "replaceRandomCardWithUpgrade"; } } //name used to refer to this effect in XML

    public override void trigger(ref PlayerCard card, GameObject card_gameObject) { PlayerHandScript.instance.StartCoroutine(effectCoroutine(card, card_gameObject)); }
    public IEnumerator effectCoroutine(PlayerCard card, GameObject card_gameObject)
    {
        int toReplace = Mathf.Min( Mathf.RoundToInt(strength), PlayerHandScript.instance.discardableCardCount-1);  //how many cards are being replaced

        //if we are replacing the entire hand, do them all at once with no delay
        bool applyDelay = true;
        if (toReplace == PlayerHandScript.instance.currentHandSize - 1)
            applyDelay = false;

        yield return PlayerHandScript.instance.StartCoroutine(PlayerHandScript.instance.discardRandomCards(card_gameObject.GetComponent<CardScript>(), toReplace, applyDelay)); //discard toReplace random cards that are NOT this one (this card will be discarded regardless, since it was just played)
        yield return null; //wait a frame for things to happen

        //draw new cards to replace them:
        PlayerCard[] cards = new PlayerCard[toReplace];

        //fill the array with the Cards we want to draw
        for (int i = 0; i < toReplace; i++)
        {
            //attempt to draw from the deck
            PlayerCard? drawn = DeckManagerScript.instance.DrawCardType(PlayerCardType.upgrade);

            if (drawn != null)
            {
                //the draw succeeded, so we can use it directly
                cards[i] = drawn.Value;
            }
            else
            {
                //the draw failed, so make a new card from thin air using the "Improvised Spell" token.
                PlayerCard newCard = new PlayerCard();
                newCard.data = CardTypeManagerScript.instance.getCardByName("Improvised Upgrade");
                newCard.charges = newCard.data.cardMaxCharges;
                cards[i] = newCard;
            }

        }

        //tell the hand to draw these specific Cards.
        PlayerHandScript.instance.StartCoroutine(PlayerHandScript.instance.drawCards(cards));
    }
}